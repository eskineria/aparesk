using Aparesk.Eskineria.Core.Localization.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Collections.Concurrent;

namespace Aparesk.Eskineria.Core.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<JsonStringLocalizer> _logger;
    private readonly EskineriaLocalizationOptions _options;

    public JsonStringLocalizer(
        ILogger<JsonStringLocalizer> logger,
        EskineriaLocalizationOptions options)
    {
        _logger = logger;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private Dictionary<string, string> GetLocalizations(bool includeParentCultures = true)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var cacheKey = $"{_options.ResourcesPath}|{_options.DefaultCulture}|{culture}|{includeParentCultures}";
        return Cache.GetOrAdd(cacheKey, _ => LoadLocalizations(culture, includeParentCultures));
    }

    private Dictionary<string, string> LoadLocalizations(string culture, bool includeParentCultures)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var culturesToTry = includeParentCultures
            ? BuildFallbackChain(culture)
            : BuildExactChain(culture);

        foreach (var c in culturesToTry)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, _options.ResourcesPath, $"{c}.json");

            if (File.Exists(filePath))
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                    if (loaded != null)
                    {
                        foreach (var kvp in loaded)
                        {
                            dictionary[kvp.Key] = kvp.Value;
                        }

                        _logger.LogDebug(
                            "Loaded {Count} localization keys from {FilePath} for culture {Culture}.",
                            loaded.Count,
                            filePath,
                            c);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading localization file: {FilePath}", filePath);
                }
            }
        }

        return dictionary;
    }

    public LocalizedString this[string name]
    {
        get
        {
            var localizations = GetLocalizations();
            var value = localizations.GetValueOrDefault(name);

            return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localizations = GetLocalizations();
            var value = localizations.GetValueOrDefault(name);
            var format = value ?? name;

            string rendered;
            if (value == null)
            {
                rendered = SafeFormat(name, arguments);
                return new LocalizedString(name, rendered, resourceNotFound: true);
            }

            rendered = SafeFormat(format, arguments);
            return new LocalizedString(name, rendered, resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return GetLocalizations(includeParentCultures)
            .Select(x => new LocalizedString(x.Key, x.Value, false));
    }

    private IEnumerable<string> BuildFallbackChain(string culture)
    {
        var chain = new List<string>();
        AppendCultureVariants(_options.DefaultCulture, chain);
        AppendCultureVariants(culture, chain);

        return chain.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> BuildExactChain(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return Array.Empty<string>();
        }

        return new[] { culture.Trim() };
    }

    private static void AppendCultureVariants(string? culture, List<string> target)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        var normalized = culture.Trim();
        var separatorIndex = normalized.IndexOf('-');
        if (separatorIndex > 0)
        {
            target.Add(normalized[..separatorIndex]);
        }

        target.Add(normalized);
    }

    private string SafeFormat(string format, object[] arguments)
    {
        if (arguments == null || arguments.Length == 0)
        {
            return format;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid localization format string for key/value: {Format}", format);
            return format;
        }
    }
}
