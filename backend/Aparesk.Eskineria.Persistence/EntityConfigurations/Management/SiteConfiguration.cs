using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.LegalTitle).HasMaxLength(250);
        builder.Property(e => e.TaxNumber).HasMaxLength(32);
        builder.Property(e => e.TaxOffice).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(32);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.AddressLine).HasMaxLength(500);
        builder.Property(e => e.District).HasMaxLength(100);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.PostalCode).HasMaxLength(16);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsArchived).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => new { e.IsArchived, e.IsActive });
    }
}
