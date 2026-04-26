using Aparesk.Eskineria.Core.Auth.Services;
using Aparesk.Eskineria.Core.Notifications.Templates.Services;
using Aparesk.Eskineria.Core.Settings.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aparesk.Eskineria.Core.Shared.Startup;

public static class WebApplicationExtensions
{
    public static async Task<bool> TryHandleStartupCommandsAsync(this WebApplication app, string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return false;
        }

        using var scope = app.Services.CreateScope();
        var authSeedService = scope.ServiceProvider.GetRequiredService<AuthSeedService>();
        return await authSeedService.TryHandleSeedUserRolesCommandAsync(args);
    }

    public static async Task RunConfiguredStartupSeedAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("StartupSeed:Enabled"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        await serviceProvider.GetRequiredService<AuthSeedService>().SeedStartupAsync(app.Configuration);
        await serviceProvider.GetRequiredService<SystemSettingsSeedService>().SeedStartupAsync(app.Configuration);
        await serviceProvider.GetRequiredService<EmailTemplateSeedService>().SeedStartupAsync();
    }
}
