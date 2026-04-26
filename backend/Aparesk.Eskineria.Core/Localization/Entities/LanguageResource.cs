namespace Aparesk.Eskineria.Core.Localization.Entities;

public class LanguageResource
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? DraftValue { get; set; }
    public string Culture { get; set; } = string.Empty; // e.g. "tr-TR"
    public string? ResourceSet { get; set; } // "Frontend", "Backend", "Common" (Optional)
    public string WorkflowStatus { get; set; } = "Published";
    public string? OwnerUserId { get; set; }
    public DateTime? LastPublishedAtUtc { get; set; }
    public string? LastPublishedByUserId { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedByUserId { get; set; }
}
