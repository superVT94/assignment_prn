using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Entities;

public sealed class Student : IEntity<int>
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<SessionStudent> SessionStudents { get; set; } = new List<SessionStudent>();
}
