using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Repositories;

namespace AssignmentPRN.Business;

public sealed class InstructorService : IInstructorService
{
    private readonly IInstructorRepository _repository;

    public InstructorService(IInstructorRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<ServiceResponse<IReadOnlyList<InstructorResponse>>> ListAsync(
        InstructorListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<IReadOnlyList<InstructorResponse>>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var instructors = await _repository.ListAsync(cancellationToken);
                return instructors.Select(Map).ToList();
            },
            "Instructors could not be loaded.");
    }

    public Task<ServiceResponse<InstructorResponse>> GetAsync(
        InstructorGetRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<InstructorResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var instructor = await _repository.GetByIdAsync(id, cancellationToken);
                return instructor is null
                    ? throw new KeyNotFoundException()
                    : Map(instructor);
            },
            "Instructor could not be loaded.");
    }

    public Task<ServiceResponse<InstructorResponse>> CreateAsync(
        InstructorCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<InstructorResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
                var email = BusinessValidation.NormalizeEmail(request.Email);
                if (await _repository.ExistsByEmailAsync(email, cancellationToken))
                {
                    throw new BusinessValidationException("An instructor with this email already exists.");
                }

                var created = await _repository.CreateAsync(
                    new Instructor { Name = name, Email = email },
                    cancellationToken);
                return Map(created);
            },
            "Instructor could not be created.");
    }

    public Task<ServiceResponse<InstructorResponse>> UpdateAsync(
        InstructorUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<InstructorResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
                var email = BusinessValidation.NormalizeEmail(request.Email);
                if (await _repository.ExistsByEmailAsync(email, id, cancellationToken))
                {
                    throw new BusinessValidationException("An instructor with this email already exists.");
                }

                var updated = new Instructor { Id = id, Name = name, Email = email };
                await _repository.UpdateAsync(updated, cancellationToken);
                return Map(updated);
            },
            "Instructor could not be updated.");
    }

    public Task<ServiceResponse> DeleteAsync(
        InstructorDeleteRequest request,
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
                var instructor = await _repository.GetByIdAsync(id, cancellationToken);
                if (instructor is null)
                {
                    throw new KeyNotFoundException();
                }

                await _repository.DeleteAsync(id, cancellationToken);
            },
            "Instructor could not be deleted.");
    }

    private static InstructorResponse Map(Instructor instructor)
    {
        return new InstructorResponse
        {
            Id = instructor.Id,
            Name = instructor.Name,
            Email = instructor.Email
        };
    }
}

public sealed class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _repository;

    public SubjectService(ISubjectRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<ServiceResponse<IReadOnlyList<SubjectResponse>>> ListAsync(
        SubjectListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<IReadOnlyList<SubjectResponse>>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var subjects = await _repository.ListAsync(cancellationToken);
                return subjects.Select(Map).ToList();
            },
            "Subjects could not be loaded.");
    }

    public Task<ServiceResponse<SubjectResponse>> GetAsync(
        SubjectGetRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<SubjectResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var subject = await _repository.GetByIdAsync(id, cancellationToken);
                return subject is null
                    ? throw new KeyNotFoundException()
                    : Map(subject);
            },
            "Subject could not be loaded.");
    }

    public Task<ServiceResponse<SubjectResponse>> CreateAsync(
        SubjectCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<SubjectResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var code = BusinessValidation.NormalizeCode(request.Code, nameof(request.Code), 20);
                var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
                var description = BusinessValidation.OptionalText(
                    request.Description,
                    nameof(request.Description),
                    1000);
                if (await _repository.ExistsByCodeAsync(code, cancellationToken))
                {
                    throw new BusinessValidationException("A subject with this code already exists.");
                }

                var created = await _repository.CreateAsync(
                    new Subject { Code = code, Name = name, Description = description },
                    cancellationToken);
                return Map(created);
            },
            "Subject could not be created.");
    }

    public Task<ServiceResponse<SubjectResponse>> UpdateAsync(
        SubjectUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<SubjectResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var code = BusinessValidation.NormalizeCode(request.Code, nameof(request.Code), 20);
                var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
                var description = BusinessValidation.OptionalText(
                    request.Description,
                    nameof(request.Description),
                    1000);
                if (await _repository.ExistsByCodeAsync(code, id, cancellationToken))
                {
                    throw new BusinessValidationException("A subject with this code already exists.");
                }

                var updated = new Subject { Id = id, Code = code, Name = name, Description = description };
                await _repository.UpdateAsync(updated, cancellationToken);
                return Map(updated);
            },
            "Subject could not be updated.");
    }

    public Task<ServiceResponse> DeleteAsync(
        SubjectDeleteRequest request,
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
                var subject = await _repository.GetByIdAsync(id, cancellationToken);
                if (subject is null)
                {
                    throw new KeyNotFoundException();
                }

                await _repository.DeleteAsync(id, cancellationToken);
            },
            "Subject could not be deleted.");
    }

    private static SubjectResponse Map(Subject subject)
    {
        return new SubjectResponse
        {
            Id = subject.Id,
            Code = subject.Code,
            Name = subject.Name,
            Description = subject.Description
        };
    }
}

public sealed class StudentService : IStudentService
{
    private readonly IStudentRepository _repository;

    public StudentService(IStudentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<ServiceResponse<IReadOnlyList<StudentResponse>>> ListAsync(
        StudentListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<IReadOnlyList<StudentResponse>>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var students = await _repository.ListAsync(cancellationToken);
                return students.Select(Map).ToList();
            },
            "Students could not be loaded.");
    }

    public Task<ServiceResponse<StudentResponse>> GetAsync(
        StudentGetRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<StudentResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var student = await _repository.GetByIdAsync(id, cancellationToken);
                return student is null
                    ? throw new KeyNotFoundException()
                    : Map(student);
            },
            "Student could not be loaded.");
    }

    public Task<ServiceResponse<StudentResponse>> CreateAsync(
        StudentCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<StudentResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var code = BusinessValidation.NormalizeCode(request.Code, nameof(request.Code), 30);
                var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
                if (await _repository.ExistsByCodeAsync(code, cancellationToken))
                {
                    throw new BusinessValidationException("A student with this code already exists.");
                }

                var created = await _repository.CreateAsync(
                    new Student { Code = code, Name = name },
                    cancellationToken);
                return Map(created);
            },
            "Student could not be created.");
    }

    public Task<ServiceResponse<StudentResponse>> UpdateAsync(
        StudentUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<StudentResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var code = BusinessValidation.NormalizeCode(request.Code, nameof(request.Code), 30);
                var name = BusinessValidation.RequiredText(request.Name, nameof(request.Name), 200);
                if (await _repository.ExistsByCodeAsync(code, id, cancellationToken))
                {
                    throw new BusinessValidationException("A student with this code already exists.");
                }

                var updated = new Student { Id = id, Code = code, Name = name };
                await _repository.UpdateAsync(updated, cancellationToken);
                return Map(updated);
            },
            "Student could not be updated.");
    }

    public Task<ServiceResponse> DeleteAsync(
        StudentDeleteRequest request,
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
                var student = await _repository.GetByIdAsync(id, cancellationToken);
                if (student is null)
                {
                    throw new KeyNotFoundException();
                }

                await _repository.DeleteAsync(id, cancellationToken);
            },
            "Student could not be deleted.");
    }

    private static StudentResponse Map(Student student)
    {
        return new StudentResponse
        {
            Id = student.Id,
            Code = student.Code,
            Name = student.Name
        };
    }
}

public sealed class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly ISubjectRepository _subjectRepository;

    public QuestionService(
        IQuestionRepository questionRepository,
        ISubjectRepository subjectRepository)
    {
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _subjectRepository = subjectRepository ?? throw new ArgumentNullException(nameof(subjectRepository));
    }

    public Task<ServiceResponse<IReadOnlyList<QuestionResponse>>> ListAsync(
        QuestionListRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<IReadOnlyList<QuestionResponse>>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var subjectId = BusinessValidation.PositiveId(request.SubjectId, nameof(request.SubjectId));
                await EnsureSubjectExistsAsync(subjectId, cancellationToken);
                var questions = await _questionRepository.ListBySubjectAsync(subjectId, cancellationToken);
                return questions.Select(Map).ToList();
            },
            "Questions could not be loaded.");
    }

    public Task<ServiceResponse<QuestionResponse>> GetAsync(
        QuestionGetRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<QuestionResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
                return question is null
                    ? throw new KeyNotFoundException()
                    : Map(question);
            },
            "Question could not be loaded.");
    }

    public Task<ServiceResponse<QuestionResponse>> CreateAsync(
        QuestionCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<QuestionResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var subjectId = BusinessValidation.PositiveId(request.SubjectId, nameof(request.SubjectId));
                var category = request.Category;
                BusinessValidation.EnsureEnum(category, nameof(request.Category));
                var topic = BusinessValidation.RequiredText(request.Topic, nameof(request.Topic), 200);
                var content = BusinessValidation.RequiredText(request.Content, nameof(request.Content), 4000);
                EnsureDifficulty(request.Difficulty);
                await EnsureSubjectExistsAsync(subjectId, cancellationToken);

                var created = await _questionRepository.CreateAsync(
                    new Question
                    {
                        SubjectId = subjectId,
                        Category = category,
                        Topic = topic,
                        Content = content,
                        Difficulty = request.Difficulty
                    },
                    cancellationToken);
                return Map(created);
            },
            "Question could not be created.");
    }

    public Task<ServiceResponse<QuestionResponse>> UpdateAsync(
        QuestionUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<QuestionResponse>(
            async () =>
            {
                if (request is null)
                {
                    throw new BusinessValidationException("Request is required.");
                }

                var id = BusinessValidation.PositiveId(request.Id, nameof(request.Id));
                var subjectId = BusinessValidation.PositiveId(request.SubjectId, nameof(request.SubjectId));
                var category = request.Category;
                BusinessValidation.EnsureEnum(category, nameof(request.Category));
                var topic = BusinessValidation.RequiredText(request.Topic, nameof(request.Topic), 200);
                var content = BusinessValidation.RequiredText(request.Content, nameof(request.Content), 4000);
                EnsureDifficulty(request.Difficulty);
                await EnsureSubjectExistsAsync(subjectId, cancellationToken);

                var updated = new Question
                {
                    Id = id,
                    SubjectId = subjectId,
                    Category = category,
                    Topic = topic,
                    Content = content,
                    Difficulty = request.Difficulty
                };
                await _questionRepository.UpdateAsync(updated, cancellationToken);
                return Map(updated);
            },
            "Question could not be updated.");
    }

    public Task<ServiceResponse> DeleteAsync(
        QuestionDeleteRequest request,
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
                var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
                if (question is null)
                {
                    throw new KeyNotFoundException();
                }

                await _questionRepository.DeleteAsync(id, cancellationToken);
            },
            "Question could not be deleted.");
    }

    private async Task EnsureSubjectExistsAsync(int subjectId, CancellationToken cancellationToken)
    {
        if (await _subjectRepository.GetByIdAsync(subjectId, cancellationToken) is null)
        {
            throw new BusinessValidationException($"Subject {subjectId} was not found.");
        }
    }

    private static void EnsureDifficulty(int difficulty)
    {
        if (difficulty is < 1 or > 5)
        {
            throw new BusinessValidationException("Difficulty must be between 1 and 5.");
        }
    }

    private static QuestionResponse Map(Question question)
    {
        return new QuestionResponse
        {
            Id = question.Id,
            SubjectId = question.SubjectId,
            Category = question.Category,
            Topic = question.Topic,
            Content = question.Content,
            Difficulty = question.Difficulty
        };
    }
}
