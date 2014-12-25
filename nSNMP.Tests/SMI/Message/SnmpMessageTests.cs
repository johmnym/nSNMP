using nSNMP.SMI;
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

            SnmpMessage message = SMIDataFactory.CreateSnmpMessage(data);
        }
    }
}
