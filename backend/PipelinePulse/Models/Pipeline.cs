namespace PipelinePulse.Models;

public class Pipeline
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public PipelineProduct Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Run> Runs { get; set; } = new List<Run>();
}
