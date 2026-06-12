using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectTemplateConfiguration : IEntityTypeConfiguration<ProjectTemplate>
{
    public void Configure(EntityTypeBuilder<ProjectTemplate> builder)
    {
        builder.ToTable("ProjectTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.HasIndex(t => t.Name).IsUnique();

        // Default povolené typy úkolů pro šablonu (M:N přes implicitní join tabulku).
        builder.HasMany(t => t.AllowedTaskTypes)
            .WithMany()
            .UsingEntity(
                "ProjectTemplateAllowedTaskTypes",
                l => l.HasOne(typeof(TaskType)).WithMany().HasForeignKey("TaskTypeId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(ProjectTemplate)).WithMany().HasForeignKey("ProjectTemplateId").OnDelete(DeleteBehavior.Cascade));
    }
}
