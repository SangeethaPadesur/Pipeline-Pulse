namespace PipelinePulse.Dtos;

public sealed class PipelinePageDto
{
    public required IReadOnlyList<PipelineDto> Items { get; init; }

    public long TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalPages { get; init; }
}
