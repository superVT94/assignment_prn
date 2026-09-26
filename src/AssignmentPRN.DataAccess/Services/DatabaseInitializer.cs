using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AssignmentPRN.DataAccess.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.EnsureCreatedAsync(cancellationToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (await HasAnyDataAsync(cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var instructors = CreateInstructors();
        var subjects = CreateSubjects();
        var students = CreateStudents();

        _context.Instructors.AddRange(instructors);
        _context.Subjects.AddRange(subjects);
        _context.Students.AddRange(students);
        await _context.SaveChangesAsync(cancellationToken);

        var subjectByCode = subjects.ToDictionary(subject => subject.Code, StringComparer.OrdinalIgnoreCase);
        var questions = CreateQuestions(subjectByCode);
        _context.Questions.AddRange(questions);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation(
            "Seeded demonstration data with {InstructorCount} instructor, {SubjectCount} subjects, {StudentCount} students, and {QuestionCount} questions.",
            instructors.Count,
            subjects.Count,
            students.Count,
            questions.Count);
    }

    private async Task<bool> HasAnyDataAsync(CancellationToken cancellationToken)
    {
        return await _context.Instructors.AnyAsync(cancellationToken)
            || await _context.Subjects.AnyAsync(cancellationToken)
            || await _context.Students.AnyAsync(cancellationToken)
            || await _context.Questions.AnyAsync(cancellationToken)
            || await _context.ExamSessions.AnyAsync(cancellationToken)
            || await _context.SessionStudents.AnyAsync(cancellationToken)
            || await _context.ExamQuestionAssignments.AnyAsync(cancellationToken);
    }

    private static List<Instructor> CreateInstructors()
    {
        return
        [
            new Instructor
            {
                Name = "Dr. Minh Nguyen",
                Email = "minh.nguyen@assignment-prn.local"
            },
            new Instructor
            {
                Name = "Dr. Lan Tran",
                Email = "lan.tran@assignment-prn.local"
            }
        ];
    }

    private static List<Subject> CreateSubjects()
    {
        return
        [
            new Subject
            {
                Code = "PRN204",
                Name = "Object-Oriented Programming",
                Description = "Core object-oriented programming concepts and modern C# practices."
            },
            new Subject
            {
                Code = "PRN301",
                Name = "Web Application Development",
                Description = "Data access, web application architecture, and production practices."
            }
        ];
    }

    private static List<Student> CreateStudents()
    {
        return
        [
            new Student { Code = "ST001", Name = "An Nguyen" },
            new Student { Code = "ST002", Name = "Bao Tran" },
            new Student { Code = "ST003", Name = "Chi Le" },
            new Student { Code = "ST004", Name = "Duc Pham" },
            new Student { Code = "ST005", Name = "Gia Hoang" },
            new Student { Code = "ST006", Name = "Hieu Vo" }
        ];
    }

    private static List<Question> CreateQuestions(IReadOnlyDictionary<string, Subject> subjects)
    {
        var programming = subjects["PRN204"];
        var web = subjects["PRN301"];

        return
        [
            CreateQuestion(programming, QuestionCategory.Core, "Classes and objects", "Explain the purpose of a class and the difference between a class and an object.", 1),
            CreateQuestion(programming, QuestionCategory.Core, "Encapsulation", "Describe encapsulation and provide a C# example that protects an invariant.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "Inheritance", "Compare interface implementation and class inheritance, including when each design is appropriate.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "Collections", "Choose an appropriate .NET collection for indexed lookup, ordered traversal, and uniqueness requirements.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "LINQ", "Write a LINQ query that groups orders by customer and calculates the total value of completed orders.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "Asynchronous programming", "Explain the purpose of async and await and how cancellation is propagated through an asynchronous call chain.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "Exception handling", "Explain how to design exception boundaries, preserve exception details, and avoid swallowing failures.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "Interfaces", "Design an interface for a clock and explain how dependency inversion improves testability.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "Generics", "Explain generic type parameters, constraints, and one benefit over using object everywhere.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "Delegates and events", "Contrast delegates with events and give a suitable publisher-subscriber example.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "Nullable reference types", "Use nullable annotations to model a method that may return no value without hiding a null-related defect.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "Value and reference types", "Compare value and reference types and show how mutation affects copies in C#.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "Memory management", "Describe the role of the garbage collector and the circumstances in which unmanaged resources require special handling.", 3),
            CreateQuestion(programming, QuestionCategory.Core, "Unit testing", "Write a focused unit test for a service with one external dependency and explain the test double used.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "SOLID principles", "Identify the SOLID principle most directly related to a class having one reason to change and give an example.", 2),
            CreateQuestion(programming, QuestionCategory.Core, "Algorithms", "Compare binary search and linear search by precondition and asymptotic time complexity.", 2),
            CreateQuestion(programming, QuestionCategory.Deep, "Application architecture", "Design a layered application architecture and explain the direction of dependencies between layers.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Concurrency", "Compare a monitor, SemaphoreSlim, and an immutable state design for protecting shared state in C#.", 5),
            CreateQuestion(programming, QuestionCategory.Deep, "Performance analysis", "Given a slow asynchronous method, describe how you would measure latency, allocation, and contention before optimizing.", 5),
            CreateQuestion(programming, QuestionCategory.Deep, "Secure coding", "Identify risks of deserializing untrusted object graphs and explain safer alternatives or safeguards.", 5),
            CreateQuestion(programming, QuestionCategory.Deep, "Transactions", "Explain transaction boundaries, isolation, and why a business operation should not open unrelated transactions.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Design patterns", "Choose a design pattern for a pluggable notification pipeline and justify the trade-offs of the pattern.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Dependency injection", "Design constructor-injected dependencies for a service with optional decorators and explain lifetime choices.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Caching", "Compare an in-process cache with a distributed cache and describe invalidation failure modes.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Serialization", "Explain versioning and compatibility concerns when serializing a domain contract for a message queue.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Resilience", "Design retry, timeout, and circuit-breaker policies for a network dependency without causing retry storms.", 5),
            CreateQuestion(programming, QuestionCategory.Deep, "Domain modeling", "Turn a vague business requirement into a bounded context, aggregate boundary, and invariant-focused model.", 5),
            CreateQuestion(programming, QuestionCategory.Deep, "Observability", "Define structured logs, metrics, and traces for diagnosing a failed background job.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Code quality", "Evaluate a design that exposes mutable collections and propose an API that makes invalid states harder to represent.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Testing strategy", "Design a test pyramid for a domain service, an EF repository, and an HTTP endpoint.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Data structures", "Select data structures for a workload with frequent insertions, prefix searches, and stable ordering.", 5),
            CreateQuestion(programming, QuestionCategory.Deep, "API contracts", "Design a versioned API contract for a long-running operation and explain idempotency requirements.", 4),
            CreateQuestion(programming, QuestionCategory.Deep, "Threat modeling", "Use a threat modeling approach to identify risks in a public question-bank upload workflow.", 5),
            CreateQuestion(web, QuestionCategory.Core, "Relational modeling", "Explain primary keys, foreign keys, and the role of a unique constraint in a relational model.", 1),
            CreateQuestion(web, QuestionCategory.Core, "Normalization", "Describe why a table may be normalized and identify a tradeoff introduced by denormalization.", 3),
            CreateQuestion(web, QuestionCategory.Core, "SQL filtering", "Write a parameterized SQL query that returns subjects matching a code prefix and explain why parameters matter.", 2),
            CreateQuestion(web, QuestionCategory.Core, "Transactions", "Explain when to use a database transaction and what commit and rollback mean for a request.", 3),
            CreateQuestion(web, QuestionCategory.Core, "Indexes", "Explain how an index improves reads and what write and storage costs it introduces.", 3),
            CreateQuestion(web, QuestionCategory.Core, "Entity Framework mappings", "Configure a one-to-many relationship with a required foreign key and a restricted delete behavior.", 3),
            CreateQuestion(web, QuestionCategory.Core, "Web request lifecycle", "Trace a request from routing to an application handler and back through model binding and a view.", 2),
            CreateQuestion(web, QuestionCategory.Core, "Model validation", "Explain validation at an application boundary and why validation should not replace database constraints.", 2),
            CreateQuestion(web, QuestionCategory.Core, "Dependency injection", "Explain constructor dependency injection and how a scoped service differs from a singleton service.", 2),
            CreateQuestion(web, QuestionCategory.Core, "Configuration", "Explain why connection strings and environment-specific settings should be supplied through configuration.", 2),
            CreateQuestion(web, QuestionCategory.Core, "Async database calls", "Explain why asynchronous Entity Framework operations should be awaited and how cancellation is passed.", 3),
            CreateQuestion(web, QuestionCategory.Core, "Repository boundaries", "Describe the responsibility of a repository and why it should not expose an EF query object to callers.", 2),
            CreateQuestion(web, QuestionCategory.Core, "REST endpoints", "Design a REST endpoint for creating a resource and describe validation and response status codes.", 3),
            CreateQuestion(web, QuestionCategory.Core, "Git workflow", "Explain the purpose of a feature branch, a focused commit, and a pull request review.", 1),
            CreateQuestion(web, QuestionCategory.Core, "Unit testing web code", "Write a unit test for an application endpoint or service using a fake dependency and a meaningful assertion.", 2),
            CreateQuestion(web, QuestionCategory.Core, "Data transfer objects", "Separate persistence entities from API response models and explain the benefit of the boundary.", 2),
            CreateQuestion(web, QuestionCategory.Deep, "Query optimization", "Analyze a slow SQL Server query using execution plans and propose an evidence-based optimization sequence.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Distributed data", "Compare replication, partitioning, and sharding for a growing multi-tenant system.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Consistency", "Compare strong and eventual consistency in an order-processing system and describe compensating actions.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Authentication and authorization", "Design authentication and authorization boundaries for an oral-examination administration system.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Security review", "Threat-model a session-creation endpoint that accepts student and question identifiers from an administrator.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Caching at scale", "Evaluate cache-aside, write-through, and invalidation strategies for subject and question-bank reads.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "Concurrency control", "Compare optimistic and pessimistic concurrency for edits to an exam session aggregate.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Deployment", "Plan a safe database-backed deployment with migrations, backward-compatible changes, and rollback.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Observability", "Define useful telemetry for database saturation, failed requests, and background initialization.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "Domain boundaries", "Design repository and service boundaries that keep request handlers thin without leaking persistence details.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "Performance testing", "Design a load test for concurrent session creation and identify the most useful latency percentiles.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "Security of secrets", "Explain how to store, rotate, and audit database credentials without committing them to source control.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "Data retention", "Design retention and archival rules for examination sessions while preserving auditability.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "API versioning", "Plan a compatibility strategy when a question assignment response must gain a new field.", 4),
            CreateQuestion(web, QuestionCategory.Deep, "Resilient integrations", "Design timeout, retry, and idempotency behavior for a notification service called after a session is created.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Testing migrations", "Verify a schema migration against a production-like data set and detect destructive changes before deployment.", 5),
            CreateQuestion(web, QuestionCategory.Deep, "Architecture trade-offs", "Compare modular monolith, service-oriented, and event-driven designs for a small examination system.", 5)
        ];
    }

    private static Question CreateQuestion(
        Subject subject,
        QuestionCategory category,
        string topic,
        string content,
        int difficulty)
    {
        return new Question
        {
            SubjectId = subject.Id,
            Category = category,
            Topic = topic,
            Content = content,
            Difficulty = difficulty
        };
    }
}
