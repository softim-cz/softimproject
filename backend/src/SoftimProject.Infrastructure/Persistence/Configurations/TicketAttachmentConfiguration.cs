using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");
        builder.HasKey(ta => ta.Id);

        builder.Property(ta => ta.FileName).HasMaxLength(500).IsRequired();
        builder.Property(ta => ta.BlobUrl).HasMaxLength(2048).IsRequired();
        builder.Property(ta => ta.ContentType).HasMaxLength(200).IsRequired();

        builder.HasOne(ta => ta.Ticket).WithMany(t => t.Attachments).HasForeignKey(ta => ta.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ta => ta.UploadedBy).WithMany().HasForeignKey(ta => ta.UploadedById).OnDelete(DeleteBehavior.Restrict);
    }
}
