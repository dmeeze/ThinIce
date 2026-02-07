# AGENTS.md

This file provides guidance to Coding Agents when working with code in this repository.

## Build and Run Commands

```bash
# Build the entire solution
dotnet build ThinIce.sln

# Run the web app
dotnet run --project src/Meeze.ThinIce.App

# Run tests (test/ directory exists but no test projects yet)
dotnet test ThinIce.sln
```

## Architecture

ThinIce is a multitenanted proxy for Apache Iceberg REST catalog and blob storage APIs. It sits between client applications (using bearer tokens) and back-end storage providers (S3, Snowflake, local files), so clients never see the underlying provider or its credentials.

This is a **service layer** (not a browser webapp). It uses a **DI-based plugin architecture** (ADR 001) where providers are registered via `IOptions` at startup.

### Project Roles

The projects follow an **adaptor/provider pattern**:

- **Meeze.ThinIce.App** — ASP.NET web service (the host). Implements controllers for Iceberg REST catalog and blob read/write. Uses `CreateSlimBuilder` with AOT publishing enabled.
- **Meeze.ThinIce.Auth** (adaptor) — Token proxy layer. Issues its own tokens to callers; providers are responsible for exchanging these for back-end credentials (ADR 002). Inner tokens (e.g., S3 signed keys) never leave ThinIce.
- **Meeze.ThinIce.Iceberg** (adaptor) — Iceberg-specific interfaces and structures common across all providers. Abstracts whether the back-end is S3+Glue, Snowflake, or local files.
- **Meeze.ThinIce.Iceberg.LocalDev** (provider) — Files-on-disk implementation for local development (ADR 003). Uses static tokens from config mapped to tenant+user identity.

### Key Design Decisions

Architecture Decision Records are in `doc/`:
- **ADR 001** — DI plugin architecture over reflection-based plugins or hard-coded switches
- **ADR 002** — Issue own OIDC tokens; never expose underlying provider tokens
- **ADR 003** — Local dev uses flat files on disk (no Minio/Spark/containers)

### Current State

The project is in early scaffolding — library projects contain stub `Class1` classes, and the App project has the default template code. The `test/` directory is empty.

## Tech Stack

- .NET 10, C#, nullable reference types enabled, implicit usings
- ASP.NET Core with AOT publishing (`PublishAot`)
- Solution file: `ThinIce.sln`
