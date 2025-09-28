using nSNMP.Message;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.PDUs;
using Xunit;

namespace nSNMP.Tests.nSNMP.Message
{
    public class SnmpMessageTests
    {
        [Fact]
        public void CanParseSnmpMessage()
        {
            byte[] data = SnmpMessageFactory.CreateMessage();

            SnmpMessage message = SnmpMessage.Create(data);

            Assert.NotNull(message);
            Assert.Equal(SnmpVersion.V1, message.Version);
            Assert.NotNull(message.CommunityString);
            Assert.NotNull(message.PDU);
        }
        
        [Fact]
        public void CanParseLargeSnmpMessage()
        {
            byte[] data = SnmpMessageFactory.CreateLargeMessage();

            var message = SnmpMessage.Create(data);

            Assert.NotNull(message);
            Assert.NotNull(message.Version);
            Assert.NotNull(message.CommunityString);
            Assert.NotNull(message.PDU);
            Assert.IsType<GetResponse>(message.PDU);
            var pdu = (GetResponse)message.PDU;
            Assert.NotNull(pdu.VarbindList);
        }

        [Fact]
        public void CanParseRequestMessage()
        {
            byte[] data = SnmpMessageFactory.CreateRequestMessage();

            var message = SnmpMessage.Create(data);

            Assert.NotNull(message);
            Assert.NotNull(message.Version);
            Assert.NotNull(message.CommunityString);
            Assert.NotNull(message.PDU);
            Assert.IsType<GetRequest>(message.PDU);
            var pdu = (GetRequest)message.PDU;
            Assert.NotNull(pdu.RequestId);
            Assert.NotNull(pdu.Error);
            Assert.NotNull(pdu.ErrorIndex);
        }
    }
}
