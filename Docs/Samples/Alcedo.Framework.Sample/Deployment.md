# Alcedo Framework Sample Deployment

This guide deploys the paired ASP.NET Core backend and Blazor WebAssembly
frontend with the infrastructure versions used by the original reference
deployment.

## Services

| Service | Image or build | Host port | Purpose |
|---|---|---:|---|
| PostgreSQL | `postgres:18.4` | `127.0.0.1:5432` | Application, Quartz, and PostgreSQL message transport data |
| Redis | `redis:8.8.1` | `127.0.0.1:6379` | Cache, sessions, locks, and idempotency |
| SeaweedFS | `chrislusf/seaweedfs:4.40` | `127.0.0.1:8333` | S3-compatible object storage |
| SeaweedFS Admin | `chrislusf/seaweedfs:4.40` | `127.0.0.1:23646` | Local storage administration |
| Backend | Sample Dockerfile | `127.0.0.1:8081` | API, MVC/HTMX sample, and health endpoints |
| Frontend | Sample Dockerfile + nginx | `127.0.0.1:8082` | Blazor WebAssembly management UI |

The sample uses PostgreSQL as its MassTransit transport, so RabbitMQ is not
required. All published ports are bound to loopback by default. Add a secured
reverse proxy if remote access is required.

## Prerequisites

- Docker Engine with Docker Compose v2
- At least 4 GB of free memory for building and running the stack
- `openssl` or another cryptographically secure key generator

Run all commands from:

```text
Samples/Alcedo.Framework.Sample/deploy
```

## Configure Secrets

Copy the template and edit the resulting `.env` file:

```shell
cp .env.example .env
```

Generate separate JWT and column-encryption keys:

```shell
openssl rand -base64 32
openssl rand -base64 32
```

Replace every `change-me` value. Never commit `.env`; the repository only
tracks `.env.example`.

The S3 credentials are configured after SeaweedFS starts. Do not reuse the
example project's old development credentials.

## Start Infrastructure

```shell
docker compose up -d postgres redis seaweedfs seaweedfs-admin
docker compose ps
```

PostgreSQL and Redis include health checks. SeaweedFS management endpoints are
available only on the local machine:

- S3 endpoint: `http://localhost:8333`
- Admin console: `http://localhost:23646`

Use the admin console to create the credentials entered in `.env`. If storage
features are exercised, create the bucket required by the feature. Core's
public storage convention uses `global-public-bucket`.

## Initialize the Database

PostgreSQL creates the database named by `SAMPLE_DATABASE`. Apply the committed,
idempotent EF Core script:

```shell
docker compose exec -T postgres sh -c \
  'PGPASSWORD="$POSTGRES_PASSWORD" psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
  < ../back/Migrations/InitialCreate.sql
```

Alternatively, from the repository root with PostgreSQL available on
`localhost:5432`, configure `appsettings.Development.json` and run:

```shell
dotnet ef database update \
  --project Samples/Alcedo.Framework.Sample/back/Alcedo.Framework.Sample.csproj \
  --startup-project Samples/Alcedo.Framework.Sample/back/Alcedo.Framework.Sample.csproj \
  --context AlcedoDbContext
```

## Build and Start the Applications

```shell
docker compose up -d --build back front
docker compose ps
```

Open:

- MVC/HTMX Juice catalog: `http://localhost:8081/juices`
- Backend health: `http://localhost:8081/health/ready`
- Blazor management UI: `http://localhost:8082`

The Scalar API UI is only enabled in the Development environment. To inspect
it locally with `ASPNETCORE_ENVIRONMENT=Development`, use `/scalar/v1`; the
OpenAPI JSON is `/openapi/v1.json`.

## Runtime Configuration

Backend public settings are mounted from:

```text
deploy/back/appsettings.Production.json
```

Secrets and connection strings are injected with environment variables in
`compose.yaml`. .NET nested configuration keys use double underscores, for
example `JwtOptions__Key`.

The frontend configuration is public and mounted from:

```text
deploy/front/appsettings.json
```

It must contain only browser-visible values such as `ApiBaseUrl`.

`deploy/front/default.conf` supplies the SPA fallback required for direct
navigation to Blazor routes.

For a real domain or TLS deployment, change:

- `AlcedoCorsOptions.AllowedOrigins`
- frontend `ApiBaseUrl`
- `StorageOptions.S3PublicUrl`
- `SecurityOptions.CookieSecureOnly` to `true`
- trusted proxy addresses or networks

Put a reverse proxy in front of ports 8081 and 8082 instead of publishing
PostgreSQL, Redis, or SeaweedFS administration endpoints.

## Operations

View logs:

```shell
docker compose logs -f back
docker compose logs -f front
```

Stop containers while preserving named volumes:

```shell
docker compose down
```

Delete the sample database, cache, storage data, and logs:

```shell
docker compose down --volumes
```

Rebuild only the applications after a source change:

```shell
docker compose up -d --build --no-deps back front
```

## Troubleshooting

| Symptom | Check |
|---|---|
| Backend cannot reach PostgreSQL | `docker compose ps postgres`; verify `POSTGRES_USER`, `POSTGRES_PASSWORD`, and `SAMPLE_DATABASE` |
| Backend readiness is unhealthy | Request `/health/ready`; it checks PostgreSQL and Redis |
| Frontend reports CORS errors | The browser origin must be listed in `AlcedoCorsOptions.AllowedOrigins` |
| Frontend calls the wrong backend | Edit `deploy/front/appsettings.json` and recreate `front` |
| S3 operations fail | Verify SeaweedFS credentials, bucket existence, and `AWS__ServiceURL` |
| Direct navigation to a Blazor route returns 404 | Verify `deploy/front/default.conf` is mounted and recreate `front` |

## Security Notes

- The tracked files contain placeholders only; `.env` must remain untracked.
- Rotate any credentials previously stored in development files or Git history.
- Use distinct values for JWT signing and field encryption.
- Protect database backups, TLS private keys, and production configuration.
- Do not expose PostgreSQL, Redis, SeaweedFS master/admin ports, or application
  origin ports directly to the internet.
