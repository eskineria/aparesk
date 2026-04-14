using Eskineria.Core.Localization.Abstractions;

namespace Eskineria.Core.Localization;

public sealed class DatabaseStringLocalizerCacheInvalidator : ILocalizationCacheInvalidator
{
    public void Clear()
    {
        DatabaseStringLocalizer.ClearCache();
    }
}
