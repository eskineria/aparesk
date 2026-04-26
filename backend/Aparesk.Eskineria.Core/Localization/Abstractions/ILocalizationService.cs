using Aparesk.Eskineria.Core.Localization.Entities;

namespace Aparesk.Eskineria.Core.Localization.Abstractions;

public interface ILocalizationService
{
    Task<Dictionary<string, string>> GetResourcesAsync(string lang, CancellationToken cancellationToken = default);
    Task<LocalizationListResult> GetListAsync(
        string? search,
        string? culture,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<LanguageResource?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<LocalizationCreateResult> CreateAsync(
        CreateLocalizationRequest request,
        bool saveAsDraft = false,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);
    Task<LanguageResource?> UpdateValueAsync(
        int id,
        string value,
        bool saveAsDraft = false,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);
    Task<LocalizationPublishResult> PublishAsync(
        IReadOnlyCollection<int> ids,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<LocalizationDeleteCultureResult> DeleteCultureAsync(string culture, CancellationToken cancellationToken = default);
    Task<List<string>> GetCulturesAsync(CancellationToken cancellationToken = default);
    Task<LocalizationCloneCultureResult> CloneCultureAsync(
        string sourceCulture,
        string targetCulture,
        CancellationToken cancellationToken = default);
    Task<LocalizationMissingKeysResult> GetMissingKeysAsync(string? culture, CancellationToken cancellationToken = default);
}

public sealed record CreateLocalizationRequest
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public required string Culture { get; init; }
    public string ResourceSet { get; init; } = "Backend";
}

public sealed record UpdateLocalizationRequest
{
    public required string Value { get; init; }
}

public sealed record LocalizationListResult(IReadOnlyList<LanguageResource> Items, int TotalCount);

public sealed record LocalizationCreateResult(bool IsDuplicate, LanguageResource? Resource);

public sealed record LocalizationDeleteCultureResult(
    bool Success,
    string? MatchedCulture,
    int DeletedCount,
    LocalizationDeleteCultureFailureReason FailureReason,
    string? ErrorMessage);

public sealed record LocalizationCloneCultureResult(bool SourceCultureMissing, int ClonedCount);

public sealed record LocalizationPublishResult(
    int RequestedCount,
    int PublishedCount,
    List<int> MissingIds);

public sealed record LocalizationMissingKeysResult(
    string RequestedCulture,
    string? MatchedCulture,
    string? FallbackCulture,
    List<string> MissingKeys);

public enum LocalizationDeleteCultureFailureReason
{
    None = 0,
    CultureNotFound = 1,
    LastCultureMustRemain = 2,
    NoResourcesFound = 3
}
