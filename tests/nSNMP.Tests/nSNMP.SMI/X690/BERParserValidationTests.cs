using System;
using nSNMP.SMI.X690;
using Xunit;

namespace nSNMP.Tests.nSNMP.SMI.X690
{
    public class BERParserValidationTests
    {
        [Fact]
        public void ParseType_ThrowsOnEmptyMemory()
        {
            var memory = ReadOnlyMemory<byte>.Empty;

            Assert.Throws<ArgumentException>(() => BERParser.ParseType(ref memory));
        }

        [Fact]
        public void ParseLengthOfNextDataField_ThrowsOnEmptyMemory()
        {
            var memory = ReadOnlyMemory<byte>.Empty;

            Assert.Throws<ArgumentException>(() => BERParser.ParseLengthOfNextDataField(ref memory));
        }

        [Fact]
        public void ParseLengthOfNextDataField_ThrowsOnIncompleteMultiByteLength()
        {
            // 0x82 indicates 2-byte length follows, but we only provide 1 byte
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x82, 0x01 });

            Assert.Throws<ArgumentException>(() => BERParser.ParseLengthOfNextDataField(ref memory));
        }

        [Fact]
        public void ParseDataField_ThrowsOnNegativeLength()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03 });

            Assert.Throws<ArgumentException>(() => BERParser.ParseDataField(ref memory, -1));
        }

        [Fact]
        public void ParseDataField_ReturnsEmptyArrayForZeroLength()
        { 
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03 });

            var result = BERParser.ParseDataField(ref memory, 0);

            Assert.True(result.IsEmpty);
        }

        [Fact]
        public void ParseDataField_ThrowsOnShortRead()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02 });

            // Request 5 bytes but only 2 are available
            Assert.Throws<ArgumentException>(() => BERParser.ParseDataField(ref memory, 5));
        }

        [Fact]
        public void ParseDataField_ReadsExactLength()
        {
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            var result = BERParser.ParseDataField(ref memory, 3);

            Assert.Equal(3, result.Length);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, result.ToArray());
            // Remaining memory should be 2 bytes
            Assert.Equal(2, memory.Length);
        }

        [Fact]
        public void ParseLengthOfNextDataField_HandlesShortForm()
        {
            // 0x05 is a short form length (bit 7 = 0)
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x05 });

            var length = BERParser.ParseLengthOfNextDataField(ref memory);

            Assert.Equal(5, length);
        }

        [Fact]
        public void ParseLengthOfNextDataField_HandlesLongForm()
        {
            // 0x82 means 2 bytes follow for the length
            // 0x01 0x23 = 291 in decimal
            var memory = new ReadOnlyMemory<byte>(new byte[] { 0x82, 0x01, 0x23 });

            var length = BERParser.ParseLengthOfNextDataField(ref memory);

            Assert.Equal(291, length);
        }
    }
}