# Mirka CMS

ASP.NET Core modular monolith CMS (Clean Architecture). Admin panel is Farsi / RTL.

## Solution structure

```
src/
├── CMS.Domain
├── CMS.Application
├── CMS.Infrastructure
├── CMS.Web                 # MVC host + Admin area
└── CMS.Modules
    ├── Blog/{Domain,Application,Infrastructure,Web}
    ├── Shop/{Domain,Application,Infrastructure,Web}
    └── Forms/{Domain,Application,Infrastructure,Web}
```

## Run

```bash
dotnet run --project src/CMS.Web
```

Admin dashboard: `/Admin/Dashboard`

## Feature toggles

Configured in `src/CMS.Web/appsettings.json` under `FeatureManagement` (`Blog`, `Shop`, `Forms`).

## Database

PostgreSQL, single database. Connection string key: `DefaultConnection`.

**Do not rely on the placeholder password in `appsettings.json`.** Prefer user secrets locally, or copy [`.env.example`](.env.example) → `.env` and set real values (`.env` is gitignored).

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=mirka_cms;Username=postgres;Password=YOUR_PASSWORD" --project src/CMS.Web
```

## Object storage (S3)

All file uploads (Blog covers, Shop product images, Forms attachments, Media library) use **S3** via `IObjectStorage`.

Configure under `Storage:S3` (see `.env.example`):

| Setting | Env var |
|---------|---------|
| Access key | `Storage__S3__AccessKey` |
| Secret key | `Storage__S3__SecretKey` |
| Bucket | `Storage__S3__BucketName` |
| Region | `Storage__S3__Region` |
| Custom endpoint (MinIO / compatible) | `Storage__S3__ServiceUrl` |
| Public/CDN base URL | `Storage__S3__PublicBaseUrl` |
| Path-style (MinIO) | `Storage__S3__ForcePathStyle` |

Example (user secrets):

```bash
dotnet user-secrets set "Storage:S3:AccessKey" "..." --project src/CMS.Web
dotnet user-secrets set "Storage:S3:SecretKey" "..." --project src/CMS.Web
dotnet user-secrets set "Storage:S3:BucketName" "mirka-cms" --project src/CMS.Web
dotnet user-secrets set "Storage:S3:Region" "us-east-1" --project src/CMS.Web
# Optional MinIO:
# dotnet user-secrets set "Storage:S3:ServiceUrl" "http://localhost:9000" --project src/CMS.Web
# dotnet user-secrets set "Storage:S3:ForcePathStyle" "true" --project src/CMS.Web
# dotnet user-secrets set "Storage:S3:PublicBaseUrl" "http://localhost:9000/mirka-cms" --project src/CMS.Web
```

Ensure the bucket (or CDN) allows public read for media URLs, or set `UsePublicReadAcl` only if your provider still supports object ACLs.

For production, set the same keys as environment variables (see `.env.example`: `ConnectionStrings__DefaultConnection`, `Storage__S3__*`, `Seed__Admin__*`). Locally, CMS.Web loads a repo-root `.env` at startup (without overriding vars already set by the host/Docker/shell).

Create the database once (psql or pgAdmin):

```sql
CREATE DATABASE mirka_cms;
```

Then run:

```bash
dotnet run --project src/CMS.Web
```

Startup migrates Identity tables and seeds the Admin user. Dev seed: `admin@local.test` / `Admin123!` (from `appsettings.Development.json`).

## Next steps

1. Antiforgery + security hardening (rate limiting)
2. Blog module vertical slice (replace stubs)
3. Keep Postgres `DefaultConnection` and S3 credentials in user secrets / env for non-dev

Logs: `src/CMS.Web/Logs/cms-*.log` (Serilog rolling files).
