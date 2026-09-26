using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class ExamSession : IEntity<int>
{
    public int Id { get; set; }

    public int InstructorId { get; set; }

    public Instructor Instructor { get; set; } = null!;

    public int SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public ExamSessionStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<SessionStudent> SessionStudents { get; set; } = new List<SessionStudent>();
}
