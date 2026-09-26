using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class SessionStudent : IEntity<int>
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public ExamSession Session { get; set; } = null!;

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int RequestedCoreCount { get; set; }

    public int MaximumDeepCount { get; set; }

    public int ActualCoreCount { get; set; }

    public int ActualDeepCount { get; set; }

    public SessionStudentStatus Status { get; set; }

    public ICollection<ExamQuestionAssignment> ExamQuestionAssignments { get; set; } = new List<ExamQuestionAssignment>();
}
