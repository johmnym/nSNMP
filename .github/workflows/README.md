# GitHub Actions Workflows

This directory contains automated CI/CD workflows for the nSNMP project.

## Workflows

### 🧪 Unit Tests (`unit-tests.yml`)
- **Triggers**: Push/PR to main branches, workflow_dispatch
- **Platforms**: Ubuntu, Windows, macOS
- **Framework**: .NET 9.0 with xUnit
- **Features**:
  - Cross-platform unit test execution
  - JUnit XML test result reporting
  - Code coverage with Coverlet
  - Test result artifacts
  - PR comments with coverage info

**Projects tested:**
- nSNMP.Core.Tests
- nSNMP.Abstractions.Tests
- nSNMP.SMI.Tests
- nSNMP.Extensions.Tests
- nSNMP.MIB.Tests
- nSNMP.Agent.Tests

### 🔄 Integration Tests (`integration-tests.yml`)
- **Triggers**: Push/PR to main branches, workflow_dispatch
- **Platform**: Ubuntu (with Docker)
- **Framework**: .NET 9.0 with xUnit
- **Features**:
  - Docker-based SNMP simulators
  - Real network testing scenarios
  - JUnit XML test result reporting
  - Matrix testing for different test suites

**Test categories:**
- SNMP Communication Tests
- SNMP Error Handling Tests
- SNMPv3 Security Tests
- Simple SNMP Agent Tests

## Configuration Files

### `coverlet.runsettings`
Code coverage configuration for Coverlet:
- Excludes test projects and generated code
- Focuses on source code coverage
- Cobertura format output

### Test Project Dependencies
All test projects include:
- `Microsoft.NET.Test.Sdk` - Test framework
- `xunit` - Test runner
- `JunitXml.TestLogger` - JUnit XML output
- `coverlet.collector` - Code coverage

## Usage

### Running Tests Locally

```bash
# Unit tests only
dotnet test nSNMP.sln --filter "Category!=Integration"

# Integration tests only
dotnet test tests/nSNMP.Integration.Tests/

# All tests with coverage
dotnet test nSNMP.sln --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### Manual Workflow Triggers

Both workflows support manual triggering via GitHub's "Actions" tab using the "workflow_dispatch" event.

## Artifacts

### Unit Tests
- `unit-test-results-{os}` - Test results per platform
- `coverage-{os}` - Coverage data per platform
- `coverage-report` - Combined HTML coverage report

### Integration Tests
- `integration-test-results` - Test results and logs
- `container-logs` - Docker container logs

## Best Practices

1. **Test Categorization**: Use `[Trait("Category", "Integration")]` for integration tests
2. **Cross-Platform**: Unit tests run on all platforms to catch platform-specific issues
3. **Fast Feedback**: Unit tests complete in ~15 minutes, integration tests in ~45 minutes
4. **Coverage Goals**: Aim for >80% code coverage on core libraries
5. **Test Isolation**: Each test should be independent and repeatable

## Troubleshooting

### Common Issues
- **Docker not available**: Integration tests require Docker on the runner
- **Platform differences**: Some tests may behave differently on Windows vs Linux
- **Flaky tests**: Network-dependent tests may occasionally fail due to timeouts

### Debug Tips
- Check workflow logs in GitHub Actions tab
- Download test result artifacts for detailed analysis
- Run tests locally with same .NET version to reproduce issues