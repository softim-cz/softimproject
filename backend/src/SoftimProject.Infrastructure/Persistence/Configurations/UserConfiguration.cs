using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.EntraObjectId).HasMaxLength(128).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.GlobalRole).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(u => u.EntraObjectId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
