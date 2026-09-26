using AssignmentPRN.DataAccess.Common;
using AssignmentPRN.DataAccess.Daos;
using AssignmentPRN.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentPRN.DataAccess.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly IEntityDao<Subject> _dao;

    public SubjectRepository(IEntityDao<Subject> dao)
    {
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
    }

    public async Task<IReadOnlyList<Subject>> ListAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _dao.ListAsync(cancellationToken);
        return subjects.OrderBy(subject => subject.Code).ToList();
    }

    public Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult<Subject?>(null);
        }

        return _dao.GetByIdAsync(id, cancellationToken);
    }

    public Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = RepositoryInput.NormalizeCode(code, "Code", 20);
        return _dao.GetAsync(subject => subject.Code == normalizedCode, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = RepositoryInput.NormalizeCode(code, "Code", 20);
        return _dao.ExistsAsync(subject => subject.Code == normalizedCode, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, int excludingId, CancellationToken cancellationToken = default)
    {
        var normalizedCode = RepositoryInput.NormalizeCode(code, "Code", 20);
        return _dao.ExistsAsync(subject => subject.Code == normalizedCode && subject.Id != excludingId, cancellationToken);
    }

    public async Task<Subject> CreateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);

        if (subject.Id != 0)
        {
            throw new ArgumentException("A new subject cannot have an identifier.", nameof(subject));
        }

        var code = RepositoryInput.NormalizeCode(subject.Code, "Code", 20);
        var name = RepositoryInput.RequiredText(subject.Name, "Name", 200);
        var description = RepositoryInput.OptionalText(subject.Description, "Description", 1000);

        if (await ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateEntityException($"A subject with code '{code}' already exists.");
        }

        subject.Code = code;
        subject.Name = name;
        subject.Description = description;
        await _dao.AddAsync(subject, cancellationToken);
        return subject;
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        RepositoryInput.PositiveId(subject.Id, nameof(subject.Id));

        var code = RepositoryInput.NormalizeCode(subject.Code, "Code", 20);
        var name = RepositoryInput.RequiredText(subject.Name, "Name", 200);
        var description = RepositoryInput.OptionalText(subject.Description, "Description", 1000);

        var existing = await _dao.GetByIdAsync(subject.Id, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Subject {subject.Id} was not found.");
        }

        if (await ExistsByCodeAsync(code, subject.Id, cancellationToken))
        {
            throw new DuplicateEntityException($"A subject with code '{code}' already exists.");
        }

        existing.Code = code;
        existing.Name = name;
        existing.Description = description;
        subject.Code = existing.Code;
        subject.Name = existing.Name;
        subject.Description = existing.Description;
        await _dao.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return;
        }

        var subject = await _dao.GetByIdAsync(id, cancellationToken);
        if (subject is not null)
        {
            await _dao.DeleteAsync(subject, cancellationToken);
        }
    }
}
