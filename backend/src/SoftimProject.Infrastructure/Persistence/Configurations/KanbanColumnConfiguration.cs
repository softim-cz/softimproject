using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class KanbanColumnConfiguration : IEntityTypeConfiguration<KanbanColumn>
{
    public void Configure(EntityTypeBuilder<KanbanColumn> builder)
    {
        builder.ToTable("KanbanColumns");
        builder.HasKey(kc => kc.Id);

        builder.Property(kc => kc.Name).HasMaxLength(200).IsRequired();

        builder.Property(kc => kc.Color).HasMaxLength(9);

        builder.HasOne(kc => kc.Board).WithMany(kb => kb.Columns).HasForeignKey(kc => kc.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(kc => kc.MapsToTaskStates)
            .WithMany(ts => ts.KanbanColumns)
            .UsingEntity("KanbanColumnTaskState",
                l => l.HasOne(typeof(TaskState)).WithMany().HasForeignKey("TaskStateId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(KanbanColumn)).WithMany().HasForeignKey("KanbanColumnId").OnDelete(DeleteBehavior.Cascade));
    }
}
