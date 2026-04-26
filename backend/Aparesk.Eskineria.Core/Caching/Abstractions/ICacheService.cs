namespace Aparesk.Eskineria.Core.Caching.Abstractions;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefixKey);
    Task<bool> ExistsAsync(string key);
}
