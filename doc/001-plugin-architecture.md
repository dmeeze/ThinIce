# Use a DI based plugin architecture

## Context and Problem Statement

ThinIce is a proxy implementing the simplest possible iceberg REST catalog API and blob-file retrieval APIs.
By proxing multiple back-end services (S3, Snowflake, etc), in a tenanted fashion, it can
- allow different tenants to use different back-end services (eg: MyCompany in AWS, YourCompany in GCP)
- separate front-end user tokens from back-end service tokens.

## Considered Options

- reflection based plugins - add a dll at runtime without a recompile of core
- hard-coded - this is a tiny project, we might be overcomplicating doing more than a switch
- dependency injection and IOption - light touch but expandable, even at boot time (eg: in environment X don't start provider Y)

## Decision Outcome

- Use IOptions at app start time to register and configure each provider with DI

### Middleware and Routing

- auth middleware runs first, validates bearer token, sets scoped TenantContext (tenant + user)
- `IIcebergRouter` uses that tenant information to find the `IIcebergProvider`

### Minimal API over MVC Controllers

- MVC controllers require runtime reflection for model binding, routing, and filter pipelines
- This is incompatible with `PublishAot` and `CreateSlimBuilder`
- Minimal API endpoints with source-generated `[JsonSerializable]` contexts are fully AOT-compatible
- Endpoints are organised in static extension classes under `Endpoints/`, mapped as route groups
