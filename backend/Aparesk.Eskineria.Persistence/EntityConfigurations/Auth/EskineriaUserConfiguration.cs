using Aparesk.Eskineria.Core.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Auth;

public sealed class EskineriaUserConfiguration : IEntityTypeConfiguration<EskineriaUser>
{
    public void Configure(EntityTypeBuilder<EskineriaUser> builder)
    {
        builder.Property(e => e.ActiveRole).HasMaxLength(100);
        builder.Property(e => e.EmailVerificationCodeHash).HasMaxLength(128);
        builder.Property(e => e.EmailVerificationFailedAttempts).HasDefaultValue(0);
        builder.Property(e => e.PasswordResetCodeHash).HasMaxLength(128);
        builder.Property(e => e.PasswordResetFailedAttempts).HasDefaultValue(0);
    }
}

