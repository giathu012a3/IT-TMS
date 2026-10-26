namespace ItTms.Domain.Enums;

/// <summary>
/// Hằng số chuỗi tên vai trò dùng cho Attribute Authorize và phân quyền hệ thống.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Leader = "Leader";
    public const string Staff = "Staff";
    public const string User = "User";

    public static readonly IReadOnlyList<string> All = [Admin, Leader, Staff, User];
}
