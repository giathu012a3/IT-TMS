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
using ItTms.Web.Models.Realtime;
using Xunit;

namespace ItTms.Tests.SignalR;

public class TicketHubLifecycleAndBolaAdversarialTests : IDisposable
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

    public TicketHubLifecycleAndBolaAdversarialTests()
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

        var ticket = Ticket.Create("Network Outage", "Printer connection lost", TicketPriority.High, TicketCategory.Network, _creatorId).Value;
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

    private TicketHub CreateHub(
        int? userId, 
        string? role, 
        out TestGroupManager groupManager, 
        out TestHubCallerClients callerClients,
        ItTmsDbContext? customCtx = null)
    {
        var db = customCtx ?? new ItTmsDbContext(_dbOptions);
        var loggerMock = new Mock<ILogger<TicketHub>>();
        var hub = new TicketHub(db, loggerMock.Object);

        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns("conn-adv-999");

        if (userId.HasValue && !string.IsNullOrEmpty(role))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, $"user_{userId.Value}"),
                new(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            contextMock.Setup(c => c.User).Returns(new ClaimsPrincipal(identity));
        }
        else
        {
            contextMock.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
        }

        groupManager = new TestGroupManager();
        callerClients = new TestHubCallerClients();

        hub.Context = contextMock.Object;
        hub.Groups = groupManager;
        hub.Clients = callerClients;

        return hub;
    }

    #region 1. Connection Lifecycle Verification

    [Fact]
    public async Task OnConnectedAsync_AuthenticatedUser_AddsToUserAndRoleGroups()
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out var groups, out _);

        await hub.OnConnectedAsync();

        groups.Added.Should().Contain(x => x.ConnectionId == "conn-adv-999" && x.GroupName == $"user_{_creatorId}");
        groups.Added.Should().Contain(x => x.ConnectionId == "conn-adv-999" && x.GroupName == "role_User");
    }

    [Fact]
    public async Task OnConnectedAsync_UnauthenticatedClient_DoesNotAddToGroups()
    {
        var hub = CreateHub(null, null, out var groups, out _);

        await hub.OnConnectedAsync();

        groups.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task OnDisconnectedAsync_GracefullyCompletesWithoutError()
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out _, out _);

        var act = async () => await hub.OnDisconnectedAsync(new InvalidOperationException("Client network drop"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task JoinTicketGroup_Creator_JoinsPublicTicketGroups()
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out var groups, out _);

        await hub.JoinTicketGroup(_ticketId);

        groups.Added.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}");
        groups.Added.Should().Contain(x => x.GroupName == $"Ticket_{_ticketId}");
        groups.Added.Should().NotContain(x => x.GroupName.Contains("internal"));
    }

    [Fact]
    public async Task JoinTicketGroup_AssignedStaff_JoinsBothPublicAndInternalGroups()
    {
        var hub = CreateHub(_assignedStaffId, RoleNames.Staff, out var groups, out _);

        await hub.JoinTicketGroup(_ticketId);

        groups.Added.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}");
        groups.Added.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}_internal");
    }

    [Fact]
    public async Task JoinTicketGroup_LeaderAndAdmin_JoinsBothPublicAndInternalGroups()
    {
        var leaderHub = CreateHub(_leaderId, RoleNames.Leader, out var leaderGroups, out _);
        await leaderHub.JoinTicketGroup(_ticketId);
        leaderGroups.Added.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}_internal");

        var adminHub = CreateHub(_adminId, RoleNames.Admin, out var adminGroups, out _);
        await adminHub.JoinTicketGroup(_ticketId);
        adminGroups.Added.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}_internal");
    }

    [Fact]
    public async Task LeaveTicketGroup_RemovesClientFromAllRelatedGroups()
    {
        var hub = CreateHub(_assignedStaffId, RoleNames.Staff, out var groups, out _);

        await hub.LeaveTicketGroup(_ticketId);

        groups.Removed.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}");
        groups.Removed.Should().Contain(x => x.GroupName == $"Ticket_{_ticketId}");
        groups.Removed.Should().Contain(x => x.GroupName == $"ticket_{_ticketId}_internal");
        groups.Removed.Should().Contain(x => x.GroupName == $"Ticket_{_ticketId}_internal");
    }

    #endregion

    #region 2. BOLA / IDOR Verification on JoinTicketGroup

    [Fact]
    public async Task JoinTicketGroup_UnrelatedUser_ThrowsHubException_BOLA()
    {
        var hub = CreateHub(_otherUserId, RoleNames.User, out _, out _);

        var act = async () => await hub.JoinTicketGroup(_ticketId);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*không có quyền truy cập*");
    }

    [Fact]
    public async Task JoinTicketGroup_UnassignedStaff_ThrowsHubException_BOLA()
    {
        var hub = CreateHub(_unassignedStaffId, RoleNames.Staff, out _, out _);

        var act = async () => await hub.JoinTicketGroup(_ticketId);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*không có quyền truy cập*");
    }

    [Fact]
    public async Task JoinTicketGroup_NonExistentTicket_ThrowsHubException()
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out _, out _);

        var act = async () => await hub.JoinTicketGroup(999999);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*Không tìm thấy*");
    }

    [Fact]
    public async Task JoinTicketGroup_UnauthenticatedClient_ThrowsHubException()
    {
        var hub = CreateHub(null, null, out _, out _);

        var act = async () => await hub.JoinTicketGroup(_ticketId);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*xác thực*");
    }

    #endregion

    #region 3. BOLA / IDOR & State Verification on SendComment

    [Fact]
    public async Task SendComment_Creator_SucceedsAndBroadcastsToPublicGroup()
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out _, out var clients);

        await hub.SendComment(_ticketId, "Can you check the network cable?");

        // Verify broadcasted to ticket group
        clients.GroupsMap.Should().ContainKey($"ticket_{_ticketId}");
        var groupProxy = clients.GroupsMap[$"ticket_{_ticketId}"];
        groupProxy.SentMessages.Should().Contain(m => m.Method == "ReceiveComment");

        var payload = groupProxy.SentMessages.First(m => m.Method == "ReceiveComment").Args[0] as CommentDto;
        payload.Should().NotBeNull();
        payload!.Content.Should().Be("Can you check the network cable?");
        payload.TicketId.Should().Be(_ticketId);
        payload.IsInternal.Should().BeFalse();

        // Verify stored in DB
        using var verifyCtx = new ItTmsDbContext(_dbOptions);
        var commentInDb = await verifyCtx.Comments.FirstOrDefaultAsync(c => c.TicketId == _ticketId && c.UserId == _creatorId);
        commentInDb.Should().NotBeNull();
        commentInDb!.Content.Should().Be("Can you check the network cable?");
    }

    [Fact]
    public async Task SendComment_AssignedStaff_CanSendInternalComment()
    {
        var hub = CreateHub(_assignedStaffId, RoleNames.Staff, out _, out var clients);

        await hub.SendComment(_ticketId, "Internal investigation note: switch port 4 failed", isInternal: true);

        clients.GroupsMap.Should().ContainKey($"ticket_{_ticketId}_internal");
        var internalProxy = clients.GroupsMap[$"ticket_{_ticketId}_internal"];
        internalProxy.SentMessages.Should().Contain(m => m.Method == "ReceiveComment");

        var payload = internalProxy.SentMessages.First(m => m.Method == "ReceiveComment").Args[0] as CommentDto;
        payload.Should().NotBeNull();
        payload!.IsInternal.Should().BeTrue();
    }

    [Fact]
    public async Task SendComment_RegularUser_FlaggedAsInternal_IsDowngradedToPublic()
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out _, out var clients);

        // Regular user attempts to set isInternal = true
        await hub.SendComment(_ticketId, "User comment attempting internal flag", isInternal: true);

        // Must be downgraded to public group, NOT sent to internal group
        clients.GroupsMap.Should().ContainKey($"ticket_{_ticketId}");
        var publicProxy = clients.GroupsMap[$"ticket_{_ticketId}"];
        var payload = publicProxy.SentMessages.First(m => m.Method == "ReceiveComment").Args[0] as CommentDto;
        payload!.IsInternal.Should().BeFalse();
    }

    [Fact]
    public async Task SendComment_UnrelatedUser_ThrowsHubException_BOLA()
    {
        var hub = CreateHub(_otherUserId, RoleNames.User, out _, out _);

        var act = async () => await hub.SendComment(_ticketId, "Adversarial comment by unauthorized user");

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*Bạn không phải người tạo yêu cầu này*");
    }

    /// <summary>
    /// ADVERSARIAL CHALLENGE TEST:
    /// In TicketHub.cs:
    /// `bool isStaffOrLeaderOrAdmin = role == RoleNames.Staff || ...`
    /// `if (role == RoleNames.Staff && ticket.AssignedToId != currentUserId && !isStaffOrLeaderOrAdmin)`
    /// Because !isStaffOrLeaderOrAdmin is FALSE when role is Staff, unassigned staff is NOT blocked!
    /// This test empirically exercises this exact path to expose the BOLA vulnerability.
    /// </summary>
    [Fact]
    public async Task SendComment_UnassignedStaff_MustThrowHubException_BOLA()
    {
        var hub = CreateHub(_unassignedStaffId, RoleNames.Staff, out _, out _);

        var act = async () => await hub.SendComment(_ticketId, "Adversarial comment by unassigned staff");

        // Per Table 7.2 TC-07 and BOLA security invariant, an unassigned staff MUST be blocked from commenting!
        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*không được phân công*");
    }

    [Fact]
    public async Task SendComment_OnResolvedTicket_ThrowsHubException_Table72TC08()
    {
        using (var setupCtx = new ItTmsDbContext(_dbOptions))
        {
            var t = await setupCtx.Tickets.FirstAsync(x => x.Id == _ticketId);
            t.StartProgress(_assignedStaffId);
            t.Resolve(_assignedStaffId);
            await setupCtx.SaveChangesAsync();
        }

        var hub = CreateHub(_creatorId, RoleNames.User, out _, out _);

        var act = async () => await hub.SendComment(_ticketId, "Trying to comment after resolved");

        // Table 7.2 TC-08: Comments on a Resolved ticket are blocked
        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*đã hoàn tất hoặc bị khóa, không thể gửi thêm bình luận*");
    }

    [Fact]
    public async Task SendComment_OnClosedTicket_ThrowsHubException_Table72TC08()
    {
        using (var setupCtx = new ItTmsDbContext(_dbOptions))
        {
            var t = await setupCtx.Tickets.FirstAsync(x => x.Id == _ticketId);
            t.StartProgress(_assignedStaffId);
            t.Resolve(_assignedStaffId);
            t.Close(_creatorId);
            await setupCtx.SaveChangesAsync();
        }

        var hub = CreateHub(_creatorId, RoleNames.User, out _, out _);

        var act = async () => await hub.SendComment(_ticketId, "Trying to comment after closed");

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*đã hoàn tất hoặc bị khóa, không thể gửi thêm bình luận*");
    }

    [Fact]
    public async Task SendComment_OnRejectedTicket_ThrowsHubException_Table72TC08()
    {
        int rejectedTicketId;
        using (var setupCtx = new ItTmsDbContext(_dbOptions))
        {
            var t = Ticket.Create("Bad Request", "Spam issue", TicketPriority.Low, TicketCategory.Other, _creatorId).Value;
            t.Reject("Out of scope", _leaderId);
            setupCtx.Tickets.Add(t);
            await setupCtx.SaveChangesAsync();
            rejectedTicketId = t.Id;
        }

        var hub = CreateHub(_creatorId, RoleNames.User, out _, out _);

        var act = async () => await hub.SendComment(rejectedTicketId, "Trying to comment after rejected");

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("*đã hoàn tất hoặc bị khóa, không thể gửi thêm bình luận*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendComment_EmptyOrWhitespaceMessage_ThrowsHubException(string emptyMessage)
    {
        var hub = CreateHub(_creatorId, RoleNames.User, out _, out _);

        var act = async () => await hub.SendComment(_ticketId, emptyMessage);

        await act.Should().ThrowAsync<HubException>();
    }

    #endregion
}
