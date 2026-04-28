using Aparesk.Eskineria.Core.Auth.Utilities;
using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public sealed class SiteResidentConfiguration : IEntityTypeConfiguration<SiteResident>
{
    public void Configure(EntityTypeBuilder<SiteResident> builder)
    {
        builder.ToTable("SiteResidents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.IdentityNumber)
            .HasMaxLength(500)
            .HasConversion(
                v => AuthSensitiveDataProtector.Encrypt(v),
                v => AuthSensitiveDataProtector.Decrypt(v));
                
        builder.Property(e => e.Type).IsRequired().HasConversion<int>();
        
        builder.Property(e => e.Phone)
            .HasMaxLength(500)
            .HasConversion(
                v => AuthSensitiveDataProtector.Encrypt(v),
                v => AuthSensitiveDataProtector.Decrypt(v));
                
        builder.Property(e => e.Email)
            .HasMaxLength(500)
            .HasConversion(
                v => AuthSensitiveDataProtector.Encrypt(v),
                v => AuthSensitiveDataProtector.Decrypt(v));
                
        builder.Property(e => e.OwnerPhone)
            .HasMaxLength(500)
            .HasConversion(
                v => AuthSensitiveDataProtector.Encrypt(v),
                v => AuthSensitiveDataProtector.Decrypt(v));

        builder.Property(e => e.Occupation).HasMaxLength(150);
        builder.Property(e => e.MoveInDate).HasColumnType("date");
        builder.Property(e => e.MoveOutDate).HasColumnType("date");
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsArchived).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasOne(e => e.Site)
            .WithMany(e => e.Residents)
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Unit)
            .WithMany(e => e.Residents)
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SiteId, e.LastName, e.FirstName });
        builder.HasIndex(e => new { e.SiteId, e.UnitId, e.IsArchived, e.IsActive });
        builder.HasIndex(e => new { e.SiteId, e.Type, e.IsArchived });
    }
}
