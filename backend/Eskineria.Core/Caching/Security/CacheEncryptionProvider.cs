using System.Security.Cryptography;
using System.Text;

namespace Eskineria.Core.Caching.Security;

public class CacheEncryptionProvider
{
    private const string V2Prefix = "v2:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _currentKey;
    private readonly List<byte[]> _previousKeys; // SECURITY: For key rotation support

    public CacheEncryptionProvider(string base64Key, string[]? previousBase64Keys = null)
    {
        if (string.IsNullOrEmpty(base64Key))
            throw new ArgumentNullException(nameof(base64Key));

        _currentKey = Convert.FromBase64String(base64Key);
        
        if (_currentKey.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes (AES-256).", nameof(base64Key));
        
        // SECURITY: Load previous keys for rotation support
        _previousKeys = new List<byte[]>();
        if (previousBase64Keys != null)
        {
            foreach (var prevKey in previousBase64Keys)
            {
                if (!string.IsNullOrEmpty(prevKey))
                {
                    var keyBytes = Convert.FromBase64String(prevKey);
                    if (keyBytes.Length == 32)
                    {
                        _previousKeys.Add(keyBytes);
                    }
                }
            }
        }
    }

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aesGcm = new AesGcm(_currentKey, TagSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);

        return $"{V2Prefix}{Convert.ToBase64String(payload)}";
    }

    /// <summary>
    /// SECURITY: Decrypts with key rotation support.
    /// Tries current key first, then falls back to previous keys.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        var isV2 = cipherText.StartsWith(V2Prefix, StringComparison.Ordinal);

        // Try current key first
        try
        {
            return isV2
                ? DecryptV2WithKey(cipherText, _currentKey)
                : DecryptLegacyCbcWithKey(cipherText, _currentKey);
        }
        catch (CryptographicException)
        {
            // SECURITY: Fallback to previous keys for rotation support
            foreach (var previousKey in _previousKeys)
            {
                try
                {
                    return isV2
                        ? DecryptV2WithKey(cipherText, previousKey)
                        : DecryptLegacyCbcWithKey(cipherText, previousKey);
                }
                catch (CryptographicException)
                {
                    // Try next key
                    continue;
                }
            }
            
            // All keys failed
            throw new CryptographicException("Failed to decrypt cache value. Data may be corrupted or encrypted with an unknown key.");
        }
    }

    private static string DecryptV2WithKey(string cipherText, byte[] key)
    {
        if (!cipherText.StartsWith(V2Prefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Invalid cipher format.");
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(cipherText[V2Prefix.Length..]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Invalid base64 payload.", ex);
        }

        if (payload.Length <= NonceSize + TagSize)
        {
            throw new CryptographicException("Invalid encrypted payload.");
        }

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipher = new byte[payload.Length - NonceSize - TagSize];

        Buffer.BlockCopy(payload, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(payload, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(payload, NonceSize + TagSize, cipher, 0, cipher.Length);

        var plain = new byte[cipher.Length];
        using var aesGcm = new AesGcm(key, TagSize);
        aesGcm.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private static string DecryptLegacyCbcWithKey(string cipherText, byte[] key)
    {
        byte[] fullCipher;
        try
        {
            fullCipher = Convert.FromBase64String(cipherText);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Invalid base64 payload.", ex);
        }

        using var aes = Aes.Create();
        aes.Key = key;

        // Extract IV (first 16 bytes for AES block size)
        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}
