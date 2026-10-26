using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ItTms.Application;
using ItTms.Application.Common.Interfaces;
using ItTms.Infrastructure;
using ItTms.Infrastructure.Persistence;
using ItTms.Web.Hubs;
using ItTms.Web.Services;

var currentDir = Directory.GetCurrentDirectory();
var webProjectDir = Path.Combine(currentDir, "src", "ItTms.Web");
var resolvedContentRoot = (!Directory.Exists(Path.Combine(currentDir, "Views")) && Directory.Exists(Path.Combine(webProjectDir, "Views")))
    ? webProjectDir
    : currentDir;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = resolvedContentRoot,
    WebRootPath = Path.Combine(resolvedContentRoot, "wwwroot")
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 1. Đăng ký HttpContextAccessor và CurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 2. Đăng ký Application và Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Đăng ký SignalR Realtime Dispatcher tại tầng Web để kết nối TicketHub
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

// 4. Cấu hình Cookie Authentication Scheme (8 giờ, sliding expiration)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "ItTms.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// 5. Đăng ký MVC Controllers với Views & SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

// =========================================================================
// PIPELINE TỰ ĐỘNG DI CHUYỂN CSDL & SEED DỮ LIỆU KHI KHỞI ĐỘNG
// =========================================================================
await ApplyDatabaseMigrationsAndSeedAsync(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 6. Định tuyến MVC Areas và Auth Routes
app.MapControllerRoute(
    name: "auth_shortcut",
    pattern: "Auth/{action=Login}/{id?}",
    defaults: new { area = "Auth", controller = "Auth" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 7. SignalR WebSocket Hub Endpoint
app.MapHub<TicketHub>("/hubs/ticket");

await app.RunAsync();

// =========================================================================
// HÀM BỔ TRỢ TỰ ĐỘNG KHỞI TẠO CSDL
// =========================================================================
static async Task ApplyDatabaseMigrationsAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ItTmsDbContext>();

        logger.LogInformation("⏳ [DATABASE] Đang kiểm tra và thực thi EF Core Migrations trên SQLite...");
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync();
        }

        // Kích hoạt SQLite PRAGMAs tối ưu hiệu năng
        logger.LogInformation("⚙️ [DATABASE] Đang cấu hình SQLite PRAGMAs (WAL mode, Foreign Keys, Busy Timeout)...");
        await context.Database.ExecuteSqlRawAsync(@"
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            PRAGMA synchronous = NORMAL;
        ");

        logger.LogInformation("🌱 [DATA SEEDER] Đang thực thi nạp dữ liệu mẫu ban đầu...");
        var seeder = services.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();

        logger.LogInformation("✅ [DATABASE] Khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu thành công 100%!");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ [DATABASE ERROR] Quá trình khởi tạo cơ sở dữ liệu hoặc nạp dữ liệu mẫu thất bại!");
        throw;
    }
}

public partial class Program { }
