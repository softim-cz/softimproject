using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class TicketPriorityConfiguration : IEntityTypeConfiguration<TicketPriority>
{
    public void Configure(EntityTypeBuilder<TicketPriority> builder)
    {
        builder.ToTable("TicketPriorities");
        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.Name).HasMaxLength(200).IsRequired();
        builder.Property(tp => tp.NameCs).HasMaxLength(200);
        builder.Property(tp => tp.NameEn).HasMaxLength(200);
        builder.Property(tp => tp.Color).HasMaxLength(50).IsRequired();

        builder.HasOne(tp => tp.ProjectTemplate)
            .WithMany(t => t.TicketPriorities)
            .HasForeignKey(tp => tp.ProjectTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tp => new { tp.ProjectTemplateId, tp.Name }).IsUnique();
    }
}
