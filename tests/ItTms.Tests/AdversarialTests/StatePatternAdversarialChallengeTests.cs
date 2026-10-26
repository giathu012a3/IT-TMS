namespace ItTms.Tests.AdversarialTests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Domain.States;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// ADVERSARIAL STRESS & EMPIRICAL CHALLENGE HARNESS FOR STATE PATTERN
/// Authored by challenger_m2_1.
/// 
/// Systematically verifies:
/// 1. Exhaustive 49 state permutations (7 states x 7 actions) with strict invariant immutability checks on failure.
/// 2. Boundary edge cases: Reopening closed tickets, rejecting in non-New states, invalid/null feedback, unauthorized actors.
/// 3. Invariant conservation: CanComment truth table, ConcurrencyToken isolation, zero ghost events on rejection.
/// 4. High-concurrency race conditions: Multi-threaded SQLite execution with Barrier synchronization and Optimistic Concurrency validation.
/// 5. Property-based randomized fuzzing across 1,000 state transitions.
/// </summary>
public class StatePatternAdversarialChallengeTests : IDisposable
{
    private const int CreatorId = 10;
    private const int AssignedStaffId = 5;
    private const int OtherStaffId = 88;
    private const int LeaderId = 2;
    private const int OtherUserId = 99;

    private readonly string _dbFileName;
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    public StatePatternAdversarialChallengeTests()
    {
        _dbFileName = $"adv_state_challenge_{Guid.NewGuid():N}.db";
        var connectionString = $"Data Source={_dbFileName};Cache=Shared";

        var builder = new DbContextOptionsBuilder<ItTmsDbContext>();
        builder.UseSqlite(connectionString);
        builder.AddInterceptors(new ConcurrencyTokenInterceptor());
        _dbOptions = builder.Options;

        using var initContext = new ItTmsDbContext(_dbOptions);
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

        var leader = User.Create("leader_adv", "leader_adv@local", "hash", "Leader", UserRole.Leader).Value;
        var staff1 = User.Create("staff1_adv", "staff1_adv@local", "hash", "Staff 1", UserRole.Staff).Value;
        var staff2 = User.Create("staff2_adv", "staff2_adv@local", "hash", "Staff 2", UserRole.Staff).Value;
        var user = User.Create("user_adv", "user_adv@local", "hash", "User", UserRole.User).Value;

        initContext.Users.AddRange(leader, staff1, staff2, user);
        initContext.SaveChanges();
    }

    public enum ActionType
    {
        Assign,
        StartProgress,
        HoldForInfo,
        Resolve,
        Close,
        Reopen,
        Reject
    }

    #region Part 1: Exhaustive 49 Permutations & Strict Immutability on Failure

    public static IEnumerable<object[]> GetAll49Permutations()
    {
        int[] statuses = [
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.Waiting,
            TicketStatus.Resolved,
            TicketStatus.Closed,
            TicketStatus.Rejected
        ];

        ActionType[] actions = [
            ActionType.Assign,
            ActionType.StartProgress,
            ActionType.HoldForInfo,
            ActionType.Resolve,
            ActionType.Close,
            ActionType.Reopen,
            ActionType.Reject
        ];

        foreach (var status in statuses)
        {
            foreach (var action in actions)
            {
                bool isAllowed = IsAllowedTransition(status, action);
                yield return new object[] { status, action, isAllowed };
            }
        }
    }

    private static bool IsAllowedTransition(int status, ActionType action) => (status, action) switch
    {
        (TicketStatus.New, ActionType.Assign) => true,
        (TicketStatus.New, ActionType.Reject) => true,

        (TicketStatus.Assigned, ActionType.Assign) => true, // Reassign
        (TicketStatus.Assigned, ActionType.StartProgress) => true,

        (TicketStatus.InProgress, ActionType.Assign) => true, // Reassign
        (TicketStatus.InProgress, ActionType.HoldForInfo) => true,
        (TicketStatus.InProgress, ActionType.Resolve) => true,

        (TicketStatus.Waiting, ActionType.Assign) => true, // Reassign
        (TicketStatus.Waiting, ActionType.StartProgress) => true,
        (TicketStatus.Waiting, ActionType.Resolve) => true,

        (TicketStatus.Resolved, ActionType.Close) => true,
        (TicketStatus.Resolved, ActionType.Reopen) => true,

        _ => false // All other 37 combinations are forbidden
    };

    [Theory]
    [MemberData(nameof(GetAll49Permutations))]
    public void Adversarial_All49Permutations_EnforcesZeroException_AndPreservesStateOnFailure(
        int initialStatus, ActionType action, bool isAllowed)
    {
        // ARRANGE
        var ticket = CreateFreshTicket(initialStatus);
        var initialToken = ticket.ConcurrencyToken;
        var initialUpdatedAt = ticket.UpdatedAt;
        var initialAssignedTo = ticket.AssignedToId;
        var initialResolvedAt = ticket.ResolvedAt;
        var initialClosedAt = ticket.ClosedAt;
        var initialRejectionReason = ticket.RejectionReason;
        var initialFeedback = ticket.Feedback;

        ticket.ClearDomainEvents();

        // ACT
        Result result = ExecuteAction(ticket, action);

        // ASSERT
        if (isAllowed)
        {
            result.IsSuccess.Should().BeTrue($"Thao tác '{action}' từ trạng thái '{initialStatus}' phải thành công.");
            result.Error.Should().Be(Error.None);

            if (action == ActionType.Assign && initialStatus == TicketStatus.Assigned)
            {
                ticket.StatusId.Should().Be(TicketStatus.Assigned, "Reassign tại Assigned giữ nguyên trạng thái Assigned.");
            }
            else
            {
                ticket.StatusId.Should().NotBe(initialStatus, "Trạng thái phải thay đổi sau thao tác hợp lệ.");
            }

            ticket.CurrentState.StatusId.Should().Be(ticket.StatusId);
            ticket.DomainEvents.Should().NotBeEmpty("Phải phát sinh Domain Event sau thao tác thành công.");
            ticket.ConcurrencyToken.Should().NotBe(initialToken, "ConcurrencyToken phải được làm mới khi đổi trạng thái.");
        }
        else
        {
            // CRITICAL ADVERSARIAL ASSERTIONS: 100% State Immutability
            result.IsFailure.Should().BeTrue($"Thao tác '{action}' từ trạng thái '{initialStatus}' bắt buộc phải BỊ TỪ CHỐI.");
            result.Error.Code.Should().NotBeNullOrWhiteSpace("Phải trả về mã lỗi cụ thể khi thao tác bị từ chối.");

            // 1. Status ID unchanged
            ticket.StatusId.Should().Be(initialStatus, "StatusId phải được bảo toàn 100% khi thao tác thất bại.");

            // 2. CurrentState unchanged
            ticket.CurrentState.StatusId.Should().Be(initialStatus, "CurrentState phải giữ nguyên trạng thái ban đầu.");

            // 3. ConcurrencyToken untouched
            ticket.ConcurrencyToken.Should().Be(initialToken, "ConcurrencyToken KHÔNG được phép đổi khi thao tác thất bại.");

            // 4. UpdatedAt untouched
            ticket.UpdatedAt.Should().Be(initialUpdatedAt, "UpdatedAt KHÔNG được phép cập nhật khi thao tác thất bại.");

            // 5. Zero false domain events
            ticket.DomainEvents.Should().BeEmpty("KHÔNG được phép phát sinh bất kỳ Domain Event ảo nào khi thao tác thất bại.");

            // 6. Navigation / Auxiliary fields preserved
            ticket.AssignedToId.Should().Be(initialAssignedTo);
            ticket.ResolvedAt.Should().Be(initialResolvedAt);
            ticket.ClosedAt.Should().Be(initialClosedAt);
            ticket.RejectionReason.Should().Be(initialRejectionReason);
            ticket.Feedback.Should().Be(initialFeedback);
        }
    }

    #endregion

    #region Part 2: Specific Boundary Edge Cases Demanded in Dispatch

    [Fact]
    public void Adversarial_ReopeningClosedTicket_IsForbiddenWithExplicitError()
    {
        // ARRANGE: Ticket đã chuyển đến Closed
        var ticket = CreateFreshTicket(TicketStatus.Closed);
        ticket.ClearDomainEvents();
        var closedAt = ticket.ClosedAt;

        // ACT 1: Reopen with string reason
        var result1 = ticket.Reopen("Người dùng muốn mở lại để sửa thêm");

        // ASSERT 1
        result1.IsFailure.Should().BeTrue();
        result1.Error.Code.Should().Be("TICKET_ALREADY_CLOSED");
        ticket.StatusId.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().Be(closedAt);
        ticket.DomainEvents.Should().BeEmpty();

        // ACT 2: Reopen with int overload
        var result2 = ticket.Reopen(CreatorId);

        // ASSERT 2
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("TICKET_ALREADY_CLOSED");
        ticket.StatusId.Should().Be(TicketStatus.Closed);
    }

    [Theory]
    [InlineData(TicketStatus.Assigned, "INVALID_STATE_TRANSITION")]
    [InlineData(TicketStatus.InProgress, "CANNOT_REJECT_IN_PROGRESS")] // Bảng 7.2 TC-06
    [InlineData(TicketStatus.Waiting, "INVALID_STATE_TRANSITION")]
    [InlineData(TicketStatus.Resolved, "INVALID_STATE_TRANSITION")]
    [InlineData(TicketStatus.Closed, "TICKET_ALREADY_CLOSED")]
    [InlineData(TicketStatus.Rejected, "TICKET_ALREADY_REJECTED")]
    public void Adversarial_RejectingInNonNewStates_IsStrictlyRejectedWithExpectedErrorCode(
        int nonNewStatus, string expectedErrorCode)
    {
        // ARRANGE
        var ticket = CreateFreshTicket(nonNewStatus);
        ticket.ClearDomainEvents();

        // ACT
        var result = ticket.Reject("Lý do từ chối thử nghiệm", LeaderId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedErrorCode);
        ticket.StatusId.Should().Be(nonNewStatus);
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Adversarial_ClosingResolvedTicket_WithNullFeedback_IsRejectedSafely()
    {
        // ARRANGE
        var ticket = CreateFreshTicket(TicketStatus.Resolved);
        ticket.ClearDomainEvents();

        // ACT
        var result = ticket.Close(feedback: null!);

        // ASSERT: Zero Exception, Failure Result returned
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FEEDBACK_REQUIRED");
        ticket.StatusId.Should().Be(TicketStatus.Resolved);
        ticket.ClosedAt.Should().BeNull();
        ticket.Feedback.Should().BeNull();
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    public void Adversarial_FeedbackCreation_WithOutOfRangeRating_IsRejected(int invalidRating)
    {
        var result = Feedback.Create(ticketId: 10, rating: invalidRating, comment: "Đánh giá ngoài dải");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FEEDBACK_RATING_RANGE");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Adversarial_FeedbackCreation_WithInvalidTicketId_IsRejected(int invalidTicketId)
    {
        var result = Feedback.Create(ticketId: invalidTicketId, rating: 5, comment: "Mã ticket không hợp lệ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FEEDBACK_TICKET_INVALID");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Adversarial_ReopenResolvedTicket_WithEmptyOrWhitespaceReason_IsRejected(string? emptyReason)
    {
        // ARRANGE
        var ticket = CreateFreshTicket(TicketStatus.Resolved);
        ticket.ClearDomainEvents();

        // ACT
        var result = ticket.Reopen(emptyReason!);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("REOPEN_REASON_REQUIRED");
        ticket.StatusId.Should().Be(TicketStatus.Resolved);
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Adversarial_RejectNewTicket_WithEmptyOrWhitespaceReason_IsRejected(string? emptyReason)
    {
        // ARRANGE
        var ticket = CreateFreshTicket(TicketStatus.New);
        ticket.ClearDomainEvents();

        // ACT
        var result = ticket.Reject(emptyReason!, LeaderId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("REJECTION_REASON_REQUIRED");
        ticket.StatusId.Should().Be(TicketStatus.New);
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void Adversarial_AssignNewTicket_WithNonPositiveStaffId_IsRejected(int invalidStaffId)
    {
        // ARRANGE
        var ticket = CreateFreshTicket(TicketStatus.New);
        ticket.ClearDomainEvents();

        // ACT
        var result = ticket.Assign(invalidStaffId, LeaderId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("STAFF_ID_INVALID");
        ticket.StatusId.Should().Be(TicketStatus.New);
        ticket.AssignedToId.Should().BeNull();
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Adversarial_StartProgress_ByUnauthorizedStaff_IsRejected()
    {
        // ARRANGE: Ticket giao cho AssignedStaffId (5)
        var ticket = CreateFreshTicket(TicketStatus.Assigned);
        ticket.ClearDomainEvents();

        // ACT: OtherStaffId (88) cố gắng StartProgress
        var result = ticket.StartProgress(OtherStaffId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_STAFF");
        ticket.StatusId.Should().Be(TicketStatus.Assigned);
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Adversarial_HoldForInfo_ByUnauthorizedStaff_IsRejected()
    {
        // ARRANGE: Ticket giao cho AssignedStaffId (5)
        var ticket = CreateFreshTicket(TicketStatus.InProgress);
        ticket.ClearDomainEvents();

        // ACT: OtherStaffId (88) cố gắng HoldForInfo
        var result = ticket.HoldForInfo("Chờ thông tin", OtherStaffId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_STAFF");
        ticket.StatusId.Should().Be(TicketStatus.InProgress);
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Adversarial_Resolve_ByUnauthorizedStaff_IsRejected()
    {
        // ARRANGE: Ticket giao cho AssignedStaffId (5)
        var ticket = CreateFreshTicket(TicketStatus.InProgress);
        ticket.ClearDomainEvents();

        // ACT: OtherStaffId (88) cố gắng Resolve
        var result = ticket.Resolve(OtherStaffId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_STAFF");
        ticket.StatusId.Should().Be(TicketStatus.InProgress);
        ticket.ResolvedAt.Should().BeNull();
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Adversarial_CloseResolvedTicket_ByNonCreator_IsRejected()
    {
        // ARRANGE
        var ticket = CreateFreshTicket(TicketStatus.Resolved);
        ticket.ClearDomainEvents();

        // ACT: OtherUserId (99) cố gắng đóng
        var result = ticket.Close(OtherUserId);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_USER");
        ticket.StatusId.Should().Be(TicketStatus.Resolved);
        ticket.ClosedAt.Should().BeNull();
        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Adversarial_ReopenResolvedTicket_ByNonCreator_IsRejected()
    {
        // ARRANGE
        var ticket = CreateFreshTicket(TicketStatus.Resolved);
        ticket.ClearDomainEvents();

        // ACT: OtherUser cố gắng Reopen
        var otherUser = User.Create("other_adv", "other_adv@local", "hash", "Other", UserRole.User).Value;
        typeof(EntityBase<int>).GetProperty(nameof(otherUser.Id))!.SetValue(otherUser, OtherUserId);
        var result = ticket.Reopen("Mở lại ticket", otherUser);

        // ASSERT
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UNAUTHORIZED_USER");
        ticket.StatusId.Should().Be(TicketStatus.Resolved);
        ticket.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Part 3: High-Stress Concurrency & Race Condition Verification

    [Fact]
    public async Task Adversarial_ConcurrentStateTransitions_FromNewState_GuaranteesOptimisticSafety()
    {
        // ARRANGE: Tạo ticket trong database SQLite
        int ticketId;
        using (var setupContext = new ItTmsDbContext(_dbOptions))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var ticket = Ticket.Create("Concurrency Race New", "Description", TicketPriority.High, TicketCategory.Software, user.Id).Value;
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        const int workerCount = 10;
        var barrier = new Barrier(workerCount);

        var successCount = 0;
        var concurrencyConflictCount = 0;
        var domainFailureCount = 0;
        var unexpectedExceptions = new ConcurrentBag<Exception>();

        // ACT: 10 workers đồng thời cố gắng thực hiện các thao tác khác nhau
        var tasks = Enumerable.Range(0, workerCount).Select(index => Task.Run(async () =>
        {
            using var context = new ItTmsDbContext(_dbOptions);
            var t = await context.Tickets.FindAsync(ticketId);

            // Đồng bộ hoá thời điểm kích hoạt qua Barrier
            barrier.SignalAndWait();

            Result opResult;
            switch (index % 5)
            {
                case 0: // Hợp lệ: Assign staff 1
                    opResult = t!.Assign(staffId: 2, leaderId: LeaderId);
                    break;
                case 1: // Hợp lệ: Assign staff 2
                    opResult = t!.Assign(staffId: 3, leaderId: LeaderId);
                    break;
                case 2: // Hợp lệ: Reject
                    opResult = t!.Reject($"Từ chối bởi worker {index}", LeaderId);
                    break;
                case 3: // Bất hợp pháp: Close trên New (TC-05)
                    opResult = t!.Close(CreatorId);
                    break;
                case 4: // Bất hợp pháp: Resolve trên New
                    opResult = t!.Resolve(AssignedStaffId);
                    break;
                default:
                    opResult = Result.Failure(new Error("DEFAULT", "Default"));
                    break;
            }

            if (opResult.IsFailure)
            {
                Interlocked.Increment(ref domainFailureCount);
                return;
            }

            try
            {
                await context.SaveChangesAsync();
                Interlocked.Increment(ref successCount);
            }
            catch (DbUpdateConcurrencyException)
            {
                Interlocked.Increment(ref concurrencyConflictCount);
            }
            catch (Exception ex)
            {
                unexpectedExceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // ASSERT:
        unexpectedExceptions.Should().BeEmpty("Không được phát sinh lỗi deadlock, database locked hoặc unhandled exception.");
        domainFailureCount.Should().Be(4, "4 thao tác vi phạm quy tắc State Machine (Close, Resolve) phải bị chặn ở Domain Layer.");
        successCount.Should().Be(1, "Chỉ duy nhất 1 thao tác hợp lệ được phép ghi thành công vào CSDL SQLite.");
        concurrencyConflictCount.Should().Be(5, "5 thao tác hợp lệ còn lại bị chặn bởi DbUpdateConcurrencyException.");

        // Kiểm tra tính toàn vẹn của dữ liệu trong CSDL
        using var verifyContext = new ItTmsDbContext(_dbOptions);
        var finalTicket = await verifyContext.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        finalTicket.StatusId.Should().BeOneOf(TicketStatus.Assigned, TicketStatus.Rejected);
    }

    [Fact]
    public async Task Adversarial_ConcurrentStateTransitions_FromInProgressState_ProtectsTC06AndOptimisticControl()
    {
        // ARRANGE: Tạo ticket trong CSDL ở trạng thái InProgress
        int ticketId;
        using (var setupContext = new ItTmsDbContext(_dbOptions))
        {
            var user = await setupContext.Users.FirstAsync(u => u.Role == UserRole.User);
            var staff = await setupContext.Users.FirstAsync(u => u.Role == UserRole.Staff);
            var leader = await setupContext.Users.FirstAsync(u => u.Role == UserRole.Leader);

            var ticket = Ticket.Create("Concurrency Race InProgress", "Desc", TicketPriority.Critical, TicketCategory.Network, user.Id).Value;
            ticket.Assign(staff.Id, leader.Id);
            ticket.StartProgress(staff.Id);
            setupContext.Tickets.Add(ticket);
            await setupContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        const int workerCount = 10;
        var barrier = new Barrier(workerCount);

        var successCount = 0;
        var concurrencyConflictCount = 0;
        var rejectedCount = 0;
        var unexpectedExceptions = new ConcurrentBag<Exception>();

        // ACT: 10 workers đồng thời:
        // - 3 workers cố gắng Reject (Bảng 7.2 TC-06: Phải bị chặn đứng ở domain)
        // - 4 workers cố gắng Resolve
        // - 3 workers cố gắng PutOnHold
        var tasks = Enumerable.Range(0, workerCount).Select(index => Task.Run(async () =>
        {
            using var context = new ItTmsDbContext(_dbOptions);
            var t = await context.Tickets.FindAsync(ticketId);

            barrier.SignalAndWait();

            Result opResult;
            if (index < 3)
            {
                // TC-06: Leader cố gắng Reject ticket đang InProgress
                opResult = t!.Reject("Leader reject attempt", LeaderId);
            }
            else if (index < 7)
            {
                // Staff cố gắng Resolve
                opResult = t!.Resolve(t.AssignedToId ?? 0);
            }
            else
            {
                // Staff cố gắng PutOnHold
                opResult = t!.PutOnHold(t.AssignedToId ?? 0);
            }

            if (opResult.IsFailure)
            {
                if (opResult.Error.Code == "CANNOT_REJECT_IN_PROGRESS")
                {
                    Interlocked.Increment(ref rejectedCount);
                }
                return;
            }

            try
            {
                await context.SaveChangesAsync();
                Interlocked.Increment(ref successCount);
            }
            catch (DbUpdateConcurrencyException)
            {
                Interlocked.Increment(ref concurrencyConflictCount);
            }
            catch (Exception ex)
            {
                unexpectedExceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // ASSERT:
        unexpectedExceptions.Should().BeEmpty();
        rejectedCount.Should().Be(3, "Cả 3 nỗ lực Reject ticket InProgress phải bị chặn đứng với mã CANNOT_REJECT_IN_PROGRESS.");
        successCount.Should().Be(1, "Chỉ duy nhất 1 thao tác Resolve hoặc PutOnHold được phép commit thành công.");
        concurrencyConflictCount.Should().Be(6, "6 tác vụ hợp lệ còn lại phải nhận DbUpdateConcurrencyException.");

        // Kiểm chứng trạng thái cuối cùng
        using var verifyContext = new ItTmsDbContext(_dbOptions);
        var finalTicket = await verifyContext.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticketId);
        finalTicket.StatusId.Should().BeOneOf(TicketStatus.Resolved, TicketStatus.Waiting);
    }

    #endregion

    #region Part 4: Property-Based Randomized Invariant Fuzzing

    [Fact]
    public void Adversarial_Randomized1000Transitions_MaintainsZeroExceptionsAndInvariants()
    {
        var random = new Random(42);
        int[] allStatuses = [
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.Waiting,
            TicketStatus.Resolved,
            TicketStatus.Closed,
            TicketStatus.Rejected
        ];

        var actionValues = Enum.GetValues<ActionType>();

        for (int i = 0; i < 1000; i++)
        {
            var initialStatus = allStatuses[random.Next(allStatuses.Length)];
            var randomAction = actionValues[random.Next(actionValues.Length)];

            var ticket = CreateFreshTicket(initialStatus);
            var prevStatus = ticket.StatusId;
            var prevToken = ticket.ConcurrencyToken;

            // Act: Zero exceptions must escape
            Action act = () => ExecuteAction(ticket, randomAction);
            act.Should().NotThrow($"Vòng lặp fuzzing #{i}: Hành động {randomAction} trên trạng thái {initialStatus} không được văng Exception.");

            // If status changed, it must be a valid status ID between 1 and 7
            ticket.StatusId.Should().BeInRange(TicketStatus.New, TicketStatus.Rejected);
            ticket.CurrentState.StatusId.Should().Be(ticket.StatusId);

            if (ticket.StatusId != prevStatus)
            {
                ticket.ConcurrencyToken.Should().NotBe(prevToken);
            }
        }
    }

    #endregion

    #region Helpers

    private static Ticket CreateFreshTicket(int statusId)
    {
        var ticket = Ticket.Create("Adv Ticket", "Adv Desc", TicketPriority.Medium, TicketCategory.Software, CreatorId).Value;

        switch (statusId)
        {
            case TicketStatus.New:
                break;
            case TicketStatus.Assigned:
                ticket.Assign(AssignedStaffId, LeaderId);
                break;
            case TicketStatus.InProgress:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                break;
            case TicketStatus.Waiting:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                ticket.PutOnHold(AssignedStaffId);
                break;
            case TicketStatus.Resolved:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                ticket.Resolve(AssignedStaffId);
                break;
            case TicketStatus.Closed:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                ticket.Resolve(AssignedStaffId);
                ticket.Close(CreatorId);
                break;
            case TicketStatus.Rejected:
                ticket.Reject("Lý do từ chối ban đầu", LeaderId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(statusId));
        }

        return ticket;
    }

    private static Result ExecuteAction(Ticket ticket, ActionType action) => action switch
    {
        ActionType.Assign => ticket.Assign(staffId: 77, leaderId: LeaderId),
        ActionType.StartProgress => ticket.StartProgress(staffId: AssignedStaffId),
        ActionType.HoldForInfo => ticket.HoldForInfo(reason: "Cần thêm thông tin", staffId: AssignedStaffId),
        ActionType.Resolve => ticket.Resolve(staffId: AssignedStaffId),
        ActionType.Close => ticket.Close(userId: CreatorId),
        ActionType.Reopen => ticket.Reopen(CreatorId),
        ActionType.Reject => ticket.Reject(reason: "Từ chối tiếp nhận", leaderId: LeaderId),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public void Dispose()
    {
        using (var cleanupContext = new ItTmsDbContext(_dbOptions))
        {
            cleanupContext.Database.EnsureDeleted();
        }

        if (File.Exists(_dbFileName))
        {
            try { File.Delete(_dbFileName); } catch { /* ignore */ }
        }

        GC.SuppressFinalize(this);
    }

    #endregion
}
