using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Products;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Sku).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        builder.Property(e => e.StockQuantity).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsArchived).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasIndex(e => e.Sku).IsUnique();
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => new { e.IsArchived, e.IsActive });
    }
}
