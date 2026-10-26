using System.ComponentModel.DataAnnotations;
using ItTms.Domain.Enums;

namespace ItTms.Web.Models.ViewModels;

public class UserItemViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Department { get; set; }
    public bool IsActive => Status == UserStatus.Active;
    public DateTime CreatedAt { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
    [StringLength(80, ErrorMessage = "Tên đăng nhập tối đa 80 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu ban đầu không được để trống.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [Display(Name = "Họ và tên đầy đủ")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [Display(Name = "Vai trò hệ thống")]
    public UserRole Role { get; set; } = UserRole.User;

    [Display(Name = "Phòng ban")]
    public string? Department { get; set; }
}

public class EditUserViewModel
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    [Display(Name = "Họ và tên đầy đủ")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [Display(Name = "Vai trò hệ thống")]
    public UserRole Role { get; set; }

    [Display(Name = "Phòng ban")]
    public string? Department { get; set; }
}

public class ResetPasswordViewModel
{
    [Required]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;
}

public class SystemLogItemViewModel
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedTime => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}

public class AuditFilterViewModel
{
    public string? ActionType { get; set; }
    public int? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<SystemLogItemViewModel> Logs { get; set; } = [];
}
