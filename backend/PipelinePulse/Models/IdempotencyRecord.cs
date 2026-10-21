namespace PipelinePulse.Models;

public class IdempotencyRecord
{
    public required string Key { get; set; }

    public required string RequestHash { get; set; }

    public required string ResponseBody { get; set; }

    public required string ResponseLocation { get; set; }
}
