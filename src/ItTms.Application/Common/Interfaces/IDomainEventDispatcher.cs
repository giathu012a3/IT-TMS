using ItTms.Domain.Common;

namespace ItTms.Application.Common.Interfaces;

/// <summary>
/// Hợp đồng điều phối Domain Events theo 2 pha độc lập:
/// Pha 1 (In-transaction): Observers tạo các bản ghi mới (Notification, SystemLog) vào cùng DbContext.
/// Pha 2 (Post-commit): Phát sóng thời gian thực qua SignalR WebSocket.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Pha 1: Thực thi các Observer trong cùng Transaction (Staging Notification & SystemLog vào DbContext).
    /// </summary>
    Task DispatchInTransactionAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pha 2: Thực thi các tác vụ ngoại vi sau khi Transaction đã commit thành công (SignalR WebSocket Broadcast).
    /// </summary>
    Task DispatchPostCommitAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
