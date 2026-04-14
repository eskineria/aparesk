using System.Text.RegularExpressions;
using Eskineria.Core.Shared.Configuration;
using Eskineria.Core.Shared.Localization;
using Eskineria.Core.Settings.Abstractions;
using Eskineria.Core.Settings.Models;
using Eskineria.Core.Auth.Entities;
using Eskineria.Core.Auth.Models;
using Eskineria.Core.Auth.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Eskineria.Core.Settings.Middlewares;

public sealed class MaintenanceModeMiddleware
{
    private static readonly TimeSpan SettingsCacheTtl = TimeSpan.FromSeconds(5);
    private static readonly object SettingsCacheLock = new();
    private static AuthSystemSettingsDto? _cachedSettings;
    private static DateTime _cachedSettingsAtUtc;

    private static readonly Regex AuthLoginPathRegex = new(
        "^/api/v[^/]+/auth/login/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AuthRefreshTokenPathRegex = new(
        "^/api/v[^/]+/auth/refresh-token/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AuthLogoutPathRegex = new(
        "^/api/v[^/]+/auth/logout/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PublicSystemSettingsPathRegex = new(
        "^/api/v[^/]+/systemsettings/public/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LocalizationResourcesPathRegex = new(
        "^/api/v[^/]+/localization/resources/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LocalizationCulturesPathRegex = new(
        "^/api/v[^/]+/localization/cultures/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISystemSettingsService systemSettingsService,
        UserManager<EskineriaUser> userManager,
        IStringLocalizer<MaintenanceModeMiddleware> localizer)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!IsProtectedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var settings = TryGetCachedSettings();
        if (settings == null)
        {
            var settingsResponse = await systemSettingsService.GetAuthSettingsAsync();
            settings = settingsResponse.Success ? settingsResponse.Data : null;
            SetCachedSettings(settings);
        }

        if (settings == null || !settings.MaintenanceModeEnabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsBypassedPath(path))
        {
            await _next(context);
            return;
        }

        if (AccessPolicyEvaluator.IsIpAllowed(
                context.Connection.RemoteIpAddress?.ToString(),
                settings.MaintenanceIpWhitelist))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (AccessPolicyEvaluator.IsRoleAllowed(roles, settings.MaintenanceRoleWhitelist))
                {
                    await _next(context);
                    return;
                }
            }
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new AuthResponse
        {
            Success = false,
            Message = localizer[LocalizationKeys.MaintenanceModeEnabled],
            Errors = Array.Empty<string>()
        });
    }

    private static bool IsProtectedPath(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBypassedPath(string path)
    {
        return AuthLoginPathRegex.IsMatch(path)
               || AuthRefreshTokenPathRegex.IsMatch(path)
               || AuthLogoutPathRegex.IsMatch(path)
               || PublicSystemSettingsPathRegex.IsMatch(path)
               || LocalizationResourcesPathRegex.IsMatch(path)
               || LocalizationCulturesPathRegex.IsMatch(path);
    }

    private static AuthSystemSettingsDto? TryGetCachedSettings()
    {
        lock (SettingsCacheLock)
        {
            if (_cachedSettings == null)
            {
                return null;
            }

            if (DateTime.UtcNow - _cachedSettingsAtUtc > SettingsCacheTtl)
            {
                _cachedSettings = null;
                return null;
            }

            return _cachedSettings;
        }
    }

    private static void SetCachedSettings(AuthSystemSettingsDto? settings)
    {
        lock (SettingsCacheLock)
        {
            _cachedSettings = settings;
            _cachedSettingsAtUtc = DateTime.UtcNow;
        }
    }
}
