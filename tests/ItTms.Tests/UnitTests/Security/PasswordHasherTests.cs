using FluentAssertions;
using ItTms.Infrastructure.Security;

namespace ItTms.Tests.UnitTests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void HashPassword_ProducesNonEmptySaltedHash()
    {
        // Act
        var hash1 = _hasher.HashPassword("Admin@123");
        var hash2 = _hasher.HashPassword("Admin@123");

        // Assert
        hash1.Should().NotBeNullOrWhiteSpace();
        hash2.Should().NotBeNullOrWhiteSpace();
        hash1.Should().NotBe(hash2); // Khác nhau do cơ chế random salt 128-bit
    }

    [Theory]
    [InlineData("Admin@123", true)]
    [InlineData("Admin@124", false)]
    [InlineData("admin@123", false)]
    [InlineData("", false)]
    public void VerifyPassword_ValidatesCorrectly(string candidatePassword, bool expectedResult)
    {
        // Arrange
        var hash = _hasher.HashPassword("Admin@123");

        // Act
        var isValid = _hasher.VerifyPassword(hash, candidatePassword);

        // Assert
        isValid.Should().Be(expectedResult);
    }
}
