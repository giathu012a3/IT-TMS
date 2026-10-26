using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;





namespace ItTms.Data;

public class DataSeeder : IDataSeeder
{
    private readonly ItTmsDbContext _context;
    private readonly IPasswordHasherService _hasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ItTmsDbContext context,
        IPasswordHasherService hasher,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 1. Seed danh mục 7 Trạng thái Ticket (Idempotent)
        await SeedTicketStatusesAsync(cancellationToken);

        // 2. Seed các tài khoản người dùng mẫu (Idempotent)
        await SeedUsersAsync(cancellationToken);

        // 3. Seed các tickets và comments demo ban đầu (Idempotent)
        await SeedDemoTicketsAsync(cancellationToken);
    }

    private async Task SeedTicketStatusesAsync(CancellationToken cancellationToken)
    {
        if (await _context.TicketStatuses.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("ℹ️ [DATA SEEDER] Danh mục TicketStatuses đã tồn tại. Bỏ qua.");
            return;
        }

        var statuses = new List<TicketStatus>
        {
            TicketStatus.Create(TicketStatus.New, "New", "Mới tạo", "badge bg-primary", 1, "Ticket vừa được User tạo, chờ Leader phân công"),
            TicketStatus.Create(TicketStatus.Assigned, "Assigned", "Đã phân công", "badge bg-info text-dark", 2, "Leader đã giao việc cho Staff phụ trách"),
            TicketStatus.Create(TicketStatus.InProgress, "InProgress", "Đang xử lý", "badge bg-warning text-dark", 3, "Staff đã tiếp nhận và đang tiến hành khắc phục"),
            TicketStatus.Create(TicketStatus.Waiting, "Waiting", "Chờ phản hồi", "badge bg-secondary", 4, "Tạm hoãn chờ thêm thông tin từ User"),
            TicketStatus.Create(TicketStatus.Resolved, "Resolved", "Đã giải quyết", "badge bg-success", 5, "Kỹ thuật viên đã xử lý xong sự cố. Chặn thêm Comment"),
            TicketStatus.Create(TicketStatus.Closed, "Closed", "Đã đóng", "badge bg-dark", 6, "User xác nhận hài lòng và gửi đánh giá chất lượng"),
            TicketStatus.Create(TicketStatus.Rejected, "Rejected", "Đã từ chối", "badge bg-danger", 7, "Leader từ chối ticket không hợp lệ ngay từ trạng thái New")
        };

        await _context.TicketStatuses.AddRangeAsync(statuses, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("✅ [DATA SEEDER] Đã nạp thành công 7 TicketStatuses mặc định.");
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("ℹ️ [DATA SEEDER] Danh sách Users đã tồn tại. Bỏ qua.");
            return;
        }

        var users = new List<User>
        {
            // 1. Admin
            User.Create("admin", "admin@ittms.local", _hasher.HashPassword("Admin@123"), "Quản trị viên Hệ thống", UserRole.Admin, "Ban Quản Trị Hệ Thống", UserStatus.Active).Value,

            // 2. Leader
            User.Create("leader", "leader@ittms.local", _hasher.HashPassword("Leader@123"), "Trưởng nhóm Kỹ thuật IT", UserRole.Leader, "Đội Hỗ Trợ Kỹ Thuật (IT Helpdesk)", UserStatus.Active).Value,

            // 3. Staff 1
            User.Create("staff1", "staff1@ittms.local", _hasher.HashPassword("Staff@123"), "Kỹ thuật viên 1 (Phần cứng & Mạng)", UserRole.Staff, "Đội Hỗ Trợ Kỹ Thuật (IT Helpdesk)", UserStatus.Active).Value,

            // 4. Staff 2
            User.Create("staff2", "staff2@ittms.local", _hasher.HashPassword("Staff@123"), "Kỹ thuật viên 2 (Phần mềm & Hệ thống)", UserRole.Staff, "Đội Hỗ Trợ Kỹ Thuật (IT Helpdesk)", UserStatus.Active).Value,

            // 5. User 1
            User.Create("user1", "user1@ittms.local", _hasher.HashPassword("User@123"), "Nguyễn Văn A (Phòng Kế toán)", UserRole.User, "Phòng Kế Toán - Tài Chính", UserStatus.Active).Value,

            // 6. User 2
            User.Create("user2", "user2@ittms.local", _hasher.HashPassword("User@123"), "Trần Thị B (Phòng Nhân sự)", UserRole.User, "Phòng Nhân Sự & Đào Tạo", UserStatus.Active).Value,

            // 7. Deactivated Account (Kiểm thử Bảng 7.2 TC-01)
            User.Create("deactivated", "deactivated@ittms.local", _hasher.HashPassword("User@123"), "Tài khoản Bị vô hiệu hóa", UserRole.User, "Cán bộ đã thôi việc", UserStatus.Inactive).Value,

            // 8. Alias kiểm thử inactive_user
            User.Create("inactive_user", "inactive_user@ittms.local", _hasher.HashPassword("User@123"), "Tài khoản Khóa Thử Nghiệm", UserRole.User, "Nhân viên tạm khóa", UserStatus.Inactive).Value
        };

        await _context.Users.AddRangeAsync(users, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("✅ [DATA SEEDER] Đã nạp thành công 8 Users mẫu (bao gồm tài khoản Inactive cho TC-01).");
    }

    private async Task SeedDemoTicketsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Tickets.AnyAsync(cancellationToken))
        {
            return;
        }

        var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "user1", cancellationToken);
        var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "user2", cancellationToken);
        var staff1 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "staff1", cancellationToken);
        var staff2 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "staff2", cancellationToken);

        if (user1 == null || staff1 == null) return;

        var utcNow = DateTime.UtcNow;

        // 1. Ticket New
        var ticket1 = Ticket.Create(
            "Máy tính phòng kế toán không bật được nguồn",
            "Bật nút nguồn CPU nhưng đèn không sáng, quạt nguồn không quay. Đã kiểm tra lại ổ cắm điện.",
            TicketPriority.High,
            TicketCategory.Hardware,
            user1.Id,
            utcNow.AddMinutes(-30)
        ).Value;

        // 2. Ticket InProgress
        var ticket2 = Ticket.Create(
            "Mất kết nối Internet toàn bộ khu vực tầng 3",
            "Không truy cập được mạng nội bộ và Internet từ 9h sáng nay. Đèn switch phòng máy nhấp nháy đỏ.",
            TicketPriority.Critical,
            TicketCategory.Network,
            user2?.Id ?? user1.Id,
            utcNow.AddHours(-2)
        ).Value;

        ticket2.Assign(staff1.Id, 2); // leaderId = 2
        ticket2.StartProgress(staff1.Id);

        // 3. Ticket Closed kèm Feedback
        var ticket3 = Ticket.Create(
            "Cài đặt phần mềm VPN làm việc từ xa",
            "Cần hỗ trợ cài đặt cấu hình VPN FortiClient trên laptop cá nhân để truy cập tài liệu công ty.",
            TicketPriority.Medium,
            TicketCategory.Software,
            user1.Id,
            utcNow.AddDays(-1)
        ).Value;

        var assignedStaffId = staff2?.Id ?? staff1.Id;
        ticket3.Assign(assignedStaffId, 2);
        ticket3.StartProgress(assignedStaffId);
        ticket3.Resolve(assignedStaffId);
        ticket3.Close(user1.Id);

        await _context.Tickets.AddRangeAsync(new[] { ticket1, ticket2, ticket3 }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Thêm Feedback cho Ticket 3
        var feedback = Feedback.Create(
            ticket3.Id,
            5,
            "Kỹ thuật viên hỗ trợ rất nhiệt tình và hướng dẫn chi tiết cách kết nối VPN!"
        ).Value;
        await _context.Feedbacks.AddAsync(feedback, cancellationToken);

        // Thêm Comment cho Ticket 2
        var comment1 = Comment.Create(
            ticket2.Id,
            staff1.Id,
            "Chào chị B, em đang kiểm tra switch trung tâm tại phòng server tầng 3, nghi ngờ bị sập nguồn.",
            false
        ).Value;

        var comment2 = Comment.Create(
            ticket2.Id,
            user2?.Id ?? user1.Id,
            "Cảm ơn em, nhờ team xử lý sớm giúp phòng nhân sự chuẩn bị tài liệu phỏng vấn.",
            false
        ).Value;

        await _context.Comments.AddRangeAsync(new[] { comment1, comment2 }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ [DATA SEEDER] Đã nạp thành công 3 Ticket demo kèm Feedback và Comments.");
    }
}
