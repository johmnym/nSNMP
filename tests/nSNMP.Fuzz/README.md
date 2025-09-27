# nSNMP Security Fuzzing Harness

This project provides security-focused fuzzing capabilities for the nSNMP library using [SharpFuzz](https://github.com/Metalnem/sharpfuzz).

## Overview

The fuzzing harness targets the most critical attack surfaces in SNMP parsing:

1. **BER/DER Decoder** (`ber-decoder`) - Primary target for fuzzing
   - ObjectIdentifier parsing (critical for all SNMP operations)
   - Integer parsing (used in message headers and values)
   - OctetString parsing (community strings, values)
   - Counter types (Counter32, Gauge32, TimeTicks, etc.)
   - Length encoding edge cases

2. **OID Parser** (`oid-parser`) - String-based OID parsing
   - Tests various string encodings (UTF-8, ASCII, Latin1)
   - Edge cases in OID format parsing
   - Memory allocation patterns

## Usage

### Prerequisites

- .NET 9.0 SDK
- American Fuzzy Lop (AFL++) for coverage-guided fuzzing
- SharpFuzz instrumentation

### Running Fuzzers

```bash
# Build the fuzzing project
dotnet build nSNMP.Fuzz

# Run BER decoder fuzzing (primary target)
dotnet run -- ber-decoder

# Run OID parser fuzzing
dotnet run -- oid-parser
```

### Integration with AFL++

For production fuzzing campaigns:

1. Install AFL++:
   ```bash
   # On Ubuntu/Debian
   sudo apt install afl++

   # On macOS
   brew install afl-fuzz
   ```

2. Instrument the target with SharpFuzz:
   ```bash
   # This would typically be done in CI/CD
   sharpfuzz nSNMP.SMI.dll
   ```

3. Run fuzzing campaign:
   ```bash
   # Create input/output directories
   mkdir -p fuzz_inputs fuzz_outputs

   # Add seed inputs (valid SNMP packets)
   echo "1.3.6.1.2.1.1.1.0" > fuzz_inputs/oid_seed

   # Run AFL fuzzer
   afl-fuzz -i fuzz_inputs -o fuzz_outputs -- dotnet nSNMP.Fuzz.dll ber-decoder
   ```

## Security Focus

### What We're Testing For

1. **Memory Safety Issues**
   - Buffer overflows/underflows
   - Out-of-bounds array access
   - Null pointer dereferences
   - Use-after-free conditions

2. **Input Validation Vulnerabilities**
   - Malformed BER/DER structures
   - Invalid length encodings
   - Crafted OID values
   - Boundary condition attacks

3. **Denial of Service**
   - Infinite loops in parsing
   - Excessive memory allocation
   - Stack overflow from deep recursion
   - Algorithmic complexity attacks

4. **Logic Errors**
   - Incorrect state transitions
   - Type confusion
   - Integer overflow/underflow
   - Encoding/decoding mismatch

### Expected vs Unexpected Exceptions

The fuzzer distinguishes between expected exceptions (proper error handling) and unexpected crashes:

**Expected (Good Error Handling):**
- `ArgumentException`
- `FormatException`
- `InvalidOperationException`
- `ArgumentOutOfRangeException`

**Unexpected (Potential Security Issues):**
- `AccessViolationException`
- `StackOverflowException`
- `OutOfMemoryException`
- Infinite loops (detected by timeout)
- Silent corruption

## Corpus and Seeds

### Initial Seed Files

The fuzzer should be seeded with:

1. **Valid SNMP packets** (various versions)
2. **Known problematic inputs** from previous testing
3. **Edge case structures**:
   - Maximum length encodings
   - Minimum valid structures
   - Common OID patterns
   - Various data type combinations

### Corpus Maintenance

- Regularly minimize corpus to remove redundant test cases
- Add interesting inputs discovered during fuzzing
- Include regression test cases for fixed vulnerabilities

## Continuous Integration

Recommended CI/CD integration:

```yaml
# Example GitHub Actions workflow
name: Security Fuzzing
on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2 AM

jobs:
  fuzz:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Install AFL++
        run: sudo apt install afl++
      - name: Build fuzzer
        run: dotnet build nSNMP.Fuzz
      - name: Run fuzzing session
        run: |
          timeout 3600 afl-fuzz -i seeds -o findings \
            -- dotnet nSNMP.Fuzz.dll ber-decoder || true
      - name: Archive findings
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: fuzz-findings
          path: findings/
```

## Analysis and Reporting

### Crash Analysis

When the fuzzer finds crashes:

1. **Reproduce locally** with the crashing input
2. **Minimize test case** to smallest reproducing input
3. **Classify vulnerability** (memory corruption, DoS, etc.)
4. **Create unit test** for regression testing
5. **Fix root cause** in the library

### Performance Monitoring

Monitor for:
- Fuzzing throughput (executions per second)
- Code coverage metrics
- Memory usage patterns
- Discovery of new crash buckets

## Limitations

1. **API Coverage**: Currently focuses on BER parsing and OID handling
2. **State Space**: Limited to single-operation fuzzing (not protocol flows)
3. **Environment**: Primarily tests parsing, not network/crypto operations

## Future Enhancements

- [ ] Add USM security parameter fuzzing
- [ ] Include SNMP message composition fuzzing
- [ ] Test multi-step protocol interactions
- [ ] Add property-based testing integration
- [ ] Implement structured fuzzing for complex ASN.1 structures