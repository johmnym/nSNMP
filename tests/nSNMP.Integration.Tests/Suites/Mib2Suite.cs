using Microsoft.Extensions.Logging;
using nSNMP.Manager;
using nSNMP.Message;
using nSNMP.SMI;
using nSNMP.Integration.Tests.Infrastructure;
using nSNMP.Integration.Tests.Reporting;
using System.Net;

namespace nSNMP.Integration.Tests.Suites;

public class Mib2Suite : IAsyncDisposable
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ILogger<Mib2Suite> _logger;
    private readonly TestReporter _reporter;

    public Mib2Suite(IntegrationTestFixture fixture, ILogger<Mib2Suite>? logger = null)
    {
        _fixture = fixture;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Mib2Suite>.Instance;
        _reporter = new TestReporter("Mib2Suite", _logger);
    }

    public async Task<TestSuiteResult> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MIB-II test suite");
        await _fixture.InitializeAsync(cancellationToken);

        var suiteResult = new TestSuiteResult("Mib2Suite", "Standard MIB-II object testing with system and interface groups");

        try
        {
            // Test system group
            var systemResult = await TestSystemGroupAsync(cancellationToken);
            suiteResult.AddTestResult(systemResult);

            // Test interfaces table
            var interfacesResult = await TestInterfacesTableAsync(cancellationToken);
            suiteResult.AddTestResult(interfacesResult);

            // Test IP address table
            var ipAddressResult = await TestIpAddressTableAsync(cancellationToken);
            suiteResult.AddTestResult(ipAddressResult);

            // Test SNMP walk operation
            var walkResult = await TestSnmpWalkAsync(cancellationToken);
            suiteResult.AddTestResult(walkResult);

            // Test GETBULK operation
            var getBulkResult = await TestGetBulkAsync(cancellationToken);
            suiteResult.AddTestResult(getBulkResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MIB-II suite failed with exception");
            suiteResult.AddError($"Suite exception: {ex.Message}");
        }

        suiteResult.Complete();
        _logger.LogInformation("MIB-II test suite completed. Success: {Success}, Tests: {TestCount}, Duration: {Duration}ms",
            suiteResult.Success, suiteResult.TestResults.Count, suiteResult.DurationMs);

        return suiteResult;
    }

    private async Task<TestResult> TestSystemGroupAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("SystemGroup", "Test MIB-II system group objects (1.3.6.1.2.1.1)");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("mib2-basic");
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var ipEndpoint = new IPEndPoint(ip, port);
            using var client = new SnmpClient(ipEndpoint, SnmpVersion.V2c, "public");

            // Test sysDescr (1.3.6.1.2.1.1.1.0)
            var sysDescrResult = await client.GetAsync("1.3.6.1.2.1.1.1.0");
            var sysDescr = sysDescrResult.FirstOrDefault();
            test.AddAssertion("sysDescr retrieved",
                !string.IsNullOrEmpty(sysDescr?.Value?.ToString()));
            test.AddAssertion("sysDescr contains expected text",
                sysDescr?.Value?.ToString()?.Contains("nSNMP Integration Testbed") == true);

            // Test sysObjectID (1.3.6.1.2.1.1.2.0)
            var sysObjectIDResult = await client.GetAsync("1.3.6.1.2.1.1.2.0");
            var sysObjectID = sysObjectIDResult.FirstOrDefault();
            test.AddAssertion("sysObjectID retrieved",
                !string.IsNullOrEmpty(sysObjectID?.Value?.ToString()));
            test.AddAssertion("sysObjectID is correct",
                sysObjectID?.Value?.ToString() == "1.3.6.1.4.1.12345.1.1");

            // Test sysUpTime (1.3.6.1.2.1.1.3.0)
            var sysUpTimeResult = await client.GetAsync("1.3.6.1.2.1.1.3.0");
            var sysUpTime = sysUpTimeResult.FirstOrDefault();
            test.AddAssertion("sysUpTime retrieved",
                !string.IsNullOrEmpty(sysUpTime?.Value?.ToString()));

            if (uint.TryParse(sysUpTime?.Value?.ToString(), out var upTime))
            {
                test.AddAssertion("sysUpTime is reasonable", upTime > 0);
                test.AddMetric("SysUpTimeHundredths", upTime);
                test.AddMetric("SysUpTimeSeconds", upTime / 100.0);
            }

            // Test sysContact (1.3.6.1.2.1.1.4.0)
            var sysContactResult = await client.GetAsync("1.3.6.1.2.1.1.4.0");
            var sysContact = sysContactResult.FirstOrDefault();
            test.AddAssertion("sysContact retrieved",
                !string.IsNullOrEmpty(sysContact?.Value?.ToString()));
            test.AddAssertion("sysContact contains expected email",
                sysContact?.Value?.ToString()?.Contains("admin@testbed.local") == true);

            // Test sysName (1.3.6.1.2.1.1.5.0)
            var sysNameResult = await client.GetAsync("1.3.6.1.2.1.1.5.0");
            var sysName = sysNameResult.FirstOrDefault();
            test.AddAssertion("sysName retrieved",
                !string.IsNullOrEmpty(sysName?.Value?.ToString()));
            test.AddAssertion("sysName is correct",
                sysName?.Value?.ToString() == "testbed-device");

            // Test sysLocation (1.3.6.1.2.1.1.6.0)
            var sysLocationResult = await client.GetAsync("1.3.6.1.2.1.1.6.0");
            var sysLocation = sysLocationResult.FirstOrDefault();
            test.AddAssertion("sysLocation retrieved",
                !string.IsNullOrEmpty(sysLocation?.Value?.ToString()));
            test.AddAssertion("sysLocation is correct",
                sysLocation?.Value?.ToString() == "Test Lab");

            // Test sysServices (1.3.6.1.2.1.1.7.0)
            var sysServicesResult = await client.GetAsync("1.3.6.1.2.1.1.7.0");
            var sysServices = sysServicesResult.FirstOrDefault();
            test.AddAssertion("sysServices retrieved",
                !string.IsNullOrEmpty(sysServices?.Value?.ToString()));
            test.AddAssertion("sysServices value is correct",
                sysServices?.Value?.ToString() == "76");

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System group test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestInterfacesTableAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("InterfacesTable", "Test MIB-II interfaces table (1.3.6.1.2.1.2.2.1)");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("mib2-basic");
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var ipEndpoint = new IPEndPoint(ip, port);
            using var client = new SnmpClient(ipEndpoint, SnmpVersion.V2c, "public");

            // Test interface count (1.3.6.1.2.1.2.1.0)
            var ifNumberResult = await client.GetAsync("1.3.6.1.2.1.2.1.0");
            var ifNumber = ifNumberResult.FirstOrDefault();
            test.AddAssertion("ifNumber retrieved",
                !string.IsNullOrEmpty(ifNumber?.Value?.ToString()));
            test.AddAssertion("ifNumber is 3",
                ifNumber?.Value?.ToString() == "3");

            // Test each interface
            var expectedInterfaces = new[]
            {
                (Index: 1, Name: "lo", Type: 24, Mtu: 65536, Speed: 10000000UL, AdminStatus: 1, OperStatus: 1),
                (Index: 2, Name: "eth0", Type: 6, Mtu: 1500, Speed: 1000000000UL, AdminStatus: 1, OperStatus: 1),
                (Index: 3, Name: "eth1", Type: 6, Mtu: 1500, Speed: 1000000000UL, AdminStatus: 2, OperStatus: 2)
            };

            foreach (var iface in expectedInterfaces)
            {
                // Test interface index
                var ifIndexResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.1.{iface.Index}");
                var ifIndex = ifIndexResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} index correct",
                    ifIndex?.Value?.ToString() == iface.Index.ToString());

                // Test interface description/name
                var ifDescrResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.2.{iface.Index}");
                var ifDescr = ifDescrResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} name is {iface.Name}",
                    ifDescr?.Value?.ToString() == iface.Name);

                // Test interface type
                var ifTypeResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.3.{iface.Index}");
                var ifType = ifTypeResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} type is {iface.Type}",
                    ifType?.Value?.ToString() == iface.Type.ToString());

                // Test interface MTU
                var ifMtuResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.4.{iface.Index}");
                var ifMtu = ifMtuResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} MTU is {iface.Mtu}",
                    ifMtu?.Value?.ToString() == iface.Mtu.ToString());

                // Test interface speed
                var ifSpeedResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.5.{iface.Index}");
                var ifSpeed = ifSpeedResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} speed is {iface.Speed}",
                    ifSpeed?.Value?.ToString() == iface.Speed.ToString());

                // Test admin status
                var ifAdminStatusResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.7.{iface.Index}");
                var ifAdminStatus = ifAdminStatusResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} admin status is {iface.AdminStatus}",
                    ifAdminStatus?.Value?.ToString() == iface.AdminStatus.ToString());

                // Test operational status
                var ifOperStatusResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.8.{iface.Index}");
                var ifOperStatus = ifOperStatusResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} oper status is {iface.OperStatus}",
                    ifOperStatus?.Value?.ToString() == iface.OperStatus.ToString());

                // Test counters (just verify they exist and are reasonable)
                var ifInOctetsResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.10.{iface.Index}");
                var ifInOctets = ifInOctetsResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} InOctets retrieved",
                    !string.IsNullOrEmpty(ifInOctets?.Value?.ToString()));

                var ifInUcastPktsResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.11.{iface.Index}");
                var ifInUcastPkts = ifInUcastPktsResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} InUcastPkts retrieved",
                    !string.IsNullOrEmpty(ifInUcastPkts?.Value?.ToString()));

                var ifOutOctetsResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.16.{iface.Index}");
                var ifOutOctets = ifOutOctetsResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} OutOctets retrieved",
                    !string.IsNullOrEmpty(ifOutOctets?.Value?.ToString()));

                var ifOutUcastPktsResult = await client.GetAsync($"1.3.6.1.2.1.2.2.1.17.{iface.Index}");
                var ifOutUcastPkts = ifOutUcastPktsResult.FirstOrDefault();
                test.AddAssertion($"Interface {iface.Index} OutUcastPkts retrieved",
                    !string.IsNullOrEmpty(ifOutUcastPkts?.Value?.ToString()));

                // Add metrics for monitoring
                test.AddMetric($"Interface{iface.Index}Speed", iface.Speed);
                test.AddMetric($"Interface{iface.Index}Mtu", iface.Mtu);
            }

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interfaces table test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestIpAddressTableAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("IpAddressTable", "Test MIB-II IP address table (1.3.6.1.2.1.4.20.1)");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("mib2-basic");
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var ipEndpoint = new IPEndPoint(ip, port);
            using var client = new SnmpClient(ipEndpoint, SnmpVersion.V2c, "public");

            // Test expected IP addresses
            var expectedAddresses = new[]
            {
                (Address: "127.0.0.1", IfIndex: 1, NetMask: "255.0.0.0"),
                (Address: "192.168.1.100", IfIndex: 2, NetMask: "255.255.255.0")
            };

            foreach (var addr in expectedAddresses)
            {
                // Test IP address
                var ipAdEntAddrResult = await client.GetAsync($"1.3.6.1.2.1.4.20.1.1.{addr.Address}");
                var ipAdEntAddr = ipAdEntAddrResult.FirstOrDefault();
                test.AddAssertion($"IP address {addr.Address} retrieved",
                    ipAdEntAddr?.Value?.ToString() == addr.Address);

                // Test interface index
                var ipAdEntIfIndexResult = await client.GetAsync($"1.3.6.1.2.1.4.20.1.2.{addr.Address}");
                var ipAdEntIfIndex = ipAdEntIfIndexResult.FirstOrDefault();
                test.AddAssertion($"IP address {addr.Address} interface index is {addr.IfIndex}",
                    ipAdEntIfIndex?.Value?.ToString() == addr.IfIndex.ToString());

                // Test netmask
                var ipAdEntNetMaskResult = await client.GetAsync($"1.3.6.1.2.1.4.20.1.3.{addr.Address}");
                var ipAdEntNetMask = ipAdEntNetMaskResult.FirstOrDefault();
                test.AddAssertion($"IP address {addr.Address} netmask is {addr.NetMask}",
                    ipAdEntNetMask?.Value?.ToString() == addr.NetMask);
            }

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP address table test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestSnmpWalkAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("SnmpWalk", "Test SNMP walk operation on system group");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("mib2-basic");
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var ipEndpoint = new IPEndPoint(ip, port);
            using var client = new SnmpClient(ipEndpoint, SnmpVersion.V2c, "public");

            // Walk the system group (1.3.6.1.2.1.1)
            var walkResults = new List<VarBind>();
            await foreach (var varbind in client.WalkAsync("1.3.6.1.2.1.1"))
            {
                walkResults.Add(varbind);
            }

            test.AddAssertion("Walk results retrieved",
                walkResults.Count > 0);

            test.AddAssertion("Walk contains sysDescr",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.1.0")));

            test.AddAssertion("Walk contains sysObjectID",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.2.0")));

            test.AddAssertion("Walk contains sysUpTime",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.3.0")));

            test.AddAssertion("Walk contains sysContact",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.4.0")));

            test.AddAssertion("Walk contains sysName",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.5.0")));

            test.AddAssertion("Walk contains sysLocation",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.6.0")));

            test.AddAssertion("Walk contains sysServices",
                walkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.1.7.0")));

            test.AddMetric("WalkResultCount", walkResults.Count);
            test.AddAssertion("Walk returned expected number of results",
                walkResults.Count >= 7); // At least the 7 standard system objects

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP walk test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestGetBulkAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("GetBulk", "Test SNMP GETBULK operation on interfaces table");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("mib2-basic");
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var ipEndpoint = new IPEndPoint(ip, port);
            using var client = new SnmpClient(ipEndpoint, SnmpVersion.V2c, "public");

            // GETBULK on interfaces table (1.3.6.1.2.1.2.2.1)
            var bulkResults = await client.GetBulkAsync(0, 10, "1.3.6.1.2.1.2.2.1");

            test.AddAssertion("GETBULK results retrieved",
                bulkResults.Count > 0);

            test.AddAssertion("GETBULK returned interface data",
                bulkResults.Any(r => r.Oid.StartsWith("1.3.6.1.2.1.2.2.1.1")));

            test.AddAssertion("GETBULK returned multiple columns",
                bulkResults.Select(r => r.Oid.Split('.').Take(12).Last()).Distinct().Count() > 1);

            test.AddMetric("GetBulkResultCount", bulkResults.Count);

            // Verify we got data for all 3 interfaces
            var interfaceIndices = bulkResults
                .Where(r => r.Oid.StartsWith("1.3.6.1.2.1.2.2.1.1."))
                .Select(r => r.Oid.Split('.').Last())
                .Distinct()
                .ToList();

            test.AddAssertion("GETBULK returned data for 3 interfaces",
                interfaceIndices.Count == 3);

            test.AddAssertion("GETBULK returned interface 1",
                interfaceIndices.Contains("1"));

            test.AddAssertion("GETBULK returned interface 2",
                interfaceIndices.Contains("2"));

            test.AddAssertion("GETBULK returned interface 3",
                interfaceIndices.Contains("3"));

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GETBULK test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    public async ValueTask DisposeAsync()
    {
        await _reporter.DisposeAsync();
    }
}