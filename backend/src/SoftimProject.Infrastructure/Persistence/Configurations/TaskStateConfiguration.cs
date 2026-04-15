using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class TaskStateConfiguration : IEntityTypeConfiguration<TaskState>
{
    public void Configure(EntityTypeBuilder<TaskState> builder)
    {
        builder.ToTable("TaskStates");
        builder.HasKey(ts => ts.Id);

        builder.Property(ts => ts.Name).HasMaxLength(200).IsRequired();
        builder.Property(ts => ts.Color).HasMaxLength(50).IsRequired();

        builder.HasOne(ts => ts.ProjectTemplate)
            .WithMany(t => t.TaskStates)
            .HasForeignKey(ts => ts.ProjectTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ts => new { ts.ProjectTemplateId, ts.Name }).IsUnique();
    }
}
