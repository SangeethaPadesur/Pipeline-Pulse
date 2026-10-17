namespace PipelinePulse.Models;

public class Run
{
    public required string Id { get; set; }

    public required string PipelineId { get; set; }

    public RunStatus Status { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public long? DurationMs { get; set; }

    public required string TriggeredBy { get; set; }

    public required string CommitSha { get; set; }

    public Pipeline Pipeline { get; set; } = null!;

    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
