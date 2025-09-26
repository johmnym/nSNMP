using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.PDUs;
using nSNMP.Message;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI
{
    public class BERSerializationTests
    {
        [Fact]
        public void Integer_RoundTrip_Works()
        {
            var original = Integer.Create(42);

            var encoded = original.ToBytes();
            var decoded = (Integer)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void Integer_NegativeValue_RoundTrip_Works()
        {
            var original = Integer.Create(-123);

            var encoded = original.ToBytes();
            var decoded = (Integer)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void OctetString_RoundTrip_Works()
        {
            var original = OctetString.Create("Hello, SNMP!");

            var encoded = original.ToBytes();
            var decoded = (OctetString)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void OctetString_EmptyString_RoundTrip_Works()
        {
            var original = OctetString.Create("");

            var encoded = original.ToBytes();
            var decoded = (OctetString)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void ObjectIdentifier_RoundTrip_Works()
        {
            var original = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 2, 1, 1, 1, 0 });

            var encoded = original.ToBytes();
            var decoded = (ObjectIdentifier)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void Null_RoundTrip_Works()
        {
            var original = new Null();

            var encoded = original.ToBytes();
            var decoded = (Null)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Value, decoded.Value);
        }

        [Fact]
        public void Sequence_Empty_RoundTrip_Works()
        {
            var original = new Sequence(new IDataType[] { });

            var encoded = original.ToBytes();
            var decoded = (Sequence)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Elements.Count, decoded.Elements.Count);
        }

        [Fact]
        public void Sequence_WithElements_RoundTrip_Works()
        {
            var elements = new IDataType[]
            {
                Integer.Create(123),
                OctetString.Create("test"),
                new Null()
            };
            var original = new Sequence(elements);

            var encoded = original.ToBytes();
            var decoded = (Sequence)SMIDataFactory.Create(encoded);

            Assert.Equal(original.Elements.Count, decoded.Elements.Count);
            Assert.Equal(123, ((Integer)decoded.Elements[0]).Value);
            Assert.Equal("test", ((OctetString)decoded.Elements[1]).Value);
            Assert.IsType<Null>(decoded.Elements[2]);
        }

        [Fact]
        public void GetRequest_RoundTrip_Works()
        {
            var original = new GetRequest(
                null,
                Integer.Create(12345),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (GetRequest)SMIDataFactory.Create(encoded);

            Assert.Equal(12345, decoded.RequestId.Value);
            Assert.Equal(0, decoded.Error.Value);
            Assert.Equal(0, decoded.ErrorIndex.Value);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void SetRequest_RoundTrip_Works()
        {
            var original = new SetRequest(
                null,
                Integer.Create(54321),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (SetRequest)SMIDataFactory.Create(encoded);

            Assert.Equal(54321, decoded.RequestId.Value);
            Assert.Equal(0, decoded.Error.Value);
            Assert.Equal(0, decoded.ErrorIndex.Value);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void GetResponse_RoundTrip_Works()
        {
            var original = new GetResponse(
                null,
                Integer.Create(98765),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (GetResponse)SMIDataFactory.Create(encoded);

            Assert.Equal(98765, decoded.RequestId.Value);
            Assert.Equal(0, decoded.Error.Value);
            Assert.Equal(0, decoded.ErrorIndex.Value);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void SnmpMessage_RoundTrip_Works()
        {
            var pdu = new GetRequest(
                null,
                Integer.Create(1),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var original = new SnmpMessage(
                SnmpVersion.V1,
                OctetString.Create("public"),
                pdu
            );

            var encoded = original.ToBytes();
            var decoded = SnmpMessage.Create(encoded);

            Assert.Equal(SnmpVersion.V1, decoded.Version);
            Assert.Equal("public", decoded.CommunityString.Value);
            Assert.IsType<GetRequest>(decoded.PDU);
            Assert.Equal(1, decoded.PDU.RequestId.Value);
        }
    }
}