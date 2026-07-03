using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QACopilot.Domain.Entities;

namespace QACopilot.Infrastructure.Data.Configurations;

public class JiraConfigConfiguration : IEntityTypeConfiguration<JiraConfig>
{
    public void Configure(EntityTypeBuilder<JiraConfig> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.BaseUrl)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.ProjectKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.ApiToken)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(j => j.AccountEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.ToTable("JiraConfigs");
    }
}