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

public class AuthorizeTicketAccessFilterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    public AuthorizeTicketAccessFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<ItTmsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new ItTmsDbContext(_dbOptions);
        ctx.Database.EnsureCreated();
        SeedTicketStatuses(ctx);
    }

    private static void SeedTicketStatuses(ItTmsDbContext ctx)
    {
        ctx.TicketStatuses.AddRange(
            TicketStatus.Create(TicketStatus.New, "New", "Mới tạo", "badge bg-primary", 1),
            TicketStatus.Create(TicketStatus.Assigned, "Assigned", "Đã phân công", "badge bg-info", 2),
            TicketStatus.Create(TicketStatus.InProgress, "InProgress", "Đang xử lý", "badge bg-warning", 3),
            TicketStatus.Create(TicketStatus.Waiting, "Waiting", "Chờ phản hồi", "badge bg-secondary", 4),
            TicketStatus.Create(TicketStatus.Resolved, "Resolved", "Đã giải quyết", "badge bg-success", 5),
            TicketStatus.Create(TicketStatus.Closed, "Closed", "Đã đóng", "badge bg-dark", 6),
            TicketStatus.Create(TicketStatus.Rejected, "Rejected", "Đã từ chối", "badge bg-danger", 7)
        );
        ctx.SaveChanges();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Staff_AccessingAnotherStaffTicket_Returns403Forbidden_Table72TC07()
    {
        // Arrange (Bảng 7.2 TC-07: Staff cannot access tickets not assigned to them)
        int ticketId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("user_creator", "creator@local.test", "hash", "Creator", UserRole.User).Value;
            var staff1 = User.Create("staff_assigned", "staff1@local.test", "hash", "Staff 1", UserRole.Staff).Value;
            var staff2 = User.Create("staff_intruder", "staff2@local.test", "hash", "Staff 2", UserRole.Staff).Value;
            ctx.Users.AddRange(user, staff1, staff2);
            await ctx.SaveChangesAsync();

            var ticket = Ticket.Create("Ticket của Staff 1", "Desc", TicketPriority.Medium, TicketCategory.Software, user.Id).Value;
            ticket.Assign(staff1.Id, leaderId: 1);
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        // Act: Staff 2 cố truy cập ticket của Staff 1
        var filter = new AuthorizeTicketAccessAttribute("id");
        var context = CreateActionExecutingContext(ticketId: ticketId, userId: 3, role: RoleNames.Staff);
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert (Bảng 7.2 TC-07: Chặn đứng bằng HTTP 403 Forbidden)
        nextCalled.Should().BeFalse();
        var viewResult = context.Result.Should().BeOfType<ViewResult>().Subject;
        viewResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        // Kiểm tra SystemLog ghi vết cảnh báo BOLA
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var auditLog = await ctx.SystemLogs.FirstOrDefaultAsync(l => l.Action == "BOLA_Violation_Attempt");
            auditLog.Should().NotBeNull();
            auditLog!.Details.Should().Contain($"Staff #3 vi phạm BOLA khi cố truy cập trái phép Ticket #{ticketId}");
        }
    }

    [Fact]
    public async Task Staff_AccessingOwnAssignedTicket_ProceedsToNext()
    {
        // Arrange
        int ticketId;
        int staff1Id;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("user_creator2", "c2@local.test", "hash", "Creator 2", UserRole.User).Value;
            var staff1 = User.Create("staff_assigned2", "s2@local.test", "hash", "Staff Assigned", UserRole.Staff).Value;
            ctx.Users.AddRange(user, staff1);
            await ctx.SaveChangesAsync();
            staff1Id = staff1.Id;

            var ticket = Ticket.Create("Ticket của Staff 1", "Desc", TicketPriority.Medium, TicketCategory.Software, user.Id).Value;
            ticket.Assign(staff1.Id, leaderId: 1);
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        // Act: Staff 1 truy cập ticket được phân công cho mình
        var filter = new AuthorizeTicketAccessAttribute("id");
        var context = CreateActionExecutingContext(ticketId: ticketId, userId: staff1Id, role: RoleNames.Staff);
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task User_AccessingAnotherUserTicket_Returns403Forbidden()
    {
        // Arrange: User 2 cố truy cập ticket của User 1
        int ticketId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user1 = User.Create("user1", "u1@local.test", "hash", "User 1", UserRole.User).Value;
            var user2 = User.Create("user2", "u2@local.test", "hash", "User 2", UserRole.User).Value;
            ctx.Users.AddRange(user1, user2);
            await ctx.SaveChangesAsync();

            var ticket = Ticket.Create("Ticket của User 1", "Desc", TicketPriority.Medium, TicketCategory.Software, user1.Id).Value;
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var filter = new AuthorizeTicketAccessAttribute("id");
        var context = CreateActionExecutingContext(ticketId: ticketId, userId: 2, role: RoleNames.User);
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert
        nextCalled.Should().BeFalse();
        var viewResult = context.Result.Should().BeOfType<ViewResult>().Subject;
        viewResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Leader_Or_Admin_AccessingAnyTicket_ProceedsToNext()
    {
        // Arrange
        int ticketId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("user_creator3", "c3@local.test", "hash", "Creator 3", UserRole.User).Value;
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var ticket = Ticket.Create("Ticket", "Desc", TicketPriority.Medium, TicketCategory.Software, user.Id).Value;
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var filter = new AuthorizeTicketAccessAttribute("id");
        var context = CreateActionExecutingContext(ticketId: ticketId, userId: 99, role: RoleNames.Leader);
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Assert
        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    private ActionExecutingContext CreateActionExecutingContext(int ticketId, int userId, string role)
    {
        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();

        services.AddScoped<IItTmsDbContext>(_ => new ItTmsDbContext(_dbOptions));
        services.AddLogging();
        httpContext.RequestServices = services.BuildServiceProvider();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, $"user_{userId}")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var routeData = new RouteData();
        routeData.Values.Add("id", ticketId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        var mockController = new Mock<Controller>();

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["id"] = ticketId },
            mockController.Object
        );
    }
}
