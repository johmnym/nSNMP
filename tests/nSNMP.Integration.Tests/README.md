# nSNMP Integration Test Suite

This is a comprehensive integration testing framework for the nSNMP library that uses containerized SNMP simulators to test real-world scenarios including printer devices, network impairment conditions, and trap/inform handling.

## 🎯 Overview

The integration test suite provides:

- **Real-world device simulation** using SNMPsim containers
- **Network impairment testing** with Pumba for latency, jitter, and packet loss
- **SNMPv2c and SNMPv3** protocol testing with authentication and privacy
- **Printer MIB support** with supply levels and alert conditions
- **Trap and INFORM testing** for event-driven SNMP scenarios
- **Comprehensive reporting** in both Markdown and JUnit XML formats
- **CI/CD integration** with GitHub Actions

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK
- Docker and Docker Compose
- Linux environment (for network impairment features)

### Running All Tests

```bash
cd tests.integration
dotnet run -- run
```

### Running Specific Test Suites

```bash
# Run only printer tests
dotnet run -- run-suite printer

# Run only MIB-II tests
dotnet run -- run-suite mib2

# Run only trap/inform tests
dotnet run -- run-suite traps

# Run only network impairment tests
dotnet run -- run-suite network
```

### Validate Configuration

```bash
dotnet run -- validate
```

### List Available Scenarios

```bash
dotnet run -- list
```

## 📁 Project Structure

```
tests.integration/
├── Scenarios/                    # Test scenario definitions
│   ├── scenarios.yml            # Main device configurations
│   ├── v3-users.yml             # SNMPv3 user credentials
│   ├── mib2-basic.snmprec       # Basic MIB-II data
│   ├── printers-mono.snmprec    # Monochrome printer data
│   ├── printers-color.snmprec   # Color printer with alerts
│   └── oversized.snmprec        # Large response data
├── Containers/                   # Container orchestration
│   ├── SnmpSimContainer.cs      # SNMP simulator container
│   ├── NetSnmpToolsContainer.cs # Trap sending tools
│   └── PumbaNetemContainer.cs   # Network impairment
├── Configuration/                # Configuration models
│   ├── ScenarioConfig.cs        # YAML configuration models
│   └── ScenarioLoader.cs        # Configuration loader
├── Infrastructure/              # Test infrastructure
│   └── IntegrationTestFixture.cs # Main test coordinator
├── Suites/                      # Test suite implementations
│   ├── PrinterSuite.cs          # Printer MIB tests
│   ├── Mib2Suite.cs             # Standard MIB-II tests
│   ├── TrapsInformsSuite.cs     # Trap/inform tests
│   └── AdverseNetSuite.cs       # Network impairment tests
├── Reporting/                   # Report generation
│   ├── TestModels.cs            # Test result models
│   ├── TestReporter.cs          # Report coordinator
│   ├── MarkdownReportWriter.cs  # Markdown reports
│   └── JUnitReportWriter.cs     # JUnit XML reports
├── TestRunner.cs                # Main test runner
└── Program.cs                   # CLI entry point
```

## 🧪 Test Scenarios

### 1. Printer MIB Tests (`PrinterSuite`)

Tests real printer scenarios with supply monitoring and alerts:

- **Monochrome Printer**: HP LaserJet with black toner (SNMPv2c)
- **Color Printer**: Canon imageRUNNER with CMYK supplies (SNMPv3 authPriv)
- **Supply Levels**: Toner percentages, waste container status
- **Alert Conditions**: Low toner, paper jams, cover open
- **Input/Output Trays**: Paper levels and capacity

### 2. MIB-II Tests (`Mib2Suite`)

Standard SNMP MIB-II object testing:

- **System Group**: sysDescr, sysContact, sysName, etc.
- **Interface Table**: Multiple interfaces with statistics
- **IP Address Table**: Network configuration
- **SNMP Operations**: GET, GETNEXT, GETBULK, WALK

### 3. Trap/Inform Tests (`TrapsInformsSuite`)

Event-driven SNMP testing:

- **SNMPv2c Traps**: Community-based trap sending
- **SNMPv3 INFORMs**: Authenticated and encrypted notifications
- **Bulk Trap Testing**: High-volume trap handling
- **Trap Filtering**: Source IP and community filtering

### 4. Network Impairment Tests (`AdverseNetSuite`)

SNMP behavior under adverse network conditions:

- **High Latency**: 200ms delay + 50ms jitter
- **Packet Loss**: 5% random packet drops
- **Combined Conditions**: Latency + jitter + loss
- **Timeout Behavior**: Adaptive timeout testing
- **Large Responses**: tooBig error handling

## 🐳 Container Images

The test suite uses the following container images:

- **snmpsim**: SNMP agent simulator
- **net-snmp-tools**: Trap/inform sending tools
- **gaiaadm/pumba**: Network impairment injection

## 📊 Test Reporting

### Markdown Reports

Human-readable test reports with:
- Executive summary with pass/fail rates
- Detailed test results with assertions
- Performance metrics and measurements
- Error details and debugging information

### JUnit XML Reports

CI/CD compatible reports with:
- Individual test case results
- Test suite grouping
- Timing information
- Properties and metadata
- Error and failure details

### Example Output Directory

```
TestResults/
├── TestSummary.md               # Executive summary
├── TestRun_nSNMP-Integration-Tests_20241127_143022.md
├── PrinterSuite_20241127_143022.md
├── TEST-PrinterSuite.xml
├── TEST-Mib2Suite.xml
├── TEST-TrapsInformsSuite.xml
├── TEST-AdverseNetSuite.xml
├── TEST-nSNMP-Integration-Tests-Combined.xml
└── container-logs.txt
```

## ⚙️ Configuration

### Environment Variables

```bash
# Test suite selection
INTEGRATION_RUN_PRINTER_TESTS=true
INTEGRATION_RUN_MIB2_TESTS=true
INTEGRATION_RUN_TRAP_TESTS=true
INTEGRATION_RUN_NETWORK_TESTS=true

# Directories
INTEGRATION_SCENARIO_DIRECTORY=/path/to/scenarios
INTEGRATION_OUTPUT_DIRECTORY=/path/to/results
```

### Command Line Options

```bash
dotnet run -- run [OPTIONS]

Options:
  --scenario-dir <path>   Directory containing test scenarios
  --output-dir <path>     Directory for test results
  --skip-printer         Skip printer MIB tests
  --skip-mib2            Skip MIB-II tests
  --skip-traps           Skip trap/inform tests
  --skip-network         Skip network impairment tests
  --no-markdown          Disable Markdown reports
  --no-junit             Disable JUnit XML reports
  --verbose              Enable verbose logging
```

## 🔧 Development

### Adding New Test Scenarios

1. **Create SNMP Data**: Add `.snmprec` files to `Scenarios/`
2. **Update Configuration**: Modify `scenarios.yml`
3. **Implement Tests**: Create test methods in appropriate suite
4. **Add Assertions**: Use `test.AddAssertion()` for validation
5. **Capture Metrics**: Use `test.AddMetric()` for measurements

### Example Test Method

```csharp
private async Task<TestResult> TestCustomDeviceAsync(CancellationToken cancellationToken)
{
    var test = new TestResult("CustomDevice", "Test custom device functionality");

    try
    {
        var container = await _fixture.GetOrCreateDeviceAsync("custom-device", cancellationToken);
        var endpoint = container.GetConnectionEndpoint();
        var parts = endpoint.Split(':');
        var ip = IPAddress.Parse(parts[0]);
        var port = int.Parse(parts[1]);

        using var client = new SnmpClient(ip, port);

        // Perform SNMP operations
        var result = await client.GetAsync("1.3.6.1.2.1.1.1.0", cancellationToken);

        // Add assertions
        test.AddAssertion("System description retrieved", !string.IsNullOrEmpty(result?.Value?.ToString()));

        // Add metrics
        test.AddMetric("ResponseTimeMs", responseTime);

        test.SetSuccess(true);
    }
    catch (Exception ex)
    {
        test.AddError($"Test failed: {ex.Message}");
    }

    test.Complete();
    return test;
}
```

## 🚀 CI/CD Integration

### GitHub Actions

The included workflow (`.github/workflows/integration-tests.yml`) provides:

- **Automated testing** on push/PR
- **Matrix testing** for individual suites
- **Artifact uploads** for test results and logs
- **PR comments** with test summaries
- **Docker cleanup** to prevent resource exhaustion

### Manual Workflow Triggers

Use the GitHub Actions UI to run tests with custom parameters:
- Select which test suites to run
- Override default configurations
- Run matrix tests in parallel

## 📋 Test Results Interpretation

### Success Criteria

- ✅ **All assertions pass** in each test
- ✅ **No unhandled exceptions** during execution
- ✅ **Containers start successfully** and remain healthy
- ✅ **Network operations complete** within timeout limits

### Common Failure Scenarios

- ❌ **Container startup failures**: Docker/networking issues
- ❌ **SNMP timeouts**: Network impairment too aggressive
- ❌ **Authentication failures**: SNMPv3 credential mismatches
- ❌ **Data validation errors**: Unexpected SNMP response values

### Performance Metrics

Key metrics tracked across tests:
- **Response times** for GET/WALK operations
- **Success rates** under network impairment
- **Throughput** for bulk operations
- **Resource utilization** (container memory/CPU)

## 🔍 Troubleshooting

### Docker Issues

```bash
# Check Docker daemon
sudo systemctl status docker

# Verify image availability
docker images | grep -E '(snmpsim|pumba|net-snmp)'

# Check network connectivity
docker network ls
```

### Test Failures

1. **Review detailed logs** in test output directory
2. **Check container logs** in `container-logs.txt`
3. **Validate scenarios** with `dotnet run -- validate`
4. **Run single suite** to isolate issues

### Network Impairment Issues

- Ensure running on **Linux** (required for tc/netem)
- Check **Docker privileges** for Pumba container
- Verify **network interfaces** are accessible

## 📚 References

- [SNMPsim Documentation](http://snmpsim.sourceforge.net/)
- [Pumba Network Emulation](https://github.com/alexei-led/pumba)
- [Printer MIB RFC 3805](https://tools.ietf.org/rfc/rfc3805.txt)
- [SNMP v3 Architecture RFC 3411](https://tools.ietf.org/rfc/rfc3411.txt)

## 🤝 Contributing

1. **Fork the repository**
2. **Create feature branch**: `git checkout -b feature/new-test-suite`
3. **Add tests and documentation**
4. **Verify all tests pass**: `dotnet run -- run`
5. **Submit pull request**

---

*This integration test suite provides comprehensive validation of nSNMP library functionality across realistic network conditions and device scenarios.*