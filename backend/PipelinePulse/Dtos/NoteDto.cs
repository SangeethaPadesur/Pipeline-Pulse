namespace PipelinePulse.Dtos;

public sealed class NoteDto
{
    public required string Id { get; init; }

    public required string Author { get; init; }

    public required string Body { get; init; }

    public DateTime CreatedAt { get; init; }
}
