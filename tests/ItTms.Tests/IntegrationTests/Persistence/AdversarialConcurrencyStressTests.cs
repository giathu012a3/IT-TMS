using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;
using System.Collections.Concurrent;

namespace ItTms.Tests.IntegrationTests.Persistence;

/// <summary>
/// Adversarial Concurrency Stress Harness:
/// Stress-tests optimistic concurrency control, race conditions, and DbUpdateConcurrencyException
/// under simultaneous multi-context execution against SQLite.
/// </summary>
public class AdversarialConcurrencyStressTests : IDisposable
{
    private readonly string _dbFileName;
    private readonly DbContextOptions<ItTmsDbContext> _options;

    public AdversarialConcurrencyStressTests()
    {
        _dbFileName = $"test_adv_concurrency_{Guid.NewGuid():N}.db";
        var connectionString = $"Data Source={_dbFileName};Cache=Shared";

        var optionsBuilder = new DbContextOptionsBuilder<ItTmsDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        optionsBuilder.AddInterceptors(new ConcurrencyTokenInterceptor());

        _options = optionsBuilder.Options;

        // Khởi tạo schema và các trạng thái chuẩn
        using var initContext = new ItTmsDbContext(_options);
        initContext.Database.Migrate();
        initContext.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000;");

        initContext.TicketStatuses.AddRange(
            TicketStatus.Create(TicketStatus.New, "New", "Mới tạo", "badge bg-primary", 1),
            TicketStatus.Create(TicketStatus.Assigned, "Assigned", "Đã phân công", "badge bg-info", 2),
            TicketStatus.Create(TicketStatus.InProgress, "InProgress", "Đang xử lý", "badge bg-warning", 3),
            TicketStatus.Create(TicketStatus.Waiting, "Waiting", "Chờ phản hồi", "badge bg-secondary", 4),
            TicketStatus.Create(TicketStatus.Resolved, "Resolved", "Đã giải quyết", "badge bg-success", 5),
            TicketStatus.Create(TicketStatus.Closed, "Closed", "Đã đóng", "badge bg-dark", 6),
            TicketStatus.Create(TicketStatus.Rejected, "Rejected", "Đã từ chối", "badge bg-danger", 7)
        );

        var admin = User.Create("admin_test", "admin_test@local", "hash", "Admin", UserRole.Admin).Value;
        var leader = User.Create("leader_test", "leader_test@local", "hash", "Leader", UserRole.Leader).Value;
        var staff = User.Create("staff_test", "staff_test@local", "hash", "Staff", UserRole.Staff).Value;
        var user = User.Create("user_test", "user_test@local", "hash", "User", UserRole.User).Value;

        initContext.Users.AddRange(admin, leader, staff, user);
        initContext.SaveChanges();
    }

    [Fact]
    public async Task ConcurrentDisjointPropertyEdits_ThrowsDbUpdateConcurrencyException_AndPreservesRowVersionIntegrity()
    {
        // ARRANGE: Tạo ticket ban đầu
        int ticketId;
        Guid initialToken;
        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var ticket = Ticket.Create("Original Title", "Original Description", TicketPriority.Medium, TicketCategory.Software, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();

            ticketId = ticket.Id;
            initialToken = ticket.ConcurrencyToken;
        }

        // ACT: 2 DbContext riêng biệt đọc cùng 1 bản ghi
        using var contextA = new ItTmsDbContext(_options);
        using var contextB = new ItTmsDbContext(_options);

        var ticketA = await contextA.Tickets.FindAsync(ticketId);
        var ticketB = await contextB.Tickets.FindAsync(ticketId);

        ticketA.Should().NotBeNull();
        ticketB.Should().NotBeNull();
        ticketA!.ConcurrencyToken.Should().Be(initialToken);
        ticketB!.ConcurrencyToken.Should().Be(initialToken);

        // Context A cập nhật Title
        ticketA.Title = "Title Updated By Context A";
        await contextA.SaveChangesAsync();

        var tokenAfterA = ticketA.ConcurrencyToken;
        tokenAfterA.Should().NotBe(initialToken);

        // Context B cập nhật Description dựa trên token cũ
        ticketB.Description = "Description Updated By Context B";

        // ASSERT: Context B phải bị chặn bởi DbUpdateConcurrencyException
        var actB = async () => await contextB.SaveChangesAsync();
        await actB.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // Verify: Trong DB, dữ liệu của Context A được bảo toàn và ConcurrencyToken đã thay đổi
        using var verifyContext = new ItTmsDbContext(_options);
        var ticketInDb = await verifyContext.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        ticketInDb.Title.Should().Be("Title Updated By Context A");
        ticketInDb.Description.Should().Be("Original Description");
        ticketInDb.ConcurrencyToken.Should().Be(tokenAfterA);
    }

    [Fact]
    public async Task ConflictResolution_ReloadAndRetry_SucceedsWithNewConcurrencyToken()
    {
        // ARRANGE
        int ticketId;
        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var ticket = Ticket.Create("Conflict Retry Test", "Initial Desc", TicketPriority.High, TicketCategory.Hardware, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var context1 = new ItTmsDbContext(_options);
        using var context2 = new ItTmsDbContext(_options);

        var t1 = await context1.Tickets.FindAsync(ticketId);
        var t2 = await context2.Tickets.FindAsync(ticketId);

        // User 1 sửa và lưu
        t1!.Title = "Update 1";
        await context1.SaveChangesAsync();
        var token1 = t1.ConcurrencyToken;

        // User 2 sửa và lưu -> Xung đột
        t2!.Description = "Update 2";
        Func<Task> act = async () => await context2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // ACT: User 2 giải quyết xung đột bằng cách ReloadAsync(), áp dụng lại thay đổi
        var entry = context2.Entry(t2);
        await entry.ReloadAsync();

        // Sau reload, t2 phản ánh Title của User 1
        t2.Title.Should().Be("Update 1");
        t2.ConcurrencyToken.Should().Be(token1);

        // Áp dụng lại thay đổi của User 2
        t2.Description = "Update 2 resolved";
        await context2.SaveChangesAsync();

        var token2 = t2.ConcurrencyToken;
        token2.Should().NotBe(token1);

        // ASSERT: Dữ liệu cuối cùng kết hợp cả hai bản cập nhật hợp lệ
        using var finalContext = new ItTmsDbContext(_options);
        var finalTicket = await finalContext.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        finalTicket.Title.Should().Be("Update 1");
        finalTicket.Description.Should().Be("Update 2 resolved");
        finalTicket.ConcurrencyToken.Should().Be(token2);
    }

    [Fact]
    public async Task ConcurrentStateTransitionVsDirectEdit_ThrowsDbUpdateConcurrencyException()
    {
        // ARRANGE
        int ticketId;
        int leaderId;
        int staffId;
        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var leader = await setupContext.Users.FirstAsync(u => u.Role == UserRole.Leader);
            var staff = await setupContext.Users.FirstAsync(u => u.Role == UserRole.Staff);
            leaderId = leader.Id;
            staffId = staff.Id;

            var ticket = Ticket.Create("Transition Conflict Test", "Desc", TicketPriority.Medium, TicketCategory.Network, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var leaderContext = new ItTmsDbContext(_options);
        using var userContext = new ItTmsDbContext(_options);

        var leaderTicket = await leaderContext.Tickets.FindAsync(ticketId);
        var userTicket = await userContext.Tickets.FindAsync(ticketId);

        // Leader phân công vé (chuyển sang Assigned)
        var assignResult = leaderTicket!.Assign(staffId, leaderId);
        assignResult.IsSuccess.Should().BeTrue();
        await leaderContext.SaveChangesAsync();

        // User cố gắng chỉnh sửa Description trên phiên bản cũ
        userTicket!.Description = "User late description edit";
        Func<Task> act = async () => await userContext.SaveChangesAsync();

        // ASSERT: Bị chặn bởi ConcurrencyToken
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task ConcurrentUpdateAfterDelete_ThrowsDbUpdateConcurrencyException()
    {
        // ARRANGE
        int ticketId;
        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var ticket = Ticket.Create("Delete Race Test", "Desc", TicketPriority.Low, TicketCategory.Other, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var deleteContext = new ItTmsDbContext(_options);
        using var updateContext = new ItTmsDbContext(_options);

        var ticketToDelete = await deleteContext.Tickets.FindAsync(ticketId);
        var ticketToUpdate = await updateContext.Tickets.FindAsync(ticketId);

        // Delete context xóa vé
        deleteContext.Tickets.Remove(ticketToDelete!);
        await deleteContext.SaveChangesAsync();

        // Update context cố gắng cập nhật vé đã bị xóa
        ticketToUpdate!.Title = "Ghost Update";
        Func<Task> act = async () => await updateContext.SaveChangesAsync();

        // ASSERT: EF Core nhận diện 0 rows affected và ném DbUpdateConcurrencyException
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task HighConcurrencyRace_10ConcurrentTasks_ExactlyOneSucceedsPerRound()
    {
        // ARRANGE: Tạo 1 ticket
        int ticketId;
        Guid initialToken;
        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var ticket = Ticket.Create("Multi-Thread Race", "Original", TicketPriority.Critical, TicketCategory.Software, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
            initialToken = ticket.ConcurrencyToken;
        }

        const int concurrencyDegree = 10;
        var barrier = new Barrier(concurrencyDegree);
        var successCount = 0;
        var conflictCount = 0;
        var otherErrors = new ConcurrentBag<Exception>();

        // ACT: 10 tasks đồng thời đọc ticket ban đầu và cố gắng lưu cùng lúc
        var tasks = Enumerable.Range(1, concurrencyDegree).Select(workerIndex => Task.Run(async () =>
        {
            using var context = new ItTmsDbContext(_options);
            var ticket = await context.Tickets.FindAsync(ticketId);

            // Đồng bộ thời điểm ghi
            barrier.SignalAndWait();

            ticket!.Title = $"Update from Worker {workerIndex}";

            try
            {
                await context.SaveChangesAsync();
                Interlocked.Increment(ref successCount);
            }
            catch (DbUpdateConcurrencyException)
            {
                Interlocked.Increment(ref conflictCount);
            }
            catch (Exception ex)
            {
                otherErrors.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // ASSERT:
        otherErrors.Should().BeEmpty("Không được phát sinh lỗi khóa database (SQLite busy/locked) hoặc ngoại lệ không mong muốn");
        successCount.Should().Be(1, "Chỉ duy nhất 1 tác vụ được phép thành công trong cùng một thế hệ token");
        conflictCount.Should().Be(concurrencyDegree - 1, "Toàn bộ N-1 tác vụ còn lại phải nhận DbUpdateConcurrencyException");

        // Token trong DB đã được cập nhật
        using var finalContext = new ItTmsDbContext(_options);
        var finalTicket = await finalContext.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        finalTicket.ConcurrencyToken.Should().NotBe(initialToken);
    }

    [Theory]
    [InlineData("Assign")]
    [InlineData("StartProgress")]
    [InlineData("PutOnHold")]
    [InlineData("Resolve")]
    [InlineData("Close")]
    [InlineData("Reopen")]
    [InlineData("Reject")]
    [InlineData("PropertyChange")]
    public async Task EveryModificationPath_RenewsConcurrencyTokenAndUpdatedAt(string action)
    {
        // ARRANGE
        int ticketId;
        int leaderId;
        int staffId;
        int creatorId;

        using (var setupContext = new ItTmsDbContext(_options))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var leader = await setupContext.Users.FirstAsync(u => u.Role == UserRole.Leader);
            var staff = await setupContext.Users.FirstAsync(u => u.Role == UserRole.Staff);
            creatorId = user.Id;
            leaderId = leader.Id;
            staffId = staff.Id;

            var ticket = Ticket.Create("Token Renewal Invariant Test", "Desc", TicketPriority.Medium, TicketCategory.Software, creatorId).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var actContext = new ItTmsDbContext(_options);
        var t = await actContext.Tickets.FindAsync(ticketId);
        t.Should().NotBeNull();
        var initialToken = t!.ConcurrencyToken;
        var initialUpdatedAt = t.UpdatedAt;

        // Chờ ngắn để bảo đảm timestamp UpdatedAt dịch chuyển
        await Task.Delay(15);

        // ACT
        switch (action)
        {
            case "Assign":
                t.Assign(staffId, leaderId);
                break;
            case "StartProgress":
                t.Assign(staffId, leaderId);
                await actContext.SaveChangesAsync();
                await Task.Delay(15);
                t.StartProgress(staffId);
                break;
            case "PutOnHold":
                t.Assign(staffId, leaderId);
                t.StartProgress(staffId);
                await actContext.SaveChangesAsync();
                await Task.Delay(15);
                t.PutOnHold(staffId);
                break;
            case "Resolve":
                t.Assign(staffId, leaderId);
                t.StartProgress(staffId);
                await actContext.SaveChangesAsync();
                await Task.Delay(15);
                t.Resolve(staffId);
                break;
            case "Close":
                t.Assign(staffId, leaderId);
                t.StartProgress(staffId);
                t.Resolve(staffId);
                await actContext.SaveChangesAsync();
                await Task.Delay(15);
                t.Close(creatorId);
                break;
            case "Reopen":
                t.Assign(staffId, leaderId);
                t.StartProgress(staffId);
                t.Resolve(staffId);
                await actContext.SaveChangesAsync();
                await Task.Delay(15);
                t.Reopen(creatorId);
                break;
            case "Reject":
                t.Reject("Không thuộc thẩm quyền", leaderId);
                break;
            case "PropertyChange":
                t.Title = "Altered Title Direct";
                break;
        }

        await actContext.SaveChangesAsync();

        // ASSERT: ConcurrencyToken phải đổi mới, UpdatedAt phải lớn hơn hoặc bằng
        t.ConcurrencyToken.Should().NotBe(initialToken, $"Hành động '{action}' phải cấp mới ConcurrencyToken");
        t.UpdatedAt.Should().BeOnOrAfter(initialUpdatedAt);

        // Kiểm chứng trực tiếp trong CSDL
        using var verifyContext = new ItTmsDbContext(_options);
        var dbTicket = await verifyContext.Tickets.AsNoTracking().FirstAsync(x => x.Id == ticketId);
        dbTicket.ConcurrencyToken.Should().Be(t.ConcurrencyToken);
    }

    public void Dispose()
    {
        using (var cleanupContext = new ItTmsDbContext(_options))
        {
            cleanupContext.Database.EnsureDeleted();
        }

        if (File.Exists(_dbFileName))
        {
            try { File.Delete(_dbFileName); } catch { /* ignore */ }
        }

        GC.SuppressFinalize(this);
    }
}
