using Aparesk.Eskineria.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aparesk.Eskineria.Persistence.EntityConfigurations.Management;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GeneralAssemblyId).IsRequired();
        builder.Property(x => x.ResidentId).IsRequired();
        builder.Property(x => x.BoardType).IsRequired();
        builder.Property(x => x.MemberType).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(100);

        builder.HasOne(x => x.Resident)
            .WithMany()
            .HasForeignKey(x => x.ResidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
