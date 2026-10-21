using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PipelinePulse.Data;
using PipelinePulse.Dtos;
using PipelinePulse.Models;

namespace PipelinePulse.Repositories;

public class PipelineRepository(PipelineDbContext dbContext) : IPipelineRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<Pipeline?> GetPipelineAsync(string id, CancellationToken cancellationToken) =>
        dbContext.Pipelines
            .AsNoTracking()
            .SingleOrDefaultAsync(pipeline => pipeline.Id == id, cancellationToken);

    public async Task<bool> DeletePipelineAsync(string id, CancellationToken cancellationToken)
    {
        var pipeline = await dbContext.Pipelines
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (pipeline is null)
        {
            return false;
        }

        dbContext.Pipelines.Remove(pipeline);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Pipeline?> UpdatePipelineAsync(
        string id,
        string? name,
        PipelineProduct? productType,
        CancellationToken cancellationToken)
    {
        var pipeline = await dbContext.Pipelines
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (pipeline is null)
        {
            return null;
        }

        if (name is not null)
        {
            pipeline.Name = name;
        }

        if (productType.HasValue)
        {
            pipeline.Product = productType.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return pipeline;
    }

    public async Task<(IReadOnlyList<Pipeline> Items, long TotalCount)> GetPipelinesAsync(
        string? name,
        PipelineProduct? productType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Pipelines.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            var escapedName = name.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            query = query.Where(pipeline =>
                EF.Functions.Like(pipeline.Name, $"%{escapedName}%", "\\"));
        }

        if (productType.HasValue)
        {
            query = query.Where(pipeline => pipeline.Product == productType.Value);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var offset = ((long)page - 1) * pageSize;
        var items = offset > int.MaxValue
            ? []
            : await query
                .OrderBy(pipeline => pipeline.Name)
                .ThenBy(pipeline => pipeline.Id)
                .Skip((int)offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Run>> GetPipelineRunsAsync(
        string pipelineId,
        RunStatus? status,
        DateTime? cursorStartedAt,
        string? cursorRunId,
        int takeCount,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Runs
            .AsNoTracking()
            .Where(run => run.PipelineId == pipelineId);

        if (status.HasValue)
        {
            query = query.Where(run => run.Status == status.Value);
        }

        if (cursorStartedAt.HasValue && cursorRunId is not null)
        {
            var startedAt = cursorStartedAt.Value;
            query = query.Where(run =>
                run.StartedAt < startedAt
                || (run.StartedAt == startedAt && string.Compare(run.Id, cursorRunId) < 0));
        }

        return await query
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<Run?> CreatePipelineRunAsync(
        string pipelineId,
        string triggeredBy,
        string commitSha,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Pipelines.AnyAsync(pipeline => pipeline.Id == pipelineId, cancellationToken))
        {
            return null;
        }

        var run = new Run
        {
            Id = Guid.NewGuid().ToString(),
            PipelineId = pipelineId,
            Status = RunStatus.Queued,
            StartedAt = DateTime.UtcNow,
            TriggeredBy = triggeredBy,
            CommitSha = commitSha
        };

        dbContext.Runs.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public Task<Run?> GetRunWithNotesAsync(string runId, CancellationToken cancellationToken) =>
        dbContext.Runs
            .AsNoTracking()
            .Include(run => run.Notes)
            .SingleOrDefaultAsync(run => run.Id == runId, cancellationToken);

    public async Task<RunStatus?> GetRunStatusAsync(string runId, CancellationToken cancellationToken) =>
        await dbContext.Runs
            .AsNoTracking()
            .Where(run => run.Id == runId)
            .Select(run => (RunStatus?)run.Status)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Run?> UpdateRunStatusAsync(
        string runId,
        RunStatus status,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.Runs
            .SingleOrDefaultAsync(item => item.Id == runId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        run.Status = status;
        if (status is RunStatus.Passed or RunStatus.Failed or RunStatus.Flaky)
        {
            var finishedAt = DateTime.UtcNow;
            run.FinishedAt = finishedAt;
            run.DurationMs = Math.Max(0L, (long)(finishedAt - run.StartedAt).TotalMilliseconds);
        }
        else
        {
            run.FinishedAt = null;
            run.DurationMs = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task<Note?> CreateRunNoteAsync(
        string runId,
        string author,
        string body,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Runs.AnyAsync(run => run.Id == runId, cancellationToken))
        {
            return null;
        }

        var note = new Note
        {
            Id = Guid.NewGuid().ToString(),
            RunId = runId,
            Author = author,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Notes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return note;
    }

    public async Task<bool> DeleteNoteAsync(string noteId, CancellationToken cancellationToken)
    {
        var note = await dbContext.Notes
            .SingleOrDefaultAsync(item => item.Id == noteId, cancellationToken);
        if (note is null)
        {
            return false;
        }

        dbContext.Notes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(PipelineDto Response, string Location)> CreatePipelineAsync(
        string name,
        PipelineProduct productType,
        string? idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (idempotencyKey is not null)
        {
            var existingRecord = await dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Key == idempotencyKey, cancellationToken);
            if (existingRecord is not null)
            {
                return ReadReplay(existingRecord, requestHash);
            }
        }

        var pipeline = new Pipeline
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Product = productType
        };
        var response = new PipelineDto
        {
            Id = pipeline.Id,
            Name = pipeline.Name,
            ProductType = productType switch
            {
                PipelineProduct.FlashArray => "flasharray",
                PipelineProduct.FlashBlade => "flashblade",
                PipelineProduct.Hyperscale => "hyperscale",
                _ => throw new ArgumentOutOfRangeException(nameof(productType), productType, "Unsupported pipeline product.")
            },
            CreatedAt = pipeline.CreatedAt
        };
        var location = $"/pipelines/{pipeline.Id}";

        if (idempotencyKey is null)
        {
            dbContext.Pipelines.Add(pipeline);
            await dbContext.SaveChangesAsync(cancellationToken);
            return (response, location);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Pipelines.Add(pipeline);
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = idempotencyKey,
            RequestHash = requestHash,
            ResponseBody = JsonSerializer.Serialize(response, JsonOptions),
            ResponseLocation = location
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return (response, location);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            var record = await dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Key == idempotencyKey, cancellationToken);
            if (record is null)
            {
                throw;
            }

            return ReadReplay(record, requestHash);
        }
    }

    private static (PipelineDto Response, string Location) ReadReplay(
        IdempotencyRecord record,
        string requestHash)
    {
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(record.RequestHash),
                Convert.FromHexString(requestHash)))
        {
            throw new IdempotencyKeyConflictException();
        }

        var response = JsonSerializer.Deserialize<PipelineDto>(record.ResponseBody, JsonOptions)
            ?? throw new InvalidDataException($"Stored response for idempotency key '{record.Key}' is invalid.");

        return (response, record.ResponseLocation);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 1555 or 2067
        };
}
