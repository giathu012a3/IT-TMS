using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Infrastructure.Persistence;
using ItTms.Web.Hubs;
using ItTms.Web.Services;
using Xunit;

namespace ItTms.Tests.SignalR;

public class SignalRConcurrencyAndStressTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    private readonly int _creatorId;
    private readonly int _staffId;
    private readonly int _leaderId;
    private readonly int _ticketId;

    public SignalRConcurrencyAndStressTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Mode=Memory;Cache=Shared");
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

        var u = User.Create("stressuser", "stress@ittms.com", "hash", "Stress User", UserRole.User).Value;
        var s = User.Create("stressstaff", "stressstaff@ittms.com", "hash", "Stress Staff", UserRole.Staff).Value;
        var l = User.Create("stressleader", "stressleader@ittms.com", "hash", "Stress Leader", UserRole.Leader).Value;

        ctx.Users.AddRange(u, s, l);
        ctx.SaveChanges();

        _creatorId = u.Id;
        _staffId = s.Id;
        _leaderId = l.Id;

        var ticket = Ticket.Create("Stress Ticket", "Stress Details", TicketPriority.High, TicketCategory.Network, _creatorId).Value;
        ticket.Assign(_staffId, _leaderId);
        ctx.Tickets.Add(ticket);
        ctx.SaveChanges();

        _ticketId = ticket.Id;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private static TicketHub CreateHub(
        string connectionId,
        int userId, 
        string role, 
        TestGroupManager groupManager, 
        TestHubCallerClients callerClients,
        ItTmsDbContext dbContext)
    {
        var loggerMock = new Mock<ILogger<TicketHub>>();
        var hub = new TicketHub(dbContext, loggerMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, $"user_{userId}"),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns(connectionId);
        contextMock.Setup(c => c.User).Returns(principal);

        hub.Context = contextMock.Object;
        hub.Groups = groupManager;
        hub.Clients = callerClients;

        return hub;
    }

    [Fact]
    public async Task ConcurrencyStress_50ClientsConnectingAndJoiningSimultaneously()
    {
        const int clientCount = 50;
        var sharedGroupManager = new TestGroupManager();
        var sharedClients = new TestHubCallerClients();

        var tasks = Enumerable.Range(1, clientCount).Select(async i =>
        {
            var connId = $"conn_stress_{i}";
            using var scopedCtx = new ItTmsDbContext(_dbOptions);
            var hub = CreateHub(connId, _creatorId, RoleNames.User, sharedGroupManager, sharedClients, scopedCtx);

            await hub.OnConnectedAsync();
            await hub.JoinTicketGroup(_ticketId);
            await hub.LeaveTicketGroup(_ticketId);
        });

        await Task.WhenAll(tasks);

        // Verify all 50 joined user and role groups
        sharedGroupManager.Added.Count(x => x.GroupName == $"user_{_creatorId}").Should().Be(clientCount);
        sharedGroupManager.Added.Count(x => x.GroupName == $"ticket_{_ticketId}").Should().Be(clientCount);
        sharedGroupManager.Removed.Count(x => x.GroupName == $"ticket_{_ticketId}").Should().Be(clientCount);
    }

    [Fact]
    public async Task ConcurrencyStress_ConcurrentComments_DispatchedWithoutDeadlock()
    {
        const int commentCount = 20;
        var sharedGroupManager = new TestGroupManager();
        var sharedClients = new TestHubCallerClients();

        // In real ASP.NET Core, each request/invocation has its own DbContext instance
        var tasks = Enumerable.Range(1, commentCount).Select(async i =>
        {
            var connId = $"conn_comment_{i}";
            using var scopedCtx = new ItTmsDbContext(_dbOptions);
            var hub = CreateHub(connId, _creatorId, RoleNames.User, sharedGroupManager, sharedClients, scopedCtx);

            await hub.SendComment(_ticketId, $"Concurrent comment #{i}");
        });

        await Task.WhenAll(tasks);

        // Verify in DB that all comments were persisted
        using var verifyCtx = new ItTmsDbContext(_dbOptions);
        var totalComments = await verifyCtx.Comments.CountAsync(c => c.TicketId == _ticketId);
        totalComments.Should().Be(commentCount);

        // Verify shared clients received all broadcasts
        var groupProxy = sharedClients.GroupsMap[$"ticket_{_ticketId}"];
        groupProxy.SentMessages.Count(m => m.Method == "ReceiveComment").Should().Be(commentCount);
    }

    [Fact]
    public async Task ConcurrencyStress_ConcurrentDomainEventDispatching()
    {
        const int eventCount = 30;
        var hubContext = new TestHubContext();
        var loggerMock = new Mock<ILogger<SignalRNotificationDispatcher>>();

        var tasks = Enumerable.Range(1, eventCount).Select(async i =>
        {
            using var scopedCtx = new ItTmsDbContext(_dbOptions);
            var dispatcher = new SignalRNotificationDispatcher(hubContext, scopedCtx, loggerMock.Object);

            var ev = new TicketStatusChangedEvent(_ticketId, TicketStatus.InProgress, TicketStatus.Waiting, _staffId, DateTime.UtcNow);
            await dispatcher.DispatchDomainEventAsync(ev);
        });

        await Task.WhenAll(tasks);

        var ticketGroup = hubContext.HubClients.GroupsMap[$"ticket_{_ticketId}"];
        ticketGroup.SentMessages.Count(m => m.Method == "TicketStatusChanged").Should().Be(eventCount);
    }
}
