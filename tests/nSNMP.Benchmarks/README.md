# nSNMP Benchmarks

Performance benchmarking suite for the nSNMP library using BenchmarkDotNet.

## Running Benchmarks

### Run All Benchmarks
```bash
dotnet run -c Release
```

### Run Specific Benchmark Categories
```bash
# Codec performance (targeting 100k varbinds/s)
dotnet run -c Release -- --filter "*CodecBenchmarks*"

# Security/USM performance
dotnet run -c Release -- --filter "*SecurityBenchmarks*"

# Network transport performance
dotnet run -c Release -- --filter "*NetworkBenchmarks*"
```

### Run Individual Benchmarks
```bash
# Critical path: bulk varbind encoding
dotnet run -c Release -- --filter "*BulkVarBindEncoding*"

# Memory allocation analysis
dotnet run -c Release -- --filter "*MemoryAllocationTest*"
```

## Performance Targets

### Codec Operations
- **Target**: 100,000 varbinds/s encode/decode
- **Latency**: P95 under 10µs for basic operations
- **Memory**: Minimal GC pressure, prefer stack allocation

### Security Operations
- **USM Processing**: <1ms for typical message sizes
- **Encryption/Decryption**: Competitive with .NET crypto primitives
- **Key Localization**: Reasonable for infrequent operations

### Network Operations
- **Message Processing**: <100µs for typical SNMP messages
- **Buffer Management**: Zero-copy where possible
- **Fragmentation**: Efficient handling of large messages

## Benchmark Categories

### CodecBenchmarks
Tests core BER encoding/decoding performance:
- OID parsing and encoding
- Integer, OctetString creation
- VarBind construction
- Sequence encoding
- PDU encoding (GetRequest, GetResponse)
- Bulk operations (100 varbinds)
- Memory allocation patterns

### SecurityBenchmarks
Tests USM security operations:
- Key localization (MD5, SHA1, SHA256)
- Authentication (digest calculation)
- Privacy encryption/decryption (DES, AES128, AES256)
- USM parameter creation
- Cryptographic random number generation
- Key stretching operations

### NetworkBenchmarks
Tests transport and message processing:
- Mock network operations (various message sizes)
- Message size analysis
- Packet fragmentation simulation
- Buffer copying (Array.Copy vs Span)
- Memory operations

## Analysis

### Memory Diagnostics
All benchmarks include memory allocation tracking to identify:
- GC pressure hotspots
- Unnecessary allocations
- Opportunities for object pooling
- Stack vs heap allocation patterns

### Performance Profiling
Results show:
- Operations per second
- Memory allocated per operation
- P95/P99 latency distributions
- Min/Max/Mean execution times

## Optimization Opportunities

Based on benchmark results, focus optimization efforts on:
1. High-frequency codec operations
2. Memory allocation reduction
3. USM security processing
4. Buffer management improvements

Run benchmarks regularly to ensure performance regressions are caught early.