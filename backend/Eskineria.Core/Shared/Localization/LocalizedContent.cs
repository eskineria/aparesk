using System.Text.Json.Serialization;
using System.Globalization;

namespace Eskineria.Core.Shared.Localization;

public class LocalizedContent : Dictionary<string, string>
{
    public LocalizedContent() : base(StringComparer.OrdinalIgnoreCase) { }

    public LocalizedContent(IDictionary<string, string> dictionary) : base(dictionary ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase) { }

    [JsonIgnore]
    public string CurrentValue => GetValue(System.Globalization.CultureInfo.CurrentUICulture.Name);

    public string GetValue(string culture)
    {
        var normalizedCulture = NormalizeCulture(culture);
        if (TryGetNonEmptyValue(normalizedCulture, out var exactValue))
        {
            return exactValue;
        }

        var neutralCulture = GetNeutralCulture(normalizedCulture);
        if (TryGetBestNeutralValue(neutralCulture, out var neutralValue))
        {
            return neutralValue;
        }

        // Explicit fallback to English to keep a predictable default.
        if (TryGetNonEmptyValue("en-US", out var englishUsValue))
        {
            return englishUsValue;
        }

        if (TryGetBestNeutralValue("en", out var englishValue))
        {
            return englishValue;
        }

        // Last resort: first non-empty value.
        return Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    private static string NormalizeCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "en-US";
        }

        var normalized = culture.Trim().Replace('_', '-');
        try
        {
            return CultureInfo.GetCultureInfo(normalized).Name;
        }
        catch (CultureNotFoundException)
        {
            return "en-US";
        }
    }

    private static string GetNeutralCulture(string culture)
    {
        var separatorIndex = culture.IndexOf('-');
        return separatorIndex > 0 ? culture[..separatorIndex] : culture;
    }

    private bool TryGetNonEmptyValue(string culture, out string value)
    {
        if (TryGetValue(culture, out var exact) && !string.IsNullOrWhiteSpace(exact))
        {
            value = exact;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private bool TryGetBestNeutralValue(string neutralCulture, out string value)
    {
        string? bestKey = null;
        foreach (var key in Keys)
        {
            if (!string.Equals(GetNeutralCulture(key), neutralCulture, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (bestKey == null || CompareCandidateCultureKey(key, bestKey) < 0)
            {
                bestKey = key;
            }
        }

        if (bestKey != null && TryGetValue(bestKey, out var candidate) && !string.IsNullOrWhiteSpace(candidate))
        {
            value = candidate;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static int CompareCandidateCultureKey(string left, string right)
    {
        var leftPriority = left.Contains('-', StringComparison.Ordinal) ? 1 : 0;
        var rightPriority = right.Contains('-', StringComparison.Ordinal) ? 1 : 0;
        if (leftPriority != rightPriority)
        {
            return leftPriority.CompareTo(rightPriority);
        }

        var lengthComparison = left.Length.CompareTo(right.Length);
        if (lengthComparison != 0)
        {
            return lengthComparison;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(left, right);
    }
}
