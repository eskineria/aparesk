using Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Compliance;

public sealed class TermsAndConditionsConfiguration : IEntityTypeConfiguration<TermsAndConditions>
{
    public void Configure(EntityTypeBuilder<TermsAndConditions> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Version).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Summary).HasMaxLength(500);
        builder.HasIndex(e => new { e.Type, e.Version }).IsUnique();
        builder.HasIndex(e => new { e.Type, e.IsActive });
    }
}

