using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectTypeConfiguration : IEntityTypeConfiguration<ProjectType>
{
    public void Configure(EntityTypeBuilder<ProjectType> builder)
    {
        builder.ToTable("ProjectTypes");
        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Name).HasMaxLength(200).IsRequired();
        builder.Property(pt => pt.Description).HasMaxLength(2000);

        builder.HasIndex(pt => pt.Name).IsUnique();
    }
}
