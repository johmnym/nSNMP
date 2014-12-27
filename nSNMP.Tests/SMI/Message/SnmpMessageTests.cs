using nSNMP.SMI;
using nSNMP.SMI.DataTypes.V1.Constructed;
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

            var message = (Sequence)SMIDataFactory.Create(data);
        }
    }
}
