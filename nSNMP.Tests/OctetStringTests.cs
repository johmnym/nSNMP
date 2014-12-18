using System.Text;
using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;
using Xunit;

namespace nSNMP.Tests
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

    }
}
