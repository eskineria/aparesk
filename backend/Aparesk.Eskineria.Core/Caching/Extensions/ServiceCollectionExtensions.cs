using Aparesk.Eskineria.Core.Caching.Abstractions;
using Aparesk.Eskineria.Core.Caching.Configuration;
using Aparesk.Eskineria.Core.Caching.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Aparesk.Eskineria.Core.Caching.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEskineriaCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Caching")
    {
        return services.AddEskineriaCaching(options =>
        {
            configuration.GetSection(sectionName).Bind(options);
        });
    }

    public static IServiceCollection AddEskineriaCaching(
        this IServiceCollection services, 
        Action<CacheOptions> configureOptions)
    {
        var options = new CacheOptions();
        configureOptions(options);
        ValidateAndNormalize(options);

        services.AddSingleton(options);

        switch (options.CacheType)
        {
            case CacheType.Redis:
                RegisterRedisMultiplexer(services, options.RedisConnectionString);
                services.AddSingleton<ICacheService, RedisCacheService>();
                break;
            case CacheType.Memory:
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, MemoryCacheService>();
                break;
            case CacheType.Hybrid:
                services.AddMemoryCache();
                RegisterRedisMultiplexer(services, options.RedisConnectionString);
                services.AddSingleton<MemoryCacheService>();
                services.AddSingleton<RedisCacheService>();
                services.AddSingleton<ICacheService, HybridCacheService>();
                break;
        }

        return services;
    }

    private static void ValidateAndNormalize(CacheOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.KeyPrefix))
        {
            throw new InvalidOperationException("CacheOptions.KeyPrefix is required and cannot be empty.");
        }

        options.KeyPrefix = NormalizePrefix(options.KeyPrefix);

        if ((options.CacheType == CacheType.Redis || options.CacheType == CacheType.Hybrid) &&
            string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            throw new InvalidOperationException("CacheOptions.RedisConnectionString is required when CacheType is Redis or Hybrid.");
        }

        options.HybridL1TtlSeconds = options.HybridL1TtlSeconds <= 0
            ? 60
            : Math.Clamp(options.HybridL1TtlSeconds, 1, 3600);

        options.HybridRedisRetryDelaySeconds = options.HybridRedisRetryDelaySeconds <= 0
            ? 5
            : Math.Clamp(options.HybridRedisRetryDelaySeconds, 1, 300);

        options.HybridMaxPendingOperations = options.HybridMaxPendingOperations <= 0
            ? 5000
            : Math.Clamp(options.HybridMaxPendingOperations, 100, 50000);

        options.MaxKeyLength = options.MaxKeyLength <= 0
            ? 256
            : Math.Clamp(options.MaxKeyLength, 32, 1024);

        options.RedisRemoveByPrefixBatchSize = options.RedisRemoveByPrefixBatchSize <= 0
            ? 1000
            : Math.Clamp(options.RedisRemoveByPrefixBatchSize, 100, 5000);

        ValidateEncryptionKey(options.EncryptionKey, "CacheOptions.EncryptionKey");
        if (options.PreviousEncryptionKeys is { Length: > 0 })
        {
            foreach (var previousKey in options.PreviousEncryptionKeys)
            {
                ValidateEncryptionKey(previousKey, "CacheOptions.PreviousEncryptionKeys");
            }
        }
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalized = prefix.Trim();
        if (!normalized.EndsWith(":", StringComparison.Ordinal))
        {
            normalized += ":";
        }

        return normalized;
    }

    private static void ValidateEncryptionKey(string? key, string optionName)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"{optionName} must be base64 encoded.", ex);
        }

        if (keyBytes.Length != 32)
        {
            throw new InvalidOperationException($"{optionName} must decode to exactly 32 bytes.");
        }
    }

    private static void RegisterRedisMultiplexer(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configuration = ConfigurationOptions.Parse(connectionString);
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });
    }
}
