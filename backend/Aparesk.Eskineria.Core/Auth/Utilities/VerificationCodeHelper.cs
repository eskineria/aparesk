using System.Security.Cryptography;
using System.Text;

namespace Aparesk.Eskineria.Core.Auth.Utilities;

internal static class VerificationCodeHelper
{
    public static string GenerateCode()
    {
        var number = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return number.ToString("D6");
    }

    public static string HashCode(string code, string? securityStamp)
    {
        var salt = Encoding.UTF8.GetBytes((securityStamp ?? "default-stamp").PadRight(16)[..16]);
        return Convert.ToHexString(Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(code),
            salt,
            iterations: 100_000,
            HashAlgorithmName.SHA256,
            outputLength: 32));
    }

    public static bool IsCodeValid(string? expectedHash, string? securityStamp, string code)
    {
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var providedHash = HashCode(code, securityStamp);
        var expectedHashBytes = Encoding.UTF8.GetBytes(expectedHash);
        var providedHashBytes = Encoding.UTF8.GetBytes(providedHash);

        if (expectedHashBytes.Length != providedHashBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedHashBytes, providedHashBytes);
    }
}
