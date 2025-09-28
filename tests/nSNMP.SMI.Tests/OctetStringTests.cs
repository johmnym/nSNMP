using System.Text;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.SMI.Tests
{
    public class OctetStringTests
    {
        [Fact]
        public void CanReadPublicString()
        {
            const string str = "public";

            var data = Encoding.ASCII.GetBytes(str);

            var octetString = new OctetString(data);

            Assert.Equal(str, octetString.ToString());
        }

        [Fact]
        public void CantReadNonAsciiCharacters()
        {
            const string str = "åäö";

            var data = Encoding.ASCII.GetBytes(str);

            var octetString = new OctetString(data);

            Assert.NotEqual(str, octetString.ToString());
        }

        [Fact]
        public void CanCreateOctetStringFromString()
        {
            const string expected = "This is a string";

            var actual = OctetString.Create(expected);

            Assert.Equal(expected, actual.ToString());
        }

    }
}
