namespace PipelinePulse.Dtos;

public sealed class RunPageDto
{
    public required IReadOnlyList<RunDto> Items { get; init; }

    public string? NextCursor { get; init; }
}
