
using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public class GeneralAssemblyAgendaItemConfiguration : IEntityTypeConfiguration<GeneralAssemblyAgendaItem>
{
    public void Configure(EntityTypeBuilder<GeneralAssemblyAgendaItem> builder)
    {
        builder.ToTable("GeneralAssemblyAgendaItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Order)
            .IsRequired();

        builder.HasOne(x => x.GeneralAssembly)
            .WithMany(x => x.AgendaItems)
            .HasForeignKey(x => x.GeneralAssemblyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
