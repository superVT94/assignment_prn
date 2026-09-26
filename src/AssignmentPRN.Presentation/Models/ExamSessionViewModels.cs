using AssignmentPRN.Business;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AssignmentPRN.Presentation.Models;

public sealed class ExamSessionListViewModel
{
    public IReadOnlyList<ExamSessionListItemResponse> Sessions { get; init; } =
        Array.Empty<ExamSessionListItemResponse>();

    public string? LoadError { get; init; }
}

public sealed class ExamSessionDetailViewModel
{
    public ExamSessionDetailResponse Session { get; init; } = new();
}

public sealed class ExamSessionCreateViewModel : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn giảng viên.")]
    public int InstructorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học.")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên phiên thi.")]
    [StringLength(200, ErrorMessage = "Tên phiên thi không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày thi.")]
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; } = DateTime.Today.AddDays(1);

    [MinLength(1, ErrorMessage = "Phiên thi phải có ít nhất một lượt thi.")]
    public List<ExamSessionParticipantFormViewModel> Participants { get; set; } =
        new() { new ExamSessionParticipantFormViewModel() };

    public IReadOnlyList<SelectListItem> InstructorOptions { get; set; } =
        Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } =
        Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> StudentOptions { get; set; } =
        Array.Empty<SelectListItem>();

    public bool OptionsLoaded { get; set; }

    public string? OptionsError { get; set; }

    public bool CanCreate => OptionsLoaded
        && InstructorOptions.Count > 0
        && SubjectOptions.Count > 0
        && StudentOptions.Count > 0;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Participants is null || Participants.Count == 0)
        {
            yield return new ValidationResult(
                "Phải có ít nhất một lượt thi.",
                new[] { nameof(Participants) });
            yield break;
        }

        var studentIds = Participants
            .Where(participant => participant is not null && participant.StudentId > 0)
            .Select(participant => participant.StudentId)
            .ToList();
        if (studentIds.Count != studentIds.Distinct().Count())
        {
            yield return new ValidationResult(
                "Mỗi sinh viên chỉ được xuất hiện một lần trong phiên thi.",
                new[] { nameof(Participants) });
        }

        foreach (var participant in Participants)
        {
            if (participant is null)
            {
                yield return new ValidationResult(
                    "Lượt thi không được để trống.",
                    new[] { nameof(Participants) });
                continue;
            }

            if (Date.HasValue && participant.StartTime.HasValue
                && participant.StartTime.Value.Date != Date.Value.Date)
            {
                yield return new ValidationResult(
                    $"Lượt thi của sinh viên {participant.StudentId} phải diễn ra đúng ngày thi.",
                    new[] { nameof(Participants) });
            }
        }
    }
}

public sealed class ExamSessionParticipantFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sinh viên.")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu.")]
    [DataType(DataType.DateTime)]
    public DateTime? StartTime { get; set; } = DateTime.Today.AddDays(1).AddHours(9);

    [Range(1, 1440, ErrorMessage = "Thời lượng phải từ 1 đến 1440 phút.")]
    public int DurationMinutes { get; set; } = 60;

    [Range(1, 100, ErrorMessage = "Số câu Core phải từ 1 đến 100.")]
    public int RequestedCoreCount { get; set; } = 2;

    [Range(0, 100, ErrorMessage = "Số câu Deep tối đa phải từ 0 đến 100.")]
    public int MaximumDeepCount { get; set; } = 1;
}

public static class ExamSessionText
{
    public static string Status(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return text switch
        {
            "Draft" => "Bản nháp",
            "Scheduled" => "Đã lên lịch",
            "InProgress" => "Đang thi",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            _ => text
        };
    }

    public static string ParticipantStatus(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return text switch
        {
            "Pending" => "Chờ thi",
            "InProgress" => "Đang thi",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            _ => text
        };
    }
}
