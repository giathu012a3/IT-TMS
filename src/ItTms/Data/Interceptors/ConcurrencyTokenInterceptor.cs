
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ItTms.Data.Interceptors;

public class ConcurrencyTokenInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        UpdateConcurrencyTokens(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyTokens(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateConcurrencyTokens(DbContext? context)
    {
        if (context is null) return;

        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<Ticket>())
        {
            if (entry.State == EntityState.Modified)
            {
                // Cấp phát Guid mới cho ConcurrencyToken của Ticket bị sửa đổi
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
