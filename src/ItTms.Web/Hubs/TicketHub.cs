using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Web.Models.Realtime;

namespace ItTms.Web.Hubs;

/// <summary>
/// Hub SignalR quản lý kết nối WebSocket hai chiều cho thảo luận Ticket và thông báo đẩy.
/// Route: /hubs/ticket
/// Triệt tiêu 100% cơ chế Polling 3s.
/// </summary>
[Authorize]
public class TicketHub : Hub
{
    private readonly IItTmsDbContext _dbContext;
    private readonly ILogger<TicketHub> _logger;

    public TicketHub(IItTmsDbContext dbContext, ILogger<TicketHub> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Khi Client kết nối: Tự động ghi danh vào user_{userId} và role_{roleName}.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity != null && user.Identity.IsAuthenticated)
        {
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            if (int.TryParse(userIdStr, out int userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogDebug("Client {ConnectionId} tham gia nhóm user_{UserId}", Context.ConnectionId, userId);
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");
                _logger.LogDebug("Client {ConnectionId} tham gia nhóm role_{Role}", Context.ConnectionId, role);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client {ConnectionId} ngắt kết nối SignalR: {Error}", 
            Context.ConnectionId, exception?.Message ?? "Bình thường");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Tham gia phòng thảo luận của Ticket với cơ chế kiểm tra BOLA / IDOR chặt chẽ.
    /// </summary>
    public async Task JoinTicketGroup(int ticketId)
    {
        var user = Context.User;
        if (user == null) throw new HubException("Yêu cầu xác thực danh tính.");

        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        if (!int.TryParse(userIdStr, out int currentUserId))
            throw new HubException("Định danh người dùng không hợp lệ.");

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.CreatorId, t.AssignedToId })
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new HubException($"Không tìm thấy yêu cầu hỗ trợ #{ticketId}.");

        // Nguyên tắc phân quyền Resource-Based (Bảng 7.2 TC-07)
        bool hasAccess = false;
        if (role == RoleNames.Admin || role == RoleNames.Leader)
        {
            hasAccess = true;
        }
        else if (role == RoleNames.Staff && ticket.AssignedToId == currentUserId)
        {
            hasAccess = true;
        }
        else if (role == RoleNames.User && ticket.CreatorId == currentUserId)
        {
            hasAccess = true;
        }

        if (!hasAccess)
        {
            _logger.LogWarning("BOLA SIGNALR VIOLATION: Người dùng #{UserId} ({Role}) cố truy cập Ticket #{TicketId}",
                currentUserId, role, ticketId);
            throw new HubException("Bạn không có quyền truy cập vào nội dung trao đổi của yêu cầu này.");
        }

        // Hỗ trợ cả 2 định dạng nhóm: "ticket_123" và "Ticket_123" để tương thích tối đa
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Ticket_{ticketId}");

        if (role == RoleNames.Staff || role == RoleNames.Leader || role == RoleNames.Admin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket_{ticketId}_internal");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Ticket_{ticketId}_internal");
        }

        _logger.LogInformation("Người dùng #{UserId} ({Role}) đã tham gia phòng ticket_{TicketId}", currentUserId, role, ticketId);
    }

    /// <summary>
    /// Rời khỏi phòng thảo luận khi rời khỏi trang chi tiết ticket.
    /// </summary>
    public async Task LeaveTicketGroup(int ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Ticket_{ticketId}");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket_{ticketId}_internal");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Ticket_{ticketId}_internal");
        _logger.LogDebug("Client {ConnectionId} đã rời phòng ticket_{TicketId}", Context.ConnectionId, ticketId);
    }

    /// <summary>
    /// Gửi bình luận trực tiếp qua WebSocket (hỗ trợ client gửi qua Hub hoặc HTTP POST).
    /// </summary>
    public async Task SendComment(int ticketId, string message, bool isInternal = false)
    {
        var user = Context.User;
        if (user == null) throw new HubException("Yêu cầu xác thực danh tính.");

        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int currentUserId))
            throw new HubException("Định danh người dùng không hợp lệ.");

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
            throw new HubException($"Không tìm thấy ticket #{ticketId}.");

        // Ràng buộc Bảng 7.2 TC-08: Chặn bình luận nếu Resolved, Closed hoặc Rejected
        if (!ticket.CanComment())
            throw new HubException("Yêu cầu này đã hoàn tất hoặc bị khóa, không thể gửi thêm bình luận.");

        // BOLA Check
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        bool isStaffOrLeaderOrAdmin = role == RoleNames.Staff || role == RoleNames.Leader || role == RoleNames.Admin;
        if (role == RoleNames.Staff && ticket.AssignedToId != currentUserId)
            throw new HubException("Bạn không được phân công xử lý yêu cầu này.");
        if (role == RoleNames.User && ticket.CreatorId != currentUserId)
            throw new HubException("Bạn không phải người tạo yêu cầu này.");

        // Chỉ Staff, Leader, Admin mới được gửi ghi chú nội bộ
        if (isInternal && !isStaffOrLeaderOrAdmin)
            isInternal = false;

        var commentResult = Comment.Create(ticketId, currentUserId, message, isInternal);
        if (commentResult.IsFailure)
            throw new HubException(commentResult.Error.Description);

        await _dbContext.Comments.AddAsync(commentResult.Value);
        await _dbContext.SaveChangesAsync();

        var author = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);

        var dto = new CommentDto
        {
            Id = commentResult.Value.Id,
            TicketId = ticketId,
            AuthorUserId = currentUserId,
            AuthorName = author?.FullName ?? user.Identity?.Name ?? "Người dùng",
            AuthorRole = author?.Role.ToString() ?? role ?? "User",
            Content = message,
            IsInternal = isInternal,
            CreatedAt = commentResult.Value.CreatedAt
        };

        if (isInternal)
        {
            await Clients.Group($"ticket_{ticketId}_internal").SendAsync("ReceiveComment", dto);
            await Clients.Group($"Ticket_{ticketId}_internal").SendAsync("ReceiveComment", dto);
        }
        else
        {
            await Clients.Group($"ticket_{ticketId}").SendAsync("ReceiveComment", dto);
            await Clients.Group($"Ticket_{ticketId}").SendAsync("ReceiveComment", dto);
        }
    }
}
