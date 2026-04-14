namespace Eskineria.Core.Notifications.Templates.Models;

public class EmailTemplateDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Culture { get; set; } = "en-US";
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> RequiredVariables { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsDraft { get; set; }
    public int CurrentVersion { get; set; }
    public int? PublishedVersion { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByUserId { get; set; }
    public string? AutoTranslatedFromCulture { get; set; }
    public DateTime? AutoTranslatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEmailTemplateRequest
{
    public string Key { get; set; } = string.Empty;
    public string Culture { get; set; } = "en-US";
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string>? RequiredVariables { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PublishNow { get; set; }
}

public class UpdateEmailTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string>? RequiredVariables { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PublishNow { get; set; }
}

public class EmailTemplateRevisionDto
{
    public int Id { get; set; }
    public int Version { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> RequiredVariables { get; set; } = new();
    public bool IsPublishedSnapshot { get; set; }
    public string ChangeSource { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublishEmailTemplateRequest
{
    public bool MarkActive { get; set; } = true;
}

public class RollbackEmailTemplateRequest
{
    public int Version { get; set; }
    public bool PublishNow { get; set; }
}

public class EmailTemplateCoverageItemDto
{
    public string Key { get; set; } = string.Empty;
    public int TotalCultures { get; set; }
    public int MissingCultures { get; set; }
    public List<string> MissingCultureCodes { get; set; } = new();
    public List<string> DraftCultureCodes { get; set; } = new();
}

public class ValidateEmailTemplateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string>? RequiredVariables { get; set; }
}

public class EmailTemplateValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> UsedVariables { get; set; } = new();
    public List<string> NormalizedRequiredVariables { get; set; } = new();
    public List<string> MissingRequiredVariables { get; set; } = new();
    public List<string> ExtraVariables { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class SendEmailTemplateTestRequest
{
    public int? TemplateId { get; set; }
    public string? Key { get; set; }
    public string? Culture { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public bool UsePublishedVersion { get; set; } = true;
    public Dictionary<string, string>? Variables { get; set; }
}

public class AutoTranslateEmailTemplateRequest
{
    public string Key { get; set; } = string.Empty;
    public string SourceCulture { get; set; } = string.Empty;
    public string TargetCulture { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; }
}

public class AutoTranslateEmailTemplateResultDto
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
}
