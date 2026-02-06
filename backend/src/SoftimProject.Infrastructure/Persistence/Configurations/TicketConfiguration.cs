using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Description).HasColumnType("nvarchar(max)");
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.ExternalId).HasMaxLength(200);
        builder.Property(t => t.ExternalUrl).HasMaxLength(2048);
        builder.Property(t => t.AiSummary).HasColumnType("nvarchar(max)");
        builder.Property(t => t.EstimatedHours).HasPrecision(8, 2);

        builder.HasOne(t => t.Project).WithMany(p => p.Tickets).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Column).WithMany(c => c.Tickets).HasForeignKey(t => t.ColumnId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Assignee).WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Reporter).WithMany().HasForeignKey(t => t.ReporterId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.ProjectId, t.Status });
        builder.HasIndex(t => t.AssigneeId);
        builder.HasIndex(t => t.ExternalId);
    }
}
