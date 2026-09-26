using AssignmentPRN.DataAccess.Common;
using AssignmentPRN.DataAccess.Daos;
using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;

namespace AssignmentPRN.DataAccess.Repositories;

public sealed class QuestionRepository : IQuestionRepository
{
    private readonly IEntityDao<Question> _questionDao;
    private readonly IEntityDao<Subject> _subjectDao;
    private readonly IExamSessionDao _examSessionDao;

    public QuestionRepository(
        IEntityDao<Question> questionDao,
        IEntityDao<Subject> subjectDao,
        IExamSessionDao examSessionDao)
    {
        _questionDao = questionDao ?? throw new ArgumentNullException(nameof(questionDao));
        _subjectDao = subjectDao ?? throw new ArgumentNullException(nameof(subjectDao));
        _examSessionDao = examSessionDao ?? throw new ArgumentNullException(nameof(examSessionDao));
    }

    public async Task<IReadOnlyList<Question>> ListBySubjectAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        if (subjectId <= 0)
        {
            return Array.Empty<Question>();
        }

        var questions = await _questionDao.ListAsync(
            question => question.SubjectId == subjectId,
            cancellationToken);

        return questions
            .OrderBy(question => question.Category)
            .ThenBy(question => question.Difficulty)
            .ThenBy(question => question.Topic)
            .ToList();
    }

    public Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult<Question?>(null);
        }

        return _questionDao.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Question> CreateAsync(Question question, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (question.Id != 0)
        {
            throw new ArgumentException("A new question cannot have an identifier.", nameof(question));
        }

        RepositoryInput.PositiveId(question.SubjectId, nameof(question.SubjectId));
        ValidateQuestion(question);

        if (!await _subjectDao.ExistsAsync(subject => subject.Id == question.SubjectId, cancellationToken))
        {
            throw new KeyNotFoundException($"Subject {question.SubjectId} was not found.");
        }

        await _questionDao.AddAsync(question, cancellationToken);
        return question;
    }

    public async Task UpdateAsync(Question question, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);
        RepositoryInput.PositiveId(question.Id, nameof(question.Id));
        RepositoryInput.PositiveId(question.SubjectId, nameof(question.SubjectId));
        ValidateQuestion(question);

        if (!await _subjectDao.ExistsAsync(subject => subject.Id == question.SubjectId, cancellationToken))
        {
            throw new KeyNotFoundException($"Subject {question.SubjectId} was not found.");
        }

        var existing = await _questionDao.GetByIdAsync(question.Id, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Question {question.Id} was not found.");
        }

        var assignmentIdentityChanged = existing.SubjectId != question.SubjectId
            || existing.Category != question.Category;
        if (assignmentIdentityChanged
            && await _examSessionDao.HasQuestionAssignmentsAsync(question.Id, cancellationToken))
        {
            throw new InvalidOperationException(
                "The subject and category cannot be changed after the question has been assigned.");
        }

        existing.SubjectId = question.SubjectId;
        existing.Category = question.Category;
        existing.Topic = question.Topic;
        existing.Content = question.Content;
        existing.Difficulty = question.Difficulty;
        await _questionDao.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return;
        }

        var question = await _questionDao.GetByIdAsync(id, cancellationToken);
        if (question is not null)
        {
            await _questionDao.DeleteAsync(question, cancellationToken);
        }
    }

    private static void ValidateQuestion(Question question)
    {
        RepositoryInput.EnsureEnum(question.Category, nameof(question.Category));
        question.Topic = RepositoryInput.RequiredText(question.Topic, nameof(question.Topic), 200);
        question.Content = RepositoryInput.RequiredText(question.Content, nameof(question.Content), 4000);

        if (question.Difficulty is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(question.Difficulty), "Difficulty must be between 1 and 5.");
        }
    }
}
