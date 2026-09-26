using AssignmentPRN.Business;
using AssignmentPRN.Presentation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AssignmentPRN.Presentation.Controllers;

public sealed class QuestionsController : Controller
{
    private readonly IQuestionService _questionService;
    private readonly ISubjectService _subjectService;

    public QuestionsController(
        IQuestionService questionService,
        ISubjectService subjectService)
    {
        _questionService = questionService;
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? subjectId,
        CancellationToken cancellationToken)
    {
        var subjectResponse = await _subjectService.ListAsync(
            new SubjectListRequest(),
            cancellationToken);
        if (!subjectResponse.Success)
        {
            var error = GetError(subjectResponse, "Không thể tải danh sách môn học.");
            TempData["Error"] = error;
            return View(new QuestionListViewModel { LoadError = error });
        }

        var subjects = subjectResponse.Data ?? Array.Empty<SubjectResponse>();
        var subjectOptions = subjects
            .OrderBy(subject => subject.Code)
            .Select(subject => new SelectListItem
            {
                Value = subject.Id.ToString(),
                Text = $"{subject.Code} — {subject.Name}",
                Selected = subject.Id == subjectId
            })
            .ToList();
        if (subjects.Count == 0)
        {
            return View(new QuestionListViewModel
            {
                SubjectOptions = subjectOptions,
                HasSubjects = false
            });
        }

        var selectedId = subjectId ?? subjects.OrderBy(subject => subject.Code).First().Id;
        var selectedSubject = subjects.FirstOrDefault(subject => subject.Id == selectedId);
        if (selectedSubject is null)
        {
            return NotFound();
        }

        var response = await _questionService.ListAsync(new QuestionListRequest
        {
            SubjectId = selectedSubject.Id
        }, cancellationToken);
        if (!response.Success)
        {
            var error = GetError(response, "Không thể tải ngân hàng câu hỏi.");
            TempData["Error"] = error;
            return View(new QuestionListViewModel
            {
                SubjectId = selectedSubject.Id,
                Subject = selectedSubject,
                SubjectOptions = subjectOptions,
                HasSubjects = true,
                LoadError = error
            });
        }

        var questions = response.Data ?? Array.Empty<QuestionResponse>();
        return View(new QuestionListViewModel
        {
            SubjectId = selectedSubject.Id,
            Subject = selectedSubject,
            SubjectOptions = subjectOptions,
            HasSubjects = true,
            Questions = questions
                .OrderBy(question => question.Category.ToString())
                .ThenBy(question => question.Difficulty)
                .ThenBy(question => question.Topic)
                .Select(question => new QuestionRowViewModel
                {
                    Id = question.Id,
                    Category = question.CategoryName,
                    IsDeep = question.IsDeep,
                    Topic = question.Topic,
                    Content = question.Content,
                    Difficulty = question.Difficulty
                })
                .ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int? subjectId,
        CancellationToken cancellationToken)
    {
        var model = new QuestionFormViewModel
        {
            SubjectId = subjectId.GetValueOrDefault()
        };
        var loaded = await PopulateSubjectsAsync(model, cancellationToken);
        if (!loaded)
        {
            TempData["Error"] = "Không thể tải danh sách môn học.";
        }
        else if (!model.SubjectOptions.Any(option =>
            int.TryParse(option.Value, out var id) && id > 0))
        {
            TempData["Error"] = "Chưa có môn học. Hãy tạo môn học trước khi thêm câu hỏi.";
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        QuestionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin câu hỏi.";
            await PopulateSubjectsAsync(model, cancellationToken);
            return View(model);
        }

        var response = await _questionService.CreateAsync(
            model.ToCreateRequest(),
            cancellationToken);
        if (!response.Success || response.Data is null)
        {
            AddErrors(response.Errors, response.Error);
            TempData["Error"] = GetError(response, "Không thể tạo câu hỏi.");
            await PopulateSubjectsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Đã thêm câu hỏi vào ngân hàng.";
        return RedirectToAction(nameof(Index), new { subjectId = response.Data.SubjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var response = await _questionService.GetAsync(
            new QuestionGetRequest { Id = id },
            cancellationToken);
        if (!response.Success || response.Data is null)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(response, "Không thể tải câu hỏi.");
            return RedirectToAction(nameof(Index));
        }

        var model = new QuestionFormViewModel
        {
            Id = response.Data.Id,
            SubjectId = response.Data.SubjectId,
            Category = response.Data.IsDeep
                ? QuestionCategoryChoice.Deep
                : QuestionCategoryChoice.Core,
            Topic = response.Data.Topic,
            Content = response.Data.Content,
            Difficulty = response.Data.Difficulty
        };
        await PopulateSubjectsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        QuestionFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin câu hỏi.";
            await PopulateSubjectsAsync(model, cancellationToken);
            return View(model);
        }

        var response = await _questionService.UpdateAsync(
            model.ToUpdateRequest(),
            cancellationToken);
        if (!response.Success)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            AddErrors(response.Errors, response.Error);
            TempData["Error"] = GetError(response, "Không thể cập nhật câu hỏi.");
            await PopulateSubjectsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật câu hỏi.";
        return RedirectToAction(nameof(Index), new { subjectId = model.SubjectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var getResponse = await _questionService.GetAsync(
            new QuestionGetRequest { Id = id },
            cancellationToken);
        if (!getResponse.Success || getResponse.Data is null)
        {
            if (IsNotFound(getResponse))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(getResponse, "Không thể tải câu hỏi.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _questionService.DeleteAsync(
            new QuestionDeleteRequest { Id = id },
            cancellationToken);
        if (!response.Success)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(response, "Không thể xóa câu hỏi.");
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa câu hỏi.";
        return RedirectToAction(nameof(Index), new { subjectId = getResponse.Data.SubjectId });
    }

    private async Task<bool> PopulateSubjectsAsync(
        QuestionFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.CategoryOptions = new List<SelectListItem>
        {
            new() { Value = ((int)QuestionCategoryChoice.Core).ToString(), Text = "Core — câu cốt lõi" },
            new() { Value = ((int)QuestionCategoryChoice.Deep).ToString(), Text = "Deep — câu mở rộng" }
        };
        model.DifficultyOptions = Enumerable.Range(1, 5)
            .Select(level => new SelectListItem
            {
                Value = level.ToString(),
                Text = $"{level} / 5"
            })
            .ToList();

        var response = await _subjectService.ListAsync(
            new SubjectListRequest(),
            cancellationToken);
        if (!response.Success)
        {
            model.SubjectOptions = Array.Empty<SelectListItem>();
            ModelState.AddModelError(
                string.Empty,
                GetError(response, "Không thể tải danh sách môn học."));
            return false;
        }

        model.SubjectOptions = (response.Data ?? Array.Empty<SubjectResponse>())
            .OrderBy(subject => subject.Code)
            .Select(subject => new SelectListItem
            {
                Value = subject.Id.ToString(),
                Text = $"{subject.Code} — {subject.Name}"
            })
            .Prepend(new SelectListItem { Value = string.Empty, Text = "Chọn môn học" })
            .ToList();
        return true;
    }

    private void AddErrors(IReadOnlyList<string> errors, string? error)
    {
        if (errors.Count > 0)
        {
            foreach (var message in errors)
            {
                ModelState.AddModelError(string.Empty, message);
            }
        }
        else if (!string.IsNullOrWhiteSpace(error))
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private static string GetError<T>(ServiceResponse<T> response, string fallback)
    {
        return string.IsNullOrWhiteSpace(response.Error) ? fallback : response.Error;
    }

    private static string GetError(ServiceResponse response, string fallback)
    {
        return string.IsNullOrWhiteSpace(response.Error) ? fallback : response.Error;
    }

    private static bool IsNotFound<T>(ServiceResponse<T> response)
    {
        return response.Errors.Any(error =>
            error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNotFound(ServiceResponse response)
    {
        return response.Errors.Any(error =>
            error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }
}
