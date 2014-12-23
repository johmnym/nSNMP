using System.IO;
using nSNMP.SMI;
using nSNMP.SMI.X690;
using Xunit;

namespace nSNMP.Tests.SMI.X690
{
    public class BERParserTests
    {
        [Fact]
        public void CanParseBerTypeInteger()
        {
            byte data = 2;

            SnmpDataType actual = BERParser.ParseType(data);

            Assert.Equal(SnmpDataType.Integer, actual);
        }

        [Fact]
        public void CanParseBerTypeOctetString()
        {
            byte data = 4;

            SnmpDataType actual = BERParser.ParseType(data);

            Assert.Equal(SnmpDataType.OctetString, actual);
        }

        [Fact]
        public void CanParseBerTypeSequence()
        {
            byte data = 48;

            SnmpDataType actual = BERParser.ParseType(data);

            Assert.Equal(SnmpDataType.Sequence, actual);
        }

        [Fact]
        public void CanParseBerClassUniversal()
        {
            byte data = 48;

            BERClass actual = BERParser.ParseClass(data);

            Assert.Equal(BERClass.Universal, actual);
        }

        [Fact]
        public void CanParseBerClassApplication()
        {
            byte data = 112;

            BERClass actual = BERParser.ParseClass(data);

            Assert.Equal(BERClass.Application, actual);
        }

        [Fact]
        public void CanParseBerClassContextSpecific()
        {
            byte data = 128;

            BERClass actual = BERParser.ParseClass(data);

            Assert.Equal(BERClass.ContextSpecific, actual);
        }

        [Fact]
        public void CanParseBerClassPrivate()
        {
            byte data = 192;

            BERClass actual = BERParser.ParseClass(data);

            Assert.Equal(BERClass.Private, actual);
        }

        [Fact]
        public void CanParseShortFormLength()
        {
            byte[] message = SnmpMessageFactory.CreateMessage();

            var stream = new MemoryStream(message);

            stream.ReadByte();

            int length = BERParser.ParseLengthOfNextDataField(stream);

            Assert.Equal(70, length);
        }

        [Fact]
        public void CanParseLongFormLength()
        {
            byte[] largeMessage = SnmpMessageFactory.CreateLargeMessage();

            var stream = new MemoryStream(largeMessage);

            stream.ReadByte();

            int length = BERParser.ParseLengthOfNextDataField(stream);

            Assert.Equal(134, length);
        }
    }
}
