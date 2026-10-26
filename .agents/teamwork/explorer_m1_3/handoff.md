# BÁO CÁO BÀN GIAO KIẾN TRÚC (HANDOFF REPORT) — EXPLORER M1-3

> **Nhiệm vụ**: Thiết kế `ItTmsDbContext`, cơ chế EF Core 9 Migrations, cấu hình cơ sở dữ liệu SQLite, và dịch vụ `DataSeeder`.  
> **Người thực hiện**: `explorer_m1_3`  
> **Người nhận**: `parent` (ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Ngày lập**: 2026-10-25  
> **Loại bàn giao**: Hard Handoff (Hoàn tất 100% nhiệm vụ)  

---

## 1. OBSERVATION (QUAN SÁT THỰC TẾ)

1. **Khảo sát Hiện trạng & Môi trường**:
   - `c:\IT_System` đang ở trạng thái Greenfield: chưa có file `.sln`, `.csproj`, `.cs` hay database SQLite `ittms.db` nào (kiểm chứng từ `explorer_codebase_survey_2\codebase_assessment.md`, mục 1).
   - Môi trường SDK: .NET SDK `9.0.317` x64, ASP.NET Core Runtime `9.0.19` sẵn sàng trên Windows (chạy lệnh `dotnet --list-sdks`).
2. **Yêu cầu Bất biến từ Đặc tả**:
   - `ORIGINAL_REQUEST.md` (dòng 17-19, 39, 43): Yêu cầu triển khai 8 thực thể (`Users`, `TicketStatuses`, `Tickets`, `Comments`, `Feedbacks`, `Attachments`, `Notifications`, `SystemLogs`) với SQLite via EF Core 9, Concurrency Tokens, SLA timestamps, và tự động tạo database + seed tài khoản 4 roles trên startup; kịch bản TC-01 yêu cầu tài khoản bị vô hiệu hóa đăng nhập thất bại.
   - `PROJECT.md` (dòng 61-65, 87-93): `IItTmsDbContext` nằm tại `src/ItTms.Application/Common/Interfaces/IItTmsDbContext.cs`, `ItTmsDbContext` và `DataSeeder` nằm tại `src/ItTms.Infrastructure/Persistence/`.
   - `.agents/skills/backend/dotnet-clean-architecture/SKILL.md` (dòng 56-60): Tiêu diệt Anti-Pattern Generic `IRepository<T>`, sử dụng trực tiếp `IItTmsDbContext` phơi bày `DbSet<T>` và `SaveChangesAsync`.
   - `.agents/skills/backend/efcore-database-performance/SKILL.md` (dòng 65-72): Bắt buộc kiểm soát Optimistic Concurrency, CommandTimeout 30s, bảo vệ chống xung đột khóa và race conditions.

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN LOGIC)

1. **Từ Quan sát 2 (`IRepository<T>` Anti-Pattern) $\to$ Trừu tượng hóa `IItTmsDbContext`**:
   - EF Core `DbContext` bản chất đã là Unit of Work và `DbSet<T>` là Repository. Việc bọc thêm một lớp Generic Repository sẽ cản trở các tính năng hiệu năng cao của EF Core 9 như `.AsNoTracking()`, Keyset Pagination, `.Select()` projection và `ExecuteUpdateAsync`.
   - Do đó, giải pháp tối ưu là tạo `IItTmsDbContext` trong `ItTms.Application` chứa 8 `DbSet<T>` và `SaveChangesAsync`, và `ItTmsDbContext` trong `ItTms.Infrastructure`.
2. **Từ Đặc thù SQLite Đa Luồng $\to$ Cấu hình Bắt buộc SQLite PRAGMAs**:
   - SQLite mặc định sử dụng Rollback Journal và tắt khóa ngoại. Khi có nhiều request HTTP MVC và SignalR WebSocket kết nối đồng thời, việc ghi dữ liệu sẽ gây ra lỗi `SQLITE_BUSY: database is locked`.
   - Do đó, bắt buộc kích hoạt:
     - `PRAGMA journal_mode = WAL;` (cho phép concurrent readers & writer).
     - `PRAGMA foreign_keys = ON;` (đảm bảo toàn vẹn tham chiếu 8 bảng).
     - `PRAGMA busy_timeout = 5000;` (chờ nhả khóa tối đa 5 giây).
     - `PRAGMA synchronous = NORMAL;` (tăng tốc độ I/O đĩa lên 3-5 lần).
3. **Từ Hạn chế SQLite RowVersion $\to$ Cơ chế Concurrency Token tự động**:
   - SQLite không hỗ trợ kiểu dữ liệu `ROWVERSION` tự tăng như SQL Server.
   - Do đó, `Ticket` khai báo thuộc tính `Guid ConcurrencyToken`. Trong `ItTmsDbContext.SaveChangesAsync`, ChangeTracker tự động gán `entry.Entity.ConcurrencyToken = Guid.NewGuid()` cho mọi thực thể `Ticket` ở trạng thái `EntityState.Modified`. Khi có 2 thao tác cập nhật đồng thời, EF Core sẽ ném `DbUpdateConcurrencyException`, controller chuyển thành mã HTTP 409 Conflict.
4. **Từ Yêu cầu Khởi động Tự hành $\to$ Tự động `Database.MigrateAsync()`**:
   - Không sử dụng `EnsureCreatedAsync()` vì phương thức này không tạo bảng `__EFMigrationsHistory` và ngăn chặn khả năng nâng cấp migration về sau.
   - Sử dụng `await context.Database.MigrateAsync()` trong `Program.cs` đảm bảo database tự động sinh file `ittms.db` nếu chưa có, áp dụng đầy đủ schema theo đúng thứ tự lịch sử.
   - Cung cấp `ItTmsDbContextFactory` (`IDesignTimeDbContextFactory<ItTmsDbContext>`) để hỗ trợ EF Core CLI hoạt động độc lập.
5. **Từ Yêu cầu Kiểm thử Bảng 7.2 TC-01 $\to$ Thiết kế `DataSeeder` Idempotent**:
   - `DataSeeder` kiểm tra `AnyAsync()` trước khi chèn để đảm bảo tính Idempotent (chạy nhiều lần không trùng dữ liệu).
   - Nạp 7 trạng thái chuẩn với CSS Bootstrap 5 badges.
   - Nạp 7 tài khoản cho 4 roles, băm mật khẩu bằng `Microsoft.AspNetCore.Identity.IPasswordHasher<User>` (PBKDF2 HMAC-SHA512).
   - Bổ sung tài khoản `deactivated@ittms.local` (`IsActive = false`) và alias `inactive_user` để đáp ứng hoàn hảo kịch bản kiểm thử TC-01 trong Bảng 7.2.
   - Bổ sung 3 demo tickets kèm comments và feedbacks để giao diện sẵn sàng hoạt động trực quan ngay lần chạy đầu tiên.

---

## 3. CAVEATS (CÁC ĐIỂM LƯU Ý & GIẢ ĐỊNH)

1. **Giả định về Package Reference trong Application Layer**:
   - Giả định rằng `ItTms.Application` được phép tham chiếu gói cơ bản `Microsoft.EntityFrameworkCore` (chỉ chứa abstractions và LINQ extensions) để sử dụng `DbSet<T>`. Gói cụ thể `Microsoft.EntityFrameworkCore.Sqlite` và `Design` chỉ nằm tại `ItTms.Infrastructure` và `ItTms.Web`.
2. **SQLite File Permission trên Windows**:
   - Đảm bảo tiến trình ứng dụng có quyền ghi (Write Permission) tại thư mục chứa file `ittms.db`.
3. **Bộ nhớ In-Memory cho Unit/Integration Tests**:
   - Đối với các bài test chạy song song, khuyến nghị sử dụng SQLite In-Memory với connection `Data Source=:memory:;Mode=Memory;Cache=Shared` và mở kết nối trước để tránh bị giải phóng bộ nhớ.

---

## 4. CONCLUSION (KẾT LUẬN & KIẾN NGHỊ HÀNH ĐỘNG)

1. Thiết kế đã hoàn thiện toàn diện, đạt chuẩn nhận thức `#Argon-Omega` và sẵn sàng 100% để đội ngũ Implementer chuyển hóa thành mã nguồn thực thi:
   - Hồ sơ thiết kế chi tiết: `c:\IT_System\.agents\teamwork\explorer_m1_3\seeder_dbcontext_plan.md`.
2. **Các thành phần sẵn sàng cung cấp cho Milestone 1**:
   - `IItTmsDbContext`: Interface chuẩn cho Application layer.
   - `ItTmsDbContext`: Implementation chuẩn EF Core 9 SQLite kèm Concurrency Token và UTC DateTime Converter.
   - `ItTmsDbContextFactory`: Factory cho `dotnet ef` CLI.
   - `DependencyInjection.cs`: Cấu hình DbContext, Scoped registration, CommandTimeout 30s.
   - `Program.cs`: Pipeline tự động di chuyển CSDL và kích hoạt `DataSeeder`.
   - `DataSeeder`: Hiện thực nạp 7 statuses, 4 roles, 8 user accounts (kèm tài khoản inactive cho TC-01), và 3 demo tickets.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

Để kiểm chứng tính đúng đắn và độc lập của thiết kế sau khi implement:
1. **Kiểm tra Tạo Migration qua CLI**:
   ```powershell
   dotnet ef migrations add InitialCreate --project src/ItTms.Infrastructure --startup-project src/ItTms.Web --output-dir Persistence/Migrations
   ```
   *Tiêu chí đạt*: Sinh thành công file Migration trong `src/ItTms.Infrastructure/Persistence/Migrations/`.
2. **Kiểm tra Tự động Khởi tạo & SQLite PRAGMAs**:
   ```powershell
   dotnet run --project src/ItTms.Web
   ```
   *Tiêu chí đạt*: Log console hiển thị:
   - `[DATABASE] Đang kiểm tra và thực thi EF Core Migrations...`
   - `[DATABASE] Đang cấu hình SQLite PRAGMAs (WAL mode, Foreign Keys)...`
   - `[DATA SEEDER] Đã nạp thành công 7 TicketStatuses mặc định.`
   - `[DATA SEEDER] Đã nạp thành công 8 Users mẫu...`
   - File `ittms.db` được tạo tự động trên đĩa.
3. **Kiểm tra Bảng 7.2 TC-01 (Tài khoản Inactive)**:
   - Gửi yêu cầu đăng nhập với `deactivated@ittms.local` hoặc `inactive_user`, mật khẩu `User@123`.
   - *Tiêu chí đạt*: Hệ thống từ chối đăng nhập, hiển thị thông báo lỗi `"Tài khoản đã bị vô hiệu hóa"`.
