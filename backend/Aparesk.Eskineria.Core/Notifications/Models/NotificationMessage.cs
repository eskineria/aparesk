namespace Aparesk.Eskineria.Core.Notifications.Models;

public class NotificationMessage
{
    public required string Recipient { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public NotificationChannel Channel { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public DateTime? ScheduledTime { get; set; }
}
