using System.Diagnostics;

namespace nSNMP.Integration.Tests.Reporting;

public class TestSuiteResult
{
    private readonly Stopwatch _stopwatch;

    public string Name { get; }
    public string Description { get; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public long DurationMs => _stopwatch.ElapsedMilliseconds;
    public bool Success => TestResults.All(t => t.Success) && Errors.Count == 0;
    public List<TestResult> TestResults { get; } = new();
    public List<string> Errors { get; } = new();

    public TestSuiteResult(string name, string description)
    {
        Name = name;
        Description = description;
        StartTime = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
    }

    public void AddTestResult(TestResult testResult)
    {
        TestResults.Add(testResult);
    }

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public void Complete()
    {
        _stopwatch.Stop();
        EndTime = DateTime.UtcNow;
    }

    public int PassedCount => TestResults.Count(t => t.Success);
    public int FailedCount => TestResults.Count(t => !t.Success);
    public int TotalCount => TestResults.Count;
}

public class TestResult
{
    private readonly Stopwatch _stopwatch;

    public string Name { get; }
    public string Description { get; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public long DurationMs => _stopwatch.ElapsedMilliseconds;
    public bool Success { get; private set; }
    public List<TestAssertion> Assertions { get; } = new();
    public List<TestMetric> Metrics { get; } = new();
    public List<string> Errors { get; } = new();

    public TestResult(string name, string description)
    {
        Name = name;
        Description = description;
        StartTime = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
    }

    public void AddAssertion(string description, bool passed, string? details = null)
    {
        Assertions.Add(new TestAssertion(description, passed, details));
    }

    public void AddMetric(string name, double value, string? unit = null)
    {
        Metrics.Add(new TestMetric(name, value, unit));
    }

    public void AddError(string error)
    {
        Errors.Add(error);
        Success = false;
    }

    public void SetSuccess(bool success)
    {
        Success = success && Assertions.All(a => a.Passed) && Errors.Count == 0;
    }

    public void Complete()
    {
        _stopwatch.Stop();
        EndTime = DateTime.UtcNow;

        // If success wasn't explicitly set, determine from assertions and errors
        if (!Success && Errors.Count == 0)
        {
            Success = Assertions.Count > 0 && Assertions.All(a => a.Passed);
        }
    }

    public int PassedAssertions => Assertions.Count(a => a.Passed);
    public int FailedAssertions => Assertions.Count(a => !a.Passed);
    public int TotalAssertions => Assertions.Count;
}

public record TestAssertion(
    string Description,
    bool Passed,
    string? Details = null,
    DateTime Timestamp = default)
{
    public DateTime Timestamp { get; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}

public record TestMetric(
    string Name,
    double Value,
    string? Unit = null,
    DateTime Timestamp = default)
{
    public DateTime Timestamp { get; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}

public class TestRunResult
{
    private readonly Stopwatch _stopwatch;

    public string Name { get; }
    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public long DurationMs => _stopwatch.ElapsedMilliseconds;
    public bool Success => TestSuites.All(s => s.Success) && Errors.Count == 0;
    public List<TestSuiteResult> TestSuites { get; } = new();
    public List<string> Errors { get; } = new();
    public Dictionary<string, object> Metadata { get; } = new();

    public TestRunResult(string name)
    {
        Name = name;
        StartTime = DateTime.UtcNow;
        _stopwatch = Stopwatch.StartNew();
    }

    public void AddTestSuite(TestSuiteResult testSuite)
    {
        TestSuites.Add(testSuite);
    }

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public void Complete()
    {
        _stopwatch.Stop();
        EndTime = DateTime.UtcNow;
    }

    public int TotalTests => TestSuites.Sum(s => s.TotalCount);
    public int PassedTests => TestSuites.Sum(s => s.PassedCount);
    public int FailedTests => TestSuites.Sum(s => s.FailedCount);
    public int TotalSuites => TestSuites.Count;
    public int PassedSuites => TestSuites.Count(s => s.Success);
    public int FailedSuites => TestSuites.Count(s => !s.Success);
}