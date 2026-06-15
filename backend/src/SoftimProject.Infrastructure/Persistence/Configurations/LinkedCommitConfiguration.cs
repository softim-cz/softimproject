using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class LinkedCommitConfiguration : IEntityTypeConfiguration<LinkedCommit>
{
    public void Configure(EntityTypeBuilder<LinkedCommit> builder)
    {
        builder.ToTable("LinkedCommits");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Provider).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Sha).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Message).HasMaxLength(1000);
        builder.Property(c => c.Url).HasMaxLength(2048);
        builder.Property(c => c.AuthorLogin).HasMaxLength(100);

        builder.HasIndex(c => new { c.TicketId, c.Provider, c.Sha }).IsUnique();

        builder.HasOne(c => c.Ticket)
            .WithMany()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
