# Never expose the underlying token

## Context and Problem Statement

We want users to be able to use this system without seeing or knowing about an external provider token.
That means when a user is using this service we need to be able to validate that token but internally
use any other token required for the back-end provider. For example, a user may have the token AAAAAAAA, 
but when querying AWS S3, we could desire to use an AWSv4 signed JWT, a separate IDP token, or an IAM service role.

## Considered Options

- don't (just use the provider token) - simplest do-nothing approach
  - doesn't allow user-scope front end to service-scope back-end
  - requires user-auth integrated tightly to provider auth (OIDC trust etc)
- wrap the token - issue our own token attaching the underlying token as context
  - could expose inner service scope tokens which should never be exposed
- require token auth implementations to resolve to a provider:tenant:user

## Decision Outcome

Chosen option: require token auth implementations to resolve to a provider:tenant:user

### Token Flow

- (external) user obtains a bearer token.
- all endpoints except /v1/config require Authorization: Bearer {token}
  - GET /v1/config is unauthenticated (Iceberg clients call it before auth)
- auth middleware validates token via IAuthProvider, extracts tenant+user, sets scoped TenantContext
- LocalDev: uses static tokens from config, format `token:Tenant:user@email`
- Future: accept JWT, validate, use jwt claims like `sub` or `act` or `scope` to identify and verify the tenant and user

### Consequences

* Good - user never knows the provider
* Good - provider needs to validate keys, not issue them.
* Bad - keys must provide sufficient scope to completely identify a user, meaning caching may be required for 
  multi-validation
