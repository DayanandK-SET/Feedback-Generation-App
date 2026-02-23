using Feedback_Generation_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Feedback_Generation_App.Contexts
{
    public class FeedbackContext : DbContext
    {
        public FeedbackContext(DbContextOptions<FeedbackContext> options): base(options)
        {
        }

        // =========================
        // DbSets
        // =========================
        public DbSet<User> Users => Set<User>();
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
        public DbSet<Response> Responses => Set<Response>();
        public DbSet<Answer> Answers => Set<Answer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // User → Surveys (One-to-Many)
            // ============================================
            modelBuilder.Entity<User>()
                .HasMany(u => u.Surveys)
                .WithOne(s => s.CreatedBy)
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================
            // Survey → Questions (One-to-Many)
            // ============================================
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Questions)
                .WithOne(q => q.Survey)
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================
            // Survey → Responses (One-to-Many)
            // ============================================
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Responses)
                .WithOne(r => r.Survey)
                .HasForeignKey(r => r.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================
            // Question → Options (One-to-Many)
            // ============================================
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================
            // Question → Answers (One-to-Many)
            // ============================================
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ============================================
            // Response → Answers (One-to-Many)
            // ============================================
            modelBuilder.Entity<Response>()
                .HasMany(r => r.Answers)
                .WithOne(a => a.Response)
                .HasForeignKey(a => a.ResponseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================
            // Unique constraint for PublicIdentifier
            // ============================================
            modelBuilder.Entity<Survey>()
                .HasIndex(s => s.PublicIdentifier)
                .IsUnique();
        }
    }
}