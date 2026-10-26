using ItTms.Domain.Common;

namespace ItTms.Domain.States;

/// <summary>
/// Danh mục lỗi nghiệp vụ chuẩn hóa cho State Pattern và thực thể Ticket.
/// Áp dụng Railway-Oriented Programming (Zero Exceptions for Business Logic).
/// </summary>
public static class TicketErrors
{
    public static Error InvalidTransition(string currentState, string action) =>
        new("INVALID_STATE_TRANSITION", 
            $"Không thể thực hiện thao tác '{action}' khi ticket đang ở trạng thái '{currentState}'.", 
            ErrorType.Conflict);

    public static readonly Error CannotRejectInProgress =
        new("CANNOT_REJECT_IN_PROGRESS", 
            "Chỉ có thể từ chối ticket mới tạo ở hàng đợi phân công (New). Không thể từ chối ticket đang xử lý.", 
            ErrorType.Conflict);

    public static readonly Error CommentsBlockedOnResolved =
        new("COMMENTS_BLOCKED_ON_RESOLVED", 
            "Yêu cầu đã được giải quyết (Resolved), không thể thêm bình luận mới.", 
            ErrorType.Conflict);

    public static readonly Error TicketAlreadyClosed =
        new("TICKET_ALREADY_CLOSED", 
            "Ticket đã đóng hoàn tất (Closed), không thể thay đổi trạng thái hoặc thực hiện thêm thao tác.", 
            ErrorType.Conflict);

    public static readonly Error TicketAlreadyRejected =
        new("TICKET_ALREADY_REJECTED", 
            "Ticket đã bị từ chối tiếp nhận (Rejected), toàn bộ tiến trình đã bị khóa vĩnh viễn.", 
            ErrorType.Conflict);

    public static readonly Error RejectionReasonRequired =
        new("REJECTION_REASON_REQUIRED", 
            "Bắt buộc nhập lý do từ chối tiếp nhận yêu cầu.", 
            ErrorType.Validation);

    public static readonly Error ReopenReasonRequired =
        new("REOPEN_REASON_REQUIRED", 
            "Bắt buộc nhập lý do yêu cầu mở lại ticket.", 
            ErrorType.Validation);

    public static readonly Error HoldReasonRequired =
        new("HOLD_REASON_REQUIRED", 
            "Bắt buộc nhập lý do hoặc nội dung cần người dùng bổ sung thông tin.", 
            ErrorType.Validation);

    public static readonly Error FeedbackRequired =
        new("FEEDBACK_REQUIRED", 
            "Bắt buộc cung cấp thông tin đánh giá dịch vụ khi đóng ticket.", 
            ErrorType.Validation);

    public static readonly Error InvalidStaff =
        new("STAFF_ID_INVALID", 
            "Kỹ thuật viên được giao không hợp lệ.", 
            ErrorType.Validation);

    public static readonly Error UnauthorizedStaff =
        new("UNAUTHORIZED_STAFF", 
            "Chỉ kỹ thuật viên được phân công mới có quyền tiếp nhận xử lý.", 
            ErrorType.Forbidden);

    public static readonly Error UnauthorizedUser =
        new("UNAUTHORIZED_USER", 
            "Chỉ người tạo ticket mới có quyền thực hiện thao tác này.", 
            ErrorType.Forbidden);
}
