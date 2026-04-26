using Aparesk.Eskineria.Core.Localization.Abstractions;

namespace Aparesk.Eskineria.Core.Localization;

public sealed class DatabaseStringLocalizerCacheInvalidator : ILocalizationCacheInvalidator
{
    public void Clear()
    {
        DatabaseStringLocalizer.ClearCache();
    }
}
