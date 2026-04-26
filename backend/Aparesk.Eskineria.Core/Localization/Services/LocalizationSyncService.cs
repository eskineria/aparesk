using System.Text.Json;
using Aparesk.Eskineria.Core.Localization.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Localization.Services;

public sealed class LocalizationSyncService
{
    private readonly DbContext _dbContext;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LocalizationSyncService> _logger;

    public LocalizationSyncService(
        DbContext dbContext,
        IHostEnvironment environment,
        ILogger<LocalizationSyncService> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    public async Task SyncAsync()
    {
        try
        {
            var paths = new List<(string Path, string ResourceSet)>
            {
                (Path.Combine(_environment.ContentRootPath, "Localization"), "Backend"),
                (Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "frontend", "src", "locales")), "Frontend")
            };

            foreach (var (path, resourceSet) in paths)
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }

                var files = Directory.GetFiles(path, "*.json");
                var insertedTotal = 0;
                var updatedTotal = 0;

                foreach (var file in files)
                {
                    _logger.LogInformation("Processing localization file: {File} for set {Set}", file, resourceSet);
                    var culture = Path.GetFileNameWithoutExtension(file);
                    var json = await File.ReadAllTextAsync(file);
                    var dict = FlattenJson(json);

                    if (dict.Count == 0)
                    {
                        continue;
                    }

                    var existingResources = await _dbContext.Set<LanguageResource>()
                        .Where(x => x.Culture == culture)
                        .ToListAsync();

                    var existingByKey = existingResources
                        .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                    var normalizedEntries = dict
                        .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .ToList();

                    var insertedCount = 0;
                    var updatedCount = 0;

                    foreach (var entry in normalizedEntries)
                    {
                        if (existingByKey.TryGetValue(entry.Key, out var existing))
                        {
                            var sameResourceSet = string.Equals(existing.ResourceSet, resourceSet, StringComparison.Ordinal);

                            // Protect existing values from cross-resource-set overwrite.
                            if (!sameResourceSet &&
                                !string.IsNullOrWhiteSpace(existing.ResourceSet) &&
                                !string.Equals(existing.Value, entry.Value, StringComparison.Ordinal))
                            {
                                _logger.LogWarning(
                                    "Localization key collision skipped. Culture={Culture}, Key={Key}, ExistingSet={ExistingSet}, IncomingSet={IncomingSet}",
                                    culture,
                                    entry.Key,
                                    existing.ResourceSet,
                                    resourceSet);
                                continue;
                            }

                            if (!string.Equals(existing.Value, entry.Value, StringComparison.Ordinal) ||
                                (!string.IsNullOrWhiteSpace(resourceSet) &&
                                 (string.IsNullOrWhiteSpace(existing.ResourceSet) || sameResourceSet)))
                            {
                                existing.Value = entry.Value;
                                if (string.IsNullOrWhiteSpace(existing.ResourceSet) || sameResourceSet)
                                {
                                    existing.ResourceSet = resourceSet;
                                }

                                updatedCount++;
                            }

                            continue;
                        }

                        _dbContext.Set<LanguageResource>().Add(new LanguageResource
                        {
                            Key = entry.Key,
                            Value = entry.Value,
                            Culture = culture,
                            ResourceSet = resourceSet
                        });
                        insertedCount++;
                    }

                    if (insertedCount == 0 && updatedCount == 0)
                    {
                        continue;
                    }

                    await _dbContext.SaveChangesAsync();

                    insertedTotal += insertedCount;
                    updatedTotal += updatedCount;

                    _logger.LogInformation(
                        "Localization synced for {Culture} ({ResourceSet}): +{InsertedCount}, ~{UpdatedCount}",
                        culture,
                        resourceSet,
                        insertedCount,
                        updatedCount);
                }

                if (insertedTotal > 0 || updatedTotal > 0)
                {
                    _logger.LogInformation(
                        "Localization sync completed for {ResourceSet}: +{InsertedTotal}, ~{UpdatedTotal}",
                        resourceSet,
                        insertedTotal,
                        updatedTotal);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed localization data");
        }
    }

    private static Dictionary<string, string> FlattenJson(string json)
    {
        var dict = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(json);
        FlattenElement(document.RootElement, string.Empty, dict);
        return dict;
    }

    private static void FlattenElement(JsonElement element, string prefix, Dictionary<string, string> dict)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                FlattenElement(property.Value, newPrefix, dict);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                FlattenElement(item, $"{prefix}.{index++}", dict);
            }
        }
        else
        {
            dict[prefix] = element.ToString();
        }
    }
}
