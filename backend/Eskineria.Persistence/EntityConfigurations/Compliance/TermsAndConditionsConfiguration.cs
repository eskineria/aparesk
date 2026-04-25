using Eskineria.Core.Compliance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Eskineria.Core.Shared.Localization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace Eskineria.Persistence.EntityConfigurations.Compliance;

public sealed class TermsAndConditionsConfiguration : IEntityTypeConfiguration<TermsAndConditions>
{
    public void Configure(EntityTypeBuilder<TermsAndConditions> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Version).IsRequired().HasMaxLength(20);

        var localizedContentComparer = new ValueComparer<LocalizedContent>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => new LocalizedContent(c));

        builder.Property(e => e.Content)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LocalizedContent>(v, (JsonSerializerOptions?)null) ?? new LocalizedContent())
            .Metadata.SetValueComparer(localizedContentComparer);

        builder.Property(e => e.Summary)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<LocalizedContent>(v, (JsonSerializerOptions?)null) ?? new LocalizedContent())
            .Metadata.SetValueComparer(localizedContentComparer);

        builder.Property(e => e.Content).HasColumnType("nvarchar(max)");
        builder.Property(e => e.Summary).HasColumnType("nvarchar(max)");

        builder.HasIndex(e => new { e.Type, e.Version }).IsUnique();
        builder.HasIndex(e => new { e.Type, e.IsActive });
    }
}

