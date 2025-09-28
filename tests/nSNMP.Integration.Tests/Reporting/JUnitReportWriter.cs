using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml;

namespace nSNMP.Integration.Tests.Reporting;

public class JUnitReportWriter : ITestReportWriter, IAsyncDisposable
{
    private readonly string _outputDirectory;
    private readonly ILogger<JUnitReportWriter> _logger;

    public JUnitReportWriter(string outputDirectory, ILogger<JUnitReportWriter>? logger = null)
    {
        _outputDirectory = outputDirectory;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<JUnitReportWriter>.Instance;

        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task WriteSuiteReportAsync(TestSuiteResult suiteResult, CancellationToken cancellationToken = default)
    {
        var fileName = $"TEST-{suiteResult.Name}.xml";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var xmlContent = GenerateSuiteJUnitXml(suiteResult);
        await File.WriteAllTextAsync(filePath, xmlContent, cancellationToken);

        _logger.LogInformation("JUnit suite report written to: {FilePath}", filePath);
    }

    public async Task WriteRunReportAsync(TestRunResult runResult, CancellationToken cancellationToken = default)
    {
        // Write individual suite files
        foreach (var suite in runResult.TestSuites)
        {
            await WriteSuiteReportAsync(suite, cancellationToken);
        }

        // Write combined report
        var fileName = $"TEST-{runResult.Name}-Combined.xml";
        var filePath = Path.Combine(_outputDirectory, fileName);

        var xmlContent = GenerateRunJUnitXml(runResult);
        await File.WriteAllTextAsync(filePath, xmlContent, cancellationToken);

        _logger.LogInformation("JUnit combined report written to: {FilePath}", filePath);
    }

    private string GenerateSuiteJUnitXml(TestSuiteResult suiteResult)
    {
        var doc = new XmlDocument();
        var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.AppendChild(declaration);

        // Create testsuite element
        var testSuite = doc.CreateElement("testsuite");
        testSuite.SetAttribute("name", suiteResult.Name);
        testSuite.SetAttribute("tests", suiteResult.TotalCount.ToString());
        testSuite.SetAttribute("failures", suiteResult.FailedCount.ToString());
        testSuite.SetAttribute("errors", suiteResult.Errors.Count.ToString());
        testSuite.SetAttribute("time", (suiteResult.DurationMs / 1000.0).ToString("F3"));
        testSuite.SetAttribute("timestamp", suiteResult.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        testSuite.SetAttribute("hostname", Environment.MachineName);

        // Add properties
        var properties = doc.CreateElement("properties");
        AddProperty(doc, properties, "suite.description", suiteResult.Description);
        AddProperty(doc, properties, "suite.startTime", suiteResult.StartTime.ToString("O"));
        AddProperty(doc, properties, "suite.endTime", suiteResult.EndTime?.ToString("O") ?? "");
        testSuite.AppendChild(properties);

        // Add test cases
        foreach (var test in suiteResult.TestResults)
        {
            var testCase = CreateTestCaseElement(doc, test);
            testSuite.AppendChild(testCase);
        }

        // Add suite-level errors
        if (suiteResult.Errors.Count > 0)
        {
            var systemOut = doc.CreateElement("system-out");
            var suiteErrors = string.Join("\n", suiteResult.Errors.Select(e => $"SUITE ERROR: {e}"));
            systemOut.AppendChild(doc.CreateCDataSection(suiteErrors));
            testSuite.AppendChild(systemOut);
        }

        doc.AppendChild(testSuite);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            Encoding = Encoding.UTF8
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        doc.Save(xmlWriter);
        return stringWriter.ToString();
    }

    private string GenerateRunJUnitXml(TestRunResult runResult)
    {
        var doc = new XmlDocument();
        var declaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.AppendChild(declaration);

        // Create testsuites element (root for multiple suites)
        var testSuites = doc.CreateElement("testsuites");
        testSuites.SetAttribute("name", runResult.Name);
        testSuites.SetAttribute("tests", runResult.TotalTests.ToString());
        testSuites.SetAttribute("failures", runResult.FailedTests.ToString());
        testSuites.SetAttribute("errors", runResult.Errors.Count.ToString());
        testSuites.SetAttribute("time", (runResult.DurationMs / 1000.0).ToString("F3"));
        testSuites.SetAttribute("timestamp", runResult.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"));

        // Add properties for the entire run
        var properties = doc.CreateElement("properties");
        AddProperty(doc, properties, "run.name", runResult.Name);
        AddProperty(doc, properties, "run.startTime", runResult.StartTime.ToString("O"));
        AddProperty(doc, properties, "run.endTime", runResult.EndTime?.ToString("O") ?? "");
        AddProperty(doc, properties, "environment.machineName", Environment.MachineName);
        AddProperty(doc, properties, "environment.osVersion", Environment.OSVersion.ToString());
        AddProperty(doc, properties, "environment.clrVersion", Environment.Version.ToString());

        // Add metadata as properties
        foreach (var kvp in runResult.Metadata)
        {
            AddProperty(doc, properties, $"metadata.{kvp.Key}", kvp.Value?.ToString() ?? "");
        }

        testSuites.AppendChild(properties);

        // Add each test suite
        foreach (var suite in runResult.TestSuites)
        {
            var testSuite = doc.CreateElement("testsuite");
            testSuite.SetAttribute("name", suite.Name);
            testSuite.SetAttribute("tests", suite.TotalCount.ToString());
            testSuite.SetAttribute("failures", suite.FailedCount.ToString());
            testSuite.SetAttribute("errors", suite.Errors.Count.ToString());
            testSuite.SetAttribute("time", (suite.DurationMs / 1000.0).ToString("F3"));
            testSuite.SetAttribute("timestamp", suite.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"));
            testSuite.SetAttribute("package", "nSNMP.Integration.Tests");

            // Add suite properties
            var suiteProperties = doc.CreateElement("properties");
            AddProperty(doc, suiteProperties, "suite.description", suite.Description);
            testSuite.AppendChild(suiteProperties);

            // Add test cases
            foreach (var test in suite.TestResults)
            {
                var testCase = CreateTestCaseElement(doc, test);
                testSuite.AppendChild(testCase);
            }

            // Add suite errors
            if (suite.Errors.Count > 0)
            {
                var systemErr = doc.CreateElement("system-err");
                var errors = string.Join("\n", suite.Errors);
                systemErr.AppendChild(doc.CreateCDataSection(errors));
                testSuite.AppendChild(systemErr);
            }

            testSuites.AppendChild(testSuite);
        }

        // Add run-level errors
        if (runResult.Errors.Count > 0)
        {
            var systemErr = doc.CreateElement("system-err");
            var runErrors = string.Join("\n", runResult.Errors.Select(e => $"RUN ERROR: {e}"));
            systemErr.AppendChild(doc.CreateCDataSection(runErrors));
            testSuites.AppendChild(systemErr);
        }

        doc.AppendChild(testSuites);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            Encoding = Encoding.UTF8
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        doc.Save(xmlWriter);
        return stringWriter.ToString();
    }

    private XmlElement CreateTestCaseElement(XmlDocument doc, TestResult test)
    {
        var testCase = doc.CreateElement("testcase");
        testCase.SetAttribute("name", test.Name);
        testCase.SetAttribute("classname", "nSNMP.Integration.Tests");
        testCase.SetAttribute("time", (test.DurationMs / 1000.0).ToString("F3"));

        // Add test properties
        var properties = doc.CreateElement("properties");
        AddProperty(doc, properties, "test.description", test.Description);
        AddProperty(doc, properties, "test.startTime", test.StartTime.ToString("O"));
        AddProperty(doc, properties, "test.endTime", test.EndTime?.ToString("O") ?? "");
        AddProperty(doc, properties, "test.assertions.total", test.TotalAssertions.ToString());
        AddProperty(doc, properties, "test.assertions.passed", test.PassedAssertions.ToString());
        AddProperty(doc, properties, "test.assertions.failed", test.FailedAssertions.ToString());

        // Add metrics as properties
        foreach (var metric in test.Metrics)
        {
            var metricName = $"metric.{metric.Name}";
            var metricValue = metric.Unit != null ? $"{metric.Value} {metric.Unit}" : metric.Value.ToString();
            AddProperty(doc, properties, metricName, metricValue);
        }

        testCase.AppendChild(properties);

        // If test failed, add failure element
        if (!test.Success)
        {
            var failure = doc.CreateElement("failure");
            failure.SetAttribute("type", "TestFailure");

            var failureMessage = new StringBuilder();
            failureMessage.AppendLine($"Test '{test.Name}' failed");

            // Add failed assertions
            var failedAssertions = test.Assertions.Where(a => !a.Passed).ToList();
            if (failedAssertions.Count > 0)
            {
                failureMessage.AppendLine("\nFailed Assertions:");
                foreach (var assertion in failedAssertions)
                {
                    failureMessage.AppendLine($"- {assertion.Description}");
                    if (!string.IsNullOrEmpty(assertion.Details))
                    {
                        failureMessage.AppendLine($"  Details: {assertion.Details}");
                    }
                }
            }

            // Add errors
            if (test.Errors.Count > 0)
            {
                failureMessage.AppendLine("\nErrors:");
                foreach (var error in test.Errors)
                {
                    failureMessage.AppendLine($"- {error}");
                }
            }

            failure.SetAttribute("message", failureMessage.ToString().Trim());
            failure.AppendChild(doc.CreateTextNode(failureMessage.ToString()));
            testCase.AppendChild(failure);
        }

        // Add system-out with assertions and metrics details
        var systemOut = doc.CreateElement("system-out");
        var output = new StringBuilder();

        output.AppendLine($"Test: {test.Name}");
        output.AppendLine($"Description: {test.Description}");
        output.AppendLine($"Duration: {test.DurationMs} ms");
        output.AppendLine();

        if (test.Assertions.Count > 0)
        {
            output.AppendLine("Assertions:");
            foreach (var assertion in test.Assertions)
            {
                var status = assertion.Passed ? "PASS" : "FAIL";
                output.AppendLine($"  [{status}] {assertion.Description}");
                if (!string.IsNullOrEmpty(assertion.Details))
                {
                    output.AppendLine($"         Details: {assertion.Details}");
                }
            }
            output.AppendLine();
        }

        if (test.Metrics.Count > 0)
        {
            output.AppendLine("Metrics:");
            foreach (var metric in test.Metrics)
            {
                var unit = !string.IsNullOrEmpty(metric.Unit) ? $" {metric.Unit}" : "";
                output.AppendLine($"  {metric.Name}: {metric.Value:F2}{unit}");
            }
            output.AppendLine();
        }

        systemOut.AppendChild(doc.CreateCDataSection(output.ToString()));
        testCase.AppendChild(systemOut);

        return testCase;
    }

    private void AddProperty(XmlDocument doc, XmlElement parent, string name, string value)
    {
        var property = doc.CreateElement("property");
        property.SetAttribute("name", name);
        property.SetAttribute("value", value);
        parent.AppendChild(property);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}