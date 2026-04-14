using System.Text.Json;
using Eskineria.Core.Caching.Abstractions;
using Eskineria.Core.Caching.Configuration;
using Eskineria.Core.Caching.Security;
using Eskineria.Core.Caching.Utilities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Eskineria.Core.Caching.Implementations;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly int _maxKeyLength;
    private readonly int _removeByPrefixBatchSize;
    private readonly CacheEncryptionProvider? _encryptionProvider;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis, 
        ILogger<RedisCacheService> logger,
        CacheOptions options)
    {
        _redis = redis;
        _logger = logger;
        _keyPrefix = options.KeyPrefix;
        _maxKeyLength = options.MaxKeyLength;
        _removeByPrefixBatchSize = options.RedisRemoveByPrefixBatchSize;

        if (!string.IsNullOrEmpty(options.EncryptionKey))
        {
            _encryptionProvider = new CacheEncryptionProvider(options.EncryptionKey, options.PreviousEncryptionKeys);
        }

    }

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IDatabase Database => _redis.GetDatabase();

    private string GetKey(string key) => $"{_keyPrefix}{key}";

    public async Task<T?> GetAsync<T>(string key)
    {
        var cacheKey = GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength));
        var redisValue = await Database.StringGetAsync(cacheKey);
        if (redisValue.IsNullOrEmpty) return default;

        string json = redisValue.ToString();

        if (_encryptionProvider != null)
        {
            try 
            {
                json = _encryptionProvider.Decrypt(json);
            }
            catch (Exception)
            {
                return default;
            }
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON payload in redis cache for key {CacheKey}.", cacheKey);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (value == null) return;
        key = CacheKeyGuard.EnsureValidKey(key, _maxKeyLength);

        string json = JsonSerializer.Serialize(value, DefaultJsonOptions);

        if (_encryptionProvider != null)
        {
            json = _encryptionProvider.Encrypt(json);
        }

        if (expiration.HasValue)
        {
            await Database.StringSetAsync(GetKey(key), json, expiration.Value);
        }
        else
        {
            await Database.StringSetAsync(GetKey(key), json);
        }
    }

    public async Task RemoveAsync(string key)
    {
        await Database.KeyDeleteAsync(GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength)));
    }

    public async Task RemoveByPrefixAsync(string prefixKey)
    {
        var fullPrefix = GetKey(CacheKeyGuard.EnsureValidPrefix(prefixKey, _maxKeyLength));
        var endpoints = _redis.GetEndPoints();

        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsConnected)
            {
                continue;
            }

            try
            {
                var batch = new List<RedisKey>(_removeByPrefixBatchSize);
                foreach (var key in server.Keys(database: Database.Database, pattern: fullPrefix + "*"))
                {
                    batch.Add(key);
                    if (batch.Count < _removeByPrefixBatchSize)
                    {
                        continue;
                    }

                    await Database.KeyDeleteAsync(batch.ToArray());
                    batch.Clear();
                }

                if (batch.Count > 0)
                {
                    await Database.KeyDeleteAsync(batch.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis RemoveByPrefix failed on endpoint {RedisEndpoint}.", endpoint);
            }
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await Database.KeyExistsAsync(GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength)));
    }
}
