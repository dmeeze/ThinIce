# ThinIce - Standalone Iceberg REST Catalog Proxy

Apache Iceberg is a big data system which is effectively a set of columnar data files and a metadata catalog of what
data's in those files. You can get one off the shelf from many vendors and providers, but for local dev that can be
annoying.

So this is a simple local mock/proxy for multitenanted Iceberg. This means the user gets a token, and sees an Iceberg
limited only to the tenant they're using (eg, MyCompany). However under the covers, we can use flat file storage
or service role IAM to S3 etc without having to link the user identity to the upstream provider directly.

This is a _service_ layer using bearer tokens, it is not a browser based webapp.

## Structure

**Meeze.ThinIce.App** (API service)
  - ASP.NET Minimal API host with AOT publishing, explicit `Program.Main` entry point
  - Endpoint groups for config, namespaces, tables, blob data
  - Pass-through streaming async layer to underlying providers

**Meeze.ThinIce.Auth** (adaptor)
  - Auth middleware validates Bearer tokens, sets TenantContext on request features
  - Anonymous endpoints (e.g. config) configurable via `ThinIceAuthOptions`
  - Outside callers see a single token without ever knowing what the inner provider is
  - S3 access tokens, for example, never leave ThinIce

**Meeze.ThinIce.Auth.Abstractions** (leaf)
  - `IAuthProvider`, `TenantContext`

**Meeze.ThinIce.Iceberg** (adaptor)
  - Iceberg proxy interfaces and models common to all providers
  - `IIcebergCatalog`, `IIcebergStorage` — single-tenant interfaces (no tenant parameter)
  - `IcebergProvider` — provider selection interface using chain-of-responsibility pattern
  - `TenantResolver` — creates catalog/storage instances for a specific tenant
  - `IcebergRouter` — session-scoped router that resolves providers and caches resolvers
  - `NamespaceHelpers`, 14 Iceberg model records

**Meeze.ThinIce.Iceberg.LocalDev** (provider)
  - Files-on-disk implementation for local development
  - `Provider` implements `IcebergProvider`, accepting all tenants for local development
  - `Resolver` implements `TenantResolver`, creating catalog/storage instances for a specific tenant
  - `Catalog` implements single-tenant namespace and table CRUD on filesystem (partial class split: `.Namespaces.cs`, `.Tables.cs`)
  - `Storage` implements filesystem-backed blob I/O under `{tenant}/data/`
  - Isolates metadata/storage by tenant
  - Static tokens defined in config (format `token:Tenant:user@email`)
  - Default data path: `{LocalApplicationData}/ThinIce/data` (configurable via `LocalDev:BasePath`)

## API (minimum viable subset of Iceberg REST catalog)

| Method | Path | Status |
|--------|------|--------|
| GET | `/v1/config` | Implemented (catalog configuration) |
| GET/POST/DELETE | `/v1/namespaces` | Implemented |
| GET | `/v1/namespaces/{ns}` | Implemented |
| GET/POST/DELETE | `/v1/namespaces/{ns}/tables` | Implemented |
| GET | `/v1/namespaces/{ns}/tables/{table}` | Implemented |
| GET/PUT | `/v1/data/{path}` | Implemented (streaming blob I/O) |

## Usage

### Running the app

```bash
dotnet run --project src/Meeze.ThinIce.App
```

The app starts on `http://localhost:5000` by default.

### Running tests

```bash
dotnet test ThinIce.sln
```

### Publishing (AOT native binary)

```bash
dotnet publish src/Meeze.ThinIce.App -c Release
```

### API examples

The development config (`appsettings.Development.json`) ships with two tokens:
- `freeze-ray-token-001` → tenant `SnowyConesIceCream`, user `mrfreeze@example.com`
- `ice-age-token-002` → tenant `WayneEnterprises`, user `batman@example.org`

**Get catalog config:**

```bash
curl http://localhost:5000/v1/config
```

**Create a namespace:**

```bash
curl -X POST http://localhost:5000/v1/namespaces \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{"namespace": ["analytics"], "properties": {"owner": "data-team"}}'
```

**List namespaces:**

```bash
curl http://localhost:5000/v1/namespaces \
  -H "Authorization: Bearer <access_token>"
```

**Create a table:**

```bash
curl -X POST http://localhost:5000/v1/namespaces/analytics/tables \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{"name": "events", "schema": {"type": "struct", "fields": [{"id": 1, "name": "ts", "type": "timestamp", "required": true}]}}'
```

**Write data (blob):**

```bash
curl -X PUT http://localhost:5000/v1/data/warehouse/events/data.parquet \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/octet-stream" \
  --data-binary @data.parquet
```

**Read data (blob):**

```bash
curl http://localhost:5000/v1/data/warehouse/events/data.parquet \
  -H "Authorization: Bearer <access_token>" \
  -o data.parquet
```

## Current State

**Phase 7 complete** — All planned phases implemented. LocalDev provider fully functional with namespace CRUD, table CRUD, and streaming blob storage. AOT native binary publishes cleanly.

**Multi-provider routing complete** — Implemented chain-of-responsibility pattern for routing different tenants to different backends. All 6 stages complete:
- Stage 1: Core abstractions (`IcebergProvider`, `TenantResolver`, `IcebergRouter`)
- Stage 2: LocalDev class naming simplified
- Stage 3: LocalDev Provider and Resolver implemented
- Stage 4: Endpoints migrated to `IcebergRouter`
- Stage 5: Comprehensive test suite added (123 tests)
- Stage 6: Documentation updated, obsolete interfaces removed, AOT verified (zero warnings)

**Test refactoring complete** — Provider and Router tests reimplemented using Moq for cleaner mocking:
- LocalDevProviderTests: 17 tests covering CanHandle, GetCatalog, GetStorage, caching, tenant isolation, and concurrency
- IcebergRouterTests: 15 tests covering single/multiple providers, chain-of-responsibility, exception handling, and provider selection
- All tests use Moq for mocking IIcebergProvider, ICatalog, IStorage, and ILogger dependencies
- Total: 32 new unit tests, all passing

See [doc/006-multi-provider-implementation-summary.md](doc/006-multi-provider-implementation-summary.md) for complete implementation details.

The architecture supports routing different tenants to different backends (LocalDev, AWS, GCP, etc.) via the `IcebergProvider` chain-of-responsibility pattern. See `doc/ARCHITECTURE_REVIEW.md`, `doc/005-multi-provider-routing.md`, and `doc/IMPLEMENTATION_PLAN.md` for details.

## Multi-Provider Configuration

ThinIce uses a **chain-of-responsibility pattern** to route tenants to different backend providers. Multiple providers can be registered, and each tenant request is routed to the first provider that can handle it.

### Registering Providers

In `Program.cs`:

```csharp
// Register multiple providers in order of priority
builder.Services.AddLocalDevProvider(builder.Configuration);
// builder.Services.AddAwsProvider(builder.Configuration);  // Future
// builder.Services.AddGcpProvider(builder.Configuration);  // Future

// Required for multi-provider routing
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IcebergRouter>();
```

### Provider Selection

Providers are evaluated in registration order via `IcebergProvider.CanHandle(tenant)`:

1. Request arrives with tenant "Acme" extracted from bearer token
2. `IcebergRouter` iterates registered providers
3. First provider returning `true` from `CanHandle("Acme")` is selected
4. Provider creates a `TenantResolver` for that tenant
5. Resolver is cached in `HttpContext.Features` for the request lifetime

### Implementing a Custom Provider

```csharp
public sealed class MyProvider : IcebergProvider
{
    public bool CanHandle(string tenant)
    {
        // Return true if this provider should handle the tenant
        return tenant.StartsWith("MyPrefix");
    }

    public TenantResolver GetResolver(string tenant)
    {
        return new MyResolver(tenant);
    }
}

public sealed class MyResolver : TenantResolver
{
    private readonly string _tenant;

    public MyResolver(string tenant)
    {
        _tenant = tenant;
    }

    public IIcebergCatalog GetCatalog()
    {
        // Return catalog implementation for this tenant
    }

    public IIcebergStorage GetStorage()
    {
        // Return storage implementation for this tenant
    }
}
```

### LocalDev Provider Configuration

The LocalDev provider accepts all tenants by default (useful for development):

```json
{
  "LocalDev": {
    "BasePath": "/path/to/data",
    "Tokens": [
      "freeze-ray-token-001:SnowyConesIceCream:mrfreeze@example.com",
      "ice-age-token-002:WayneEnterprises:batman@example.org"
    ]
  }
}
```

## Config Options

The `/v1/config` endpoint returns catalog configuration to clients. Configurable via `appsettings.json`:

```json
{
  "Config": {
    "Prefix": "",
    "OAuth2ServerUri": "https://localhost/"
  }
}
```

- **Prefix** — The path prefix returned to clients in `defaults["prefix"]`. Clients construct URLs as `http://{host}/v1/{prefix}/{path}`. When set (e.g. `"foo/bar"`), all authenticated endpoints are registered at `/v1/foo/bar/...`. Defaults to `""` (no prefix), meaning clients use `/v1/namespaces` etc. directly.
- **OAuth2ServerUri** — Returned in `overrides["oauth2-server-uri"]`. Defaults to `https://localhost/`.

## Throttling

Rate limiting is applied via ASP.NET Core Rate Limiting middleware (ADR 004):

- `/v1/config` (unauthenticated): 100 requests/minute (sliding window)
- All authenticated endpoints: 10,000 requests/minute shared across all APIs (sliding window)

Configurable via `appsettings.json`:

```json
{
  "Throttling": {
    "ConfigPermitsPerMinute": 100,
    "AuthenticatedPermitsPerMinute": 10000
  }
}
```

## What's Next

These are not planned but are natural next steps:

- **S3 + Glue provider** — `IIcebergCatalog` backed by AWS Glue, `IIcebergStorage` backed by S3 with IAM role assumption
- **JWT validation** — accept and validate JWTs, extract tenant+user from claims (per ADR 002)
- **Namespace update** (PATCH properties) — not in the Iceberg REST minimum viable subset but commonly used
- **Table commit/update** — `POST /v1/namespaces/{ns}/tables/{table}` for metadata updates
