using System.ComponentModel.DataAnnotations;

namespace PipelinePulse.Dtos;

public sealed class CreateRunRequest
{
    [Required]
    [StringLength(200)]
    public required string TriggeredBy { get; init; }

    [Required]
    [StringLength(64)]
    public required string CommitSha { get; init; }
}
