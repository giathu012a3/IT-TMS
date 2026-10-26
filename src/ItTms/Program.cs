using Microsoft.AspNetCore.Authentication.Cookies;

Console.WriteLine(">>> [INIT] Program.cs bắt đầu chạy...");

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(">>> [INIT] WebApplication.CreateBuilder hoàn tất.");

// 1. Dịch vụ cơ bản
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 2. EF Core 9 SQLite với Interceptors và SQLite PRAGMAs
builder.Services.AddSingleton<ConcurrencyTokenInterceptor>();
builder.Services.AddDbContext<ItTmsDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                           ?? "Data Source=ittms.db";
    options.UseSqlite(connectionString)
           .AddInterceptors(sp.GetRequiredService<ConcurrencyTokenInterceptor>());
});
builder.Services.AddScoped<IItTmsDbContext>(sp => sp.GetRequiredService<ItTmsDbContext>());

// 3. Đăng ký Domain Events Dispatcher & Observers (Observer Pattern - GoF)
builder.Services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();

builder.Services.AddScoped<NotificationObserver>();
builder.Services.AddScoped<IDomainEventObserver<TicketCreatedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
builder.Services.AddScoped<IDomainEventObserver<TicketAssignedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
builder.Services.AddScoped<IDomainEventObserver<TicketStatusChangedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
builder.Services.AddScoped<IDomainEventObserver<CommentAddedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());
builder.Services.AddScoped<IDomainEventObserver<TicketClosedEvent>>(sp => sp.GetRequiredService<NotificationObserver>());

builder.Services.AddScoped<SystemLogObserver>();
builder.Services.AddScoped<IDomainEventObserver<TicketCreatedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
builder.Services.AddScoped<IDomainEventObserver<TicketAssignedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
builder.Services.AddScoped<IDomainEventObserver<TicketStatusChangedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
builder.Services.AddScoped<IDomainEventObserver<CommentAddedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
builder.Services.AddScoped<IDomainEventObserver<TicketClosedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());
builder.Services.AddScoped<IDomainEventObserver<UserDeactivatedEvent>>(sp => sp.GetRequiredService<SystemLogObserver>());

// 4. Đăng ký các dịch vụ hệ thống (Singleton / Scoped)
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();

// 5. Cấu hình Cookie Authentication
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

// 6. MVC Controllers & SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

Console.WriteLine(">>> [INIT] Sắp gọi builder.Build()...");
var app = builder.Build();
Console.WriteLine(">>> [INIT] builder.Build() thành công.");

// Khởi tạo CSDL SQLite và Seed Data ban đầu
Console.WriteLine(">>> [INIT] Sắp khởi tạo CSDL SQLite và Seed Data...");
await ApplyDatabaseMigrationsAndSeedAsync(app);
Console.WriteLine(">>> [INIT] Khởi tạo CSDL SQLite và Seed Data xong!");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // Tắt HTTPS redirect để chạy cục bộ mượt mà trên HTTP port 5050
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Định tuyến MVC chuẩn (Standard ASP.NET Core MVC Route)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR WebSocket Hub Endpoint
app.MapHub<TicketHub>("/hubs/ticket");

app.Urls.Add("http://localhost:5050");
Console.WriteLine(">>> [INIT] Server đang khởi động tại http://localhost:5050 ...");

await app.RunAsync();

// Hàm hỗ trợ khởi tạo CSDL & SQLite PRAGMAs
static async Task ApplyDatabaseMigrationsAndSeedAsync(WebApplication app)
{
    Console.WriteLine(">>> [INIT] Bắt đầu hàm ApplyDatabaseMigrationsAndSeedAsync...");
    using var scope = app.Services.CreateScope();
    Console.WriteLine(">>> [INIT] CreateScope xong.");
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    Console.WriteLine(">>> [INIT] ILogger<Program> xong.");

    try
    {
        Console.WriteLine(">>> [INIT] Sắp lấy ItTmsDbContext...");
        var db = services.GetRequiredService<ItTmsDbContext>();
        Console.WriteLine(">>> [INIT] Đã lấy ItTmsDbContext thành công!");

        Console.WriteLine("⏳ [DATABASE] Đang kiểm tra và thực thi EF Core Migrations trên SQLite...");
        await db.Database.MigrateAsync();

        Console.WriteLine("⚙️ [DATABASE] Đang cấu hình SQLite PRAGMAs (WAL mode, Foreign Keys)...");
        await db.Database.ExecuteSqlRawAsync(@"
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            PRAGMA synchronous = NORMAL;
        ");

        Console.WriteLine("🌱 [DATA SEEDER] Đang nạp dữ liệu mẫu ban đầu...");
        var seeder = services.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();

        Console.WriteLine("✅ [DATABASE] Khởi tạo cơ sở dữ liệu và nạp dữ liệu mẫu thành công 100%!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [DATABASE] Lỗi: {ex}");
        throw;
    }
}
