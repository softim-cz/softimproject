using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftimProject.Domain.Entities;

namespace SoftimProject.Infrastructure.Persistence.Configurations;

public sealed class AiReportConfiguration : IEntityTypeConfiguration<AiReport>
{
    public void Configure(EntityTypeBuilder<AiReport> builder)
    {
        builder.ToTable("AiReports");
        builder.HasKey(ar => ar.Id);

        builder.Property(ar => ar.ReportType).HasConversion<string>().HasMaxLength(50);
        builder.Property(ar => ar.Content).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasOne(ar => ar.Project).WithMany(p => p.AiReports).HasForeignKey(ar => ar.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
