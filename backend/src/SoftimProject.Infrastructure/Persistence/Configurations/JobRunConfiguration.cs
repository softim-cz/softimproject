using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("JobRuns");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobName).IsRequired().HasMaxLength(100);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(j => j.ErrorMessage).HasMaxLength(4000);

        // Supports the `/health/jobs` query: latest run per job name.
        builder.HasIndex(j => new { j.JobName, j.StartedAt }).IsDescending(false, true);
    }
}
