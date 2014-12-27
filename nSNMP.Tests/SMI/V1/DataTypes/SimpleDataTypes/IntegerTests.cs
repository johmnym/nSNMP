using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.Tests.SMI.V1.DataTypes.SimpleDataTypes
{
    public class IntegerTests
    {
        [Fact]
        public void CanParseZero()
        {
            var intData = new[] {(byte)0};

            var actual = new Integer(intData);

            Assert.Equal(0, actual.Value);
        }
        
        [Fact]
        public void CanParseSixteen()
        {
            var intData = new[] {(byte)16};

            var actual = new Integer(intData);

            Assert.Equal(16, actual.Value);
        }

        [Fact]
        public void CanParse1024()
        {
            var intData = new byte[2];
            intData[0] = 4;
            intData[1] = 0;

            var actual = new Integer(intData);

            Assert.Equal(1024, actual.Value);
        }

        [Fact]
        public void CanParseNegativeEight()
        {
            var intData = new byte[1];

            intData[0] = 248; //Two's complement

            var actual = new Integer(intData);

            Assert.Equal(-8, actual.Value);
        }

        [Fact]
        public void CanParseNegativeTwo()
        {
            var intData = new byte[1];

            intData[0] = 254; //Two's complement

            var actual = new Integer(intData);

            Assert.Equal(-2, actual.Value);
        }
    }
}
