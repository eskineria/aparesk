namespace Aparesk.Eskineria.Core.Notifications.Email;

public class EmailTemplateContent
{
    public string Key { get; set; } = string.Empty;
    public string TrackingKey { get; set; } = string.Empty;
    public string Culture { get; set; } = "en-US";
    public int Version { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
