using AssignmentPRN.DataAccess.Data;
using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentPRN.DataAccess;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Instructor> Instructors => Set<Instructor>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();

    public DbSet<SessionStudent> SessionStudents => Set<SessionStudent>();

    public DbSet<ExamQuestionAssignment> ExamQuestionAssignments => Set<ExamQuestionAssignment>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)");
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.ToTable("Instructors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subjects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("Questions", table =>
            {
                table.HasCheckConstraint("CK_Questions_Difficulty", "[Difficulty] BETWEEN 1 AND 5");
                table.HasCheckConstraint("CK_Questions_Category", "[Category] IN ('Core', 'Deep')");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.Topic).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Difficulty).IsRequired();
            entity.HasIndex(x => new { x.SubjectId, x.Category, x.Difficulty });
            entity.HasIndex(x => x.Topic);
            entity.HasOne(x => x.Subject)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.ToTable("ExamSessions", table =>
            {
                table.HasCheckConstraint(
                    "CK_ExamSessions_Status",
                    "[Status] IN ('Draft', 'Scheduled', 'InProgress', 'Completed', 'Cancelled')");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Date).HasColumnType("datetime2(3)").IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            entity.HasIndex(x => new { x.InstructorId, x.Date });
            entity.HasIndex(x => new { x.SubjectId, x.Date });
            entity.HasIndex(x => x.Status);
            entity.HasOne(x => x.Instructor)
                .WithMany(x => x.ExamSessions)
                .HasForeignKey(x => x.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Subject)
                .WithMany(x => x.ExamSessions)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionStudent>(entity =>
        {
            entity.ToTable("SessionStudents", table =>
            {
                table.HasCheckConstraint("CK_SessionStudents_RequestedCoreCount", "[RequestedCoreCount] >= 0");
                table.HasCheckConstraint("CK_SessionStudents_MaximumDeepCount", "[MaximumDeepCount] >= 0");
                table.HasCheckConstraint("CK_SessionStudents_ActualCoreCount", "[ActualCoreCount] >= 0");
                table.HasCheckConstraint("CK_SessionStudents_ActualDeepCount", "[ActualDeepCount] >= 0");
                table.HasCheckConstraint(
                    "CK_SessionStudents_Counts",
                    "[ActualCoreCount] <= [RequestedCoreCount] AND [ActualDeepCount] <= [MaximumDeepCount]");
                table.HasCheckConstraint(
                    "CK_SessionStudents_Status",
                    "[Status] IN ('Pending', 'InProgress', 'Completed', 'Cancelled')");
                table.HasCheckConstraint("CK_SessionStudents_Times", "[StartTime] IS NULL OR [EndTime] IS NULL OR [EndTime] >= [StartTime]");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.StartTime)
                .HasColumnType("datetime2(3)")
                .HasConversion<NullableUtcDateTimeConverter>();
            entity.Property(x => x.EndTime)
                .HasColumnType("datetime2(3)")
                .HasConversion<NullableUtcDateTimeConverter>();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
            entity.HasIndex(x => x.StudentId);
            entity.HasOne(x => x.Session)
                .WithMany(x => x.SessionStudents)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Student)
                .WithMany(x => x.SessionStudents)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExamQuestionAssignment>(entity =>
        {
            entity.ToTable("ExamQuestionAssignments", table =>
            {
                table.HasCheckConstraint("CK_ExamQuestionAssignments_Position", "[Position] >= 1");
                table.HasCheckConstraint("CK_ExamQuestionAssignments_Order", "[Order] >= 0");
                table.HasCheckConstraint(
                    "CK_ExamQuestionAssignments_Category",
                    "[Category] IN ('Core', 'Deep')");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.SessionStudentId, x.QuestionId }).IsUnique();
            entity.HasIndex(x => new { x.SessionStudentId, x.Position, x.Order });
            entity.HasOne(x => x.SessionStudent)
                .WithMany(x => x.ExamQuestionAssignments)
                .HasForeignKey(x => x.SessionStudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question)
                .WithMany(x => x.ExamQuestionAssignments)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
