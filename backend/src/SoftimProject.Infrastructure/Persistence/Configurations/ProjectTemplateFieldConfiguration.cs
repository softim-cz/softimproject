using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectTemplateFieldConfiguration : IEntityTypeConfiguration<ProjectTemplateField>
{
    public void Configure(EntityTypeBuilder<ProjectTemplateField> builder)
    {
        builder.ToTable("ProjectTemplateFields");
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.ProjectTemplate)
            .WithMany(t => t.Fields)
            .HasForeignKey(f => f.ProjectTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.CustomFieldDefinition)
            .WithMany(d => d.TemplateFields)
            .HasForeignKey(f => f.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.ProjectTemplateId, f.CustomFieldDefinitionId }).IsUnique();
    }
}
