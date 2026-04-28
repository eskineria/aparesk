using Aparesk.Eskineria.Core.Auditing.Models;
using Microsoft.EntityFrameworkCore;
using Aparesk.Eskineria.Core.Auth.Data;
using Aparesk.Eskineria.Core.Auth.Entities;
using Aparesk.Eskineria.Core.Compliance.Entities;
using Aparesk.Eskineria.Core.Localization.Entities;
using Aparesk.Eskineria.Core.Notifications.DeliveryLogs.Entities;
using Aparesk.Eskineria.Core.Notifications.Templates.Entities;
using Aparesk.Eskineria.Core.Settings.Entities;
using Aparesk.Eskineria.Domain.Entities;

namespace Aparesk.Eskineria.Persistence;

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
    public DbSet<Site> Sites { get; set; }
    public DbSet<SiteBlock> SiteBlocks { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<SiteResident> SiteResidents { get; set; }
    public DbSet<HouseholdMember> HouseholdMembers { get; set; }
    public DbSet<GeneralAssembly> GeneralAssemblies { get; set; }
    public DbSet<GeneralAssemblyAgendaItem> GeneralAssemblyAgendaItems { get; set; }
    public DbSet<GeneralAssemblyDecision> GeneralAssemblyDecisions { get; set; }
    public DbSet<BoardMember> BoardMembers { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
