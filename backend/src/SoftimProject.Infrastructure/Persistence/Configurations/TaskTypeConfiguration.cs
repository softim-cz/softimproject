using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class TaskTypeConfiguration : IEntityTypeConfiguration<TaskType>
{
    public void Configure(EntityTypeBuilder<TaskType> builder)
    {
        builder.ToTable("TaskTypes");
        builder.HasKey(tt => tt.Id);

        builder.Property(tt => tt.Name).HasMaxLength(200).IsRequired();
        builder.Property(tt => tt.NameCs).HasMaxLength(200);
        builder.Property(tt => tt.NameEn).HasMaxLength(200);
        builder.Property(tt => tt.Icon).HasMaxLength(100);

        builder.HasIndex(tt => tt.Name).IsUnique();
    }
}
