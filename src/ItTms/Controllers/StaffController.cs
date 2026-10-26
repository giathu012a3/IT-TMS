namespace ItTms.Controllers;

[AuthorizeRoles(RoleNames.Staff, RoleNames.Leader, RoleNames.Admin)]
public class StaffController : BaseController
{
    private readonly ItTmsDbContext _dbContext;
    private readonly ILogger<StaffController> _logger;

    public StaffController(
        ItTmsDbContext dbContext,
        ILogger<StaffController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [Route("/Staff/Dashboard")]
    [Route("/Staff")]
    [Route("/Staff/Staff/Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth");

        var staffId = CurrentUserId.Value;

        var myAssigned = _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedToId == staffId);

        var assignedCount = await myAssigned.CountAsync(t => t.StatusId == TicketStatus.Assigned);
        var inProgressCount = await myAssigned.CountAsync(t => t.StatusId == TicketStatus.InProgress);
        var waitingCount = await myAssigned.CountAsync(t => t.StatusId == TicketStatus.Waiting);

        var today = DateTime.UtcNow.Date;
        var resolvedTodayCount = await myAssigned.CountAsync(t => 
            (t.StatusId == TicketStatus.Resolved || t.StatusId == TicketStatus.Closed) &&
            t.ResolvedAt >= today);

        var urgentTickets = await myAssigned
            .Where(t => t.StatusId == TicketStatus.Assigned || t.StatusId == TicketStatus.InProgress)
            .OrderBy(t => t.DueAt)
            .Take(5)
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

        var activeTickets = await myAssigned
            .Where(t => t.StatusId == TicketStatus.Assigned || t.StatusId == TicketStatus.InProgress || t.StatusId == TicketStatus.Waiting)
            .OrderByDescending(t => t.UpdatedAt)
            .Take(10)
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

        var vm = new StaffDashboardViewModel
        {
            AssignedCount = assignedCount,
            InProgressCount = inProgressCount,
            WaitingCount = waitingCount,
            ResolvedTodayCount = resolvedTodayCount,
            UrgentSlaTickets = urgentTickets,
            ActiveTickets = activeTickets
        };

        return View(vm);
    }

    // BẢNG 7.2 TC-07: Staff chỉ xem danh sách ticket phân công cho mình (AssignedToId == CurrentUserId)
    [HttpGet]
    [Route("/Staff/Assigned")]
    [Route("/Staff/Ticket/AssignedQueue")]
    [Route("/Staff/StaffTicket/Assigned")]
    public async Task<IActionResult> Assigned(string tab = "active")
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth");

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

    [HttpGet]
    [Route("/Staff/Details/{id}")]
    [Route("/Staff/StaffTicket/Details/{id}")]
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
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        var log = SystemLog.Create(
            action: "Ticket InProgress",
            details: $"Staff #{CurrentUserId.Value} bắt đầu xử lý ticket #{ticket.Id}",
            userId: CurrentUserId.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã tiếp nhận xử lý yêu cầu.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoldForInfo(int id, string reason)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        var result = ticket.HoldForInfo(reason);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        var log = SystemLog.Create(
            action: "Ticket Waiting",
            details: $"Ticket #{ticket.Id} chuyển sang Chờ thông tin: {reason}",
            userId: CurrentUserId.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật trạng thái Chờ thông tin phản hồi.";
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

        var log = SystemLog.Create(
            action: "Ticket Resolved",
            details: $"Staff #{CurrentUserId.Value} đã giải quyết thành công ticket #{ticket.Id}",
            userId: CurrentUserId.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã đánh dấu yêu cầu là Đã giải quyết thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string content, bool isInternal = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            if (IsAjaxRequest) return BadRequest(new { message = "Nội dung bình luận không được để trống." });
            TempData["ErrorMessage"] = "Nội dung bình luận không được để trống.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        if (!ticket.CanComment())
        {
            var msg = "Ticket đã hoàn tất hoặc bị từ chối, không thể thêm bình luận.";
            if (IsAjaxRequest) return BadRequest(new { message = msg });
            TempData["ErrorMessage"] = msg;
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
                message = isInternal ? "Ghi chú nội bộ đã được lưu." : "Bình luận đã gửi thành công.",
                comment = commentDto
            });
        }

        TempData["SuccessMessage"] = isInternal ? "Đã lưu ghi chú nội bộ." : "Bình luận đã được gửi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Route("/Staff/History")]
    [Route("/Staff/Ticket/History")]
    [Route("/Staff/StaffTicket/History")]
    public async Task<IActionResult> History()
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth");

        var staffId = CurrentUserId.Value;

        var tickets = await _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.Status)
            .Include(t => t.Creator)
            .Include(t => t.Feedback)
            .Where(t => t.AssignedToId == staffId && 
                       (t.StatusId == TicketStatus.Resolved || t.StatusId == TicketStatus.Closed))
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

        return View(tickets);
    }
}
