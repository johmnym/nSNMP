using Microsoft.Extensions.Logging;
using nSNMP.Integration.Tests.Configuration;
using nSNMP.Integration.Tests.Reporting;
using System.Net;

namespace nSNMP.Integration.Tests;

/// <summary>
/// Simplified test runner to demonstrate the integration test framework
/// This validates the scenario configuration and test structure without requiring Docker
/// </summary>
public class SimpleTestRunner : IAsyncDisposable
{
    private readonly ILogger<SimpleTestRunner> _logger;
    private readonly string _scenarioDirectory;

    public SimpleTestRunner(string? scenarioDirectory = null, ILogger<SimpleTestRunner>? logger = null)
    {
        _scenarioDirectory = scenarioDirectory ?? Path.Combine(AppContext.BaseDirectory, "Scenarios");
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SimpleTestRunner>.Instance;
    }

    public async Task<TestRunResult> RunValidationAsync(CancellationToken cancellationToken = default)
    {
        var runResult = new TestRunResult("nSNMP-Integration-Validation");

        try
        {
            _logger.LogInformation("Starting nSNMP Integration Test Validation");

            // Add environment metadata
            AddEnvironmentMetadata(runResult);

            // Run validation tests
            var configValidation = await ValidateConfigurationAsync(cancellationToken);
            runResult.AddTestSuite(configValidation);

            var scenarioValidation = await ValidateScenarioDataAsync(cancellationToken);
            runResult.AddTestSuite(scenarioValidation);

            var apiValidation = await ValidateApiStructureAsync(cancellationToken);
            runResult.AddTestSuite(apiValidation);

            _logger.LogInformation("Integration test validation completed. Status: {Success}",
                runResult.Success ? "PASSED" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Integration test validation failed with exception");
            runResult.AddError($"Validation exception: {ex.Message}");
        }
        finally
        {
            runResult.Complete();
        }

        return runResult;
    }

    public async Task<TestSuiteResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var suite = new TestSuiteResult("ConfigurationValidation", "Validate YAML configuration files and structure");

        try
        {
            _logger.LogInformation("Validating configuration files");

            var loader = new ScenarioLoader();

            // Test scenarios.yml loading
            var configTest = new TestResult("ScenariosConfig", "Load and validate scenarios.yml");
            try
            {
                var scenarioPath = Path.Combine(_scenarioDirectory, "scenarios.yml");
                configTest.AddAssertion("scenarios.yml file exists", File.Exists(scenarioPath));

                if (File.Exists(scenarioPath))
                {
                    var scenarios = await loader.LoadScenariosAsync(scenarioPath, cancellationToken);

                    configTest.AddAssertion("Scenarios loaded successfully", scenarios != null);
                    configTest.AddAssertion("Has devices", scenarios?.Devices.Count > 0);
                    configTest.AddAssertion("Has MIB-2 device",
                        scenarios?.Devices.Any(d => d.Name?.Contains("mib2") == true) == true);
                    configTest.AddAssertion("Has printer devices",
                        scenarios?.Devices.Any(d => d.Name?.Contains("printer") == true) == true);

                    configTest.AddMetric("DeviceCount", scenarios?.Devices.Count ?? 0);
                    configTest.AddMetric("NetworkImpairmentCount", scenarios?.NetworkImpairment.Count ?? 0);

                    // Validate individual devices
                    foreach (var device in scenarios?.Devices ?? [])
                    {
                        configTest.AddAssertion($"Device {device.Name} has valid port",
                            device.UdpPort > 1024 && device.UdpPort < 65536);
                        configTest.AddAssertion($"Device {device.Name} has protocol config",
                            device.V2c != null || device.V3 != null);
                    }
                }

                configTest.SetSuccess(true);
            }
            catch (Exception ex)
            {
                configTest.AddError($"Configuration validation failed: {ex.Message}");
            }
            configTest.Complete();
            suite.AddTestResult(configTest);

            // Test v3-users.yml loading
            var usersTest = new TestResult("V3UsersConfig", "Load and validate v3-users.yml");
            try
            {
                var usersPath = Path.Combine(_scenarioDirectory, "v3-users.yml");
                usersTest.AddAssertion("v3-users.yml file exists", File.Exists(usersPath));

                if (File.Exists(usersPath))
                {
                    var users = await loader.LoadV3UsersAsync(usersPath, cancellationToken);

                    usersTest.AddAssertion("Users loaded successfully", users != null);
                    usersTest.AddAssertion("Has SNMPv3 users", users?.Users.Count > 0);

                    usersTest.AddMetric("V3UserCount", users?.Users.Count ?? 0);

                    foreach (var user in users?.Users ?? [])
                    {
                        usersTest.AddAssertion($"User {user.Username} has auth key",
                            !string.IsNullOrEmpty(user.AuthKey));
                        usersTest.AddAssertion($"User {user.Username} has priv key",
                            !string.IsNullOrEmpty(user.PrivKey));
                    }
                }

                usersTest.SetSuccess(true);
            }
            catch (Exception ex)
            {
                usersTest.AddError($"V3 users validation failed: {ex.Message}");
            }
            usersTest.Complete();
            suite.AddTestResult(usersTest);

        }
        catch (Exception ex)
        {
            suite.AddError($"Configuration validation suite failed: {ex.Message}");
        }

        suite.Complete();
        return suite;
    }

    public async Task<TestSuiteResult> ValidateScenarioDataAsync(CancellationToken cancellationToken = default)
    {
        var suite = new TestSuiteResult("ScenarioDataValidation", "Validate SNMP record files and test data");

        try
        {
            _logger.LogInformation("Validating scenario data files");

            var expectedFiles = new[]
            {
                "mib2-basic.snmprec",
                "printers-mono.snmprec",
                "printers-color.snmprec",
                "oversized.snmprec"
            };

            foreach (var filename in expectedFiles)
            {
                var test = new TestResult($"DataFile_{filename}", $"Validate {filename}");

                try
                {
                    var filePath = Path.Combine(_scenarioDirectory, filename);
                    test.AddAssertion($"{filename} exists", File.Exists(filePath));

                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        test.AddAssertion($"{filename} is not empty", lines.Length > 0);

                        var dataLines = lines.Where(l => !l.TrimStart().StartsWith('#') && l.Contains('|')).ToArray();
                        test.AddAssertion($"{filename} has SNMP data", dataLines.Length > 0);

                        test.AddMetric($"{filename}_Lines", lines.Length);
                        test.AddMetric($"{filename}_DataLines", dataLines.Length);

                        // Validate format
                        if (dataLines.Length > 0)
                        {
                            var sampleLine = dataLines[0];
                            var parts = sampleLine.Split('|');
                            test.AddAssertion($"{filename} has correct format (OID|TYPE|VALUE)", parts.Length == 3);

                            if (parts.Length == 3)
                            {
                                test.AddAssertion($"{filename} has valid OID format",
                                    parts[0].Contains('.') && parts[0].Length > 5);
                            }
                        }

                        // File-specific validations
                        if (filename?.Contains("mib2") == true)
                        {
                            test.AddAssertion("MIB-2 file contains system OIDs",
                                content.Contains("1.3.6.1.2.1.1."));
                        }
                        else if (filename?.Contains("printer") == true)
                        {
                            test.AddAssertion("Printer file contains printer MIB OIDs",
                                content.Contains("1.3.6.1.2.1.43."));
                        }
                    }

                    test.SetSuccess(true);
                }
                catch (Exception ex)
                {
                    test.AddError($"Data file validation failed: {ex.Message}");
                }

                test.Complete();
                suite.AddTestResult(test);
            }
        }
        catch (Exception ex)
        {
            suite.AddError($"Scenario data validation suite failed: {ex.Message}");
        }

        suite.Complete();
        return suite;
    }

    public async Task<TestSuiteResult> ValidateApiStructureAsync(CancellationToken cancellationToken = default)
    {
        var suite = new TestSuiteResult("ApiValidation", "Validate nSNMP API structure and basic functionality");

        try
        {
            _logger.LogInformation("Validating nSNMP API structure");

            // Test SnmpClient creation
            var clientTest = new TestResult("SnmpClientCreation", "Test SNMP client instantiation");
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Loopback, 161);
                using var client = new nSNMP.Manager.SnmpClient(endpoint);

                clientTest.AddAssertion("SnmpClient can be created", client != null);
                clientTest.AddAssertion("SnmpClient is disposable", client is IDisposable);

                clientTest.SetSuccess(true);
            }
            catch (Exception ex)
            {
                clientTest.AddError($"SnmpClient creation failed: {ex.Message}");
            }
            clientTest.Complete();
            suite.AddTestResult(clientTest);

            // Test ObjectIdentifier creation
            var oidTest = new TestResult("ObjectIdentifierCreation", "Test OID creation and formatting");
            try
            {
                var systemOids = new[]
                {
                    "1.3.6.1.2.1.1.1.0",
                    "1.3.6.1.2.1.1.2.0",
                    "1.3.6.1.2.1.1.3.0"
                };

                foreach (var oidStr in systemOids)
                {
                    var oid = nSNMP.SMI.DataTypes.V1.Primitive.ObjectIdentifier.Create(oidStr);
                    oidTest.AddAssertion($"OID {oidStr} created successfully", oid != null);
                    oidTest.AddAssertion($"OID {oidStr} has correct format",
                        oid?.ToString().EndsWith(oidStr) == true);
                }

                oidTest.AddMetric("ValidOidsCreated", systemOids.Length);
                oidTest.SetSuccess(true);
            }
            catch (Exception ex)
            {
                oidTest.AddError($"ObjectIdentifier creation failed: {ex.Message}");
            }
            oidTest.Complete();
            suite.AddTestResult(oidTest);

            // Test SNMP data types
            var dataTypesTest = new TestResult("SnmpDataTypes", "Test SNMP data type creation");
            try
            {
                var integer = nSNMP.SMI.DataTypes.V1.Primitive.Integer.Create(42);
                var counter = nSNMP.SMI.DataTypes.V1.Primitive.Counter32.Create(1234567);
                var gauge = nSNMP.SMI.DataTypes.V1.Primitive.Gauge32.Create(98765);
                var ticks = nSNMP.SMI.DataTypes.V1.Primitive.TimeTicks.Create(123456789);

                dataTypesTest.AddAssertion("Integer created", integer != null && integer.Value == 42);
                dataTypesTest.AddAssertion("Counter32 created", counter != null && counter.Value == 1234567);
                dataTypesTest.AddAssertion("Gauge32 created", gauge != null && gauge.Value == 98765);
                dataTypesTest.AddAssertion("TimeTicks created", ticks != null && ticks.Value == 123456789);

                dataTypesTest.SetSuccess(true);
            }
            catch (Exception ex)
            {
                dataTypesTest.AddError($"SNMP data types test failed: {ex.Message}");
            }
            dataTypesTest.Complete();
            suite.AddTestResult(dataTypesTest);

            // Test timeout behavior (simulated)
            var timeoutTest = new TestResult("TimeoutHandling", "Test timeout configuration");
            try
            {
                var endpoint = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 161); // RFC 5737 test address
                var timeout = TimeSpan.FromMilliseconds(500);

                using var client = new nSNMP.Manager.SnmpClient(endpoint, timeout: timeout);
                timeoutTest.AddAssertion("Client with custom timeout created", client != null);

                // Test that timeout exception occurs (but catch it as expected)
                var timeoutOccurred = false;
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    var result = await client!.GetAsync("1.3.6.1.2.1.1.1.0");
                    _ = result; // Suppress unused variable warning
                    // Intentionally not using the result as we expect this to timeout
                }
                catch
                {
                    timeoutOccurred = true;
                }

                timeoutTest.AddAssertion("Timeout behavior works", timeoutOccurred);
                timeoutTest.SetSuccess(true);
            }
            catch (Exception ex)
            {
                timeoutTest.AddError($"Timeout handling test failed: {ex.Message}");
            }
            timeoutTest.Complete();
            suite.AddTestResult(timeoutTest);

            await Task.Delay(10, cancellationToken); // Simulate async work
        }
        catch (Exception ex)
        {
            suite.AddError($"API validation suite failed: {ex.Message}");
        }

        suite.Complete();
        return suite;
    }

    private void AddEnvironmentMetadata(TestRunResult runResult)
    {
        runResult.AddMetadata("Environment.MachineName", Environment.MachineName);
        runResult.AddMetadata("Environment.OSVersion", Environment.OSVersion.ToString());
        runResult.AddMetadata("Environment.ProcessorCount", Environment.ProcessorCount);
        runResult.AddMetadata("Environment.CLRVersion", Environment.Version.ToString());
        runResult.AddMetadata("Environment.WorkingDirectory", Environment.CurrentDirectory);

        runResult.AddMetadata("TestConfig.ScenarioDirectory", _scenarioDirectory);
        runResult.AddMetadata("TestConfig.ValidationMode", "True");
        runResult.AddMetadata("TestConfig.ContainerMode", "False");
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}