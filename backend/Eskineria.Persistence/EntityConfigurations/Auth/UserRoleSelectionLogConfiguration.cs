using Eskineria.Core.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Auth;

public sealed class UserRoleSelectionLogConfiguration : IEntityTypeConfiguration<UserRoleSelectionLog>
{
    public void Configure(EntityTypeBuilder<UserRoleSelectionLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PreviousRole).HasMaxLength(100);
        builder.Property(e => e.NewRole).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ChangedAt);
    }
}

