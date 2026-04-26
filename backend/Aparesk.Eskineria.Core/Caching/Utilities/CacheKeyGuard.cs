namespace Aparesk.Eskineria.Core.Caching.Utilities;

internal static class CacheKeyGuard
{
    private static readonly char[] ReservedPatternChars = ['*', '?', '[', ']'];

    public static string EnsureValidKey(string key, int maxKeyLength)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        }

        var normalized = key.Trim();
        if (normalized.Length > maxKeyLength)
        {
            throw new ArgumentException($"Cache key length cannot exceed {maxKeyLength}.", nameof(key));
        }

        return normalized;
    }

    public static string EnsureValidPrefix(string prefix, int maxKeyLength)
    {
        var normalized = EnsureValidKey(prefix, maxKeyLength);
        if (normalized.IndexOfAny(ReservedPatternChars) >= 0)
        {
            throw new ArgumentException("Cache prefix cannot contain wildcard pattern characters.", nameof(prefix));
        }

        return normalized;
    }
}
