using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class Subject : IEntity<int>
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
