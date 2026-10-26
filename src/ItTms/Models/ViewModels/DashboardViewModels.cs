

namespace ItTms.Models.ViewModels;

public class UserDashboardViewModel
{
    public int TotalTickets { get; set; }
    public int ProcessingTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int WaitingFeedbackTickets { get; set; }
    public List<TicketListItemViewModel> RecentTickets { get; set; } = [];
}

public class StaffDashboardViewModel
{
    public int AssignedCount { get; set; }
    public int InProgressCount { get; set; }
    public int WaitingCount { get; set; }
    public int ResolvedTodayCount { get; set; }
    public List<TicketListItemViewModel> UrgentSlaTickets { get; set; } = [];
    public List<TicketListItemViewModel> ActiveTickets { get; set; } = [];
}

public class LeaderDashboardViewModel
{
    public int NewCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int BreachedSlaCount { get; set; }
    public double SlaComplianceRate { get; set; }
    public List<StaffWorkloadItemViewModel> StaffWorkloads { get; set; } = [];
    public Dictionary<TicketCategory, int> CategoryDistribution { get; set; } = [];
    public List<TicketListItemViewModel> UrgentTickets { get; set; } = [];
}

public class StaffWorkloadItemViewModel
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ActiveTicketsCount { get; set; }
    public int ResolvedTicketsCount { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTickets { get; set; }
    public int TotalLogs { get; set; }
    public Dictionary<string, int> RoleCounts { get; set; } = [];
    public List<SystemLogItemViewModel> RecentLogs { get; set; } = [];
}
