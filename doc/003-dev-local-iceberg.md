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