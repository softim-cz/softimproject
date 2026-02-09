using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments", t =>
            t.HasCheckConstraint("CK_Comment_TicketOrProject", "[TicketId] IS NOT NULL OR [ProjectId] IS NOT NULL"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.Source).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.ExternalId).HasMaxLength(200);
        builder.Property(c => c.ExternalUser).HasMaxLength(256);

        builder.HasOne(c => c.Ticket).WithMany(t => t.Comments).HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.Project).WithMany(p => p.Comments).HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}
