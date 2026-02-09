using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class WorklogConfiguration : IEntityTypeConfiguration<Worklog>
{
    public void Configure(EntityTypeBuilder<Worklog> builder)
    {
        builder.ToTable("Worklogs");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Hours).HasPrecision(6, 2);
        builder.Property(w => w.Description).HasMaxLength(2000);
        builder.Property(w => w.Source).HasConversion<string>().HasMaxLength(50);
        builder.Property(w => w.HourlyRateSnapshot).HasPrecision(10, 2);
        builder.Property(w => w.AiSummary).HasColumnType("nvarchar(max)");
        builder.Property(w => w.Invoiced).HasMaxLength(200);

        builder.HasOne(w => w.Project).WithMany(p => p.Worklogs).HasForeignKey(w => w.ProjectId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(w => w.Ticket).WithMany(t => t.Worklogs).HasForeignKey(w => w.TicketId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(w => w.User).WithMany(u => u.Worklogs).HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.ProjectId, w.Date });
        builder.HasIndex(w => new { w.UserId, w.Date });
    }
}
