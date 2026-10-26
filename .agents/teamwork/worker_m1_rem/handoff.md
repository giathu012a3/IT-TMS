# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — WORKER M1 REMEDIATION

> **Người thực hiện**: `worker_m1_rem` (Implementer / QA / Specialist)  
> **Người nhận**: `orchestrator_1` / caller (`9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Milestone**: Milestone 1 Remediation  
> **Thời gian**: 2026-10-25T09:03:00Z  
> **Loại bàn giao**: Hard Handoff (Nhiệm vụ hoàn thành trọn vẹn)

---

## 1. Observation (Quan Sát Thực Nghiệm)

1. **Khảo sát ban đầu**:
   - `challenger_m1_2` đã vạch ra 4 khiếm khuyết biên và viết 13 test assertions để minh chứng sự tồn tại của lỗi trong `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs`.
   - `explorer_m1_rem_1`, `explorer_m1_rem_2`, và `explorer_m1_rem_3` đã xây dựng các kế hoạch khắc phục và cảnh báo nguy cơ: nếu chỉ sửa code mà không đồng bộ 13 test assertions, tỷ lệ pass sẽ sụt giảm từ 100% (109/109) xuống 88.07% (96/109).

2. **Các tệp mã nguồn và hiện trạng trước sửa đổi**:
   - `src/ItTms.Infrastructure/Security/PasswordHasherService.cs` (Dòng 20–24): Gọi trực tiếp `_hasher.VerifyHashedPassword` không bọc try-catch, ném `System.FormatException` khi gặp chuỗi Base-64 biến dạng.
   - `src/ItTms.Domain/Entities/User.cs` (Dòng 47–52): Thiếu hàm xác thực cú pháp email, chấp nhận `plainaddress`, `user name@domain.com`, `user@domain..com`.
   - `src/ItTms.Domain/Entities/Ticket.cs` (Dòng 66–71): Thiếu kiểm tra `TicketPriority` enum hợp lệ, nhận cả các số nguyên âm hoặc ngoài dải định nghĩa.
   - `src/ItTms.Domain/Entities/Attachment.cs` (Dòng 34–48): Thiếu kiểm tra `contentType` null/whitespace trước khi gọi `contentType.Trim().ToLowerInvariant()`, làm phát sinh ngoại lệ `System.NullReferenceException`.

3. **Hiện tượng phát hiện trong quá trình biên dịch**:
   - Khi áp dụng `!Enum.IsDefined(typeof(TicketPriority), priority)`, trình biên dịch .NET 9 phát hiện cảnh báo mã lỗi phân tích chất lượng (treated as error):
     `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs(69,14): error CA2263: Prefer the generic overload 'System.Enum.IsDefined<TEnum>(TEnum)' instead of 'System.Enum.IsDefined(System.Type, object)'`
   - Đã được khắc phục ngay lập tức thành cú pháp tối ưu: `!Enum.IsDefined(priority)`.

4. **Kết quả xác minh thực tế sau khi áp dụng toàn bộ bản vá**:
   - Lệnh `dotnet build c:\IT_System\ItTms.sln`:
     ```
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:21.39
     ```
   - Lệnh `dotnet test c:\IT_System\ItTms.sln --logger "console;verbosity=normal"`:
     ```
     Test Run Successful.
     Total tests: 109
          Passed: 109
      Total time: 27.3511 Seconds
     ```

---

## 2. Logic Chain (Chuỗi Lập Luận Suy Luận)

1. **Từ Quan sát 2.1 đến Bản vá PasswordHasher**:
   - `Convert.FromBase64String` ném `FormatException` đối với payload không phải Base-64 chuẩn. Vì đây là hành vi mặc định của thư viện Identity V3, bắt `FormatException` và trả về `false` là giải pháp an toàn, triệt tiêu nguy cơ máy chủ gặp unhandled exception (HTTP 500) và tuân thủ nguyên tắc Fail-Closed.
2. **Từ Quan sát 2.2 đến Bản vá User.Create**:
   - `MailAddress.TryCreate` hỗ trợ các tên miền nội bộ doanh nghiệp như `@ittms.local` và `dummy@local` (không bắt buộc có TLD công khai). Tuy nhiên, `MailAddress` vẫn chấp nhận Display Name hoặc ký tự RFC mở rộng chứa `..`. Do đó, kết hợp `normalizedEmail.Contains("..")`, `parsedEmail.Address == normalizedEmail`, và kiểm tra không bắt đầu/kết thúc bằng dấu chấm tạo thành một lớp phòng vệ tuyến tính $O(N)$ chống ReDoS, đồng thời bảo toàn 100% dữ liệu seed trong `DataSeeder.cs`.
3. **Từ Quan sát 2.3 & 3 đến Bản vá Ticket.Create**:
   - C# enum không tự động ràng buộc giá trị hợp lệ ở runtime. Việc bổ sung `!Enum.IsDefined(priority)` loại bỏ hoàn toàn việc ghi các chuỗi rác như `"-1"` vào bảng SQLite, bảo vệ chỉ mục `IX_Tickets_StatusId_Priority_CreatedAt`. Sử dụng generic overload thỏa mãn quy tắc `CA2263` của .NET 9.
4. **Từ Quan sát 2.4 đến Bản vá Attachment.Create**:
   - Việc bổ sung `string.IsNullOrWhiteSpace(contentType)` trả về `ATTACH_CONTENT_TYPE_REQUIRED` ngăn ngừa triệt để `NullReferenceException` trước khi gọi `.Trim()`. Giới hạn `contentType.Length > 100` (`ATTACH_CONTENT_TYPE_TOO_LONG`) bảo đảm toàn vẹn ràng buộc cột cơ sở dữ liệu `HasMaxLength(100)`.
5. **Từ Quan sát 1 & 4 đến Kết quả toàn diện**:
   - Việc đồng bộ hóa 13 assertions trong `DomainBoundaryAndSecurityAdversarialTests.cs` từ "chứng minh khiếm khuyết tồn tại" sang "khẳng định lỗi bị từ chối với Error Code chuẩn và trả về false" đảm bảo toàn bộ 109 test cases đều chuyển sang trạng thái xanh (Passed), khẳng định không có sự suy thoái hay hồi quy.

---

## 3. Caveats (Lưu Ý & Giả Định)

- **Các trường hợp enum mở rộng**: Hiện tại `TicketCategory` chưa được bổ sung `Enum.IsDefined` do không nằm trong yêu cầu dispatch của 4 remediations, tuy nhiên trường này hiện không bị khai thác và có thể bổ sung nếu có yêu cầu thêm.
- **Dữ liệu đính kèm**: Hệ thống hiện chỉ kiểm tra MIME type chuỗi và dung lượng 16MB ở tầng Domain; việc quét mã độc file nhị phân sẽ thuộc phạm vi tầng I/O Controller hoặc Antivirus scanner.
- Không có giả định chưa được kiểm chứng nào khác. "No caveats."

---

## 4. Conclusion (Kết Luận)

Bốn khiếm khuyết biên và bảo mật đã được khắc phục hoàn chỉnh, đạt chuẩn Clean Architecture và triết lý `#Argon-Omega`:
1. `PasswordHasherService` fail-closed an toàn với `FormatException`.
2. `User.Create` từ chối triệt để email dị dạng, chặn dấu chấm kép `..`, hỗ trợ đầy đủ các tên miền kiểm thử và nội bộ.
3. `Ticket.Create` từ chối các giá trị priority ngoài enum với `TICKET_PRIORITY_INVALID`.
4. `Attachment.Create` từ chối Content-Type rỗng/whitespace/quá 100 ký tự với mã lỗi chuẩn, loại bỏ hoàn toàn `NullReferenceException`.
5. Toàn bộ giải pháp biên dịch với `0 Warning(s), 0 Error(s)` và vượt qua `109/109` bài kiểm thử tự động.

---

## 5. Verification Method (Phương Pháp Xác Minh Độc Lập)

Người kiểm định độc lập (Auditor) có thể kiểm chứng lại kết quả bằng các bước sau:

1. **Biên dịch toàn bộ giải pháp**:
   ```powershell
   dotnet build c:\IT_System\ItTms.sln
   ```
   *Điều kiện hợp lệ*: Kết quả trả về `Build succeeded. 0 Warning(s), 0 Error(s)`.

2. **Chạy toàn bộ bộ kiểm thử**:
   ```powershell
   dotnet test c:\IT_System\ItTms.sln --logger "console;verbosity=normal"
   ```
   *Điều kiện hợp lệ*: Kết quả trả về `Test Run Successful. Total tests: 109. Passed: 109. Failed: 0.`

3. **Kiểm tra trực tiếp các tệp tin đã sửa đổi**:
   - `c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs`
   - `c:\IT_System\src\ItTms.Domain\Entities\User.cs`
   - `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs`
   - `c:\IT_System\src\ItTms.Domain\Entities\Attachment.cs`
   - `c:\IT_System\tests\ItTms.Tests\AdversarialTests\DomainBoundaryAndSecurityAdversarialTests.cs`
