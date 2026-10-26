# BÁO CÁO TRIỂN KHAI KHẮC PHỤC KHIẾM KHUYẾT (IMPLEMENTATION REPORT)
## MILESTONE 1 REMEDIATION: BẢO VỆ BIÊN THỰC THỂ & ĐỒNG BỘ TEST SUITE ĐỐI KHÁNG

> **Kỹ sư triển khai**: `worker_m1_rem`  
> **Dự án**: IT Ticket Management System (.NET 9) — IT-TMS  
> **Thời gian**: 2026-10-25  
> **Trạng thái**: Hoàn thành 100% (Build: 0 errors/0 warnings, Tests: 109/109 Passed)

---

## 1. TỔNG QUAN THỰC THI (BLUF)

Theo chỉ đạo tại `DISPATCH.md` và các phương án từ `explorer_m1_rem_1`, `explorer_m1_rem_2`, `explorer_m1_rem_3`:
Toàn bộ 4 khiếm khuyết biên và bảo mật đã được khắc phục triệt để trên mã nguồn tầng Domain và Infrastructure. Đồng thời, 13 test case đối kháng trước đây được thiết kế nhằm vạch trần khiếm khuyết (assert fail/exception) đã được đồng bộ hóa thành các assertion nghiệm thu bảo vệ tính toàn vẹn hệ thống (assert failure với mã lỗi chuẩn và fail-closed).

| # | Hạng mục | Tệp tin chỉnh sửa | Bản vá áp dụng | Kết quả kiểm chứng |
|---|---|---|---|:---:|
| 1 | PasswordHasherService | `src/ItTms.Infrastructure/Security/PasswordHasherService.cs` | Guard rỗng/khoảng trắng; bọc `try-catch (FormatException)` trả về `false` |  PASS (Fail-Closed) |
| 2 | User Email Validation | `src/ItTms.Domain/Entities/User.cs` | Thêm `IsValidEmail` dùng `MailAddress.TryCreate`, chặn `..` liên tiếp, chặn chấm đầu/cuối, hỗ trợ domain nội bộ |  PASS (`USER_EMAIL_INVALID`) |
| 3 | Ticket Priority Enum | `src/ItTms.Domain/Entities/Ticket.cs` | Kiểm tra `!Enum.IsDefined(priority)` tuân thủ CA2263, trả về `TICKET_PRIORITY_INVALID` |  PASS (`TICKET_PRIORITY_INVALID`) |
| 4 | Attachment ContentType | `src/ItTms.Domain/Entities/Attachment.cs` | Kiểm tra `string.IsNullOrWhiteSpace` (`ATTACH_CONTENT_TYPE_REQUIRED`) và `Length > 100` (`ATTACH_CONTENT_TYPE_TOO_LONG`) |  PASS (Không văng NRE) |
| 5 | Test Suite Synchronization | `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` | Đồng bộ 13 assertions sang nghiệm thu bảo vệ |  PASS (109/109 tests) |

---

## 2. CHI TIẾT CÁC THAY ĐỔI MÃ NGUỒN

### 2.1. `PasswordHasherService.cs`: Bắt `FormatException` bảo vệ Fail-Closed
- **Vấn đề trước đây**: `_hasher.VerifyHashedPassword` gọi trực tiếp `Convert.FromBase64String`. Khi chuỗi hash bị biến dạng Base-64 (chứa ký tự đặc biệt không thuộc bảng mã Base-64), hệ thống ném `System.FormatException` làm văng unhandled exception (HTTP 500).
- **Mã nguồn đã cập nhật**:
  ```csharp
  public bool VerifyPassword(string hashedPassword, string providedPassword)
  {
      if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
          return false;

      try
      {
          var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
          return result != PasswordVerificationResult.Failed;
      }
      catch (FormatException)
      {
          return false;
      }
  }
  ```

### 2.2. `User.cs`: Kiểm tra cú pháp Email chuẩn RFC và tên miền nội bộ
- **Vấn đề trước đây**: Chỉ kiểm tra `IsNullOrWhiteSpace` và `Length > 150`, chấp nhận các chuỗi không phải email như `"plainaddress"`, `"missing_at_symbol.com"`, `"user name@domain.com"`.
- **Mã nguồn đã cập nhật**:
  - Bổ sung `using System.Net.Mail;`.
  - Bổ sung helper kiểm tra email:
    ```csharp
    private static bool IsValidEmail(string email, out string normalizedEmail)
    {
        normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail.Contains(".."))
            return false;

        if (!MailAddress.TryCreate(normalizedEmail, out var parsedEmail))
            return false;

        return parsedEmail.Address == normalizedEmail
            && !parsedEmail.Host.StartsWith('.')
            && !parsedEmail.Host.EndsWith('.')
            && !parsedEmail.User.StartsWith('.')
            && !parsedEmail.User.EndsWith('.');
    }
    ```
  - Trong `User.Create`:
    ```csharp
    if (!IsValidEmail(email, out var normalizedEmail))
        return Result<User>.Failure(new Error("USER_EMAIL_INVALID", "Định dạng địa chỉ email không hợp lệ.", ErrorType.Validation));
    ```
  - Gán `Email = normalizedEmail;`.
  - Đảm bảo 100% tương thích với các tài khoản seed: `admin@ittms.local`, `staff1@ittms.local`, `dummy@local`...

### 2.3. `Ticket.cs`: Xác thực Enum `TicketPriority`
- **Vấn đề trước đây**: C# enum cho phép ép kiểu số nguyên bất kỳ `(TicketPriority)(-1)`. Khi không kiểm tra, chuỗi `"-1"` bị ghi vào SQLite và làm sai lệch chỉ mục, đồng thời che giấu lỗi bằng fallback SLA ngầm định.
- **Mã nguồn đã cập nhật**:
  Sử dụng generic overload `Enum.IsDefined(priority)` vừa giải quyết triệt để khiếm khuyết vừa tuân thủ quy tắc phân tích mã nguồn .NET 9 (CA2263: Prefer the generic overload to avoid boxing):
  ```csharp
  if (!Enum.IsDefined(priority))
      return Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", "Mức độ ưu tiên yêu cầu không hợp lệ.", ErrorType.Validation));
  ```

### 2.4. `Attachment.cs`: Phòng vệ Content-Type và giới hạn độ dài
- **Vấn đề trước đây**: Lệnh `contentType.Trim().ToLowerInvariant()` tại dòng gán thực thể ném ngoại lệ `NullReferenceException` khi tham số `contentType` là `null`, vi phạm nguyên tắc Result pattern. Đồng thời chuỗi toàn khoảng trắng sẽ bị lưu thành rỗng và chuỗi quá dài gây tràn cột DB 100 ký tự.
- **Mã nguồn đã cập nhật**:
  ```csharp
  if (string.IsNullOrWhiteSpace(contentType))
      return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_REQUIRED", "Kiểu nội dung tệp tin không được để trống.", ErrorType.Validation));

  if (contentType.Length > 100)
      return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_TOO_LONG", "Kiểu nội dung tệp tin không được vượt quá 100 ký tự.", ErrorType.Validation));
  ```

---

## 3. ĐỒNG BỘ HÓA TEST SUITE ĐỐI KHÁNG

Trong `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs`, 13 test assertions được đồng bộ hóa từ dạng "vạch trần khiếm khuyết" sang dạng "nghiệm thu bảo vệ":

1. **`TicketCreate_WithNegativeOrUndefinedPriority_FailsValidation`** (4 test cases: `-1`, `0`, `999`, `int.MinValue`):
   - Đổi assertion từ `result.IsSuccess.Should().BeTrue()` sang:
     ```csharp
     result.IsFailure.Should().BeTrue("Mức độ ưu tiên ngoài dải định nghĩa Enum phải bị từ chối");
     result.Error.Code.Should().Be("TICKET_PRIORITY_INVALID");
     result.Error.Type.Should().Be(ErrorType.Validation);
     ```
2. **`AttachmentCreate_WithNullContentType_FailsValidation`** (1 test case):
   - Đổi assertion từ `act.Should().Throw<NullReferenceException>()` sang:
     ```csharp
     result.IsFailure.Should().BeTrue("Attachment.Create phải từ chối ContentType null hoặc rỗng thay vì ném NullReferenceException");
     result.Error.Code.Should().Be("ATTACH_CONTENT_TYPE_REQUIRED");
     result.Error.Type.Should().Be(ErrorType.Validation);
     ```
3. **`UserCreate_WithMalformedEmailFormat_FailsValidation`** (7 test cases: `plainaddress`, `missing_at_symbol.com`, `@missingusername.com`, `username@`, `username@.com`, `user name@domain.com`, `user@domain..com`):
   - Đổi assertion từ `result.IsSuccess.Should().BeTrue()` sang:
     ```csharp
     result.IsFailure.Should().BeTrue($"Định dạng email bất hợp lệ '{malformedEmail}' phải bị từ chối");
     result.Error.Code.Should().Be("USER_EMAIL_INVALID");
     result.Error.Type.Should().Be(ErrorType.Validation);
     ```
4. **`PasswordHasher_VerifyPassword_WithNonBase64MalformedHash_ReturnsFalseFailClosed`** (1 test case):
   - Đổi assertion từ `act.Should().Throw<FormatException>()` sang:
     ```csharp
     var isValid = _hasher.VerifyPassword(nonBase64Hash, "AnyPassword123");
     isValid.Should().BeFalse("PasswordHasherService must catch FormatException and fail-closed with false for malformed Base-64 hash strings");
     ```

---

## 4. KẾT QUẢ XÁC MINH KIỂM THỬ

### 4.1. Lệnh Biên Dịch (`dotnet build`)
```powershell
dotnet build c:\IT_System\ItTms.sln
```
**Kết quả**:
- Build succeeded.
- `0 Warning(s)`
- `0 Error(s)`
- Thời gian biên dịch: 21.39 giây.

### 4.2. Lệnh Kiểm Thử Toàn Diện (`dotnet test`)
```powershell
dotnet test c:\IT_System\ItTms.sln --logger "console;verbosity=normal"
```
**Kết quả**:
- `Test Run Successful.`
- **Total tests**: `109`
- **Passed**: `109`
- **Failed**: `0`
- **Skipped**: `0`
- Thời gian thực thi: `27.3511 Seconds`.

---

## 5. KẾT LUẬN

1. Không xảy ra bất kỳ sự hồi quy (regression) nào trên toàn bộ 109 test cases của hệ thống.
2. Dữ liệu nạp mẫu (`DataSeeder`), tính tương thích đồng thời (`AdversarialConcurrencyStressTests`), và kiểm thử tính hạn cam kết SLA (`AdversarialSlaStressTests`) hoạt động chuẩn xác 100%.
3. Hệ thống hoàn toàn sẵn sàng cho các giai đoạn tiếp theo của Milestone 1 và Milestone 2.
