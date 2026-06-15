using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ProcessedWebhookDeliveryConfiguration : IEntityTypeConfiguration<ProcessedWebhookDelivery>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookDelivery> builder)
    {
        builder.ToTable("ProcessedWebhookDeliveries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).IsRequired().HasMaxLength(32);
        builder.Property(x => x.DeliveryId).IsRequired().HasMaxLength(128);
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(64);

        // Idempotency target: a (provider, delivery id) is processed at most once.
        builder.HasIndex(x => new { x.Provider, x.DeliveryId }).IsUnique();
    }
}
