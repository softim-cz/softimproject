using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectStateConfiguration : IEntityTypeConfiguration<ProjectState>
{
    public void Configure(EntityTypeBuilder<ProjectState> builder)
    {
        builder.ToTable("ProjectStates");
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Name).HasMaxLength(200).IsRequired();
        builder.Property(ps => ps.NameCs).HasMaxLength(200);
        builder.Property(ps => ps.NameEn).HasMaxLength(200);
        builder.Property(ps => ps.Color).HasMaxLength(50).IsRequired();

        builder.HasIndex(ps => ps.Name).IsUnique();
    }
}
