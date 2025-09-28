using Microsoft.Extensions.Logging;
using nSNMP.Integration.Tests.Infrastructure;
using nSNMP.Integration.Tests.Reporting;
using nSNMP.Integration.Tests.Suites;
using System.Diagnostics;
using System.Reflection;

namespace nSNMP.Integration.Tests;

public class TestRunner : IAsyncDisposable
{
    private readonly TestRunnerOptions _options;
    private readonly ILogger<TestRunner> _logger;
    private readonly IntegrationTestFixture _fixture;
    private readonly TestReporter _reporter;

    public TestRunner(TestRunnerOptions? options = null, ILogger<TestRunner>? logger = null)
    {
        _options = options ?? new TestRunnerOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TestRunner>.Instance;
        _fixture = new IntegrationTestFixture(_options.ScenarioDirectory, _logger);
        _reporter = new TestReporter("IntegrationTestRun", _logger);

        SetupReporters();
    }

    private void SetupReporters()
    {
        if (_options.GenerateMarkdownReport)
        {
            _reporter.AddWriter(new MarkdownReportWriter(_options.OutputDirectory, _logger));
        }

        if (_options.GenerateJUnitReport)
        {
            _reporter.AddWriter(new JUnitReportWriter(_options.OutputDirectory, _logger));
        }
    }

    public async Task<TestRunResult> RunAllAsync(CancellationToken cancellationToken = default)
    {
        var runResult = new TestRunResult("nSNMP-Integration-Tests");

        try
        {
            _logger.LogInformation("Starting nSNMP Integration Test Run");

            // Add environment metadata
            AddEnvironmentMetadata(runResult);

            // Initialize test infrastructure
            await _fixture.InitializeAsync(cancellationToken);

            // Run test suites based on configuration
            if (_options.RunPrinterTests)
            {
                var printerSuite = await RunPrinterSuiteAsync(cancellationToken);
                runResult.AddTestSuite(printerSuite);
            }

            if (_options.RunMib2Tests)
            {
                var mib2Suite = await RunMib2SuiteAsync(cancellationToken);
                runResult.AddTestSuite(mib2Suite);
            }

            if (_options.RunTrapTests)
            {
                var trapsSuite = await RunTrapsSuiteAsync(cancellationToken);
                runResult.AddTestSuite(trapsSuite);
            }

            if (_options.RunNetworkImpairmentTests)
            {
                var adverseNetSuite = await RunAdverseNetSuiteAsync(cancellationToken);
                runResult.AddTestSuite(adverseNetSuite);
            }

            _logger.LogInformation("nSNMP Integration Test Run completed. Overall Status: {Success}",
                runResult.Success ? "PASSED" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Integration test run failed with exception");
            runResult.AddError($"Test run exception: {ex.Message}");
        }
        finally
        {
            runResult.Complete();

            // Generate reports
            await _reporter.WriteReportAsync(runResult, cancellationToken);

            // Collect and log container logs if requested
            if (_options.CollectContainerLogs)
            {
                await CollectContainerLogsAsync(runResult, cancellationToken);
            }
        }

        return runResult;
    }

    private async Task<TestSuiteResult> RunPrinterSuiteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Printer MIB test suite");

        try
        {
            using var suite = new PrinterSuite(_fixture, _logger);
            var result = await suite.RunAsync(cancellationToken);

            if (_options.GenerateIndividualSuiteReports)
            {
                await _reporter.WriteReportAsync(result, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer suite execution failed");
            var errorResult = new TestSuiteResult("PrinterSuite", "Printer MIB testing with supply levels and alerts");
            errorResult.AddError($"Suite execution failed: {ex.Message}");
            errorResult.Complete();
            return errorResult;
        }
    }

    private async Task<TestSuiteResult> RunMib2SuiteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running MIB-II test suite");

        try
        {
            using var suite = new Mib2Suite(_fixture, _logger);
            var result = await suite.RunAsync(cancellationToken);

            if (_options.GenerateIndividualSuiteReports)
            {
                await _reporter.WriteReportAsync(result, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MIB-II suite execution failed");
            var errorResult = new TestSuiteResult("Mib2Suite", "Standard MIB-II object testing with system and interface groups");
            errorResult.AddError($"Suite execution failed: {ex.Message}");
            errorResult.Complete();
            return errorResult;
        }
    }

    private async Task<TestSuiteResult> RunTrapsSuiteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Traps/Informs test suite");

        try
        {
            using var suite = new TrapsInformsSuite(_fixture, _logger);
            var result = await suite.RunAsync(cancellationToken);

            if (_options.GenerateIndividualSuiteReports)
            {
                await _reporter.WriteReportAsync(result, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Traps/Informs suite execution failed");
            var errorResult = new TestSuiteResult("TrapsInformsSuite", "SNMP trap and inform message testing");
            errorResult.AddError($"Suite execution failed: {ex.Message}");
            errorResult.Complete();
            return errorResult;
        }
    }

    private async Task<TestSuiteResult> RunAdverseNetSuiteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Adverse Network Conditions test suite");

        try
        {
            using var suite = new AdverseNetSuite(_fixture, _logger);
            var result = await suite.RunAsync(cancellationToken);

            if (_options.GenerateIndividualSuiteReports)
            {
                await _reporter.WriteReportAsync(result, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adverse Network Conditions suite execution failed");
            var errorResult = new TestSuiteResult("AdverseNetSuite", "SNMP behavior under network impairment conditions");
            errorResult.AddError($"Suite execution failed: {ex.Message}");
            errorResult.Complete();
            return errorResult;
        }
    }

    private void AddEnvironmentMetadata(TestRunResult runResult)
    {
        runResult.AddMetadata("Environment.MachineName", Environment.MachineName);
        runResult.AddMetadata("Environment.OSVersion", Environment.OSVersion.ToString());
        runResult.AddMetadata("Environment.ProcessorCount", Environment.ProcessorCount);
        runResult.AddMetadata("Environment.CLRVersion", Environment.Version.ToString());
        runResult.AddMetadata("Environment.WorkingDirectory", Environment.CurrentDirectory);

        // Assembly information
        var assembly = Assembly.GetExecutingAssembly();
        runResult.AddMetadata("Assembly.Version", assembly.GetName().Version?.ToString() ?? "Unknown");
        runResult.AddMetadata("Assembly.Location", assembly.Location);

        // Test configuration
        runResult.AddMetadata("TestConfig.ScenarioDirectory", _options.ScenarioDirectory);
        runResult.AddMetadata("TestConfig.OutputDirectory", _options.OutputDirectory);
        runResult.AddMetadata("TestConfig.RunPrinterTests", _options.RunPrinterTests);
        runResult.AddMetadata("TestConfig.RunMib2Tests", _options.RunMib2Tests);
        runResult.AddMetadata("TestConfig.RunTrapTests", _options.RunTrapTests);
        runResult.AddMetadata("TestConfig.RunNetworkImpairmentTests", _options.RunNetworkImpairmentTests);

        // Git information (if available)
        try
        {
            var gitCommit = Environment.GetEnvironmentVariable("GITHUB_SHA") ??
                           Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION") ??
                           "Unknown";
            var gitBranch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ??
                           Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH") ??
                           "Unknown";

            runResult.AddMetadata("Git.Commit", gitCommit);
            runResult.AddMetadata("Git.Branch", gitBranch);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve git information");
        }

        // CI/CD information
        var ciSystem = Environment.GetEnvironmentVariable("CI") != null ? "True" : "False";
        runResult.AddMetadata("CI.IsRunningInCI", ciSystem);

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null)
        {
            runResult.AddMetadata("CI.System", "GitHub Actions");
            runResult.AddMetadata("CI.RunId", Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "Unknown");
            runResult.AddMetadata("CI.RunNumber", Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "Unknown");
        }
    }

    private async Task CollectContainerLogsAsync(TestRunResult runResult, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Collecting container logs");

            var logs = await _fixture.CollectAllLogsAsync(cancellationToken);
            var logsPath = Path.Combine(_options.OutputDirectory, "container-logs.txt");

            await File.WriteAllTextAsync(logsPath, logs, cancellationToken);
            runResult.AddMetadata("ContainerLogsPath", logsPath);

            _logger.LogInformation("Container logs saved to: {LogsPath}", logsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect container logs");
            runResult.AddError($"Failed to collect container logs: {ex.Message}");
        }
    }

    public async Task<TestRunResult> RunSingleSuiteAsync(string suiteName, CancellationToken cancellationToken = default)
    {
        var runResult = new TestRunResult($"nSNMP-Integration-Tests-{suiteName}");
        AddEnvironmentMetadata(runResult);

        try
        {
            await _fixture.InitializeAsync(cancellationToken);

            TestSuiteResult? suiteResult = suiteName.ToLowerInvariant() switch
            {
                "printer" or "printersuite" => await RunPrinterSuiteAsync(cancellationToken),
                "mib2" or "mib2suite" => await RunMib2SuiteAsync(cancellationToken),
                "traps" or "trapsinformssuite" => await RunTrapsSuiteAsync(cancellationToken),
                "network" or "adversenetsuite" => await RunAdverseNetSuiteAsync(cancellationToken),
                _ => null
            };

            if (suiteResult != null)
            {
                runResult.AddTestSuite(suiteResult);
            }
            else
            {
                runResult.AddError($"Unknown test suite: {suiteName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Single suite run failed");
            runResult.AddError($"Suite run exception: {ex.Message}");
        }
        finally
        {
            runResult.Complete();
            await _reporter.WriteReportAsync(runResult, cancellationToken);
        }

        return runResult;
    }

    public async ValueTask DisposeAsync()
    {
        await _reporter.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}

public class TestRunnerOptions
{
    public string ScenarioDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Scenarios");
    public string OutputDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "TestResults");

    public bool RunPrinterTests { get; set; } = true;
    public bool RunMib2Tests { get; set; } = true;
    public bool RunTrapTests { get; set; } = true;
    public bool RunNetworkImpairmentTests { get; set; } = true;

    public bool GenerateMarkdownReport { get; set; } = true;
    public bool GenerateJUnitReport { get; set; } = true;
    public bool GenerateIndividualSuiteReports { get; set; } = false;
    public bool CollectContainerLogs { get; set; } = true;

    public int MaxConcurrentSuites { get; set; } = 1; // Run suites sequentially by default

    public static TestRunnerOptions FromEnvironment()
    {
        var options = new TestRunnerOptions();

        if (bool.TryParse(Environment.GetEnvironmentVariable("INTEGRATION_RUN_PRINTER_TESTS"), out var runPrinter))
            options.RunPrinterTests = runPrinter;

        if (bool.TryParse(Environment.GetEnvironmentVariable("INTEGRATION_RUN_MIB2_TESTS"), out var runMib2))
            options.RunMib2Tests = runMib2;

        if (bool.TryParse(Environment.GetEnvironmentVariable("INTEGRATION_RUN_TRAP_TESTS"), out var runTraps))
            options.RunTrapTests = runTraps;

        if (bool.TryParse(Environment.GetEnvironmentVariable("INTEGRATION_RUN_NETWORK_TESTS"), out var runNetwork))
            options.RunNetworkImpairmentTests = runNetwork;

        var outputDir = Environment.GetEnvironmentVariable("INTEGRATION_OUTPUT_DIRECTORY");
        if (!string.IsNullOrEmpty(outputDir))
            options.OutputDirectory = outputDir;

        var scenarioDir = Environment.GetEnvironmentVariable("INTEGRATION_SCENARIO_DIRECTORY");
        if (!string.IsNullOrEmpty(scenarioDir))
            options.ScenarioDirectory = scenarioDir;

        return options;
    }
}