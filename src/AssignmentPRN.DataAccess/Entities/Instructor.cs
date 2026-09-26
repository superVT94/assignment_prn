using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class Instructor : IEntity<int>
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
