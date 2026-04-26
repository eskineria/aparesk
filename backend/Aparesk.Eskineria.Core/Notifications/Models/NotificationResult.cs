namespace Aparesk.Eskineria.Core.Notifications.Models;

public class NotificationResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }

    public static NotificationResult CreateSuccess(string messageId)
        => new() { Success = true, MessageId = messageId };

    public static NotificationResult CreateFailure(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
