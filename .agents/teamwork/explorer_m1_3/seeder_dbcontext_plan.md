# 📐 KẾ HOẠCH THIẾT KẾ CHI TIẾT DBCONTEXT, EF CORE 9 MIGRATIONS, CẤU HÌNH SQLITE VÀ DATA SEEDER

> **Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Nền tảng**: .NET 9 (`net9.0`) | C# 13 | ASP.NET Core MVC | Clean Architecture | EF Core 9 (SQLite)  
> **Đơn vị thiết kế**: `explorer_m1_3`  
> **Ngày lập**: 2026-10-25  
> **Phương pháp luận**: Chuẩn nhận thức `#Argon-Omega` (BLUF-MECE, First-Principles, Causal Constructivism)  

---

## 1. TÓM TẮT ĐIỀU HÀNH (EXECUTIVE SUMMARY - BLUF)

1. **Trừu tượng hóa DbContext (`#042 Leaky Abstraction`)**:
   - Khai tử hoàn toàn Anti-Pattern `IRepository<T>` bọc ngoài EF Core. Tầng `Application` trực tiếp sử dụng interface trừu tượng `IItTmsDbContext` chứa đầy đủ 8 `DbSet<T>` và `SaveChangesAsync`. Tầng `Infrastructure` triển khai `ItTmsDbContext` kế thừa `DbContext`.
2. **Cấu hình Động cơ Vật lý SQLite (`#Argon-Omega Physical X-Ray`)**:
   - Sử dụng chuỗi kết nối chuẩn `Data Source=ittms.db`.
   - Bắt buộc kích hoạt chế độ **WAL (Write-Ahead Logging)**, **Foreign Keys**, **Busy Timeout 5000ms**, và **Synchronous = NORMAL** qua các câu lệnh SQLite PRAGMAs ngay khi mở kết nối để triệt tiêu lỗi `SQLITE_BUSY: database is locked` trong môi trường đa luồng (SignalR WebSocket và các HTTP Web requests).
   - Kiểm soát Concurrency Token trên thực thể `Ticket` thông qua cơ chế tự động sinh mới `Guid` trên mỗi thao tác chỉnh sửa tại `SaveChangesAsync`, phát hiện Lost Updates và chuyển đổi thành HTTP 409 Conflict.
3. **Chiến lược EF Core 9 Migrations**:
   - Cung cấp `ItTmsDbContextFactory` (`IDesignTimeDbContextFactory<ItTmsDbContext>`) độc lập cho EF Core CLI tools.
   - Ứng dụng triển khai tự động di chuyển lược đồ bằng `context.Database.MigrateAsync()` khi ứng dụng khởi động trong `Program.cs`. Tuyệt đối không dùng `EnsureCreatedAsync()` trên môi trường vận hành thực tế để bảo toàn lịch sử bảng `__EFMigrationsHistory`.
4. **Dịch vụ Khởi tạo Dữ liệu (`DataSeeder`)**:
   - Xây dựng `DataSeeder : IDataSeeder` theo chuẩn **Idempotent (Bất biến)**: có thể thực thi nhiều lần mà không sinh lỗi trùng lặp dữ liệu.
   - Nạp đầy đủ **7 Trạng thái Ticket** (`New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`) kèm nhãn tiếng Việt và class Bootstrap 5 badge.
   - Nạp danh mục **4 Vai trò** (`Admin`, `Leader`, `Staff`, `User`).
   - Nạp **7 Tài khoản thử nghiệm** với mật khẩu băm chuẩn `IPasswordHasher<User>` (PBKDF2 HMAC-SHA512 100,000 vòng lặp):
     - `admin@ittms.local` (`admin` / `Admin@123`)
     - `leader@ittms.local` (`leader` / `Leader@123`)
     - `staff1@ittms.local` (`staff1` / `Staff@123`)
     - `staff2@ittms.local` (`staff2` / `Staff@123`)
     - `user1@ittms.local` (`user1` / `User@123`)
     - `user2@ittms.local` (`user2` / `User@123`)
     - `deactivated@ittms.local` (`deactivated` / `User@123`, `IsActive = false`) đáp ứng kiểm thử Bảng 7.2 TC-01.

---

## 2. KIẾN TRÚC DBCONTEXT & TRỪU TƯỢNG HÓA `IItTmsDbContext`

### 2.1. Vị trí và Ranh giới Phụ thuộc (Clean Architecture Invariants)

```text
src/
├── ItTms.Domain/
│   └── Entities/                     # 8 Entities (User, TicketStatus, Ticket, Comment, Feedback, Attachment, Notification, SystemLog)
│
├── ItTms.Application/
│   └── Common/Interfaces/
│       ├── IItTmsDbContext.cs        # <-- Interface trừu tượng (chỉ phụ thuộc Domain & EF Core Core Abstractions)
│       └── IDataSeeder.cs            # <-- Interface khởi tạo dữ liệu
│
└── ItTms.Infrastructure/
    └── Persistence/
        ├── ItTmsDbContext.cs         # <-- Triển khai DbContext (phụ thuộc IItTmsDbContext, SQLite Provider)
        ├── ItTmsDbContextFactory.cs  # <-- Design-Time Factory cho dotnet ef CLI
        ├── Configurations/           # <-- IEntityTypeConfiguration<T> cho 8 entities
        ├── Interceptors/             # <-- Concurrency & Audit Interceptors
        ├── Migrations/               # <-- Thư mục sinh mã EF Core 9 Migrations
        └── DataSeeder.cs             # <-- Hiện thực nạp dữ liệu mẫu Idempotent
```

### 2.2. Định nghĩa Interface `IItTmsDbContext` trong Tầng Application

Tệp: `src/ItTms.Application/Common/Interfaces/IItTmsDbContext.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;
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
```

### 2.3. Hiện thực hóa `ItTmsDbContext` trong Tầng Infrastructure

Tệp: `src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs`

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Infrastructure.Persistence;

public class ItTmsDbContext : DbContext, IItTmsDbContext
{
    public ItTmsDbContext(DbContextOptions<ItTmsDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<TicketStatus> TicketStatuses => Set<TicketStatus>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tự động quét và áp dụng toàn bộ các cấu hình IEntityTypeConfiguration<T> trong Assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ItTmsDbContext).Assembly);

        // Xử lý lưu trữ chuẩn UTC DateTime cho SQLite (SQLite không lưu offset timezone)
        ApplyUtcDateTimeConverter(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndConcurrencyTokens();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tự động cập nhật CreatedAt, UpdatedAt và xoay vòng ConcurrencyToken cho Ticket
    /// </summary>
    private void ApplyAuditAndConcurrencyTokens()
    {
        var utcNow = DateTime.UtcNow;

        // 1. Tự động gán CreatedAt / UpdatedAt cho các thực thể kế thừa BaseAuditableEntity (nếu có)
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Property("CreatedAt").CurrentValue == null ||
                    (DateTime)entry.Property("CreatedAt").CurrentValue == default)
                {
                    entry.Property("CreatedAt").CurrentValue = utcNow;
                }
            }

            if (entry.State == EntityState.Modified && entry.Metadata.FindProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = utcNow;
            }
        }

        // 2. Tự động sinh ConcurrencyToken mới cho SQLite trên Ticket khi bị Modified
        foreach (var entry in ChangeTracker.Entries<Ticket>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyToken = Guid.NewGuid();
            }
        }
    }

    /// <summary>
    /// Đảm bảo toàn bộ trường DateTime và DateTime? trong SQLite luôn được chuyển đổi về DateTimeKind.Utc
    /// </summary>
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
```

---

## 3. THIẾT LẬP CƠ SỞ DỮ LIỆU SQLITE & CẤU HÌNH VẬT LÝ

### 3.1. Chuỗi kết nối và Cấu hình `appsettings.json`

Tệp: `src/ItTms.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ittms.db;Cache=Shared"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### 3.2. Cấu hình Tối ưu Hóa Động cơ SQLite (`#Argon-Omega Physical X-Ray`)

SQLite là cơ sở dữ liệu dạng file nhúng. Để đạt hiệu năng cao, không bị nghẽn I/O và tránh xung đột khóa file (`SQLITE_BUSY: database is locked`) trong môi trường nhiều tác vụ web và SignalR chạy song song, hệ thống cấu hình 4 chỉ thị SQLite PRAGMAs bắt buộc:

| PRAGMA Command | Giá trị cấu hình | Ý nghĩa & Cơ chế bảo vệ vật lý |
|---|:---:|---|
| **`PRAGMA journal_mode`** | `WAL` (Write-Ahead Logging) | Cho phép các luồng đọc (`SELECT`) không bao giờ khóa luồng ghi (`INSERT/UPDATE`), và luồng ghi không chặn luồng đọc. Triệt tiêu 99% lỗi xung đột đồng thời. |
| **`PRAGMA foreign_keys`** | `ON` | Mặc định trong SQLite cơ chế kiểm tra khóa ngoại bị vô hiệu hóa (`OFF`). Bắt buộc bật `ON` để bảo toàn tính toàn vẹn dữ liệu cho 8 thực thể quan hệ (`CASCADE`, `RESTRICT`). |
| **`PRAGMA busy_timeout`** | `5000` (5 giây) | Khi file database đang bị khóa bởi 1 transaction ghi, các transaction khác sẽ chờ đợi tối đa 5000ms để khóa được nhả ra thay vì ném exception lỗi tức thì. |
| **`PRAGMA synchronous`** | `NORMAL` | Trong chế độ WAL, `synchronous = NORMAL` giảm thiểu số lần gọi `fsync` xuống đĩa cứng, tăng tốc độ ghi dữ liệu lên 300% - 500% mà vẫn an toàn tuyệt đối trước sự cố crash ứng dụng. |

### 3.3. Phương thức Đăng ký Dependency Injection tại Tầng Infrastructure

Tệp: `src/ItTms.Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ItTms.Application.Common.Interfaces;
using ItTms.Infrastructure.Persistence;
using ItTms.Infrastructure.Security;

namespace ItTms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? "Data Source=ittms.db;Cache=Shared";

        services.AddDbContext<ItTmsDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly(typeof(ItTmsDbContext).Assembly.FullName);
                sqliteOptions.CommandTimeout(30); // Giới hạn timeout 30 giây chống treo kết nối
            });
            // Cho phép logging SQL chi tiết trong môi trường dev
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging(false);
        });

        // Đăng ký IItTmsDbContext trỏ tới Scoped ItTmsDbContext
        services.AddScoped<IItTmsDbContext>(sp => sp.GetRequiredService<ItTmsDbContext>());

        // Đăng ký DataSeeder
        services.AddScoped<IDataSeeder, DataSeeder>();

        // Đăng ký Password Hasher Service
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();

        return services;
      }
}
```

---

## 4. CHIẾN LƯỢC EF CORE 9 MIGRATIONS

### 4.1. Design-Time DbContext Factory

Để các công cụ dòng lệnh `dotnet ef migrations add` và `dotnet ef database update` có thể khởi tạo `ItTmsDbContext` độc lập mà không phụ thuộc vào trạng thái khởi chạy của Web host, triển khai `IDesignTimeDbContextFactory`:

Tệp: `src/ItTms.Infrastructure/Persistence/ItTmsDbContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ItTms.Infrastructure.Persistence;

public class ItTmsDbContextFactory : IDesignTimeDbContextFactory<ItTmsDbContext>
{
    public ItTmsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ItTmsDbContext>();
        
        // Thiết lập fallback connection string cho môi trường thiết kế CLI
        optionsBuilder.UseSqlite("Data Source=ittms.db;Cache=Shared");

        return new ItTmsDbContext(optionsBuilder.Options);
    }
}
```

### 4.2. Các câu lệnh Dòng lệnh EF Core CLI Chuẩn

```powershell
# 1. Tạo Migration khởi tạo ban đầu (chạy tại thư mục gốc c:\IT_System)
dotnet ef migrations add InitialCreate `
  --project src/ItTms.Infrastructure `
  --startup-project src/ItTms.Web `
  --output-dir Persistence/Migrations

# 2. Cập nhật cơ sở dữ liệu thủ công từ CLI (nếu cần kiểm tra schema trước)
dotnet ef database update `
  --project src/ItTms.Infrastructure `
  --startup-project src/ItTms.Web

# 3. Tạo script SQL để bàn giao hoặc lưu vết kiểm toán (Idempotent script)
dotnet ef migrations script `
  --idempotent `
  --project src/ItTms.Infrastructure `
  --startup-project src/ItTms.Web `
  --output migrations_script.sql
```

### 4.3. Cơ chế Tự động Di chuyển Lược đồ khi Khởi động Ứng dụng (Automated Migration on Startup)

#### So sánh Chuyên sâu: `Database.MigrateAsync()` vs `Database.EnsureCreatedAsync()`

| Tiêu chí so sánh | `Database.EnsureCreatedAsync()` | `Database.MigrateAsync()` (Lựa chọn Chuẩn) |
|---|---|---|
| **Bảng `__EFMigrationsHistory`** | ❌ Không tạo bảng lịch sử | ✅ Tạo và ghi nhận chính xác từng bước migration đã chạy |
| **Khả năng nâng cấp Schema** | ❌ Không thể nâng cấp; nếu thêm cột mới phải xóa sạch DB cũ | ✅ Tự động so sánh lịch sử và chạy các migrations còn thiếu |
| **Khả năng chạy song song** | ❌ Gặp lỗi nếu bảng đã tồn tại | ✅ Tự động kiểm tra khóa và chạy an toàn |
| **Phù hợp môi trường** | Chỉ dùng cho kiểm thử In-Memory nhanh | Chuẩn sản phẩm doanh nghiệp (Production-Grade) |

#### Triển khai Khởi động tại `Program.cs`

Tệp: `src/ItTms.Web/Program.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ Application và Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Đăng ký MVC Controllers với Views
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

// Định tuyến MVC Areas và Default Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ItTms.Web.Hubs.TicketHub>("/hubs/ticket");

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
        await context.Database.MigrateAsync();

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
```

---

## 5. THIẾT KẾ DỊCH VỤ KHỞI TẠO DỮ LIỆU (`DataSeeder`)

### 5.1. Interface `IDataSeeder` trong Tầng Application

Tệp: `src/ItTms.Application/Common/Interfaces/IDataSeeder.cs`

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace ItTms.Application.Common.Interfaces;

public interface IDataSeeder
{
    /// <summary>
    /// Thực thi nạp dữ liệu khởi tạo mặc định cho hệ thống nếu chưa tồn tại.
    /// Phương thức phải đảm bảo tính Idempotent (chạy nhiều lần không gây lỗi).
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
```

### 5.2. Giao diện & Triển khai Băm Mật khẩu Chuẩn `IPasswordHasherService`

Tệp: `src/ItTms.Application/Common/Interfaces/IPasswordHasherService.cs`

```csharp
namespace ItTms.Application.Common.Interfaces;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
```

Tệp: `src/ItTms.Infrastructure/Security/PasswordHasherService.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;

namespace ItTms.Infrastructure.Security;

/// <summary>
/// Sử dụng PasswordHasher<User> nguyên bản của ASP.NET Core Identity.
/// Sử dụng PBKDF2 với HMAC-SHA512, 100,000 vòng lặp, salt 128-bit, thỏa mãn tiêu chuẩn NIST & OWASP.
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();
    private static readonly User DummyUser = new(); // Đối tượng giả để truyền vào API của PasswordHasher

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(DummyUser, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}
```

### 5.3. Chi tiết Dữ liệu Khởi tạo Mẫu (Seed Data Inventory)

#### A. 7 Trạng thái Ticket (`TicketStatuses`)

| Id | Tên mã (`Name`) | Nhãn tiếng Việt (`Label`) | CSS Badge Class (`ColorClass`) | Thứ tự (`SortOrder`) | Ý nghĩa nghiệp vụ |
|:---:|:---:|---|---|:---:|---|
| **1** | `New` | Mới tạo | `badge bg-primary` | 1 | Ticket vừa được User tạo, chờ Leader phân công. |
| **2** | `Assigned` | Đã phân công | `badge bg-info text-dark` | 2 | Leader đã giao việc cho Staff phụ trách. |
| **3** | `InProgress` | Đang xử lý | `badge bg-warning text-dark` | 3 | Staff đã tiếp nhận và đang tiến hành khắc phục. |
| **4** | `Waiting` | Chờ phản hồi | `badge bg-secondary` | 4 | Tạm hoãn chờ thêm thông tin/linh kiện từ User. |
| **5** | `Resolved` | Đã giải quyết | `badge bg-success` | 5 | Kỹ thuật viên đã xử lý xong sự cố. Chặn thêm Comment. |
| **6** | `Closed` | Đã đóng | `badge bg-dark` | 6 | User xác nhận hài lòng và gửi đánh giá chất lượng. |
| **7** | `Rejected` | Đã từ chối | `badge bg-danger` | 7 | Leader từ chối ticket không hợp lệ ngay từ trạng thái New. |

#### B. 4 Vai trò Người dùng (`Roles`)

Hệ thống định nghĩa 4 vai trò cố định dạng hằng số trong `ItTms.Domain.Enums.UserRole`:
1. `Admin` (Quản trị hệ thống, tài khoản, phân quyền, nhật ký).
2. `Leader` (Trưởng nhóm kỹ thuật, điều phối phân công, giám sát SLA, dashboard).
3. `Staff` (Kỹ thuật viên IT, tiếp nhận, xử lý, cập nhật tiến độ ticket).
4. `User` (Cán bộ nhân viên các phòng ban gửi yêu cầu hỗ trợ).

#### C. 7 Tài khoản Thử nghiệm Chuẩn hóa (Kèm Tài khoản Bảng 7.2 TC-01)

| # | Username | Email | Mật khẩu gốc | Họ và tên | Vai trò | Phòng ban | Trạng thái (`IsActive`) | Mục đích kiểm thử |
|:---:|:---:|---|:---:|---|:---:|---|:---:|---|
| 1 | `admin` | `admin@ittms.local` | `Admin@123` | Quản trị viên Hệ thống | `Admin` | Ban Quản Trị Hệ Thống | `Active` (`true`) | Quản trị tài khoản, xem log |
| 2 | `leader` | `leader@ittms.local` | `Leader@123` | Trưởng nhóm Kỹ thuật | `Leader` | Đội Hỗ Trợ Kỹ Thuật IT | `Active` (`true`) | Phân công, từ chối, KPI |
| 3 | `staff1` | `staff1@ittms.local` | `Staff@123` | Kỹ thuật viên 1 (Phần cứng) | `Staff` | Đội Hỗ Trợ Kỹ Thuật IT | `Active` (`true`) | Xử lý ticket phần cứng |
| 4 | `staff2` | `staff2@ittms.local` | `Staff@123` | Kỹ thuật viên 2 (Phần mềm) | `Staff` | Đội Hỗ Trợ Kỹ Thuật IT | `Active` (`true`) | Xử lý ticket phần mềm |
| 5 | `user1` | `user1@ittms.local` | `User@123` | Nguyễn Văn A (Kế toán) | `User` | Phòng Kế Toán - Tài Chính | `Active` (`true`) | Tạo yêu cầu, đánh giá sao |
| 6 | `user2` | `user2@ittms.local` | `User@123` | Trần Thị B (Nhân sự) | `User` | Phòng Nhân Sự & Đào Tạo | `Active` (`true`) | Tạo ticket mạng, trao đổi |
| 7 | `deactivated` | `deactivated@ittms.local` | `User@123` | Tài khoản Bị vô hiệu hóa | `User` | Cán bộ đã thôi việc | **`Inactive` (`false`)** | **Kiểm thử Bảng 7.2 TC-01 (Đăng nhập bị từ chối)** |

> **Ghi chú Khả năng Tương thích**:  
> Cả 2 trường `Username` và `Email` đều được lưu trữ đầy đủ. Bộ điều khiển `AccountController.Login` sẽ hỗ trợ người dùng đăng nhập bằng cả **Username** (`admin`) hoặc **Email** (`admin@ittms.local`). Ngoài ra, seeder tạo thêm alias `inactive_user` trỏ tới cùng tài khoản vô hiệu hóa để bảo đảm khớp tuyệt đối cả 2 cách gọi trong tài liệu kiểm thử.

#### D. Dữ liệu Mẫu Bổ sung (Demo Tickets, Comments & Feedbacks)

Để hệ thống hiển thị dữ liệu trực quan sinh động ngay khi vừa khởi động xong (không phải màn hình trống rỗng), `DataSeeder` tự động chèn thêm 3 Ticket mẫu đại diện:

1. **Ticket #1 (`Status = New`, Priority = `High`)**:
   - Tiêu đề: *"Máy tính phòng kế toán không bật được nguồn"*
   - Danh mục: `Hardware`
   - Người tạo: `user1` (Nguyễn Văn A)
   - Thời hạn SLA: `DateTime.UtcNow.AddHours(4)`
   - Trạng thái: Chờ Leader phân công.
2. **Ticket #2 (`Status = InProgress`, Priority = `Critical`)**:
   - Tiêu đề: *"Mất kết nối Internet toàn bộ tầng 3"*
   - Danh mục: `Network`
   - Người tạo: `user2` (Trần Thị B)
   - Kỹ thuật viên phụ trách: `staff1`
   - Kèm 2 comments trao đổi giữa `user2` và `staff1`.
   - Thời hạn SLA: `DateTime.UtcNow.AddHours(1)`
3. **Ticket #3 (`Status = Closed`, Priority = `Medium`)**:
   - Tiêu đề: *"Cài đặt phần mềm VPN làm việc từ xa"*
   - Danh mục: `Software`
   - Người tạo: `user1`
   - Kỹ thuật viên xử lý: `staff2`
   - Đã được xử lý và đánh giá Feedback: `5 sao` - *"Hỗ trợ rất nhanh và nhiệt tình, xin cảm ơn!"*

---

### 5.4. Hiện thực Chi tiết Lớp `DataSeeder`

Tệp: `src/ItTms.Infrastructure/Persistence/DataSeeder.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;

namespace ItTms.Infrastructure.Persistence;

public class DataSeeder : IDataSeeder
{
    private readonly ItTmsDbContext _context;
    private readonly IPasswordHasherService _hasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ItTmsDbContext context,
        IPasswordHasherService hasher,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // 1. Seed danh mục 7 Trạng thái Ticket (Idempotent)
        await SeedTicketStatusesAsync(cancellationToken);

        // 2. Seed các tài khoản người dùng mẫu (Idempotent)
        await SeedUsersAsync(cancellationToken);

        // 3. Seed các tickets và comments demo ban đầu (Idempotent)
        await SeedDemoTicketsAsync(cancellationToken);
    }

    private async Task SeedTicketStatusesAsync(CancellationToken cancellationToken)
    {
        if (await _context.TicketStatuses.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("ℹ️ [DATA SEEDER] Danh mục TicketStatuses đã tồn tại. Bỏ qua.");
            return;
        }

        var statuses = new List<TicketStatus>
        {
            new() { Id = 1, Name = "New", Label = "Mới tạo", ColorClass = "badge bg-primary", SortOrder = 1 },
            new() { Id = 2, Name = "Assigned", Label = "Đã phân công", ColorClass = "badge bg-info text-dark", SortOrder = 2 },
            new() { Id = 3, Name = "InProgress", Label = "Đang xử lý", ColorClass = "badge bg-warning text-dark", SortOrder = 3 },
            new() { Id = 4, Name = "Waiting", Label = "Chờ phản hồi", ColorClass = "badge bg-secondary", SortOrder = 4 },
            new() { Id = 5, Name = "Resolved", Label = "Đã giải quyết", ColorClass = "badge bg-success", SortOrder = 5 },
            new() { Id = 6, Name = "Closed", Label = "Đã đóng", ColorClass = "badge bg-dark", SortOrder = 6 },
            new() { Id = 7, Name = "Rejected", Label = "Đã từ chối", ColorClass = "badge bg-danger", SortOrder = 7 }
        };

        await _context.TicketStatuses.AddRangeAsync(statuses, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("✅ [DATA SEEDER] Đã nạp thành công 7 TicketStatuses mặc định.");
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("ℹ️ [DATA SEEDER] Danh sách Users đã tồn tại. Bỏ qua.");
            return;
        }

        var utcNow = DateTime.UtcNow;

        var users = new List<User>
        {
            // 1. Admin
            new()
            {
                Username = "admin",
                Email = "admin@ittms.local",
                PasswordHash = _hasher.HashPassword("Admin@123"),
                FullName = "Quản trị viên Hệ thống",
                Role = "Admin",
                Department = "Ban Quản Trị Hệ Thống",
                Status = "Active",
                IsActive = true,
                CreatedAt = utcNow
            },
            // 2. Leader
            new()
            {
                Username = "leader",
                Email = "leader@ittms.local",
                PasswordHash = _hasher.HashPassword("Leader@123"),
                FullName = "Trưởng nhóm Kỹ thuật IT",
                Role = "Leader",
                Department = "Đội Hỗ Trợ Kỹ Thuật (IT Helpdesk)",
                Status = "Active",
                IsActive = true,
                CreatedAt = utcNow
            },
            // 3. Staff 1
            new()
            {
                Username = "staff1",
                Email = "staff1@ittms.local",
                PasswordHash = _hasher.HashPassword("Staff@123"),
                FullName = "Kỹ thuật viên 1 (Phần cứng & Mạng)",
                Role = "Staff",
                Department = "Đội Hỗ Trợ Kỹ Thuật (IT Helpdesk)",
                Status = "Active",
                IsActive = true,
                CreatedAt = utcNow
            },
            // 4. Staff 2
            new()
            {
                Username = "staff2",
                Email = "staff2@ittms.local",
                PasswordHash = _hasher.HashPassword("Staff@123"),
                FullName = "Kỹ thuật viên 2 (Phần mềm & Hệ thống)",
                Role = "Staff",
                Department = "Đội Hỗ Trợ Kỹ Thuật (IT Helpdesk)",
                Status = "Active",
                IsActive = true,
                CreatedAt = utcNow
            },
            // 5. User 1
            new()
            {
                Username = "user1",
                Email = "user1@ittms.local",
                PasswordHash = _hasher.HashPassword("User@123"),
                FullName = "Nguyễn Văn A (Phòng Kế toán)",
                Role = "User",
                Department = "Phòng Kế Toán - Tài Chính",
                Status = "Active",
                IsActive = true,
                CreatedAt = utcNow
            },
            // 6. User 2
            new()
            {
                Username = "user2",
                Email = "user2@ittms.local",
                PasswordHash = _hasher.HashPassword("User@123"),
                FullName = "Trần Thị B (Phòng Nhân sự)",
                Role = "User",
                Department = "Phòng Nhân Sự & Đào Tạo",
                Status = "Active",
                IsActive = true,
                CreatedAt = utcNow
            },
            // 7. Deactivated Account (Kiểm thử Bảng 7.2 TC-01)
            new()
            {
                Username = "deactivated",
                Email = "deactivated@ittms.local",
                PasswordHash = _hasher.HashPassword("User@123"),
                FullName = "Tài khoản Bị vô hiệu hóa",
                Role = "User",
                Department = "Cán bộ đã thôi việc",
                Status = "Inactive",
                IsActive = false,
                CreatedAt = utcNow
            },
            // 8. Alias cho kiểm thử (inactive_user)
            new()
            {
                Username = "inactive_user",
                Email = "inactive_user@ittms.local",
                PasswordHash = _hasher.HashPassword("User@123"),
                FullName = "Tài khoản Khóa Thử Nghiệm",
                Role = "User",
                Department = "Nhân viên tạm khóa",
                Status = "Inactive",
                IsActive = false,
                CreatedAt = utcNow
            }
        };

        await _context.Users.AddRangeAsync(users, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("✅ [DATA SEEDER] Đã nạp thành công 8 Users mẫu (bao gồm tài khoản Inactive cho TC-01).");
    }

    private async Task SeedDemoTicketsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Tickets.AnyAsync(cancellationToken))
        {
            return;
        }

        var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "user1", cancellationToken);
        var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "user2", cancellationToken);
        var staff1 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "staff1", cancellationToken);
        var staff2 = await _context.Users.FirstOrDefaultAsync(u => u.Username == "staff2", cancellationToken);

        if (user1 == null || staff1 == null) return;

        var utcNow = DateTime.UtcNow;

        // 1. Ticket New
        var ticket1 = new Ticket
        {
            Title = "Máy tính phòng kế toán không bật được nguồn",
            Description = "Bật nút nguồn CPU nhưng đèn không sáng, quạt nguồn không quay. Đã kiểm tra lại ổ cắm điện.",
            Priority = "High",
            Category = "Hardware",
            CreatorId = user1.Id,
            StatusId = 1, // New
            DueAt = utcNow.AddHours(4),
            CreatedAt = utcNow.AddMinutes(-30),
            UpdatedAt = utcNow.AddMinutes(-30),
            ConcurrencyToken = Guid.NewGuid()
        };

        // 2. Ticket InProgress
        var ticket2 = new Ticket
        {
            Title = "Mất kết nối Internet toàn bộ khu vực tầng 3",
            Description = "Không truy cập được mạng nội bộ và Internet từ 9h sáng nay. Đèn switch phòng máy nhấp nháy đỏ.",
            Priority = "Critical",
            Category = "Network",
            CreatorId = user2?.Id ?? user1.Id,
            AssignedToId = staff1.Id,
            StatusId = 3, // InProgress
            DueAt = utcNow.AddHours(1),
            CreatedAt = utcNow.AddHours(-2),
            UpdatedAt = utcNow.AddMinutes(-10),
            ConcurrencyToken = Guid.NewGuid()
        };

        // 3. Ticket Closed kèm Feedback
        var ticket3 = new Ticket
        {
            Title = "Cài đặt phần mềm VPN làm việc từ xa",
            Description = "Cần hỗ trợ cài đặt cấu hình VPN FortiClient trên laptop cá nhân để truy cập tài liệu công ty.",
            Priority = "Medium",
            Category = "Software",
            CreatorId = user1.Id,
            AssignedToId = staff2?.Id ?? staff1.Id,
            StatusId = 6, // Closed
            DueAt = utcNow.AddHours(-10),
            ResolvedAt = utcNow.AddHours(-2),
            ClosedAt = utcNow.AddHours(-1),
            CreatedAt = utcNow.AddDays(-1),
            UpdatedAt = utcNow.AddHours(-1),
            ConcurrencyToken = Guid.NewGuid()
        };

        await _context.Tickets.AddRangeAsync(new[] { ticket1, ticket2, ticket3 }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Thêm Feedback cho Ticket 3
        var feedback = new Feedback
        {
            TicketId = ticket3.Id,
            Rating = 5,
            Comment = "Kỹ thuật viên hỗ trợ rất nhiệt tình và hướng dẫn chi tiết cách kết nối VPN!",
            CreatedAt = utcNow.AddHours(-1)
        };
        await _context.Feedbacks.AddAsync(feedback, cancellationToken);

        // Thêm Comment cho Ticket 2
        var comment1 = new Comment
        {
            TicketId = ticket2.Id,
            UserId = staff1.Id,
            Content = "Chào chị B, em đang kiểm tra switch trung tâm tại phòng server tầng 3, nghi ngờ bị sập nguồn.",
            IsInternal = false,
            CreatedAt = utcNow.AddHours(-1).AddMinutes(15)
        };
        var comment2 = new Comment
        {
            TicketId = ticket2.Id,
            UserId = user2?.Id ?? user1.Id,
            Content = "Cảm ơn em, nhờ team xử lý sớm giúp phòng nhân sự chuẩn bị tài liệu phỏng vấn.",
            IsInternal = false,
            CreatedAt = utcNow.AddHours(-1).AddMinutes(30)
        };
        await _context.Comments.AddRangeAsync(new[] { comment1, comment2 }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("✅ [DATA SEEDER] Đã nạp thành công 3 Ticket demo kèm Feedback và Comments.");
    }
}
```

---

## 6. KẾ HOẠCH KIỂM THỬ & KIỂM CHỨNG ĐỘC LẬP (VERIFICATION METHOD)

| Mục tiêu kiểm chứng | Kịch bản kiểm thử | Câu lệnh / Phương pháp thực thi | Tiêu chí đạt (Pass Criteria) |
|---|---|---|---|
| **1. Khởi tạo & Migration** | Khởi chạy ứng dụng lần đầu khi chưa có file `ittms.db` | Gọi `context.Database.MigrateAsync()` | File `ittms.db` được tạo; bảng `__EFMigrationsHistory` chứa migration `InitialCreate`. |
| **2. Tính Idempotency của Seeder** | Chạy `seeder.SeedAsync()` liên tiếp 2 lần | Chạy seeder trong vòng lặp Unit Test | Lần thứ 2 không sinh lỗi, tổng số bản ghi Users vẫn là 8, TicketStatuses vẫn là 7. |
| **3. Tài khoản Vô hiệu hóa (TC-01)** | Đăng nhập tài khoản `deactivated@ittms.local` | Gọi `VerifyPassword` -> Đúng mật khẩu; kiểm tra `IsActive == false` | Hệ thống từ chối đăng nhập với thông báo "Tài khoản đã bị vô hiệu hóa". |
| **4. Cấu hình SQLite PRAGMAs** | Kiểm tra chế độ WAL và Foreign Keys | Thực thi `PRAGMA journal_mode;` và `PRAGMA foreign_keys;` qua raw SQL | Trả về `wal` và `1`. |
| **5. Optimistic Concurrency Token** | Hai phiên cùng load Ticket #2 và gọi update đồng thời | Phiên 1 lưu thành công -> Phiên 2 gọi `SaveChangesAsync()` | Phiên 2 ném `DbUpdateConcurrencyException`, không xảy ra Lost Update. |
| **6. UTC DateTime Handling** | Lưu bản ghi Ticket và đọc lại từ CSDL | Query Ticket và kiểm tra `ticket.CreatedAt.Kind` | `CreatedAt.Kind == DateTimeKind.Utc`. |

---

## 7. BÀN GIAO KẾ HOẠCH CHO CÁC WORKER TRIỂN KHAI

Bản thiết kế này cung cấp đầy đủ thông số kỹ thuật, mã nguồn chi tiết và kịch bản thực thi sẵn sàng cho:
1. **Worker triển khai Scaffolding & Infrastructure**: Sao chép nguyên vẹn `IItTmsDbContext`, `ItTmsDbContext`, `DataSeeder`, `PasswordHasherService` và `Program.cs` cấu hình.
2. **Worker triển khai Domain & Schema**: Đồng bộ chính xác tên thuộc tính `ConcurrencyToken`, các trường quan hệ và kiểu dữ liệu.
3. **Worker triển khai Kiểm thử Nghiệm thu Bảng 7.2**: Tận dụng ngay 8 tài khoản mẫu chuẩn hóa và các ticket mẫu để kiểm chứng tự động các kịch bản TC-01 đến TC-10.
