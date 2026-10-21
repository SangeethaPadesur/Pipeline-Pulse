using PipelinePulse.Models;
using PipelinePulse.Dtos;

namespace PipelinePulse.Repositories;

public interface IPipelineRepository
{
    Task<Pipeline?> GetPipelineAsync(string id, CancellationToken cancellationToken);

    Task<bool> DeletePipelineAsync(string id, CancellationToken cancellationToken);

    Task<Pipeline?> UpdatePipelineAsync(
        string id,
        string? name,
        PipelineProduct? productType,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Pipeline> Items, long TotalCount)> GetPipelinesAsync(
        string? name,
        PipelineProduct? productType,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Run>> GetPipelineRunsAsync(
        string pipelineId,
        RunStatus? status,
        DateTime? cursorStartedAt,
        string? cursorRunId,
        int takeCount,
        CancellationToken cancellationToken);

    Task<Run?> CreatePipelineRunAsync(
        string pipelineId,
        string triggeredBy,
        string commitSha,
        CancellationToken cancellationToken);

    Task<Run?> GetRunWithNotesAsync(string runId, CancellationToken cancellationToken);

    Task<RunStatus?> GetRunStatusAsync(string runId, CancellationToken cancellationToken);

    Task<Run?> UpdateRunStatusAsync(
        string runId,
        RunStatus status,
        CancellationToken cancellationToken);

    Task<Note?> CreateRunNoteAsync(
        string runId,
        string author,
        string body,
        CancellationToken cancellationToken);

    Task<bool> DeleteNoteAsync(string noteId, CancellationToken cancellationToken);

    Task<(PipelineDto Response, string Location)> CreatePipelineAsync(
        string name,
        PipelineProduct productType,
        string? idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken);
}
