using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Repositories;

public interface IQuestionRepository
{
    Task<IReadOnlyList<Question>> ListBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);

    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Question> CreateAsync(Question question, CancellationToken cancellationToken = default);

    Task UpdateAsync(Question question, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
