namespace AssignmentPRN.Business;

public interface IInstructorService
{
    Task<ServiceResponse<IReadOnlyList<InstructorResponse>>> ListAsync(
        InstructorListRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<InstructorResponse>> GetAsync(
        InstructorGetRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<InstructorResponse>> CreateAsync(
        InstructorCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<InstructorResponse>> UpdateAsync(
        InstructorUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        InstructorDeleteRequest request,
        CancellationToken cancellationToken = default);
}

public interface ISubjectService
{
    Task<ServiceResponse<IReadOnlyList<SubjectResponse>>> ListAsync(
        SubjectListRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<SubjectResponse>> GetAsync(
        SubjectGetRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<SubjectResponse>> CreateAsync(
        SubjectCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<SubjectResponse>> UpdateAsync(
        SubjectUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        SubjectDeleteRequest request,
        CancellationToken cancellationToken = default);
}

public interface IStudentService
{
    Task<ServiceResponse<IReadOnlyList<StudentResponse>>> ListAsync(
        StudentListRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StudentResponse>> GetAsync(
        StudentGetRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StudentResponse>> CreateAsync(
        StudentCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<StudentResponse>> UpdateAsync(
        StudentUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        StudentDeleteRequest request,
        CancellationToken cancellationToken = default);
}

public interface IQuestionService
{
    Task<ServiceResponse<IReadOnlyList<QuestionResponse>>> ListAsync(
        QuestionListRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<QuestionResponse>> GetAsync(
        QuestionGetRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<QuestionResponse>> CreateAsync(
        QuestionCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<QuestionResponse>> UpdateAsync(
        QuestionUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        QuestionDeleteRequest request,
        CancellationToken cancellationToken = default);
}

public interface IQuestionAllocationService
{
    Task<ServiceResponse<QuestionAllocationResponse>> AllocateAsync(
        QuestionAllocationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IExamSessionService
{
    Task<ServiceResponse<IReadOnlyList<ExamSessionListItemResponse>>> ListAsync(
        ExamSessionListRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<ExamSessionDetailResponse>> GetAsync(
        ExamSessionGetRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<ExamSessionDetailResponse>> CreateAsync(
        ExamSessionCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeleteAsync(
        ExamSessionDeleteRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<ExamSessionCreationOptionsResponse>> GetCreationOptionsAsync(
        ExamSessionCreationOptionsRequest request,
        CancellationToken cancellationToken = default);
}
