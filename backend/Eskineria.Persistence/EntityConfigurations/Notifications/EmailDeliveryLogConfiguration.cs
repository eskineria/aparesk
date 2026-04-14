using Eskineria.Core.Notifications.DeliveryLogs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eskineria.Persistence.EntityConfigurations.Notifications;

public sealed class EmailDeliveryLogConfiguration : IEntityTypeConfiguration<EmailDeliveryLog>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Channel).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Recipient).IsRequired().HasMaxLength(320);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TemplateKey).HasMaxLength(150);
        builder.Property(e => e.Culture).HasMaxLength(10);
        builder.Property(e => e.Status).HasMaxLength(40);
        builder.Property(e => e.ProviderName).HasMaxLength(100);
        builder.Property(e => e.MessageId).HasMaxLength(200);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.RequestedByUserId).HasMaxLength(128);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.Channel, e.Status, e.CreatedAt });
        builder.HasIndex(e => new { e.TemplateKey, e.Culture, e.CreatedAt });
    }
}

