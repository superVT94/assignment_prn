using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class ExamQuestionAssignment : IEntity<int>
{
    public int Id { get; set; }

    public int SessionStudentId { get; set; }

    public SessionStudent SessionStudent { get; set; } = null!;

    public int QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public QuestionCategory Category { get; set; }

    public int Position { get; set; }

    public int Order { get; set; }
}
