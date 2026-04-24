using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class AiInvocationConfiguration : IEntityTypeConfiguration<AiInvocation>
{
    public void Configure(EntityTypeBuilder<AiInvocation> builder)
    {
        builder.ToTable("AiInvocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Trigger).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.InputHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Model).IsRequired().HasMaxLength(64);
        builder.Property(x => x.OutputPreview).HasMaxLength(1000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.EstimatedCostUsd).HasColumnType("decimal(10,6)");

        // Ticket-history view on the detail page queries by ticket, newest first.
        builder.HasIndex(x => new { x.TicketId, x.StartedAt });
        // Admin per-project usage query filters project + date window.
        builder.HasIndex(x => new { x.ProjectId, x.StartedAt });
        // Rate-limit check filters by user + date window.
        builder.HasIndex(x => new { x.TriggeredByUserId, x.StartedAt });

        builder.HasOne(x => x.TriggeredByUser).WithMany().HasForeignKey(x => x.TriggeredByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Ticket).WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.SetNull);
    }
}
