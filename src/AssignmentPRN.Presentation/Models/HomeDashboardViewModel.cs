namespace AssignmentPRN.Presentation.Models;

public sealed class HomeDashboardViewModel
{
    public int? InstructorCount { get; init; }

    public int? SubjectCount { get; init; }

    public int? StudentCount { get; init; }

    public int? QuestionCount { get; init; }

    public int? SessionCount { get; init; }

    public int? ParticipantCount { get; init; }

    public string? ServiceMessage { get; init; }
}
