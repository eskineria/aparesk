using System.Globalization;

namespace Aparesk.Eskineria.Core.Notifications.Email;

public sealed class PersistentEmailTemplateProvider : IEmailTemplateProvider
{
    private const string DefaultCulture = "en-US";

    private readonly IEmailTemplatePersistence _emailTemplatePersistence;

    public PersistentEmailTemplateProvider(IEmailTemplatePersistence emailTemplatePersistence)
    {
        _emailTemplatePersistence = emailTemplatePersistence;
    }

    public async Task<EmailTemplateContent?> GetActiveTemplateAsync(
        string key,
        string? recipient = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalizedKey = key.Trim();
        var cultureChain = BuildCultureFallbackChain(CultureInfo.CurrentUICulture.Name);
        var templates = await _emailTemplatePersistence.GetActiveTemplatesAsync(
            normalizedKey,
            cultureChain,
            cancellationToken);

        if (templates.Count == 0)
        {
            return null;
        }

        var selectedTemplate = SelectByCulture(templates, cultureChain, x => x.Culture);
        if (selectedTemplate == null)
        {
            return null;
        }

        return await _emailTemplatePersistence.GetRevisionAsync(
            selectedTemplate.Id,
            selectedTemplate.Key,
            selectedTemplate.Culture,
            selectedTemplate.Version,
            normalizedKey,
            cancellationToken);
    }

    private static TItem? SelectByCulture<TItem>(
        IReadOnlyCollection<TItem> items,
        IReadOnlyList<string> cultureChain,
        Func<TItem, string?> cultureSelector)
        where TItem : class
    {
        return items
            .Select(x => new
            {
                Item = x,
                Order = FindCultureIndex(cultureChain, cultureSelector(x)),
            })
            .Where(x => x.Order >= 0)
            .OrderBy(x => x.Order)
            .Select(x => x.Item)
            .FirstOrDefault();
    }

    private static int FindCultureIndex(IReadOnlyList<string> cultureChain, string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return -1;
        }

        for (var i = 0; i < cultureChain.Count; i++)
        {
            if (string.Equals(cultureChain[i], culture, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static List<string> BuildCultureFallbackChain(string? culture)
    {
        var chain = new List<string>();

        void AddCulture(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim().Replace('_', '-');
            if (!chain.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                chain.Add(normalized);
            }
        }

        AddCulture(culture);

        if (!string.IsNullOrWhiteSpace(culture))
        {
            var normalized = culture.Trim().Replace('_', '-');
            var separatorIndex = normalized.IndexOf('-');
            if (separatorIndex > 0)
            {
                AddCulture(normalized[..separatorIndex]);
            }
        }

        AddCulture(DefaultCulture);
        AddCulture("en");

        return chain;
    }
}
