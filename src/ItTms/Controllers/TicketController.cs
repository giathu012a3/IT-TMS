namespace ItTms.Controllers;

[Authorize]
public class TicketController : BaseController
{
    private readonly ItTmsDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TicketController> _logger;

    public TicketController(
        ItTmsDbContext dbContext,
        IWebHostEnvironment env,
        ILogger<TicketController> logger)
    {
        _dbContext = dbContext;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    [Route("/Ticket/Dashboard")]
    [Route("/User/Dashboard")]
    [Route("/User/User/Dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth");

        var userId = CurrentUserId.Value;

        var myTicketsQuery = _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.CreatorId == userId);

        var total = await myTicketsQuery.CountAsync();
        var processing = await myTicketsQuery.CountAsync(t => 
            t.StatusId == TicketStatus.New || 
            t.StatusId == TicketStatus.Assigned || 
            t.StatusId == TicketStatus.InProgress || 
            t.StatusId == TicketStatus.Waiting);

        var resolved = await myTicketsQuery.CountAsync(t => t.StatusId == TicketStatus.Resolved);
        var closed = await myTicketsQuery.CountAsync(t => t.StatusId == TicketStatus.Closed);

        var recentTickets = await myTicketsQuery
            .OrderByDescending(t => t.CreatedAt)
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

        var viewModel = new UserDashboardViewModel
        {
            TotalTickets = total,
            ProcessingTickets = processing,
            ResolvedTickets = resolved,
            WaitingFeedbackTickets = resolved,
            RecentTickets = recentTickets
        };

        return View(viewModel);
    }

    [HttpGet]
    [Route("/Ticket")]
    [Route("/Ticket/Index")]
    [Route("/User/Ticket")]
    [Route("/User/Ticket/Index")]
    public async Task<IActionResult> Index(string tab = "all")
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth");

        var userId = CurrentUserId.Value;
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.CreatorId == userId);

        query = tab.ToLowerInvariant() switch
        {
            "active"   => query.Where(t => t.StatusId == TicketStatus.New || 
                                           t.StatusId == TicketStatus.Assigned || 
                                           t.StatusId == TicketStatus.InProgress || 
                                           t.StatusId == TicketStatus.Waiting),
            "resolved" => query.Where(t => t.StatusId == TicketStatus.Resolved),
            "closed"   => query.Where(t => t.StatusId == TicketStatus.Closed),
            "rejected" => query.Where(t => t.StatusId == TicketStatus.Rejected),
            _          => query
        };

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

        ViewBag.ActiveTab = tab;
        return View(tickets);
    }

    [HttpGet]
    [Route("/Ticket/Create")]
    [Route("/User/Ticket/Create")]
    public IActionResult Create()
    {
        return View(new CreateTicketViewModel());
    }

    [HttpPost]
    [Route("/Ticket/Create")]
    [Route("/User/Ticket/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth");

        var userId = CurrentUserId.Value;

        // BẢNG 7.2 TC-03: Khởi tạo ticket ở trạng thái New, tự tính DueAt theo SLA, phát TicketCreatedEvent
        var ticketResult = Ticket.Create(model.Title, model.Description, model.Priority, model.Category, userId);
        if (ticketResult.IsFailure)
        {
            ModelState.AddModelError(string.Empty, ticketResult.Error.Description);
            return View(model);
        }

        var ticket = ticketResult.Value;
        await _dbContext.Tickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        // Xử lý tệp tin đính kèm nếu có
        if (model.Attachment != null && model.Attachment.Length > 0)
        {
            if (model.Attachment.Length > Attachment.MaxFileSizeBytes)
            {
                ModelState.AddModelError(nameof(model.Attachment), "Dung lượng tệp tin vượt quá 16MB.");
                return View(model);
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "tickets");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(model.Attachment.FileName).ToLowerInvariant();
            var physicalFileName = $"{Guid.NewGuid()}{ext}";
            var physicalPath = Path.Combine(uploadsFolder, physicalFileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await model.Attachment.CopyToAsync(stream);
            }

            var attachResult = Attachment.Create(
                ticketId: ticket.Id,
                fileName: physicalFileName,
                originalFileName: Path.GetFileName(model.Attachment.FileName),
                contentType: model.Attachment.ContentType,
                fileSize: model.Attachment.Length
            );

            if (attachResult.IsSuccess)
            {
                await _dbContext.Attachments.AddAsync(attachResult.Value);
                await _dbContext.SaveChangesAsync();
            }
        }

        TempData["SuccessMessage"] = $"Yêu cầu hỗ trợ #{ticket.Id} đã được tạo thành công!";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [HttpGet]
    [Route("/Ticket/Details/{id}")]
    [Route("/User/Ticket/Details/{id}")]
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

        // BOLA Defense: Chỉ người tạo hoặc Leader/Admin/Staff được giao được xem
        if (ticket.CreatorId != CurrentUserId!.Value && 
            ticket.AssignedToId != CurrentUserId.Value && 
            !User.IsInRole(RoleNames.Leader) && 
            !User.IsInRole(RoleNames.Admin))
        {
            _logger.LogWarning("BOLA Defense: User {UserId} cố truy cập ticket {TicketId} của người khác", CurrentUserId, id);
            return Forbid();
        }

        var visibleComments = ticket.Comments
            .Where(c => !c.IsInternal)
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
            Comments = visibleComments,
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
    public async Task<IActionResult> AddComment(int id, string content)
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

        if (ticket == null)
            return NotFound();

        if (ticket.CreatorId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        // BẢNG 7.2 TC-08: Chặn bình luận nếu ticket ở trạng thái Resolved, Closed hoặc Rejected
        if (!ticket.CanComment())
        {
            _logger.LogWarning("TC-08 violation blocked: Cố gắng gửi bình luận trên ticket đã đóng/giải quyết #{TicketId}", id);
            var errorMsg = "Yêu cầu này đã được giải quyết hoặc đóng, không thể gửi thêm bình luận.";
            if (IsAjaxRequest) return BadRequest(new { message = errorMsg });
            TempData["ErrorMessage"] = errorMsg;
            return RedirectToAction(nameof(Details), new { id });
        }

        // Tự động chuyển Waiting -> InProgress khi User phản hồi
        if (ticket.StatusId == TicketStatus.Waiting)
        {
            ticket.StartProgress(CurrentUserId.Value);
        }

        var commentResult = Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal: false);
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

            var authorName = author?.FullName ?? User.Identity?.Name ?? "Người dùng";
            var authorRole = author?.Role.ToString() ?? CurrentUserRole ?? "User";

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
                isInternal = false,
                formattedTime = "Vừa xong"
            };

            return Ok(new
            {
                success = true,
                message = "Bình luận đã gửi thành công.",
                comment = commentDto
            });
        }

        TempData["SuccessMessage"] = "Bình luận đã được gửi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseAndRate(CloseAndRateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn số sao đánh giá hợp lệ (1-5 sao).";
            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == model.TicketId);

        if (ticket == null) return NotFound();

        if (ticket.CreatorId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        // BẢNG 7.2 TC-09: User đóng và đánh giá ticket đang ở trạng thái Resolved
        var feedbackResult = Feedback.Create(model.TicketId, model.Rating, model.Comment);
        if (feedbackResult.IsFailure)
        {
            TempData["ErrorMessage"] = feedbackResult.Error.Description;
            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        var closeResult = ticket.Close(feedbackResult.Value);
        if (closeResult.IsFailure)
        {
            TempData["ErrorMessage"] = closeResult.Error.Description;
            return RedirectToAction(nameof(Details), new { id = model.TicketId });
        }

        await _dbContext.Feedbacks.AddAsync(feedbackResult.Value);

        var log = SystemLog.Create(
            action: "Ticket Closed & Rated",
            details: $"Ticket #{ticket.Id} đã được đóng kèm đánh giá {model.Rating} sao: {model.Comment ?? "Không có nhận xét"}",
            userId: CurrentUserId.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value);
        }

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cảm ơn bạn đã nghiệm thu và đánh giá chất lượng hỗ trợ!";
        return RedirectToAction(nameof(Details), new { id = model.TicketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id, string reason)
    {
        var ticket = await _dbContext.Tickets
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        if (ticket.CreatorId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
            return Forbid();

        var reopenResult = ticket.Reopen(reason);
        if (reopenResult.IsFailure)
        {
            TempData["ErrorMessage"] = reopenResult.Error.Description;
            return RedirectToAction(nameof(Details), new { id });
        }

        var log = SystemLog.Create(
            action: "Ticket Reopened",
            details: $"Ticket #{ticket.Id} đã được mở lại bởi người dùng. Lý do: {reason}",
            userId: CurrentUserId.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Yêu cầu đã được chuyển lại cho kỹ thuật viên tiếp tục xử lý.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await _dbContext.Attachments
            .Include(a => a.Ticket)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null) return NotFound();

        if (attachment.Ticket.CreatorId != CurrentUserId!.Value && 
            attachment.Ticket.AssignedToId != CurrentUserId.Value && 
            !User.IsInRole(RoleNames.Leader) && 
            !User.IsInRole(RoleNames.Admin))
        {
            return Forbid();
        }

        var filePath = Path.Combine(_env.WebRootPath, "uploads", "tickets", attachment.FileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound("Tệp tin không tồn tại trên máy chủ lưu trữ.");

        return PhysicalFile(filePath, attachment.ContentType, attachment.OriginalFileName);
    }
}
