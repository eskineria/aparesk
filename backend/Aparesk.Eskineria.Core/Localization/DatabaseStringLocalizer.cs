using System.Collections.Concurrent;
using System.Globalization;
using Aparesk.Eskineria.Core.Localization.Entities;
using Aparesk.Eskineria.Core.Shared.Configuration;
using Aparesk.Eskineria.Core.Shared.Localization;
using Aparesk.Eskineria.Core.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Localization;

public sealed class DatabaseStringLocalizer : IStringLocalizer
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new(StringComparer.OrdinalIgnoreCase);
    private const string WorkflowStatusPublished = "Published";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseStringLocalizer> _logger;
    private readonly string? _resourceSet;

    private sealed record CacheEntry(Dictionary<string, string> Values, DateTime ExpiresAtUtc);

    public DatabaseStringLocalizer(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseStringLocalizer> logger,
        string? resourceSet)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _resourceSet = resourceSet;
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }

    public LocalizedString this[string name]
    {
        get
        {
            var values = GetLocalizations();
            var value = values.GetValueOrDefault(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value == null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var values = GetLocalizations();
            var value = values.GetValueOrDefault(name);
            var format = value ?? name;

            try
            {
                return new LocalizedString(name, string.Format(format, arguments), resourceNotFound: value == null);
            }
            catch (FormatException)
            {
                return new LocalizedString(name, format, resourceNotFound: value == null);
            }
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return GetLocalizations().Select(x => new LocalizedString(x.Key, x.Value, false));
    }

    private Dictionary<string, string> GetLocalizations()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var cacheKey = $"{_resourceSet ?? "*"}::{culture}";

        if (Cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
        {
            return entry.Values;
        }

        var values = LoadLocalizations(culture);
        Cache[cacheKey] = new CacheEntry(values, DateTime.UtcNow.Add(CacheTtl));
        return values;
    }

    private Dictionary<string, string> LoadLocalizations(string culture)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var configuredFallbackCulture = ReadFallbackCulture(dbContext);
            var cultures = BuildFallbackChain(culture, configuredFallbackCulture);

            var query = dbContext.Set<LanguageResource>()
                .AsNoTracking()
                .Where(x =>
                    cultures.Contains(x.Culture) &&
                    x.WorkflowStatus == WorkflowStatusPublished);

            if (!string.IsNullOrWhiteSpace(_resourceSet))
            {
                query = query.Where(x => x.ResourceSet == _resourceSet || x.ResourceSet == null);
            }

            var allResources = query
                .Select(x => new { x.Culture, x.Key, x.Value })
                .ToList();

            foreach (var fallbackCulture in cultures.Reverse<string>())
            {
                var currentCultureItems = allResources.Where(x => string.Equals(x.Culture, fallbackCulture, StringComparison.OrdinalIgnoreCase));
                foreach (var resource in currentCultureItems)
                {
                    dictionary[resource.Key] = resource.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localization resources from database for culture {Culture}", culture);
        }

        return dictionary;
    }

    private static string? ReadFallbackCulture(DbContext dbContext)
    {
        var value = dbContext.Set<Setting>()
            .AsNoTracking()
            .Where(x => x.Name == SystemSettingKeys.SystemLocalizationFallbackCulture)
            .Select(x => x.Value)
            .FirstOrDefault();

        return value?.Trim();
    }

    private static List<string> BuildFallbackChain(string culture, string? configuredFallbackCulture)
    {
        var cultures = new List<string>();

        if (!string.IsNullOrWhiteSpace(culture))
        {
            cultures.Add(culture);

            var separatorIndex = culture.IndexOf('-');
            if (separatorIndex > 0)
            {
                cultures.Add(culture[..separatorIndex]);
            }
        }

        if (!string.IsNullOrWhiteSpace(configuredFallbackCulture))
        {
            cultures.Add(configuredFallbackCulture);
        }

        if (!cultures.Contains("en-US", StringComparer.OrdinalIgnoreCase))
        {
            cultures.Add("en-US");
        }

        return cultures
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
