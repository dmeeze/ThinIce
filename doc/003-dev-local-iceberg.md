# dev local iceberg

## Context and Problem Statement

We want the thinnest and lightest implementation possible of iceberg for dev purposes.  This means
- use files on disk for metadata (similar to https://github.com/boringdata/boring-catalog)
- use files on disk for blob storage

## Considered Options

- minio + local sqlite etc 
- spark or other full implementation in a container
- files on disk

## Decision Outcome

For development purposes - files on disk is simple. the rest can be built as plugins if they're needed.

### File Layout

{basePath}/{tenant}/namespaces/{ns}/properties.json
{basePath}/{tenant}/namespaces/{ns}/tables/{table}/metadata/v{n}.metadata.json
{basePath}/{tenant}/data/{**path}

Blob read/write streams directly through ThinIce endpoints (`GET/PUT /v1/data/{**path}`, no presigned URLs). Storage is per-tenant via `IIcebergStorageResolver`.