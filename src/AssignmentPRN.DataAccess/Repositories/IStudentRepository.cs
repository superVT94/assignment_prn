using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyList<Student>> ListAsync(CancellationToken cancellationToken = default);

    Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Student?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, int excludingId, CancellationToken cancellationToken = default);

    Task<Student> CreateAsync(Student student, CancellationToken cancellationToken = default);

    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
