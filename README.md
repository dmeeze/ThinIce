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
  - `IIcebergCatalogResolver`, `IIcebergStorageResolver` — create/cache per-tenant instances
  - `NamespaceHelpers`, 14 Iceberg model records

**Meeze.ThinIce.Iceberg.LocalDev** (provider)
  - Files-on-disk implementation for local development
  - `LocalDevCatalogResolver` caches per-tenant `LocalDevCatalog` instances
  - `LocalDevCatalog` implements single-tenant namespace and table CRUD on filesystem (partial class split: `.Namespaces.cs`, `.Tables.cs`)
  - `LocalDevStorageResolver` caches per-tenant `LocalDevStorage` instances for blob I/O under `{tenant}/data/`
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

See `doc/PLAN.md` for the full implementation plan.

## What's Next

These are not planned but are natural next steps:

- **S3 + Glue provider** — `IIcebergCatalog` backed by AWS Glue, `IIcebergStorage` backed by S3 with IAM role assumption
- **Keyed DI for multi-provider routing** — route different tenants to different backend providers via `TenantContext.ProviderKey`
- **JWT validation** — accept and validate JWTs, extract tenant+user from claims (per ADR 002)
- **Namespace update** (PATCH properties) — not in the Iceberg REST minimum viable subset but commonly used
- **Table commit/update** — `POST /v1/namespaces/{ns}/tables/{table}` for metadata updates
