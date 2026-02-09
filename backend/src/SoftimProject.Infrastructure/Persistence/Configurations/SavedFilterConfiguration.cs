using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class SavedFilterConfiguration : IEntityTypeConfiguration<SavedFilter>
{
    public void Configure(EntityTypeBuilder<SavedFilter> builder)
    {
        builder.ToTable("SavedFilters");
        builder.HasKey(sf => sf.Id);

        builder.Property(sf => sf.Name).HasMaxLength(200).IsRequired();
        builder.Property(sf => sf.ViewType).HasMaxLength(50).IsRequired();
        builder.Property(sf => sf.FilterJson).HasColumnType("nvarchar(max)");

        builder.HasOne(sf => sf.User).WithMany().HasForeignKey(sf => sf.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(sf => sf.Project).WithMany().HasForeignKey(sf => sf.ProjectId).OnDelete(DeleteBehavior.NoAction);
    }
}
