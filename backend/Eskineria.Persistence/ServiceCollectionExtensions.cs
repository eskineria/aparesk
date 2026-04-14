using System.Reflection;
using Eskineria.Core.Auditing.Abstractions;
using Eskineria.Core.Compliance.Abstractions;
using Eskineria.Core.Compliance.Repositories;
using Eskineria.Core.Compliance.Services;
using Eskineria.Core.Notifications.DeliveryLogs.Abstractions;
using Eskineria.Core.Notifications.DeliveryLogs.Repositories;
using Eskineria.Core.Notifications.DeliveryLogs.Services;
using Eskineria.Core.Notifications.Templates.Abstractions;
using Eskineria.Core.Notifications.Templates.Repositories;
using Eskineria.Core.Notifications.Templates.Services;
using Eskineria.Core.Localization.Abstractions;
using Eskineria.Core.Localization.Repositories;
using Eskineria.Core.Localization.Services;
using Eskineria.Core.Settings.Abstractions;
using Eskineria.Core.Settings.Repositories;
using Eskineria.Core.Settings.Services;
using Eskineria.Core.Auditing.Extensions;
using Eskineria.Core.Auditing.Repositories;
using Eskineria.Core.Auditing.Services;
using Eskineria.Core.Auth.Abstractions;
using Eskineria.Core.Auth.Extensions;
using Eskineria.Core.Auth.Repositories;
using Eskineria.Core.Auth.Services;
using Eskineria.Core.Notifications.Extensions;
using Eskineria.Core.Notifications.Email;
using Eskineria.Core.Notifications.Abstractions;
using Eskineria.Core.Notifications.Providers;
using Eskineria.Core.Repository.Extensions;
using Eskineria.Persistence.Features.Products.Abstractions;
using Eskineria.Persistence.Features.Products.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eskineria.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] permissionAssemblies)
    {
        // Database Context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddEskineriaRepository<ApplicationDbContext>(options =>
        {
            options.AutoSave = false;
        });

        // Eskineria Auth
        services.AddEskineriaAuth<ApplicationDbContext>(configuration, permissionAssemblies);

        // Eskineria Auditing
        services.AddScoped<IAuditingPersistence, EfAuditingPersistence>();
        services.AddAppAuditing<DbAuditingStore>();
        services.AddScoped<PersistentAuditLoggingPolicyProvider>();
        services.AddScoped<IAuditLoggingPolicyProvider>(provider => provider.GetRequiredService<PersistentAuditLoggingPolicyProvider>());
        services.AddScoped<IAuditLoggingPolicyCacheInvalidator>(provider => provider.GetRequiredService<PersistentAuditLoggingPolicyProvider>());

        services.AddScoped<IEmailTemplatePersistence, EfEmailTemplatePersistence>();
        services.AddScoped<IEmailTemplateProvider, PersistentEmailTemplateProvider>();

        services.AddScoped<INotificationDeliveryPersistence, EfNotificationDeliveryPersistence>();
        services.AddScoped<INotificationDeliveryStore, PersistentNotificationDeliveryStore>();

        services.AddScoped<IRoleSelectionAuditStore, UserRoleSelectionLogRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogIntegrityRepository, AuditLogIntegrityRepository>();
        services.AddScoped<ITermsRepository, TermsRepository>();
        services.AddScoped<IUserTermsAcceptanceRepository, UserTermsAcceptanceRepository>();
        services.AddScoped<IEmailDeliveryLogRepository, EmailDeliveryLogRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailTemplateRevisionRepository, EmailTemplateRevisionRepository>();
        services.AddScoped<ILanguageResourceRepository, LanguageResourceRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<ComplianceSeedService>();
        services.AddScoped<IEmailDeliveryLogService, EmailDeliveryLogService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<EmailTemplateSeedService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<SystemSettingsSeedService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
