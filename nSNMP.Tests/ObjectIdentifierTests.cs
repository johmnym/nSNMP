using nSNMP.SMI.V1.DataTypes.SimpleDataTypes;
using Xunit;

namespace nSNMP.Tests
{
    public class ObjectIdentifierTests
    {
        [Fact]
        public void CanDecodeObjectIdentifierWithLarge()
        {
            const string data = "1.3.6.1.4.1.5518";

            var oid = ObjectIdentifier.Create(data);

            Assert.Equal("1.2.840.113549", oid.ToString());
        }
    }
}
