using Eskineria.Core.Caching.Security;

namespace Eskineria.Core.Auth.Utilities;

public static class AuthSensitiveDataProtector
{
    private const string Prefix = "enc:";
    private static readonly object Sync = new();

    private static CacheEncryptionProvider? _provider;

    public static void Configure(string encryptionKey, string[]? previousEncryptionKeys = null)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new InvalidOperationException("Auth data encryption key must be configured.");
        }

        lock (Sync)
        {
            _provider = new CacheEncryptionProvider(encryptionKey, previousEncryptionKeys);
        }
    }

    public static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        var provider = _provider;
        if (provider == null || plainText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return plainText;
        }

        return $"{Prefix}{provider.Encrypt(plainText)}";
    }

    public static string? Decrypt(string? encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        if (!encryptedText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            // Backward compatibility for legacy plaintext rows.
            return encryptedText;
        }

        var provider = _provider;
        if (provider == null)
        {
            return encryptedText;
        }

        return provider.Decrypt(encryptedText[Prefix.Length..]);
    }
}
