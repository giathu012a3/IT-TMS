using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ItTms.Application;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Security;
using ItTms.Web.Areas.Admin.Controllers;
using ItTms.Web.Areas.Auth.Controllers;
using ItTms.Web.Areas.Leader.Controllers;
using ItTms.Web.Areas.Staff.Controllers;
using ItTms.Web.Areas.User.Controllers;
using ItTms.Web.Filters;
using ItTms.Web.Models.ViewModels;
using ItTms.Tests.SignalR;
using Xunit;

namespace ItTms.Tests.E2EVerificationTests;

/// <summary>
/// Milestone 4 — Phase A: Kiểm thử nghiệm thu toàn diện 10 kịch bản trong Bảng 7.2 của Báo cáo (TC-01 đến TC-10).
/// Mỗi bài kiểm thử thực thi trực tiếp trên cơ sở dữ liệu SQLite, hệ thống DI, Controllers, ActionFilters,
/// State Pattern và Observer Pattern thực tế.
/// </summary>
public class Table72VerificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly IPasswordHasherService _passwordHasher;

    private readonly int _adminId;
    private readonly int _leaderId;
    private readonly int _staff1Id;
    private readonly int _staff2Id;
    private readonly int _activeUserId;
    private readonly int _inactiveUserId;

    public Table72VerificationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ItTmsDbContext>((sp, options) =>
        {
            options.UseSqlite(_connection);
        });
        services.AddApplication();
        services.AddScoped<IItTmsDbContext>(sp => sp.GetRequiredService<ItTmsDbContext>());
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<INotificationDispatcher>(_ => new Mock<INotificationDispatcher>().Object);
        services.AddScoped<IDomainEventDispatcher, ItTms.Infrastructure.Events.InMemoryDomainEventDispatcher>();

        _serviceProvider = services.BuildServiceProvider();
        _passwordHasher = _serviceProvider.GetRequiredService<IPasswordHasherService>();

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        ctx.Database.EnsureCreated();

        ctx.TicketStatuses.AddRange(
            TicketStatus.Create(TicketStatus.New, "New", "Mới tạo", "badge bg-primary", 1),
            TicketStatus.Create(TicketStatus.Assigned, "Assigned", "Đã phân công", "badge bg-info", 2),
            TicketStatus.Create(TicketStatus.InProgress, "InProgress", "Đang xử lý", "badge bg-warning", 3),
            TicketStatus.Create(TicketStatus.Waiting, "Waiting", "Chờ phản hồi", "badge bg-secondary", 4),
            TicketStatus.Create(TicketStatus.Resolved, "Resolved", "Đã giải quyết", "badge bg-success", 5),
            TicketStatus.Create(TicketStatus.Closed, "Closed", "Đã đóng", "badge bg-dark", 6),
            TicketStatus.Create(TicketStatus.Rejected, "Rejected", "Đã từ chối", "badge bg-danger", 7)
        );

        var hash = _passwordHasher.HashPassword("Password@123");

        var admin = User.Create("admin_tc", "admin_tc@ittms.local", hash, "Quản trị viên TC", UserRole.Admin, "Ban Giám đốc", UserStatus.Active).Value;
        var leader = User.Create("leader_tc", "leader_tc@ittms.local", hash, "Trưởng nhóm TC", UserRole.Leader, "Phòng IT", UserStatus.Active).Value;
        var staff1 = User.Create("staff1_tc", "staff1_tc@ittms.local", hash, "Kỹ thuật viên 1", UserRole.Staff, "Phòng IT", UserStatus.Active).Value;
        var staff2 = User.Create("staff2_tc", "staff2_tc@ittms.local", hash, "Kỹ thuật viên 2", UserRole.Staff, "Phòng IT", UserStatus.Active).Value;
        var activeUser = User.Create("user_active", "user_active@ittms.local", hash, "Người dùng Hoạt động", UserRole.User, "Phòng Kế toán", UserStatus.Active).Value;
        var inactiveUser = User.Create("user_inactive", "user_inactive@ittms.local", hash, "Người dùng Bị khóa", UserRole.User, "Phòng Hành chính", UserStatus.Inactive).Value;

        ctx.Users.AddRange(admin, leader, staff1, staff2, activeUser, inactiveUser);
        ctx.SaveChanges();

        _adminId = admin.Id;
        _leaderId = leader.Id;
        _staff1Id = staff1.Id;
        _staff2Id = staff2.Id;
        _activeUserId = activeUser.Id;
        _inactiveUserId = inactiveUser.Id;
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ConfigureControllerContext(Controller controller, IServiceProvider sp, int userId, string username, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestCookieAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            RequestServices = sp
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
        };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        controller.Url = new Mock<IUrlHelper>().Object;
    }

    /// <summary>
    /// Kịch bản 1 (Bảng 7.2 TC-01): Đăng nhập bằng tài khoản đã bị vô hiệu hóa.
    /// Kết quả mong đợi: Từ chối, báo "Tài khoản của bạn đã bị vô hiệu hóa..." và ghi SystemLog.
    /// </summary>
    [Fact]
    public async Task TC01_LoginWithInactiveAccount_IsRejectedWithDeactivatedErrorMessage()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthController>>();
        var controller = new AuthController(db, _passwordHasher, logger);
        ConfigureControllerContext(controller, scope.ServiceProvider, 0, "anonymous", "");

        var loginModel = new LoginViewModel
        {
            Username = "user_inactive",
            Password = "Password@123"
        };

        var result = await controller.Login(loginModel);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        controller.ModelState.IsValid.Should().BeFalse();
        var errorMessages = controller.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage).ToList();
        errorMessages.Should().Contain(m => m.Contains("Tài khoản của bạn đã bị vô hiệu hóa"));

        var denyLog = await db.SystemLogs.FirstOrDefaultAsync(l => l.Action == "Login Denied" && l.UserId == _inactiveUserId);
        denyLog.Should().NotBeNull();
    }

    /// <summary>
    /// Kịch bản 2 (Bảng 7.2 TC-02): User truy cập trang chỉ dành cho Leader.
    /// Kết quả mong đợi: AuthorizeRolesAttribute chặn truy cập và trả về lỗi HTTP 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task TC02_UserAccessingLeaderPage_ReturnsHttp403Forbidden()
    {
        using var scope = _serviceProvider.CreateScope();
        var filter = new AuthorizeRolesAttribute(RoleNames.Leader, RoleNames.Admin);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _activeUserId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, "user_active"),
            new(ClaimTypes.Role, RoleNames.User)
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookie")),
            RequestServices = scope.ServiceProvider
        };
        httpContext.Request.Path = "/Leader/Dashboard";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new Mock<Controller>().Object);

        bool nextCalled = false;
        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
        }

        await filter.OnActionExecutionAsync(executingContext, Next);

        nextCalled.Should().BeFalse("User không được phép đi vào Action của Leader");
        var viewResult = executingContext.Result.Should().BeOfType<ViewResult>().Subject;
        viewResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Kịch bản 3 (Bảng 7.2 TC-03): User tạo ticket mới.
    /// Kết quả mong đợi: Ticket ở trạng thái New; Leader nhận được thông báo (NotificationObserver).
    /// </summary>
    [Fact]
    public async Task TC03_UserCreatesNewTicket_InitializesNewStateAndNotifiesLeader()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var envMock = new Mock<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketController>>();

        var controller = new TicketController(db, envMock.Object, logger);
        ConfigureControllerContext(controller, scope.ServiceProvider, _activeUserId, "user_active", RoleNames.User);

        var createModel = new CreateTicketViewModel
        {
            Title = "Mất kết nối mạng nội bộ tầng 3",
            Description = "Không thể truy cập máy chủ ERP từ 8h sáng.",
            Priority = TicketPriority.High,
            Category = TicketCategory.Network
        };

        var result = await controller.Create(createModel);

        result.Should().BeOfType<RedirectToActionResult>();

        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Title == createModel.Title);
        ticket.Should().NotBeNull();
        ticket!.StatusId.Should().Be(TicketStatus.New);

        var leaderNotifications = await db.Notifications
            .Where(n => n.UserId == _leaderId && n.Message.Contains("Mất kết nối mạng nội bộ tầng 3"))
            .ToListAsync();
        leaderNotifications.Should().ContainSingle("Leader phải nhận được thông báo khi có ticket mới");
    }

    /// <summary>
    /// Kịch bản 4 (Bảng 7.2 TC-04): Leader phân công ticket New cho Staff.
    /// Kết quả mong đợi: Ticket chuyển sang Assigned; cả Staff và User đều nhận được thông báo.
    /// </summary>
    [Fact]
    public async Task TC04_LeaderAssignsNewTicketToStaff_TransitionsToAssignedAndNotifiesStaffAndUser()
    {
        int ticketId;
        using (var setupScope = _serviceProvider.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var t = Ticket.Create("Lỗi phần mềm kế toán", "Không xuất được hóa đơn", TicketPriority.High, TicketCategory.Software, _activeUserId).Value;
            setupDb.Tickets.Add(t);
            await setupDb.SaveChangesAsync();
            ticketId = t.Id;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaderTicketController>>();

        var controller = new LeaderTicketController(db, logger);
        ConfigureControllerContext(controller, scope.ServiceProvider, _leaderId, "leader_tc", RoleNames.Leader);

        var assignModel = new AssignTicketViewModel
        {
            TicketId = ticketId,
            StaffId = _staff1Id
        };

        var result = await controller.Assign(assignModel);
        result.Should().BeOfType<RedirectToActionResult>();

        var updatedTicket = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        updatedTicket.StatusId.Should().Be(TicketStatus.Assigned);
        updatedTicket.AssignedToId.Should().Be(_staff1Id);

        var staffNotif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _staff1Id && n.Message.Contains($"#{ticketId}"));
        var userNotif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _activeUserId && n.Message.Contains($"#{ticketId}"));

        staffNotif.Should().NotBeNull("Staff được phân công phải nhận thông báo");
        userNotif.Should().NotBeNull("User tạo ticket phải nhận thông báo đã phân công");
    }

    /// <summary>
    /// Kịch bản 5 (Bảng 7.2 TC-05): Staff đặt trạng thái Closed (hoặc thao tác sai trạng thái) cho ticket đang New.
    /// Kết quả mong đợi: Bị từ chối bởi State Pattern, dữ liệu giữ nguyên trạng thái New.
    /// </summary>
    [Fact]
    public async Task TC05_InvalidStateTransitionOnNewTicket_IsRejectedAndDataRemainsUnchanged()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();

        var ticket = Ticket.Create("Sự cố máy chiếu", "Không lên hình", TicketPriority.Medium, TicketCategory.Hardware, _activeUserId).Value;
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Thử đóng trực tiếp ticket đang ở trạng thái New
        var closeResult = ticket.Close(_staff1Id);
        closeResult.IsFailure.Should().BeTrue("Ticket đang ở trạng thái New không được phép chuyển sang Closed");

        // Thử Resolve trực tiếp ticket đang ở trạng thái New
        var resolveResult = ticket.Resolve(_staff1Id);
        resolveResult.IsFailure.Should().BeTrue("Ticket đang ở trạng thái New không được phép chuyển sang Resolved");

        // Kiểm tra dữ liệu giữ nguyên
        var unchangedTicket = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        unchangedTicket.StatusId.Should().Be(TicketStatus.New);
        unchangedTicket.ClosedAt.Should().BeNull();
    }

    /// <summary>
    /// Kịch bản 6 (Bảng 7.2 TC-06): Leader từ chối ticket đang In Progress.
    /// Kết quả mong đợi: Bị từ chối bởi InProgressState, dữ liệu giữ nguyên trạng thái InProgress.
    /// </summary>
    [Fact]
    public async Task TC06_LeaderRejectsInProgressTicket_IsRejectedAndRemainsInProgress()
    {
        int ticketId;
        using (var setupScope = _serviceProvider.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var t = Ticket.Create("Cài đặt Visual Studio", "Cần bản 2022", TicketPriority.Low, TicketCategory.Software, _activeUserId).Value;
            t.Assign(_staff1Id, _leaderId);
            t.StartProgress(_staff1Id);
            setupDb.Tickets.Add(t);
            await setupDb.SaveChangesAsync();
            ticketId = t.Id;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaderTicketController>>();
        var controller = new LeaderTicketController(db, logger);
        ConfigureControllerContext(controller, scope.ServiceProvider, _leaderId, "leader_tc", RoleNames.Leader);

        var rejectModel = new RejectTicketViewModel
        {
            TicketId = ticketId,
            Reason = "Thử từ chối khi đang xử lý"
        };

        await controller.Reject(rejectModel);

        controller.TempData["ErrorMessage"]?.ToString().Should().NotBeNullOrEmpty();

        var verifyTicket = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        verifyTicket.StatusId.Should().Be(TicketStatus.InProgress, "Trạng thái phải giữ nguyên là InProgress");
        verifyTicket.RejectionReason.Should().BeNull();
    }

    /// <summary>
    /// Kịch bản 7 (Bảng 7.2 TC-07): Staff mở ticket không được phân công cho mình.
    /// Kết quả mong đợi: Trả về lỗi HTTP 403 Forbidden (AuthorizeTicketAccessAttribute & Controller BOLA guard).
    /// </summary>
    [Fact]
    public async Task TC07_StaffAccessingUnassignedTicket_ReturnsHttp403Forbidden()
    {
        int ticketId;
        using (var setupScope = _serviceProvider.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var t = Ticket.Create("Hỗ trợ tài khoản AD", "Quên mật khẩu domain", TicketPriority.Medium, TicketCategory.Account, _activeUserId).Value;
            t.Assign(_staff1Id, _leaderId); // Phân công cho Staff 1
            setupDb.Tickets.Add(t);
            await setupDb.SaveChangesAsync();
            ticketId = t.Id;
        }

        // Staff 2 (không được phân công) cố truy cập ticket của Staff 1 qua AuthorizeTicketAccessAttribute
        using var scope = _serviceProvider.CreateScope();
        var filter = new AuthorizeTicketAccessAttribute("id");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _staff2Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, "staff2_tc"),
            new(ClaimTypes.Role, RoleNames.Staff)
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookie")),
            RequestServices = scope.ServiceProvider
        };

        var routeData = new RouteData();
        routeData.Values["id"] = ticketId;

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        var actionArgs = new Dictionary<string, object?> { ["id"] = ticketId };
        var executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArgs,
            new Mock<Controller>().Object);

        bool nextCalled = false;
        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
        }

        await filter.OnActionExecutionAsync(executingContext, Next);

        nextCalled.Should().BeFalse("Staff 2 không được phép mở ticket phân công cho Staff 1");
        var viewResult = executingContext.Result.Should().BeOfType<ViewResult>().Subject;
        viewResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Kịch bản 8 (Bảng 7.2 TC-08): User bình luận trên ticket đã Resolved.
    /// Kết quả mong đợi: Bị chặn bởi State Pattern (CanComment == false), không lưu bình luận vào CSDL.
    /// </summary>
    [Fact]
    public async Task TC08_UserCommentsOnResolvedTicket_IsBlockedAndNotSaved()
    {
        int ticketId;
        using (var setupScope = _serviceProvider.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var t = Ticket.Create("Sửa lỗi ổ cứng", "Máy kêu to", TicketPriority.Medium, TicketCategory.Hardware, _activeUserId).Value;
            t.Assign(_staff1Id, _leaderId);
            t.StartProgress(_staff1Id);
            t.Resolve(_staff1Id);
            setupDb.Tickets.Add(t);
            await setupDb.SaveChangesAsync();
            ticketId = t.Id;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var envMock = new Mock<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketController>>();
        var controller = new TicketController(db, envMock.Object, logger);
        ConfigureControllerContext(controller, scope.ServiceProvider, _activeUserId, "user_active", RoleNames.User);

        await controller.AddComment(ticketId, "Bình luận sau khi đã Resolved");

        controller.TempData["ErrorMessage"]?.ToString().Should().Contain("không thể gửi thêm bình luận");
        var commentCount = await db.Comments.CountAsync(c => c.TicketId == ticketId);
        commentCount.Should().Be(0, "Không được lưu bình luận trên ticket đã Resolved");
    }

    /// <summary>
    /// Kịch bản 9 (Bảng 7.2 TC-09): User xác nhận đóng và đánh giá ticket Resolved.
    /// Kết quả mong đợi: Ticket chuyển sang Closed; lưu đánh giá vào bảng Feedbacks; có ghi nhận trong SystemLogs.
    /// </summary>
    [Fact]
    public async Task TC09_UserClosesAndRatesResolvedTicket_TransitionsToClosedSavesFeedbackAndLogs()
    {
        int ticketId;
        using (var setupScope = _serviceProvider.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var t = Ticket.Create("Cấu hình VPN", "Cần kết nối từ xa", TicketPriority.High, TicketCategory.Network, _activeUserId).Value;
            t.Assign(_staff1Id, _leaderId);
            t.StartProgress(_staff1Id);
            t.Resolve(_staff1Id);
            setupDb.Tickets.Add(t);
            await setupDb.SaveChangesAsync();
            ticketId = t.Id;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var envMock = new Mock<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketController>>();
        var controller = new TicketController(db, envMock.Object, logger);
        ConfigureControllerContext(controller, scope.ServiceProvider, _activeUserId, "user_active", RoleNames.User);

        var closeModel = new CloseAndRateViewModel
        {
            TicketId = ticketId,
            Rating = 5,
            Comment = "Hỗ trợ rất nhanh và nhiệt tình!"
        };

        await controller.CloseAndRate(closeModel);

        var closedTicket = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        closedTicket.StatusId.Should().Be(TicketStatus.Closed);
        closedTicket.ClosedAt.Should().NotBeNull();

        var feedback = await db.Feedbacks.AsNoTracking().FirstOrDefaultAsync(f => f.TicketId == ticketId);
        feedback.Should().NotBeNull();
        feedback!.Rating.Should().Be(5);
        feedback.Comment.Should().Be("Hỗ trợ rất nhanh và nhiệt tình!");

        var logs = await db.SystemLogs.AsNoTracking()
            .Where(l => l.Details != null && l.Details.Contains($"#{ticketId}"))
            .ToListAsync();
        logs.Should().NotBeEmpty("Nhật ký hệ thống phải ghi nhận sự kiện đóng và đánh giá ticket");
    }

    /// <summary>
    /// Kịch bản 10 (Bảng 7.2 TC-10): Admin vô hiệu hóa tài khoản khác; thử tự vô hiệu hóa chính mình.
    /// Kết quả mong đợi: Tài khoản kia bị chuyển sang Inactive (không đăng nhập được); tự vô hiệu hóa chính mình bị chặn.
    /// </summary>
    [Fact]
    public async Task TC10_AdminDeactivatesOtherUserAndAttemptsSelfDeactivation_OtherBlockedAndSelfProtected()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserController>>();
        var userCtrl = new UserController(db, _passwordHasher, logger);
        ConfigureControllerContext(userCtrl, scope.ServiceProvider, _adminId, "admin_tc", RoleNames.Admin);

        // 1. Admin thử tự vô hiệu hóa chính mình -> Bị chặn
        await userCtrl.ToggleActive(_adminId);
        userCtrl.TempData["ErrorMessage"]?.ToString().Should().Contain("không thể tự vô hiệu hóa");

        var adminInDb = await db.Users.AsNoTracking().FirstAsync(u => u.Id == _adminId);
        adminInDb.Status.Should().Be(UserStatus.Active, "Admin không thể tự khóa tài khoản của chính mình");

        // 2. Admin vô hiệu hóa tài khoản của ActiveUser -> Thành công
        await userCtrl.ToggleActive(_activeUserId);
        var targetUserInDb = await db.Users.AsNoTracking().FirstAsync(u => u.Id == _activeUserId);
        targetUserInDb.Status.Should().Be(UserStatus.Inactive);

        // 3. Tài khoản vừa bị vô hiệu hóa thử đăng nhập -> Bị từ chối
        var authLogger = scope.ServiceProvider.GetRequiredService<ILogger<AuthController>>();
        var authCtrl = new AuthController(db, _passwordHasher, authLogger);
        ConfigureControllerContext(authCtrl, scope.ServiceProvider, 0, "anonymous", "");

        var loginResult = await authCtrl.Login(new LoginViewModel
        {
            Username = "user_active",
            Password = "Password@123"
        });

        loginResult.Should().BeOfType<ViewResult>();
        authCtrl.ModelState.IsValid.Should().BeFalse();
    }
}
