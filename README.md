# ThinIce - Standalone Iceberg and OICD

Apache iceberg is a big data system which is effectively a set of columnar data files and a metadata catalog of what 
data's in those files. You can get one off the shelf from many vendors and providers, but for local dev that can be
annoying.

So this is a simple local mock for multitenanted iceberg.  This means the user gets a token, and sees an iceberg 
limited only to the tenant they're using (eg, MyCompany).  However under the covers, we can use flat file storage 
or service role IAM to S3 etc without having to link the user identity to the upstream provider directly.

This is a _service_ layer using bearer tokens, it is not a browser based webapp.

Structure:
Meeze.ThinIce.App (api service app)
  - asp.net web service.  implements controllers for iceberg rest catalog and blob read/write paths.
  - pass-through streaming async layer to underlying providers
Meeze.ThinIce.Auth (adaptor)
  - auth token proxy. outside callers see a single token without ever knowing what the inner provider is.
  - S3 access tokens, for example, never leave ThinIce.
  - relies on plugin/provider to validate user tokens and convert that to the tenant+user identity
Meeze.ThinIce.Iceberg (adaptor)
  - iceberg proxy. outside callers don't know if they're using S3+Glue or Snowflake or local files.
  - provides interfaces and iceberg specific structures and features common to all providers
Meeze.ThinIce.Iceberg.LocalDev (provider)
  - a simple local provider which uses files on disk as the data and metadata store
  - isolate metadata/storage by tenant
  - local developer system uses static tokens defined in config, eg:
    - xxwefnuiwqbnergiqbiybaoysudbvcolas:MyCompany:me@mycompany.example
    - jjasdufiwbiuqbwruiqbwerouwbnrgfunr:YourCompany:you@yourcompany.example
Meeze.ThinIce.Dev.Iceberg.S3 (provider)
  - NOT IMPLEMENTED YET
  - TODO stub intended as a basis to implement iceberg using IAM Role, S3 + Glue