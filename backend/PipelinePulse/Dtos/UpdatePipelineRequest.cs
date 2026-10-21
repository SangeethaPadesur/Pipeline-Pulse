using System.ComponentModel.DataAnnotations;

namespace PipelinePulse.Dtos;

public sealed class UpdatePipelineRequest
{
    [StringLength(200)]
    public string? Name { get; init; }

    [StringLength(32)]
    public string? ProductType { get; init; }
}
