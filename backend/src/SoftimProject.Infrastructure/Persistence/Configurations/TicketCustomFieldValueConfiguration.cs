using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class TicketCustomFieldValueConfiguration : IEntityTypeConfiguration<TicketCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<TicketCustomFieldValue> builder)
    {
        builder.ToTable("TicketCustomFieldValues");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Value).HasMaxLength(4000);

        builder.HasOne(v => v.Ticket).WithMany(t => t.CustomFieldValues).HasForeignKey(v => v.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(v => v.CustomFieldDefinition).WithMany(d => d.TicketValues).HasForeignKey(v => v.CustomFieldDefinitionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.TicketId, v.CustomFieldDefinitionId }).IsUnique();
    }
}
