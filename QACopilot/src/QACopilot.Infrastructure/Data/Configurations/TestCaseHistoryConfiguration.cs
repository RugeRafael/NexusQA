using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QACopilot.Domain.Entities;

namespace QACopilot.Infrastructure.Data.Configurations;

public class TestCaseHistoryConfiguration : IEntityTypeConfiguration<TestCaseHistory>
{
    public void Configure(EntityTypeBuilder<TestCaseHistory> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.GeneratedContent)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.ConfidenceScore)
            .HasPrecision(5, 2);

        builder.ToTable("TestCaseHistories");
    }
}