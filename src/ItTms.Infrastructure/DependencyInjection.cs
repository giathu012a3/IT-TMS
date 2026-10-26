using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ItTms.Application.Common.Interfaces;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Persistence.Interceptors;
using ItTms.Infrastructure.Security;
using ItTms.Infrastructure.Seed;

namespace ItTms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? "Data Source=ittms.db";

        // Đăng ký Interceptor
        services.AddSingleton<ConcurrencyTokenInterceptor>();

        services.AddDbContext<ItTmsDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<ConcurrencyTokenInterceptor>();

            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly(typeof(ItTmsDbContext).Assembly.FullName);
                sqliteOptions.CommandTimeout(30);
            });

            options.AddInterceptors(interceptor);
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging(false);
        });

        // Đăng ký IItTmsDbContext trỏ tới Scoped ItTmsDbContext
        services.AddScoped<IItTmsDbContext>(sp => sp.GetRequiredService<ItTmsDbContext>());

        // Đăng ký DataSeeder
        services.AddScoped<IDataSeeder, DataSeeder>();

        // Đăng ký Password Hasher Service
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        // Đăng ký Realtime Notification Dispatcher & Domain Event Dispatcher
        services.AddScoped<INotificationDispatcher, Realtime.SignalRNotificationDispatcher>();
        services.AddScoped<IDomainEventDispatcher>(sp =>
            new Events.InMemoryDomainEventDispatcher(
                sp,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Events.InMemoryDomainEventDispatcher>>()));

        return services;
    }
}
