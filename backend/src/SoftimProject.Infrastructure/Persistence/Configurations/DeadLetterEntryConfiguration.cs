using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class DeadLetterEntryConfiguration : IEntityTypeConfiguration<DeadLetterEntry>
{
    public void Configure(EntityTypeBuilder<DeadLetterEntry> builder)
    {
        builder.ToTable("DeadLetterEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OperationType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.OperationKey).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(64);

        // Upsert-by-key on the writer side + fast lookup for replay.
        builder.HasIndex(x => new { x.OperationType, x.OperationKey }).IsUnique();
        // Admin listing ordered by newest failures — supports Pending filter + paging.
        builder.HasIndex(x => new { x.Status, x.LastFailedAt });
    }
}
