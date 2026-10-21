# PipelinePulse database

The API applies pending EF Core migrations and inserts any missing sample pipelines, runs, and notes from `seed/seed.json` during startup. Existing records with the same IDs are left unchanged, so restarting the API does not reset seeded or application data. The seed file is copied to the build and publish output.

## List pipelines

`GET /pipelines` (also available at `GET /api/v1/pipelines`) returns pipelines ordered by name and ID. Pagination is 1-based, with a default page size of 20 and a maximum of 100:

```text
GET /pipelines?page=1&pageSize=20&name=nightly&productType=flasharray
```

The optional `name` parameter performs a case-insensitive substring search. `productType` accepts `FlashArray`, `FlashBlade`, or `Hyperscale` (case-insensitive). The response contains `items`, `totalCount`, `page`, `pageSize`, and `totalPages`; invalid pagination or product type values return HTTP 400.

## Get a pipeline

`GET /pipelines/{id}` (also available at `GET /api/v1/pipelines/{id}`) returns the matching pipeline using the same shape as the create endpoint. If the ID does not exist, the API returns HTTP 404.

## Update a pipeline

`PATCH /pipelines/{id}` (also available at `PATCH /api/v1/pipelines/{id}`) partially updates a pipeline and returns the updated pipeline. Include `name`, `productType`, or both; omitted fields remain unchanged. `name` must be non-blank and at most 200 characters. `productType` accepts `FlashArray`, `FlashBlade`, or `Hyperscale` (case-insensitive). An empty update or invalid field returns HTTP 400; an unknown ID returns HTTP 404.

```json
{
  "name": "FlashArray Nightly Updated"
}
```

## Delete a pipeline

`DELETE /pipelines/{id}` (also available at `DELETE /api/v1/pipelines/{id}`) deletes the pipeline and cascades the deletion to its runs and notes. A successful deletion returns HTTP 204; an unknown ID returns HTTP 404.

## List pipeline runs

`GET /pipelines/{id}/runs` (also available at `GET /api/v1/pipelines/{id}/runs`) returns a pipeline's runs newest-first, ordered by `startedAt` and then ID. Use the optional `status` filter (`Queued`, `Running`, `Passed`, `Failed`, or `Flaky`), `limit` (default 20, maximum 100), and `cursor` for keyset pagination:

```text
GET /pipelines/{id}/runs?status=failed&limit=20
GET /pipelines/{id}/runs?status=failed&limit=20&cursor={nextCursor}
```

The response contains `items` and `nextCursor`; `nextCursor` is null when there are no more runs. Invalid statuses, cursors, or limits return HTTP 400. An unknown pipeline ID returns HTTP 404.

## Create a pipeline run

`POST /pipelines/{id}/runs` (also available at `POST /api/v1/pipelines/{id}/runs`) creates a queued run and returns HTTP 201. The server generates the run ID and start time; the initial status is always `queued`.

```json
{
  "triggeredBy": "scheduler",
  "commitSha": "9f1c2a7d3b4e5f60718293a4b5c6d7e8f9a0b1c2"
}
```

`triggeredBy` is required and may contain up to 200 characters. `commitSha` is required and may contain up to 64 characters. Blank values or invalid requests return HTTP 400; an unknown pipeline ID returns HTTP 404.

## Get a run

`GET /runs/{id}` (also available at `GET /api/v1/runs/{id}`) returns a single run and its notes. Notes are ordered oldest-first by creation time and ID. An unknown run ID returns HTTP 404.

## Update a run status

`PATCH /runs/{id}` (also available at `PATCH /api/v1/runs/{id}`) updates the run status and returns the updated run:

```json
{
  "status": "passed"
}
```

`status` accepts `Queued`, `Running`, `Passed`, `Failed`, or `Flaky` (case-insensitive). Setting a terminal status (`Passed`, `Failed`, or `Flaky`) sets `finishedAt` to the current UTC time and calculates `durationMs` from `startedAt`. Setting `Queued` or `Running` clears `finishedAt` and `durationMs`. Invalid statuses return HTTP 400; an unknown run ID returns HTTP 404.

## Add a run note

`POST /runs/{id}/notes` (also available at `POST /api/v1/runs/{id}/notes`) adds a triage note and returns HTTP 201:

```json
{
  "author": "priya.n",
  "body": "Retry passed; investigate intermittent timeout."
}
```

`author` is required and may contain up to 200 characters. `body` is required and may contain up to 4000 characters. Blank values or invalid requests return HTTP 400; an unknown run ID returns HTTP 404.

## Delete a note

`DELETE /notes/{id}` (also available at `DELETE /api/v1/notes/{id}`) deletes a triage note. A successful deletion returns HTTP 204; an unknown note ID returns HTTP 404.

## Stream run status events

`GET /runs/{id}/events` (also available at `GET /api/v1/runs/{id}/events`) opens a Server-Sent Events stream. It immediately sends the current status, then emits an event whenever the status changes:

```text
data: {"Status":"running"}
```

The stream remains open until the client disconnects or the run is deleted. An unknown run ID returns HTTP 404 before the stream begins.

## Create a pipeline

`POST /pipelines` (also available at `POST /api/v1/pipelines`) accepts a name and product type. The API generates a UUID for the pipeline and returns HTTP 201 with the created pipeline:

```json
{
  "name": "FlashArray Nightly",
  "productType": "flasharray"
}
```

`name` is required, non-blank, and at most 200 characters. `productType` is required and accepts `FlashArray`, `FlashBlade`, or `Hyperscale` (case-insensitive). Invalid requests return HTTP 400.

For retry-safe creation, send an `Idempotency-Key` header (1 to 200 non-whitespace characters). Reusing a key with the same request returns the original HTTP 201 response without creating another pipeline. Reusing a key with a different request returns HTTP 409. Idempotency records and pipelines are stored atomically.

## Create and apply migrations

From the repository root:

```powershell
dotnet ef migrations add <MigrationName> --project backend\PipelinePulse\PipelinePulse.csproj --startup-project backend\PipelinePulse\PipelinePulse.csproj --output-dir Data\Migrations
dotnet ef database update --project backend\PipelinePulse\PipelinePulse.csproj --startup-project backend\PipelinePulse\PipelinePulse.csproj
```

The API also calls `Database.MigrateAsync()` at startup, so a separate `database update` command is not required for normal application startup.

## SQLite storage in containers

The development connection string stores `pipelinepulse.db` relative to the API's working directory. For a container, set `ConnectionStrings__DefaultConnection` to a path on a mounted persistent volume, for example `Data Source=/data/pipelinepulse.db`, and mount the volume at `/data`. Without persistent storage, deleting/recreating a pod also deletes its SQLite database.
