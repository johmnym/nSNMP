# nSNMP Project Overview

This document provides a comprehensive overview of the nSNMP project, its structure, and how to build and test it.

## Project Purpose

nSNMP is a C# implementation of the Simple Network Management Protocol (SNMP). It provides a set of libraries for creating, parsing, and handling SNMP messages. The project is structured into several components, each with a specific responsibility:

*   **nSNMP**: The core project that handles SNMP message creation and parsing.
*   **nSNMP.SMI**: Implements the Structure of Management Information (SMI), which defines the data types and structures used in SNMP.
*   **nSNMP.MIB**:  Provides functionality for working with Management Information Bases (MIBs), which are databases of managed objects.
*   **nSNMP.Tests**: Contains unit tests for the other projects.

## Technologies and Architecture

*   **Language**: C#
*   **Framework**: .NET 9.0
*   **Testing Framework**: xUnit
*   **Architecture**: The project follows a modular architecture, with each component responsible for a specific part of the SNMP protocol.

## Building and Running

To build the project, you can use the `dotnet build` command in the root directory:

```bash
dotnet build
```

To run the tests, you can use the `dotnet test` command:

```bash
dotnet test
```

## Development Conventions

*   **Coding Style**: The project follows standard C# coding conventions.
*   **Testing**: The project uses xUnit for unit testing. All new functionality should be accompanied by unit tests.
*   **Contributions**: Contributions are welcome. Please follow the existing coding style and ensure that all tests pass before submitting a pull request.
