namespace ItTms.Tests.UnitTests.Security;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using ItTms.Web.Filters;
using Xunit;

public class AuthorizeRolesAttributeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    public AuthorizeRolesAttributeTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<ItTmsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new ItTmsDbContext(_dbOptions);
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserIsAnonymous_RedirectsToLogin()
    {
        // Arrange
        var filter = new AuthorizeRolesAttribute(RoleNames.Leader);
        var context = CreateActionExecutingContext(isAuthenticated: false);
        bool nextCalled = false;

        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert
        nextCalled.Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserIsInactive_RejectsAccess_Table72TC01()
    {
        // Arrange: Tạo user bị vô hiệu hóa trong CSDL (TC-01)
        int inactiveUserId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("staff_locked", "locked@local.test", "hash", "Staff Locked", UserRole.Staff, status: UserStatus.Inactive).Value;
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            inactiveUserId = user.Id;
        }

        var filter = new AuthorizeRolesAttribute(RoleNames.Staff);
        var context = CreateActionExecutingContext(isAuthenticated: true, userId: inactiveUserId, role: RoleNames.Staff);
        bool nextCalled = false;

        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert (Bảng 7.2 TC-01: Chuyển hướng kèm TempData báo tài khoản bị khóa)
        nextCalled.Should().BeFalse();
        context.Result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");

        var controller = context.Controller as Controller;
        controller?.TempData["ErrorMessage"].Should().Be("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên hệ thống để được hỗ trợ.");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserHasWrongRole_Returns403Forbidden_Table72TC02()
    {
        // Arrange: User thường truy cập trang của Leader (TC-02)
        int activeUserId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("user_normal", "user@local.test", "hash", "Normal User", UserRole.User, status: UserStatus.Active).Value;
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            activeUserId = user.Id;
        }

        var filter = new AuthorizeRolesAttribute(RoleNames.Leader);
        var context = CreateActionExecutingContext(isAuthenticated: true, userId: activeUserId, role: RoleNames.User);
        bool nextCalled = false;

        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert (Bảng 7.2 TC-02: Trả về HTTP 403 Forbidden)
        nextCalled.Should().BeFalse();
        var viewResult = context.Result.Should().BeOfType<ViewResult>().Subject;
        viewResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserHasValidRole_ProceedsToNext()
    {
        // Arrange: Staff truy cập trang Staff
        int activeUserId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("staff_user", "staff@local.test", "hash", "Staff User", UserRole.Staff, status: UserStatus.Active).Value;
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            activeUserId = user.Id;
        }

        var filter = new AuthorizeRolesAttribute(RoleNames.Staff);
        var context = CreateActionExecutingContext(isAuthenticated: true, userId: activeUserId, role: RoleNames.Staff);
        bool nextCalled = false;

        // Act
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    private ActionExecutingContext CreateActionExecutingContext(bool isAuthenticated, int userId = 1, string role = "User")
    {
        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();

        services.AddScoped<IItTmsDbContext>(_ => new ItTmsDbContext(_dbOptions));
        services.AddLogging();
        services.AddAuthentication();
        httpContext.RequestServices = services.BuildServiceProvider();

        if (isAuthenticated)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "testuser")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var mockController = new Mock<Controller>();
        mockController.Object.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            mockController.Object
        );
    }
}
