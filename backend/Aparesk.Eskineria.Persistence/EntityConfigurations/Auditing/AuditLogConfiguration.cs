using Aparesk.Eskineria.Core.Auditing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Auditing;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AppAuditLogs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.MethodName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Parameters).HasMaxLength(2000);
        builder.Property(e => e.ClientIpAddress).HasMaxLength(50);
        builder.Property(e => e.BrowserInfo).HasMaxLength(500);
    }
}

