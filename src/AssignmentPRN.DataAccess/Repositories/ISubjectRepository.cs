using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Repositories;

public interface ISubjectRepository
{
    Task<IReadOnlyList<Subject>> ListAsync(CancellationToken cancellationToken = default);

    Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, int excludingId, CancellationToken cancellationToken = default);

    Task<Subject> CreateAsync(Subject subject, CancellationToken cancellationToken = default);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
