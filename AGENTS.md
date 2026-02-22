# AGENTS.md

This file provides guidance to Coding Agents when working with code in this repository.

## Build and Run Commands

```bash
# Build the entire solution
dotnet build ThinIce.sln

# Run the web app
dotnet run --project src/Meeze.ThinIce.App

# Run tests
dotnet test ThinIce.sln

# Publish AOT native binary
dotnet publish src/Meeze.ThinIce.App -c Release
```

## Architecture

ThinIce is a multitenanted proxy for Apache Iceberg REST catalog and blob storage APIs. It sits between client applications (using bearer tokens) and back-end storage providers (S3, Snowflake, local files), so clients never see the underlying provider or its credentials.

This is a **service layer** (not a browser webapp). It uses a **DI-based plugin architecture** (ADR 001) where providers are registered via `IOptions` at startup.

### Key Design Decisions

Architecture Decision Records are in `doc/`:
- Use a DI based plugin architecture
- Never expose the underlying token
- Dev local Iceberg
- endpoints should allow throttling
- Multi-provider routing via chain-of-responsibility

### Conventions

- `[LoggerMessage]` source-generated logging only — no `ILogger.Log*()` calls
- `[JsonSerializable]` source-generated contexts for all serialized types (AOT)
- `TreatWarningsAsErrors` in all projects
- Primary constructor records for all models/DTOs
- MSTest with `[DataRow]` parameterization, ice-themed test data
- Error messages in source code are plain and factual; ice-themed language is reserved for test data and examples
- AOT-compatible configuration binding (manual `Configure<T>` lambda, no reflection-based `IConfiguration.Bind`)
- Never put volatile counts (test counts, warning counts, etc.) in docs — they create a maintenance burden on every change

## Tech Stack

- .NET 10, C#, nullable reference types enabled, implicit usings
- ASP.NET Core Minimal APIs with AOT publishing (`PublishAot`)
- MSTest 4.x for testing, `Microsoft.AspNetCore.Mvc.Testing` for integration tests, `Microsoft.AspNetCore.TestHost` for middleware tests
- Solution file: `ThinIce.sln`
