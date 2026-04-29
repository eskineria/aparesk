using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public class GeneralAssemblyConfiguration : IEntityTypeConfiguration<GeneralAssembly>
{
    public void Configure(EntityTypeBuilder<GeneralAssembly> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SiteId).IsRequired();
        builder.Property(x => x.MeetingDate).IsRequired();
        builder.Property(x => x.SecondMeetingDate);
        builder.Property(x => x.Term).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Type).IsRequired();
        
        builder.HasOne(x => x.Site)
            .WithMany()
            .HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Decisions)
            .WithOne(x => x.GeneralAssembly)
            .HasForeignKey(x => x.GeneralAssemblyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BoardMembers)
            .WithOne(x => x.GeneralAssembly)
            .HasForeignKey(x => x.GeneralAssemblyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
