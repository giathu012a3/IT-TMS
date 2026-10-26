using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Web.Models.ViewModels;

namespace ItTms.Web.Areas.Leader.Controllers;

public class LeaderDashboardController : LeaderBaseController
{
    private readonly IItTmsDbContext _dbContext;

    public LeaderDashboardController(IItTmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
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

        // Thống kê phân bổ tải công việc giữa các Staff
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

        // Thống kê theo danh mục sự cố
        var categoryDistribution = Enum.GetValues<TicketCategory>()
            .ToDictionary(cat => cat, cat => allTickets.Count(t => t.Category == cat));

        // Danh sách ticket khẩn cấp hoặc cận hạn
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
}
