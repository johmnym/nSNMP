using nSNMP.SMI;
using Xunit;

namespace nSNMP.Tests
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
    }
}
