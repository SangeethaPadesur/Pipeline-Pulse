namespace PipelinePulse.Models;

public class Note
{
    public required string Id { get; set; }

    public required string RunId { get; set; }

    public required string Author { get; set; }

    public required string Body { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Run Run { get; set; } = null!;
}
