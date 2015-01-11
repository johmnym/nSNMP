using nSNMP.SMI.Message;
using Xunit;

namespace nSNMP.Tests.SMI.Message
{
    public class SnmpMessageTests
    {
        [Fact]
        public void CanParseSnmpMessage()
        {
            byte[] data = SnmpMessageFactory.CreateMessage();

            SnmpMessage message = SnmpMessage.Create(data);


        }
        
        [Fact]
        public void CanParseLargeSnmpMessage()
        {
            byte[] data = SnmpMessageFactory.CreateLargeMessage();

            var message = SnmpMessage.Create(data);
        }

        [Fact]
        public void CanParseRequestMessage()
        {
            byte[] data = SnmpMessageFactory.CreateRequestMessage();

            var message = SnmpMessage.Create(data);
        }
    }
}
