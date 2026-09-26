using AssignmentPRN.Business;
using AssignmentPRN.Presentation.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentPRN.Presentation.Controllers;

public sealed class SubjectsController : Controller
{
    private readonly ISubjectService _service;

    public SubjectsController(ISubjectService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var response = await _service.ListAsync(new SubjectListRequest(), cancellationToken);
        if (!response.Success)
        {
            TempData["Error"] = GetError(response, "Không thể tải danh sách môn học.");
            return View(new SubjectListViewModel());
        }

        return View(new SubjectListViewModel
        {
            Items = response.Data ?? Array.Empty<SubjectResponse>()
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SubjectFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SubjectFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại các trường bắt buộc.";
            return View(model);
        }

        var response = await _service.CreateAsync(new SubjectCreateRequest
        {
            Code = model.Code,
            Name = model.Name,
            Description = model.Description
        }, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            AddErrors(response.Errors, response.Error);
            TempData["Error"] = GetError(response, "Không thể tạo môn học.");
            return View(model);
        }

        TempData["Success"] = $"Đã thêm môn học {response.Data.Name}.";
        return RedirectToAction(nameof(Index));
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

        var response = await _service.GetAsync(new SubjectGetRequest { Id = id }, cancellationToken);
        if (!response.Success || response.Data is null)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(response, "Không thể tải môn học.");
            return RedirectToAction(nameof(Index));
        }

        return View(new SubjectFormViewModel
        {
            Id = response.Data.Id,
            Code = response.Data.Code,
            Name = response.Data.Name,
            Description = response.Data.Description
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        SubjectFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Id <= 0)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng kiểm tra lại các trường bắt buộc.";
            return View(model);
        }

        var response = await _service.UpdateAsync(new SubjectUpdateRequest
        {
            Id = model.Id,
            Code = model.Code,
            Name = model.Name,
            Description = model.Description
        }, cancellationToken);
        if (!response.Success)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            AddErrors(response.Errors, response.Error);
            TempData["Error"] = GetError(response, "Không thể cập nhật môn học.");
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật môn học.";
        return RedirectToAction(nameof(Index));
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

        var getResponse = await _service.GetAsync(new SubjectGetRequest { Id = id }, cancellationToken);
        if (!getResponse.Success || getResponse.Data is null)
        {
            if (IsNotFound(getResponse))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(getResponse, "Không thể tải môn học.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _service.DeleteAsync(new SubjectDeleteRequest { Id = id }, cancellationToken);
        if (!response.Success)
        {
            if (IsNotFound(response))
            {
                return NotFound();
            }

            TempData["Error"] = GetError(response, "Không thể xóa môn học.");
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa môn học.";
        return RedirectToAction(nameof(Index));
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
