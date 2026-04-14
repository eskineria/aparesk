using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Eskineria.Application.Utilities;

public static class StringExtensions
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex InvalidSeoCharsRegex = new(@"[^a-z0-9\-_]", RegexOptions.Compiled);
    private static readonly Regex RepeatedDashRegex = new(@"-{2,}", RegexOptions.Compiled);
    private static readonly Regex SafeExtensionRegex = new(@"^\.[a-z0-9]{1,10}$", RegexOptions.Compiled);

    public static string ToSeoFriendly(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        text = RemoveDiacritics(text).ToLowerInvariant();

        // Specific Turkish character replacements
        text = text.Replace("ç", "c")
                   .Replace("ğ", "g")
                   .Replace("ı", "i")
                   .Replace("ö", "o")
                   .Replace("ş", "s")
                   .Replace("ü", "u");

        // Replace spaces with - and remove non-valid chars.
        text = WhitespaceRegex.Replace(text, "-");
        text = InvalidSeoCharsRegex.Replace(text, string.Empty);

        // Remove multiple hyphens
        text = RepeatedDashRegex.Replace(text, "-");

        // Trim hyphens from ends
        return text.Trim('-');
    }

    public static string ToSeoFriendlyFileName(this string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;

        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var sanitizedName = nameWithoutExtension.ToSeoFriendly();
        var safeName = string.IsNullOrWhiteSpace(sanitizedName) ? "file" : sanitizedName;

        var normalizedExtension = extension.ToLowerInvariant();
        if (!SafeExtensionRegex.IsMatch(normalizedExtension))
        {
            normalizedExtension = string.Empty;
        }

        return $"{safeName}{normalizedExtension}";
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var buffer = new char[normalized.Length];
        var index = 0;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            buffer[index++] = character;
        }

        return new string(buffer, 0, index).Normalize(NormalizationForm.FormC);
    }
}
