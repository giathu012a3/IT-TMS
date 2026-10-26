using Microsoft.Extensions.DependencyInjection;
using ItTms.Application.Common.Interfaces;
using ItTms.Application.Observers;
using ItTms.Domain.Events;

namespace ItTms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký Observers cho Domain Events
        services.AddScoped<NotificationObserver>();
        services.AddScoped<IDomainEventObserver<TicketCreatedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
        services.AddScoped<IDomainEventObserver<TicketAssignedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
        services.AddScoped<IDomainEventObserver<TicketStatusChangedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
        services.AddScoped<IDomainEventObserver<CommentAddedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
        services.AddScoped<IDomainEventObserver<TicketClosedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());

        services.AddScoped<SystemLogObserver>();
        services.AddScoped<IDomainEventObserver<TicketCreatedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
        services.AddScoped<IDomainEventObserver<TicketAssignedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
        services.AddScoped<IDomainEventObserver<TicketStatusChangedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
        services.AddScoped<IDomainEventObserver<CommentAddedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
        services.AddScoped<IDomainEventObserver<TicketClosedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
        services.AddScoped<IDomainEventObserver<UserDeactivatedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());

        return services;
    }
}
