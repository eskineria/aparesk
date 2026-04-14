using Eskineria.Core.Localization.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Localization;

public sealed class LanguageResourceConfiguration : IEntityTypeConfiguration<LanguageResource>
{
    public void Configure(EntityTypeBuilder<LanguageResource> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).IsRequired().HasMaxLength(450);
        builder.Property(e => e.Value).IsRequired();
        builder.Property(e => e.DraftValue);
        builder.Property(e => e.Culture).IsRequired().HasMaxLength(10);
        builder.Property(e => e.ResourceSet).HasMaxLength(50);
        builder.Property(e => e.WorkflowStatus).IsRequired().HasMaxLength(40).HasDefaultValue("Published");
        builder.Property(e => e.OwnerUserId).HasMaxLength(128);
        builder.Property(e => e.LastPublishedByUserId).HasMaxLength(128);
        builder.Property(e => e.LastModifiedByUserId).HasMaxLength(128);

        builder.HasIndex(e => new { e.Key, e.Culture }).IsUnique();
        builder.HasIndex(e => e.WorkflowStatus);
    }
}

