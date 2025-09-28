using Microsoft.Extensions.Logging;
using nSNMP.Integration.Tests.Configuration;
using nSNMP.Integration.Tests.Reporting;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace nSNMP.Integration.Tests;

public class IntegrationTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly SimpleTestRunner _runner;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _runner = new SimpleTestRunner();
    }

    [Fact]
    public async Task ConfigurationValidation_ShouldPass()
    {
        var result = await _runner.ValidateConfigurationAsync();

        _output.WriteLine($"Configuration validation: {result.Name}");
        _output.WriteLine($"Tests: {result.PassedCount}/{result.TotalCount}");
        _output.WriteLine($"Duration: {result.DurationMs}ms");

        if (!result.Success)
        {
            foreach (var test in result.TestResults.Where(t => !t.Success))
            {
                _output.WriteLine($"FAILED: {test.Name}");
                foreach (var error in test.Errors)
                {
                    _output.WriteLine($"  Error: {error}");
                }
            }
        }

        Assert.True(result.Success, $"Configuration validation failed: {result.PassedCount}/{result.TotalCount} tests passed");
    }

    [Fact]
    public async Task ScenarioDataValidation_ShouldPass()
    {
        var result = await _runner.ValidateScenarioDataAsync();

        _output.WriteLine($"Scenario data validation: {result.Name}");
        _output.WriteLine($"Tests: {result.PassedCount}/{result.TotalCount}");
        _output.WriteLine($"Duration: {result.DurationMs}ms");

        if (!result.Success)
        {
            foreach (var test in result.TestResults.Where(t => !t.Success))
            {
                _output.WriteLine($"FAILED: {test.Name}");
                foreach (var error in test.Errors)
                {
                    _output.WriteLine($"  Error: {error}");
                }
            }
        }

        Assert.True(result.Success, $"Scenario data validation failed: {result.PassedCount}/{result.TotalCount} tests passed");
    }

    [Fact]
    public async Task ApiValidation_ShouldPass()
    {
        var result = await _runner.ValidateApiStructureAsync();

        _output.WriteLine($"API validation: {result.Name}");
        _output.WriteLine($"Tests: {result.PassedCount}/{result.TotalCount}");
        _output.WriteLine($"Duration: {result.DurationMs}ms");

        if (!result.Success)
        {
            foreach (var test in result.TestResults.Where(t => !t.Success))
            {
                _output.WriteLine($"FAILED: {test.Name}");
                foreach (var error in test.Errors)
                {
                    _output.WriteLine($"  Error: {error}");
                }
            }
        }

        Assert.True(result.Success, $"API validation failed: {result.PassedCount}/{result.TotalCount} tests passed");
    }

    [Fact]
    public async Task FullIntegrationValidation_ShouldPass()
    {
        var result = await _runner.RunValidationAsync();

        _output.WriteLine("=== nSNMP Integration Test Validation Results ===");
        _output.WriteLine($"📊 Overall Status: {(result.Success ? "✅ PASSED" : "❌ FAILED")}");
        _output.WriteLine($"⏱️  Duration: {result.DurationMs / 1000.0:F1} seconds");
        _output.WriteLine($"📋 Test Suites: {result.PassedSuites}/{result.TotalSuites} passed");
        _output.WriteLine($"🧪 Total Tests: {result.PassedTests}/{result.TotalTests} passed");
        _output.WriteLine($"📈 Success Rate: {(result.TotalTests > 0 ? (double)result.PassedTests / result.TotalTests * 100 : 0):F1}%");

        if (result.TestSuites.Count > 0)
        {
            _output.WriteLine("");
            _output.WriteLine("📋 Suite Details:");
            foreach (var suite in result.TestSuites)
            {
                var icon = suite.Success ? "✅" : "❌";
                var rate = suite.TotalCount > 0 ? (double)suite.PassedCount / suite.TotalCount * 100 : 0;
                _output.WriteLine($"  {icon} {suite.Name}: {suite.PassedCount}/{suite.TotalCount} tests ({rate:F0}%) - {suite.DurationMs}ms");

                if (!suite.Success && suite.TestResults.Any(t => !t.Success))
                {
                    foreach (var failedTest in suite.TestResults.Where(t => !t.Success))
                    {
                        _output.WriteLine($"     ❌ {failedTest.Name}: {failedTest.Errors.FirstOrDefault() ?? "Test failed"}");
                    }
                }
            }
        }

        // Generate reports for reference
        var outputDir = Path.Combine(AppContext.BaseDirectory, "TestResults");
        Directory.CreateDirectory(outputDir);

        var markdownWriter = new MarkdownReportWriter(outputDir);
        var junitWriter = new JUnitReportWriter(outputDir);

        await markdownWriter.WriteRunReportAsync(result);
        await junitWriter.WriteRunReportAsync(result);

        _output.WriteLine("");
        _output.WriteLine($"📁 Reports generated in: {outputDir}");
        _output.WriteLine("   - TestSummary.md (human-readable summary)");
        _output.WriteLine("   - TestRun_*.md (detailed Markdown report)");
        _output.WriteLine("   - TEST-*.xml (JUnit XML for CI/CD)");

        Assert.True(result.Success, $"Integration validation failed: {result.PassedTests}/{result.TotalTests} tests passed");
    }

    public async ValueTask DisposeAsync()
    {
        await _runner.DisposeAsync();
    }
}