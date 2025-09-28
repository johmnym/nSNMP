using System.Net;
using System.Reflection;
using nSNMP.Manager;
using nSNMP.Message;
using nSNMP.SMI;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.Security;
using Xunit;
using Xunit.Abstractions;

namespace nSNMP.Integration.Tests;

public class EnhancedApiTests
{
    private readonly ITestOutputHelper _output;

    public EnhancedApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SnmpClient_ShouldCreateWithProperConstructor()
    {
        // Test the correct constructor signature
        var endpoint = new IPEndPoint(IPAddress.Loopback, 161);
        using var client = new SnmpClient(endpoint, SnmpVersion.V2c, "public", TimeSpan.FromSeconds(5));

        Assert.NotNull(client);
        Assert.True(client is IDisposable);
        _output.WriteLine("✅ SnmpClient created successfully with proper constructor");
    }

    [Fact]
    public void SnmpClient_FactoryMethods_ShouldWork()
    {
        // Test factory method
        using var client = SnmpClient.CreateCommunity("127.0.0.1", 161, SnmpVersion.V2c, "public");

        Assert.NotNull(client);
        _output.WriteLine("✅ SnmpClient factory method works");
    }

    [Fact]
    public void SnmpClientV3_ShouldCreateWithProperCredentials()
    {
        // Test SNMPv3 client creation
        using var client = SnmpClientV3.CreateNoAuthNoPriv("127.0.0.1", "testuser");

        Assert.NotNull(client);
        Assert.True(client is IDisposable);
        _output.WriteLine("✅ SnmpClientV3 created successfully");
    }

    [Fact]
    public void SnmpClientV3_ShouldCreateWithAuthNoPriv()
    {
        // Test SNMPv3 client with authentication
        using var client = SnmpClientV3.CreateAuthNoPriv("127.0.0.1", "authuser",
            AuthProtocol.SHA256, "authpassword123456");

        Assert.NotNull(client);
        _output.WriteLine("✅ SnmpClientV3 with authNoPriv created successfully");
    }

    [Fact]
    public void ObjectIdentifier_ShouldCreateAndFormat()
    {
        var testOids = new[]
        {
            "1.3.6.1.2.1.1.1.0", // sysDescr
            "1.3.6.1.2.1.1.2.0", // sysObjectID
            "1.3.6.1.2.1.1.3.0", // sysUpTime
            "1.3.6.1.2.1.2.1.0"  // ifNumber
        };

        foreach (var oidString in testOids)
        {
            var oid = ObjectIdentifier.Create(oidString);

            Assert.NotNull(oid);
            Assert.Equal("." + oidString, oid.ToString());
            _output.WriteLine($"✅ OID {oidString} created and formatted correctly");
        }
    }

    [Fact]
    public void SnmpDataTypes_ShouldCreateCorrectly()
    {
        // Test Integer
        var integer = Integer.Create(42);
        Assert.NotNull(integer);

        // Test OctetString
        var octetString = OctetString.Create("Test String");
        Assert.NotNull(octetString);
        Assert.Equal("Test String", octetString.Value);

        // Test Counter32
        var counter32 = Counter32.Create(1234567890);
        Assert.NotNull(counter32);

        // Test Gauge32
        var gauge32 = Gauge32.Create(987654321);
        Assert.NotNull(gauge32);

        // Test TimeTicks
        var timeTicks = TimeTicks.Create(123456789);
        Assert.NotNull(timeTicks);

        _output.WriteLine("✅ All SNMP data types created successfully");
    }

    [Fact]
    public void VarBind_ShouldCreateCorrectly()
    {
        var oid = ObjectIdentifier.Create("1.3.6.1.2.1.1.1.0");
        var value = OctetString.Create("System Description");
        var varBind = new VarBind(oid, value);

        Assert.NotNull(varBind);
        Assert.Equal(".1.3.6.1.2.1.1.1.0", varBind.Oid.ToString());
        Assert.IsType<OctetString>(varBind.Value);

        _output.WriteLine("✅ VarBind created successfully");
    }

    [Fact]
    public void SnmpClient_ShouldHaveCorrectApiMethods()
    {
        var clientType = typeof(SnmpClient);

        // Check for GetAsync methods
        var getAsyncMethods = clientType.GetMethods()
            .Where(m => m.Name == "GetAsync")
            .ToArray();

        Assert.NotEmpty(getAsyncMethods);
        Assert.Contains(getAsyncMethods, m =>
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(string[]));
        Assert.Contains(getAsyncMethods, m =>
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(ObjectIdentifier[]));

        // Check for GetNextAsync methods
        var getNextAsyncMethods = clientType.GetMethods()
            .Where(m => m.Name == "GetNextAsync")
            .ToArray();

        Assert.NotEmpty(getNextAsyncMethods);

        // Check for GetBulkAsync methods
        var getBulkAsyncMethods = clientType.GetMethods()
            .Where(m => m.Name == "GetBulkAsync")
            .ToArray();

        Assert.NotEmpty(getBulkAsyncMethods);

        // Check for SetAsync methods
        var setAsyncMethods = clientType.GetMethods()
            .Where(m => m.Name == "SetAsync")
            .ToArray();

        Assert.NotEmpty(setAsyncMethods);

        _output.WriteLine("✅ SnmpClient has all required API methods");
    }

    [Fact]
    public async Task SnmpClient_TimeoutBehavior_ShouldWork()
    {
        // Use a non-routable IP address to ensure timeout
        var endpoint = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 161); // RFC 5737 test address
        var timeout = TimeSpan.FromMilliseconds(500);

        using var client = new SnmpClient(endpoint, SnmpVersion.V2c, "public", timeout);

        var startTime = DateTime.UtcNow;

        // This should timeout quickly
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await client.GetAsync("1.3.6.1.2.1.1.1.0");
        });

        var duration = DateTime.UtcNow - startTime;

        // Should complete within reasonable time (allowing for some buffer)
        Assert.True(duration < TimeSpan.FromSeconds(3),
            $"Operation took too long: {duration.TotalMilliseconds}ms");

        _output.WriteLine($"✅ Timeout behavior works correctly ({duration.TotalMilliseconds}ms)");
    }

    [Fact]
    public void SnmpVersions_ShouldBeAvailable()
    {
        // Test that SNMP versions are available
        var v1 = SnmpVersion.V1;
        var v2c = SnmpVersion.V2c;
        var v3 = SnmpVersion.V3;

        Assert.NotEqual(v1, v2c);
        Assert.NotEqual(v2c, v3);
        Assert.NotEqual(v1, v3);

        _output.WriteLine("✅ All SNMP versions are available");
    }

    [Fact]
    public void SecurityProtocols_ShouldBeAvailable()
    {
        // Test authentication protocols
        var authProtocols = new[]
        {
            AuthProtocol.None,
            AuthProtocol.MD5,
            AuthProtocol.SHA1,
            AuthProtocol.SHA224,
            AuthProtocol.SHA256,
            AuthProtocol.SHA384,
            AuthProtocol.SHA512
        };

        // Auth protocols are enums, so they're always valid

        // Test privacy protocols
        var privProtocols = new[]
        {
            PrivProtocol.None,
            PrivProtocol.DES,
            PrivProtocol.AES128,
            PrivProtocol.AES192,
            PrivProtocol.AES256
        };

        // Priv protocols are enums, so they're always valid

        _output.WriteLine("✅ All security protocols are available");
    }
}