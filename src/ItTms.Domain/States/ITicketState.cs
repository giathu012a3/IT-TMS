using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Giao diện hợp đồng đại diện cho một trạng thái trong vòng đời của Ticket (GoF State Pattern).
/// Định nghĩa các hành vi nghiệp vụ chuyển trạng thái với Railway-oriented Result return type.
/// </summary>
public interface ITicketState
{
    /// <summary>
    /// Mã định danh trạng thái tương ứng với TicketStatus (1..7).
    /// </summary>
    int StatusId { get; }

    /// <summary>
    /// Tên trạng thái bằng tiếng Anh chuẩn hóa (New, Assigned, InProgress...).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Nhãn hiển thị tiếng Việt (Mới tạo, Đã phân công, Đang xử lý...).
    /// </summary>
    string DisplayLabel { get; }

    /// <summary>
    /// Thuộc tính xác định trạng thái có cho phép thêm bình luận/trao đổi hay không.
    /// Trả về false đối với Resolved, Closed, Rejected (Bảng 7.2 TC-08).
    /// </summary>
    bool CanComment { get; }

    /// <summary>
    /// Phân công ticket cho kỹ thuật viên (Hợp lệ tại NewState).
    /// </summary>
    Result Assign(User staff, User? leader = null);
    Result Assign(int staffId, int leaderId);

    /// <summary>
    /// Phân công lại hoặc điều chuyển ticket cho kỹ thuật viên khác (Hợp lệ tại Assigned, InProgress, Waiting).
    /// </summary>
    Result Reassign(User staff, User? leader = null);
    Result Reassign(int staffId, int leaderId);

    /// <summary>
    /// Kỹ thuật viên tiếp nhận và bắt đầu xử lý ticket (Hợp lệ tại Assigned, Waiting).
    /// </summary>
    Result StartProgress(User? staff = null);
    Result StartProgress(int staffId);

    /// <summary>
    /// Tạm dừng xử lý để chờ người dùng cung cấp thêm thông tin (Hợp lệ tại InProgress).
    /// </summary>
    Result HoldForInfo(string reason, User? staff = null);
    Result HoldForInfo(string reason, int staffId);

    /// <summary>
    /// Hoàn tất khắc phục sự cố kỹ thuật (Hợp lệ tại InProgress, Waiting).
    /// </summary>
    Result Resolve(User? staff = null);
    Result Resolve(int staffId);

    /// <summary>
    /// Người dùng xác nhận kết quả và đóng ticket kèm đánh giá (Chỉ hợp lệ tại Resolved).
    /// </summary>
    Result Close(Feedback feedback, User? user = null);
    Result Close(int userId);

    /// <summary>
    /// Người dùng yêu cầu xử lý lại vì sự cố chưa khắc phục triệt để (Chỉ hợp lệ tại Resolved).
    /// </summary>
    Result Reopen(string reason, User? user = null);
    Result Reopen(int userId);

    /// <summary>
    /// Trưởng nhóm từ chối tiếp nhận yêu cầu không hợp lệ (Chỉ hợp lệ tại New; chặn đứng tại InProgress).
    /// </summary>
    Result Reject(string reason, User? leader = null);
    Result Reject(string reason, int leaderId);
}
