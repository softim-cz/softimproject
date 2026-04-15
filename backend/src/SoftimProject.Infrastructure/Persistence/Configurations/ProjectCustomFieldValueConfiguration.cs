using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectCustomFieldValueConfiguration : IEntityTypeConfiguration<ProjectCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<ProjectCustomFieldValue> builder)
    {
        builder.ToTable("ProjectCustomFieldValues");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Value).HasMaxLength(4000);

        builder.HasOne(v => v.Project)
            .WithMany(p => p.CustomFieldValues)
            .HasForeignKey(v => v.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.CustomFieldDefinition)
            .WithMany(d => d.Values)
            .HasForeignKey(v => v.CustomFieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ProjectId, v.CustomFieldDefinitionId }).IsUnique();
    }
}
