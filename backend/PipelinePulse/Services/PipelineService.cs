using PipelinePulse.Dtos;
using PipelinePulse.Models;
using PipelinePulse.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PipelinePulse.Services;

public class PipelineService(IPipelineRepository pipelineRepository) : IPipelineService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PipelineDto?> GetPipelineAsync(string id, CancellationToken cancellationToken)
    {
        var pipeline = await pipelineRepository.GetPipelineAsync(id, cancellationToken);
        return pipeline is null ? null : ToDto(pipeline);
    }

    public Task<bool> DeletePipelineAsync(string id, CancellationToken cancellationToken) =>
        pipelineRepository.DeletePipelineAsync(id, cancellationToken);

    public async Task<PipelineDto?> UpdatePipelineAsync(
        string id,
        string? name,
        PipelineProduct? productType,
        CancellationToken cancellationToken)
    {
        var pipeline = await pipelineRepository.UpdatePipelineAsync(
            id,
            name,
            productType,
            cancellationToken);
        return pipeline is null ? null : ToDto(pipeline);
    }

    public async Task<PipelinePageDto> GetPipelinesAsync(
        string? name,
        PipelineProduct? productType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (pipelines, totalCount) = await pipelineRepository.GetPipelinesAsync(
            name,
            productType,
            page,
            pageSize,
            cancellationToken);

        return new PipelinePageDto
        {
            Items = pipelines.Select(pipeline => new PipelineDto
            {
                Id = pipeline.Id,
                Name = pipeline.Name,
                ProductType = GetProductTypeName(pipeline.Product),
                CreatedAt = pipeline.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (totalCount + pageSize - 1) / pageSize
        };
    }

    public async Task<RunPageDto?> GetPipelineRunsAsync(
        string pipelineId,
        RunStatus? status,
        int limit,
        DateTime? cursorStartedAt,
        string? cursorRunId,
        CancellationToken cancellationToken)
    {
        if (await pipelineRepository.GetPipelineAsync(pipelineId, cancellationToken) is null)
        {
            return null;
        }

        var runs = await pipelineRepository.GetPipelineRunsAsync(
            pipelineId,
            status,
            cursorStartedAt,
            cursorRunId,
            limit + 1,
            cancellationToken);
        var hasMore = runs.Count > limit;
        var pageRuns = hasMore ? runs.Take(limit).ToList() : runs;

        return new RunPageDto
        {
            Items = pageRuns.Select(ToDto).ToList(),
            NextCursor = hasMore
                ? RunCursorCodec.Encode(pageRuns[^1].StartedAt, pageRuns[^1].Id)
                : null
        };
    }

    public async Task<RunDto?> CreatePipelineRunAsync(
        string pipelineId,
        string triggeredBy,
        string commitSha,
        CancellationToken cancellationToken)
    {
        var run = await pipelineRepository.CreatePipelineRunAsync(
            pipelineId,
            triggeredBy,
            commitSha,
            cancellationToken);
        return run is null ? null : ToDto(run);
    }

    public async Task<RunDetailsDto?> GetRunWithNotesAsync(
        string runId,
        CancellationToken cancellationToken)
    {
        var run = await pipelineRepository.GetRunWithNotesAsync(runId, cancellationToken);
        return run is null
            ? null
            : new RunDetailsDto
            {
                Id = run.Id,
                Status = GetRunStatusName(run.Status),
                StartedAt = run.StartedAt,
                FinishedAt = run.FinishedAt,
                DurationMs = run.DurationMs,
                TriggeredBy = run.TriggeredBy,
                CommitSha = run.CommitSha,
                Notes = run.Notes
                    .OrderBy(note => note.CreatedAt)
                    .ThenBy(note => note.Id)
                    .Select(note => new NoteDto
                    {
                        Id = note.Id,
                        Author = note.Author,
                        Body = note.Body,
                        CreatedAt = note.CreatedAt
                    })
                    .ToList()
            };
    }

    public async Task<string?> GetRunStatusAsync(string runId, CancellationToken cancellationToken)
    {
        var status = await pipelineRepository.GetRunStatusAsync(runId, cancellationToken);
        return status.HasValue ? GetRunStatusName(status.Value) : null;
    }

    public async Task<RunDto?> UpdateRunStatusAsync(
        string runId,
        RunStatus status,
        CancellationToken cancellationToken)
    {
        var run = await pipelineRepository.UpdateRunStatusAsync(runId, status, cancellationToken);
        return run is null ? null : ToDto(run);
    }

    public async Task<NoteDto?> CreateRunNoteAsync(
        string runId,
        string author,
        string body,
        CancellationToken cancellationToken)
    {
        var note = await pipelineRepository.CreateRunNoteAsync(
            runId,
            author,
            body,
            cancellationToken);
        return note is null
            ? null
            : new NoteDto
            {
                Id = note.Id,
                Author = note.Author,
                Body = note.Body,
                CreatedAt = note.CreatedAt
            };
    }

    public Task<bool> DeleteNoteAsync(string noteId, CancellationToken cancellationToken) =>
        pipelineRepository.DeleteNoteAsync(noteId, cancellationToken);

    public async Task<(PipelineDto Response, string Location)> CreatePipelineAsync(
        string name,
        PipelineProduct productType,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await pipelineRepository.CreatePipelineAsync(
            name,
            productType,
            idempotencyKey,
            idempotencyKey is null ? string.Empty : CreateRequestHash(name, productType),
            cancellationToken);
    }

    private static string CreateRequestHash(string name, PipelineProduct productType)
    {
        var canonicalRequest = JsonSerializer.Serialize(new { name, productType }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
    }

    private static string GetProductTypeName(PipelineProduct product) => product switch
    {
        PipelineProduct.FlashArray => "flasharray",
        PipelineProduct.FlashBlade => "flashblade",
        PipelineProduct.Hyperscale => "hyperscale",
        _ => throw new ArgumentOutOfRangeException(nameof(product), product, "Unsupported pipeline product.")
    };

    private static PipelineDto ToDto(Pipeline pipeline) => new()
    {
        Id = pipeline.Id,
        Name = pipeline.Name,
        ProductType = GetProductTypeName(pipeline.Product),
        CreatedAt = pipeline.CreatedAt
    };

    private static RunDto ToDto(Run run) => new()
    {
        Id = run.Id,
        Status = GetRunStatusName(run.Status),
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        DurationMs = run.DurationMs,
        TriggeredBy = run.TriggeredBy,
        CommitSha = run.CommitSha
    };

    private static string GetRunStatusName(RunStatus status) => status switch
    {
        RunStatus.Queued => "queued",
        RunStatus.Running => "running",
        RunStatus.Passed => "passed",
        RunStatus.Failed => "failed",
        RunStatus.Flaky => "flaky",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported run status.")
    };
}
