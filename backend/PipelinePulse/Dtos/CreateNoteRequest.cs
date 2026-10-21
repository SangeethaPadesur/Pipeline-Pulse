using System.ComponentModel.DataAnnotations;

namespace PipelinePulse.Dtos;

public sealed class CreateNoteRequest
{
    [Required]
    [StringLength(200)]
    public required string Author { get; init; }

    [Required]
    [StringLength(4000)]
    public required string Body { get; init; }
}
