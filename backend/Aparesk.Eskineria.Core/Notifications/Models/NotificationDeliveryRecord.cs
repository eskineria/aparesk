namespace Aparesk.Eskineria.Core.Notifications.Models;

public class NotificationDeliveryRecord
{
    public string Channel { get; set; } = "Email";
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
    public string? Culture { get; set; }
    public string Status { get; set; } = "Sent";
    public string? ProviderName { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
