using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Aparesk.Eskineria.Core.Localization;

public sealed class DatabaseStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggerFactory _loggerFactory;

    public DatabaseStringLocalizerFactory(IServiceScopeFactory scopeFactory, ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _loggerFactory = loggerFactory;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        return new DatabaseStringLocalizer(
            _scopeFactory,
            _loggerFactory.CreateLogger<DatabaseStringLocalizer>(),
            resourceSet: "Backend");
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        return new DatabaseStringLocalizer(
            _scopeFactory,
            _loggerFactory.CreateLogger<DatabaseStringLocalizer>(),
            resourceSet: "Backend");
    }
}
