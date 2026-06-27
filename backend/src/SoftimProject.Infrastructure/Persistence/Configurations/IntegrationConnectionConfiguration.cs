using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class IntegrationConnectionConfiguration : IEntityTypeConfiguration<IntegrationConnection>
{
    public void Configure(EntityTypeBuilder<IntegrationConnection> builder)
    {
        builder.ToTable("IntegrationConnections");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.SourceSystem).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.BaseUrl).IsRequired().HasMaxLength(2048);
        builder.Property(c => c.EncryptedApiToken).HasColumnType("nvarchar(max)");
        builder.Property(c => c.WebhookSecret).HasMaxLength(256);
        builder.Property(c => c.ConflictPolicy).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.Mode).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.OptionsJson).HasColumnType("nvarchar(max)");
        builder.Property(c => c.MappingsJson).HasColumnType("nvarchar(max)");
        builder.Property(c => c.ProjectSelectorJson).HasColumnType("nvarchar(max)");

        builder.HasOne(c => c.TargetProjectTemplate).WithMany().HasForeignKey(c => c.TargetProjectTemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.TargetCompany).WithMany().HasForeignKey(c => c.TargetCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.SourceSystem, c.IsEnabled });
    }
}
