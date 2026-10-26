using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Infrastructure.Persistence;

public class ItTmsDbContext : DbContext, IItTmsDbContext
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public ItTmsDbContext(
        DbContextOptions<ItTmsDbContext> options,
        IDomainEventDispatcher? domainEventDispatcher = null) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0 || _domainEventDispatcher == null)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        // Pha 1: Kích hoạt In-Transaction Observers (Ghi SystemLog & Notification)
        await _domainEventDispatcher.DispatchInTransactionAsync(domainEvents, cancellationToken);

        // Lưu toàn bộ thay đổi xuống SQLite trong một giao dịch nguyên tử duy nhất
        var result = await base.SaveChangesAsync(cancellationToken);

        // Pha 2: Kích hoạt Post-Commit Handlers (SignalR WebSocket Push)
        await _domainEventDispatcher.DispatchPostCommitAsync(domainEvents, cancellationToken);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động quét và áp dụng toàn bộ các cấu hình IEntityTypeConfiguration<T> trong Assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ItTmsDbContext).Assembly);

        // Xử lý lưu trữ chuẩn UTC DateTime cho SQLite (SQLite không lưu offset timezone)
        ApplyUtcDateTimeConverter(modelBuilder);
    }

    private static void ApplyUtcDateTimeConverter(ModelBuilder modelBuilder)
    {
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
            v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}
