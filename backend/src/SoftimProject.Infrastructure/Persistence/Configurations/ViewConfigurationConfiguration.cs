using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class ViewConfigurationConfiguration : IEntityTypeConfiguration<ViewConfiguration>
{
    public void Configure(EntityTypeBuilder<ViewConfiguration> builder)
    {
        builder.ToTable("ViewConfigurations");
        builder.HasKey(vc => vc.Id);

        builder.Property(vc => vc.ViewType).HasMaxLength(50).IsRequired();
        builder.Property(vc => vc.ConfigurationJson).HasColumnType("nvarchar(max)");

        builder.HasOne(vc => vc.User).WithMany(u => u.ViewConfigurations).HasForeignKey(vc => vc.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(vc => vc.Project).WithMany(p => p.ViewConfigurations).HasForeignKey(vc => vc.ProjectId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(vc => new { vc.UserId, vc.ProjectId, vc.ViewType }).IsUnique();
    }
}
