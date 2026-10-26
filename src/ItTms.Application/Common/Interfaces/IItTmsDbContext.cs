using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ItTms.Domain.Entities;

namespace ItTms.Application.Common.Interfaces;

/// <summary>
/// Interface trừu tượng hóa DbContext phục vụ tầng Application.
/// Khai tử Generic IRepository<T>, cho phép truy vấn trực tiếp LINQ, AsNoTracking và Projections.
/// </summary>
public interface IItTmsDbContext
{
    DbSet<User> Users { get; }
    DbSet<TicketStatus> TicketStatuses { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Feedback> Feedbacks { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<SystemLog> SystemLogs { get; }

    /// <summary>
    /// Lưu các thay đổi hiện tại vào cơ sở dữ liệu.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cung cấp Database Facade để hỗ trợ quản lý Transaction và các thao tác raw SQL nếu cần.
    /// </summary>
    DatabaseFacade Database { get; }
}
