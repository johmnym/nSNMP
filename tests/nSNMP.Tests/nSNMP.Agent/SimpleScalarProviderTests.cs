using nSNMP.Agent;
using nSNMP.SMI.DataTypes.V1.Primitive;
using Xunit;

namespace nSNMP.Tests.nSNMP.Agent
{
    public class SimpleScalarProviderTests
    {
        [Fact]
        public void Constructor_WithValidOid_CreatesProvider()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var value = OctetString.Create("Test Value");

            var provider = new SimpleScalarProvider(oid, value);

            Assert.NotNull(provider);
        }

        [Fact]
        public void Constructor_WithNullOid_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new SimpleScalarProvider(null!, null));
        }

        [Fact]
        public void GetValue_WithMatchingOid_ReturnsValue()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var value = OctetString.Create("System Description");
            var provider = new SimpleScalarProvider(oid, value);

            var result = provider.GetValue(oid);

            Assert.Equal(value, result);
        }

        [Fact]
        public void GetValue_WithNonMatchingOid_ReturnsNull()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var differentOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var value = OctetString.Create("System Description");
            var provider = new SimpleScalarProvider(oid, value);

            var result = provider.GetValue(differentOid);

            Assert.Null(result);
        }

        [Fact]
        public void SetValue_WithReadOnlyProvider_ReturnsFalse()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var value = OctetString.Create("Initial Value");
            var provider = new SimpleScalarProvider(oid, value, readOnly: true);

            var newValue = OctetString.Create("New Value");
            var result = provider.SetValue(oid, newValue);

            Assert.False(result);
            Assert.Equal(value, provider.GetValue(oid)); // Value should not change
        }

        [Fact]
        public void SetValue_WithWritableProvider_ReturnsTrue()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.6.0");
            var initialValue = OctetString.Create("Initial Location");
            var provider = new SimpleScalarProvider(oid, initialValue, readOnly: false);

            var newValue = OctetString.Create("New Location");
            var result = provider.SetValue(oid, newValue);

            Assert.True(result);
            Assert.Equal(newValue, provider.GetValue(oid));
        }

        [Fact]
        public void SetValue_WithNonMatchingOid_ReturnsFalse()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.6.0");
            var differentOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.7.0");
            var provider = new SimpleScalarProvider(oid, null, readOnly: false);

            var result = provider.SetValue(differentOid, OctetString.Create("Value"));

            Assert.False(result);
        }

        [Fact]
        public void CanHandle_WithMatchingOid_ReturnsTrue()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var provider = new SimpleScalarProvider(oid, null);

            var result = provider.CanHandle(oid);

            Assert.True(result);
        }

        [Fact]
        public void CanHandle_WithNonMatchingOid_ReturnsFalse()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var differentOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var provider = new SimpleScalarProvider(oid, null);

            var result = provider.CanHandle(differentOid);

            Assert.False(result);
        }

        [Fact]
        public void GetNextOid_WithExactMatch_ReturnsNull()
        {
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var provider = new SimpleScalarProvider(oid, null);

            var result = provider.GetNextOid(oid);

            Assert.Null(result);
        }

        [Fact]
        public void GetNextOid_WithSmallerOid_ReturnsProviderOid()
        {
            var providerOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var smallerOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var provider = new SimpleScalarProvider(providerOid, null);

            var result = provider.GetNextOid(smallerOid);

            Assert.Equal(providerOid, result);
        }

        [Fact]
        public void GetNextOid_WithLargerOid_ReturnsNull()
        {
            var providerOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var largerOid = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var provider = new SimpleScalarProvider(providerOid, null);

            var result = provider.GetNextOid(largerOid);

            Assert.Null(result);
        }
    }
}