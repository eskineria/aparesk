using Eskineria.Core.Auditing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Auditing;

public sealed class AuditLogIntegrityConfiguration : IEntityTypeConfiguration<AuditLogIntegrity>
{
    public void Configure(EntityTypeBuilder<AuditLogIntegrity> builder)
    {
        builder.ToTable("AppAuditLogIntegrities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AuditTable).IsRequired().HasMaxLength(128);
        builder.Property(e => e.PreviousHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.CurrentHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Algorithm).IsRequired().HasMaxLength(32);
        builder.Property(e => e.KeyId).IsRequired().HasMaxLength(32);
        builder.HasIndex(e => new { e.AuditTable, e.AuditLogId }).IsUnique();
        builder.HasIndex(e => e.CreatedAtUtc);
    }
}

