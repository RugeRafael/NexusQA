using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QACopilot.Domain.Entities;

namespace QACopilot.Infrastructure.Data.Configurations;

public class AuditMetricConfiguration : IEntityTypeConfiguration<AuditMetric>
{
    public void Configure(EntityTypeBuilder<AuditMetric> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Module)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(50);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(500);

        builder.ToTable("AuditMetrics");
    }
}