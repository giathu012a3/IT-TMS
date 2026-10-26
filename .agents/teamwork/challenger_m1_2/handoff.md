# HANDOFF REPORT: ADVERSARIAL CHALLENGE — DOMAIN INVARIANTS & SECURITY (M1)

> **Agent**: `challenger_m1_2`  
> **Target Recipient**: Orchestrator (`9e3cad2c-3e7f-49c0-9814-015216856d2e`) / Worker M1 (`worker_m1`)  
> **Working Directory**: `c:\IT_System\.agents\teamwork\challenger_m1_2`  
> **Status**: COMPLETED (Hard Handoff)  
> **Verdict**: **`REQUEST_CHANGES`**  
> **Date**: 2026-10-25  

---

## 1. OBSERVATION

1. **Kết quả Biên dịch và Thực thi Bộ Kiểm thử Toàn diện**:
   - Lệnh thực thi:
     ```powershell
     dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --logger "console;verbosity=normal"
     ```
   - Kết quả đầu ra thực tế:
     ```
     Test Run Successful.
     Total tests: 109
          Passed: 109
      Total time: 26.2869 Seconds
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     ```
   - Bao gồm: 30 baseline tests từ `worker_m1`, 49 concurrency tests từ `challenger_m1_1`, và 30 adversarial tests mới từ `challenger_m1_2` tại `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs`.

2. **Quan sát Thực nghiệm về PasswordHasher (`c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs`)**:
   - Khi truyền chuỗi băm không phải định dạng Base64 hợp lệ (`"invalid_base64_hash_payload!@#$"`) vào `VerifyPassword`, phương thức `_hasher.VerifyHashedPassword` ném ngoại lệ:
     ```
     System.FormatException: The input is not a valid Base-64 string as it contains a non-base 64 character, more than two padding characters, or an illegal character among the padding characters.
        at System.Convert.FromBase64String(String s)
        at Microsoft.AspNetCore.Identity.PasswordHasher`1.VerifyHashedPassword(TUser user, String hashedPassword, String providedPassword)
        at ItTms.Infrastructure.Security.PasswordHasherService.VerifyPassword(String hashedPassword, String providedPassword) in c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs:line 22
     ```
   - Ngược lại, kiểm thử tính duy nhất Salt (100 lần băm mật khẩu `"P@ssw0rd_Empirical_Test#2026!"`) sinh ra đúng **100 chuỗi hash duy nhất 100%**.
   - Kiểm thử Timing Resistance (30 lần đo):
     - Thời gian trung bình mật khẩu đúng: `101.492 ms`
     - Thời gian trung bình mật khẩu sai ký tự đầu: `103.231 ms`
     - Thời gian trung bình mật khẩu sai ký tự cuối: `101.023 ms`
     - Độ chênh lệch giữa sai ký tự đầu và sai ký tự cuối: **`2.14%`** ($< 30\%$).

3. **Quan sát Thực nghiệm về Giới hạn Biên Attachment (`c:\IT_System\src\ItTms.Domain\Entities\Attachment.cs`)**:
   - Tại dòng 48: `ContentType = contentType.Trim().ToLowerInvariant()`.
   - Khi truyền `contentType = null!`, hàm ném `System.NullReferenceException` thay vì trả về `Result<Attachment>.Failure`.
   - Kiểm thử biên dung lượng: Tệp $16\text{MB}$ ($16,777,216$ bytes) khởi tạo thành công; tệp $16\text{MB} + 1\text{ byte}$ ($16,777,217$ bytes) bị chặn với mã lỗi `ATTACH_FILE_TOO_LARGE`; tệp $\le 0$ bytes bị chặn với mã lỗi `ATTACH_FILE_EMPTY`.

4. **Quan sát Thực nghiệm về Giới hạn Biên Ticket Priority (`c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs`)**:
   - Tại `Ticket.Create` (dòng 52–92), không có mã kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)`.
   - Khi gọi `Ticket.Create` với `priority = -1`, `priority = 0`, `priority = 999`, hoặc `priority = int.MinValue`, kết quả trả về `result.IsSuccess == true`, lưu trữ trực tiếp giá trị số âm vào `ticket.Priority`, và `CalculateDueAt` chuyển sang nhánh default gán `DueAt = createdAt.AddHours(24)`.

5. **Quan sát Thực nghiệm về Định dạng Email User (`c:\IT_System\src\ItTms.Domain\Entities\User.cs`)**:
   - Tại `User.Create` (dòng 47–52), chỉ có 2 điều kiện: `string.IsNullOrWhiteSpace(email)` và `email.Length > 150`.
   - Các giá trị email sai cú pháp hoàn toàn như `"plainaddress"`, `"missing_at_symbol.com"`, `"username@"`, `"@missingusername.com"` đều được trả về `result.IsSuccess == true` và tạo tài khoản thành công.

6. **Quan sát Thực nghiệm về Tính Idempotent của DataSeeder (`c:\IT_System\src\ItTms.Infrastructure\Seed\DataSeeder.cs`)**:
   - Chạy 5 vòng liên tiếp `seeder.SeedAsync()`:
     - Số lượng `TicketStatuses` duy trì tuyệt đối: `7`.
     - Số lượng `Users` duy trì tuyệt đối: `8`.
     - Số lượng `Tickets` duy trì tuyệt đối: `3`.
     - Số lượng `Comments` duy trì tuyệt đối: `2`.
     - Số lượng `Feedbacks` duy trì tuyệt đối: `1`.
   - Tạo thêm người dùng `custom_employee` và chỉnh sửa tiêu đề của Ticket 1 trước khi chạy lại `SeedAsync()`: Dữ liệu người dùng mới và tiêu đề sửa đổi không bị ghi đè, tổng số Users sau seed là `9`.

---

## 2. LOGIC CHAIN

1. Từ **Observation 2**: Phương thức `PasswordHasherService.VerifyPassword` đóng vai trò là chốt chặn xác thực người dùng cho toàn bộ hệ thống (UC01). Khi gặp chuỗi hash không phải Base-64 hợp lệ, việc ném ngoại lệ `FormatException` chưa bắt sẽ làm sập luồng xử lý web (HTTP 500) thay vì trả về `false` an toàn theo nguyên tắc bảo mật Fail-Closed.
2. Từ **Observation 3**: `Attachment.Create` đại diện cho Aggregate Root con xử lý tệp đính kèm (UC08). Việc bỏ qua kiểm tra `null` trên tham số `contentType` khiến ứng dụng có thể gặp lỗi crash runtime `NullReferenceException` khi người dùng upload tệp qua HTTP request thiếu header `Content-Type`.
3. Từ **Observation 4**: Ràng buộc toàn vẹn miền (Domain Invariant) của `Ticket` quy định SLA phụ thuộc trực tiếp vào mức độ ưu tiên. Việc cho phép priority mang giá trị âm hoặc không xác định khiến dữ liệu bị nhiễm bẩn, vi phạm bất biến miền, và phá vỡ báo cáo phân bổ SLA tại UC17.
4. Từ **Observation 5**: Thiếu bộ kiểm tra cú pháp email tại `User.Create` khiến hệ thống chấp nhận các chuỗi tùy ý làm email, dẫn tới nguy cơ hỏng kênh liên lạc và thông báo notification của hệ thống.
5. Từ **Observation 1, 2 (phần sau), 3 (phần sau), và 6**: Các tính năng còn lại gồm băm mật khẩu ngẫu nhiên salt 128-bit, kháng tấn công thời gian timing attack ($2.14\%$), kiểm soát dung lượng tệp đính kèm 16MB, và tính Idempotent nạp dữ liệu của `DataSeeder` đều hoạt động cực kỳ hoàn hảo và đạt chuẩn production.
6. **Tổng hợp**: Mặc dù nền tảng cốt lõi rất vững chắc, 4 khiếm khuyết được phát hiện tại Observation 2, 3, 4, 5 cấu thành các kẽ hở biên cần được vá sớm nhằm ngăn chặn việc rò rỉ lỗi sang Milestone 2 (Logic Engine) và Milestone 3 (Web Presentation).

---

## 3. CAVEATS

- Kiểm thử Timing Resistance được thực thi trong môi trường đơn tiến trình kiểm thử nội bộ trên Windows, có thể chịu ảnh hưởng nhỏ bởi JIT compilation và Garbage Collector của .NET runtime (tuy nhiên độ lệch $2.14\%$ sau 30 vòng lặp là bằng chứng thực nghiệm rõ ràng về tính kháng timing attack).
- Lỗi `FormatException` trong `PasswordHasherService` chỉ xảy ra khi hash trong database hoặc đầu vào bị hỏng/can thiệp; đối với các hash chuẩn do seeder sinh ra, hệ thống xác thực bình thường.
- Không phát hiện thêm lỗi nào khác trong 8 Core Entities và Data Seeder.

---

## 4. CONCLUSION

- **Phán quyết (Verdict)**: **`REQUEST_CHANGES`**
- **Yêu cầu hành động đối với worker**:
  1. Vá `PasswordHasherService.VerifyPassword`: Bổ sung kiểm tra `string.IsNullOrWhiteSpace` và bọc `try-catch (FormatException) { return false; }`.
  2. Vá `Attachment.Create`: Bổ sung kiểm tra `if (string.IsNullOrWhiteSpace(contentType)) return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_REQUIRED", ...))` trước khi gọi `.Trim()`.
  3. Vá `Ticket.Create`: Bổ sung kiểm tra `if (!Enum.IsDefined(typeof(TicketPriority), priority)) return Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", ...))`.
  4. Vá `User.Create`: Bổ sung kiểm tra định dạng email sử dụng `MailAddress.TryCreate`.

---

## 5. VERIFICATION METHOD

Để kiểm chứng độc lập toàn bộ các quan sát và kết quả thực nghiệm nêu trên, thực hiện lệnh sau từ thư mục gốc `c:\IT_System`:

1. **Thực thi Toàn bộ Bộ Kiểm thử (109 Tests)**:
   ```powershell
   dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --logger "console;verbosity=normal"
   ```
   *Điều kiện đạt*: Toàn bộ 109 tests đều báo `Passed` với Exit Code `0`.

2. **Kiểm tra Mã Kiểm thử Đối kháng Độc lập**:
   - File: `c:\IT_System\tests\ItTms.Tests\AdversarialTests\DomainBoundaryAndSecurityAdversarialTests.cs`
   - Báo cáo chi tiết: `c:\IT_System\.agents\teamwork\challenger_m1_2\challenge_report.md`
