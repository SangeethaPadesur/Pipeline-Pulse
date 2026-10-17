# PipelinePulse database

The API applies pending EF Core migrations and inserts any missing sample pipelines, runs, and notes from `seed/seed.json` during startup. Existing records with the same IDs are left unchanged, so restarting the API does not reset seeded or application data. The seed file is copied to the build and publish output.

## Create and apply migrations

From the repository root:

```powershell
dotnet ef migrations add <MigrationName> --project backend\PipelinePulse\PipelinePulse.csproj --startup-project backend\PipelinePulse\PipelinePulse.csproj --output-dir Data\Migrations
dotnet ef database update --project backend\PipelinePulse\PipelinePulse.csproj --startup-project backend\PipelinePulse\PipelinePulse.csproj
```

The API also calls `Database.MigrateAsync()` at startup, so a separate `database update` command is not required for normal application startup.

## SQLite storage in containers

The development connection string stores `pipelinepulse.db` relative to the API's working directory. For a container, set `ConnectionStrings__DefaultConnection` to a path on a mounted persistent volume, for example `Data Source=/data/pipelinepulse.db`, and mount the volume at `/data`. Without persistent storage, deleting/recreating a pod also deletes its SQLite database.
