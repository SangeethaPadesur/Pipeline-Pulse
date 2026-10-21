namespace PipelinePulse.Dtos;

public sealed class PipelineDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string ProductType { get; init; }

    public DateTime CreatedAt { get; init; }
}
