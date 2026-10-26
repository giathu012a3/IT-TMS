using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Web.Filters;
using ItTms.Web.Models.ViewModels;

namespace ItTms.Web.Areas.Staff.Controllers;

public class StaffTicketController : StaffBaseController
{
    private readonly IItTmsDbContext _dbContext;
    private readonly ILogger<StaffTicketController> _logger;

    public StaffTicketController(
        IItTmsDbContext dbContext,
        ILogger<StaffTicketController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    // BẢNG 7.2 TC-07: Staff chỉ xem danh sách ticket phân công cho mình (AssignedToId == CurrentUserId)
    [HttpGet]
    public async Task<IActionResult> Assigned(string tab = "active")
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth", new { area = "Auth" });

        var staffId = CurrentUserId.Value;
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedToId == staffId);

        query = tab.ToLowerInvariant() switch
        {
            "resolved" => query.Where(t => t.StatusId == TicketStatus.Resolved),
            "closed"   => query.Where(t => t.StatusId == TicketStatus.Closed),
            "all"      => query,
            _          => query.Where(t => t.StatusId == TicketStatus.Assigned || 
                                           t.StatusId == TicketStatus.InProgress || 
                                           t.StatusId == TicketStatus.Waiting)
        };

        var tickets = await query
            .OrderBy(t => t.DueAt)
            .Select(t => new TicketListItemViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                Category = t.Category,
                StatusId = t.StatusId,
                StatusName = t.Status.Name,
                StatusLabel = t.Status.Label,
                StatusColorClass = t.Status.ColorClass,
                CreatorId = t.CreatorId,
                CreatorName = t.Creator.FullName,
                AssignedToId = t.AssignedToId,
                AssignedToName = t.AssignedTo != null ? t.AssignedTo.FullName : null,
                CreatedAt = t.CreatedAt,
                DueAt = t.DueAt,
                ResolvedAt = t.ResolvedAt
            })
            .ToListAsync();

        ViewBag.ActiveTab = tab;
        return View(tickets);
    }

    // BẢNG 7.2 TC-07: Staff A truy cập ticket của Staff B -> Trả về HTTP 403 Forbidden
    [HttpGet]
    [AuthorizeTicketAccess("id")]
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .Include(t => t.Creator)
            .Include(t => t.AssignedTo)
            .Include(t => t.Attachments)
            .Include(t => t.Feedback)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
        {
            _logger.LogWarning("BOLA Violation Blocked: Staff {UserId} cố truy cập Ticket {TicketId} của Staff khác", CurrentUserId, id);
            return Forbid();
        }

        var comments = ticket.Comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentItemViewModel
            {
                Id = c.Id,
                UserId = c.UserId,
                AuthorName = c.User.FullName,
                AuthorRole = c.User.Role.ToString(),
                Content = c.Content,
                IsInternal = c.IsInternal,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        var attachments = ticket.Attachments
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentItemViewModel
            {
                Id = a.Id,
                FileName = a.FileName,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                CreatedAt = a.CreatedAt
            })
            .ToList();

        var model = new TicketDetailsViewModel
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority,
            Category = ticket.Category,
            StatusId = ticket.StatusId,
            StatusName = ticket.Status.Name,
            StatusLabel = ticket.Status.Label,
            StatusColorClass = ticket.Status.ColorClass,
            CanComment = ticket.CanComment(),
            CreatorId = ticket.CreatorId,
            CreatorName = ticket.Creator.FullName,
            CreatorEmail = ticket.Creator.Email,
            CreatorDepartment = ticket.Creator.Department,
            AssignedToId = ticket.AssignedToId,
            AssignedToName = ticket.AssignedTo?.FullName,
            AssignedToEmail = ticket.AssignedTo?.Email,
            CreatedAt = ticket.CreatedAt,
            DueAt = ticket.DueAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            RejectionReason = ticket.RejectionReason,
            Comments = comments,
            Attachments = attachments,
            Feedback = ticket.Feedback != null ? new FeedbackItemViewModel
            {
                Id = ticket.Feedback.Id,
                Rating = ticket.Feedback.Rating,
                Comment = ticket.Feedback.Comment,
                CreatedAt = ticket.Feedback.CreatedAt
            } : null
        };

        return View(model);
    }

    // BẢNG 7.2 TC-05: Chuyển sang InProgress (chặn nếu sai quy trình State Machine)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProgress(int id)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        var result = ticket.StartProgress(CurrentUserId.Value);
        if (result.IsFailure)
        {
            _logger.LogWarning("TC-05: State Pattern chặn chuyển trạng thái không hợp lệ trên ticket #{TicketId}: {Error}", id, result.Error.Description);
            if (IsAjaxRequest) return BadRequest(new { message = result.Error.Description });
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _dbContext.SaveChangesAsync();

        if (IsAjaxRequest) return Ok(new { success = true, message = "Đã bắt đầu xử lý yêu cầu." });
        TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Đang xử lý.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoldForInfo(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do tạm dừng yêu cầu.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        var result = ticket.HoldForInfo(reason, CurrentUserId.Value);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã chuyển trạng thái: Chờ thông tin từ người dùng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        var result = ticket.Resolve(CurrentUserId.Value);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã giải quyết yêu cầu thành công. Chờ người dùng nghiệm thu.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string content, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            if (IsAjaxRequest) return BadRequest(new { message = "Nội dung phản hồi không được để trống." });
            TempData["ErrorMessage"] = "Nội dung phản hồi không được để trống.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        // BẢNG 7.2 TC-08: Chặn bình luận nếu Resolved, Closed hoặc Rejected
        if (!ticket.CanComment())
        {
            _logger.LogWarning("TC-08 violation blocked: Staff cố gửi bình luận trên ticket đã giải quyết/đóng #{TicketId}", id);
            var errorMsg = "Yêu cầu này đã được giải quyết hoặc đóng, không thể gửi thêm bình luận.";
            if (IsAjaxRequest) return BadRequest(new { message = errorMsg });
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Details), new { id });
        }

        var commentResult = Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal);
        if (commentResult.IsFailure)
        {
            if (IsAjaxRequest) return BadRequest(new { message = commentResult.Error.Description });
            TempData["ErrorMessage"] = commentResult.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        var comment = commentResult.Value;
        ticket.Comments.Add(comment);
        await _dbContext.Comments.AddAsync(comment);
        await _dbContext.SaveChangesAsync();

        // Gắn Domain Event vào Ticket với đúng CommentId vừa sinh từ CSDL
        ticket.AddDomainEvent(new CommentAddedEvent(
            comment.Id,
            ticket.Id,
            CurrentUserId.Value,
            comment.Content,
            comment.IsInternal,
            comment.CreatedAt));

        await _dbContext.SaveChangesAsync();

        if (IsAjaxRequest)
        {
            var author = await _dbContext.Users.AsNoTracking()
                .Where(u => u.Id == CurrentUserId.Value)
                .Select(u => new { u.FullName, u.Role })
                .FirstOrDefaultAsync();

            var authorName = author?.FullName ?? User.Identity?.Name ?? "Kỹ thuật viên";
            var authorRole = author?.Role.ToString() ?? CurrentUserRole ?? "Staff";

            var commentDto = new
            {
                id = comment.Id,
                ticketId = ticket.Id,
                authorId = CurrentUserId.Value,
                authorUserId = CurrentUserId.Value,
                authorName,
                authorRole,
                content = comment.Content,
                createdAt = comment.CreatedAt,
                isInternal = comment.IsInternal,
                formattedTime = "Vừa xong"
            };

            return Ok(new
            {
                success = true,
                message = isInternal ? "Đã lưu ghi chú nội bộ." : "Bình luận đã gửi.",
                comment = commentDto
            });
        }

        TempData["SuccessMessage"] = isInternal ? "Đã lưu ghi chú nội bộ." : "Đã gửi phản hồi tới người dùng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth", new { area = "Auth" });

        var staffId = CurrentUserId.Value;
        var historyTickets = await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedToId == staffId && (t.StatusId == TicketStatus.Resolved || t.StatusId == TicketStatus.Closed))
            .OrderByDescending(t => t.ResolvedAt ?? t.UpdatedAt)
            .Select(t => new TicketListItemViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                Category = t.Category,
                StatusId = t.StatusId,
                StatusName = t.Status.Name,
                StatusLabel = t.Status.Label,
                StatusColorClass = t.Status.ColorClass,
                CreatorId = t.CreatorId,
                CreatorName = t.Creator.FullName,
                AssignedToId = t.AssignedToId,
                AssignedToName = t.AssignedTo != null ? t.AssignedTo.FullName : null,
                CreatedAt = t.CreatedAt,
                DueAt = t.DueAt,
                ResolvedAt = t.ResolvedAt,
                FeedbackRating = t.Feedback != null ? t.Feedback.Rating : null
            })
            .ToListAsync();

        return View(historyTickets);
    }
}
