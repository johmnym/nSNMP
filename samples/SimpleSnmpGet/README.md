# Simple SNMP GET Example

This sample demonstrates basic SNMP GET operations using the nSNMP library.

## Features

- Command-line interface for flexible testing
- Fluent API usage with retry policies
- Structured logging with Microsoft.Extensions.Logging
- Error handling and graceful failure

## Usage

```bash
dotnet run -- <host> [community] [oid1] [oid2] ...
```

### Examples

```bash
# Basic usage with default OIDs
dotnet run -- 192.168.1.1

# Custom community string
dotnet run -- 192.168.1.1 private

# Custom OIDs
dotnet run -- 192.168.1.1 public 1.3.6.1.2.1.1.1.0 1.3.6.1.2.1.1.5.0

# System description and name
dotnet run -- 192.168.1.1 public 1.3.6.1.2.1.1.1.0 1.3.6.1.2.1.1.5.0
```

## Default OIDs

If no OIDs are specified, the sample retrieves:
- `1.3.6.1.2.1.1.1.0` - System Description (sysDescr)
- `1.3.6.1.2.1.1.5.0` - System Name (sysName)

## Key Concepts Demonstrated

1. **Fluent API**: Using `SnmpClient.CreateCommunity()` for easy client creation
2. **Retry Policies**: Automatic retry with exponential backoff on failures
3. **Logging**: Structured logging for monitoring and debugging
4. **Error Handling**: Graceful handling of network and SNMP errors