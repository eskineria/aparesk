using Aparesk.Eskineria.Core.Auth.Utilities;
using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("HouseholdMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Phone)
            .HasMaxLength(500)
            .HasConversion(
                v => AuthSensitiveDataProtector.Encrypt(v),
                v => AuthSensitiveDataProtector.Decrypt(v));
                
        builder.Property(x => x.IdentityNumber)
            .HasMaxLength(500)
            .HasConversion(
                v => AuthSensitiveDataProtector.Encrypt(v),
                v => AuthSensitiveDataProtector.Decrypt(v));
        builder.Property(x => x.Relationship).HasMaxLength(50);

        builder.HasOne(x => x.Resident)
            .WithMany(x => x.HouseholdMembers)
            .HasForeignKey(x => x.ResidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
