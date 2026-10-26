namespace ItTms.Tests.UnitTests.Observers;

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ItTms.Application.Common.Interfaces;
using ItTms.Application.Observers;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Infrastructure.Events;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Realtime;
using Xunit;

public class ObserverPatternTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    public ObserverPatternTests()
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
    public async Task NotificationObserver_OnTicketCreated_NotifiesAllActiveLeaders_Table72TC03()
    {
        // Arrange
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var leader1 = User.Create("leader1", "l1@local.test", "hash", "Leader 1", UserRole.Leader, status: UserStatus.Active).Value;
            var leader2 = User.Create("leader2", "l2@local.test", "hash", "Leader 2", UserRole.Leader, status: UserStatus.Active).Value;
            var inactiveLeader = User.Create("leader_inactive", "li@local.test", "hash", "Inactive Leader", UserRole.Leader, status: UserStatus.Inactive).Value;
            var normalUser = User.Create("user1", "u1@local.test", "hash", "User 1", UserRole.User, status: UserStatus.Active).Value;

            ctx.Users.AddRange(leader1, leader2, inactiveLeader, normalUser);
            await ctx.SaveChangesAsync();
        }

        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var observer = new NotificationObserver(ctx);
            var domainEvent = new TicketCreatedEvent(TicketId: 101, Title: "Máy in hỏng", CreatorId: 4, OccurredOn: DateTime.UtcNow);

            // Act
            await observer.HandleAsync(domainEvent);
            await ctx.SaveChangesAsync();
        }

        // Assert: 2 active leaders nhận được thông báo, inactive leader và normal user không nhận
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var notifications = await ctx.Notifications.ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Select(n => n.UserId).Should().Contain([1, 2]);
            notifications.All(n => n.Message.Contains("Máy in hỏng")).Should().BeTrue();
        }
    }

    [Fact]
    public async Task NotificationObserver_OnTicketAssigned_NotifiesStaffAndUser_Table72TC04()
    {
        // Arrange
        int ticketId;
        int creatorId;
        int staffId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("creator", "creator@local.test", "hash", "Creator", UserRole.User).Value;
            var staff = User.Create("staff", "staff@local.test", "hash", "Staff", UserRole.Staff).Value;
            var leader = User.Create("leader_assigner", "la@local.test", "hash", "Leader", UserRole.Leader).Value;
            ctx.Users.AddRange(user, staff, leader);
            await ctx.SaveChangesAsync();
            creatorId = user.Id;
            staffId = staff.Id;

            var ticket = Ticket.Create("Lỗi phần mềm", "Desc", TicketPriority.High, TicketCategory.Software, creatorId).Value;
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var observer = new NotificationObserver(ctx);
            var domainEvent = new TicketAssignedEvent(TicketId: ticketId, AssignedToId: staffId, LeaderId: 3, OccurredOn: DateTime.UtcNow);

            // Act
            await observer.HandleAsync(domainEvent);
            await ctx.SaveChangesAsync();
        }

        // Assert: Cả Staff và User đều nhận thông báo
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var notifications = await ctx.Notifications.ToListAsync();
            notifications.Should().HaveCount(2);
            notifications.Select(n => n.UserId).Should().Contain([creatorId, staffId]);
        }
    }

    [Fact]
    public async Task NotificationObserver_OnCommentAdded_DoesNotNotifyUser_WhenInternalNote()
    {
        // Arrange
        int ticketId;
        int creatorId;
        int staffId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("creator", "c@local.test", "hash", "Creator", UserRole.User).Value;
            var staff = User.Create("staff", "s@local.test", "hash", "Staff", UserRole.Staff).Value;
            ctx.Users.AddRange(user, staff);
            await ctx.SaveChangesAsync();
            creatorId = user.Id;
            staffId = staff.Id;

            var ticket = Ticket.Create("Title", "Desc", TicketPriority.Medium, TicketCategory.Hardware, creatorId).Value;
            ticket.Assign(staffId, leaderId: user.Id);
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var observer = new NotificationObserver(ctx);
            // Ghi chú nội bộ: IsInternal = true
            var domainEvent = new CommentAddedEvent(
                CommentId: 1,
                TicketId: ticketId,
                AuthorUserId: staffId,
                Content: "Mã bí mật nội bộ",
                IsInternal: true,
                OccurredOn: DateTime.UtcNow);

            // Act
            await observer.HandleAsync(domainEvent);
            await ctx.SaveChangesAsync();
        }

        // Assert: Không có thông báo nào được gửi khi là internal note
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var notifications = await ctx.Notifications.ToListAsync();
            notifications.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task SystemLogObserver_OnUserDeactivated_RecordsImmutableAuditLog()
    {
        // Arrange
        int adminId;
        int targetUserId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var admin = User.Create("admin_actor", "admin@local.test", "hash", "Admin", UserRole.Admin).Value;
            var targetUser = User.Create("target_user", "target@local.test", "hash", "Target", UserRole.User).Value;
            ctx.Users.AddRange(admin, targetUser);
            await ctx.SaveChangesAsync();
            adminId = admin.Id;
            targetUserId = targetUser.Id;
        }

        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var observer = new SystemLogObserver(ctx);
            var domainEvent = new UserDeactivatedEvent(
                UserId: targetUserId,
                DeactivatedById: adminId,
                Reason: "Vi phạm chính sách",
                OccurredOn: DateTime.UtcNow);

            // Act
            await observer.HandleAsync(domainEvent);
            await ctx.SaveChangesAsync();
        }

        // Assert
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var log = await ctx.SystemLogs.FirstOrDefaultAsync(l => l.Action == "DeactivateUser");
            log.Should().NotBeNull();
            log!.UserId.Should().Be(adminId);
            log.Details.Should().Contain($"User #{targetUserId}");
        }
    }

    [Fact]
    public async Task TwoPhaseDispatching_ViaDbContext_ExecutesInTransactionAndCommitsBoth()
    {
        // Arrange: Cấu hình ServiceProvider với Scoped DbContext và Two-Phase Dispatcher
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ItTmsDbContext>((sp, options) =>
        {
            options.UseSqlite(_connection);
        });
        services.AddScoped<IItTmsDbContext>(sp => sp.GetRequiredService<ItTmsDbContext>());
        services.AddScoped<NotificationObserver>();
        services.AddScoped<SystemLogObserver>();

        services.AddScoped<IDomainEventObserver<TicketCreatedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
        services.AddScoped<IDomainEventObserver<TicketCreatedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());

        services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
        services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();

        var provider = services.BuildServiceProvider();

        // Tạo sẵn Leader và User trong CSDL
        int creatorId;
        using (var setupScope = provider.CreateScope())
        {
            var setupCtx = setupScope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var leader = User.Create("lead", "lead@local.test", "hash", "Leader", UserRole.Leader).Value;
            var creator = User.Create("creator_user", "cu@local.test", "hash", "Creator", UserRole.User).Value;
            setupCtx.Users.AddRange(leader, creator);
            await setupCtx.SaveChangesAsync();
            creatorId = creator.Id;
        }

        // Act: Lưu Ticket có DomainEvent qua Scoped DbContext đã tích hợp Dispatcher
        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ItTmsDbContext>();
            var ticket = Ticket.Create("Cần cấp màn hình mới", "Màn hình bị sọc", TicketPriority.Medium, TicketCategory.Hardware, creatorId: creatorId).Value;
            ctx.Tickets.Add(ticket);

            // Khi gọi SaveChangesAsync, Pha 1 được kích hoạt tự động với cùng Scoped DbContext
            await ctx.SaveChangesAsync();
        }

        // Assert: Cả Ticket, Notification (cho Leader) và SystemLog đều được lưu trong 1 Transaction nguyên tử
        using (var verifyCtx = new ItTmsDbContext(_dbOptions))
        {
            var tickets = await verifyCtx.Tickets.ToListAsync();
            tickets.Should().HaveCount(1);

            var notifications = await verifyCtx.Notifications.ToListAsync();
            notifications.Should().HaveCount(1);
            notifications[0].Message.Should().Contain("Cần cấp màn hình mới");

            var logs = await verifyCtx.SystemLogs.ToListAsync();
            logs.Should().ContainSingle(l => l.Action == "CreateTicket");
        }
    }
}
