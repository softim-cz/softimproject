using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class UserApplicationRoleConfiguration : IEntityTypeConfiguration<UserApplicationRole>
{
    public void Configure(EntityTypeBuilder<UserApplicationRole> builder)
    {
        builder.ToTable("UserApplicationRoles");
        builder.HasKey(uar => uar.Id);

        builder.HasOne(uar => uar.User)
            .WithMany(u => u.UserApplicationRoles)
            .HasForeignKey(uar => uar.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uar => uar.ApplicationRole)
            .WithMany(r => r.UserApplicationRoles)
            .HasForeignKey(uar => uar.ApplicationRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(uar => new { uar.UserId, uar.ApplicationRoleId }).IsUnique();
    }
}
