using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ItTms.Infrastructure.Persistence.Interceptors;

namespace ItTms.Infrastructure.Persistence;

public class ItTmsDbContextFactory : IDesignTimeDbContextFactory<ItTmsDbContext>
{
    public ItTmsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ItTmsDbContext>();
        
        // Thiết lập connection string phục vụ môi trường thiết kế CLI
        optionsBuilder.UseSqlite("Data Source=ittms.db;Cache=Shared");
        optionsBuilder.AddInterceptors(new ConcurrencyTokenInterceptor());

        return new ItTmsDbContext(optionsBuilder.Options);
    }
}
