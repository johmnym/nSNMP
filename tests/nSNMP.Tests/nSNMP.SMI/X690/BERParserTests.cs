using System;
using nSNMP.SMI;
using nSNMP.SMI.X690;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.X690
{
    public class BERParserTests
    {
        [Fact]
        public void CanParseBerTypeInteger()
        {
            ReadOnlyMemory<byte> memory = new byte[] { 2, 0 };

            SnmpDataType actual = BERParser.ParseType(ref memory);

            Assert.Equal(SnmpDataType.Integer, actual);
            Assert.Equal(1, memory.Length);
        }

        [Fact]
        public void CanParseBerTypeOctetString()
        {
            ReadOnlyMemory<byte> memory = new byte[] { 4, 0 };

            SnmpDataType actual = BERParser.ParseType(ref memory);

            Assert.Equal(SnmpDataType.OctetString, actual);
            Assert.Equal(1, memory.Length);
        }

        [Fact]
        public void CanParseBerTypeSequence()
        {
            ReadOnlyMemory<byte> memory = new byte[] { 48, 0 };

            SnmpDataType actual = BERParser.ParseType(ref memory);

            Assert.Equal(SnmpDataType.Sequence, actual);
            Assert.Equal(1, memory.Length);
        }

        [Fact]
        public void CanParseShortFormLength()
        {
            byte[] message = SnmpMessageFactory.CreateMessage();
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(message);

            BERParser.ParseType(ref memory); // Consume the type byte

            int length = BERParser.ParseLengthOfNextDataField(ref memory);

            Assert.Equal(70, length);
        }

        [Fact]
        public void CanParseLongFormLength()
        {
            byte[] largeMessage = SnmpMessageFactory.CreateLargeMessage();
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(largeMessage);

            BERParser.ParseType(ref memory); // Consume the type byte

            int length = BERParser.ParseLengthOfNextDataField(ref memory);

            Assert.Equal(134, length);
        }
    }
}