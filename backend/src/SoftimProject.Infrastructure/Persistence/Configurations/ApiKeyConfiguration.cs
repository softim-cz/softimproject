using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name).HasMaxLength(100).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(k => k.Prefix).HasMaxLength(32).IsRequired();

        // Lookup by hash on every API-key authenticated request.
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => k.UserId);

        builder.HasOne(k => k.User)
            .WithMany()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
