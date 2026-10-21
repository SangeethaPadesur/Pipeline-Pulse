using System.ComponentModel.DataAnnotations;

namespace PipelinePulse.Dtos;

public sealed class UpdateRunStatusRequest
{
    [Required]
    [StringLength(32)]
    public required string Status { get; init; }
}
