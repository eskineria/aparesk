using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public class GeneralAssemblyDecisionConfiguration : IEntityTypeConfiguration<GeneralAssemblyDecision>
{
    public void Configure(EntityTypeBuilder<GeneralAssemblyDecision> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GeneralAssemblyId).IsRequired();
        builder.Property(x => x.DecisionNumber).IsRequired();
        builder.Property(x => x.Description).IsRequired();
    }
}
