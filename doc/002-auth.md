# Issue our own OICD tokens and never expose the underlying token

## Context and Problem Statement

We want users to be able to use this system without knowing what the underlying provider is.  That means when a user 
authenticates we want to issue our own token, but to be able to validate that token and exchange it internally for a
token used by the back-end provider. For example, a user may have the token AAAAAAAA, but when
querying AWS S3, we could desire to use an AWsv4 signed JWT as an IAM with service role.

## Considered Options

- don't (just use the provider token) - simplest do-nothing approach
  - doesn't allow user-scope front end to service-scope back-end
  - requires user-auth integrated tightly to provider auth (OICD trust etc)
- wrap the token - issue our own token attaching the underlying token as context
  - could expose inner service scope tokens which should never be exposed
- proxy/exchange user for backend

## Decision Outcome

Chosen option: We issue our own tokens seen by users and used in the App, and each provider implementation is 
responsible for issuing and using appropriate underlying tokens or auth methods.

TODO : Middleware sequence.  Identity options. Provider routing.

### Consequences

* Good - user never knows the provider
* Bad - providers need to issue keys, deal with securely caching them when needed for performance