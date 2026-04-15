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
        builder.Property(t => t.ExternalId).HasMaxLength(200);
        builder.Property(t => t.ExternalUrl).HasMaxLength(2048);
        builder.Property(t => t.AiSummary).HasColumnType("nvarchar(max)");
        builder.Property(t => t.EstimatedHours).HasPrecision(8, 2);
        builder.Property(t => t.CumulativeWorkedHours).HasPrecision(10, 2);
        builder.Property(t => t.ExternalBudget).HasPrecision(14, 2);
        builder.Property(t => t.ExternalUser).HasMaxLength(256);
        builder.Property(t => t.ImplementationNotes).HasColumnType("nvarchar(max)");
        builder.Property(t => t.LastComment).HasMaxLength(2000);

        builder.HasOne(t => t.Project).WithMany(p => p.Tickets).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(t => t.Column).WithMany(c => c.Tickets).HasForeignKey(t => t.ColumnId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Assignee).WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(t => t.Reporter).WithMany().HasForeignKey(t => t.ReporterId).OnDelete(DeleteBehavior.Restrict);

        // Lookup FK
        builder.HasOne(t => t.TaskType).WithMany(tt => tt.Tickets).HasForeignKey(t => t.TaskTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TaskState).WithMany(ts => ts.Tickets).HasForeignKey(t => t.TaskStateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.TicketPriority).WithMany(tp => tp.Tickets).HasForeignKey(t => t.TicketPriorityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ParentTicket).WithMany(t => t.SubTickets).HasForeignKey(t => t.ParentTicketId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.Number).IsRequired();
        builder.HasIndex(t => new { t.ProjectId, t.Number }).IsUnique();

        builder.HasIndex(t => new { t.ProjectId, t.TaskStateId });
        builder.HasIndex(t => t.AssigneeId);
        builder.HasIndex(t => t.ExternalId);
    }
}
