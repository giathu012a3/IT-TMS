namespace ItTms.Tests.UnitTests.Web;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using ItTms.Domain.Enums;
using ItTms.Web.Services;
using Xunit;

public class CurrentUserServiceTests
{
    [Fact]
    public void CurrentUserService_WhenAuthenticated_ReturnsCorrectClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "42"),
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Role, nameof(UserRole.Staff))
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.IsAuthenticated.Should().BeTrue();
        service.UserId.Should().Be(42);
        service.Username.Should().Be("testuser");
        service.Role.Should().Be(nameof(UserRole.Staff));
    }

    [Fact]
    public void CurrentUserService_WhenUnauthenticated_ReturnsNullProperties()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.IsAuthenticated.Should().BeFalse();
        service.UserId.Should().BeNull();
        service.Username.Should().BeNull();
        service.Role.Should().BeNull();
    }

    [Fact]
    public void CurrentUserService_WhenHttpContextIsNull_ReturnsNullProperties()
    {
        // Arrange
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.IsAuthenticated.Should().BeFalse();
        service.UserId.Should().BeNull();
        service.Username.Should().BeNull();
        service.Role.Should().BeNull();
    }

    [Fact]
    public void CurrentUserService_WhenNonNumericUserId_ReturnsNullUserId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-number"),
            new(ClaimTypes.Name, "someuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.UserId.Should().BeNull();
        service.Username.Should().Be("someuser");
    }
}
