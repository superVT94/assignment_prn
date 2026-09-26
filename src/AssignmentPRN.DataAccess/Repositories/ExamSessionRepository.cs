using AssignmentPRN.DataAccess.Contracts;
using AssignmentPRN.DataAccess.Daos;
using AssignmentPRN.DataAccess.Entities;

namespace AssignmentPRN.DataAccess.Repositories;

public sealed class ExamSessionRepository : IExamSessionRepository
{
    private readonly IExamSessionDao _dao;

    public ExamSessionRepository(IExamSessionDao dao)
    {
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
    }

    public Task<IReadOnlyList<ExamSessionListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _dao.ListAsync(cancellationToken);
    }

    public Task<ExamSessionDetail?> GetDetailAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return _dao.GetDetailAsync(sessionId, cancellationToken);
    }

    public Task<ExamSessionDetail> CreateAsync(
        ExamSessionAggregateInput input,
        CancellationToken cancellationToken = default)
    {
        return _dao.CreateAsync(input, cancellationToken);
    }

    public Task<ExamSessionDetail> CreateAsync(
        ExamSession session,
        CancellationToken cancellationToken = default)
    {
        return _dao.CreateAsync(session, cancellationToken);
    }

    public Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return _dao.DeleteAsync(sessionId, cancellationToken);
    }
}
