using AssignmentPRN.DataAccess.Common;
using AssignmentPRN.DataAccess.Daos;
using AssignmentPRN.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentPRN.DataAccess.Repositories;

public sealed class StudentRepository : IStudentRepository
{
    private readonly IEntityDao<Student> _dao;

    public StudentRepository(IEntityDao<Student> dao)
    {
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
    }

    public async Task<IReadOnlyList<Student>> ListAsync(CancellationToken cancellationToken = default)
    {
        var students = await _dao.ListAsync(cancellationToken);
        return students.OrderBy(student => student.Code).ToList();
    }

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult<Student?>(null);
        }

        return _dao.GetByIdAsync(id, cancellationToken);
    }

    public Task<Student?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = RepositoryInput.NormalizeCode(code, "Code", 30);
        return _dao.GetAsync(student => student.Code == normalizedCode, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = RepositoryInput.NormalizeCode(code, "Code", 30);
        return _dao.ExistsAsync(student => student.Code == normalizedCode, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, int excludingId, CancellationToken cancellationToken = default)
    {
        var normalizedCode = RepositoryInput.NormalizeCode(code, "Code", 30);
        return _dao.ExistsAsync(student => student.Code == normalizedCode && student.Id != excludingId, cancellationToken);
    }

    public async Task<Student> CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);

        if (student.Id != 0)
        {
            throw new ArgumentException("A new student cannot have an identifier.", nameof(student));
        }

        var code = RepositoryInput.NormalizeCode(student.Code, "Code", 30);
        var name = RepositoryInput.RequiredText(student.Name, "Name", 200);

        if (await ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateEntityException($"A student with code '{code}' already exists.");
        }

        student.Code = code;
        student.Name = name;
        await _dao.AddAsync(student, cancellationToken);
        return student;
    }

    public async Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(student);
        RepositoryInput.PositiveId(student.Id, nameof(student.Id));

        var code = RepositoryInput.NormalizeCode(student.Code, "Code", 30);
        var name = RepositoryInput.RequiredText(student.Name, "Name", 200);

        var existing = await _dao.GetByIdAsync(student.Id, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Student {student.Id} was not found.");
        }

        if (await ExistsByCodeAsync(code, student.Id, cancellationToken))
        {
            throw new DuplicateEntityException($"A student with code '{code}' already exists.");
        }

        existing.Code = code;
        existing.Name = name;
        student.Code = existing.Code;
        student.Name = existing.Name;
        await _dao.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return;
        }

        var student = await _dao.GetByIdAsync(id, cancellationToken);
        if (student is not null)
        {
            await _dao.DeleteAsync(student, cancellationToken);
        }
    }
}
