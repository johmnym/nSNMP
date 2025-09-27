# nSNMP Interoperability Testing Framework

This project provides comprehensive interoperability testing for nSNMP against various SNMP implementations to ensure protocol compliance and real-world compatibility.

## Overview

The interop tests validate nSNMP behavior against:

1. **Net-SNMP** (Linux/Unix standard implementation)
2. **Windows SNMP Service** (Microsoft implementation)
3. **Cisco IOS SNMP** (Network equipment)
4. **Ubuntu snmpd** (Common Linux distribution)

## Test Categories

### Protocol Compliance Tests
- **SNMPv1/v2c/v3 Message Parsing** - Ensure messages parse correctly
- **BER/DER Encoding Compatibility** - Test ASN.1 encoding/decoding
- **OID Format Compatibility** - Verify OID string/binary formats
- **Error Response Handling** - Test error condition responses

### Security Interoperability
- **USM Authentication** - MD5, SHA1, SHA-256, SHA-384, SHA-512
- **USM Privacy** - DES, AES-128, AES-192, AES-256
- **Key Localization** - RFC 3414 key derivation compatibility
- **Time Window Validation** - Engine time synchronization

### Transport Layer Tests
- **UDP Transport** - Standard SNMP over UDP
- **IPv4 and IPv6** - Dual-stack support validation
- **Message Size Limits** - Large message handling
- **Timeout and Retry** - Network resilience testing

### MIB Compatibility
- **Standard MIB-2** - System, interfaces, IP groups
- **Enterprise MIBs** - Vendor-specific MIB support
- **Complex Data Types** - Tables, sequences, choice types
- **SNMP Operations** - GET, GETNEXT, GETBULK, SET, TRAP

## Docker-Based Testing

Tests use Testcontainers to spin up real SNMP implementations:

```bash
# Run all interoperability tests
dotnet test

# Run specific test category
dotnet test --filter Category=NetSnmpInterop
dotnet test --filter Category=SecurityInterop
dotnet test --filter Category=TransportInterop
```

### Container Images Used

- **Net-SNMP**: `quay.io/netsnmp/netsnmp:latest`
- **Ubuntu SNMP**: `ubuntu:22.04` with snmpd installed
- **Windows SNMP**: Windows containers (requires Windows host)

## Test Structure

```
InteropTests/
├── NetSnmpInteropTests.cs      # Tests against Net-SNMP
├── WindowsSnmpInteropTests.cs  # Tests against Windows SNMP
├── SecurityInteropTests.cs     # Cross-implementation security tests
├── TransportInteropTests.cs    # Network transport compatibility
├── MibInteropTests.cs          # MIB compatibility tests
├── Containers/                 # Docker configuration
│   ├── netsnmp/               # Net-SNMP container setup
│   ├── ubuntu-snmpd/          # Ubuntu snmpd setup
│   └── scripts/               # Test scripts and configs
└── TestData/                  # Reference test data
    ├── packets/               # Captured SNMP packets
    ├── mibs/                  # Test MIB files
    └── configs/               # SNMP daemon configurations
```

## Test Scenarios

### Basic Protocol Tests
1. **Simple GET Request** - Test basic SNMP GET operation
2. **GETNEXT Walking** - Test MIB tree traversal
3. **GETBULK Operations** - Test bulk data retrieval
4. **SET Operations** - Test write operations
5. **TRAP Generation** - Test trap/notification sending

### Security Tests
1. **Authentication Only** - authNoPriv security level
2. **Privacy Encryption** - authPriv with various algorithms
3. **User Management** - Adding/removing USM users
4. **Key Rollover** - Testing key change procedures
5. **Time Synchronization** - Engine time window validation

### Error Handling Tests
1. **Invalid Community** - Test access control
2. **Unknown OIDs** - Test noSuchObject responses
3. **Wrong Types** - Test wrongType error responses
4. **Access Violations** - Test notWritable responses
5. **Timeout Scenarios** - Test request timeout handling

### Performance Tests
1. **Large Response Handling** - Test message size limits
2. **Concurrent Requests** - Test multiple simultaneous operations
3. **Memory Usage** - Monitor memory consumption patterns
4. **Connection Limits** - Test agent connection handling

## Configuration

### Environment Variables

```bash
# Test configuration
INTEROP_TEST_TIMEOUT=30000      # Test timeout in milliseconds
INTEROP_CONTAINER_STARTUP=60    # Container startup timeout
INTEROP_SKIP_WINDOWS=false      # Skip Windows-specific tests
INTEROP_VERBOSE_LOGGING=false   # Enable detailed logging

# Container configuration
NETSNMP_IMAGE=quay.io/netsnmp/netsnmp:latest
UBUNTU_IMAGE=ubuntu:22.04
```

### Test Data

Tests include pre-captured packets from various SNMP implementations to ensure parsing compatibility:

- **Wireshark Captures** - Real-world SNMP traffic
- **Protocol Conformance** - RFC compliance test vectors
- **Edge Cases** - Boundary conditions and malformed packets
- **Vendor Specific** - Enterprise-specific message formats

## Running Tests

### Prerequisites

- Docker Engine (for container-based tests)
- .NET 9.0 SDK
- Network access for container downloads

### Local Development

```bash
# Build interop tests
dotnet build nSNMP.InteropTests

# Run quick interop tests (no containers)
dotnet test nSNMP.InteropTests --filter Category=Quick

# Run full interop test suite
dotnet test nSNMP.InteropTests

# Generate detailed test report
dotnet test nSNMP.InteropTests --logger "trx;LogFileName=interop-results.trx"
```

### CI/CD Integration

```yaml
# Example GitHub Actions workflow
name: Interoperability Tests
on: [push, pull_request]

jobs:
  interop-tests:
    runs-on: ubuntu-latest
    services:
      docker:
        image: docker:latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Run Interoperability Tests
        run: dotnet test nSNMP.InteropTests --verbosity normal
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: interop-test-results
          path: '**/TestResults/*.trx'
```

## Expected Results

### Baseline Compatibility

All tests should pass against these baseline implementations:

1. **Net-SNMP 5.9+** - 100% compatibility expected
2. **Windows SNMP Service** - 95% compatibility (some vendor extensions)
3. **Ubuntu 22.04 snmpd** - 100% compatibility expected

### Known Limitations

1. **Vendor Extensions** - Some proprietary MIB extensions may not be supported
2. **Legacy Protocols** - SNMPv1 deprecated features may have limited support
3. **Platform Specific** - Some tests require specific OS features

## Troubleshooting

### Common Issues

1. **Container Startup Failures**
   - Check Docker daemon is running
   - Verify network connectivity for image pulls
   - Increase startup timeout for slow systems

2. **Test Timeouts**
   - Increase `INTEROP_TEST_TIMEOUT` environment variable
   - Check system resources and load
   - Verify container health status

3. **Network Connectivity**
   - Ensure UDP port 161/162 are available
   - Check firewall rules for container networking
   - Verify localhost binding permissions

### Debug Mode

Enable verbose logging for detailed troubleshooting:

```bash
export INTEROP_VERBOSE_LOGGING=true
dotnet test nSNMP.InteropTests --logger "console;verbosity=detailed"
```

This will output:
- Container startup logs
- SNMP message dumps
- Network packet traces
- Timing information
- Error stack traces

## Contributing

When adding new interoperability tests:

1. **Document Test Purpose** - Clearly explain what compatibility is being tested
2. **Use Standard Test Data** - Leverage existing test vectors when possible
3. **Handle Timeouts Gracefully** - Account for network and container variability
4. **Test Cleanup** - Ensure containers are properly disposed
5. **Cross-Platform** - Consider Windows/Linux/macOS compatibility