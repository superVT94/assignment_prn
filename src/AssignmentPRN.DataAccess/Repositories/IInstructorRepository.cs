using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Repositories;

public interface IInstructorRepository
{
    Task<IReadOnlyList<Instructor>> ListAsync(CancellationToken cancellationToken = default);

    Task<Instructor?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Instructor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, int excludingId, CancellationToken cancellationToken = default);

    Task<Instructor> CreateAsync(Instructor instructor, CancellationToken cancellationToken = default);

    Task UpdateAsync(Instructor instructor, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
