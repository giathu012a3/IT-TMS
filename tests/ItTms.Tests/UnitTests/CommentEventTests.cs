using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
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
using ItTms.Domain.Events;
using ItTms.Infrastructure.Events;
using ItTms.Infrastructure.Persistence;
using ItTms.Tests.SignalR;
using ItTms.Web.Areas.Leader.Controllers;
using ItTms.Web.Areas.Staff.Controllers;
using ItTms.Web.Areas.User.Controllers;
using ItTms.Web.Models.Realtime;
using ItTms.Web.Services;
using Xunit;

namespace ItTms.Tests.UnitTests;

/// <summary>
/// Bộ kiểm thử chuyên sâu cho luồng Comment Domain Event, phân phối SignalR WebSocket và kiểm soát bình luận nội bộ (IsInternal).
/// </summary>
public class CommentEventTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<INotificationDispatcher> _mockNotificationDispatcher;

    private readonly int _creatorId;
    private readonly int _staffId;
    private readonly int _leaderId;
    private readonly int _ticketId;

    public CommentEventTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<ItTmsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _mockNotificationDispatcher = new Mock<INotificationDispatcher>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ItTmsDbContext>(options => options.UseSqlite(_connection));
        services.AddApplication();
        services.AddScoped<IItTmsDbContext>(sp => sp.GetRequiredService<ItTmsDbContext>());
        services.AddScoped<INotificationDispatcher>(_ => _mockNotificationDispatcher.Object);
        services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();

        _serviceProvider = services.BuildServiceProvider();

        using var ctx = new ItTmsDbContext(_dbOptions);
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

        var u = User.Create("alice", "alice@example.com", "hash", "Alice Creator", UserRole.User).Value;
        var s = User.Create("bob", "bob@example.com", "hash", "Bob Staff", UserRole.Staff).Value;
        var l = User.Create("charlie", "charlie@example.com", "hash", "Charlie Leader", UserRole.Leader).Value;

        ctx.Users.AddRange(u, s, l);
        ctx.SaveChanges();

        _creatorId = u.Id;
        _staffId = s.Id;
        _leaderId = l.Id;

        var ticket = Ticket.Create("VPN Issue", "Cannot connect to VPN", TicketPriority.High, TicketCategory.Network, _creatorId).Value;
        ticket.Assign(_staffId, _leaderId);
        ticket.StartProgress(_staffId);
        ctx.Tickets.Add(ticket);
        ctx.SaveChanges();

        _ticketId = ticket.Id;
    }

    public void Dispose()
    {
        _connection.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void ConfigureController(Controller controller, int userId, string userName, string role, bool isAjax = true)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        if (isAjax)
        {
            httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        controller.Url = new Mock<IUrlHelper>().Object;
    }

    private static readonly System.Text.Json.JsonSerializerOptions s_jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public async Task UserAddComment_TriggersDomainEvent_CreatesNotificationAndSystemLog_ReturnsCommentDto()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketController>>();
        var envMock = new Mock<IWebHostEnvironment>();

        var controller = new TicketController(db, envMock.Object, logger);
        ConfigureController(controller, _creatorId, "Alice Creator", RoleNames.User, isAjax: true);

        var actionResult = await controller.AddComment(_ticketId, "User phản hồi: Đã kiểm tra lại kết nối mạng.");

        // 1. Phản hồi Ajax phải là OkObjectResult với DTO
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value;
        data.Should().NotBeNull();

        var json = System.Text.Json.JsonSerializer.Serialize(data, s_jsonOptions);
        json.Should().Contain("\"success\":true");
        json.Should().Contain("User phản hồi: Đã kiểm tra lại kết nối mạng.");
        json.Should().Contain("\"isInternal\":false");

        // 2. Bình luận đã được lưu vào CSDL
        var commentInDb = await db.Comments.FirstOrDefaultAsync(c => c.TicketId == _ticketId && c.Content.Contains("User phản hồi"));
        commentInDb.Should().NotBeNull();
        commentInDb!.IsInternal.Should().BeFalse();
        commentInDb.UserId.Should().Be(_creatorId);

        // 3. NotificationObserver đã được kích hoạt -> tạo bản ghi Notification cho Staff
        var notif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _staffId && n.Link.Contains($"/Staff/Ticket/Details/{_ticketId}"));
        notif.Should().NotBeNull("NotificationObserver phải tạo thông báo cho Staff phụ trách");

        // 4. SystemLogObserver đã được kích hoạt -> tạo bản ghi SystemLog
        var log = await db.SystemLogs.FirstOrDefaultAsync(l => l.Action == "AddComment" && l.UserId == _creatorId);
        log.Should().NotBeNull("SystemLogObserver phải ghi nhật ký cho AddComment");

        // 5. INotificationDispatcher phải nhận được CommentAddedEvent với CommentId > 0
        _mockNotificationDispatcher.Verify(d => d.DispatchDomainEventAsync(
            It.Is<CommentAddedEvent>(e => e.CommentId == commentInDb.Id && e.TicketId == _ticketId && !e.IsInternal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StaffAddComment_InternalNote_AddsDomainEvent_ShieldsUserFromNotification_LogsInternalNote()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<StaffTicketController>>();

        var controller = new StaffTicketController(db, logger);
        ConfigureController(controller, _staffId, "Bob Staff", RoleNames.Staff, isAjax: true);

        var actionResult = await controller.AddComment(_ticketId, "Ghi chú nội bộ: Khách hàng cần nâng cấp gói VPN.", isInternal: true);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value, s_jsonOptions);
        json.Should().Contain("\"isInternal\":true");
        json.Should().Contain("Đã lưu ghi chú nội bộ.");

        // 1. Kiểm tra bình luận nội bộ trong CSDL
        var commentInDb = await db.Comments.FirstOrDefaultAsync(c => c.TicketId == _ticketId && c.IsInternal);
        commentInDb.Should().NotBeNull();
        commentInDb!.IsInternal.Should().BeTrue();

        // 2. BẢO MẬT: NotificationObserver TUYỆT ĐỐI KHÔNG sinh thông báo cho User khi IsInternal = true
        var userNotif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _creatorId && n.Message.Contains("nội bộ"));
        userNotif.Should().BeNull("Ghi chú nội bộ tuyệt đối không được sinh Notification cho User");

        // 3. SystemLogObserver phải ghi nhận action AddInternalNote
        var log = await db.SystemLogs.FirstOrDefaultAsync(l => l.Action == "AddInternalNote" && l.UserId == _staffId);
        log.Should().NotBeNull("SystemLogObserver phải ghi nhận AddInternalNote");

        // 4. INotificationDispatcher nhận được sự kiện với isInternal = true
        _mockNotificationDispatcher.Verify(d => d.DispatchDomainEventAsync(
            It.Is<CommentAddedEvent>(e => e.CommentId == commentInDb.Id && e.TicketId == _ticketId && e.IsInternal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaderAddComment_Public_TriggersDomainEvent_NotifiesUserAndStaff()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LeaderTicketController>>();

        var controller = new LeaderTicketController(db, logger);
        ConfigureController(controller, _leaderId, "Charlie Leader", RoleNames.Leader, isAjax: true);

        var actionResult = await controller.AddComment(_ticketId, "Leader chỉ đạo: Ưu tiên xử lý trong buổi sáng.", isInternal: false);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        json.Should().Contain("\"success\":true");

        // NotificationObserver tạo thông báo cho User tạo ticket
        var userNotif = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == _creatorId);
        userNotif.Should().NotBeNull("User phải nhận thông báo khi Leader phản hồi công khai");
    }

    [Fact]
    public async Task SignalRNotificationDispatcher_HandleCommentAddedAsync_PublicComment_DispatchesReceiveComment_AndNotifiesCounterparts()
    {
        using var db = new ItTmsDbContext(_dbOptions);
        var hubContext = new TestHubContext();
        var loggerMock = new Mock<ILogger<SignalRNotificationDispatcher>>();
        var dispatcher = new SignalRNotificationDispatcher(hubContext, db, loggerMock.Object);

        var ev = new CommentAddedEvent(
            CommentId: 101,
            TicketId: _ticketId,
            AuthorUserId: _creatorId,
            Content: "Tin nhắn công khai kiểm thử SignalR",
            IsInternal: false,
            OccurredOn: DateTime.UtcNow
        );

        await dispatcher.HandleCommentAddedAsync(ev);

        // 1. Nhóm phòng ticket_{id} nhận sự kiện ReceiveComment
        var ticketGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}"];
        ticketGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveComment");
        var commentDto = ticketGroup.SentMessages.First(m => m.Method == "ReceiveComment").Args[0] as CommentDto;
        commentDto.Should().NotBeNull();
        commentDto!.Content.Should().Be("Tin nhắn công khai kiểm thử SignalR");
        commentDto.IsInternal.Should().BeFalse();

        // 2. Staff phụ trách nhận ReceiveNotification
        var staffGroup = hubContext.HubClients.GroupsMap[$"user_{_staffId}"];
        staffGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");

        // 3. Leader nhận ReceiveNotification
        var leaderGroup = hubContext.HubClients.GroupsMap[$"user_{_leaderId}"];
        leaderGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");

        // 4. User tác giả không nhận thông báo chuông về chính bình luận của mình
        hubContext.HubClients.GroupsMap.ContainsKey($"user_{_creatorId}").Should().BeFalse();
    }

    [Fact]
    public async Task SignalRNotificationDispatcher_HandleCommentAddedAsync_InternalComment_StrictlyShieldsUser_OnlyDispatchesInternalAndStaffLeader()
    {
        using var db = new ItTmsDbContext(_dbOptions);
        var hubContext = new TestHubContext();
        var loggerMock = new Mock<ILogger<SignalRNotificationDispatcher>>();
        var dispatcher = new SignalRNotificationDispatcher(hubContext, db, loggerMock.Object);

        var ev = new CommentAddedEvent(
            CommentId: 202,
            TicketId: _ticketId,
            AuthorUserId: _staffId,
            Content: "Ghi chú tuyệt mật giữa Staff và Leader",
            IsInternal: true,
            OccurredOn: DateTime.UtcNow
        );

        await dispatcher.HandleCommentAddedAsync(ev);

        // 1. Phòng nội bộ nhận ReceiveComment
        var internalGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}_internal"];
        internalGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveComment");

        // 2. Phòng công khai TUYỆT ĐỐI không nhận
        hubContext.HubClients.GroupsMap.ContainsKey($"ticket_{_ticketId}").Should().BeFalse();

        // 3. User TUYỆT ĐỐI KHÔNG nhận thông báo chuông
        hubContext.HubClients.GroupsMap.ContainsKey($"user_{_creatorId}").Should().BeFalse();

        // 4. Leader nhận ReceiveNotification
        var leaderGroup = hubContext.HubClients.GroupsMap[$"user_{_leaderId}"];
        leaderGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");
        var notifDto = leaderGroup.SentMessages.First(m => m.Method == "ReceiveNotification").Args[0] as NotificationDto;
        notifDto!.Message.Should().Contain("ghi chú nội bộ");
    }

    [Fact]
    public async Task UserAddComment_WhenTicketResolved_IsBlockedByCanComment_DoesNotAddCommentOrEvent()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketController>>();
        var envMock = new Mock<IWebHostEnvironment>();

        var ticket = await db.Tickets.FirstAsync(t => t.Id == _ticketId);
        ticket.Resolve(_staffId);
        await db.SaveChangesAsync();

        var controller = new TicketController(db, envMock.Object, logger);
        ConfigureController(controller, _creatorId, "Alice Creator", RoleNames.User, isAjax: true);

        var actionResult = await controller.AddComment(_ticketId, "Cố gắng gửi bình luận trên ticket đã Resolved");

        var badRequest = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = System.Text.Json.JsonSerializer.Serialize(badRequest.Value, s_jsonOptions);
        json.Should().Contain("không thể gửi thêm bình luận");

        var commentCount = await db.Comments.CountAsync(c => c.TicketId == _ticketId && c.Content.Contains("Cố gắng gửi"));
        commentCount.Should().Be(0);

        _mockNotificationDispatcher.Verify(d => d.DispatchDomainEventAsync(
            It.IsAny<CommentAddedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
