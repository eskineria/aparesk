namespace Eskineria.Core.RateLimit.Models;

public sealed class RateLimitRejectionData
{
    public int RetryAfterSeconds { get; init; }
    public string? TraceId { get; init; }
}
