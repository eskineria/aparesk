namespace Eskineria.Core.Notifications.Templates.Entities;

public class EmailTemplate
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Culture { get; set; } = "en-US";
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string RequiredVariables { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public bool IsDraft { get; set; } = true;
    public int CurrentVersion { get; set; } = 1;
    public int? PublishedVersion { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByUserId { get; set; }
    public string? AutoTranslatedFromCulture { get; set; }
    public DateTime? AutoTranslatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<EmailTemplateRevision> Revisions { get; set; } = new List<EmailTemplateRevision>();
}
