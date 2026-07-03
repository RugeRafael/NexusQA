using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QACopilot.Domain.Entities;

namespace QACopilot.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(d => d.TestCaseHistories)
            .WithOne(t => t.Document)
            .HasForeignKey(t => t.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Documents");
    }
}