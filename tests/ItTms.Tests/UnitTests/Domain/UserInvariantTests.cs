using FluentAssertions;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;

namespace ItTms.Tests.UnitTests.Domain;

public class UserInvariantTests
{
    [Fact]
    public void CreateUser_WithValidParameters_InitializesCorrectly()
    {
        // Act
        var result = User.Create(
            "testuser",
            "testuser@ittms.local",
            "hashed_secret",
            "Nguyễn Văn Test",
            UserRole.Staff,
            "IT Helpdesk"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.Username.Should().Be("testuser");
        user.Email.Should().Be("testuser@ittms.local");
        user.Role.Should().Be(UserRole.Staff);
        user.Status.Should().Be(UserStatus.Active);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenAdminDeactivatesThemselves_Fails_Table72TC10()
    {
        // Arrange (Bảng 7.2 TC-10: Admin cannot deactivate their own account)
        var adminUser = User.Create(
            "admin",
            "admin@ittms.local",
            "hash",
            "Admin",
            UserRole.Admin
        ).Value;

        // Giả lập Id của Admin sau khi lưu DB là 1
        typeof(User).GetProperty("Id")!.SetValue(adminUser, 1);

        // Act
        var result = adminUser.Deactivate(operatingAdminId: 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ADMIN_CANNOT_DEACTIVATE_SELF");
        adminUser.Status.Should().Be(UserStatus.Active);
        adminUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenAdminDeactivatesOtherAccount_Succeeds()
    {
        // Arrange
        var targetUser = User.Create(
            "user1",
            "user1@ittms.local",
            "hash",
            "User One",
            UserRole.User
        ).Value;
        typeof(User).GetProperty("Id")!.SetValue(targetUser, 5);

        // Act
        var result = targetUser.Deactivate(operatingAdminId: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetUser.Status.Should().Be(UserStatus.Inactive);
        targetUser.IsActive.Should().BeFalse();
    }
}
