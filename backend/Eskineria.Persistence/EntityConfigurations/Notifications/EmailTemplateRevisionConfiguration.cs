using Eskineria.Core.Notifications.Templates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Notifications;

public sealed class EmailTemplateRevisionConfiguration : IEntityTypeConfiguration<EmailTemplateRevision>
{
    public void Configure(EntityTypeBuilder<EmailTemplateRevision> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.RequiredVariables).IsRequired().HasDefaultValue("[]");
        builder.Property(e => e.ChangeSource).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ChangedByUserId).HasMaxLength(128);
        builder.HasIndex(e => new { e.EmailTemplateId, e.Version }).IsUnique();
        builder.HasIndex(e => new { e.EmailTemplateId, e.IsPublishedSnapshot });

        builder.HasOne(e => e.EmailTemplate)
            .WithMany(e => e.Revisions)
            .HasForeignKey(e => e.EmailTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

