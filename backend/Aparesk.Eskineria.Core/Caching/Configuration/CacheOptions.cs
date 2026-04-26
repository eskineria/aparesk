namespace Aparesk.Eskineria.Core.Caching.Configuration;

public class CacheOptions
{
    public CacheType CacheType { get; set; } = CacheType.Memory;
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public int HybridL1TtlSeconds { get; set; } = 60;
    public int HybridRedisRetryDelaySeconds { get; set; } = 5;
    public int HybridMaxPendingOperations { get; set; } = 5000;
    public int MaxKeyLength { get; set; } = 256;
    public int RedisRemoveByPrefixBatchSize { get; set; } = 1000;
    
    /// <summary>
    /// Mandatory Prefix to isolate application keys.
    /// Example: "Aparesk.Eskineria:PaymentService:"
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Optional encryption key (32 bytes base64 encoded) for AES-256.
    /// If null, encryption is disabled.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Optional previous encryption keys (32 bytes base64 encoded).
    /// Used for key rotation during decryption.
    /// </summary>
    public string[]? PreviousEncryptionKeys { get; set; }
}

public enum CacheType
{
    Memory,
    Redis,
    Hybrid
}
