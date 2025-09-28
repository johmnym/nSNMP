using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.SMI.Tests
{
    public class ObjectIdentifierTests
    {
        [Fact]
        public void CanDecodeObjectIdentifier()
        {
            const string actual = ".1.3.6.1.4.1.55";

            var oid = ObjectIdentifier.Create(actual);

            Assert.Equal(actual, oid.ToString());
        }        
        
        [Fact]
        public void CanDecodeObjectIdentifierWithLargeSegment()
        {
            const string actual = ".1.2.840.113549.1.1.5";

            var oid = ObjectIdentifier.Create(actual);

            Assert.Equal(actual, oid.ToString());
        }        
        [Fact]
        public void CanDecodeObjectIdentifierWithMaxValueSegment()
        {
            const string actual = ".1.2.840.113549.1.4294967295";

            var oid = ObjectIdentifier.Create(actual);

            Assert.Equal(actual, oid.ToString());
        }
    }
}