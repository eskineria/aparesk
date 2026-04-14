namespace Eskineria.Core.Notifications.DeliveryLogs.Models;

public class GetEmailDeliveryLogsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? TemplateKey { get; set; }
    public string? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class EmailDeliveryLogItemDto
{
    public long Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
    public string? Culture { get; set; }
    public string? Status { get; set; }
    public string? ProviderName { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
