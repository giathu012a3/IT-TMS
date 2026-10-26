using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Extensions;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;

namespace ItTms.Tests.UnitTests.Domain;

/// <summary>
/// Adversarial SLA Stress Harness:
/// Rigorously tests SLA calculation and invariants against leap years, DST transitions,
/// extreme boundaries, negative durations, and undefined enum inputs.
/// </summary>
public class AdversarialSlaStressTests
{
    [Theory]
    // Năm nhuận 2028 (tháng 2 có 29 ngày)
    [InlineData(2028, 2, 28, 23, 30, TicketPriority.Critical, 2028, 2, 29, 0, 30)] // +1h vượt sang 29/02
    [InlineData(2028, 2, 28, 23, 0, TicketPriority.High, 2028, 2, 29, 3, 0)]       // +4h vào rạng sáng 29/02
    [InlineData(2028, 2, 28, 12, 0, TicketPriority.Medium, 2028, 2, 29, 12, 0)]     // +24h vào giữa trưa 29/02
    [InlineData(2028, 2, 28, 0, 0, TicketPriority.Low, 2028, 3, 2, 0, 0)]          // +72h (3 ngày): 28 -> 29 -> 1/3 -> 2/3
    [InlineData(2028, 2, 29, 10, 0, TicketPriority.Medium, 2028, 3, 1, 10, 0)]      // +24h từ 29/02 sang 01/03
    public void CalculateDueAt_LeapYear2028_AdvancesAccurately(
        int year, int month, int day, int hour, int minute,
        TicketPriority priority,
        int expYear, int expMonth, int expDay, int expHour, int expMinute)
    {
        var createdAt = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        var expectedDueAt = new DateTime(expYear, expMonth, expDay, expHour, expMinute, 0, DateTimeKind.Utc);

        var actualDueAt = Ticket.CalculateDueAt(priority, createdAt);

        actualDueAt.Should().Be(expectedDueAt);
        actualDueAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    // Năm không nhuận 2026 (tháng 2 chỉ có 28 ngày)
    [InlineData(2026, 2, 28, 23, 30, TicketPriority.Critical, 2026, 3, 1, 0, 30)] // +1h nhảy thẳng sang 01/03
    [InlineData(2026, 2, 28, 22, 0, TicketPriority.High, 2026, 3, 1, 2, 0)]       // +4h nhảy sang 01/03
    [InlineData(2026, 2, 28, 12, 0, TicketPriority.Medium, 2026, 3, 1, 12, 0)]     // +24h nhảy sang 01/03
    [InlineData(2026, 2, 28, 0, 0, TicketPriority.Low, 2026, 3, 3, 0, 0)]          // +72h (3 ngày): 28 -> 1/3 -> 2/3 -> 3/3
    public void CalculateDueAt_NonLeapYear2026_TransitionsDirectlyToMarch(
        int year, int month, int day, int hour, int minute,
        TicketPriority priority,
        int expYear, int expMonth, int expDay, int expHour, int expMinute)
    {
        var createdAt = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        var expectedDueAt = new DateTime(expYear, expMonth, expDay, expHour, expMinute, 0, DateTimeKind.Utc);

        var actualDueAt = Ticket.CalculateDueAt(priority, createdAt);

        actualDueAt.Should().Be(expectedDueAt);
        actualDueAt.Day.Should().NotBe(29, "Năm 2026 không có ngày 29 tháng 2");
    }

    [Fact]
    public void CalculateDueAt_CenturyLeapYearRules_CompliesWithGregorianCalendar()
    {
        // Năm 2000 chia hết cho 400 => Là năm nhuận (có ngày 29/2)
        var y2000 = new DateTime(2000, 2, 28, 23, 0, 0, DateTimeKind.Utc);
        var due2000 = Ticket.CalculateDueAt(TicketPriority.Critical, y2000);
        due2000.Should().Be(new DateTime(2000, 2, 29, 0, 0, 0, DateTimeKind.Utc));

        // Năm 2100 chia hết cho 100 nhưng không chia hết cho 400 => Không phải năm nhuận (không có ngày 29/2)
        var y2100 = new DateTime(2100, 2, 28, 23, 0, 0, DateTimeKind.Utc);
        var due2100 = Ticket.CalculateDueAt(TicketPriority.Critical, y2100);
        due2100.Should().Be(new DateTime(2100, 3, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateDueAt_DaylightSavingTransition_MaintainsMonotonicAbsoluteDurationInUtc()
    {
        // Thời điểm chuyển đổi US DST Spring Forward 2026: 08/03/2026
        // Ở cấp độ UTC, hệ thống không bị gián đoạn hay trùng lặp giờ
        var dstTransitionUtc = new DateTime(2026, 3, 8, 6, 0, 0, DateTimeKind.Utc);

        var dueCritical = Ticket.CalculateDueAt(TicketPriority.Critical, dstTransitionUtc);
        var dueHigh = Ticket.CalculateDueAt(TicketPriority.High, dstTransitionUtc);
        var dueMedium = Ticket.CalculateDueAt(TicketPriority.Medium, dstTransitionUtc);
        var dueLow = Ticket.CalculateDueAt(TicketPriority.Low, dstTransitionUtc);

        (dueCritical - dstTransitionUtc).TotalHours.Should().Be(1);
        (dueHigh - dstTransitionUtc).TotalHours.Should().Be(4);
        (dueMedium - dstTransitionUtc).TotalHours.Should().Be(24);
        (dueLow - dstTransitionUtc).TotalHours.Should().Be(72);
    }

    [Theory]
    [InlineData((TicketPriority)0)]     // Không xác định (dưới cận)
    [InlineData((TicketPriority)99)]    // Giá trị vượt dải định nghĩa
    [InlineData((TicketPriority)(-1))]  // Giá trị âm
    public void CalculateDueAt_UndefinedPriorityEnum_FallsBackToDefault24Hours(TicketPriority invalidPriority)
    {
        var createdAt = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);

        // ACT
        var dueAt = Ticket.CalculateDueAt(invalidPriority, createdAt);

        // ASSERT: Fallback an toàn về 24 giờ mà không ném crash ngoại lệ
        dueAt.Should().Be(createdAt.AddHours(24));
    }

    [Fact]
    public void SlaCalculations_ExactSecondBoundary_EvaluatesPrecisionCorrectly()
    {
        var baseTime = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        var ticket = Ticket.Create("Boundary Test", "Desc", TicketPriority.Critical, TicketCategory.Software, 1, baseTime).Value;
        // DueAt = 13:00:00

        // 1. Đúng thời điểm DueAt: ticket chưa bị tính là quá hạn (DueAt < now là false khi DueAt == now)
        ticket.IsOverdue(ticket.DueAt).Should().BeFalse();
        ticket.RemainingTime(ticket.DueAt).Should().Be(TimeSpan.Zero);

        // 2. Sau đúng 1 tick (100 nanoseconds): ticket lập tức chuyển sang quá hạn
        var oneTickAfter = ticket.DueAt.AddTicks(1);
        ticket.IsOverdue(oneTickAfter).Should().BeTrue();
        ticket.RemainingTime(oneTickAfter).Should().BeNegative();

        // 3. Trước 1 tick: còn thời hạn
        var oneTickBefore = ticket.DueAt.AddTicks(-1);
        ticket.IsOverdue(oneTickBefore).Should().BeFalse();
        ticket.RemainingTime(oneTickBefore).Should().BePositive();
    }

    [Theory]
    [InlineData(TicketStatus.New, true)]
    [InlineData(TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Rejected, false)]
    public void IsOverdue_ExhaustiveStateMatrix_WhenPastDeadline(int statusId, bool expectedOverdue)
    {
        var baseTime = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);
        var ticket = Ticket.Create("State Overdue Test", "Desc", TicketPriority.Critical, TicketCategory.Hardware, 1, baseTime).Value;
        ticket.StatusId = statusId;

        // Kiểm tra tại thời điểm sau hạn chót (10:00:00 > DueAt 09:00:00)
        var checkTime = baseTime.AddHours(2);
        ticket.IsOverdue(checkTime).Should().Be(expectedOverdue);
    }

    [Fact]
    public void RemainingTime_WhenOverdue_ReturnsNegativeTimeSpan()
    {
        var baseTime = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);
        var ticket = Ticket.Create("Negative Duration Test", "Desc", TicketPriority.Critical, TicketCategory.Network, 1, baseTime).Value;
        // DueAt = 09:00:00

        var overdueTime = baseTime.AddHours(3); // 11:00:00 (quá hạn 2 giờ)
        var remaining = ticket.RemainingTime(overdueTime);

        remaining.TotalHours.Should().Be(-2);
        remaining.Should().BeNegative();
    }

    [Fact]
    public async Task SQLite_Roundtrip_PreservesUtcKind_AndSlaPrecision()
    {
        var dbName = $"test_sla_roundtrip_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<ItTmsDbContext>()
            .UseSqlite($"Data Source={dbName};Cache=Shared")
            .AddInterceptors(new ConcurrencyTokenInterceptor())
            .Options;

        try
        {
            using (var initContext = new ItTmsDbContext(options))
            {
                initContext.Database.Migrate();
                initContext.TicketStatuses.Add(TicketStatus.Create(TicketStatus.New, "New", "Mới tạo", "badge", 1));
                var user = User.Create("u1", "u1@local", "hash", "User 1", UserRole.User).Value;
                initContext.Users.Add(user);
                await initContext.SaveChangesAsync();

                var leapTime = new DateTime(2028, 2, 28, 23, 45, 0, DateTimeKind.Utc);
                var ticket = Ticket.Create("Leap Year Ticket", "Desc", TicketPriority.Critical, TicketCategory.Other, user.Id, leapTime).Value;
                initContext.Tickets.Add(ticket);
                await initContext.SaveChangesAsync();
            }

            using (var readContext = new ItTmsDbContext(options))
            {
                var loadedTicket = await readContext.Tickets.FirstAsync(t => t.Title == "Leap Year Ticket");

                // ASSERT: Ngày giờ đọc từ SQLite phải duy trì đúng DateTimeKind.Utc và chính xác đến giây
                loadedTicket.DueAt.Kind.Should().Be(DateTimeKind.Utc);
                loadedTicket.DueAt.Should().Be(new DateTime(2028, 2, 29, 0, 45, 0, DateTimeKind.Utc));
                loadedTicket.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
                loadedTicket.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
            }
        }
        finally
        {
            using var cleanupContext = new ItTmsDbContext(options);
            cleanupContext.Database.EnsureDeleted();
            if (File.Exists(dbName))
            {
                try { File.Delete(dbName); } catch { }
            }
        }
    }
}
