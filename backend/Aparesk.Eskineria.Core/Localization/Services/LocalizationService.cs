using Aparesk.Eskineria.Core.Localization.Abstractions;
using Aparesk.Eskineria.Core.Auditing.Abstractions;
using Aparesk.Eskineria.Core.Auditing.Models;
using Aparesk.Eskineria.Core.Repository.Specification;
using Aparesk.Eskineria.Core.Localization.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Aparesk.Eskineria.Core.Localization.Services;

public class LocalizationService : ILocalizationService
{
    private const string DefaultCulture = "en-US";
    private const string DefaultResourceSet = "Backend";
    private const string WorkflowStatusPublished = "Published";
    private const string WorkflowStatusDraft = "Draft";
    private const string WorkflowStatusPendingApproval = "PendingApproval";
    private readonly ILanguageResourceRepository _languageResourceRepository;
    private readonly IAuditingStore _auditingStore;

    public LocalizationService(
        ILanguageResourceRepository languageResourceRepository,
        IAuditingStore auditingStore)
    {
        _languageResourceRepository = languageResourceRepository;
        _auditingStore = auditingStore;
    }

    public async Task<Dictionary<string, string>> GetResourcesAsync(string lang, CancellationToken cancellationToken = default)
    {
        var requestedCulture = NormalizeCultureOrDefault(lang);

        var availableCultures = await _languageResourceRepository.Query()
            .Where(x => x.WorkflowStatus == WorkflowStatusPublished)
            .Select(x => x.Culture)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (availableCultures.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var fallbackCulture = ResolveCulture(availableCultures, DefaultCulture);
        var matchedCulture = ResolveCulture(availableCultures, requestedCulture);

        var culturesToLoad = new List<string>();
        if (!string.IsNullOrWhiteSpace(fallbackCulture))
        {
            culturesToLoad.Add(fallbackCulture);
        }

        if (!string.IsNullOrWhiteSpace(matchedCulture) &&
            culturesToLoad.All(x => !string.Equals(x, matchedCulture, StringComparison.OrdinalIgnoreCase)))
        {
            culturesToLoad.Add(matchedCulture);
        }

        if (culturesToLoad.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var resources = await _languageResourceRepository.GetListAsync(
            new QuerySpecification<LanguageResource>(x =>
                culturesToLoad.Contains(x.Culture) &&
                x.WorkflowStatus == WorkflowStatusPublished),
            cancellationToken);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in culturesToLoad)
        {
            foreach (var resource in resources.Where(x => string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.Equals(culture, fallbackCulture, StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(resource.Value))
                {
                    continue;
                }

                // Load default culture first, then overwrite with the requested culture.
                result[resource.Key] = resource.Value;
            }
        }

        return result;
    }

    public async Task<LocalizationListResult> GetListAsync(
        string? search,
        string? culture,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var normalizedSearch = NormalizeTextOrNull(search);
        var normalizedCulture = NormalizeTextOrNull(culture);

        var spec = new QuerySpecification<LanguageResource>(x =>
                (normalizedSearch == null ||
                 x.Key.Contains(normalizedSearch) ||
                 x.Value.Contains(normalizedSearch) ||
                 (x.DraftValue != null && x.DraftValue.Contains(normalizedSearch))) &&
                (normalizedCulture == null || x.Culture == normalizedCulture))
            .OrderByDescending(x => x.Id)
            .Paging((page - 1) * pageSize, pageSize);

        var pagedResult = await _languageResourceRepository.GetPagedListAsync(spec, cancellationToken);
        var items = pagedResult.Items.ToList();

        return new LocalizationListResult(items, pagedResult.Count);
    }

    public async Task<LanguageResource?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _languageResourceRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<LocalizationCreateResult> CreateAsync(
        CreateLocalizationRequest request,
        bool saveAsDraft = false,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeRequiredText(request.Key, nameof(request.Key));
        var normalizedValue = request.Value ?? string.Empty;
        var normalizedCulture = NormalizeCultureOrDefault(request.Culture);
        var normalizedResourceSet = NormalizeResourceSetOrDefault(request.ResourceSet);

        var exists = await _languageResourceRepository.GetAsync(
            x => x.Key == normalizedKey && x.Culture == normalizedCulture,
            cancellationToken) != null;

        if (exists)
        {
            await SaveAuditAsync(
                nameof(CreateAsync),
                $"Key: {normalizedKey}, Culture: {normalizedCulture}, ResourceSet: {normalizedResourceSet}, Duplicate: true");
            return new LocalizationCreateResult(IsDuplicate: true, Resource: null);
        }

        var now = DateTime.UtcNow;
        var resource = new LanguageResource
        {
            Key = normalizedKey,
            Culture = normalizedCulture,
            ResourceSet = normalizedResourceSet,
            LastModifiedAtUtc = now,
            LastModifiedByUserId = actorUserId?.ToString()
        };

        if (saveAsDraft)
        {
            resource.DraftValue = normalizedValue;
            resource.Value = string.Empty;
            resource.WorkflowStatus = WorkflowStatusDraft;
            resource.OwnerUserId = actorUserId?.ToString();
        }
        else
        {
            resource.Value = normalizedValue;
            resource.DraftValue = null;
            resource.WorkflowStatus = WorkflowStatusPublished;
            resource.OwnerUserId = actorUserId?.ToString();
            resource.LastPublishedAtUtc = now;
            resource.LastPublishedByUserId = actorUserId?.ToString();
        }

        await _languageResourceRepository.AddAsync(resource, cancellationToken);
        await _languageResourceRepository.SaveChangesAsync(cancellationToken);
        await SaveAuditAsync(
            nameof(CreateAsync),
            $"Key: {resource.Key}, Culture: {resource.Culture}, ResourceSet: {resource.ResourceSet}, Id: {resource.Id}, Duplicate: false, Draft: {saveAsDraft}, WorkflowStatus: {resource.WorkflowStatus}");

        return new LocalizationCreateResult(IsDuplicate: false, Resource: resource);
    }

    public async Task<LanguageResource?> UpdateValueAsync(
        int id,
        string value,
        bool saveAsDraft = false,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedValue = value ?? string.Empty;
        var existing = await _languageResourceRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            await SaveAuditAsync(
                nameof(UpdateValueAsync),
                $"Id: {id}, Found: false");
            return null;
        }

        var actorUserIdString = actorUserId?.ToString();
        if (saveAsDraft)
        {
            existing.DraftValue = normalizedValue;
            existing.WorkflowStatus = WorkflowStatusDraft;
            existing.OwnerUserId ??= actorUserIdString;
        }
        else
        {
            existing.Value = normalizedValue;
            existing.DraftValue = null;
            existing.WorkflowStatus = WorkflowStatusPublished;
            existing.OwnerUserId ??= actorUserIdString;
            existing.LastPublishedAtUtc = DateTime.UtcNow;
            existing.LastPublishedByUserId = actorUserIdString;
        }

        existing.LastModifiedAtUtc = DateTime.UtcNow;
        existing.LastModifiedByUserId = actorUserIdString;

        await _languageResourceRepository.UpdateAsync(existing, cancellationToken);
        await _languageResourceRepository.SaveChangesAsync(cancellationToken);
        await SaveAuditAsync(
            nameof(UpdateValueAsync),
            $"Id: {id}, Key: {existing.Key}, Culture: {existing.Culture}, Found: true, Draft: {saveAsDraft}, WorkflowStatus: {existing.WorkflowStatus}");
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var resource = await _languageResourceRepository.GetByIdAsync(id, cancellationToken);
        if (resource == null)
        {
            await SaveAuditAsync(
                nameof(DeleteAsync),
                $"Id: {id}, Found: false");
            return false;
        }

        var key = resource.Key;
        var culture = resource.Culture;
        var resourceSet = resource.ResourceSet;
        await _languageResourceRepository.DeleteAsync(resource, cancellationToken);
        await _languageResourceRepository.SaveChangesAsync(cancellationToken);
        await SaveAuditAsync(
            nameof(DeleteAsync),
            $"Id: {id}, Key: {key}, Culture: {culture}, ResourceSet: {resourceSet}, Deleted: true");
        return true;
    }

    public async Task<LocalizationPublishResult> PublishAsync(
        IReadOnlyCollection<int> ids,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedIds = ids
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return new LocalizationPublishResult(
                RequestedCount: 0,
                PublishedCount: 0,
                MissingIds: new List<int>());
        }

        var resources = await SpecificationEvaluator<LanguageResource>.GetQuery(
                _languageResourceRepository.Query(asNoTracking: false),
                new QuerySpecification<LanguageResource>(x => normalizedIds.Contains(x.Id)))
            .ToListAsync(cancellationToken);

        var foundIds = resources.Select(x => x.Id).ToHashSet();
        var missingIds = normalizedIds.Where(id => !foundIds.Contains(id)).OrderBy(x => x).ToList();

        var now = DateTime.UtcNow;
        var actorId = actorUserId.ToString();
        var publishedCount = 0;

        foreach (var resource in resources)
        {
            if (resource.DraftValue != null)
            {
                resource.Value = resource.DraftValue;
                resource.DraftValue = null;
                publishedCount++;
            }

            resource.WorkflowStatus = WorkflowStatusPublished;
            resource.OwnerUserId ??= actorId;
            resource.LastPublishedAtUtc = now;
            resource.LastPublishedByUserId = actorId;
            resource.LastModifiedAtUtc = now;
            resource.LastModifiedByUserId = actorId;
        }

        if (resources.Count > 0)
        {
            await _languageResourceRepository.SaveChangesAsync(cancellationToken);
        }

        await SaveAuditAsync(
            nameof(PublishAsync),
            $"RequestedCount: {normalizedIds.Count}, PublishedCount: {publishedCount}, MissingCount: {missingIds.Count}");

        return new LocalizationPublishResult(
            RequestedCount: normalizedIds.Count,
            PublishedCount: publishedCount,
            MissingIds: missingIds);
    }

    public async Task<LocalizationDeleteCultureResult> DeleteCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        var normalizedCulture = NormalizeTextOrNull(culture);
        if (normalizedCulture == null)
        {
            return new LocalizationDeleteCultureResult(
                Success: false,
                MatchedCulture: null,
                DeletedCount: 0,
                FailureReason: LocalizationDeleteCultureFailureReason.CultureNotFound,
                ErrorMessage: "Culture is required.");
        }

        var allCultures = await _languageResourceRepository.Query()
            .Select(x => x.Culture)
            .Distinct()
            .ToListAsync(cancellationToken);

        var matchedCulture = allCultures
            .FirstOrDefault(c => string.Equals(c, normalizedCulture, StringComparison.OrdinalIgnoreCase));

        if (matchedCulture is null)
        {
            await SaveAuditAsync(
                nameof(DeleteCultureAsync),
                $"Culture: {normalizedCulture}, Success: false, Reason: {LocalizationDeleteCultureFailureReason.CultureNotFound}");
            return new LocalizationDeleteCultureResult(
                Success: false,
                MatchedCulture: null,
                DeletedCount: 0,
                FailureReason: LocalizationDeleteCultureFailureReason.CultureNotFound,
                ErrorMessage: $"Culture not found: {normalizedCulture}");
        }

        if (allCultures.Count <= 1)
        {
            await SaveAuditAsync(
                nameof(DeleteCultureAsync),
                $"Culture: {matchedCulture}, Success: false, Reason: {LocalizationDeleteCultureFailureReason.LastCultureMustRemain}");
            return new LocalizationDeleteCultureResult(
                Success: false,
                MatchedCulture: matchedCulture,
                DeletedCount: 0,
                FailureReason: LocalizationDeleteCultureFailureReason.LastCultureMustRemain,
                ErrorMessage: "At least one culture must remain.");
        }

        var resourcesToDelete = await _languageResourceRepository.GetListAsync(
            new QuerySpecification<LanguageResource>(x => x.Culture == matchedCulture),
            cancellationToken);

        if (resourcesToDelete.Count == 0)
        {
            await SaveAuditAsync(
                nameof(DeleteCultureAsync),
                $"Culture: {matchedCulture}, Success: false, Reason: {LocalizationDeleteCultureFailureReason.NoResourcesFound}");
            return new LocalizationDeleteCultureResult(
                Success: false,
                MatchedCulture: matchedCulture,
                DeletedCount: 0,
                FailureReason: LocalizationDeleteCultureFailureReason.NoResourcesFound,
                ErrorMessage: $"No resources found for culture: {matchedCulture}");
        }

        await _languageResourceRepository.DeleteRangeAsync(resourcesToDelete, cancellationToken);
        await _languageResourceRepository.SaveChangesAsync(cancellationToken);
        await SaveAuditAsync(
            nameof(DeleteCultureAsync),
            $"Culture: {matchedCulture}, Success: true, DeletedCount: {resourcesToDelete.Count}");

        return new LocalizationDeleteCultureResult(
            Success: true,
            MatchedCulture: matchedCulture,
            DeletedCount: resourcesToDelete.Count,
            FailureReason: LocalizationDeleteCultureFailureReason.None,
            ErrorMessage: null);
    }

    public async Task<List<string>> GetCulturesAsync(CancellationToken cancellationToken = default)
    {
        return await _languageResourceRepository.Query()
            .Where(x => x.WorkflowStatus == WorkflowStatusPublished)
            .Select(x => x.Culture)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<LocalizationCloneCultureResult> CloneCultureAsync(
        string sourceCulture,
        string targetCulture,
        CancellationToken cancellationToken = default)
    {
        var normalizedSourceCulture = NormalizeRequiredText(sourceCulture, nameof(sourceCulture));
        var normalizedTargetCulture = NormalizeRequiredText(targetCulture, nameof(targetCulture));

        var sourceResources = await _languageResourceRepository.GetListAsync(
            new QuerySpecification<LanguageResource>(x => x.Culture == normalizedSourceCulture),
            cancellationToken);

        if (sourceResources.Count == 0)
        {
            await SaveAuditAsync(
                nameof(CloneCultureAsync),
                $"SourceCulture: {normalizedSourceCulture}, TargetCulture: {normalizedTargetCulture}, Success: false, Reason: SourceCultureMissing");
            return new LocalizationCloneCultureResult(SourceCultureMissing: true, ClonedCount: 0);
        }

        var existingTargetKeys = await _languageResourceRepository.Query()
            .Where(x => x.Culture == normalizedTargetCulture)
            .Select(x => x.Key)
            .ToHashSetAsync(cancellationToken);

        var newResources = sourceResources
            .Where(x => !existingTargetKeys.Contains(x.Key))
            .Select(x => new LanguageResource
            {
                Key = x.Key,
                Value = x.Value,
                DraftValue = null,
                Culture = normalizedTargetCulture,
                ResourceSet = x.ResourceSet,
                WorkflowStatus = WorkflowStatusPublished,
                OwnerUserId = x.OwnerUserId,
                LastPublishedAtUtc = x.LastPublishedAtUtc ?? DateTime.UtcNow,
                LastPublishedByUserId = x.LastPublishedByUserId,
                LastModifiedAtUtc = DateTime.UtcNow,
                LastModifiedByUserId = x.LastModifiedByUserId
            })
            .ToList();

        if (newResources.Count > 0)
        {
            await _languageResourceRepository.AddRangeAsync(newResources, cancellationToken);
            await _languageResourceRepository.SaveChangesAsync(cancellationToken);
        }

        await SaveAuditAsync(
            nameof(CloneCultureAsync),
            $"SourceCulture: {normalizedSourceCulture}, TargetCulture: {normalizedTargetCulture}, Success: true, ClonedCount: {newResources.Count}");

        return new LocalizationCloneCultureResult(SourceCultureMissing: false, ClonedCount: newResources.Count);
    }

    public async Task<LocalizationMissingKeysResult> GetMissingKeysAsync(
        string? culture,
        CancellationToken cancellationToken = default)
    {
        var requestedCulture = NormalizeCultureOrDefault(culture);

        var resources = await _languageResourceRepository.GetListAsync(
            new QuerySpecification<LanguageResource>(x => x.WorkflowStatus == WorkflowStatusPublished),
            cancellationToken);

        var availableCultures = resources
            .Select(x => x.Culture)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var fallbackCulture = ResolveCulture(availableCultures, DefaultCulture);
        var matchedCulture = ResolveCulture(availableCultures, requestedCulture);

        if (string.IsNullOrWhiteSpace(matchedCulture) || string.IsNullOrWhiteSpace(fallbackCulture))
        {
            return new LocalizationMissingKeysResult(
                RequestedCulture: requestedCulture,
                MatchedCulture: matchedCulture,
                FallbackCulture: fallbackCulture,
                MissingKeys: new List<string>());
        }

        var fallbackKeys = resources
            .Where(x => string.Equals(x.Culture, fallbackCulture, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var targetKeys = resources
            .Where(x => string.Equals(x.Culture, matchedCulture, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingKeys = fallbackKeys
            .Where(x => !targetKeys.Contains(x))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LocalizationMissingKeysResult(
            RequestedCulture: requestedCulture,
            MatchedCulture: matchedCulture,
            FallbackCulture: fallbackCulture,
            MissingKeys: missingKeys);
    }

    private Task SaveAuditAsync(string methodName, string parameters)
    {
        return _auditingStore.SaveAsync(new AuditLog
        {
            ServiceName = nameof(LocalizationService),
            MethodName = methodName,
            Parameters = JsonSerializer.Serialize(new
            {
                Before = (object?)null,
                After = ParseAuditParameters(parameters)
            })
        });
    }

    private static Dictionary<string, string?> ParseAuditParameters(string parameters)
    {
        var output = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return output;
        }

        var segments = parameters.Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var separatorIndex = segment.IndexOf(':');
            if (separatorIndex < 0)
            {
                separatorIndex = segment.IndexOf('=');
            }

            if (separatorIndex <= 0 || separatorIndex >= segment.Length - 1)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            var value = segment[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            output[key] = value;
        }

        if (output.Count == 0)
        {
            output["Details"] = parameters.Trim();
        }

        return output;
    }

    private static string? ResolveCulture(IReadOnlyCollection<string> availableCultures, string requestedCulture)
    {
        if (availableCultures.Count == 0 || string.IsNullOrWhiteSpace(requestedCulture))
        {
            return null;
        }

        var normalizedRequested = requestedCulture.Trim();
        var exact = availableCultures.FirstOrDefault(x =>
            string.Equals(x, normalizedRequested, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        var requestedPrefix = normalizedRequested.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
        if (string.IsNullOrWhiteSpace(requestedPrefix))
        {
            return null;
        }

        var prefixExact = availableCultures.FirstOrDefault(x =>
            string.Equals(x, requestedPrefix, StringComparison.OrdinalIgnoreCase));
        if (prefixExact != null)
        {
            return prefixExact;
        }

        return availableCultures.FirstOrDefault(x =>
        {
            var culturePrefix = x.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
            return string.Equals(culturePrefix, requestedPrefix, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string NormalizeCultureOrDefault(string? value)
    {
        var normalized = NormalizeTextOrNull(value);
        return normalized ?? DefaultCulture;
    }

    private static string NormalizeResourceSetOrDefault(string? value)
    {
        var normalized = NormalizeTextOrNull(value);
        return normalized ?? DefaultResourceSet;
    }

    private static string NormalizeRequiredText(string? value, string fieldName)
    {
        var normalized = NormalizeTextOrNull(value);
        if (normalized == null)
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return normalized;
    }

    private static string? NormalizeTextOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
