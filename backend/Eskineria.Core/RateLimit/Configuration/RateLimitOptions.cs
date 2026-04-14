namespace Eskineria.Core.RateLimit.Configuration;

public class RateLimitOptions
{
    public bool EnableGlobalLimiter { get; set; } = true;
    public GlobalRateLimitPolicy Global { get; set; } = new();
    public List<string> ExcludedPathPrefixes { get; set; } = new();
    public List<CustomRateLimitPolicy> Policies { get; set; } = new();
}

public class GlobalRateLimitPolicy
{
    public int PermitLimit { get; set; } = 30;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
    public int AuthenticatedPermitLimit { get; set; } = 180;
    public int AuthenticatedWindowSeconds { get; set; } = 60;
    public int AuthenticatedQueueLimit { get; set; } = 0;
}

public class CustomRateLimitPolicy
{
    public string PolicyName { get; set; } = string.Empty;
    public RateLimitAlgorithm Algorithm { get; set; } = RateLimitAlgorithm.FixedWindow;
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
    public int SegmentsPerWindow { get; set; } = 1; // For Sliding Window
    public int TokenLimit { get; set; } = 5; // For Token Bucket
    public int TokensPerPeriod { get; set; } = 1; // For Token Bucket
    public int PeriodSeconds { get; set; } = 10; // For Token Bucket
    public int QueueLimit { get; set; } = 0;
}

public enum RateLimitAlgorithm
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
    Concurrency
}
