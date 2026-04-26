namespace Aparesk.Eskineria.Core.Localization.Configuration;

public class EskineriaLocalizationOptions
{
    public string ResourcesPath { get; set; } = "Localization";
    public string DefaultCulture { get; set; } = "en-US";
    public string[] SupportedCultures { get; set; } = new[] { "en-US", "tr-TR" };
}
