using AssignmentPRN.DataAccess.Common;
using AssignmentPRN.DataAccess.Contracts;
using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AssignmentPRN.DataAccess.Daos;

public interface IExamSessionDao
{
    Task<IReadOnlyList<ExamSessionListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<ExamSessionDetail?> GetDetailAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<bool> HasQuestionAssignmentsAsync(int questionId, CancellationToken cancellationToken = default);

    Task<ExamSessionDetail> CreateAsync(
        ExamSessionAggregateInput input,
        CancellationToken cancellationToken = default);

    Task<ExamSessionDetail> CreateAsync(
        ExamSession session,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
}

public sealed class ExamSessionDao : IExamSessionDao
{
    private readonly AppDbContext _context;

    public ExamSessionDao(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<ExamSessionListItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ExamSessions
            .AsNoTracking()
            .OrderByDescending(session => session.Date)
            .ThenByDescending(session => session.CreatedAt)
            .Select(session => new ExamSessionListItem
            {
                Id = session.Id,
                Name = session.Name,
                Date = session.Date,
                Status = session.Status,
                CreatedAt = session.CreatedAt,
                InstructorId = session.InstructorId,
                InstructorName = session.Instructor.Name,
                InstructorEmail = session.Instructor.Email,
                SubjectId = session.SubjectId,
                SubjectCode = session.Subject.Code,
                SubjectName = session.Subject.Name,
                ParticipantCount = session.SessionStudents.Count(),
                AssignmentCount = session.SessionStudents
                    .SelectMany(participant => participant.ExamQuestionAssignments)
                    .Count(),
                CoreQuestionCount = session.SessionStudents
                    .SelectMany(participant => participant.ExamQuestionAssignments)
                    .Count(assignment => assignment.Category == QuestionCategory.Core),
                DeepQuestionCount = session.SessionStudents
                    .SelectMany(participant => participant.ExamQuestionAssignments)
                    .Count(assignment => assignment.Category == QuestionCategory.Deep)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ExamSessionDetail?> GetDetailAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0)
        {
            return null;
        }

        return await LoadDetailAsync(sessionId, cancellationToken);
    }

    public Task<bool> HasQuestionAssignmentsAsync(
        int questionId,
        CancellationToken cancellationToken = default)
    {
        if (questionId <= 0)
        {
            return Task.FromResult(false);
        }

        return _context.ExamQuestionAssignments
            .AsNoTracking()
            .AnyAsync(assignment => assignment.QuestionId == questionId, cancellationToken);
    }

    public Task<ExamSessionDetail> CreateAsync(
        ExamSessionAggregateInput input,
        CancellationToken cancellationToken = default)
    {
        var normalizedInput = NormalizeInput(input);
        return CreateNormalizedAsync(normalizedInput, cancellationToken);
    }

    public Task<ExamSessionDetail> CreateAsync(
        ExamSession session,
        CancellationToken cancellationToken = default)
    {
        var input = ToInput(session);
        var normalizedInput = NormalizeInput(input);
        return CreateNormalizedAsync(normalizedInput, cancellationToken);
    }

    public async Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0)
        {
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var session = await _context.ExamSessions
            .SingleOrDefaultAsync(existing => existing.Id == sessionId, cancellationToken);

        if (session is not null)
        {
            _context.ExamSessions.Remove(session);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<ExamSessionDetail> CreateNormalizedAsync(
        ExamSessionAggregateInput input,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await ValidateReferencesAsync(input, cancellationToken);

        var session = new ExamSession
        {
            InstructorId = input.InstructorId,
            SubjectId = input.SubjectId,
            Name = input.Name,
            Date = input.Date,
            Status = input.Status,
            CreatedAt = DateTime.UtcNow,
            SessionStudents = new List<SessionStudent>()
        };

        foreach (var participantInput in input.Participants)
        {
            var participant = new SessionStudent
            {
                SessionId = 0,
                StudentId = participantInput.StudentId,
                StartTime = participantInput.StartTime,
                EndTime = participantInput.EndTime,
                RequestedCoreCount = participantInput.RequestedCoreCount,
                MaximumDeepCount = participantInput.MaximumDeepCount,
                ActualCoreCount = participantInput.ActualCoreCount,
                ActualDeepCount = participantInput.ActualDeepCount,
                Status = participantInput.Status,
                ExamQuestionAssignments = new List<ExamQuestionAssignment>()
            };

            foreach (var assignmentInput in participantInput.Assignments)
            {
                participant.ExamQuestionAssignments.Add(new ExamQuestionAssignment
                {
                    QuestionId = assignmentInput.QuestionId,
                    Category = assignmentInput.Category,
                    Position = assignmentInput.Position,
                    Order = assignmentInput.Order
                });
            }

            session.SessionStudents.Add(participant);
        }

        _context.ExamSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        var detail = await LoadDetailAsync(session.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Exam session {session.Id} could not be read after creation.");

        await transaction.CommitAsync(cancellationToken);
        return detail;
    }

    private async Task ValidateReferencesAsync(
        ExamSessionAggregateInput input,
        CancellationToken cancellationToken)
    {
        if (!await _context.Instructors.AnyAsync(instructor => instructor.Id == input.InstructorId, cancellationToken))
        {
            throw new KeyNotFoundException($"Instructor {input.InstructorId} was not found.");
        }

        if (!await _context.Subjects.AnyAsync(subject => subject.Id == input.SubjectId, cancellationToken))
        {
            throw new KeyNotFoundException($"Subject {input.SubjectId} was not found.");
        }

        var existingWindows = await _context.ExamSessions
            .AsNoTracking()
            .Where(session => session.InstructorId == input.InstructorId
                && session.Status != ExamSessionStatus.Cancelled)
            .SelectMany(session => session.SessionStudents)
            .Where(participant => participant.StartTime != null && participant.EndTime != null)
            .Select(participant => new { participant.StartTime, participant.EndTime })
            .ToListAsync(cancellationToken);
        foreach (var participant in input.Participants)
        {
            var startTime = participant.StartTime!.Value;
            var endTime = participant.EndTime!.Value;
            if (existingWindows.Any(window => window.StartTime!.Value < endTime
                && window.EndTime!.Value > startTime))
            {
                throw new InvalidOperationException(
                    $"Instructor {input.InstructorId} already has an exam window that overlaps this session.");
            }
        }

        var studentIds = input.Participants.Select(participant => participant.StudentId).Distinct().ToList();
        if (studentIds.Count > 0)
        {
            var existingStudentIds = await _context.Students
                .Where(student => studentIds.Contains(student.Id))
                .Select(student => student.Id)
                .ToListAsync(cancellationToken);

            if (existingStudentIds.Count != studentIds.Count)
            {
                var missingStudentId = studentIds.Except(existingStudentIds).First();
                throw new KeyNotFoundException($"Student {missingStudentId} was not found.");
            }
        }

        var questionIds = input.Participants
            .SelectMany(participant => participant.Assignments)
            .Select(assignment => assignment.QuestionId)
            .Distinct()
            .ToList();

        if (questionIds.Count == 0)
        {
            return;
        }

        var questions = await _context.Questions
            .Where(question => questionIds.Contains(question.Id) && question.SubjectId == input.SubjectId)
            .Select(question => new { question.Id, question.Category })
            .ToListAsync(cancellationToken);

        if (questions.Count != questionIds.Count)
        {
            var missingQuestionId = questionIds.Except(questions.Select(question => question.Id)).First();
            throw new KeyNotFoundException($"Question {missingQuestionId} was not found in subject {input.SubjectId}.");
        }

        var questionCategories = questions.ToDictionary(question => question.Id, question => question.Category);
        foreach (var assignment in input.Participants.SelectMany(participant => participant.Assignments))
        {
            if (questionCategories[assignment.QuestionId] != assignment.Category)
            {
                throw new ArgumentException(
                    $"Question {assignment.QuestionId} does not belong to the requested category.",
                    nameof(assignment));
            }
        }
    }

    private async Task<ExamSessionDetail?> LoadDetailAsync(
        int sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _context.ExamSessions
            .AsNoTracking()
            .Include(existing => existing.Subject)
            .Include(existing => existing.Instructor)
            .Include(existing => existing.SessionStudents)
                .ThenInclude(participant => participant.Student)
            .Include(existing => existing.SessionStudents)
                .ThenInclude(participant => participant.ExamQuestionAssignments)
                .ThenInclude(assignment => assignment.Question)
            .AsSplitQuery()
            .SingleOrDefaultAsync(existing => existing.Id == sessionId, cancellationToken);

        return session is null ? null : MapDetail(session);
    }

    private static ExamSessionDetail MapDetail(ExamSession session)
    {
        var participants = session.SessionStudents
            .OrderBy(participant => participant.Student.Code)
            .ThenBy(participant => participant.Id)
            .Select(participant => new SessionStudentDetail
            {
                Id = participant.Id,
                StudentId = participant.StudentId,
                StudentCode = participant.Student.Code,
                StudentName = participant.Student.Name,
                StartTime = participant.StartTime,
                EndTime = participant.EndTime,
                RequestedCoreCount = participant.RequestedCoreCount,
                MaximumDeepCount = participant.MaximumDeepCount,
                ActualCoreCount = participant.ActualCoreCount,
                ActualDeepCount = participant.ActualDeepCount,
                Status = participant.Status,
                Assignments = participant.ExamQuestionAssignments
                    .OrderBy(assignment => assignment.Position)
                    .ThenBy(assignment => assignment.Order)
                    .ThenBy(assignment => assignment.Id)
                    .Select(assignment => new ExamQuestionAssignmentDetail
                    {
                        Id = assignment.Id,
                        QuestionId = assignment.QuestionId,
                        Category = assignment.Category,
                        Position = assignment.Position,
                        Order = assignment.Order,
                        Question = new QuestionSummary
                        {
                            Id = assignment.Question.Id,
                            Category = assignment.Question.Category,
                            Topic = assignment.Question.Topic,
                            Content = assignment.Question.Content,
                            Difficulty = assignment.Question.Difficulty
                        }
                    })
                    .ToList()
            })
            .ToList();

        return new ExamSessionDetail
        {
            Id = session.Id,
            Name = session.Name,
            Date = session.Date,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            Instructor = new InstructorSummary
            {
                Id = session.Instructor.Id,
                Name = session.Instructor.Name,
                Email = session.Instructor.Email
            },
            Subject = new SubjectSummary
            {
                Id = session.Subject.Id,
                Code = session.Subject.Code,
                Name = session.Subject.Name,
                Description = session.Subject.Description
            },
            Participants = participants
        };
    }

    private static ExamSessionAggregateInput NormalizeInput(ExamSessionAggregateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var instructorId = RepositoryInput.PositiveId(input.InstructorId, nameof(input.InstructorId));
        var subjectId = RepositoryInput.PositiveId(input.SubjectId, nameof(input.SubjectId));
        var name = RepositoryInput.RequiredText(input.Name, nameof(input.Name), 200);
        var date = RepositoryInput.ToUtc(input.Date);
        if (date == default)
        {
            throw new ArgumentException("Date is required.", nameof(input.Date));
        }

        RepositoryInput.EnsureEnum(input.Status, nameof(input.Status));
        if (input.Participants is null || input.Participants.Count == 0)
        {
            throw new ArgumentException("At least one participant is required.", nameof(input.Participants));
        }

        var participants = new List<ExamSessionParticipantInput>();
        var studentIds = new HashSet<int>();

        foreach (var participant in input.Participants ?? Array.Empty<ExamSessionParticipantInput>())
        {
            if (participant is null)
            {
                throw new ArgumentException("Participants cannot contain null entries.", nameof(input.Participants));
            }

            var studentId = RepositoryInput.PositiveId(participant.StudentId, nameof(participant.StudentId));
            if (!studentIds.Add(studentId))
            {
                throw new ArgumentException($"Student {studentId} occurs more than once in the session.", nameof(input.Participants));
            }

            if (!participant.StartTime.HasValue)
            {
                throw new ArgumentException("Participant start time is required.", nameof(participant.StartTime));
            }

            if (!participant.EndTime.HasValue)
            {
                throw new ArgumentException("Participant end time is required.", nameof(participant.EndTime));
            }

            var startTime = RepositoryInput.ToUtc(participant.StartTime.Value);
            var endTime = RepositoryInput.ToUtc(participant.EndTime.Value);
            if (endTime <= startTime)
            {
                throw new ArgumentException("Participant end time must be later than start time.", nameof(participant.EndTime));
            }

            if (endTime - startTime > TimeSpan.FromDays(1))
            {
                throw new ArgumentException("Participant duration cannot exceed 1440 minutes.", nameof(participant.EndTime));
            }

            if (TimeZoneInfo.ConvertTimeFromUtc(startTime, TimeZoneInfo.Local).Date != date.Date)
            {
                throw new ArgumentException("Participant start time must be on the exam date.", nameof(participant.StartTime));
            }

            var requestedCoreCount = RepositoryInput.PositiveCount(
                participant.RequestedCoreCount,
                nameof(participant.RequestedCoreCount));
            var maximumDeepCount = RepositoryInput.NonNegative(
                participant.MaximumDeepCount,
                nameof(participant.MaximumDeepCount));
            var actualCoreCount = RepositoryInput.NonNegative(
                participant.ActualCoreCount,
                nameof(participant.ActualCoreCount));
            var actualDeepCount = RepositoryInput.NonNegative(
                participant.ActualDeepCount,
                nameof(participant.ActualDeepCount));
            RepositoryInput.EnsureEnum(participant.Status, nameof(participant.Status));

            if (actualCoreCount != requestedCoreCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(participant.ActualCoreCount),
                    "Actual core count must equal the requested core count.");
            }

            if (actualDeepCount > maximumDeepCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(participant.ActualDeepCount),
                    "Actual deep count cannot exceed the maximum deep count.");
            }

            var assignments = new List<ExamQuestionAssignmentInput>();
            var questionIds = new HashSet<int>();
            var assignmentKeys = new HashSet<(int Position, int Order)>();
            var coreAssignments = 0;
            var deepAssignments = 0;

            foreach (var assignment in participant.Assignments ?? Array.Empty<ExamQuestionAssignmentInput>())
            {
                if (assignment is null)
                {
                    throw new ArgumentException("Assignments cannot contain null entries.", nameof(participant.Assignments));
                }

                var questionId = RepositoryInput.PositiveId(assignment.QuestionId, nameof(assignment.QuestionId));
                if (!questionIds.Add(questionId))
                {
                    throw new ArgumentException(
                        $"Question {questionId} occurs more than once for student {studentId}.",
                        nameof(participant.Assignments));
                }

                RepositoryInput.EnsureEnum(assignment.Category, nameof(assignment.Category));
                var expectedPosition = assignment.Category == QuestionCategory.Core ? 1 : 2;
                if (assignment.Position != expectedPosition)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(assignment.Position),
                        "Position must match the question category.");
                }

                RepositoryInput.PositiveCount(assignment.Order, nameof(assignment.Order));
                if (!assignmentKeys.Add((assignment.Position, assignment.Order)))
                {
                    throw new ArgumentException(
                        $"Position {assignment.Position}, order {assignment.Order} occurs more than once for student {studentId}.",
                        nameof(participant.Assignments));
                }

                if (assignment.Category == QuestionCategory.Core)
                {
                    coreAssignments++;
                }
                else
                {
                    deepAssignments++;
                }

                assignments.Add(new ExamQuestionAssignmentInput
                {
                    QuestionId = questionId,
                    Category = assignment.Category,
                    Position = assignment.Position,
                    Order = assignment.Order
                });
            }

            if (coreAssignments != requestedCoreCount
                || actualCoreCount != coreAssignments)
            {
                throw new ArgumentException(
                    "Core assignments must exactly match the requested and actual core counts.",
                    nameof(participant.Assignments));
            }

            if (deepAssignments > maximumDeepCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(participant.MaximumDeepCount),
                    "The number of deep assignments cannot exceed the maximum deep count.");
            }

            if (actualDeepCount != deepAssignments)
            {
                throw new ArgumentException(
                    "Deep assignments must match the actual deep count.",
                    nameof(participant.Assignments));
            }

            participants.Add(new ExamSessionParticipantInput
            {
                StudentId = studentId,
                StartTime = startTime,
                EndTime = endTime,
                RequestedCoreCount = requestedCoreCount,
                MaximumDeepCount = maximumDeepCount,
                ActualCoreCount = actualCoreCount,
                ActualDeepCount = actualDeepCount,
                Status = participant.Status,
                Assignments = assignments
            });
        }

        var orderedParticipants = participants
            .OrderBy(participant => participant.StartTime)
            .ToList();
        for (var position = 1; position < orderedParticipants.Count; position++)
        {
            if (orderedParticipants[position].StartTime!.Value < orderedParticipants[position - 1].EndTime!.Value)
            {
                throw new ArgumentException("Participant exam windows cannot overlap.", nameof(input.Participants));
            }
        }

        return new ExamSessionAggregateInput
        {
            InstructorId = instructorId,
            SubjectId = subjectId,
            Name = name,
            Date = date,
            Status = input.Status,
            Participants = participants
        };
    }

    private static ExamSessionAggregateInput ToInput(ExamSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var participants = new List<ExamSessionParticipantInput>();
        foreach (var participant in session.SessionStudents ?? new List<SessionStudent>())
        {
            if (participant is null)
            {
                throw new ArgumentException("The session contains a null participant.", nameof(session));
            }

            var assignments = new List<ExamQuestionAssignmentInput>();
            foreach (var assignment in participant.ExamQuestionAssignments ?? new List<ExamQuestionAssignment>())
            {
                if (assignment is null)
                {
                    throw new ArgumentException("A participant contains a null assignment.", nameof(session));
                }

                assignments.Add(new ExamQuestionAssignmentInput
                {
                    QuestionId = ResolveId(assignment.QuestionId, assignment.Question, nameof(assignment.QuestionId)),
                    Category = assignment.Category,
                    Position = assignment.Position,
                    Order = assignment.Order
                });
            }

            participants.Add(new ExamSessionParticipantInput
            {
                StudentId = ResolveId(participant.StudentId, participant.Student, nameof(participant.StudentId)),
                StartTime = participant.StartTime,
                EndTime = participant.EndTime,
                RequestedCoreCount = participant.RequestedCoreCount,
                MaximumDeepCount = participant.MaximumDeepCount,
                ActualCoreCount = participant.ActualCoreCount,
                ActualDeepCount = participant.ActualDeepCount,
                Status = participant.Status,
                Assignments = assignments
            });
        }

        return new ExamSessionAggregateInput
        {
            InstructorId = ResolveId(session.InstructorId, session.Instructor, nameof(session.InstructorId)),
            SubjectId = ResolveId(session.SubjectId, session.Subject, nameof(session.SubjectId)),
            Name = session.Name,
            Date = session.Date,
            Status = session.Status,
            Participants = participants
        };
    }

    private static int ResolveId(int id, IEntity<int>? entity, string fieldName)
    {
        if (id > 0)
        {
            return id;
        }

        if (entity is null || entity.Id <= 0)
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return entity.Id;
    }
}
