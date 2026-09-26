using AssignmentPRN.DataAccess.Contracts;
using AssignmentPRN.DataAccess.Enums;
using AssignmentPRN.DataAccess.Repositories;

namespace AssignmentPRN.Business;

public sealed class ExamSessionService : IExamSessionService
{
    private readonly IExamSessionRepository _examSessionRepository;
    private readonly IQuestionAllocationService _questionAllocationService;
    private readonly IInstructorRepository _instructorRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IStudentRepository _studentRepository;

    public ExamSessionService(
        IExamSessionRepository examSessionRepository,
        IQuestionAllocationService questionAllocationService,
        IInstructorRepository instructorRepository,
        ISubjectRepository subjectRepository,
        IStudentRepository studentRepository)
    {
        _examSessionRepository = examSessionRepository ?? throw new ArgumentNullException(nameof(examSessionRepository));
        _questionAllocationService = questionAllocationService ?? throw new ArgumentNullException(nameof(questionAllocationService));
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _subjectRepository = subjectRepository ?? throw new ArgumentNullException(nameof(subjectRepository));
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
    }

    public Task<ServiceResponse<IReadOnlyList<ExamSessionListItemResponse>>> ListAsync(
        ExamSessionListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<IReadOnlyList<ExamSessionListItemResponse>>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var sessions = await _examSessionRepository.ListAsync(cancellationToken);
                return sessions.Select(MapListItem).ToList();
            },
            "Exam sessions could not be loaded.");
    }

    public Task<ServiceResponse<ExamSessionDetailResponse>> GetAsync(
        ExamSessionGetRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<ExamSessionDetailResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var session = await _examSessionRepository.GetDetailAsync(id, cancellationToken);
                return session is null
                    ? throw new BusinessValidationException($"Exam session {id} was not found.")
                    : MapDetail(session);
            },
            "Exam session could not be loaded.");
    }

    public Task<ServiceResponse<ExamSessionDetailResponse>> CreateAsync(
        ExamSessionCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<ExamSessionDetailResponse>(
            async () =>
            {
                var normalized = NormalizeRequest(request);
                await EnsureReferencesAsync(normalized, cancellationToken);
                var allocationResponse = await _questionAllocationService.AllocateAsync(
                    new QuestionAllocationRequest
                    {
                        SubjectId = normalized.SubjectId,
                        Candidates = normalized.Participants
                            .Select(participant => new QuestionAllocationCandidateRequest
                            {
                                StudentId = participant.StudentId,
                                StartTime = participant.StartTime,
                                EndTime = participant.EndTime,
                                RequestedCoreCount = participant.RequestedCoreCount,
                                MaximumDeepCount = participant.MaximumDeepCount
                            })
                            .ToList()
                    },
                    cancellationToken);
                if (!allocationResponse.Success || allocationResponse.Data is null)
                {
                    var errors = allocationResponse.Errors.Count > 0
                        ? allocationResponse.Errors
                        : new[] { allocationResponse.Error ?? "Questions could not be allocated." };
                    throw new BusinessValidationException(errors);
                }

                var allocationCandidates = allocationResponse.Data.Candidates
                    .Where(candidate => candidate is not null)
                    .ToDictionary(candidate => candidate.StudentId);
                var participants = new List<ExamSessionParticipantInput>();
                foreach (var participant in normalized.Participants)
                {
                    if (!allocationCandidates.TryGetValue(participant.StudentId, out var allocation)
                        || allocation.ActualCoreCount != participant.RequestedCoreCount)
                    {
                        throw new BusinessValidationException(
                            $"Question allocation did not produce the required core questions for student {participant.StudentId}.");
                    }

                    var assignments = MapAllocatedAssignments(participant, allocation);
                    participants.Add(new ExamSessionParticipantInput
                    {
                        StudentId = participant.StudentId,
                        StartTime = participant.StartTime,
                        EndTime = participant.EndTime,
                        RequestedCoreCount = participant.RequestedCoreCount,
                        MaximumDeepCount = participant.MaximumDeepCount,
                        ActualCoreCount = allocation.ActualCoreCount,
                        ActualDeepCount = allocation.ActualDeepCount,
                        Status = SessionStudentStatus.Pending,
                        Assignments = assignments
                    });
                }

                var created = await _examSessionRepository.CreateAsync(
                    new ExamSessionAggregateInput
                    {
                        InstructorId = normalized.InstructorId,
                        SubjectId = normalized.SubjectId,
                        Name = normalized.Name,
                        Date = normalized.Date,
                        Status = ExamSessionStatus.Scheduled,
                        Participants = participants
                    },
                    cancellationToken);
                return MapDetail(created, allocationResponse.Data.Warnings);
            },
            "Exam session could not be created.");
    }

    public Task<ServiceResponse> DeleteAsync(
        ExamSessionDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var session = await _examSessionRepository.GetDetailAsync(id, cancellationToken);
                if (session is null)
                {
                    throw new BusinessValidationException($"Exam session {id} was not found.");
                }

                await _examSessionRepository.DeleteAsync(id, cancellationToken);
            },
            "Exam session could not be deleted.");
    }

    public Task<ServiceResponse<ExamSessionCreationOptionsResponse>> GetCreationOptionsAsync(
        ExamSessionCreationOptionsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<ExamSessionCreationOptionsResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var instructors = await _instructorRepository.ListAsync(cancellationToken);
                var subjects = await _subjectRepository.ListAsync(cancellationToken);
                var students = await _studentRepository.ListAsync(cancellationToken);
                return new ExamSessionCreationOptionsResponse
                {
                    Instructors = instructors.Select(instructor => new InstructorOptionResponse
                    {
                        Id = instructor.Id,
                        Name = instructor.Name,
                        Email = instructor.Email
                    }).ToList(),
                    Subjects = subjects.Select(subject => new SubjectOptionResponse
                    {
                        Id = subject.Id,
                        Code = subject.Code,
                        Name = subject.Name,
                        Description = subject.Description
                    }).ToList(),
                    Students = students.Select(student => new StudentOptionResponse
                    {
                        Id = student.Id,
                        Code = student.Code,
                        Name = student.Name
                    }).ToList()
                };
            },
            "Exam session options could not be loaded.");
    }

    private static List<ExamQuestionAssignmentInput> MapAllocatedAssignments(
        NormalizedParticipant participant,
        QuestionAllocationCandidateResponse allocation)
    {
        var assignments = new List<ExamQuestionAssignmentInput>();
        var questionIds = new HashSet<int>();
        var coreCount = 0;
        var deepCount = 0;
        foreach (var assignment in allocation.Assignments ?? Array.Empty<QuestionAllocationAssignmentResponse>())
        {
            if (assignment is null || assignment.QuestionId <= 0 || assignment.Order <= 0)
            {
                throw new BusinessValidationException(
                    $"Question allocation returned an invalid assignment for student {participant.StudentId}.");
            }

            if (!Enum.IsDefined(typeof(QuestionCategory), assignment.Category)
                || (assignment.Category == QuestionCategory.Core && assignment.Position != 1)
                || (assignment.Category == QuestionCategory.Deep && assignment.Position != 2))
            {
                throw new BusinessValidationException(
                    $"Question allocation returned an invalid category or position for student {participant.StudentId}.");
            }

            if (!questionIds.Add(assignment.QuestionId))
            {
                throw new BusinessValidationException(
                    $"Question allocation repeated a question for student {participant.StudentId}.");
            }

            if (assignment.Category == QuestionCategory.Core)
            {
                coreCount++;
            }
            else
            {
                deepCount++;
            }

            assignments.Add(new ExamQuestionAssignmentInput
            {
                QuestionId = assignment.QuestionId,
                Category = assignment.Category,
                Position = assignment.Position,
                Order = assignment.Order
            });
        }

        if (coreCount != participant.RequestedCoreCount
            || deepCount > participant.MaximumDeepCount
            || allocation.ActualCoreCount != coreCount
            || allocation.ActualDeepCount != deepCount)
        {
            throw new BusinessValidationException(
                $"Question allocation returned invalid counts for student {participant.StudentId}.");
        }

        return assignments;
    }

    private async Task EnsureReferencesAsync(
        NormalizedSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (await _instructorRepository.GetByIdAsync(request.InstructorId, cancellationToken) is null)
        {
            throw new BusinessValidationException($"Instructor {request.InstructorId} was not found.");
        }

        if (await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken) is null)
        {
            throw new BusinessValidationException($"Subject {request.SubjectId} was not found.");
        }

        foreach (var participant in request.Participants)
        {
            if (await _studentRepository.GetByIdAsync(participant.StudentId, cancellationToken) is null)
            {
                throw new BusinessValidationException($"Student {participant.StudentId} was not found.");
            }
        }
    }

    private static NormalizedSessionRequest NormalizeRequest(ExamSessionCreateRequest request)
    {
        if (request is null)
        {
            throw new BusinessValidationException("Request is required.");
        }

        var instructorId = BusinessValidation.PositiveId(request.InstructorId, nameof(request.InstructorId));
        var subjectId = BusinessValidation.PositiveId(request.SubjectId, nameof(request.SubjectId));
        var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
        if (request.Date == default)
        {
            throw new BusinessValidationException("Date is required.");
        }

        var date = BusinessValidation.ToUtcDate(request.Date);
        if (request.Participants is null || request.Participants.Count == 0)
        {
            throw new BusinessValidationException("At least one participant is required.");
        }

        var participants = new List<NormalizedParticipant>();
        var studentIds = new HashSet<int>();
        var index = 0;
        foreach (var participant in request.Participants)
        {
            if (participant is null)
            {
                throw new BusinessValidationException("Participants cannot contain null entries.");
            }

            var studentId = BusinessValidation.PositiveId(participant.StudentId, nameof(participant.StudentId));
            if (!studentIds.Add(studentId))
            {
                throw new BusinessValidationException($"Student {studentId} occurs more than once in the session.");
            }

            if (!participant.StartTime.HasValue)
            {
                throw new BusinessValidationException($"Start time is required for student {studentId}.");
            }

            if (!participant.EndTime.HasValue)
            {
                throw new BusinessValidationException($"End time is required for student {studentId}.");
            }

            var startTime = BusinessValidation.ToUtc(participant.StartTime.Value);
            var endTime = BusinessValidation.ToUtc(participant.EndTime.Value);
            if (endTime <= startTime)
            {
                throw new BusinessValidationException(
                    $"End time must be later than start time for student {studentId}.");
            }

            if (endTime - startTime > TimeSpan.FromDays(1))
            {
                throw new BusinessValidationException(
                    $"Exam duration cannot exceed 1440 minutes for student {studentId}.");
            }

            if (TimeZoneInfo.ConvertTimeFromUtc(startTime, TimeZoneInfo.Local).Date != request.Date.Date)
            {
                throw new BusinessValidationException(
                    $"Start time must be on the exam date for student {studentId}.");
            }

            var requestedCoreCount = BusinessValidation.PositiveCount(
                participant.RequestedCoreCount,
                nameof(participant.RequestedCoreCount));
            var maximumDeepCount = BusinessValidation.NonNegative(
                participant.MaximumDeepCount,
                nameof(participant.MaximumDeepCount));
            participants.Add(new NormalizedParticipant(
                studentId,
                startTime,
                endTime,
                requestedCoreCount,
                maximumDeepCount,
                index));
            index++;
        }

        var sorted = participants
            .OrderBy(participant => participant.StartTime)
            .ThenBy(participant => participant.StudentId)
            .ThenBy(participant => participant.Index)
            .ToList();
        for (var position = 1; position < sorted.Count; position++)
        {
            if (sorted[position].StartTime < sorted[position - 1].EndTime)
            {
                throw new BusinessValidationException(
                    $"Exam windows for students {sorted[position - 1].StudentId} and {sorted[position].StudentId} overlap.");
            }
        }

        return new NormalizedSessionRequest(instructorId, subjectId, name, date, sorted);
    }

    private static ExamSessionListItemResponse MapListItem(ExamSessionListItem item)
    {
        return new ExamSessionListItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Date = BusinessValidation.ToUtcDate(item.Date),
            Status = item.Status,
            CreatedAt = ToUtc(item.CreatedAt),
            InstructorId = item.InstructorId,
            InstructorName = item.InstructorName,
            InstructorEmail = item.InstructorEmail,
            SubjectId = item.SubjectId,
            SubjectCode = item.SubjectCode,
            SubjectName = item.SubjectName,
            ParticipantCount = item.ParticipantCount,
            AssignmentCount = item.AssignmentCount,
            CoreQuestionCount = item.CoreQuestionCount,
            DeepQuestionCount = item.DeepQuestionCount
        };
    }

    private static ExamSessionDetailResponse MapDetail(
        ExamSessionDetail detail,
        IEnumerable<string>? additionalWarnings = null)
    {
        var participants = (detail.Participants ?? Array.Empty<SessionStudentDetail>())
            .Select(MapParticipant)
            .ToList();
        var warnings = new List<string>();
        foreach (var participant in participants)
        {
            if (participant.MaximumDeepCount > participant.ActualDeepCount)
            {
                warnings.Add(
                    $"Student {participant.StudentId} has {participant.ActualDeepCount} of {participant.MaximumDeepCount} optional deep questions assigned.");
            }
        }

        if (additionalWarnings is not null)
        {
            warnings.AddRange(additionalWarnings);
        }

        return new ExamSessionDetailResponse
        {
            Id = detail.Id,
            Name = detail.Name,
            Date = BusinessValidation.ToUtcDate(detail.Date),
            Status = detail.Status,
            CreatedAt = ToUtc(detail.CreatedAt),
            Instructor = new ExamSessionInstructorResponse
            {
                Id = detail.Instructor.Id,
                Name = detail.Instructor.Name,
                Email = detail.Instructor.Email
            },
            Subject = new ExamSessionSubjectResponse
            {
                Id = detail.Subject.Id,
                Code = detail.Subject.Code,
                Name = detail.Subject.Name,
                Description = detail.Subject.Description
            },
            Participants = participants,
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToList()
        };
    }

    private static ExamSessionParticipantResponse MapParticipant(SessionStudentDetail participant)
    {
        return new ExamSessionParticipantResponse
        {
            Id = participant.Id,
            StudentId = participant.StudentId,
            StudentCode = participant.StudentCode,
            StudentName = participant.StudentName,
            StartTime = ToUtc(participant.StartTime),
            EndTime = ToUtc(participant.EndTime),
            RequestedCoreCount = participant.RequestedCoreCount,
            MaximumDeepCount = participant.MaximumDeepCount,
            ActualCoreCount = participant.ActualCoreCount,
            ActualDeepCount = participant.ActualDeepCount,
            Status = participant.Status,
            Assignments = (participant.Assignments ?? Array.Empty<ExamQuestionAssignmentDetail>())
                .OrderBy(assignment => assignment.Position)
                .ThenBy(assignment => assignment.Order)
                .ThenBy(assignment => assignment.Id)
                .Select(MapAssignment)
                .ToList()
        };
    }

    private static ExamQuestionAssignmentResponse MapAssignment(ExamQuestionAssignmentDetail assignment)
    {
        var question = assignment.Question ?? new QuestionSummary();
        return new ExamQuestionAssignmentResponse
        {
            Id = assignment.Id,
            QuestionId = assignment.QuestionId,
            Category = assignment.Category,
            Position = assignment.Position,
            Order = assignment.Order,
            Question = new AssignedQuestionResponse
            {
                Id = question.Id,
                Category = question.Category,
                Topic = question.Topic,
                Content = question.Content,
                Difficulty = question.Difficulty
            }
        };
    }

    private static DateTime ToUtc(DateTime value)
    {
        return BusinessValidation.ToUtc(value);
    }

    private static DateTime? ToUtc(DateTime? value)
    {
        return BusinessValidation.ToUtc(value);
    }

    private sealed record NormalizedParticipant(
        int StudentId,
        DateTime StartTime,
        DateTime EndTime,
        int RequestedCoreCount,
        int MaximumDeepCount,
        int Index);

    private sealed record NormalizedSessionRequest(
        int InstructorId,
        int SubjectId,
        string Name,
        DateTime Date,
        IReadOnlyList<NormalizedParticipant> Participants);
}
