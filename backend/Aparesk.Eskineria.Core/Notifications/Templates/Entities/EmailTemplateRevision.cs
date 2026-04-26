namespace Aparesk.Eskineria.Core.Notifications.Templates.Entities;

public class EmailTemplateRevision
{
    public int Id { get; set; }
    public int EmailTemplateId { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string RequiredVariables { get; set; } = "[]";
    public bool IsPublishedSnapshot { get; set; }
    public string ChangeSource { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public EmailTemplate? EmailTemplate { get; set; }
}
