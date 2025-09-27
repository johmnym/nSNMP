using System.Net;
using nSNMP.Manager;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.Message;
using Xunit;

namespace nSNMP.InteropTests
{
    /// <summary>
    /// Basic interoperability tests that validate SNMP protocol compliance
    /// These tests ensure the library can create proper SNMP structures for interop
    /// </summary>
    public class BasicInteropTests
    {
        [Fact]
        [Trait("Category", "Interop")]
        public void SnmpClient_Creation_ShouldWork()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 161);
            using var client = new SnmpClient(endpoint);

            Assert.NotNull(client);
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void ObjectIdentifier_StandardFormats_ShouldBeValid()
        {
            // Test standard MIB-2 system OIDs
            var sysDescr = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
            var sysObjectId = ObjectIdentifier.Create("1.3.6.1.2.1.1.2.0");
            var sysUpTime = ObjectIdentifier.Create("1.3.6.1.2.1.1.3.0");

            Assert.Equal(".1.3.6.1.2.1.1.1.0", sysDescr.ToString());
            Assert.Equal(".1.3.6.1.2.1.1.2.0", sysObjectId.ToString());
            Assert.Equal(".1.3.6.1.2.1.1.3.0", sysUpTime.ToString());
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void SnmpDataTypes_Creation_ShouldWork()
        {
            // Test creation of common SNMP data types
            var integerValue = Integer.Create(42);
            var stringValue = new OctetString(System.Text.Encoding.ASCII.GetBytes("test"));
            var counter = Counter32.Create(1234567);
            var gauge = Gauge32.Create(98765);
            var ticks = TimeTicks.Create(123456789);

            Assert.Equal(42, integerValue.Value);
            Assert.Equal("test", stringValue.ToString());
            Assert.Equal(1234567U, counter.Value);
            Assert.Equal(98765U, gauge.Value);
            Assert.Equal(123456789U, ticks.Value);
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void SnmpVersions_ShouldBeSupported()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 161);

            // Test creating clients for different SNMP versions
            using var v1Client = new SnmpClient(endpoint, SnmpVersion.V1);
            using var v2cClient = new SnmpClient(endpoint, SnmpVersion.V2c);

            Assert.NotNull(v1Client);
            Assert.NotNull(v2cClient);
        }

        [Fact]
        [Trait("Category", "Interop")]
        public async Task SnmpRequest_Timeout_ShouldWork()
        {
            // Use an unreachable address to test timeout behavior
            var endpoint = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 161);
            using var client = new SnmpClient(endpoint, timeout: TimeSpan.FromSeconds(1));

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await client.GetAsync("1.3.6.1.2.1.1.1.0");
            });
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void OidValidation_ShouldFollowStandards()
        {
            // Valid OIDs
            Assert.NotNull(ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0"));
            Assert.NotNull(ObjectIdentifier.Create("1.3.6.1.4.1.9.2.1"));
            Assert.NotNull(ObjectIdentifier.Create("0.0"));

            // Invalid OIDs should throw
            Assert.Throws<ArgumentException>(() => ObjectIdentifier.Create(""));
            Assert.Throws<ArgumentException>(() => ObjectIdentifier.Create("1"));
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void CommunityStrings_ShouldBeHandled()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 161);

            // Test various community strings
            using var client1 = new SnmpClient(endpoint, community: "public");
            using var client2 = new SnmpClient(endpoint, community: "private");
            using var client3 = new SnmpClient(endpoint, community: "community123");

            Assert.NotNull(client1);
            Assert.NotNull(client2);
            Assert.NotNull(client3);
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void StandardMibOids_ShouldBeValid()
        {
            // System group OIDs (RFC 1213)
            var systemOids = new[]
            {
                "1.3.6.1.2.1.1.1.0", // sysDescr
                "1.3.6.1.2.1.1.2.0", // sysObjectID
                "1.3.6.1.2.1.1.3.0", // sysUpTime
                "1.3.6.1.2.1.1.4.0", // sysContact
                "1.3.6.1.2.1.1.5.0", // sysName
                "1.3.6.1.2.1.1.6.0", // sysLocation
                "1.3.6.1.2.1.1.7.0"  // sysServices
            };

            foreach (var oidStr in systemOids)
            {
                var oid = ObjectIdentifier.Create(oidStr);
                Assert.Equal("." + oidStr, oid.ToString());
            }
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void ErrorHandling_ShouldBeGraceful()
        {
            // Test null parameter handling
            Assert.Throws<ArgumentNullException>(() => new SnmpClient(null!));

            // Test invalid parameters
            var endpoint = new IPEndPoint(IPAddress.Loopback, 161);
            Assert.Throws<ArgumentNullException>(() => new SnmpClient(endpoint, community: null!));
        }

        [Fact]
        [Trait("Category", "Interop")]
        public void DataTypeEncoding_ShouldProduceValidAsn1()
        {
            // Test that data types produce valid ASN.1 BER encoding
            var integer = Integer.Create(12345);
            var octetString = new OctetString(System.Text.Encoding.ASCII.GetBytes("Hello"));
            var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");

            var intBytes = integer.ToBytes();
            var strBytes = octetString.ToBytes();
            var oidBytes = oid.ToBytes();

            // All should produce non-empty byte arrays
            Assert.NotEmpty(intBytes);
            Assert.NotEmpty(strBytes);
            Assert.NotEmpty(oidBytes);

            // Basic ASN.1 structure check (tag + length + value)
            Assert.True(intBytes.Length >= 3); // At minimum: tag + length + 1 byte value
            Assert.True(strBytes.Length >= 3);
            Assert.True(oidBytes.Length >= 3);
        }

        [Theory]
        [InlineData("127.0.0.1", 161)]
        [InlineData("::1", 161)]
        [InlineData("192.168.1.1", 161)]
        [Trait("Category", "Interop")]
        public void ClientEndpoints_ShouldBeConfigurable(string address, int port)
        {
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(address), port);
                using var client = new SnmpClient(endpoint);
                Assert.NotNull(client);
            }
            catch (FormatException)
            {
                // IPv6 might not be supported on all systems
                if (!address.Contains(':'))
                    throw;
            }
        }
    }
}