using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class MigrationJobConfiguration : IEntityTypeConfiguration<MigrationJob>
{
    public void Configure(EntityTypeBuilder<MigrationJob> builder)
    {
        builder.ToTable("MigrationJobs");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SourceSystem).HasMaxLength(50).IsRequired();
        builder.Property(m => m.SourceBaseUrl).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(m => m.CurrentPhase).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.ErrorLog).HasColumnType("nvarchar(max)");
        builder.Property(m => m.Configuration).HasColumnType("nvarchar(max)");

        builder.HasOne(m => m.InitiatedBy).WithMany().HasForeignKey(m => m.InitiatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
