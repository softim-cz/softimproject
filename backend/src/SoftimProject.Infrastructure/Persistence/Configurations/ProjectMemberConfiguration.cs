using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(pm => pm.HourlyRateOverride).HasPrecision(10, 2);

        builder.HasOne(pm => pm.Project).WithMany(p => p.Members).HasForeignKey(pm => pm.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(pm => pm.User).WithMany(u => u.ProjectMembers).HasForeignKey(pm => pm.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId }).IsUnique();
    }
}
