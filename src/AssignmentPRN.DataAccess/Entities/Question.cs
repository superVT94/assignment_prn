using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class Question : IEntity<int>
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    public QuestionCategory Category { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int Difficulty { get; set; }

    public ICollection<ExamQuestionAssignment> ExamQuestionAssignments { get; set; } = new List<ExamQuestionAssignment>();
}
