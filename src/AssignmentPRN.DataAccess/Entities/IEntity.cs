namespace AssignmentPRN.DataAccess.Entities;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}
