using Microsoft.Extensions.Logging;

namespace nSNMP.Integration.Tests.Reporting;

public class TestReporter : IAsyncDisposable
{
    private readonly string _suiteName;
    private readonly ILogger _logger;
    private readonly List<ITestReportWriter> _writers = new();

    public TestReporter(string suiteName, ILogger? logger = null)
    {
        _suiteName = suiteName;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public void AddWriter(ITestReportWriter writer)
    {
        _writers.Add(writer);
    }

    public async Task WriteReportAsync(TestSuiteResult suiteResult, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Writing test report for suite: {SuiteName}", _suiteName);

        var tasks = _writers.Select(writer => WriteWithWriterAsync(writer, suiteResult, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("Test report written for suite: {SuiteName}", _suiteName);
    }

    public async Task WriteReportAsync(TestRunResult runResult, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Writing test run report: {RunName}", runResult.Name);

        var tasks = _writers.Select(writer => WriteWithWriterAsync(writer, runResult, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("Test run report written: {RunName}", runResult.Name);
    }

    private async Task WriteWithWriterAsync(ITestReportWriter writer, TestSuiteResult suiteResult, CancellationToken cancellationToken)
    {
        try
        {
            await writer.WriteSuiteReportAsync(suiteResult, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write suite report with writer: {WriterType}", writer.GetType().Name);
        }
    }

    private async Task WriteWithWriterAsync(ITestReportWriter writer, TestRunResult runResult, CancellationToken cancellationToken)
    {
        try
        {
            await writer.WriteRunReportAsync(runResult, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write run report with writer: {WriterType}", writer.GetType().Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeTasks = _writers.OfType<IAsyncDisposable>().Select(w => w.DisposeAsync().AsTask());
        await Task.WhenAll(disposeTasks);

        foreach (var writer in _writers.OfType<IDisposable>())
        {
            writer.Dispose();
        }

        _writers.Clear();
    }
}

public interface ITestReportWriter
{
    Task WriteSuiteReportAsync(TestSuiteResult suiteResult, CancellationToken cancellationToken = default);
    Task WriteRunReportAsync(TestRunResult runResult, CancellationToken cancellationToken = default);
}