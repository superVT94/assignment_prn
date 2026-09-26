using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Contracts;

public sealed class ExamSessionAggregateInput
{
    public int InstructorId { get; init; }

    public int SubjectId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public ExamSessionStatus Status { get; init; } = ExamSessionStatus.Draft;

    public IReadOnlyCollection<ExamSessionParticipantInput> Participants { get; init; } = Array.Empty<ExamSessionParticipantInput>();
}

public sealed class ExamSessionParticipantInput
{
    public int StudentId { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int RequestedCoreCount { get; init; }

    public int MaximumDeepCount { get; init; }

    public int ActualCoreCount { get; init; }

    public int ActualDeepCount { get; init; }

    public SessionStudentStatus Status { get; init; } = SessionStudentStatus.Pending;

    public IReadOnlyCollection<ExamQuestionAssignmentInput> Assignments { get; init; } = Array.Empty<ExamQuestionAssignmentInput>();
}

public sealed class ExamQuestionAssignmentInput
{
    public int QuestionId { get; init; }

    public QuestionCategory Category { get; init; }

    public int Position { get; init; }

    public int Order { get; init; }
}
