using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Infrastructure.Persistence;
using ItTms.Web.Models.Realtime;
using ItTms.Web.Services;
using Xunit;

namespace ItTms.Tests.SignalR;

public class SignalRNotificationDispatcherTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    private readonly int _creatorId;
    private readonly int _staffId;
    private readonly int _leaderId;
    private readonly int _ticketId;

    public SignalRNotificationDispatcherTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<ItTmsDbContext>()
            .UseSqlite(_connection)
            .Options;

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
        ctx.Tickets.Add(ticket);

        // Add 2 unread notifications for creator
        ctx.Notifications.AddRange(
            Notification.Create(_creatorId, "Notif 1", "/link1").Value,
            Notification.Create(_creatorId, "Notif 2", "/link2").Value,
            Notification.Create(_staffId, "Staff Notif", "/link3").Value
        );

        ctx.SaveChanges();
        _ticketId = ticket.Id;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private (SignalRNotificationDispatcher Dispatcher, TestHubContext HubContext, ItTmsDbContext DbContext) CreateDispatcher()
    {
        var db = new ItTmsDbContext(_dbOptions);
        var hubContext = new TestHubContext();
        var loggerMock = new Mock<ILogger<SignalRNotificationDispatcher>>();

        var dispatcher = new SignalRNotificationDispatcher(hubContext, db, loggerMock.Object);
        return (dispatcher, hubContext, db);
    }

    [Fact]
    public async Task SendToUserAsync_DispatchesNotificationAndLiveUnreadCount()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        await dispatcher.SendToUserAsync(_creatorId, "Your ticket has been updated", $"/User/Ticket/Details/{_ticketId}");

        var userGroup = hubContext.HubClients.GroupsMap[$"user_{_creatorId}"];
        userGroup.Should().NotBeNull();

        // 1. ReceiveNotification
        userGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");
        var notifDto = userGroup.SentMessages.First(m => m.Method == "ReceiveNotification").Args[0] as NotificationDto;
        notifDto.Should().NotBeNull();
        notifDto!.UserId.Should().Be(_creatorId);
        notifDto.Message.Should().Be("Your ticket has been updated");

        // 2. UpdateUnreadNotificationCount (should be 2 based on seed data)
        userGroup.SentMessages.Should().Contain(m => m.Method == "UpdateUnreadNotificationCount");
        var unreadCount = (int)userGroup.SentMessages.First(m => m.Method == "UpdateUnreadNotificationCount").Args[0]!;
        unreadCount.Should().Be(2);
    }

    [Fact]
    public async Task SendToRoleAsync_BroadcastsToRoleGroup()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        await dispatcher.SendToRoleAsync(RoleNames.Leader, "New ticket awaiting triage", "/Leader/Ticket/NewQueue");

        var roleGroup = hubContext.HubClients.GroupsMap[$"role_{RoleNames.Leader}"];
        roleGroup.Should().NotBeNull();
        roleGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");

        var notifDto = roleGroup.SentMessages.First(m => m.Method == "ReceiveNotification").Args[0] as NotificationDto;
        notifDto!.Message.Should().Be("New ticket awaiting triage");
    }

    [Fact]
    public async Task NotifyTicketGroupAsync_SendsNamedEventToTicketRooms()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        await dispatcher.NotifyTicketGroupAsync(_ticketId, "TicketEscalated", new { Reason = "SLA Breach" });

        var ticketGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}"];
        ticketGroup.SentMessages.Should().Contain(m => m.Method == "TicketEscalated");
    }

    [Fact]
    public async Task DispatchDomainEventAsync_CommentAddedEvent_PublicComment_DispatchesToTicketGroup()
    {
        var (dispatcher, hubContext, db) = CreateDispatcher();

        var comment = Comment.Create(_ticketId, _creatorId, "Public message from user", isInternal: false).Value;
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var ev = new CommentAddedEvent(comment.Id, _ticketId, _creatorId, comment.Content, comment.IsInternal, DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        var ticketGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}"];
        ticketGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveComment");

        var dto = ticketGroup.SentMessages.First(m => m.Method == "ReceiveComment").Args[0] as CommentDto;
        dto!.Content.Should().Be("Public message from user");
        dto.IsInternal.Should().BeFalse();
        dto.AuthorName.Should().Be("Alice Creator");
    }

    [Fact]
    public async Task DispatchDomainEventAsync_CommentAddedEvent_InternalComment_DispatchesToInternalGroup()
    {
        var (dispatcher, hubContext, db) = CreateDispatcher();

        var comment = Comment.Create(_ticketId, _staffId, "Internal confidential technician note", isInternal: true).Value;
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var ev = new CommentAddedEvent(comment.Id, _ticketId, _staffId, comment.Content, comment.IsInternal, DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        // Internal room must receive
        var internalGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}_internal"];
        internalGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveComment");

        var dto = internalGroup.SentMessages.First(m => m.Method == "ReceiveComment").Args[0] as CommentDto;
        dto!.Content.Should().Be("Internal confidential technician note");
        dto.IsInternal.Should().BeTrue();

        // Public room must NOT receive internal comments
        hubContext.HubClients.GroupsMap.ContainsKey($"ticket_{_ticketId}").Should().BeFalse();
    }

    [Fact]
    public async Task DispatchDomainEventAsync_TicketStatusChangedEvent_SendsStatusDtoWithCanComment()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        // Transition from InProgress (3) to Resolved (5)
        var ev = new TicketStatusChangedEvent(_ticketId, TicketStatus.InProgress, TicketStatus.Resolved, _staffId, DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        var ticketGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}"];
        ticketGroup.SentMessages.Should().Contain(m => m.Method == "TicketStatusChanged");

        var payload = ticketGroup.SentMessages.First(m => m.Method == "TicketStatusChanged").Args[0] as TicketStatusChangedDto;
        payload.Should().NotBeNull();
        payload!.NewStatusId.Should().Be(TicketStatus.Resolved);
        payload.CanComment.Should().BeFalse("Resolved tickets must disable comments according to Table 7.2 TC-08");
    }

    [Fact]
    public async Task DispatchDomainEventAsync_TicketCreatedEvent_NotifiesLeaderQueue()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        var ev = new TicketCreatedEvent(_ticketId, "Critical Outage", _creatorId, DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        var leaderGroup = hubContext.HubClients.GroupsMap[$"role_{RoleNames.Leader}"];
        leaderGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");
        var notif = leaderGroup.SentMessages.First(m => m.Method == "ReceiveNotification").Args[0] as NotificationDto;
        notif!.Link.Should().Be("/Leader/Ticket/NewQueue");
        notif.Message.Should().Contain("Critical Outage");
    }

    [Fact]
    public async Task DispatchDomainEventAsync_TicketAssignedEvent_NotifiesBothStaffAndCreator()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        var ev = new TicketAssignedEvent(_ticketId, _staffId, _leaderId, DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        // Staff receives notification
        var staffGroup = hubContext.HubClients.GroupsMap[$"user_{_staffId}"];
        staffGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");

        // Creator receives notification
        var creatorGroup = hubContext.HubClients.GroupsMap[$"user_{_creatorId}"];
        creatorGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");
    }

    [Fact]
    public async Task DispatchDomainEventAsync_TicketClosedEvent_NotifiesAssignedStaffOfRating()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        var ev = new TicketClosedEvent(_ticketId, _creatorId, 5, "Excellent", DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        var staffGroup = hubContext.HubClients.GroupsMap[$"user_{_staffId}"];
        staffGroup.SentMessages.Should().Contain(m => m.Method == "ReceiveNotification");
        var notif = staffGroup.SentMessages.First(m => m.Method == "ReceiveNotification").Args[0] as NotificationDto;
        notif!.Message.Should().Contain("5 sao");
    }

    [Fact]
    public async Task DispatchDomainEventAsync_UserDeactivatedEvent_ForcesLogoutOnUserGroup()
    {
        var (dispatcher, hubContext, _) = CreateDispatcher();

        var ev = new UserDeactivatedEvent(_creatorId, _leaderId, "Policy violation", DateTime.UtcNow);

        await dispatcher.DispatchDomainEventAsync(ev);

        var userGroup = hubContext.HubClients.GroupsMap[$"user_{_creatorId}"];
        userGroup.SentMessages.Should().Contain(m => m.Method == "ForceLogout");
    }
}
