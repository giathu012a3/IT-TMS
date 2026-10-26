namespace ItTms.Tests.UnitTests.Domain;

using System;
using System.Collections.Generic;
using FluentAssertions;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.States;
using Xunit;

public class StateMachine49PermutationTests
{
    private const int CreatorId = 10;
    private const int AssignedStaffId = 5;
    private const int LeaderId = 2;

    public enum TicketAction
    {
        Assign,
        StartProgress,
        PutOnHold,
        Resolve,
        Close,
        Reopen,
        Reject
    }

    public static IEnumerable<object[]> Get49PermutationsData()
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

        TicketAction[] actions = [
            TicketAction.Assign,
            TicketAction.StartProgress,
            TicketAction.PutOnHold,
            TicketAction.Resolve,
            TicketAction.Close,
            TicketAction.Reopen,
            TicketAction.Reject
        ];

        foreach (var status in statuses)
        {
            foreach (var action in actions)
            {
                bool isExpectedSuccess = IsTransitionAllowed(status, action);
                yield return new object[] { status, action, isExpectedSuccess };
            }
        }
    }

    private static bool IsTransitionAllowed(int currentStatus, TicketAction action) => (currentStatus, action) switch
    {
        // 1. New State
        (TicketStatus.New, TicketAction.Assign) => true,
        (TicketStatus.New, TicketAction.Reject) => true,

        // 2. Assigned State
        (TicketStatus.Assigned, TicketAction.Assign) => true, // Reassign
        (TicketStatus.Assigned, TicketAction.StartProgress) => true,

        // 3. InProgress State
        (TicketStatus.InProgress, TicketAction.Assign) => true, // Reassign
        (TicketStatus.InProgress, TicketAction.PutOnHold) => true,
        (TicketStatus.InProgress, TicketAction.Resolve) => true,

        // 4. Waiting State
        (TicketStatus.Waiting, TicketAction.Assign) => true, // Reassign
        (TicketStatus.Waiting, TicketAction.StartProgress) => true,
        (TicketStatus.Waiting, TicketAction.Resolve) => true,

        // 5. Resolved State
        (TicketStatus.Resolved, TicketAction.Close) => true,
        (TicketStatus.Resolved, TicketAction.Reopen) => true,

        // 6. Closed State & 7. Rejected State: Mọi thao tác đều bị cấm
        _ => false
    };

    [Theory]
    [MemberData(nameof(Get49PermutationsData))]
    public void StateMachine_49Permutations_StrictlyEnforcesAllowedVsForbiddenTransitions(
        int initialStatus, TicketAction action, bool expectedSuccess)
    {
        // Arrange
        var ticket = CreateTicketInStatus(initialStatus);
        ticket.StatusId.Should().Be(initialStatus);

        // Act
        Result result = ExecuteAction(ticket, action);

        // Assert
        if (expectedSuccess)
        {
            result.IsSuccess.Should().BeTrue($"Thao tác {action} từ trạng thái {initialStatus} phải HỢP LỆ");
            if (action == TicketAction.Assign && initialStatus == TicketStatus.Assigned)
            {
                ticket.StatusId.Should().Be(TicketStatus.Assigned, "Tái phân công giữ nguyên trạng thái Assigned");
            }
            else
            {
                ticket.StatusId.Should().NotBe(initialStatus, "Trạng thái phải được chuyển dịch sau khi thực hiện thao tác hợp lệ");
            }
        }
        else
        {
            result.IsFailure.Should().BeTrue($"Thao tác {action} từ trạng thái {initialStatus} phải BỊ TỪ CHỐI");
            ticket.StatusId.Should().Be(initialStatus, "Trạng thái ban đầu BẮT BUỘC giữ nguyên 100% khi thao tác thất bại");
        }
    }

    [Theory]
    [InlineData(TicketStatus.New, true)]
    [InlineData(TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Waiting, true)]
    [InlineData(TicketStatus.Resolved, false)] // Bảng 7.2 TC-08: Resolved blocks comments
    [InlineData(TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Rejected, false)]
    public void StateMachine_CanComment_MatchesStateRequirements(int statusId, bool expectedCanComment)
    {
        // Arrange
        var ticket = CreateTicketInStatus(statusId);

        // Act & Assert
        ticket.CanComment().Should().Be(expectedCanComment);
        ticket.CurrentState.CanComment.Should().Be(expectedCanComment);
    }

    [Fact]
    public void StateMachine_TC05_NewTicketCannotBeClosedDirectly()
    {
        // Arrange (Bảng 7.2 TC-05: Staff setting Closed on a New ticket is rejected)
        var ticket = CreateTicketInStatus(TicketStatus.New);

        // Act
        var result = ticket.Close(CreatorId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_STATE_TRANSITION");
        ticket.StatusId.Should().Be(TicketStatus.New);
    }

    [Fact]
    public void StateMachine_TC06_InProgressTicketCannotBeRejected()
    {
        // Arrange (Bảng 7.2 TC-06: Leader cannot reject an InProgress ticket)
        var ticket = CreateTicketInStatus(TicketStatus.InProgress);

        // Act
        var result = ticket.Reject("Từ chối tiếp nhận", LeaderId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CANNOT_REJECT_IN_PROGRESS");
        ticket.StatusId.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void StateMachine_TC09_CloseAndRateResolvedTicket_TransitionsToClosedWithFeedback()
    {
        // Arrange (Bảng 7.2 TC-09: Closing and rating a Resolved ticket transitions to Closed)
        var ticket = CreateTicketInStatus(TicketStatus.Resolved);
        typeof(EntityBase<int>).GetProperty(nameof(ticket.Id))!.SetValue(ticket, 101);
        var feedback = Feedback.Create(ticket.Id, 5, "Dịch vụ xuất sắc").Value;

        // Act
        var result = ticket.Close(feedback);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.StatusId.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().NotBeNull();
        ticket.Feedback.Should().NotBeNull();
        ticket.Feedback!.Rating.Should().Be(5);
        ticket.CurrentState.Should().BeOfType<ClosedState>();
    }

    private static Ticket CreateTicketInStatus(int statusId)
    {
        var ticket = Ticket.Create("Test Title", "Test Description", TicketPriority.Medium, TicketCategory.Software, CreatorId).Value;

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
                ticket.Reject("Lý do từ chối", LeaderId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(statusId));
        }

        ticket.ClearDomainEvents();
        return ticket;
    }

    private static Result ExecuteAction(Ticket ticket, TicketAction action) => action switch
    {
        TicketAction.Assign => ticket.Assign(staffId: 99, leaderId: LeaderId),
        TicketAction.StartProgress => ticket.StartProgress(staffId: AssignedStaffId),
        TicketAction.PutOnHold => ticket.PutOnHold(staffId: AssignedStaffId),
        TicketAction.Resolve => ticket.Resolve(staffId: AssignedStaffId),
        TicketAction.Close => ticket.Close(userId: CreatorId),
        TicketAction.Reopen => ticket.Reopen(userId: CreatorId),
        TicketAction.Reject => ticket.Reject(reason: "Lý do từ chối thử nghiệm", leaderId: LeaderId),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
