using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.ToTable("SyncLogs");
        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.SyncType).HasConversion<string>().HasMaxLength(50);
        builder.Property(sl => sl.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(sl => sl.ErrorMessage).HasMaxLength(4000);

        builder.HasOne(sl => sl.Project).WithMany(p => p.SyncLogs).HasForeignKey(sl => sl.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
