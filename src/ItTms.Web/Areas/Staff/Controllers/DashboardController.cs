using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Web.Models.ViewModels;

namespace ItTms.Web.Areas.Staff.Controllers;

public class DashboardController : StaffBaseController
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

        var now = DateTime.UtcNow;
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

        var viewModel = new StaffDashboardViewModel
        {
            AssignedCount = assignedCount,
            InProgressCount = inProgressCount,
            WaitingCount = waitingCount,
            ResolvedTodayCount = resolvedTodayCount,
            UrgentSlaTickets = urgentTickets,
            ActiveTickets = activeTickets
        };

        return View(viewModel);
    }
}
