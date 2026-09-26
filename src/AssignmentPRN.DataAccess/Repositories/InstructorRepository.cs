using AssignmentPRN.DataAccess.Common;
using AssignmentPRN.DataAccess.Daos;
using AssignmentPRN.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentPRN.DataAccess.Repositories;

public sealed class InstructorRepository : IInstructorRepository
{
    private readonly IEntityDao<Instructor> _dao;

    public InstructorRepository(IEntityDao<Instructor> dao)
    {
        _dao = dao ?? throw new ArgumentNullException(nameof(dao));
    }

    public async Task<IReadOnlyList<Instructor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var instructors = await _dao.ListAsync(cancellationToken);
        return instructors.OrderBy(instructor => instructor.Name).ToList();
    }

    public Task<Instructor?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult<Instructor?>(null);
        }

        return _dao.GetByIdAsync(id, cancellationToken);
    }

    public Task<Instructor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RepositoryInput.NormalizeEmail(email);
        return _dao.GetAsync(instructor => instructor.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RepositoryInput.NormalizeEmail(email);
        return _dao.ExistsAsync(instructor => instructor.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, int excludingId, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RepositoryInput.NormalizeEmail(email);
        return _dao.ExistsAsync(instructor => instructor.Email == normalizedEmail && instructor.Id != excludingId, cancellationToken);
    }

    public async Task<Instructor> CreateAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instructor);

        if (instructor.Id != 0)
        {
            throw new ArgumentException("A new instructor cannot have an identifier.", nameof(instructor));
        }

        var name = RepositoryInput.RequiredText(instructor.Name, "Name", 200);
        var email = RepositoryInput.NormalizeEmail(instructor.Email);

        if (await ExistsByEmailAsync(email, cancellationToken))
        {
            throw new DuplicateEntityException($"An instructor with email '{email}' already exists.");
        }

        instructor.Name = name;
        instructor.Email = email;
        await _dao.AddAsync(instructor, cancellationToken);
        return instructor;
    }

    public async Task UpdateAsync(Instructor instructor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instructor);
        RepositoryInput.PositiveId(instructor.Id, nameof(instructor.Id));

        var name = RepositoryInput.RequiredText(instructor.Name, "Name", 200);
        var email = RepositoryInput.NormalizeEmail(instructor.Email);

        var existing = await _dao.GetByIdAsync(instructor.Id, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Instructor {instructor.Id} was not found.");
        }

        if (await ExistsByEmailAsync(email, instructor.Id, cancellationToken))
        {
            throw new DuplicateEntityException($"An instructor with email '{email}' already exists.");
        }

        existing.Name = name;
        existing.Email = email;
        instructor.Name = existing.Name;
        instructor.Email = existing.Email;
        await _dao.UpdateAsync(existing, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return;
        }

        var instructor = await _dao.GetByIdAsync(id, cancellationToken);
        if (instructor is not null)
        {
            await _dao.DeleteAsync(instructor, cancellationToken);
        }
    }
}
