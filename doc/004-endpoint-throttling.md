# endpoints should allow throttling

iceberg requires an unauthenticated /config endpoint and many REST/file endpoints.  We should prevent users
overwhelming the system using throttling.

## Considered Options

- do nothing - this project doesn't need throttling yet
- flat throttling - one rule for all
- tiered throttling - heavy limits on unauthed, per tenant throttling on authed

## Decision Outcome

mix of flat and tiered.  simple fixed amount throttling on REST endpoints, but much lower limits on the unauthed
/config endpoint.  We can do per-tenant throttling later if we need it.
