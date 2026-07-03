using Microsoft.EntityFrameworkCore;
using QACopilot.Domain.Entities;
using QACopilot.Infrastructure.Data.Configurations;

namespace QACopilot.Infrastructure.Data.Context;

public class QACopilotDbContext : DbContext
{
    public QACopilotDbContext(DbContextOptions<QACopilotDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<TestCaseHistory> TestCaseHistories => Set<TestCaseHistory>();
    public DbSet<AuditMetric> AuditMetrics => Set<AuditMetric>();
    public DbSet<JiraConfig> JiraConfigs => Set<JiraConfig>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<TestPlanAnalysis> TestPlanAnalyses => Set<TestPlanAnalysis>();
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<TrainingDocument> TrainingDocuments => Set<TrainingDocument>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<SessionTracking> SessionTrackings => Set<SessionTracking>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<SeniorPanelConfig> SeniorPanelConfigs => Set<SeniorPanelConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        modelBuilder.ApplyConfiguration(new TestCaseHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new AuditMetricConfiguration());
        modelBuilder.ApplyConfiguration(new JiraConfigConfiguration());

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Token).IsRequired().HasMaxLength(500);
            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("RefreshTokens");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(p => p.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(p => p.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Projects");
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasOne(p => p.Project)
                  .WithMany(p => p.Assignments)
                  .HasForeignKey(p => p.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.AssignedByUser)
                  .WithMany()
                  .HasForeignKey(p => p.AssignedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("ProjectAssignments");
        });

        modelBuilder.Entity<TestPlanAnalysis>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.FileName).IsRequired().HasMaxLength(300);
            entity.Property(t => t.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("TestPlanAnalyses");
        });

        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.StructureJson).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(r => r.AIInstructions).IsRequired().HasColumnType("nvarchar(max)");
            entity.HasOne(r => r.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(r => r.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("ReportTemplates");
        });

        modelBuilder.Entity<TrainingDocument>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.FileName).IsRequired().HasMaxLength(300);
            entity.Property(t => t.Category).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(t => t.UploadedByUser)
                  .WithMany()
                  .HasForeignKey(t => t.UploadedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("TrainingDocuments");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("ChatSessions");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Role).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.HasOne(c => c.Session)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(c => c.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("ChatMessages");
        });

        modelBuilder.Entity<SessionTracking>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Module).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Action).IsRequired().HasMaxLength(100);
            entity.Property(s => s.IpAddress).HasMaxLength(50);
            entity.Property(s => s.UserAgent).HasMaxLength(500);
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("SessionTrackings");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Email).IsRequired().HasMaxLength(200);
            entity.Property(l => l.IpAddress).HasMaxLength(50);
            entity.Property(l => l.FailureReason).HasMaxLength(200);
            entity.ToTable("LoginAttempts");
        });

        // SENIOR PANEL CONFIG
        modelBuilder.Entity<SeniorPanelConfig>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("SeniorPanelConfigs");
        });

        base.OnModelCreating(modelBuilder);
    }
}
