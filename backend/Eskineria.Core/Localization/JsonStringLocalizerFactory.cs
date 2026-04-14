using Eskineria.Core.Localization.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eskineria.Core.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<EskineriaLocalizationOptions> _options;

    public JsonStringLocalizerFactory(
        ILoggerFactory loggerFactory,
        IOptions<EskineriaLocalizationOptions> options)
    {
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return new JsonStringLocalizer(
            _loggerFactory.CreateLogger<JsonStringLocalizer>(),
            _options.Value);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new JsonStringLocalizer(
            _loggerFactory.CreateLogger<JsonStringLocalizer>(),
            _options.Value);
    }
}
