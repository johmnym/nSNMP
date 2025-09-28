# nSNMP - Modern .NET SNMP Library

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/johmnym/nSNMP)

A modern, high-performance SNMP (Simple Network Management Protocol) library for .NET 9.0, designed with clean architecture principles and comprehensive protocol support.

## 🚀 Features

### Protocol Support
- **SNMPv1** - Full support for basic SNMP operations
- **SNMPv2c** - Community-based authentication with enhanced PDUs
- **SNMPv3** - Advanced security with authentication and privacy (USM)
  - Authentication protocols: MD5, SHA1, SHA256, SHA384, SHA512
  - Privacy protocols: DES, AES128, AES192, AES256

### Core Capabilities
- ✅ **SNMP Operations**: GET, GET-NEXT, GET-BULK, SET, WALK
- ✅ **SNMP Agent**: Build custom SNMP agents with scalar and table providers
- ✅ **Trap Support**: Send and receive SNMP traps
- ✅ **MIB Support**: Parse and manage MIB files with symbolic OID resolution
- ✅ **Async/Await**: Fully asynchronous operations with cancellation support
- ✅ **Performance**: Optimized data structures and minimal allocations
- ✅ **Extensibility**: Clean abstractions and dependency injection support

## 📦 Project Structure

```
nSNMP/
├── src/
│   ├── nSNMP.Abstractions/     # Core interfaces and contracts
│   ├── nSNMP.Core/             # Main SNMP implementation
│   ├── nSNMP.SMI/              # Structure of Management Information
│   ├── nSNMP.MIB/              # MIB parsing and management
│   └── nSNMP.Extensions/       # Fluent API and extensions
├── tests/
│   ├── nSNMP.Core.Tests/       # Unit tests
│   ├── nSNMP.Integration.Tests/# Integration tests
│   ├── nSNMP.Benchmarks/       # Performance benchmarks
│   └── nSNMP.Fuzz/             # Fuzzing tests
└── samples/
    ├── SimpleSnmpGet/           # Basic SNMP GET example
    ├── SnmpScout/              # Network discovery tool
    └── SnmpTrapReceiver/       # Trap receiver service
```

## 🔧 Installation

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022, JetBrains Rider, or VS Code

### Package Installation
```bash
# Coming soon to NuGet
dotnet add package nSNMP
```

### Building from Source
```bash
git clone https://github.com/johmnym/nSNMP.git
cd nSNMP
dotnet build
dotnet test
```

## 📖 Quick Start

### Basic SNMP GET Operation

```csharp
using nSNMP.Manager;
using System.Net;

// Create SNMP client
var endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 161);
using var client = new SnmpClient(endpoint, SnmpVersion.V2c, "public");

// Perform GET operation
var result = await client.GetAsync("1.3.6.1.2.1.1.1.0"); // sysDescr
Console.WriteLine($"System Description: {result[0].Value}");
```

### Fluent API (Extensions)

```csharp
using nSNMP.Extensions;

var client = SnmpClient.Create()
    .Target("192.168.1.1", 161)
    .Version(SnmpVersion.V2c)
    .Community("public")
    .Timeout(TimeSpan.FromSeconds(5))
    .Build();

// Fluent GET operation
var results = await client
    .Get("1.3.6.1.2.1.1.1.0", "1.3.6.1.2.1.1.3.0")
    .ExecuteAsync();

// Table walk
var interfaces = await client
    .Walk("1.3.6.1.2.1.2.2.1")
    .ToListAsync();
```

### SNMPv3 Secure Operations

```csharp
using nSNMP.Manager;
using nSNMP.Security;

// Create SNMPv3 credentials
var credentials = V3Credentials.AuthPriv(
    userName: "admin",
    authProtocol: AuthProtocol.SHA256,
    authPassword: "auth_password123",
    privProtocol: PrivProtocol.AES256,
    privPassword: "priv_password456"
);

// Create SNMPv3 client
using var client = new SnmpClientV3(endpoint, credentials);

// Discover engine parameters
await client.DiscoverEngineAsync();

// Perform secure GET
var result = await client.GetAsync("1.3.6.1.2.1.1.1.0");
```

### Creating an SNMP Agent

```csharp
using nSNMP.Agent;

// Create SNMP agent
var agent = new SnmpAgentHost("public", "private");

// Register scalar values
agent.MapScalar("1.3.6.1.2.1.1.1.0",
    OctetString.Create("My SNMP Agent v1.0"));
agent.MapScalar("1.3.6.1.2.1.1.3.0",
    TimeTicks.Create(123456));

// Register table provider
agent.RegisterTableProvider(
    ObjectIdentifier.Create("1.3.6.1.2.1.2.2.1"),
    new InterfaceTableProvider());

// Start the agent
await agent.StartAsync(port: 161);
```

### MIB Management

```csharp
using nSNMP.MIB;

// Load MIB files
var mibManager = new MibManager();
mibManager.LoadMibFile("RFC1213-MIB.mib");
mibManager.LoadMibDirectory("/usr/share/snmp/mibs");

// Resolve OIDs
var oid = mibManager.NameToOid("sysDescr");        // Returns 1.3.6.1.2.1.1.1
var name = mibManager.OidToName("1.3.6.1.2.1.1.1"); // Returns "system.sysDescr"

// Get MIB object details
var obj = mibManager.GetObject("sysDescr");
Console.WriteLine($"OID: {obj.Oid}");
Console.WriteLine($"Type: {obj.Syntax}");
Console.WriteLine($"Access: {obj.Access}");
```

## 🛠️ Advanced Features

### Circuit Breaker Pattern
```csharp
var client = SnmpClient.Create()
    .Target("192.168.1.1")
    .WithCircuitBreaker(
        failureThreshold: 5,
        recoveryTimeout: TimeSpan.FromSeconds(30))
    .Build();
```

### Retry Policies
```csharp
var client = SnmpClient.Create()
    .Target("192.168.1.1")
    .WithRetryPolicy(
        maxRetries: 3,
        backoffMultiplier: 2.0)
    .Build();
```

### Bulk Operations
```csharp
// Efficient bulk retrieval
var results = await client.GetBulkAsync(
    nonRepeaters: 2,
    maxRepetitions: 10,
    oids: new[] { "1.3.6.1.2.1.1", "1.3.6.1.2.1.2" }
);
```

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/nSNMP.Core.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run integration tests
dotnet test tests/nSNMP.Integration.Tests

# Run benchmarks
dotnet run -c Release --project tests/nSNMP.Benchmarks
```

### Test Categories
- **Unit Tests**: Fast, isolated tests for individual components
- **Integration Tests**: Tests with real SNMP agents (using Docker)
- **Fuzz Tests**: Security and robustness testing
- **Benchmarks**: Performance measurements and comparisons

## 📊 Performance

The library is optimized for high performance with:
- **Sorted collections** for O(log n) OID lookups
- **Memory pooling** for reduced allocations
- **Async I/O** for scalable operations
- **Minimal boxing** with generic value types
- **Thread-safe** concurrent operations

### Benchmark Results (2025-09-28)

Performance measurements on Apple M2, .NET 9.0:

| Operation | Mean Time | Memory | Description |
|-----------|-----------|--------|-------------|
| **OID Parsing** | 1.831 μs | 1.2 KB | Parse OID string to object |
| **VarBind Creation** | 319.7 ns | 32 B | Create SNMP variable binding |
| **SHA256 Auth** | 1.307 μs | 144 B | SNMPv3 authentication |

**Key Performance Metrics:**
- ⚡ **Ultra-fast VarBind creation** at ~320 nanoseconds - critical for high-throughput SNMP operations
- 🚀 **Efficient OID parsing** at ~1.8 microseconds - excellent for protocol processing
- 🔒 **Strong security performance** with SHA256 authentication at ~1.3 microseconds
- 💾 **Memory efficient** with minimal allocations across all operations
- 📈 **Consistent performance** with low variance across all benchmarks

Run full benchmarks with: `dotnet run -c Release --project tests/nSNMP.Benchmarks`

## 🔒 Security

### SNMPv3 Security Features
- **Authentication**: HMAC-MD5, HMAC-SHA1/SHA2 family
- **Privacy**: DES-CBC, AES-CFB (128/192/256 bit)
- **Timeliness**: Protection against replay attacks
- **User-based Security Model (USM)**: Per-user authentication and privacy

### Best Practices
- Always use SNMPv3 with authentication and privacy in production
- Rotate credentials regularly
- Use strong passwords (minimum 8 characters)
- Implement access control lists (ACLs) on agents
- Monitor and log SNMP access attempts

## 📚 Documentation

### API Documentation
Full API documentation is available at: [Coming Soon]

### Common OIDs Reference
```
System Group:
1.3.6.1.2.1.1.1.0 - sysDescr
1.3.6.1.2.1.1.2.0 - sysObjectID
1.3.6.1.2.1.1.3.0 - sysUpTime
1.3.6.1.2.1.1.4.0 - sysContact
1.3.6.1.2.1.1.5.0 - sysName
1.3.6.1.2.1.1.6.0 - sysLocation

Interface Table:
1.3.6.1.2.1.2.2.1.1 - ifIndex
1.3.6.1.2.1.2.2.1.2 - ifDescr
1.3.6.1.2.1.2.2.1.5 - ifSpeed
1.3.6.1.2.1.2.2.1.8 - ifOperStatus
```

## 🤝 Contributing

We welcome contributions!

### Development Setup
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Style
- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Write unit tests for new features
- Ensure all tests pass before submitting PR

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📮 Support

- **Issues**: [GitHub Issues](https://github.com/johmnym/nSNMP/issues)

## 🗺️ Roadmap

### v1.0 (Current)
- ✅ Core SNMP v1/v2c/v3 implementation
- ✅ Basic MIB support
- ✅ Agent framework
- ✅ Fluent API

### v1.1 (Planned)
- 🔄 Enhanced MIB compiler
- 🔄 SNMP proxy support
- 🔄 Performance optimizations
- 🔄 Additional security providers

### v2.0 (Future)
- 📋 SNMP over TLS (RFC 6353)
- 📋 IPv6 support improvements
- 📋 Cloud-native features
- 📋 Distributed tracing integration

---

**Built with ❤️**