using AssignmentPRN.DataAccess.Contracts;
using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Repositories;

public interface IExamSessionRepository
{
    Task<IReadOnlyList<ExamSessionListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<ExamSessionDetail?> GetDetailAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<ExamSessionDetail> CreateAsync(
        ExamSessionAggregateInput input,
        CancellationToken cancellationToken = default);

    Task<ExamSessionDetail> CreateAsync(
        ExamSession session,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
}
