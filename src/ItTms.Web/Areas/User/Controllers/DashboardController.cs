using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Web.Models.ViewModels;

namespace ItTms.Web.Areas.User.Controllers;

public class DashboardController : UserBaseController
{
    private readonly IItTmsDbContext _dbContext;

    public DashboardController(IItTmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Auth", new { area = "Auth" });

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
            WaitingFeedbackTickets = resolved, // Các vé Resolved cần đánh giá
            RecentTickets = recentTickets
        };

        return View(viewModel);
    }
}
