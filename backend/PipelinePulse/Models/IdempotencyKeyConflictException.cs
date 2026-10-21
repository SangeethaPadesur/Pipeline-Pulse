namespace PipelinePulse.Models;

public sealed class IdempotencyKeyConflictException : Exception
{
    public IdempotencyKeyConflictException()
        : base("The Idempotency-Key has already been used with a different request body.")
    {
    }
}
