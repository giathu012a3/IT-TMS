using System.Net.Mail;
using ItTms.Domain.Common;
using ItTms.Domain.Enums;

using ItTms.Domain.Events;

namespace ItTms.Domain.Entities;

public class User : AggregateRoot<int>
{
    // EF Core hydration constructor
    private User() { }

    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string? Department { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Helper property phục vụ kiểm tra nhanh trạng thái kích hoạt
    public bool IsActive => Status == UserStatus.Active;

    // Navigation Properties
    public virtual ICollection<Ticket> CreatedTickets { get; private set; } = new List<Ticket>();
    public virtual ICollection<Ticket> AssignedTickets { get; private set; } = new List<Ticket>();
    public virtual ICollection<Comment> Comments { get; private set; } = new List<Comment>();
    public virtual ICollection<Notification> Notifications { get; private set; } = new List<Notification>();
    public virtual ICollection<SystemLog> SystemLogs { get; private set; } = new List<SystemLog>();

    private static bool IsValidEmail(string email, out string normalizedEmail)
    {
        normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail.Contains(".."))
            return false;

        if (!MailAddress.TryCreate(normalizedEmail, out var parsedEmail))
            return false;

        return parsedEmail.Address == normalizedEmail
            && !parsedEmail.Host.StartsWith('.')
            && !parsedEmail.Host.EndsWith('.')
            && !parsedEmail.User.StartsWith('.')
            && !parsedEmail.User.EndsWith('.');
    }

    // Static Factory Method
    public static Result<User> Create(
        string username,
        string email,
        string passwordHash,
        string fullName,
        UserRole role,
        string? department = null,
        UserStatus status = UserStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<User>.Failure(new Error("USER_USERNAME_REQUIRED", "Tên đăng nhập không được để trống.", ErrorType.Validation));

        if (username.Length > 80)
            return Result<User>.Failure(new Error("USER_USERNAME_TOO_LONG", "Tên đăng nhập không vượt quá 80 ký tự.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(email))
            return Result<User>.Failure(new Error("USER_EMAIL_REQUIRED", "Email không được để trống.", ErrorType.Validation));

        if (email.Length > 150)
            return Result<User>.Failure(new Error("USER_EMAIL_TOO_LONG", "Email không vượt quá 150 ký tự.", ErrorType.Validation));

        if (!IsValidEmail(email, out var normalizedEmail))
            return Result<User>.Failure(new Error("USER_EMAIL_INVALID", "Định dạng địa chỉ email không hợp lệ.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result<User>.Failure(new Error("USER_PASSWORD_REQUIRED", "Mật khẩu đã băm không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(fullName))
            return Result<User>.Failure(new Error("USER_FULLNAME_REQUIRED", "Họ và tên không được để trống.", ErrorType.Validation));

        var user = new User
        {
            Username = username.Trim().ToLowerInvariant(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            Status = status,
            Department = department?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<User>.Success(user);
    }

    // Business Methods
    public Result UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure(new Error("USER_PASSWORD_EMPTY", "Mật khẩu mới không được để trống.", ErrorType.Validation));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateProfile(string fullName, string? department)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure(new Error("USER_FULLNAME_REQUIRED", "Họ và tên không được để trống.", ErrorType.Validation));

        FullName = fullName.Trim();
        Department = department?.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ChangeRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate(int operatingAdminId)
    {
        // Ràng buộc bất biến Bảng 7.2 TC-10: Admin không thể tự vô hiệu hóa tài khoản của chính mình
        if (Id == operatingAdminId)
            return Result.Failure(new Error("ADMIN_CANNOT_DEACTIVATE_SELF", "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình.", ErrorType.Forbidden));

        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserDeactivatedEvent(Id, operatingAdminId, null, UpdatedAt.Value));
        return Result.Success();
    }
}
