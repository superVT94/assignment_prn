using AssignmentPRN.Business;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AssignmentPRN.Presentation.Models;

public enum QuestionCategoryChoice
{
    Core = 1,
    Deep = 2
}

public sealed class QuestionListViewModel
{
    public int? SubjectId { get; set; }

    public SubjectResponse? Subject { get; init; }

    public IReadOnlyList<QuestionRowViewModel> Questions { get; init; } =
        Array.Empty<QuestionRowViewModel>();

    public IReadOnlyList<SelectListItem> SubjectOptions { get; init; } =
        Array.Empty<SelectListItem>();

    public bool HasSubjects { get; init; }

    public string? LoadError { get; init; }
}

public sealed class QuestionRowViewModel
{
    public int Id { get; init; }

    public string Category { get; init; } = string.Empty;

    public bool IsDeep { get; init; }

    public string Topic { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int Difficulty { get; init; }
}

public sealed class QuestionFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học.")]
    public int SubjectId { get; set; }

    [EnumDataType(typeof(QuestionCategoryChoice), ErrorMessage = "Phân loại câu hỏi không hợp lệ.")]
    public QuestionCategoryChoice Category { get; set; } = QuestionCategoryChoice.Core;

    [Required(ErrorMessage = "Vui lòng nhập chủ đề.")]
    [StringLength(200, ErrorMessage = "Chủ đề không được vượt quá 200 ký tự.")]
    public string Topic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi.")]
    [StringLength(4000, ErrorMessage = "Nội dung không được vượt quá 4000 ký tự.")]
    public string Content { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Độ khó phải từ 1 đến 5.")]
    public int Difficulty { get; set; } = 2;

    public IReadOnlyList<SelectListItem> SubjectOptions { get; set; } =
        Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> CategoryOptions { get; set; } =
        Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> DifficultyOptions { get; set; } =
        Array.Empty<SelectListItem>();
}

public static class QuestionFormRequestMapper
{
    public static QuestionCreateRequest ToCreateRequest(this QuestionFormViewModel model)
    {
        return SetCategory(new QuestionCreateRequest
        {
            SubjectId = model.SubjectId,
            Topic = model.Topic,
            Content = model.Content,
            Difficulty = model.Difficulty
        }, model.Category);
    }

    public static QuestionUpdateRequest ToUpdateRequest(this QuestionFormViewModel model)
    {
        return SetCategory(new QuestionUpdateRequest
        {
            Id = model.Id,
            SubjectId = model.SubjectId,
            Topic = model.Topic,
            Content = model.Content,
            Difficulty = model.Difficulty
        }, model.Category);
    }

    private static TRequest SetCategory<TRequest>(
        TRequest request,
        QuestionCategoryChoice category)
        where TRequest : class
    {
        var property = typeof(TRequest).GetProperty(nameof(QuestionCreateRequest.Category))
            ?? throw new InvalidOperationException("Question category is unavailable.");
        var categoryType = property.PropertyType;
        if (!categoryType.IsEnum)
        {
            throw new InvalidOperationException("Question category is invalid.");
        }

        property.SetValue(request, Enum.ToObject(categoryType, (int)category));
        return request;
    }
}
