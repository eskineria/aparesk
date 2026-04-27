using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public sealed class SiteBlockConfiguration : IEntityTypeConfiguration<SiteBlock>
{
    public void Configure(EntityTypeBuilder<SiteBlock> builder)
    {
        builder.ToTable("SiteBlocks");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsArchived).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasOne(e => e.Site)
            .WithMany(e => e.Blocks)
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SiteId, e.Code }).IsUnique();
        builder.HasIndex(e => new { e.SiteId, e.Name });
        builder.HasIndex(e => new { e.SiteId, e.IsArchived, e.IsActive });
    }
}
