using Microsoft.Extensions.Logging;
using nSNMP.Integration.Tests;
using nSNMP.Integration.Tests.Reporting;

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();

// Simple validation run without Docker dependencies
logger.LogInformation("🧪 nSNMP Integration Test Framework - Validation Mode");
logger.LogInformation("This demonstrates the test framework structure without requiring Docker");

try
{
    // Run validation tests
    await using var runner = new SimpleTestRunner();
    var result = await runner.RunValidationAsync();

    // Generate reports
    var outputDir = Path.Combine(AppContext.BaseDirectory, "TestResults");
    Directory.CreateDirectory(outputDir);

    var markdownWriter = new MarkdownReportWriter(outputDir);
    var junitWriter = new JUnitReportWriter(outputDir);

    await markdownWriter.WriteRunReportAsync(result);
    await junitWriter.WriteRunReportAsync(result);

    // Display results
    Console.WriteLine();
    Console.WriteLine("=== nSNMP Integration Test Validation Results ===");
    Console.WriteLine($"📊 Overall Status: {(result.Success ? "✅ PASSED" : "❌ FAILED")}");
    Console.WriteLine($"⏱️  Duration: {result.DurationMs / 1000.0:F1} seconds");
    Console.WriteLine($"📋 Test Suites: {result.PassedSuites}/{result.TotalSuites} passed");
    Console.WriteLine($"🧪 Total Tests: {result.PassedTests}/{result.TotalTests} passed");
    Console.WriteLine($"📈 Success Rate: {(result.TotalTests > 0 ? (double)result.PassedTests / result.TotalTests * 100 : 0):F1}%");
    Console.WriteLine();

    if (result.TestSuites.Count > 0)
    {
        Console.WriteLine("📋 Suite Details:");
        foreach (var suite in result.TestSuites)
        {
            var icon = suite.Success ? "✅" : "❌";
            var rate = suite.TotalCount > 0 ? (double)suite.PassedCount / suite.TotalCount * 100 : 0;
            Console.WriteLine($"  {icon} {suite.Name}: {suite.PassedCount}/{suite.TotalCount} tests ({rate:F0}%) - {suite.DurationMs}ms");

            if (!suite.Success && suite.TestResults.Any(t => !t.Success))
            {
                foreach (var failedTest in suite.TestResults.Where(t => !t.Success))
                {
                    Console.WriteLine($"     ❌ {failedTest.Name}: {failedTest.Errors.FirstOrDefault() ?? "Test failed"}");
                }
            }
        }
        Console.WriteLine();
    }

    Console.WriteLine($"📁 Reports generated in: {outputDir}");
    Console.WriteLine("   - TestSummary.md (human-readable summary)");
    Console.WriteLine("   - TestRun_*.md (detailed Markdown report)");
    Console.WriteLine("   - TEST-*.xml (JUnit XML for CI/CD)");
    Console.WriteLine();

    if (result.Success)
    {
        Console.WriteLine("🎉 Integration test framework validation completed successfully!");
        Console.WriteLine("The test framework is properly configured and ready for Docker-based testing.");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Set up Docker environment with ./setup-test-env.sh");
        Console.WriteLine("2. Run full integration tests with: dotnet run -- run");
        Environment.Exit(0);
    }
    else
    {
        Console.WriteLine("⚠️  Some validation tests failed. Please check the detailed reports.");
        Console.WriteLine("Fix any configuration issues before running the full integration tests.");
        Environment.Exit(1);
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Integration test validation failed");
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}