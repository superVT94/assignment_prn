using AssignmentPRN.DataAccess.Enums;
using System.Security.Cryptography;

namespace AssignmentPRN.Business;

public sealed class ServiceResponse
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public string? Message => Error;

    public static ServiceResponse Ok()
    {
        return new ServiceResponse { Success = true };
    }

    public static ServiceResponse Fail(string error)
    {
        var value = string.IsNullOrWhiteSpace(error)
            ? "The operation could not be completed."
            : error;
        return new ServiceResponse
        {
            Success = false,
            Error = value,
            Errors = new[] { value }
        };
    }

    public static ServiceResponse Fail(IEnumerable<string> errors)
    {
        var values = errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray();
        var value = values.Length == 0 ? "The operation could not be completed." : values[0];
        return new ServiceResponse
        {
            Success = false,
            Error = value,
            Errors = new[] { value }.Concat(values.Skip(1)).ToArray()
        };
    }
}

public sealed class ServiceResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Error { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public string? Message => Error;

    public static ServiceResponse<T> Ok(T data)
    {
        return new ServiceResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    public static ServiceResponse<T> Fail(string error)
    {
        var value = string.IsNullOrWhiteSpace(error)
            ? "The operation could not be completed."
            : error;
        return new ServiceResponse<T>
        {
            Success = false,
            Error = value,
            Errors = new[] { value }
        };
    }

    public static ServiceResponse<T> Fail(IEnumerable<string> errors)
    {
        var values = errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray();
        var value = values.Length == 0 ? "The operation could not be completed." : values[0];
        return new ServiceResponse<T>
        {
            Success = false,
            Error = value,
            Errors = new[] { value }.Concat(values.Skip(1)).ToArray()
        };
    }
}

public sealed class BusinessValidationException : Exception
{
    public BusinessValidationException(string error)
        : base(error)
    {
        Errors = new[] { error };
    }

    public BusinessValidationException(IEnumerable<string> errors)
        : base(errors.FirstOrDefault() ?? "The supplied values are invalid.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}

public sealed class InstructorListRequest
{
}

public sealed class InstructorGetRequest
{
    public int Id { get; init; }
}

public sealed class InstructorCreateRequest
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class InstructorUpdateRequest
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class InstructorDeleteRequest
{
    public int Id { get; init; }
}

public sealed class InstructorResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class SubjectListRequest
{
}

public sealed class SubjectGetRequest
{
    public int Id { get; init; }
}

public sealed class SubjectCreateRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class SubjectUpdateRequest
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class SubjectDeleteRequest
{
    public int Id { get; init; }
}

public sealed class SubjectResponse
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class StudentListRequest
{
}

public sealed class StudentGetRequest
{
    public int Id { get; init; }
}

public sealed class StudentCreateRequest
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class StudentUpdateRequest
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class StudentDeleteRequest
{
    public int Id { get; init; }
}

public sealed class StudentResponse
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class QuestionListRequest
{
    public int SubjectId { get; init; }
}

public sealed class QuestionGetRequest
{
    public int Id { get; init; }
}

public sealed class QuestionCreateRequest
{
    public int SubjectId { get; init; }

    public QuestionCategory Category { get; init; }

    public string Topic { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int Difficulty { get; init; }
}

public sealed class QuestionUpdateRequest
{
    public int Id { get; init; }

    public int SubjectId { get; init; }

    public QuestionCategory Category { get; init; }

    public string Topic { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int Difficulty { get; init; }
}

public sealed class QuestionDeleteRequest
{
    public int Id { get; init; }
}

public sealed class QuestionResponse
{
    public int Id { get; init; }

    public int SubjectId { get; init; }

    public QuestionCategory Category { get; init; }

    public string CategoryName => Category.ToString();

    public bool IsDeep => Category == QuestionCategory.Deep;

    public string Topic { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int Difficulty { get; init; }
}

public sealed class ExamSessionListRequest
{
}

public sealed class ExamSessionGetRequest
{
    public int Id { get; init; }
}

public sealed class ExamSessionDeleteRequest
{
    public int Id { get; init; }
}

public sealed class ExamSessionCreationOptionsRequest
{
}

public sealed class ExamSessionCreateRequest
{
    public int InstructorId { get; init; }

    public int SubjectId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public IReadOnlyCollection<ExamSessionParticipantRequest>? Participants { get; init; }
}

public sealed class ExamSessionParticipantRequest
{
    public int StudentId { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int RequestedCoreCount { get; init; }

    public int MaximumDeepCount { get; init; }
}

public sealed class ExamSessionListItemResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public ExamSessionStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public int InstructorId { get; init; }

    public string InstructorName { get; init; } = string.Empty;

    public string InstructorEmail { get; init; } = string.Empty;

    public int SubjectId { get; init; }

    public string SubjectCode { get; init; } = string.Empty;

    public string SubjectName { get; init; } = string.Empty;

    public int ParticipantCount { get; init; }

    public int AssignmentCount { get; init; }

    public int CoreQuestionCount { get; init; }

    public int DeepQuestionCount { get; init; }
}

public sealed class ExamSessionDetailResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public ExamSessionStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public ExamSessionInstructorResponse Instructor { get; init; } = new();

    public ExamSessionSubjectResponse Subject { get; init; } = new();

    public IReadOnlyList<ExamSessionParticipantResponse> Participants { get; init; } =
        Array.Empty<ExamSessionParticipantResponse>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public bool HasWarnings => Warnings.Count > 0;
}

public sealed class ExamSessionInstructorResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class ExamSessionSubjectResponse
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class ExamSessionParticipantResponse
{
    public int Id { get; init; }

    public int StudentId { get; init; }

    public string StudentCode { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int RequestedCoreCount { get; init; }

    public int MaximumDeepCount { get; init; }

    public int ActualCoreCount { get; init; }

    public int ActualDeepCount { get; init; }

    public SessionStudentStatus Status { get; init; }

    public IReadOnlyList<ExamQuestionAssignmentResponse> Assignments { get; init; } =
        Array.Empty<ExamQuestionAssignmentResponse>();
}

public sealed class ExamQuestionAssignmentResponse
{
    public int Id { get; init; }

    public int QuestionId { get; init; }

    public QuestionCategory Category { get; init; }

    public int Position { get; init; }

    public int Order { get; init; }

    public AssignedQuestionResponse Question { get; init; } = new();
}

public sealed class AssignedQuestionResponse
{
    public int Id { get; init; }

    public QuestionCategory Category { get; init; }

    public string Topic { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int Difficulty { get; init; }
}

public sealed class InstructorOptionResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class SubjectOptionResponse
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}

public sealed class StudentOptionResponse
{
    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class ExamSessionCreationOptionsResponse
{
    public IReadOnlyList<InstructorOptionResponse> Instructors { get; init; } =
        Array.Empty<InstructorOptionResponse>();

    public IReadOnlyList<SubjectOptionResponse> Subjects { get; init; } =
        Array.Empty<SubjectOptionResponse>();

    public IReadOnlyList<StudentOptionResponse> Students { get; init; } =
        Array.Empty<StudentOptionResponse>();
}

public sealed class QuestionAllocationRequest
{
    public int SubjectId { get; init; }

    public IReadOnlyCollection<QuestionAllocationCandidateRequest>? Candidates { get; init; }
}

public sealed class QuestionAllocationCandidateRequest
{
    public int StudentId { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int RequestedCoreCount { get; init; }

    public int MaximumDeepCount { get; init; }
}

public sealed class QuestionAllocationResponse
{
    public int SubjectId { get; init; }

    public IReadOnlyList<QuestionAllocationCandidateResponse> Candidates { get; init; } =
        Array.Empty<QuestionAllocationCandidateResponse>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class QuestionAllocationCandidateResponse
{
    public int StudentId { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public int RequestedCoreCount { get; init; }

    public int MaximumDeepCount { get; init; }

    public int ActualCoreCount { get; init; }

    public int ActualDeepCount { get; init; }

    public IReadOnlyList<QuestionAllocationAssignmentResponse> Assignments { get; init; } =
        Array.Empty<QuestionAllocationAssignmentResponse>();
}

public sealed class QuestionAllocationAssignmentResponse
{
    public int QuestionId { get; init; }

    public QuestionCategory Category { get; init; }

    public int Position { get; init; }

    public int Order { get; init; }
}

public interface IQuestionRandomizer
{
    int Next(int maxExclusive);
}

public sealed class SecureQuestionRandomizer : IQuestionRandomizer
{
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return RandomNumberGenerator.GetInt32(maxExclusive);
    }
}
