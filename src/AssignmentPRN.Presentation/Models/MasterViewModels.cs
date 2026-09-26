using AssignmentPRN.Business;
using System.ComponentModel.DataAnnotations;

namespace AssignmentPRN.Presentation.Models;

public sealed class InstructorListViewModel
{
    public IReadOnlyList<InstructorResponse> Items { get; init; } = Array.Empty<InstructorResponse>();
}

public sealed class InstructorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên giảng viên.")]
    [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(320, ErrorMessage = "Email không được vượt quá 320 ký tự.")]
    public string Email { get; set; } = string.Empty;
}

public sealed class SubjectListViewModel
{
    public IReadOnlyList<SubjectResponse> Items { get; init; } = Array.Empty<SubjectResponse>();
}

public sealed class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã môn học.")]
    [StringLength(20, ErrorMessage = "Mã môn học không được vượt quá 20 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên môn học.")]
    [StringLength(200, ErrorMessage = "Tên môn học không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }
}

public sealed class StudentListViewModel
{
    public IReadOnlyList<StudentResponse> Items { get; init; } = Array.Empty<StudentResponse>();
}

public sealed class StudentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên.")]
    [StringLength(30, ErrorMessage = "Mã sinh viên không được vượt quá 30 ký tự.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên sinh viên.")]
    [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự.")]
    public string Name { get; set; } = string.Empty;
}
