namespace PipelinePulse.Dtos;

public sealed class RunDto
{
    public required string Id { get; init; }

    public required string Status { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public long? DurationMs { get; init; }

    public required string TriggeredBy { get; init; }

    public required string CommitSha { get; init; }
}
