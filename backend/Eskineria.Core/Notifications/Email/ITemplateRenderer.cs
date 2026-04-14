namespace Eskineria.Core.Notifications.Email;

public interface ITemplateRenderer
{
    Task<string> RenderAsync<T>(string template, T model);
}
