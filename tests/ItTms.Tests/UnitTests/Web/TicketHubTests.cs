namespace ItTms.Tests.UnitTests.Web;

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
using ItTms.Infrastructure.Persistence;
using ItTms.Web.Hubs;
using Xunit;

public class TicketHubTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    private readonly int _creatorId;
    private readonly int _otherUserId;
    private readonly int _assignedStaffId;
    private readonly int _unassignedStaffId;
    private readonly int _leaderId;
    private readonly int _adminId;
    private readonly int _ticketId;

    public TicketHubTests()
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

        var u1 = User.Create("creator", "creator@ittms.com", "hash", "Creator User", UserRole.User).Value;
        var u2 = User.Create("otheruser", "other@ittms.com", "hash", "Other User", UserRole.User).Value;
        var s1 = User.Create("staff1", "staff1@ittms.com", "hash", "Assigned Staff", UserRole.Staff).Value;
        var s2 = User.Create("staff2", "staff2@ittms.com", "hash", "Unassigned Staff", UserRole.Staff).Value;
        var l1 = User.Create("leader1", "leader1@ittms.com", "hash", "Leader", UserRole.Leader).Value;
        var a1 = User.Create("admin1", "admin1@ittms.com", "hash", "Admin", UserRole.Admin).Value;

        ctx.Users.AddRange(u1, u2, s1, s2, l1, a1);
        ctx.SaveChanges();

        _creatorId = u1.Id;
        _otherUserId = u2.Id;
        _assignedStaffId = s1.Id;
        _unassignedStaffId = s2.Id;
        _leaderId = l1.Id;
        _adminId = a1.Id;

        var ticket = Ticket.Create("Test Issue", "Details", TicketPriority.High, TicketCategory.Network, _creatorId).Value;
        ticket.Assign(_assignedStaffId, _leaderId);
        ctx.Tickets.Add(ticket);
        ctx.SaveChanges();

        _ticketId = ticket.Id;
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private TicketHub CreateHub(int userId, string role, out Mock<IGroupManager> groupsMock)
    {
        var db = new ItTmsDbContext(_dbOptions);
        var loggerMock = new Mock<ILogger<TicketHub>>();
        var hub = new TicketHub(db, loggerMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, $"user_{userId}"),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns("conn-123");
        contextMock.Setup(c => c.User).Returns(principal);

        groupsMock = new Mock<IGroupManager>();
        groupsMock.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        hub.Context = contextMock.Object;
        hub.Groups = groupsMock.Object;

        return hub;
    }

    [Fact]
    public async Task JoinTicketGroup_WhenCreatorJoins_Succeeds()
    {
        // Arrange
        var hub = CreateHub(_creatorId, RoleNames.User, out var groupsMock);

        // Act
        await hub.JoinTicketGroup(_ticketId);

        // Assert: added to ticket_{ticketId} group
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", $"ticket_{_ticketId}", default), Times.Once);
    }

    [Fact]
    public async Task JoinTicketGroup_WhenUnrelatedUserJoins_ThrowsHubException()
    {
        // Arrange: Other user is NOT creator of the ticket
        var hub = CreateHub(_otherUserId, RoleNames.User, out _);

        // Act & Assert (BOLA defense)
        var act = async () => await hub.JoinTicketGroup(_ticketId);
        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*không có quyền truy cập*");
    }

    [Fact]
    public async Task JoinTicketGroup_WhenAssignedStaffJoins_SucceedsAndJoinsInternalGroup()
    {
        // Arrange: Staff assigned to the ticket
        var hub = CreateHub(_assignedStaffId, RoleNames.Staff, out var groupsMock);

        // Act
        await hub.JoinTicketGroup(_ticketId);

        // Assert: added to both ticket and internal room
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", $"ticket_{_ticketId}", default), Times.Once);
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", $"ticket_{_ticketId}_internal", default), Times.Once);
    }

    [Fact]
    public async Task JoinTicketGroup_WhenUnassignedStaffJoins_ThrowsHubException()
    {
        // Arrange: Staff NOT assigned to the ticket
        var hub = CreateHub(_unassignedStaffId, RoleNames.Staff, out _);

        // Act & Assert (Table 7.2 TC-07)
        var act = async () => await hub.JoinTicketGroup(_ticketId);
        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*không có quyền truy cập*");
    }

    [Fact]
    public async Task JoinTicketGroup_WhenLeaderJoins_Succeeds()
    {
        // Arrange: Leader can monitor all tickets
        var hub = CreateHub(_leaderId, RoleNames.Leader, out var groupsMock);

        // Act
        await hub.JoinTicketGroup(_ticketId);

        // Assert
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", $"ticket_{_ticketId}", default), Times.Once);
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", $"ticket_{_ticketId}_internal", default), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_RegistersUserAndRoleGroups()
    {
        // Arrange
        var hub = CreateHub(_creatorId, RoleNames.User, out var groupsMock);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", $"user_{_creatorId}", default), Times.Once);
        groupsMock.Verify(g => g.AddToGroupAsync("conn-123", "role_User", default), Times.Once);
    }
}
