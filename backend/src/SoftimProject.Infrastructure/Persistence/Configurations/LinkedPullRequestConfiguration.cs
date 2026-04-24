using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class LinkedPullRequestConfiguration : IEntityTypeConfiguration<LinkedPullRequest>
{
    public void Configure(EntityTypeBuilder<LinkedPullRequest> builder)
    {
        builder.ToTable("LinkedPullRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).IsRequired().HasMaxLength(32);
        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Branch).IsRequired().HasMaxLength(250);
        builder.Property(x => x.AuthorLogin).HasMaxLength(100);
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(16);

        // Upsert target on webhook replays — same (provider, external id) is always the same PR.
        builder.HasIndex(x => new { x.Provider, x.ExternalId, x.TicketId }).IsUnique();

        builder.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
