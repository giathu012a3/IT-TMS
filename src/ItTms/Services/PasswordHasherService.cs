using Microsoft.AspNetCore.Identity;



namespace ItTms.Services;

/// <summary>
/// Dịch vụ băm mật khẩu chuẩn ASP.NET Core Identity (PBKDF2 HMAC-SHA512).
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();
    private static readonly User DummyUser = User.Create("dummy", "dummy@local", "dummyhash", "Dummy", UserRole.User).Value;

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(DummyUser, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
            return false;

        try
        {
            var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
