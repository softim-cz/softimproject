using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class KanbanBoardConfiguration : IEntityTypeConfiguration<KanbanBoard>
{
    public void Configure(EntityTypeBuilder<KanbanBoard> builder)
    {
        builder.ToTable("KanbanBoards");
        builder.HasKey(kb => kb.Id);

        builder.Property(kb => kb.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(kb => kb.Project).WithMany(p => p.Boards).HasForeignKey(kb => kb.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
