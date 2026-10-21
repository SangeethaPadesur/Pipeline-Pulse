using PipelinePulse.Dtos;
using PipelinePulse.Models;

namespace PipelinePulse.Services;

public interface IPipelineService
{
    Task<PipelineDto?> GetPipelineAsync(string id, CancellationToken cancellationToken);

    Task<bool> DeletePipelineAsync(string id, CancellationToken cancellationToken);

    Task<PipelineDto?> UpdatePipelineAsync(
        string id,
        string? name,
        PipelineProduct? productType,
        CancellationToken cancellationToken);

    Task<PipelinePageDto> GetPipelinesAsync(
        string? name,
        PipelineProduct? productType,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RunPageDto?> GetPipelineRunsAsync(
        string pipelineId,
        RunStatus? status,
        int limit,
        DateTime? cursorStartedAt,
        string? cursorRunId,
        CancellationToken cancellationToken);

    Task<RunDto?> CreatePipelineRunAsync(
        string pipelineId,
        string triggeredBy,
        string commitSha,
        CancellationToken cancellationToken);

    Task<RunDetailsDto?> GetRunWithNotesAsync(string runId, CancellationToken cancellationToken);

    Task<string?> GetRunStatusAsync(string runId, CancellationToken cancellationToken);

    Task<RunDto?> UpdateRunStatusAsync(
        string runId,
        RunStatus status,
        CancellationToken cancellationToken);

    Task<NoteDto?> CreateRunNoteAsync(
        string runId,
        string author,
        string body,
        CancellationToken cancellationToken);

    Task<bool> DeleteNoteAsync(string noteId, CancellationToken cancellationToken);

    Task<(PipelineDto Response, string Location)> CreatePipelineAsync(
        string name,
        PipelineProduct productType,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}
