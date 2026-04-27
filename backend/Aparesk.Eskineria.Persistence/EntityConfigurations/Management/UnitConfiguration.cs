using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Number).IsRequired().HasMaxLength(64);
        builder.Property(e => e.DoorNumber).HasMaxLength(64);
        builder.Property(e => e.Type).IsRequired().HasConversion<int>();
        builder.Property(e => e.GrossAreaSquareMeters).HasColumnType("decimal(18,2)");
        builder.Property(e => e.NetAreaSquareMeters).HasColumnType("decimal(18,2)");
        builder.Property(e => e.LandShare).HasColumnType("decimal(18,6)");
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsArchived).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasOne(e => e.Site)
            .WithMany(e => e.Units)
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SiteBlock)
            .WithMany(e => e.Units)
            .HasForeignKey(e => e.SiteBlockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SiteId, e.SiteBlockId, e.Number }).IsUnique();
        builder.HasIndex(e => new { e.SiteId, e.Number }).IsUnique().HasFilter("[SiteBlockId] IS NULL");
        builder.HasIndex(e => new { e.SiteId, e.IsArchived, e.IsActive });
    }
}
