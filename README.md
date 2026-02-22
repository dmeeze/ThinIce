# ThinIce - Standalone Iceberg REST Catalog Proxy

Apache Iceberg is a big data system which is effectively a set of columnar data files and a metadata catalog of what data's in those files. You can get one off the shelf from many vendors and providers, but for local dev that can be annoying.

So this is a simple local mock/proxy for multitenanted Iceberg. This means the user gets a token, and sees an Iceberg limited only to the tenant they're using (eg, MyCompany). However under the covers, we can use flat file storage or service role IAM to S3 etc without having to link the user identity to the upstream provider directly.

This is a _service_ layer using bearer tokens, it is not a browser based webapp.

## API (Complete Core Iceberg REST Catalog)

### Configuration
| Method | Path | Status |
|--------|------|--------|
| GET | `/v1/config` | ✓ Implemented |

### Namespaces
| Method | Path | Status |
|--------|------|--------|
| GET | `/v1/namespaces` | ✓ Implemented |
| POST | `/v1/namespaces` | ✓ Implemented |
| GET | `/v1/namespaces/{ns}` | ✓ Implemented |
| HEAD | `/v1/namespaces/{ns}` | ✓ Implemented |
| POST | `/v1/namespaces/{ns}/properties` | ✓ Implemented |
| DELETE | `/v1/namespaces/{ns}` | ✓ Implemented |
| POST | `/v1/namespaces/{ns}/register` | ✓ Implemented |

### Tables
| Method | Path | Status |
|--------|------|--------|
| GET | `/v1/namespaces/{ns}/tables` | ✓ Implemented |
| POST | `/v1/namespaces/{ns}/tables` | ✓ Implemented |
| GET | `/v1/namespaces/{ns}/tables/{table}` | ✓ Implemented |
| POST | `/v1/namespaces/{ns}/tables/{table}` | ✓ Implemented (commit updates) |
| HEAD | `/v1/namespaces/{ns}/tables/{table}` | ✓ Implemented |
| DELETE | `/v1/namespaces/{ns}/tables/{table}` | ✓ Implemented |
| POST | `/v1/tables/rename` | ✓ Implemented |

### Data Storage
| Method | Path | Status |
|--------|------|--------|
| GET | `/v1/data/{path}` | ✓ Implemented (streaming blob read) |
| PUT | `/v1/data/{path}` | ✓ Implemented (streaming blob write) |

**Note:** Advanced features like views, transactions, scan planning, metrics, and vended credentials are not implemented. This implementation provides a complete core REST catalog suitable for local development and testing.

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