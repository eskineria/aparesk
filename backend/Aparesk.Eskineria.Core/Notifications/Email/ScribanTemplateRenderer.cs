using System.Collections.Concurrent;
using Scriban;

namespace Aparesk.Eskineria.Core.Notifications.Email;

public class ScribanTemplateRenderer : ITemplateRenderer
{
    private const int MaxTemplateCacheEntries = 256;
    private static readonly ConcurrentDictionary<string, Template> TemplateCache = new(StringComparer.Ordinal);

    public async Task<string> RenderAsync<T>(string template, T model)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Template cannot be empty.", nameof(template));
        }

        var normalizedTemplate = template.Trim();
        var scribanTemplate = GetOrParseTemplate(normalizedTemplate);
        if (scribanTemplate.HasErrors)
        {
            throw new InvalidOperationException($"Template parsing error: {scribanTemplate.Messages}");
        }

        return await scribanTemplate.RenderAsync(model, member => member.Name);
    }

    private static Template GetOrParseTemplate(string template)
    {
        if (TemplateCache.TryGetValue(template, out var cached))
        {
            return cached;
        }

        var parsed = Template.Parse(template);

        // Keep cache bounded to avoid uncontrolled memory growth.
        if (TemplateCache.Count >= MaxTemplateCacheEntries)
        {
            TemplateCache.Clear();
        }

        TemplateCache[template] = parsed;
        return parsed;
    }
}
