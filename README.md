# ThinIce - Standalone Iceberg and OIDC

Apache Iceberg is a big data system which is effectively a set of columnar data files and a metadata catalog of what
data's in those files. You can get one off the shelf from many vendors and providers, but for local dev that can be
annoying.

So this is a simple local mock/proxy for multitenanted Iceberg. This means the user gets a token, and sees an Iceberg
limited only to the tenant they're using (eg, MyCompany). However under the covers, we can use flat file storage
or service role IAM to S3 etc without having to link the user identity to the upstream provider directly.

This is a _service_ layer using bearer tokens, it is not a browser based webapp.

## Structure

**Meeze.ThinIce.App** (API service)
  - ASP.NET Minimal API host with AOT publishing
  - Endpoint groups for OAuth, config, namespaces, tables, blob data
  - Pass-through streaming async layer to underlying providers

**Meeze.ThinIce.Auth** (adaptor)
  - Auth middleware validates Bearer tokens, sets TenantContext on request features
  - Anonymous endpoints (token exchange, config) configurable via `ThinIceAuthOptions`
  - Outside callers see a single token without ever knowing what the inner provider is
  - S3 access tokens, for example, never leave ThinIce

**Meeze.ThinIce.Auth.Abstractions** (leaf)
  - `IAuthProvider`, `TenantContext`, OAuth request/response models

**Meeze.ThinIce.Iceberg** (adaptor)
  - Iceberg proxy interfaces and models common to all providers
  - `IIcebergCatalog`, `IIcebergStorage` — single-tenant interfaces (no tenant parameter)
  - `IIcebergCatalogResolver`, `IIcebergStorageResolver` — create/cache per-tenant instances
  - `NamespaceHelpers`, 14 Iceberg model records

**Meeze.ThinIce.Iceberg.LocalDev** (provider)
  - Files-on-disk implementation for local development
  - `LocalDevCatalogResolver` caches per-tenant `LocalDevCatalog` instances
  - `LocalDevCatalog` implements single-tenant namespace CRUD on filesystem
  - Isolates metadata/storage by tenant
  - Static tokens defined in config, eg:
    - `xxwefnuiwqbnergiqbiybaoysudbvcolas:MyCompany:me@mycompany.example`
    - `jjasdufiwbiuqbwruiqbwerouwbnrgfunr:YourCompany:you@yourcompany.example`
  - Default data path: `{LocalApplicationData}/ThinIce/data` (configurable via `LocalDev:BasePath`)

**Meeze.ThinIce.Dev.Iceberg.S3** (provider)
  - NOT IMPLEMENTED YET
  - TODO stub intended as a basis to implement Iceberg using IAM Role, S3 + Glue

## API (minimum viable subset of Iceberg REST catalog)

| Method | Path | Status |
|--------|------|--------|
| POST | `/v1/oauth/tokens` | Implemented (form-encoded token exchange) |
| GET | `/v1/config` | Implemented (catalog configuration) |
| GET/POST/DELETE | `/v1/namespaces` | Implemented |
| GET | `/v1/namespaces/{ns}` | Implemented |
| GET/POST/DELETE | `/v1/namespaces/{ns}/tables` | Phase 4 |
| GET | `/v1/namespaces/{ns}/tables/{table}` | Phase 4 |
| GET/PUT | `/v1/data/{path}` | Phase 5 |

## Current State

**Phase 3b complete** — Auth, token exchange, namespace CRUD, and resolver pattern all implemented. Tenant resolution is a first-class concern via `IIcebergCatalogResolver`/`IIcebergStorageResolver`; catalog/storage interfaces operate on a single tenant. 51 tests passing, 0 warnings. Next: Phase 4 (Table CRUD).

See `doc/PLAN.md` for the full implementation plan.
