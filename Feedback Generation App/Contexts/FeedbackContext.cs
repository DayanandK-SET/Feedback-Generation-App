using Feedback_Generation_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Contexts
{
    public class FeedbackContext : DbContext
    {
        public FeedbackContext(DbContextOptions<FeedbackContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
        public DbSet<Response> Responses => Set<Response>();
        public DbSet<Answer> Answers => Set<Answer>();
        public DbSet<QuestionBank> QuestionBanks => Set<QuestionBank>();
        public DbSet<QuestionBankOption> QuestionBankOptions => Set<QuestionBankOption>();

        public DbSet<Log> Logs => Set<Log>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User → Surveys (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Surveys)
                .WithOne(s => s.CreatedBy)
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Survey → Questions (One-to-Many)
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Questions)
                .WithOne(q => q.Survey)
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Survey → Responses (One-to-Many)
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Responses)
                .WithOne(r => r.Survey)
                .HasForeignKey(r => r.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question → Options (One-to-Many)
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question → Answers (One-to-Many)
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Response → Answers (One-to-Many)
            modelBuilder.Entity<Response>()
                .HasMany(r => r.Answers)
                .WithOne(a => a.Response)
                .HasForeignKey(a => a.ResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint for PublicIdentifier
            modelBuilder.Entity<Survey>()
                .HasIndex(s => s.PublicIdentifier)
                .IsUnique();

            // Answer → SelectedOption (Many-to-One)
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.SelectedOption)
                .WithMany()
                .HasForeignKey(a => a.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // QuestionBank → Options
            modelBuilder.Entity<QuestionBank>()
                .HasMany(q => q.Options)
                .WithOne(o => o.QuestionBank)
                .HasForeignKey(o => o.QuestionBankId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → QuestionBank
            modelBuilder.Entity<User>()
                .HasMany<QuestionBank>()
                .WithOne(q => q.CreatedBy)
                .HasForeignKey(q => q.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Log table — no relationships, just column constraints
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.ExceptionType).HasMaxLength(200);
                entity.Property(l => l.Method).HasMaxLength(10);
                entity.Property(l => l.Path).HasMaxLength(500);
                entity.Property(l => l.QueryString).HasMaxLength(1000);
                entity.Property(l => l.Username).HasMaxLength(100);
                entity.Property(l => l.UserRole).HasMaxLength(50);
            });

            // AuditLog table — standalone, no FK constraints
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Action).HasMaxLength(100);
                entity.Property(a => a.SurveyTitle).HasMaxLength(200);
                entity.Property(a => a.PerformedBy).HasMaxLength(100);
            });
        }
    }
}
