using System.Collections.Concurrent;
using System.Text.Json;
using Aparesk.Eskineria.Core.Caching.Abstractions;
using Aparesk.Eskineria.Core.Caching.Configuration;
using Aparesk.Eskineria.Core.Caching.Security;
using Aparesk.Eskineria.Core.Caching.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Caching.Implementations;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);
    private readonly string _keyPrefix;
    private readonly int _maxKeyLength;
    private readonly CacheEncryptionProvider? _encryptionProvider;
    private readonly ILogger<MemoryCacheService> _logger;
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        CacheOptions options)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _keyPrefix = options.KeyPrefix;
        _maxKeyLength = options.MaxKeyLength;

        if (!string.IsNullOrEmpty(options.EncryptionKey))
        {
            _encryptionProvider = new CacheEncryptionProvider(options.EncryptionKey, options.PreviousEncryptionKeys);
        }
    }

    private string GetKey(string key) => $"{_keyPrefix}{key}";

    public Task<T?> GetAsync<T>(string key)
    {
        var cacheKey = GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength));
        if (_memoryCache.TryGetValue(cacheKey, out string? json) && json is not null)
        {
            if (_encryptionProvider != null)
            {
                try 
                {
                    json = _encryptionProvider.Decrypt(json);
                }
                catch 
                {
                    _memoryCache.Remove(cacheKey);
                    _knownKeys.TryRemove(cacheKey, out _);
                    return Task.FromResult(default(T));
                }
            }

            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json, DefaultJsonOptions));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON payload in memory cache for key {CacheKey}. Entry removed.", cacheKey);
                _memoryCache.Remove(cacheKey);
                _knownKeys.TryRemove(cacheKey, out _);
                return Task.FromResult(default(T));
            }
        }

        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (value == null) return Task.CompletedTask;

        var cacheKey = GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength));
        var json = JsonSerializer.Serialize(value, DefaultJsonOptions);
        
        if (_encryptionProvider != null)
        {
            json = _encryptionProvider.Encrypt(json);
        }

        var entryOptions = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            entryOptions.AbsoluteExpirationRelativeToNow = expiration;
        }

        entryOptions.RegisterPostEvictionCallback(static (evictedKey, _, _, state) =>
        {
            if (evictedKey is string keyValue &&
                state is ConcurrentDictionary<string, byte> knownKeys)
            {
                knownKeys.TryRemove(keyValue, out _);
            }
        }, _knownKeys);

        _memoryCache.Set(cacheKey, json, entryOptions);
        _knownKeys[cacheKey] = 0;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        var cacheKey = GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength));
        _memoryCache.Remove(cacheKey);
        _knownKeys.TryRemove(cacheKey, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefixKey)
    {
        var fullPrefix = GetKey(CacheKeyGuard.EnsureValidPrefix(prefixKey, _maxKeyLength));
        var keysToRemove = _knownKeys.Keys.Where(k => k.StartsWith(fullPrefix, StringComparison.Ordinal)).ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _knownKeys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_memoryCache.TryGetValue(GetKey(CacheKeyGuard.EnsureValidKey(key, _maxKeyLength)), out _));
    }
}
