using Eskineria.Core.Auditing.Models;
using Microsoft.EntityFrameworkCore;
using Eskineria.Core.Auth.Data;
using Eskineria.Core.Auth.Entities;
using Eskineria.Core.Compliance.Entities;
using Eskineria.Core.Localization.Entities;
using Eskineria.Core.Notifications.DeliveryLogs.Entities;
using Eskineria.Core.Notifications.Templates.Entities;
using Eskineria.Core.Settings.Entities;
using Eskineria.Domain.Entities;

namespace Eskineria.Persistence;

public class ApplicationDbContext : EskineriaIdentityDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AppAuditLogs { get; set; }
    public DbSet<AuditLogIntegrity> AppAuditLogIntegrities { get; set; }
    public DbSet<TermsAndConditions> TermsAndConditions { get; set; }
    public DbSet<UserTermsAcceptance> UserTermsAcceptances { get; set; }
    public DbSet<UserRoleSelectionLog> UserRoleSelectionLogs { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailTemplateRevision> EmailTemplateRevisions { get; set; }
    public DbSet<EmailDeliveryLog> EmailDeliveryLogs { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<LanguageResource> LanguageResources { get; set; }
    public DbSet<Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
