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

### Project Roles

The projects follow an **adaptor/provider pattern**:

- **Meeze.ThinIce.App** — ASP.NET Minimal API host. `CreateSlimBuilder` with AOT publishing. Endpoint groups in `Endpoints/` directory. `AppJsonSerializerContext` for source-generated JSON.
- **Meeze.ThinIce.Auth** (adaptor) — `ThinIceAuthMiddleware` validates Bearer tokens via `IEnumerable<IAuthProvider>`, sets `TenantContext` on `HttpContext.Features`. Anonymous endpoints configured via `ThinIceAuthOptions`. `AuthJsonContext` for AOT error serialization.
- **Meeze.ThinIce.Auth.Abstractions** (leaf) — `IAuthProvider`, `TenantContext` record. Root namespace: `Meeze.ThinIce.Auth`.
- **Meeze.ThinIce.Iceberg** (adaptor) — `IIcebergCatalog`, `IIcebergStorage`, `IIcebergCatalogResolver`, `IIcebergStorageResolver`, `NamespaceHelpers`, 14 Iceberg model records. Catalog/storage interfaces operate on a single tenant (no tenant parameter); resolver interfaces create/cache per-tenant instances.
- **Meeze.ThinIce.Iceberg.LocalDev** (provider) — Files-on-disk implementation for local development (ADR 003). `LocalDevAuthProvider` maps static config tokens to tenant+user identity. `LocalDevCatalogResolver` implements `IIcebergCatalogResolver`, caching per-tenant `LocalDevCatalog` instances. `LocalDevCatalog` implements `IIcebergCatalog` with filesystem-backed namespace and table CRUD for a single tenant. `LocalDevStorageResolver` implements `IIcebergStorageResolver`, caching per-tenant `LocalDevStorage` instances. `LocalDevStorage` implements `IIcebergStorage` with filesystem-backed blob read/write/delete under `{tenant}/data/`. `LocalDevJsonContext` for AOT disk I/O. `LocalDevOptions.ResolvedBasePath` defaults to `{LocalApplicationData}/ThinIce/data`.

### Key Design Decisions

Architecture Decision Records are in `doc/`:
- **ADR 001** — DI plugin architecture over reflection-based plugins or hard-coded switches
- **ADR 002** — Never expose underlying provider tokens; accept externally-issued bearer tokens
- **ADR 003** — Local dev uses flat files on disk (no Minio/Spark/containers)

### Current State

**Phase 7 complete.** All phases implemented. Error handling consistency verified, AOT publish clean (no trim warnings), documentation updated.

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
