using System.ComponentModel.DataAnnotations;

namespace PipelinePulse.Dtos;

public sealed class CreatePipelineRequest
{
    [Required]
    [StringLength(200)]
    public required string Name { get; init; }

    [Required]
    [StringLength(32)]
    public required string ProductType { get; init; }
}
