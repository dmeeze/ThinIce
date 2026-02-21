# Multi-provider routing via chain-of-responsibility

## Context and Problem Statement

ThinIce currently assumes a single provider at runtime. All tenants use the same backend (e.g., LocalDev). We need to support multiple providers where different tenants use different backends (LocalDev, AWS, GCP). For example, tenant `SnowyConesIceCream` might use LocalDev files-on-disk while tenant `WayneEnterprises` uses AWS S3 + Glue.

## Considered Options

* Keyed services with provider key routing — Use `TenantContext.ProviderKey` to look up provider via `GetKeyedService<T>(providerKey)`. Requires strict key matching.
* Chain-of-responsibility pattern — Each provider decides if it handles a given tenant. `IcebergRouter` asks each registered provider in sequence until one accepts. Similar to ASP.NET middleware pattern.
* Configuration-based routing table — Explicit tenant→provider mapping in config. Inflexible, requires deployment for new tenants.

## Decision Outcome

Chosen option: **Chain-of-responsibility pattern** - we expect only a handful of providers so checking them all is lightweight.

