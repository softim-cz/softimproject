using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("ChecklistItems");
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Text).HasMaxLength(1000).IsRequired();
        builder.Property(ci => ci.ExternalId).HasMaxLength(100);

        builder.HasOne(ci => ci.Ticket).WithMany(t => t.ChecklistItems).HasForeignKey(ci => ci.TicketId).OnDelete(DeleteBehavior.Cascade);
    }
}
