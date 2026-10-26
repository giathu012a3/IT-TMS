# HANDOFF REPORT: REVIEWER M1_2 — DBCONTEXT, CONCURRENCY, SEEDER & SECURITY

> **Agent**: `reviewer_m1_2` (reviewer / critic)  
> **Target Recipient**: Orchestrator (`9e3cad2c-3e7f-49c0-9814-015216856d2e`) / Milestone 2 Workers  
> **Working Directory**: `c:\IT_System\.agents\teamwork\reviewer_m1_2`  
> **Type**: Hard Handoff  
> **Explicit Verdict**: **`APPROVE`**  
> **Date**: 2026-10-25  

---

## 1. OBSERVATION

1. **Biên dịch Hệ thống**:
   - Chạy lệnh `dotnet build c:\IT_System\ItTms.sln` từ thư mục `c:\IT_System`:
     ```
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:08.25
     ```
   - Cả 5 dự án (`ItTms.Domain`, `ItTms.Application`, `ItTms.Infrastructure`, `ItTms.Web`, `ItTms.Tests`) đều biên dịch sạch với cờ `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

2. **Thực thi Toàn bộ Kiểm thử Tự động**:
   - Chạy lệnh `dotnet test c:\IT_System\ItTms.sln --verbosity normal`:
     ```
     Test run for c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll (.NETCoreApp,Version=v9.0)
     VSTest version 17.14.1 (x64)
     ...
     Test Run Successful.
     Total tests: 30
          Passed: 30
      Total time: 4.9507 Seconds
     ```
   - Chạy riêng kịch bản kiểm soát đồng thời `dotnet test c:\IT_System\ItTms.sln --filter "OptimisticConcurrencyTests"`: `Passed! - Failed: 0, Passed: 1, Duration: 2 s`.

3. **Cấu hình `ItTmsDbContext` và Concurrency Token**:
   - `c:\IT_System\src\ItTms.Infrastructure\Persistence\ItTmsDbContext.cs` định nghĩa đầy đủ 8 `DbSet<T>` và `ApplyUtcDateTimeConverter` chuẩn hóa mọi kiểu `DateTime` sang `DateTimeKind.Utc`.
   - `c:\IT_System\src\ItTms.Infrastructure\Persistence\Interceptors\ConcurrencyTokenInterceptor.cs` can thiệp vào sự kiện `SavingChanges` và `SavingChangesAsync`, tự động gán `entry.Entity.ConcurrencyToken = Guid.NewGuid()` cho mọi bản ghi `Ticket` có `entry.State == EntityState.Modified`.
   - `c:\IT_System\src\ItTms.Infrastructure\Persistence\Configurations\TicketConfiguration.cs` dòng 43-45:
     ```csharp
     builder.Property(t => t.ConcurrencyToken)
         .IsConcurrencyToken()
         .IsRequired();
     ```
   - `c:\IT_System\tests\ItTms.Tests\IntegrationTests\Persistence\OptimisticConcurrencyTests.cs` dòng 76-79: xác minh thành công ngoại lệ `DbUpdateConcurrencyException` khi phiên làm việc thứ 2 cố tình ghi đè bản ghi có token cũ.

4. **Dữ liệu Mẫu (DataSeeder) & Kiểm thử Bảng 7.2 TC-01**:
   - `c:\IT_System\src\ItTms.Infrastructure\Seed\DataSeeder.cs`:
     - Dòng 46-55: Nạp 7 trạng thái chuẩn (`New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`) kèm nhãn tiếng Việt và class Bootstrap 5 badge CSS.
     - Dòng 70-95: Nạp 8 người dùng thử nghiệm bao gồm `admin`, `leader`, `staff1`, `staff2`, `user1`, `user2`, `deactivated` (`Status = UserStatus.Inactive`, `IsActive = false` phục vụ kiểm thử Bảng 7.2 TC-01), và `inactive_user`.
     - Dòng 118-185: Nạp 3 demo tickets với các trạng thái New, InProgress (kèm 2 comments) và Closed (kèm Feedback đánh giá 5 sao).
   - `c:\IT_System\tests\ItTms.Tests\IntegrationTests\Persistence\DatabaseMigrationAndSeederTests.cs`: Kiểm chứng seeder nạp đầy đủ dữ liệu và chạy lại lần 2 không nhân đôi dữ liệu (Idempotency).

5. **Dịch vụ Băm Mật khẩu (PasswordHasherService)**:
   - `c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs`: Sử dụng `Microsoft.AspNetCore.Identity.PasswordHasher<User>`, kiểm tra tính duy nhất của muối ngẫu nhiên (salt 128-bit) và xác thực mật khẩu chính xác qua `PasswordHasherTests.cs`.

---

## 2. LOGIC CHAIN

1. Từ **Observation 1 & 2**: Toàn bộ giải pháp được cấu hình theo chuẩn Clean Architecture và biên dịch không lỗi với mức kiểm tra cảnh báo khắt khe nhất (`TreatWarningsAsErrors`). 30 bài kiểm thử đơn vị và tích hợp chạy trực tiếp trên file CSDL SQLite vật lý và đều vượt qua 100%. Không phát hiện bất kỳ dấu hiệu gian lận nào (hardcode kết quả, bypass kiểm thử, facade giả mạo).
2. Từ **Observation 3**: `ConcurrencyTokenInterceptor` kết hợp với cấu hình `IsConcurrencyToken()` của EF Core 9 tạo nên cơ chế bảo vệ Optimistic Concurrency chặt chẽ trên SQLite. Khi nhiều tác nhân cùng chỉnh sửa một ticket, token cũ sẽ không khớp với cơ sở dữ liệu, kích hoạt `DbUpdateConcurrencyException`, giải quyết triệt để vấn đề Lost Updates.
3. Từ **Observation 4**: `DataSeeder` đáp ứng đầy đủ và vượt mức yêu cầu nghiệp vụ: nạp 7 trạng thái có màu badge, 8 tài khoản (bao gồm tài khoản `deactivated` có `IsActive = false` phục vụ Bảng 7.2 TC-01), 3 ticket demo kèm bình luận và đánh giá sao, và hoàn toàn Idempotent.
4. Từ **Observation 5**: `PasswordHasherService` áp dụng thuật toán PBKDF2 với muối ngẫu nhiên theo chuẩn bảo mật ASP.NET Core Identity / NIST, bảo vệ an toàn thông tin định danh người dùng.

---

## 3. CAVEATS

1. **Phạm vi SQLite PRAGMA `foreign_keys`**: `Program.cs` kích hoạt `foreign_keys = ON` trong hàm khởi tạo CSDL lúc startup. Vì SQLite PRAGMA `foreign_keys` có phạm vi theo từng kết nối, khuyến nghị bổ sung `Foreign Keys=True;Default Timeout=5` vào chuỗi kết nối trong `appsettings.json` ở Milestone 3 để bảo đảm 100% kết nối sinh ra trong suốt vòng đời web server đều bật ràng buộc khóa ngoại.
2. **Xác thực Null trong Password Hasher**: Khi truyền `null` vào `VerifyPassword`, Identity Hasher có thể ném `ArgumentNullException`. Cần bổ sung guard clause kiểm tra rỗng trước khi chuyển giao hoàn toàn cho controller xử lý form đăng nhập ở M3.
3. Không có caveat nào làm vô hiệu hóa kết quả đạt được của Milestone 1.

---

## 4. CONCLUSION

- **Kết luận thẩm định**: Công việc của `worker_m1` cho Milestone 1 đạt chất lượng cao, đúng thiết kế kiến trúc, an toàn bảo mật, toàn vẹn dữ liệu, và vượt qua 100% tiêu chí nghiệm thu.
- **Quyết định (Explicit Verdict)**: **`APPROVE`**.
- Cho phép tiến hành Milestone 2 (Design Patterns & Business Logic Engine: State Pattern, Observer Pattern, AuthorizeRolesAttribute).

---

## 5. VERIFICATION METHOD

Để tái hiện và kiểm chứng độc lập kết quả này:

1. **Biên dịch Solution**:
   ```powershell
   dotnet build c:\IT_System\ItTms.sln
   ```
   *Kỳ vọng*: Exit code 0, `0 Warning(s)`, `0 Error(s)`.

2. **Chạy Bộ Test Tự Động Toàn Diện**:
   ```powershell
   dotnet test c:\IT_System\ItTms.sln --verbosity normal
   ```
   *Kỳ vọng*: Exit code 0, 30 passed, 0 failed.

3. **Kiểm tra Báo cáo Chi tiết**:
   - Đọc tệp: `c:\IT_System\.agents\teamwork\reviewer_m1_2\review_report.md`.
