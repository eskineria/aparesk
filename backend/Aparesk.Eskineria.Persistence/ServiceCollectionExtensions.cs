using System.Reflection;
using Aparesk.Eskineria.Core.Auditing.Abstractions;
using Aparesk.Eskineria.Core.Compliance.Abstractions;
using Aparesk.Eskineria.Core.Compliance.Repositories;
using Aparesk.Eskineria.Core.Compliance.Services;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Abstractions;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Repositories;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Services;
using Aparesk.Eskineria.Core.Notifications.Templates.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Templates.Repositories;
using Aparesk.Eskineria.Core.Notifications.Templates.Services;
using Aparesk.Eskineria.Core.Localization.Abstractions;
using Aparesk.Eskineria.Core.Localization.Repositories;
using Aparesk.Eskineria.Core.Localization.Services;
using Aparesk.Eskineria.Core.Settings.Abstractions;
using Aparesk.Eskineria.Core.Settings.Repositories;
using Aparesk.Eskineria.Core.Settings.Services;
using Aparesk.Eskineria.Core.Auditing.Extensions;
using Aparesk.Eskineria.Core.Auditing.Repositories;
using Aparesk.Eskineria.Core.Auditing.Services;
using Aparesk.Eskineria.Core.Auth.Abstractions;
using Aparesk.Eskineria.Core.Auth.Extensions;
using Aparesk.Eskineria.Core.Auth.Repositories;
using Aparesk.Eskineria.Core.Auth.Services;
using Aparesk.Eskineria.Core.Notifications.Extensions;
using Aparesk.Eskineria.Core.Notifications.Email;
using Aparesk.Eskineria.Core.Notifications.Abstractions;
using Aparesk.Eskineria.Core.Notifications.Providers;
using Aparesk.Eskineria.Core.Repository.Extensions;
using Aparesk.Eskineria.Persistence.Features.Management.Abstractions;
using Aparesk.Eskineria.Persistence.Features.Management.Repositories;
using Aparesk.Eskineria.Persistence.Features.Products.Abstractions;
using Aparesk.Eskineria.Persistence.Features.Products.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aparesk.Eskineria.Persistence;

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

        // Aparesk.Eskineria Auth
        services.AddEskineriaAuth<ApplicationDbContext>(configuration, permissionAssemblies);

        // Aparesk.Eskineria Auditing
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
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<ISiteBlockRepository, SiteBlockRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<ISiteResidentRepository, SiteResidentRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
