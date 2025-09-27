using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.PDUs
{
    public class NewPDUTests
    {
        [Fact]
        public void GetNextRequest_RoundTrip_Works()
        {
            var original = new GetNextRequest(
                null,
                Integer.Create(12345),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (GetNextRequest)SMIDataFactory.Create(encoded);

            Assert.Equal(12345, decoded.RequestId?.Value ?? 0);
            Assert.Equal(0, decoded.Error?.Value ?? 0);
            Assert.Equal(0, decoded.ErrorIndex?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void GetBulkRequest_RoundTrip_Works()
        {
            var original = new GetBulkRequest(
                null,
                Integer.Create(54321),
                Integer.Create(2), // Non-repeaters
                Integer.Create(10), // Max-repetitions
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (GetBulkRequest)SMIDataFactory.Create(encoded);

            Assert.Equal(54321, decoded.RequestId?.Value ?? 0);
            Assert.Equal(2, decoded.NonRepeaters?.Value ?? 0);
            Assert.Equal(10, decoded.MaxRepetitions?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void InformRequest_RoundTrip_Works()
        {
            var original = new InformRequest(
                null,
                Integer.Create(98765),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (InformRequest)SMIDataFactory.Create(encoded);

            Assert.Equal(98765, decoded.RequestId?.Value ?? 0);
            Assert.Equal(0, decoded.Error?.Value ?? 0);
            Assert.Equal(0, decoded.ErrorIndex?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void TrapV2_RoundTrip_Works()
        {
            var original = new TrapV2(
                null,
                Integer.Create(13579),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (TrapV2)SMIDataFactory.Create(encoded);

            Assert.Equal(13579, decoded.RequestId?.Value ?? 0);
            Assert.Equal(0, decoded.Error?.Value ?? 0);
            Assert.Equal(0, decoded.ErrorIndex?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void Report_RoundTrip_Works()
        {
            var original = new Report(
                null,
                Integer.Create(24680),
                Integer.Create(0),
                Integer.Create(0),
                new Sequence(new IDataType[] { })
            );

            var encoded = original.ToBytes();
            var decoded = (Report)SMIDataFactory.Create(encoded);

            Assert.Equal(24680, decoded.RequestId?.Value ?? 0);
            Assert.Equal(0, decoded.Error?.Value ?? 0);
            Assert.Equal(0, decoded.ErrorIndex?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void TrapV1_RoundTrip_Works()
        {
            var enterprise = ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 4, 1, 12345 });
            var agentAddr = global::nSNMP.SMI.DataTypes.V1.Primitive.IpAddress.Create("192.168.1.100");
            var genericTrap = Integer.Create(6); // enterpriseSpecific
            var specificTrap = Integer.Create(42);
            var timeStamp = TimeTicks.Create(123456);
            var varbindList = new Sequence(new IDataType[] { });

            var original = new TrapV1(
                null,
                enterprise,
                agentAddr,
                genericTrap,
                specificTrap,
                timeStamp,
                varbindList
            );

            var encoded = original.ToBytes();
            var decoded = (TrapV1)SMIDataFactory.Create(encoded);

            Assert.NotNull(decoded.Enterprise);
            Assert.Equal(enterprise.Value, decoded.Enterprise?.Value);
            Assert.NotNull(decoded.AgentAddr);
            Assert.Equal("192.168.1.100", decoded.AgentAddr?.Value?.ToString());
            Assert.Equal(6, decoded.GenericTrap?.Value ?? 0);
            Assert.Equal(42, decoded.SpecificTrap?.Value ?? 0);
            Assert.Equal(123456u, decoded.TimeStamp?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void TrapV1_DefaultValues_Work()
        {
            var original = new TrapV1();

            var encoded = original.ToBytes();
            var decoded = (TrapV1)SMIDataFactory.Create(encoded);

            Assert.NotNull(decoded.Enterprise);
            Assert.NotNull(decoded.AgentAddr);
            Assert.Equal("0.0.0.0", decoded.AgentAddr?.Value?.ToString());
            Assert.Equal(6, decoded.GenericTrap?.Value ?? 0); // enterpriseSpecific
            Assert.Equal(0, decoded.SpecificTrap?.Value ?? 0);
            Assert.Equal(0u, decoded.TimeStamp?.Value ?? 0);
            Assert.NotNull(decoded.VarbindList);
        }

        [Fact]
        public void GetBulkRequest_ConvenienceProperties_Work()
        {
            var original = new GetBulkRequest(
                null,
                Integer.Create(1),
                Integer.Create(3), // Non-repeaters
                Integer.Create(15), // Max-repetitions
                new Sequence(new IDataType[] { })
            );

            // Test that convenience properties map correctly
            Assert.Equal(3, original.NonRepeaters?.Value ?? 0);
            Assert.Equal(15, original.MaxRepetitions?.Value ?? 0);

            // Test that they're the same as base Error/ErrorIndex
            Assert.Equal(original.Error?.Value ?? 0, original.NonRepeaters?.Value ?? 0);
            Assert.Equal(original.ErrorIndex?.Value ?? 0, original.MaxRepetitions?.Value ?? 0);
        }

        [Fact]
        public void AllNewPDUs_HaveCorrectTags()
        {
            var getNext = new GetNextRequest();
            var getBulk = new GetBulkRequest();
            var inform = new InformRequest();
            var trapV2 = new TrapV2();
            var report = new Report();
            var trapV1 = new TrapV1();

            var getNextBytes = getNext.ToBytes();
            var getBulkBytes = getBulk.ToBytes();
            var informBytes = inform.ToBytes();
            var trapV2Bytes = trapV2.ToBytes();
            var reportBytes = report.ToBytes();
            var trapV1Bytes = trapV1.ToBytes();

            // Check that each PDU has the correct tag
            Assert.Equal((byte)SnmpDataType.GetNextRequestPDU, getNextBytes[0]);
            Assert.Equal((byte)SnmpDataType.GetBulkRequestPDU, getBulkBytes[0]);
            Assert.Equal((byte)SnmpDataType.InformRequestPDU, informBytes[0]);
            Assert.Equal((byte)SnmpDataType.TrapV2PDU, trapV2Bytes[0]);
            Assert.Equal((byte)SnmpDataType.ReportPDU, reportBytes[0]);
            Assert.Equal((byte)SnmpDataType.TrapPDU, trapV1Bytes[0]);
        }
    }
}