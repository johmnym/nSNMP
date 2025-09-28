# nSNMP Project Restructuring Plan

## Overview
This document outlines a plan to enhance the nSNMP project structure for better modularity, extensibility, and usability.

## Current Structure
```
nSNMP/
├── nSNMP/                     # Core library
├── nSNMP.SMI/                 # ASN.1/BER encoding
├── nSNMP.Core.Tests/          # Core infrastructure unit tests
├── nSNMP.Agent.Tests/         # Agent/server unit tests
├── nSNMP.InteropTests/        # Interoperability tests
├── nSNMP.Benchmarks/          # Performance benchmarks
└── nSNMP.Fuzz/                # Security fuzzing
```

## Proposed Structure
```
nSNMP/
├── src/
│   ├── nSNMP.Abstractions/    # 🆕 Interfaces & contracts
│   ├── nSNMP.SMI/             # ASN.1/BER encoding
│   ├── nSNMP.MIB/             # 🆕 MIB management (extracted)
│   ├── nSNMP.Core/            # Core library (simplified)
│   └── nSNMP.Extensions/      # 🆕 Optional extensions
├── tests/
│   ├── nSNMP.Abstractions.Tests/  # Tests for abstractions
│   ├── nSNMP.SMI.Tests/           # Tests for SMI/BER
│   ├── nSNMP.MIB.Tests/           # Tests for MIB management
│   ├── nSNMP.Core.Tests/          # Core library tests
│   ├── nSNMP.Extensions.Tests/    # Extension tests
│   ├── nSNMP.InteropTests/        # Interoperability tests
│   ├── nSNMP.Benchmarks/          # Performance benchmarks
│   └── nSNMP.Fuzz/                # Security fuzzing
├── samples/                    # 🆕 Example applications
│   ├── SimpleSnmpGet/
│   ├── SnmpTrapReceiver/
│   ├── SnmpV3SecureClient/
│   ├── SnmpAgent/
│   └── MibBrowser/
└── docs/
    ├── api/                    # 🆕 API documentation
    ├── guides/                 # 🆕 User guides
    └── architecture/           # 🆕 Architecture docs
```

## Implementation Tasks

### Phase 1: Create Abstractions Project
**Goal:** Establish a foundation of interfaces and contracts

#### 1.1 Create nSNMP.Abstractions Project
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Description>Core abstractions and interfaces for nSNMP</Description>
  </PropertyGroup>
</Project>
```

#### 1.2 Extract Interfaces
Move/create interfaces for:
- `ISnmpClient` - Client operations contract
- `ISnmpAgent` - Agent operations contract
- `IUdpChannel` - Transport abstraction
- `ISnmpLogger` - Logging abstraction
- `IMibManager` - MIB operations contract
- `ISecurityProvider` - Security operations
- `IDataType` - SMI data type interface

#### 1.3 Define Core Models
Shared models and enums:
- `SnmpVersion`
- `ErrorStatus`
- `SecurityLevel`
- `AuthProtocol`
- `PrivProtocol`

### Phase 2: Extract MIB Subsystem
**Goal:** Separate MIB management into dedicated project

#### 2.1 Create nSNMP.MIB Project
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Description>MIB parsing and management for nSNMP</Description>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\nSNMP.Abstractions\nSNMP.Abstractions.csproj" />
    <ProjectReference Include="..\nSNMP.SMI\nSNMP.SMI.csproj" />
  </ItemGroup>
</Project>
```

#### 2.2 Move MIB Components
Relocate from nSNMP to nSNMP.MIB:
- `MibParser.cs`
- `MibTree.cs`
- `MibNode.cs`
- `MibManager.cs`
- `MibResolver.cs`
- `SnmpClientMibExtensions.cs` → `MibExtensions.cs`

#### 2.3 Add MIB Resources
- Standard MIB definitions (RFC1213-MIB, SNMPv2-MIB, etc.)
- MIB compiler tools
- MIB validation utilities

### Phase 3: Create Extensions Project
**Goal:** Provide optional, convenience features

#### 3.1 Create nSNMP.Extensions Project
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Description>Extension methods and helpers for nSNMP</Description>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\nSNMP.Abstractions\nSNMP.Abstractions.csproj" />
    <ProjectReference Include="..\nSNMP.Core\nSNMP.Core.csproj" />
  </ItemGroup>
</Project>
```

#### 3.2 Extension Categories
- **Client Extensions**
  - Fluent API for client configuration
  - Bulk operations helpers
  - Retry policies
  - Circuit breakers

- **Agent Extensions**
  - Middleware pipeline
  - Request filtering
  - Rate limiting
  - Caching

- **Data Type Extensions**
  - Conversion helpers
  - Validation utilities
  - Formatting helpers

- **Observability Extensions**
  - Health checks
  - Metrics exporters
  - Distributed tracing helpers

### Phase 4: Create Sample Projects
**Goal:** Demonstrate library usage with real examples

#### 4.1 SimpleSnmpGet
Basic SNMP GET operation example:
```csharp
// Program.cs
using nSNMP;

var client = SnmpClient.CreateCommunity("192.168.1.1");
var result = await client.GetAsync("1.3.6.1.2.1.1.1.0");
Console.WriteLine($"System Description: {result.First().Value}");
```

#### 4.2 SnmpTrapReceiver
Trap receiver daemon example:
```csharp
// Program.cs
using nSNMP.Agent;

var receiver = new TrapReceiver(162);
receiver.TrapReceived += (sender, trap) =>
{
    Console.WriteLine($"Trap from {trap.Source}: {trap.Message}");
};
await receiver.StartAsync();
```

#### 4.3 SnmpV3SecureClient
SNMPv3 with full security example:
```csharp
// Program.cs
using nSNMP.Security;

var credentials = V3Credentials.AuthPriv(
    "admin",
    AuthProtocol.SHA256, "authpass123",
    PrivProtocol.AES256, "privpass123");

var client = new SnmpClientV3("192.168.1.1", credentials);
await client.DiscoverEngineAsync();
var result = await client.GetAsync("1.3.6.1.2.1.1.1.0");
```

#### 4.4 SnmpAgent
Full SNMP agent implementation:
```csharp
// Program.cs
using nSNMP.Agent;

var agent = new SnmpAgentHost(161);
agent.RegisterScalar("1.3.6.1.2.1.1.1.0",
    () => "My Custom Agent v1.0");
agent.RegisterTable("1.3.6.1.2.1.2.2",
    new InterfaceTableProvider());
await agent.StartAsync();
```

#### 4.5 MibBrowser
Interactive MIB browser application:
- Load and parse MIB files
- Browse MIB tree
- Perform SNMP operations
- Display results with MIB context

### Phase 5: Reorganize Folder Structure
**Goal:** Better organization with src/tests/samples separation

#### 5.1 Create Directory Structure
```bash
mkdir src
mkdir tests
mkdir samples
mkdir docs/api
mkdir docs/guides
mkdir docs/architecture
```

#### 5.2 Move Projects
```bash
# Move source projects
mv nSNMP* src/
mv src/nSNMP.Core.Tests tests/
mv src/nSNMP.Abstractions.Tests tests/
mv src/nSNMP.SMI.Tests tests/
mv src/nSNMP.MIB.Tests tests/
mv src/nSNMP.Extensions.Tests tests/
mv src/nSNMP.InteropTests tests/
mv src/nSNMP.Benchmarks tests/
mv src/nSNMP.Fuzz tests/
```

#### 5.3 Update Solution File
```xml
<Project>
  <ItemGroup>
    <SolutionFolder Name="src">
      <ProjectReference Include="src\nSNMP.Abstractions\nSNMP.Abstractions.csproj" />
      <ProjectReference Include="src\nSNMP.SMI\nSNMP.SMI.csproj" />
      <ProjectReference Include="src\nSNMP.MIB\nSNMP.MIB.csproj" />
      <ProjectReference Include="src\nSNMP.Core\nSNMP.Core.csproj" />
      <ProjectReference Include="src\nSNMP.Extensions\nSNMP.Extensions.csproj" />
    </SolutionFolder>
    <SolutionFolder Name="tests">
      <ProjectReference Include="tests\nSNMP.Abstractions.Tests\nSNMP.Abstractions.Tests.csproj" />
      <ProjectReference Include="tests\nSNMP.SMI.Tests\nSNMP.SMI.Tests.csproj" />
      <ProjectReference Include="tests\nSNMP.MIB.Tests\nSNMP.MIB.Tests.csproj" />
      <ProjectReference Include="tests\nSNMP.Core.Tests\nSNMP.Core.Tests.csproj" />
      <ProjectReference Include="tests\nSNMP.Extensions.Tests\nSNMP.Extensions.Tests.csproj" />
      <ProjectReference Include="tests\nSNMP.InteropTests\nSNMP.InteropTests.csproj" />
      <ProjectReference Include="tests\nSNMP.Benchmarks\nSNMP.Benchmarks.csproj" />
      <ProjectReference Include="tests\nSNMP.Fuzz\nSNMP.Fuzz.csproj" />
    </SolutionFolder>
    <SolutionFolder Name="samples">
      <ProjectReference Include="samples\SimpleSnmpGet\SimpleSnmpGet.csproj" />
      <ProjectReference Include="samples\SnmpTrapReceiver\SnmpTrapReceiver.csproj" />
      <ProjectReference Include="samples\SnmpV3SecureClient\SnmpV3SecureClient.csproj" />
      <ProjectReference Include="samples\SnmpAgent\SnmpAgent.csproj" />
      <ProjectReference Include="samples\MibBrowser\MibBrowser.csproj" />
    </SolutionFolder>
  </ItemGroup>
</Project>
```

### Phase 6: Documentation Enhancement
**Goal:** Comprehensive documentation for all audiences

#### 6.1 API Documentation
- XML documentation for all public APIs
- DocFX or similar for API reference generation
- Code examples in XML comments

#### 6.2 User Guides
- Getting Started Guide
- SNMPv3 Security Guide
- MIB Management Guide
- Performance Tuning Guide
- Troubleshooting Guide

#### 6.3 Architecture Documentation
- System Architecture Overview
- Design Decisions (ADRs)
- Component Diagrams
- Sequence Diagrams for key operations
- Security Architecture

## Migration Strategy

### Step 1: Create New Projects (Non-Breaking)
1. Create nSNMP.Abstractions
2. Create nSNMP.MIB
3. Create nSNMP.Extensions
4. Add project references

### Step 2: Extract Interfaces (Non-Breaking)
1. Define interfaces in Abstractions
2. Implement interfaces in existing classes
3. No breaking changes to public API

### Step 3: Move MIB Code (Potentially Breaking)
1. Move MIB classes to nSNMP.MIB
2. Add type forwards in nSNMP for compatibility
3. Mark old locations as [Obsolete]

### Step 4: Add Extensions (Non-Breaking)
1. Create extension methods in Extensions project
2. Pure additions, no modifications

### Step 5: Create Samples (Non-Breaking)
1. Add sample projects
2. Document usage patterns

### Step 6: Reorganize Folders (Non-Breaking)
1. Update file paths in solution
2. Update CI/CD scripts
3. Update documentation

## Benefits

### For Library Users
- **Clearer API** through abstractions
- **Better examples** with sample projects
- **Easier to extend** via Extensions project
- **Better documentation** structure

### For Library Developers
- **Better testability** via interfaces
- **Cleaner dependencies** between components
- **Easier to maintain** with clear boundaries
- **More modular** for future enhancements

### For Contributors
- **Clear structure** makes it easier to contribute
- **Obvious locations** for new features
- **Better organized** tests and samples
- **Comprehensive docs** for understanding

## Success Criteria

1. ✅ All existing tests continue to pass
2. ✅ No breaking changes to public API (or clear migration path)
3. ✅ Sample projects compile and run
4. ✅ Documentation is complete and accessible
5. ✅ NuGet packages can be created for each project
6. ✅ Performance benchmarks remain unchanged
7. ✅ Security fuzzing continues to work

## Timeline

- **Week 1**: Create Abstractions project and extract interfaces
- **Week 2**: Extract MIB subsystem to separate project
- **Week 3**: Create Extensions project with initial extensions
- **Week 4**: Develop sample projects
- **Week 5**: Reorganize folder structure
- **Week 6**: Documentation and testing

## Next Steps

1. Review and approve plan
2. Create feature branch for restructuring
3. Implement Phase 1 (Abstractions)
4. Test and validate
5. Continue with subsequent phases
6. Update CI/CD pipelines
7. Update README and documentation
8. Create migration guide for existing users