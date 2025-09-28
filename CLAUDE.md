# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/nSNMP.Core.Tests/nSNMP.Core.Tests.csproj
dotnet test tests/nSNMP.Agent.Tests/nSNMP.Agent.Tests.csproj

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Architecture Overview

nSNMP is a C# implementation of the Simple Network Management Protocol (SNMP) targeting .NET 9.0. The solution follows a modular architecture with clear separation of concerns:

### Core Components

1. **nSNMP.SMI** - Structure of Management Information implementation
   - `SMIDataFactory`: Factory for creating SMI data types from byte streams
   - `DataTypes/`: Primitive (Integer, OctetString, ObjectIdentifier, Null) and Constructed (Sequence) data types
   - `PDUs/`: Protocol Data Units (GetRequest, GetResponse, base PDU class)
   - `X690/BERParser`: BER (Basic Encoding Rules) parser for ASN.1 data

2. **nSNMP** - Core SNMP message handling
   - `Message/SnmpMessage`: Main SNMP message class with Version, CommunityString, and PDU properties
   - `Message/MessageFactory`: Factory for creating SNMP messages
   - Depends on nSNMP.SMI for data type handling

3. **nSNMP.MIB** - Management Information Base functionality
   - Provides MIB parsing and management capabilities

4. **Test Projects** - Unit tests using xUnit
   - `nSNMP.Core.Tests` - Core infrastructure tests
   - `nSNMP.Agent.Tests` - Agent/server functionality tests
   - `nSNMP.Manager.Tests` - Manager/client functionality tests (future)
   - Tests organized to mirror the source structure
   - `SnmpMessageFactory` helper for test data creation

### Key Design Patterns

- **Factory Pattern**: `SMIDataFactory` and `MessageFactory` for object creation from byte streams
- **Inheritance Hierarchy**: All data types implement `IDataType`, with base classes `PrimitiveDataType` and `ConstructedDataType`
- **Fluent API**: Message configuration using property setters

### Data Flow

1. Incoming SNMP messages are parsed as byte arrays
2. `SMIDataFactory.Create()` recursively parses BER-encoded data
3. `SnmpMessage.Create()` constructs message objects from parsed sequences
4. PDUs contain RequestId, Error, ErrorIndex, and VarbindList (Sequence of variable bindings)

### Testing Strategy

The project uses xUnit with comprehensive unit tests for:
- BER parsing (`BERParserTests`)
- Data type serialization/deserialization (Integer, OctetString, ObjectIdentifier tests)
- Message creation and manipulation (`SnmpMessageTests`, `MessageFactoryTests`)
- Sequence operations (`SequenceTests`)