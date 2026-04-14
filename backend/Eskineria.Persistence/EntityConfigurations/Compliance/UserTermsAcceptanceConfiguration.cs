using Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Compliance;

public sealed class UserTermsAcceptanceConfiguration : IEntityTypeConfiguration<UserTermsAcceptance>
{
    public void Configure(EntityTypeBuilder<UserTermsAcceptance> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.TermsAndConditions)
            .WithMany(t => t.UserAcceptances)
            .HasForeignKey(e => e.TermsAndConditionsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);

        builder.HasIndex(e => new { e.UserId, e.TermsAndConditionsId });
        builder.HasIndex(e => e.AcceptedAt);
    }
}

