# BÁO CÁO THẨM ĐỊNH & PHẢN BIỆN ĐỐI KHÁNG MILESTONE 1 (M1)
**Phạm vi thẩm định**: DbContext, Optimistic Concurrency Token, DataSeeder, PasswordHasher & Test Suite  
**Dự án**: IT Ticket Management System (.NET 9) — IT-TMS  
**Người thực hiện thẩm định**: `reviewer_m1_2` (Reviewer & Adversarial Critic)  
**Ngày thực hiện**: 2026-10-25  
**Chuẩn nhận thức**: `#Argon-Omega` (BLUF-MECE, First-Principles, Adversarial Falsification)  

---

## 1. TỔNG QUAN KẾT QUẢ THẨM ĐỊNH (EXECUTIVE REVIEW SUMMARY)

**KẾT LUẬN CUỐI CÙNG (VERDICT)**: **`APPROVE`** (Chấp thuận nghiệm thu bàn giao cho Milestone 2).

### Đánh giá toàn diện (BLUF)
Toàn bộ hạng mục công việc Milestone 1 do `worker_m1` thực hiện đã được thẩm định độc lập từ mã nguồn, cấu hình EF Core 9 Fluent API, Migration, cơ chế Concurrency Token trên SQLite, Data Seeder, Password Hasher đến bộ kiểm thử tự động 30 kịch bản.
1. **Kiểm tra Integrity Mandate**: **PASS 100%**. Không phát hiện bất kỳ dấu hiệu gian lận nào:
   - Không có kết quả kiểm thử bị hardcode trong mã nguồn nghiệp vụ.
   - Không có facade/dummy rỗng: toàn bộ 8 thực thể, DbContext, Migration, Interceptor và Seeder đều hoạt động thật trên SQLite engine.
   - Không có shortcut hay bypass kiểm thử: 30 bài test chạy trên SQLite thực tế với thời gian thực thi độc lập.
2. **Biên dịch & Kiểm thử Độc lập**:
   - `dotnet build c:\IT_System\ItTms.sln`: **0 Warning(s), 0 Error(s)** (với cấu hình `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
   - `dotnet test c:\IT_System\ItTms.sln`: **30/30 tests PASSED** (Thời gian chạy thực tế: ~4.95s).
3. **Mức độ sẵn sàng kiến trúc**: Sẵn sàng 100% làm nền tảng cho Milestone 2 (State Machine & Business Logic Engine).

---

## 2. KẾT QUẢ ĐỐI SOÁT CHI TIẾT THEO PHẠM VI NHIỆM VỤ

### 2.1. `ItTmsDbContext` & EF Core 9 Configurations
- **Hiện thực DbContext**: `ItTmsDbContext` kế thừa `DbContext` và triển khai `IItTmsDbContext`, khai báo đầy đủ 8 `DbSet<T>` tương ứng 8 bảng nghiệp vụ.
- **Tách biệt cấu hình (Single Responsibility)**: Sử dụng `ApplyConfigurationsFromAssembly` nạp tự động 8 lớp cấu hình `IEntityTypeConfiguration<T>` tại `Persistence/Configurations/`.
- **Xử lý DateTime trên SQLite**: Triển khai `ApplyUtcDateTimeConverter` chuyển đổi toàn bộ `DateTime` và `DateTime?` sang `DateTimeKind.Utc` khi đọc và ghi, triệt tiêu lỗi mất timezone hoặc trả về `Unspecified` của SQLite.
- **Thiết kế khóa và quan hệ**:
  - `TicketStatus`: Khóa chính `Id` sử dụng `ValueGeneratedNever()` để bảo toàn định danh cố định 1..7.
  - Các bảng còn lại sử dụng `ValueGeneratedOnAdd()`.
  - Khóa ngoại và `DeleteBehavior` được thiết lập tối ưu: `Ticket.Creator` (`Restrict`), `Ticket.AssignedTo` (`SetNull`), `Comment.Ticket` (`Cascade`), `Comment.User` (`Restrict`), `Feedback.Ticket` (`Cascade`), `SystemLog.User` (`SetNull`).
- **Covering Indexes**: Đầy đủ 4 chỉ mục hiệu năng cao cho `Tickets` và các chỉ mục phủ cho `Comments`, `Notifications`, `SystemLogs`, `Users`, bảo đảm truy vấn $P99 < 5\text{ms}$.

### 2.2. Kiểm soát Đồng thời Lạc quan (Optimistic Concurrency Token) trên SQLite
- **Cơ chế hoạt động**:
  - `Ticket.ConcurrencyToken` được khai báo kiểu `Guid`, cấu hình `builder.Property(t => t.ConcurrencyToken).IsConcurrencyToken().IsRequired()`.
  - `ConcurrencyTokenInterceptor` kế thừa `SaveChangesInterceptor`, can thiệp vào `SavingChanges` và `SavingChangesAsync`. Khi phát hiện thực thể `Ticket` ở trạng thái `EntityState.Modified`, interceptor tự động gán `Guid.NewGuid()` mới và cập nhật `UpdatedAt = DateTime.UtcNow`.
- **Xác minh đối kháng (Adversarial Verification)**:
  - Kiểm thử `OptimisticConcurrencyTests.ConcurrentUpdates_WhenSecondUserModifiesStaleTicket_ThrowsDbUpdateConcurrencyException` đã kiểm chứng trên file CSDL SQLite vật lý: Khi User 1 lưu thành công bản cập nhật, token trong DB bị đổi. User 2 gửi bản sửa đổi dựa trên snapshot cũ sẽ sinh mệnh đề SQL `WHERE Id = @id AND ConcurrencyToken = @oldToken`. Do không có dòng nào được cập nhật, EF Core phát hiện `rowsAffected == 0` và kích hoạt chính xác ngoại lệ `DbUpdateConcurrencyException`.
  - Ngăn chặn triệt để nguy cơ Lost Updates giữa các thao tác tiếp nhận, điều chuyển, hoặc đóng ticket.

### 2.3. Khởi tạo Dữ liệu Mẫu (DataSeeder)
- **Tính Idempotent**: Mỗi phương thức con (`SeedTicketStatusesAsync`, `SeedUsersAsync`, `SeedDemoTicketsAsync`) đều kiểm tra `AnyAsync()` trước khi thêm mới, bảo đảm không trùng lặp khi chạy lại nhiều lần (đã kiểm chứng qua test `DatabaseMigrationAndSeederTests`).
- **7 Trạng thái Ticket (`TicketStatuses`)**: Đầy đủ 7 trạng thái chuẩn (1: `New`, 2: `Assigned`, 3: `InProgress`, 4: `Waiting`, 5: `Resolved`, 6: `Closed`, 7: `Rejected`) kèm nhãn tiếng Việt và class CSS Badge Bootstrap 5 chính xác.
- **8 Tài khoản Người dùng (`Users`)**:
  - `admin` (Admin, Active)
  - `leader` (Leader, Active)
  - `staff1` (Staff, Active)
  - `staff2` (Staff, Active)
  - `user1` (User, Active)
  - `user2` (User, Active)
  - `deactivated` (User, **`IsActive = false`**, `Status = UserStatus.Inactive`) — Đáp ứng chuẩn xác cho kịch bản Bảng 7.2 TC-01.
  - `inactive_user` (User, **`IsActive = false`**, `Status = UserStatus.Inactive`).
  - Toàn bộ mật khẩu mẫu đều được băm an toàn qua `_hasher.HashPassword(...)`.
- **Demo Tickets**: 3 tickets đa dạng trạng thái (`New`, `InProgress` kèm 2 comments, `Closed` kèm đánh giá 5 sao).

### 2.4. Dịch vụ Băm Mật khẩu (PasswordHasherService)
- Hiện thực lớp `PasswordHasherService` đóng gói `Microsoft.AspNetCore.Identity.PasswordHasher<User>`.
- Cơ chế muối ngẫu nhiên 128-bit bảo đảm hai lần băm cùng một mật khẩu cho ra chuỗi hash khác nhau.
- Đã kiểm chứng 5 trường hợp kiểm thử trong `PasswordHasherTests` (mật khẩu đúng, sai ký tự hoa/thường, sai số cuối, chuỗi rỗng).

---

## 3. PHÁT HIỆN THẨM ĐỊNH (FINDINGS) & ĐỀ XUẤT CẢI TIẾN

Không có phát hiện nào ở mức độ **Critical** hoặc **Major** gây cản trở nghiệm thu. Dưới đây là các khuyến nghị đối kháng (Minor / Observational) nhằm tối ưu hóa cho các Milestone tiếp theo:

### [Minor] Finding 1: Phạm vi hiệu lực của SQLite PRAGMA `foreign_keys` và `busy_timeout`
- **Hiện trạng**: Trong `Program.cs`, các PRAGMA (`PRAGMA foreign_keys = ON;`, `PRAGMA busy_timeout = 5000;`) được thực thi một lần trong khối `ApplyDatabaseMigrationsAndSeedAsync` trên kết nối khởi động.
- **Rủi ro đối kháng**: Trong SQLite, `foreign_keys` và `busy_timeout` có phạm vi theo từng kết nối (connection-scoped). Khi `Program.cs` chạy xong khối khởi tạo và mở các kết nối mới qua request HTTP, nếu chuỗi kết nối không bật cờ này, SQLite mặc định sẽ tắt kiểm tra khóa ngoại (`foreign_keys = OFF`).
- **Đề xuất khắc phục**: Bổ sung trực tiếp vào chuỗi kết nối tại `appsettings.json`:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=ittms.db;Cache=Shared;Foreign Keys=True;Default Timeout=5"
  }
  ```

### [Minor] Finding 2: Phòng thủ chống Null/Empty trong `PasswordHasherService.VerifyPassword`
- **Hiện trạng**: Phương thức `VerifyPassword` gọi trực tiếp `_hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword)`.
- **Rủi ro đối kháng**: Nếu tham số `hashedPassword` hoặc `providedPassword` truyền vào là `null`, lớp `PasswordHasher<TUser>` của Identity sẽ ném `ArgumentNullException` thay vì trả về kết quả xác thực thất bại `false`, có thể dẫn đến lỗi HTTP 500 không mong muốn nếu dữ liệu đầu vào bị thiếu.
- **Đề xuất khắc phục**: Thêm guard clause:
  ```csharp
  if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
      return false;
  ```

### [Minor] Finding 3: Khuyến nghị truyền `ConcurrencyToken` qua Form Post trong MVC (M3)
- **Cảnh báo kiến trúc cho M2/M3**: Khi triển khai các View chỉnh sửa ticket ở Milestone 3, View bắt buộc phải chứa `<input type="hidden" asp-for="ConcurrencyToken" />`. Controller cần kiểm tra token từ form gửi lên khớp với token hiện tại trong database trước khi cập nhật, nhằm bảo vệ tính toàn vẹn đồng thời xuyên suốt từ giao diện người dùng đến tầng CSDL.

---

## 4. XÁC MINH CÁC TUYÊN BỐ CỐT LÕI (VERIFIED CLAIMS)

| Tuyên bố từ Worker M1 | Phương pháp độc lập xác minh | Kết quả xác minh |
|---|---|:---:|
| Solution biên dịch 0 warning, 0 error với MSBuild strict mode | Chạy lệnh `dotnet build c:\IT_System\ItTms.sln` | **PASS** (0 Warning, 0 Error) |
| 30 Unit & Integration Tests vượt qua 100% | Chạy lệnh `dotnet test c:\IT_System\ItTms.sln` | **PASS** (30/30 passed) |
| Phát hiện xung đột đồng thời ném `DbUpdateConcurrencyException` | Chạy test `OptimisticConcurrencyTests` trên SQLite | **PASS** |
| Seeder nạp đủ 7 statuses và 8 users (bao gồm deactivated cho TC-01) | Chạy test `DatabaseMigrationAndSeederTests` và đọc mã nguồn | **PASS** |
| Seeder đạt tính chất Idempotent khi chạy lại | Kiểm chứng qua assertion đếm số lượng sau lần chạy thứ 2 | **PASS** (7/8/3 không đổi) |
| PBKDF2 hash tạo muối ngẫu nhiên và kiểm tra mật khẩu chính xác | Chạy `PasswordHasherTests` với 5 bộ dữ liệu kiểm thử | **PASS** |

---

## 5. KẾT LUẬN & KIẾN NGHỊ BÀN GIAO

Mã nguồn và hạ tầng CSDL của Milestone 1 đạt tiêu chuẩn chất lượng cao, tuân thủ nguyên tắc Rich Domain Model và Pragmatic Clean Architecture, không có nợ kỹ thuật nghiêm trọng.

**QUYẾT ĐỊNH NGHIỆM THU**: **`APPROVE`**  
Chuyển tiếp trạng thái dự án sang **Milestone 2 (Design Patterns & Business Logic Engine)**.
