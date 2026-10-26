using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi một tài khoản người dùng bị Admin chuyển trạng thái sang Inactive.
/// </summary>
public sealed record UserDeactivatedEvent(
    int UserId,
    int DeactivatedById,
    string? Reason,
    DateTime OccurredOn
) : IDomainEvent;
