using AssignmentPRN.Business;
using AssignmentPRN.Presentation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AssignmentPRN.Presentation.Controllers;

public sealed class ExamSessionsController : Controller
{
    private readonly IExamSessionService _service;

    public ExamSessionsController(IExamSessionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var response = await _service.ListAsync(
            new ExamSessionListRequest(),
            cancellationToken);
        if (!response.Success)
        {
            var error = GetError(response, "Không thể tải danh sách phiên thi.");
            TempData["Error"] = error;
            return View(new ExamSessionListViewModel { LoadError = error });
        }

        var sessions = response.Data ?? Array.Empty<ExamSessionListItemResponse>();
        return View(new ExamSessionListViewModel
        {
            Sessions = sessions
                .OrderByDescending(session => session.Date)
                .ThenByDescending(session => session.Id)
                .ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ExamSessionCreateViewModel();
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ExamSessionCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin phiên thi.";
            await LoadOptionsAsync(model, cancellationToken);
            return View(model);
        }

        var optionsLoaded = await LoadOptionsAsync(model, cancellationToken);
        if (!optionsLoaded || !model.CanCreate)
        {
            if (optionsLoaded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Cần ít nhất một giảng viên, một môn học và một sinh viên để tạo phiên thi.");
                TempData["Error"] = "Dữ liệu lựa chọn chưa sẵn sàng.";
            }

            return View(model);
        }

        var request = new ExamSessionCreateRequest
        {
            InstructorId = model.InstructorId,
            SubjectId = model.SubjectId,
            Name = model.Name,
            Date = model.Date!.Value.Date,
            Participants = model.Participants
                .Select(participant => new ExamSessionParticipantRequest
                {
                    StudentId = participant.StudentId,
                    StartTime = ToUtc(participant.StartTime!.Value),
                    EndTime = ToUtc(participant.StartTime.Value.AddMinutes(participant.DurationMinutes)),
                    RequestedCoreCount = participant.RequestedCoreCount,
                    MaximumDeepCount = participant.MaximumDeepCount
                })
                .ToList()
        };
        var response = await _service.CreateAsync(request, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            AddErrors(response.Errors, response.Error);
            TempData["Error"] = GetError(response, "Không thể tạo phiên thi.");
            await LoadOptionsAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = $"Đã tạo phiên thi {response.Data.Name}.";
        return RedirectToAction(nameof(Details), new { id = response.Data.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var response = await _service.GetAsync(
            new ExamSessionGetRequest { Id = id },
            cancellationToken);
        if (!response.Success || response.Data is null)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(response, "Không thể tải chi tiết phiên thi.");
            return RedirectToAction(nameof(Index));
        }

        return View(new ExamSessionDetailViewModel { Session = response.Data });
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

        var getResponse = await _service.GetAsync(
            new ExamSessionGetRequest { Id = id },
            cancellationToken);
        if (!getResponse.Success || getResponse.Data is null)
        {
            if (IsNotFound(getResponse))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(getResponse, "Không thể tải phiên thi.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _service.DeleteAsync(
            new ExamSessionDeleteRequest { Id = id },
            cancellationToken);
        if (!response.Success)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(response, "Không thể xóa phiên thi.");
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Đã xóa phiên thi.";
        return RedirectToAction(nameof(Index));
    }

    private static DateTime ToUtc(DateTime localValue)
    {
        return DateTime.SpecifyKind(localValue, DateTimeKind.Local).ToUniversalTime();
    }

    private async Task<bool> LoadOptionsAsync(
        ExamSessionCreateViewModel model,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetCreationOptionsAsync(
            new ExamSessionCreationOptionsRequest(),
            cancellationToken);
        if (!response.Success || response.Data is null)
        {
            model.OptionsLoaded = false;
            model.OptionsError = GetError(response, "Không thể tải dữ liệu lựa chọn.");
            TempData["Error"] = model.OptionsError;
            return false;
        }

        model.InstructorOptions = response.Data.Instructors
            .OrderBy(instructor => instructor.Name)
            .Select(instructor => new SelectListItem
            {
                Value = instructor.Id.ToString(),
                Text = $"{instructor.Name} ({instructor.Email})"
            })
            .ToList();
        model.SubjectOptions = response.Data.Subjects
            .OrderBy(subject => subject.Code)
            .Select(subject => new SelectListItem
            {
                Value = subject.Id.ToString(),
                Text = $"{subject.Code} — {subject.Name}"
            })
            .ToList();
        model.StudentOptions = response.Data.Students
            .OrderBy(student => student.Code)
            .Select(student => new SelectListItem
            {
                Value = student.Id.ToString(),
                Text = $"{student.Code} — {student.Name}"
            })
            .ToList();
        model.OptionsLoaded = true;
        model.OptionsError = null;
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
