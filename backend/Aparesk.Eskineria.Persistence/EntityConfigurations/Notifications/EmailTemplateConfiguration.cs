using Aparesk.Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Notifications;

public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Culture).IsRequired().HasMaxLength(10);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.RequiredVariables).IsRequired().HasDefaultValue("[]");
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.IsDraft).HasDefaultValue(true);
        builder.Property(e => e.CurrentVersion).HasDefaultValue(1);
        builder.Property(e => e.PublishedByUserId).HasMaxLength(128);
        builder.Property(e => e.AutoTranslatedFromCulture).HasMaxLength(10);
        builder.HasIndex(e => new { e.Key, e.Culture }).IsUnique();
        builder.HasIndex(e => new { e.Key, e.IsActive, e.Culture });
    }
}

