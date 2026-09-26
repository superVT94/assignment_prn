using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Contracts;

public sealed class ExamSessionListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public ExamSessionStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public int InstructorId { get; init; }

    public string InstructorName { get; init; } = string.Empty;

    public string InstructorEmail { get; init; } = string.Empty;

    public int SubjectId { get; init; }

    public string SubjectCode { get; init; } = string.Empty;

    public string SubjectName { get; init; } = string.Empty;

    public int ParticipantCount { get; init; }

    public int AssignmentCount { get; init; }

    public int CoreQuestionCount { get; init; }

    public int DeepQuestionCount { get; init; }
}

public sealed class ExamSessionDetail
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public ExamSessionStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public InstructorSummary Instructor { get; init; } = new();

    public SubjectSummary Subject { get; init; } = new();

    public IReadOnlyList<SessionStudentDetail> Participants { get; init; } = Array.Empty<SessionStudentDetail>();
}

public sealed class InstructorSummary
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class SubjectSummary
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class SessionStudentDetail
{
    public int Id { get; init; }

    public int StudentId { get; init; }

    public string StudentCode { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int RequestedCoreCount { get; init; }

    public int MaximumDeepCount { get; init; }

    public int ActualCoreCount { get; init; }

    public int ActualDeepCount { get; init; }

    public SessionStudentStatus Status { get; init; }

    public IReadOnlyList<ExamQuestionAssignmentDetail> Assignments { get; init; } = Array.Empty<ExamQuestionAssignmentDetail>();
}

public sealed class ExamQuestionAssignmentDetail
{
    public int Id { get; init; }

    public int QuestionId { get; init; }

    public QuestionCategory Category { get; init; }

    public int Position { get; init; }

    public int Order { get; init; }

    public QuestionSummary Question { get; init; } = new();
}

public sealed class QuestionSummary
{
    public int Id { get; init; }

    public QuestionCategory Category { get; init; }

    public string Topic { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int Difficulty { get; init; }
}
