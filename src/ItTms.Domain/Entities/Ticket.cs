using ItTms.Domain.Common;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Domain.States;

namespace ItTms.Domain.Entities;

public class Ticket : AggregateRoot<int>
{
    private ITicketState? _currentState;
    private int _statusId = TicketStatus.New;

    public Ticket() { }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; } = TicketCategory.Other;

    // Foreign Keys
    public int CreatorId { get; set; }
    public int? AssignedToId { get; set; }

    public int StatusId
    {
        get => _statusId;
        set
        {
            if (_statusId != value)
            {
                _statusId = value;
                _currentState = null; // Khôi phục đúng State khi EF Core hoặc tests gán giá trị
            }
        }
    }

    /// <summary>
    /// Đối tượng State hiện tại của Ticket theo GoF State Pattern.
    /// </summary>
    public ITicketState CurrentState => _currentState ??= TicketStateFactory.CreateState(this, _statusId);

    // SLA & Timestamps
    public DateTime DueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic Concurrency Token phục vụ kiểm soát xung đột trên SQLite.
    /// </summary>
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    // Navigations
    public virtual User Creator { get; set; } = null!;
    public virtual User? AssignedTo { get; set; }
    public virtual TicketStatus Status { get; set; } = null!;
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual Feedback? Feedback { get; set; }

    /// <summary>
    /// Kiểm tra quyền bình luận ủy quyền cho CurrentState (Bảng 7.2 TC-08).
    /// </summary>
    public bool CanComment() => CurrentState.CanComment;

    /// <summary>
    /// Thuộc tính CanComment của State Pattern.
    /// </summary>
    public bool CanCommentProperty => CurrentState.CanComment;

    /// <summary>
    /// Chuyển đổi trạng thái nội bộ được kích hoạt bởi các lớp State cụ thể.
    /// </summary>
    internal void TransitionTo(ITicketState nextState, int changedByUserId)
    {
        var oldStatusId = _statusId;
        _currentState = nextState;
        _statusId = nextState.StatusId;
        UpdatedAt = DateTime.UtcNow;
        RenewConcurrencyToken();

        AddDomainEvent(new TicketStatusChangedEvent(Id, oldStatusId, _statusId, changedByUserId, UpdatedAt));
    }

    // Static Factory Method
    public static Result<Ticket> Create(
        string title,
        string description,
        TicketPriority priority,
        TicketCategory category,
        int creatorId,
        DateTime? currentTime = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Ticket>.Failure(new Error("TICKET_TITLE_REQUIRED", "Tiêu đề yêu cầu không được để trống.", ErrorType.Validation));

        if (title.Length > 150)
            return Result<Ticket>.Failure(new Error("TICKET_TITLE_TOO_LONG", "Tiêu đề không được vượt quá 150 ký tự.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(description))
            return Result<Ticket>.Failure(new Error("TICKET_DESC_REQUIRED", "Mô tả chi tiết không được để trống.", ErrorType.Validation));

        if (!Enum.IsDefined(priority))
            return Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", "Mức độ ưu tiên yêu cầu không hợp lệ.", ErrorType.Validation));

        if (creatorId <= 0)
            return Result<Ticket>.Failure(new Error("TICKET_CREATOR_INVALID", "Người tạo không hợp lệ.", ErrorType.Validation));

        var now = currentTime ?? DateTime.UtcNow;
        var dueAt = CalculateDueAt(priority, now);

        var ticket = new Ticket
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Priority = priority,
            Category = category,
            CreatorId = creatorId,
            StatusId = TicketStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
            DueAt = dueAt,
            ConcurrencyToken = Guid.NewGuid()
        };

        ticket.AddDomainEvent(new TicketCreatedEvent(ticket.Id, ticket.Title, ticket.CreatorId, now));

        return Result<Ticket>.Success(ticket);
    }

    /// <summary>
    /// Tính toán thời hạn cam kết SLA DueAt theo mức độ ưu tiên Priority.
    /// Critical: 1 giờ | High: 4 giờ | Medium: 24 giờ | Low: 72 giờ
    /// </summary>
    public static DateTime CalculateDueAt(TicketPriority priority, DateTime createdAt)
    {
        return priority switch
        {
            TicketPriority.Critical => createdAt.AddHours(1),
            TicketPriority.High     => createdAt.AddHours(4),
            TicketPriority.Medium   => createdAt.AddHours(24),
            TicketPriority.Low      => createdAt.AddHours(72),
            _                       => createdAt.AddHours(24)
        };
    }

    // Business Methods & State Transitions (Ủy quyền hoàn toàn cho CurrentState)
    public Result Assign(int staffId, int leaderId) => CurrentState.Assign(staffId, leaderId);
    public Result Assign(User staff, User? leader = null) => CurrentState.Assign(staff, leader);

    public Result Reassign(int staffId, int leaderId) => CurrentState.Reassign(staffId, leaderId);
    public Result Reassign(User staff, User? leader = null) => CurrentState.Reassign(staff, leader);

    public Result StartProgress(int staffId) => CurrentState.StartProgress(staffId);
    public Result StartProgress(User? staff = null) => CurrentState.StartProgress(staff);

    public Result PutOnHold(int staffId) => CurrentState.HoldForInfo("Tạm dừng chờ phản hồi", staffId);
    public Result HoldForInfo(string reason, int staffId) => CurrentState.HoldForInfo(reason, staffId);
    public Result HoldForInfo(string reason, User? staff = null) => CurrentState.HoldForInfo(reason, staff);

    public Result Resolve(int staffId) => CurrentState.Resolve(staffId);
    public Result Resolve(User? staff = null) => CurrentState.Resolve(staff);

    public Result Close(int userId) => CurrentState.Close(userId);
    public Result Close(Feedback feedback, User? user = null) => CurrentState.Close(feedback, user);

    public Result Reopen(int userId) => CurrentState.Reopen(userId);
    public Result Reopen(string reason, User? user = null) => CurrentState.Reopen(reason, user);

    public Result Reject(string reason, int leaderId) => CurrentState.Reject(reason, leaderId);
    public Result Reject(string reason, User? leader = null) => CurrentState.Reject(reason, leader);

    public void RenewConcurrencyToken()
    {
        ConcurrencyToken = Guid.NewGuid();
    }
}
