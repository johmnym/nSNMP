using nSNMP.Message;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using Xunit;

namespace nSNMP.Tests.nSNMP.Message
{
    public class SnmpMessageNullHandlingTests
    {
        [Fact]
        public void NewMessage_HasNullProperties()
        {
            var message = new SnmpMessage();

            Assert.Null(message.Version);
            Assert.Null(message.CommunityString);
            Assert.Null(message.PDU);
        }

        [Fact]
        public void CanCreateMessageWithNullVersion()
        {
            var message = new SnmpMessage(null, OctetString.Create("public"), new GetRequest());

            Assert.Null(message.Version);
            Assert.NotNull(message.CommunityString);
            Assert.NotNull(message.PDU);
        }

        [Fact]
        public void CanCreateMessageWithNullCommunityString()
        {
            var message = new SnmpMessage(SnmpVersion.V1, null, new GetRequest());

            Assert.NotNull(message.Version);
            Assert.Null(message.CommunityString);
            Assert.NotNull(message.PDU);
        }

        [Fact]
        public void CanCreateMessageWithNullPDU()
        {
            var message = new SnmpMessage(SnmpVersion.V1, OctetString.Create("public"), null);

            Assert.NotNull(message.Version);
            Assert.NotNull(message.CommunityString);
            Assert.Null(message.PDU);
        }

        [Fact]
        public void CanCreateMessageWithVersion()
        {
            var message = new SnmpMessage(SnmpVersion.V1, OctetString.Create("public"), new GetRequest());

            Assert.NotNull(message.Version);
            Assert.Equal(SnmpVersion.V1, message.Version);
        }

        [Fact]
        public void CanCreateMessageWithCommunityString()
        {
            var community = OctetString.Create("private");
            var message = new SnmpMessage(SnmpVersion.V1, community, new GetRequest());

            Assert.NotNull(message.CommunityString);
            Assert.Equal(community, message.CommunityString);
        }

        [Fact]
        public void CanCreateMessageWithPDU()
        {
            var pdu = new GetResponse(null, null, null, null, new Sequence(new IDataType[] { }));
            var message = new SnmpMessage(SnmpVersion.V1, OctetString.Create("public"), pdu);

            Assert.NotNull(message.PDU);
            Assert.Equal(pdu, message.PDU);
        }

        [Fact]
        public void RecordWithExpression_CreatesNewInstance()
        {
            var message1 = new SnmpMessage(SnmpVersion.V1, OctetString.Create("public"), new GetRequest());

            // Records support with expressions to create modified copies
            var message2 = message1 with { Version = null };

            Assert.Equal(SnmpVersion.V1, message1.Version);
            Assert.Null(message2.Version);
            Assert.Equal(message1.CommunityString, message2.CommunityString);
            Assert.Equal(message1.PDU, message2.PDU);
        }
    }
}