using Microsoft.Extensions.Logging;
using System.Text;

namespace nSNMP.Integration.Tests.Reporting;

public class MarkdownReportWriter : ITestReportWriter, IAsyncDisposable
{
    private readonly string _outputDirectory;
    private readonly ILogger<MarkdownReportWriter> _logger;

    public MarkdownReportWriter(string outputDirectory, ILogger<MarkdownReportWriter>? logger = null)
    {
        _outputDirectory = outputDirectory;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MarkdownReportWriter>.Instance;

        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task WriteSuiteReportAsync(TestSuiteResult suiteResult, CancellationToken cancellationToken = default)
    {
        var fileName = $"{suiteResult.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var content = GenerateSuiteMarkdown(suiteResult);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);

        _logger.LogInformation("Markdown suite report written to: {FilePath}", filePath);
    }

    public async Task WriteRunReportAsync(TestRunResult runResult, CancellationToken cancellationToken = default)
    {
        var fileName = $"TestRun_{runResult.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var content = GenerateRunMarkdown(runResult);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);

        _logger.LogInformation("Markdown run report written to: {FilePath}", filePath);

        // Also create a summary file
        var summaryPath = Path.Combine(_outputDirectory, "TestSummary.md");
        var summaryContent = GenerateRunSummaryMarkdown(runResult);
        await File.WriteAllTextAsync(summaryPath, summaryContent, cancellationToken);

        _logger.LogInformation("Markdown summary report written to: {FilePath}", summaryPath);
    }

    private string GenerateSuiteMarkdown(TestSuiteResult suiteResult)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Test Suite Report: {suiteResult.Name}");
        sb.AppendLine();
        sb.AppendLine($"**Description:** {suiteResult.Description}");
        sb.AppendLine($"**Start Time:** {suiteResult.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Duration:** {suiteResult.DurationMs:N0} ms");
        sb.AppendLine($"**Status:** {(suiteResult.Success ? "✅ PASSED" : "❌ FAILED")}");
        sb.AppendLine();

        // Summary
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Total Tests:** {suiteResult.TotalCount}");
        sb.AppendLine($"- **Passed:** {suiteResult.PassedCount} ✅");
        sb.AppendLine($"- **Failed:** {suiteResult.FailedCount} ❌");
        sb.AppendLine($"- **Success Rate:** {(suiteResult.TotalCount > 0 ? (double)suiteResult.PassedCount / suiteResult.TotalCount * 100 : 0):F1}%");
        sb.AppendLine();

        // Errors (if any)
        if (suiteResult.Errors.Count > 0)
        {
            sb.AppendLine("## Suite Errors");
            sb.AppendLine();
            foreach (var error in suiteResult.Errors)
            {
                sb.AppendLine($"- ❌ {error}");
            }
            sb.AppendLine();
        }

        // Test Results
        sb.AppendLine("## Test Results");
        sb.AppendLine();

        foreach (var test in suiteResult.TestResults)
        {
            sb.AppendLine($"### {test.Name} {(test.Success ? "✅" : "❌")}");
            sb.AppendLine();
            sb.AppendLine($"**Description:** {test.Description}");
            sb.AppendLine($"**Duration:** {test.DurationMs:N0} ms");
            sb.AppendLine($"**Assertions:** {test.PassedAssertions}/{test.TotalAssertions} passed");
            sb.AppendLine();

            // Assertions
            if (test.Assertions.Count > 0)
            {
                sb.AppendLine("#### Assertions");
                sb.AppendLine();
                foreach (var assertion in test.Assertions)
                {
                    var icon = assertion.Passed ? "✅" : "❌";
                    sb.AppendLine($"- {icon} {assertion.Description}");
                    if (!string.IsNullOrEmpty(assertion.Details))
                    {
                        sb.AppendLine($"  - Details: {assertion.Details}");
                    }
                }
                sb.AppendLine();
            }

            // Metrics
            if (test.Metrics.Count > 0)
            {
                sb.AppendLine("#### Metrics");
                sb.AppendLine();
                sb.AppendLine("| Metric | Value | Unit |");
                sb.AppendLine("|--------|-------|------|");
                foreach (var metric in test.Metrics)
                {
                    sb.AppendLine($"| {metric.Name} | {metric.Value:F2} | {metric.Unit ?? ""} |");
                }
                sb.AppendLine();
            }

            // Errors
            if (test.Errors.Count > 0)
            {
                sb.AppendLine("#### Errors");
                sb.AppendLine();
                foreach (var error in test.Errors)
                {
                    sb.AppendLine($"- ❌ {error}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateRunMarkdown(TestRunResult runResult)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# Integration Test Run: {runResult.Name}");
        sb.AppendLine();
        sb.AppendLine($"**Start Time:** {runResult.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**End Time:** {runResult.EndTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Duration:** {runResult.DurationMs:N0} ms");
        sb.AppendLine($"**Status:** {(runResult.Success ? "✅ PASSED" : "❌ FAILED")}");
        sb.AppendLine();

        // Overall Summary
        sb.AppendLine("## Overall Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Test Suites:** {runResult.PassedSuites}/{runResult.TotalSuites} passed");
        sb.AppendLine($"- **Total Tests:** {runResult.PassedTests}/{runResult.TotalTests} passed");
        sb.AppendLine($"- **Suite Success Rate:** {(runResult.TotalSuites > 0 ? (double)runResult.PassedSuites / runResult.TotalSuites * 100 : 0):F1}%");
        sb.AppendLine($"- **Test Success Rate:** {(runResult.TotalTests > 0 ? (double)runResult.PassedTests / runResult.TotalTests * 100 : 0):F1}%");
        sb.AppendLine();

        // Metadata
        if (runResult.Metadata.Count > 0)
        {
            sb.AppendLine("## Test Environment");
            sb.AppendLine();
            foreach (var kvp in runResult.Metadata)
            {
                sb.AppendLine($"- **{kvp.Key}:** {kvp.Value}");
            }
            sb.AppendLine();
        }

        // Suite Summary Table
        sb.AppendLine("## Suite Results");
        sb.AppendLine();
        sb.AppendLine("| Suite | Status | Tests | Duration | Success Rate |");
        sb.AppendLine("|-------|--------|-------|----------|--------------|");

        foreach (var suite in runResult.TestSuites)
        {
            var status = suite.Success ? "✅ PASSED" : "❌ FAILED";
            var successRate = suite.TotalCount > 0 ? (double)suite.PassedCount / suite.TotalCount * 100 : 0;
            sb.AppendLine($"| {suite.Name} | {status} | {suite.PassedCount}/{suite.TotalCount} | {suite.DurationMs:N0} ms | {successRate:F1}% |");
        }
        sb.AppendLine();

        // Run Errors
        if (runResult.Errors.Count > 0)
        {
            sb.AppendLine("## Run Errors");
            sb.AppendLine();
            foreach (var error in runResult.Errors)
            {
                sb.AppendLine($"- ❌ {error}");
            }
            sb.AppendLine();
        }

        // Individual Suite Details
        sb.AppendLine("## Suite Details");
        sb.AppendLine();

        foreach (var suite in runResult.TestSuites)
        {
            sb.AppendLine($"### {suite.Name} {(suite.Success ? "✅" : "❌")}");
            sb.AppendLine();
            sb.AppendLine($"**Description:** {suite.Description}");
            sb.AppendLine($"**Duration:** {suite.DurationMs:N0} ms");
            sb.AppendLine($"**Tests:** {suite.PassedCount}/{suite.TotalCount} passed");
            sb.AppendLine();

            if (suite.TestResults.Any(t => !t.Success))
            {
                sb.AppendLine("#### Failed Tests");
                sb.AppendLine();
                foreach (var failedTest in suite.TestResults.Where(t => !t.Success))
                {
                    sb.AppendLine($"- ❌ **{failedTest.Name}:** {failedTest.Description}");
                    if (failedTest.Errors.Count > 0)
                    {
                        foreach (var error in failedTest.Errors)
                        {
                            sb.AppendLine($"  - {error}");
                        }
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateRunSummaryMarkdown(TestRunResult runResult)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# nSNMP Integration Test Summary");
        sb.AppendLine();
        sb.AppendLine($"**Run:** {runResult.Name}");
        sb.AppendLine($"**Time:** {runResult.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Status:** {(runResult.Success ? "✅ ALL TESTS PASSED" : "❌ SOME TESTS FAILED")}");
        sb.AppendLine();

        // Quick Stats
        sb.AppendLine("## Quick Stats");
        sb.AppendLine();
        sb.AppendLine($"🧪 **Total Tests:** {runResult.TotalTests}");
        sb.AppendLine($"✅ **Passed:** {runResult.PassedTests}");
        sb.AppendLine($"❌ **Failed:** {runResult.FailedTests}");
        sb.AppendLine($"⏱️ **Duration:** {runResult.DurationMs / 1000.0:F1} seconds");
        sb.AppendLine($"📊 **Success Rate:** {(runResult.TotalTests > 0 ? (double)runResult.PassedTests / runResult.TotalTests * 100 : 0):F1}%");
        sb.AppendLine();

        // Suite Status
        sb.AppendLine("## Suite Status");
        sb.AppendLine();
        foreach (var suite in runResult.TestSuites.OrderBy(s => s.Name))
        {
            var icon = suite.Success ? "✅" : "❌";
            var successRate = suite.TotalCount > 0 ? (double)suite.PassedCount / suite.TotalCount * 100 : 0;
            sb.AppendLine($"- {icon} **{suite.Name}** - {suite.PassedCount}/{suite.TotalCount} tests ({successRate:F0}%)");
        }

        if (runResult.FailedTests > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Failed Tests Summary");
            sb.AppendLine();
            foreach (var suite in runResult.TestSuites.Where(s => !s.Success))
            {
                sb.AppendLine($"### {suite.Name}");
                foreach (var test in suite.TestResults.Where(t => !t.Success))
                {
                    sb.AppendLine($"- ❌ {test.Name}: {test.Description}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"*Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by nSNMP Integration Test Suite*");

        return sb.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}