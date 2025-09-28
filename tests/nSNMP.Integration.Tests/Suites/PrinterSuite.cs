using Microsoft.Extensions.Logging;
using nSNMP.Manager;
using nSNMP.Integration.Tests.Infrastructure;
using nSNMP.Integration.Tests.Reporting;
using System.Net;

namespace nSNMP.Integration.Tests.Suites;

public class PrinterSuite : IAsyncDisposable
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ILogger<PrinterSuite> _logger;
    private readonly TestReporter _reporter;

    public PrinterSuite(IntegrationTestFixture fixture, ILogger<PrinterSuite>? logger = null)
    {
        _fixture = fixture;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PrinterSuite>.Instance;
        _reporter = new TestReporter("PrinterSuite", _logger);
    }

    public async Task<TestSuiteResult> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Printer test suite");
        await _fixture.InitializeAsync(cancellationToken);

        var suiteResult = new TestSuiteResult("PrinterSuite", "Printer MIB testing with supply levels and alerts");

        try
        {
            // Test monochrome printer
            var monoResult = await TestMonochromePrinterAsync(cancellationToken);
            suiteResult.AddTestResult(monoResult);

            // Test color printer
            var colorResult = await TestColorPrinterAsync(cancellationToken);
            suiteResult.AddTestResult(colorResult);

            // Test printer alerts
            var alertsResult = await TestPrinterAlertsAsync(cancellationToken);
            suiteResult.AddTestResult(alertsResult);

            // Test printer input/output trays
            var traysResult = await TestPrinterTraysAsync(cancellationToken);
            suiteResult.AddTestResult(traysResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer suite failed with exception");
            suiteResult.AddError($"Suite exception: {ex.Message}");
        }

        suiteResult.Complete();
        _logger.LogInformation("Printer test suite completed. Success: {Success}, Tests: {TestCount}, Duration: {Duration}ms",
            suiteResult.Success, suiteResult.TestResults.Count, suiteResult.DurationMs);

        return suiteResult;
    }

    private async Task<TestResult> TestMonochromePrinterAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("MonochromePrinter", "Test monochrome printer with black toner supply");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("printer-mono", cancellationToken);
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            using var client = new SnmpClient(ip, port);

            // Test basic printer identification
            var sysDescr = await client.GetAsync("1.3.6.1.2.1.1.1.0", cancellationToken);
            test.AddAssertion("System description contains 'HP LaserJet'",
                sysDescr?.Value?.ToString()?.Contains("HP LaserJet") == true);

            // Test printer general information
            var printerName = await client.GetAsync("1.3.6.1.2.1.43.5.1.1.2.1", cancellationToken);
            test.AddAssertion("Printer name retrieved",
                !string.IsNullOrEmpty(printerName?.Value?.ToString()));

            var printerStatus = await client.GetAsync("1.3.6.1.2.1.43.5.1.1.16.1", cancellationToken);
            test.AddAssertion("Printer status is 4 (idle)",
                printerStatus?.Value?.ToString() == "4");

            // Test black toner supply
            var tonerDescription = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.3.1.1", cancellationToken);
            test.AddAssertion("Toner description contains 'Black Toner'",
                tonerDescription?.Value?.ToString()?.Contains("Black Toner") == true);

            var tonerLevel = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.8.1.1", cancellationToken);
            var tonerMaxLevel = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.9.1.1", cancellationToken);

            if (int.TryParse(tonerLevel?.Value?.ToString(), out var currentLevel) &&
                int.TryParse(tonerMaxLevel?.Value?.ToString(), out var maxLevel))
            {
                var percentage = (double)currentLevel / maxLevel * 100;
                test.AddAssertion($"Toner level is reasonable (75%): {percentage:F1}%",
                    percentage >= 70 && percentage <= 80);
                test.AddMetric("TonerLevelPercent", percentage);
            }
            else
            {
                test.AddAssertion("Toner levels retrieved", false);
            }

            // Test transfer kit (maintenance supply)
            var transferKitDesc = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.3.1.2", cancellationToken);
            test.AddAssertion("Transfer kit description retrieved",
                transferKitDesc?.Value?.ToString()?.Contains("Transfer Kit") == true);

            var transferKitLevel = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.8.1.2", cancellationToken);
            test.AddAssertion("Transfer kit level is -1 (unknown capacity)",
                transferKitLevel?.Value?.ToString() == "-1");

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monochrome printer test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestColorPrinterAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("ColorPrinter", "Test color printer with CMYK supplies using SNMPv3");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("printer-color", cancellationToken);
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            // Use SNMPv3 credentials from configuration
            var scenarios = _fixture.GetScenarios();
            var colorDevice = scenarios.Devices.First(d => d.Name == "printer-color");
            var v3Config = colorDevice.V3!;

            using var client = new SnmpV3Client(ip, port, v3Config.Username, v3Config.AuthKey, v3Config.PrivKey);

            // Test basic printer identification
            var sysDescr = await client.GetAsync("1.3.6.1.2.1.1.1.0", cancellationToken);
            test.AddAssertion("System description contains 'Canon imageRUNNER'",
                sysDescr?.Value?.ToString()?.Contains("Canon imageRUNNER") == true);

            // Test CMYK supplies
            var supplies = new[]
            {
                (Color: "Cyan", Index: 1, ExpectedChar: "C"),
                (Color: "Magenta", Index: 2, ExpectedChar: "M"),
                (Color: "Yellow", Index: 3, ExpectedChar: "Y"),
                (Color: "Black", Index: 4, ExpectedChar: "K")
            };

            foreach (var supply in supplies)
            {
                var description = await client.GetAsync($"1.3.6.1.2.1.43.11.1.1.3.1.{supply.Index}", cancellationToken);
                test.AddAssertion($"{supply.Color} toner description correct",
                    description?.Value?.ToString()?.Contains($"{supply.Color} Toner") == true);

                var colorMarker = await client.GetAsync($"1.3.6.1.2.1.43.11.1.1.4.1.{supply.Index}", cancellationToken);
                test.AddAssertion($"{supply.Color} marker is '{supply.ExpectedChar}'",
                    colorMarker?.Value?.ToString() == supply.ExpectedChar);

                var currentLevel = await client.GetAsync($"1.3.6.1.2.1.43.11.1.1.8.1.{supply.Index}", cancellationToken);
                var maxLevel = await client.GetAsync($"1.3.6.1.2.1.43.11.1.1.9.1.{supply.Index}", cancellationToken);

                if (int.TryParse(currentLevel?.Value?.ToString(), out var current) &&
                    int.TryParse(maxLevel?.Value?.ToString(), out var max))
                {
                    var percentage = (double)current / max * 100;
                    test.AddMetric($"{supply.Color}TonerPercent", percentage);

                    // Specific assertions based on test data
                    switch (supply.Color)
                    {
                        case "Cyan":
                            test.AddAssertion("Cyan toner level ~45%", Math.Abs(percentage - 44.6) < 5);
                            break;
                        case "Magenta":
                            test.AddAssertion("Magenta toner level ~16% (low)", Math.Abs(percentage - 16.1) < 5);
                            break;
                        case "Yellow":
                            test.AddAssertion("Yellow toner level ~75%", Math.Abs(percentage - 75.0) < 5);
                            break;
                        case "Black":
                            test.AddAssertion("Black toner level ~71%", Math.Abs(percentage - 71.1) < 5);
                            break;
                    }
                }
            }

            // Test waste toner container
            var wasteDesc = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.3.1.5", cancellationToken);
            test.AddAssertion("Waste toner container found",
                wasteDesc?.Value?.ToString()?.Contains("Waste Toner") == true);

            var wasteLevel = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.8.1.5", cancellationToken);
            var wasteMaxLevel = await client.GetAsync("1.3.6.1.2.1.43.11.1.1.9.1.5", cancellationToken);

            if (int.TryParse(wasteLevel?.Value?.ToString(), out var wasteAmount) &&
                int.TryParse(wasteMaxLevel?.Value?.ToString(), out var wasteMax))
            {
                var wastePercentage = (double)wasteAmount / wasteMax * 100;
                test.AddAssertion("Waste toner level ~92%", Math.Abs(wastePercentage - 92.0) < 5);
                test.AddMetric("WasteToneerPercent", wastePercentage);
            }

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Color printer test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestPrinterAlertsAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("PrinterAlerts", "Test printer alert table with various alert conditions");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("printer-color", cancellationToken);
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var scenarios = _fixture.GetScenarios();
            var colorDevice = scenarios.Devices.First(d => d.Name == "printer-color");
            var v3Config = colorDevice.V3!;

            using var client = new SnmpV3Client(ip, port, v3Config.Username, v3Config.AuthKey, v3Config.PrivKey);

            // Test specific alerts from the color printer data
            var alerts = new[]
            {
                (Index: 1, Type: 8, Group: 1502, Code: 2, Description: "Magenta toner is low"),
                (Index: 2, Type: 25, Group: 801, Code: 2, Description: "Paper jam detected in Tray 2"),
                (Index: 3, Type: 6, Group: 501, Code: 1, Description: "Front cover is open")
            };

            foreach (var alert in alerts)
            {
                var alertType = await client.GetAsync($"1.3.6.1.2.1.43.18.1.1.4.1.{alert.Index}", cancellationToken);
                test.AddAssertion($"Alert {alert.Index} type is {alert.Type}",
                    alertType?.Value?.ToString() == alert.Type.ToString());

                var alertGroup = await client.GetAsync($"1.3.6.1.2.1.43.18.1.1.6.1.{alert.Index}", cancellationToken);
                test.AddAssertion($"Alert {alert.Index} group is {alert.Group}",
                    alertGroup?.Value?.ToString() == alert.Group.ToString());

                var alertCode = await client.GetAsync($"1.3.6.1.2.1.43.18.1.1.7.1.{alert.Index}", cancellationToken);
                test.AddAssertion($"Alert {alert.Index} code is {alert.Code}",
                    alertCode?.Value?.ToString() == alert.Code.ToString());

                var alertDescription = await client.GetAsync($"1.3.6.1.2.1.43.18.1.1.8.1.{alert.Index}", cancellationToken);
                test.AddAssertion($"Alert {alert.Index} description contains expected text",
                    alertDescription?.Value?.ToString()?.Contains(alert.Description.Split(' ')[0]) == true);

                test.AddMetric($"Alert{alert.Index}Type", alert.Type);
            }

            // Test alert severities
            var alert1Severity = await client.GetAsync("1.3.6.1.2.1.43.18.1.1.5.1.1", cancellationToken);
            test.AddAssertion("Alert 1 (toner low) severity is 1 (warning)",
                alert1Severity?.Value?.ToString() == "1");

            var alert2Severity = await client.GetAsync("1.3.6.1.2.1.43.18.1.1.5.1.2", cancellationToken);
            test.AddAssertion("Alert 2 (paper jam) severity is 1 (warning)",
                alert2Severity?.Value?.ToString() == "1");

            var alert3Severity = await client.GetAsync("1.3.6.1.2.1.43.18.1.1.5.1.3", cancellationToken);
            test.AddAssertion("Alert 3 (cover open) severity is 1 (warning)",
                alert3Severity?.Value?.ToString() == "1");

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer alerts test failed");
            test.AddError($"Test exception: {ex.Message}");
        }

        test.Complete();
        return test;
    }

    private async Task<TestResult> TestPrinterTraysAsync(CancellationToken cancellationToken)
    {
        var test = new TestResult("PrinterTrays", "Test printer input and output tray status");

        try
        {
            var container = await _fixture.GetOrCreateDeviceAsync("printer-color", cancellationToken);
            var endpoint = container.GetConnectionEndpoint();
            var parts = endpoint.Split(':');
            var ip = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);

            var scenarios = _fixture.GetScenarios();
            var colorDevice = scenarios.Devices.First(d => d.Name == "printer-color");
            var v3Config = colorDevice.V3!;

            using var client = new SnmpV3Client(ip, port, v3Config.Username, v3Config.AuthKey, v3Config.PrivKey);

            // Test input trays
            var inputTrays = new[]
            {
                (Index: 1, Name: "Tray 1", MaxCapacity: 500, CurrentLevel: 420),
                (Index: 2, Name: "Tray 2", MaxCapacity: 250, CurrentLevel: 0)
            };

            foreach (var tray in inputTrays)
            {
                var maxCapacity = await client.GetAsync($"1.3.6.1.2.1.43.8.2.1.10.1.{tray.Index}", cancellationToken);
                test.AddAssertion($"Input {tray.Name} max capacity is {tray.MaxCapacity}",
                    maxCapacity?.Value?.ToString() == tray.MaxCapacity.ToString());

                var currentLevel = await client.GetAsync($"1.3.6.1.2.1.43.8.2.1.11.1.{tray.Index}", cancellationToken);
                test.AddAssertion($"Input {tray.Name} current level is {tray.CurrentLevel}",
                    currentLevel?.Value?.ToString() == tray.CurrentLevel.ToString());

                var trayName = await client.GetAsync($"1.3.6.1.2.1.43.8.2.1.13.1.{tray.Index}", cancellationToken);
                test.AddAssertion($"Input tray name is {tray.Name}",
                    trayName?.Value?.ToString() == tray.Name);

                if (tray.MaxCapacity > 0)
                {
                    var percentage = (double)tray.CurrentLevel / tray.MaxCapacity * 100;
                    test.AddMetric($"Input{tray.Name.Replace(" ", "")}Percent", percentage);
                }
            }

            // Test output tray
            var outputMaxCapacity = await client.GetAsync("1.3.6.1.2.1.43.9.2.1.8.1.1", cancellationToken);
            test.AddAssertion("Output tray max capacity is 500",
                outputMaxCapacity?.Value?.ToString() == "500");

            var outputCurrentLevel = await client.GetAsync("1.3.6.1.2.1.43.9.2.1.9.1.1", cancellationToken);
            test.AddAssertion("Output tray current level is 125",
                outputCurrentLevel?.Value?.ToString() == "125");

            var outputTrayName = await client.GetAsync("1.3.6.1.2.1.43.9.2.1.12.1.1", cancellationToken);
            test.AddAssertion("Output tray name is 'Output Tray'",
                outputTrayName?.Value?.ToString() == "Output Tray");

            if (int.TryParse(outputCurrentLevel?.Value?.ToString(), out var outputLevel) &&
                int.TryParse(outputMaxCapacity?.Value?.ToString(), out var outputMax))
            {
                var outputPercentage = (double)outputLevel / outputMax * 100;
                test.AddMetric("OutputTrayPercent", outputPercentage);
                test.AddAssertion("Output tray is 25% full", Math.Abs(outputPercentage - 25.0) < 1);
            }

            test.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer trays test failed");
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