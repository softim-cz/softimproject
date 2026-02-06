using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(6).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);

        builder.Property(p => p.BudgetHours).HasPrecision(10, 2);
        builder.Property(p => p.SpentHours).HasPrecision(10, 2);
        builder.Property(p => p.BudgetAmount).HasPrecision(14, 2);
        builder.Property(p => p.SpentAmount).HasPrecision(14, 2);

        builder.Property(p => p.ExternalSystem).HasMaxLength(50);
        builder.Property(p => p.ExternalProjectId).HasMaxLength(200);
        builder.Property(p => p.ExternalBaseUrl).HasMaxLength(2048);
        builder.Property(p => p.ExternalApiToken).HasMaxLength(1024);

        builder.Property(p => p.ClientAccessToken).HasMaxLength(128);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ClientAccessToken).IsUnique().HasFilter("[ClientAccessToken] IS NOT NULL");
    }
}
