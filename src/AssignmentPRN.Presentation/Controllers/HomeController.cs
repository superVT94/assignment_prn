using AssignmentPRN.Business;
using AssignmentPRN.Presentation.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentPRN.Presentation.Controllers;

public sealed class HomeController : Controller
{
    private readonly IInstructorService _instructorService;
    private readonly ISubjectService _subjectService;
    private readonly IStudentService _studentService;
    private readonly IQuestionService _questionService;
    private readonly IExamSessionService _examSessionService;

    public HomeController(
        IInstructorService instructorService,
        ISubjectService subjectService,
        IStudentService studentService,
        IQuestionService questionService,
        IExamSessionService examSessionService)
    {
        _instructorService = instructorService;
        _subjectService = subjectService;
        _studentService = studentService;
        _questionService = questionService;
        _examSessionService = examSessionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var messages = new List<string>();
        var instructorResponse = await _instructorService.ListAsync(
            new InstructorListRequest(),
            cancellationToken);
        var subjectResponse = await _subjectService.ListAsync(
            new SubjectListRequest(),
            cancellationToken);
        var studentResponse = await _studentService.ListAsync(
            new StudentListRequest(),
            cancellationToken);
        var sessionResponse = await _examSessionService.ListAsync(
            new ExamSessionListRequest(),
            cancellationToken);

        var instructorCount = GetCount(
            instructorResponse,
            messages,
            "Không thể tải số giảng viên.");
        var subjectCount = GetCount(
            subjectResponse,
            messages,
            "Không thể tải số môn học.");
        var studentCount = GetCount(
            studentResponse,
            messages,
            "Không thể tải số sinh viên.");
        var sessionCount = GetCount(
            sessionResponse,
            messages,
            "Không thể tải số phiên thi.");

        int? participantCount = null;
        if (sessionResponse.Success && sessionResponse.Data is not null)
        {
            participantCount = sessionResponse.Data.Sum(session => session.ParticipantCount);
        }
        else
        {
            AddError(messages, sessionResponse.Error, "Không thể tải số lượt thi.");
        }

        int? questionCount = null;
        if (subjectResponse.Success && subjectResponse.Data is not null)
        {
            var allQuestionsLoaded = true;
            var count = 0;
            foreach (var subject in subjectResponse.Data)
            {
                var response = await _questionService.ListAsync(
                    new QuestionListRequest { SubjectId = subject.Id },
                    cancellationToken);
                if (response.Success && response.Data is not null)
                {
                    count += response.Data.Count;
                }
                else
                {
                    allQuestionsLoaded = false;
                    AddError(messages, response.Error, "Không thể tải số câu hỏi.");
                    break;
                }
            }

            if (allQuestionsLoaded)
            {
                questionCount = count;
            }
        }
        else
        {
            AddError(messages, subjectResponse.Error, "Không thể tải số câu hỏi.");
        }

        return View(new HomeDashboardViewModel
        {
            InstructorCount = instructorCount,
            SubjectCount = subjectCount,
            StudentCount = studentCount,
            QuestionCount = questionCount,
            SessionCount = sessionCount,
            ParticipantCount = participantCount,
            ServiceMessage = messages.Count == 0
                ? null
                : string.Join(" ", messages.Distinct(StringComparer.OrdinalIgnoreCase))
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private static int? GetCount<T>(
        ServiceResponse<IReadOnlyList<T>> response,
        ICollection<string> messages,
        string fallback)
    {
        if (!response.Success || response.Data is null)
        {
            AddError(messages, response.Error, fallback);
            return null;
        }

        return response.Data.Count;
    }

    private static void AddError(
        ICollection<string> messages,
        string? error,
        string fallback)
    {
        messages.Add(string.IsNullOrWhiteSpace(error) ? fallback : error);
    }
}
