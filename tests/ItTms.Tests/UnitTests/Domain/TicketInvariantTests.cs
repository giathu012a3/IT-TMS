using FluentAssertions;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Extensions;

namespace ItTms.Tests.UnitTests.Domain;

public class TicketInvariantTests
{
    [Fact]
    public void CreateTicket_WithValidData_InitializesInNewStateAndCalculatesSla()
    {
        // Arrange
        var baseTime = new DateTime(2026, 10, 25, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = Ticket.Create(
            "Mất mạng toàn công ty",
            "Không vào được mạng LAN và Internet",
            TicketPriority.Critical,
            TicketCategory.Network,
            creatorId: 10,
            currentTime: baseTime
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        var ticket = result.Value;
        ticket.Title.Should().Be("Mất mạng toàn công ty");
        ticket.StatusId.Should().Be(TicketStatus.New);
        ticket.Priority.Should().Be(TicketPriority.Critical);
        ticket.Category.Should().Be(TicketCategory.Network);
        ticket.CreatorId.Should().Be(10);
        ticket.CreatedAt.Should().Be(baseTime);
        ticket.DueAt.Should().Be(baseTime.AddHours(1)); // Critical SLA: 1 hour
        ticket.ConcurrencyToken.Should().NotBeEmpty();
        ticket.DomainEvents.Should().ContainSingle();
    }

    [Theory]
    [InlineData(TicketPriority.Critical, 1)]
    [InlineData(TicketPriority.High, 4)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.Low, 72)]
    public void CalculateDueAt_MatchesSlaSpecification(TicketPriority priority, int expectedHours)
    {
        // Arrange
        var createdAt = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);

        // Act
        var dueAt = Ticket.CalculateDueAt(priority, createdAt);

        // Assert
        dueAt.Should().Be(createdAt.AddHours(expectedHours));
    }

    [Theory]
    [InlineData(TicketStatus.New, true)]
    [InlineData(TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Resolved, false)] // Bảng 7.2 TC-08: Resolved blocks comments
    [InlineData(TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Rejected, false)]
    public void CanComment_ReturnsExpectedResultBasedOnStatus(int statusId, bool expectedCanComment)
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Desc", TicketPriority.Medium, TicketCategory.Software, 1).Value;
        ticket.StatusId = statusId;

        // Act & Assert
        ticket.CanComment().Should().Be(expectedCanComment);
    }

    [Fact]
    public void Reject_WhenTicketIsInProgress_ReturnsFailure_Table72TC06()
    {
        // Arrange (Bảng 7.2 TC-06: Leader cannot reject an InProgress ticket)
        var ticket = Ticket.Create("Test", "Desc", TicketPriority.Medium, TicketCategory.Software, 1).Value;
        ticket.Assign(staffId: 5, leaderId: 2);
        ticket.StartProgress(staffId: 5);

        // Act
        var result = ticket.Reject("Không thuộc phạm vi hỗ trợ", leaderId: 2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CANNOT_REJECT_IN_PROGRESS");
        ticket.StatusId.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void Reject_WhenTicketIsNew_Succeeds()
    {
        // Arrange
        var ticket = Ticket.Create("Test", "Desc", TicketPriority.Medium, TicketCategory.Software, 1).Value;

        // Act
        var result = ticket.Reject("Yêu cầu trùng lặp", leaderId: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.StatusId.Should().Be(TicketStatus.Rejected);
        ticket.RejectionReason.Should().Be("Yêu cầu trùng lặp");
    }

    [Fact]
    public void FullLifecycle_FromNewToClosed_TransitionsCorrectly()
    {
        // 1. User tạo ticket
        var ticket = Ticket.Create("Hỏng chuột máy tính", "Chuột bị liệt click trái", TicketPriority.Low, TicketCategory.Hardware, creatorId: 5).Value;
        ticket.StatusId.Should().Be(TicketStatus.New);

        // 2. Leader phân công cho Staff 10
        var assignResult = ticket.Assign(staffId: 10, leaderId: 2);
        assignResult.IsSuccess.Should().BeTrue();
        ticket.StatusId.Should().Be(TicketStatus.Assigned);
        ticket.AssignedToId.Should().Be(10);

        // 3. Staff 10 bắt đầu xử lý
        var startResult = ticket.StartProgress(staffId: 10);
        startResult.IsSuccess.Should().BeTrue();
        ticket.StatusId.Should().Be(TicketStatus.InProgress);

        // 4. Staff 10 hoàn thành xử lý
        var resolveResult = ticket.Resolve(staffId: 10);
        resolveResult.IsSuccess.Should().BeTrue();
        ticket.StatusId.Should().Be(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().NotBeNull();

        // 5. User 5 xác nhận đóng ticket
        var closeResult = ticket.Close(userId: 5);
        closeResult.IsSuccess.Should().BeTrue();
        ticket.StatusId.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public void IsOverdue_DetectsSlaBreachCorrectly()
    {
        // Arrange
        var baseTime = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);
        var ticket = Ticket.Create("Test", "Desc", TicketPriority.Critical, TicketCategory.Software, 1, baseTime).Value;
        // DueAt is baseTime + 1h = 9:00:00

        // Act & Assert
        ticket.IsOverdue(baseTime.AddMinutes(30)).Should().BeFalse();
        ticket.IsOverdue(baseTime.AddHours(2)).Should().BeTrue(); // Overdue at 10:00

        // If resolved, should not be considered overdue even if current time is after DueAt
        ticket.Assign(staffId: 2, leaderId: 1);
        ticket.StartProgress(staffId: 2);
        ticket.Resolve(staffId: 2);
        ticket.IsOverdue(baseTime.AddHours(5)).Should().BeFalse();
    }
}
