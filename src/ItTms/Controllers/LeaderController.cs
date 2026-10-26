namespace ItTms.Controllers;

[AuthorizeRoles(RoleNames.Leader, RoleNames.Admin)]
public class LeaderController : BaseController
{
    private readonly ItTmsDbContext _dbContext;
    private readonly ILogger<LeaderController> _logger;

    public LeaderController(
        ItTmsDbContext dbContext,
        ILogger<LeaderController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [Route("/Leader/Dashboard")]
    [Route("/Leader")]
    [Route("/Leader/Leader/Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var now = DateTime.UtcNow;

        var allTickets = await _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.Status)
            .Include(t => t.Creator)
            .Include(t => t.AssignedTo)
            .ToListAsync();

        var newCount = allTickets.Count(t => t.StatusId == TicketStatus.New);
        var inProgressCount = allTickets.Count(t => 
            t.StatusId == TicketStatus.Assigned || 
            t.StatusId == TicketStatus.InProgress || 
            t.StatusId == TicketStatus.Waiting);

        var resolvedCount = allTickets.Count(t => t.StatusId == TicketStatus.Resolved || t.StatusId == TicketStatus.Closed);

        var breachedSlaCount = allTickets.Count(t => 
            (t.ResolvedAt.HasValue && t.ResolvedAt.Value > t.DueAt) ||
            (!t.ResolvedAt.HasValue && t.StatusId != TicketStatus.Closed && t.StatusId != TicketStatus.Rejected && now > t.DueAt));

        var total = allTickets.Count;
        var slaComplianceRate = total > 0 ? Math.Round((double)(total - breachedSlaCount) / total * 100, 1) : 100.0;

        var staffUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Staff && u.Status == UserStatus.Active)
            .ToListAsync();

        var staffWorkloads = staffUsers.Select(s => new StaffWorkloadItemViewModel
        {
            StaffId = s.Id,
            StaffName = s.FullName,
            Email = s.Email,
            ActiveTicketsCount = allTickets.Count(t => t.AssignedToId == s.Id && 
                (t.StatusId == TicketStatus.Assigned || t.StatusId == TicketStatus.InProgress || t.StatusId == TicketStatus.Waiting)),
            ResolvedTicketsCount = allTickets.Count(t => t.AssignedToId == s.Id && 
                (t.StatusId == TicketStatus.Resolved || t.StatusId == TicketStatus.Closed))
        }).OrderBy(s => s.ActiveTicketsCount).ToList();

        var categoryDistribution = Enum.GetValues<TicketCategory>()
            .ToDictionary(cat => cat, cat => allTickets.Count(t => t.Category == cat));

        var urgentTickets = allTickets
            .Where(t => t.StatusId == TicketStatus.New || t.StatusId == TicketStatus.Assigned || t.StatusId == TicketStatus.InProgress)
            .OrderBy(t => t.DueAt)
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
                AssignedToName = t.AssignedTo?.FullName,
                CreatedAt = t.CreatedAt,
                DueAt = t.DueAt,
                ResolvedAt = t.ResolvedAt
            })
            .ToList();

        var model = new LeaderDashboardViewModel
        {
            NewCount = newCount,
            InProgressCount = inProgressCount,
            ResolvedCount = resolvedCount,
            BreachedSlaCount = breachedSlaCount,
            SlaComplianceRate = slaComplianceRate,
            StaffWorkloads = staffWorkloads,
            CategoryDistribution = categoryDistribution,
            UrgentTickets = urgentTickets
        };

        return View(model);
    }

    [HttpGet]
    [Route("/Leader/NewQueue")]
    [Route("/Leader/Ticket/AssignQueue")]
    [Route("/Leader/LeaderTicket/NewQueue")]
    public async Task<IActionResult> NewQueue()
    {
        var newTickets = await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.StatusId == TicketStatus.New)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
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
                CreatedAt = t.CreatedAt,
                DueAt = t.DueAt
            })
            .ToListAsync();

        var staffList = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Staff && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                ActiveCount = _dbContext.Tickets.Count(t => t.AssignedToId == u.Id && 
                    (t.StatusId == TicketStatus.Assigned || t.StatusId == TicketStatus.InProgress || t.StatusId == TicketStatus.Waiting))
            })
            .ToListAsync();

        ViewBag.StaffList = staffList;
        return View(newTickets);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu phân công không hợp lệ.";
            return RedirectToAction(nameof(NewQueue));
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == model.TicketId);

        if (ticket == null) return NotFound();

        var staff = await _dbContext.Users.FindAsync(model.StaffId);
        if (staff == null || staff.Status != UserStatus.Active || staff.Role != UserRole.Staff)
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên không tồn tại hoặc không ở trạng thái sẵn sàng.";
            return RedirectToAction(nameof(NewQueue));
        }

        var result = ticket.Assign(model.StaffId, CurrentUserId!.Value);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(NewQueue));
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã phân công ticket #{ticket.Id} cho kỹ thuật viên {staff.FullName}.";
        return RedirectToAction(nameof(NewQueue));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối yêu cầu (tối thiểu 5 ký tự).";
            return RedirectToAction(nameof(NewQueue));
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == model.TicketId);

        if (ticket == null) return NotFound();

        var result = ticket.Reject(model.Reason, CurrentUserId!.Value);
        if (result.IsFailure)
        {
            _logger.LogWarning("TC-06: State Pattern chặn từ chối ticket #{TicketId} vì ở trạng thái không cho phép: {Error}", 
                model.TicketId, result.Error.Description);

            if (IsAjaxRequest) return BadRequest(new { message = result.Error.Description });
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        var log = SystemLog.Create(
            action: "Ticket Rejected",
            details: $"Leader đã từ chối tiếp nhận ticket #{ticket.Id}. Lý do: {model.Reason}",
            userId: CurrentUserId.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã từ chối ticket #{ticket.Id}.";
        return RedirectToAction(nameof(NewQueue));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(ReassignTicketViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu điều chuyển không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == model.TicketId);

        if (ticket == null) return NotFound();

        var newStaff = await _dbContext.Users.FindAsync(model.NewStaffId);
        if (newStaff == null || newStaff.Status != UserStatus.Active)
        {
            TempData["ErrorMessage"] = "Kỹ thuật viên mới không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        var result = ticket.Reassign(model.NewStaffId, CurrentUserId!.Value);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Description;
            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã điều chuyển ticket #{ticket.Id} sang kỹ thuật viên {newStaff.FullName}.";
        return RedirectToAction(nameof(Details), new { id = model.TicketId });
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int? statusId, TicketCategory? category, int? staffId)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Include(t => t.Status)
            .Include(t => t.Creator)
            .Include(t => t.AssignedTo)
            .Include(t => t.Feedback)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            query = query.Where(t => t.Title.ToLower().Contains(k) || 
                                     t.Creator.FullName.ToLower().Contains(k) || 
                                     (t.AssignedTo != null && t.AssignedTo.FullName.ToLower().Contains(k)));
        }

        if (statusId.HasValue)
        {
            query = query.Where(t => t.StatusId == statusId.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        if (staffId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == staffId.Value);
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
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

        ViewBag.StaffList = await _dbContext.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Staff && u.Status == UserStatus.Active)
            .ToListAsync();
        ViewBag.Statuses = await _dbContext.TicketStatuses.AsNoTracking().ToListAsync();
        ViewBag.CurrentKeyword = keyword;
        ViewBag.CurrentStatus = statusId;
        ViewBag.CurrentCategory = category;
        ViewBag.CurrentStaff = staffId;

        return View(tickets);
    }

    [HttpGet]
    [Route("/Leader/Details/{id}")]
    [Route("/Leader/LeaderTicket/Details/{id}")]
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

        if (ticket == null) return NotFound();

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

        ViewBag.StaffList = await _dbContext.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Staff && u.Status == UserStatus.Active)
            .ToListAsync();

        return View(model);
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

        if (!ticket.CanComment())
        {
            _logger.LogWarning("TC-08 violation blocked: Leader cố gửi bình luận trên ticket đã giải quyết/đóng #{TicketId}", id);
            var errorMsg = "Yêu cầu này đã được giải quyết hoặc đóng, không thể gửi thêm bình luận.";
            if (IsAjaxRequest) return BadRequest(new { message = errorMsg });
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Details), new { id });
        }

        var commentResult = Comment.Create(ticket.Id, CurrentUserId!.Value, content, isInternal);
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
            CurrentUserId!.Value,
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

            var authorName = author?.FullName ?? User.Identity?.Name ?? "Trưởng nhóm IT";
            var authorRole = author?.Role.ToString() ?? CurrentUserRole ?? "Leader";

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

        TempData["SuccessMessage"] = isInternal ? "Đã lưu ghi chú nội bộ." : "Đã gửi phản hồi.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
