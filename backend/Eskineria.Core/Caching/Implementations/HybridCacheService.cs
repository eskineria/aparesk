using Eskineria.Core.Caching.Abstractions;
using Eskineria.Core.Caching.Configuration;
using Eskineria.Core.Caching.Utilities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Eskineria.Core.Caching.Implementations;

public class HybridCacheService : ICacheService
{
    private readonly MemoryCacheService _memoryCache;
    private readonly RedisCacheService _redisCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly int _maxKeyLength;
    private readonly int _maxPendingOperations;
    private readonly int _hybridL1TtlSeconds;
    private readonly TimeSpan _redisRetryDelay;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly SemaphoreSlim _redisRecoveryLock = new(1, 1);
    private readonly Queue<PendingRedisOperation> _pendingOperations = new();
    private readonly object _pendingOperationsLock = new();
    private long _redisUnavailableUntilUtcTicks;
    private int _redisAvailable = 1;

    public HybridCacheService(
        MemoryCacheService memoryCache,
        RedisCacheService redisCache,
        IConnectionMultiplexer redis,
        CacheOptions options,
        ILogger<HybridCacheService> logger)
    {
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _redis = redis;
        _keyPrefix = options.KeyPrefix;
        _maxKeyLength = options.MaxKeyLength;
        _maxPendingOperations = options.HybridMaxPendingOperations;
        _hybridL1TtlSeconds = options.HybridL1TtlSeconds;
        _redisRetryDelay = TimeSpan.FromSeconds(options.HybridRedisRetryDelaySeconds);
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        key = CacheKeyGuard.EnsureValidKey(key, _maxKeyLength);

        if (await CanUseRedisAsync())
        {
            try
            {
                var value = await _redisCache.GetAsync<T>(key);
                MarkRedisAvailable();

                if (value is not null)
                {
                    var redisTtl = await GetRedisTtlAsync(key);
                    await _memoryCache.SetAsync(key, value, ResolveL1Expiration(redisTtl));
                    return value;
                }
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                MarkRedisUnavailable(ex);
            }
        }

        return await _memoryCache.GetAsync<T>(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (value == null)
        {
            return;
        }
        
        key = CacheKeyGuard.EnsureValidKey(key, _maxKeyLength);

        if (await CanUseRedisAsync())
        {
            try
            {
                await _redisCache.SetAsync(key, value, expiration);
                MarkRedisAvailable();
                await _memoryCache.SetAsync(key, value, ResolveL1Expiration(expiration));
                return;
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                MarkRedisUnavailable(ex);
            }
        }

        EnqueuePendingOperation($"set:{key}", () => _redisCache.SetAsync(key, value, expiration));
        await _memoryCache.SetAsync(key, value, ResolveL1Expiration(expiration));
    }

    public async Task RemoveAsync(string key)
    {
        key = CacheKeyGuard.EnsureValidKey(key, _maxKeyLength);
        await _memoryCache.RemoveAsync(key);

        if (await CanUseRedisAsync())
        {
            try
            {
                await _redisCache.RemoveAsync(key);
                MarkRedisAvailable();
                return;
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                MarkRedisUnavailable(ex);
            }
        }

        EnqueuePendingOperation($"remove:{key}", () => _redisCache.RemoveAsync(key));
    }

    public async Task RemoveByPrefixAsync(string prefixKey)
    {
        prefixKey = CacheKeyGuard.EnsureValidPrefix(prefixKey, _maxKeyLength);
        await _memoryCache.RemoveByPrefixAsync(prefixKey);

        if (await CanUseRedisAsync())
        {
            try
            {
                await _redisCache.RemoveByPrefixAsync(prefixKey);
                MarkRedisAvailable();
                return;
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                MarkRedisUnavailable(ex);
            }
        }

        EnqueuePendingOperation($"remove-prefix:{prefixKey}", () => _redisCache.RemoveByPrefixAsync(prefixKey));
    }

    public async Task<bool> ExistsAsync(string key)
    {
        key = CacheKeyGuard.EnsureValidKey(key, _maxKeyLength);

        if (await CanUseRedisAsync())
        {
            try
            {
                if (await _redisCache.ExistsAsync(key))
                {
                    MarkRedisAvailable();
                    return true;
                }

                MarkRedisAvailable();
            }
            catch (Exception ex) when (IsRedisFailure(ex))
            {
                MarkRedisUnavailable(ex);
            }
        }

        return await _memoryCache.ExistsAsync(key);
    }

    private async Task<TimeSpan?> GetRedisTtlAsync(string key)
    {
        var redisKey = $"{_keyPrefix}{key}";
        var ttl = await _redis.GetDatabase().KeyTimeToLiveAsync(redisKey);

        if (!ttl.HasValue || ttl.Value <= TimeSpan.Zero)
        {
            return null;
        }

        return ttl;
    }

    private TimeSpan? ResolveL1Expiration(TimeSpan? sourceExpiration)
    {
        if (_hybridL1TtlSeconds <= 0)
        {
            return sourceExpiration;
        }

        var configuredL1Ttl = TimeSpan.FromSeconds(_hybridL1TtlSeconds);
        if (!sourceExpiration.HasValue || sourceExpiration.Value <= TimeSpan.Zero)
        {
            return configuredL1Ttl;
        }

        return sourceExpiration.Value < configuredL1Ttl
            ? sourceExpiration
            : configuredL1Ttl;
    }

    private async Task<bool> CanUseRedisAsync()
    {
        if (DateTime.UtcNow.Ticks < Interlocked.Read(ref _redisUnavailableUntilUtcTicks))
        {
            return false;
        }

        return await TryReplayPendingOperationsAsync();
    }

    private async Task<bool> TryReplayPendingOperationsAsync()
    {
        if (!HasPendingOperations())
        {
            return true;
        }

        if (!await _redisRecoveryLock.WaitAsync(0))
        {
            return true;
        }

        try
        {
            while (true)
            {
                PendingRedisOperation? operation;
                lock (_pendingOperationsLock)
                {
                    operation = _pendingOperations.Count > 0 ? _pendingOperations.Peek() : null;
                }

                if (operation is null)
                {
                    MarkRedisAvailable();
                    return true;
                }

                try
                {
                    await operation.ExecuteAsync();
                    lock (_pendingOperationsLock)
                    {
                        if (_pendingOperations.Count > 0 &&
                            ReferenceEquals(_pendingOperations.Peek(), operation))
                        {
                            _pendingOperations.Dequeue();
                        }
                    }
                }
                catch (Exception ex) when (IsRedisFailure(ex))
                {
                    MarkRedisUnavailable(ex);
                    return false;
                }
            }
        }
        finally
        {
            _redisRecoveryLock.Release();
        }
    }

    private void EnqueuePendingOperation(string description, Func<Task> executeAsync)
    {
        lock (_pendingOperationsLock)
        {
            if (_pendingOperations.Count >= _maxPendingOperations)
            {
                var dropped = _pendingOperations.Dequeue();
                _logger.LogWarning(
                    "Hybrid cache pending operation queue limit reached ({MaxPendingOperations}). Oldest operation dropped: {DroppedOperation}.",
                    _maxPendingOperations,
                    dropped.Description);
            }

            _pendingOperations.Enqueue(new PendingRedisOperation(description, executeAsync));
        }
    }

    private bool HasPendingOperations()
    {
        lock (_pendingOperationsLock)
        {
            return _pendingOperations.Count > 0;
        }
    }

    private void MarkRedisUnavailable(Exception ex)
    {
        Interlocked.Exchange(ref _redisUnavailableUntilUtcTicks, DateTime.UtcNow.Add(_redisRetryDelay).Ticks);
        if (Interlocked.Exchange(ref _redisAvailable, 0) == 1)
        {
            _logger.LogWarning(ex, "Redis unavailable. Hybrid cache switched to in-memory fallback.");
        }
    }

    private void MarkRedisAvailable()
    {
        Interlocked.Exchange(ref _redisUnavailableUntilUtcTicks, 0);
        if (Interlocked.Exchange(ref _redisAvailable, 1) == 0)
        {
            _logger.LogInformation("Redis connection restored. Hybrid cache resumed Redis as primary cache.");
        }
    }

    private static bool IsRedisFailure(Exception ex)
    {
        return ex is RedisException or TimeoutException;
    }

    private sealed record PendingRedisOperation(string Description, Func<Task> ExecuteAsync);
}
