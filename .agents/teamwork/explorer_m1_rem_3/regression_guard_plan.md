# KẾ HOẠCH BẢO VỆ CHỐNG HỒI QUY VÀ ĐỒNG BỘ TEST SUITE (REGRESSION GUARD PLAN)
## DỰ ÁN: IT TICKET MANAGEMENT SYSTEM (.NET 9) — MILESTONE 1 REMEDIATION

> **Người lập**: `explorer_m1_rem_3` (Investigation & Test Suite Synchronization Specialist)  
> **Phạm vi khảo sát**: Toàn bộ 109+ bài kiểm thử trong `tests/ItTms.Tests`, tập trung vào các test do `challenger_m1_1` và `challenger_m1_2` bổ sung.  
> **Mục tiêu**: Đảm bảo 100% test cases (109/109) vượt qua kiểm thử sau khi áp dụng 4 bản vá khắc phục khiếm khuyết biên từ `challenger_m1_2`, triệt tiêu hoàn toàn rủi ro hồi quy (zero regression).  
> **Nguyên tắc**: Chuẩn nhận thức `#Argon-Omega` — BLUF-MECE, First-Principles, Causal Constructivism, Zero Fluff.

---

## 1. TỔNG QUAN HỆ THỐNG KIỂM THỬ VÀ NGHỊCH LÝ "EMPIRICAL EXPOSURE" (BLUF)

### 1.1. Hiện trạng phân bổ Test Suite (109 Test Cases)
Toàn bộ solution hiện đang có **109 test cases**, biên dịch không cảnh báo (`0 Warning(s), 0 Error(s)`) và thực thi 100% PASS trên nền tảng .NET 9 (`net9.0`) với thời gian chạy xấp xỉ `29.85` giây:

| # | Phân nhóm kiểm thử | File nguồn | Số test cases | Mục đích kiểm chứng |
|---|---|---|:---:|---|
| 1 | Baseline Domain | `UnitTests/Domain/TicketInvariantTests.cs` | 15 | Khởi tạo Ticket, tính SLA DueAt, quy tắc CanComment, Reject InProgress |
| 2 | Baseline Domain | `UnitTests/Domain/UserInvariantTests.cs` | 3 | Khởi tạo User, chặn tự vô hiệu hóa tài khoản Admin (TC-10) |
| 3 | Baseline Domain | `UnitTests/Domain/ResultMonadTests.cs` | 4 | Monad Result<T>, Success/Failure, truy cập Value an toàn |
| 4 | Baseline Security | `UnitTests/Security/PasswordHasherTests.cs` | 5 | PBKDF2 salt ngẫu nhiên, xác thực đúng/sai mật khẩu |
| 5 | Baseline Persistence | `IntegrationTests/Persistence/OptimisticConcurrencyTests.cs` | 1 | Xung đột ghi đè ConcurrencyToken trên SQLite |
| 6 | Baseline Persistence | `IntegrationTests/Persistence/DatabaseMigrationAndSeederTests.cs` | 1 | Tự động Migrate và Seed 7 status, 8 users, 3 tickets, tính Idempotent |
| 7 | Concurrency Adversarial | `IntegrationTests/Persistence/AdversarialConcurrencyStressTests.cs` | 12 | Đua đa luồng 10 tasks, xung đột chuyển trạng thái, xóa vé, reload-retry |
| 8 | SLA Boundary Adversarial | `UnitTests/Domain/AdversarialSlaStressTests.cs` | 24 | Năm nhuận 2028, thế kỷ 2000/2100, DST, biên nano-giây, Enum không xác định |
| 9 | Security & Boundary Adversarial | `AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` | 44 | Tệp đính kèm 16MB, Salt uniqueness, Timing side-channel, và 4 khiếm khuyết biên |
| **Tổng** | **Toàn Solution** | **9 files kiểm thử** | **109** | **100% PASSED** |

### 1.2. Nghịch lý "Empirical Exposure" (Critical Finding)
Trong file `AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs`, đặc phái viên `challenger_m1_2` đã thiết kế **13 test cases** nhằm mục đích **chứng minh sự tồn tại của khiếm khuyết bằng thực nghiệm**. Các test này hiện đang assert rằng:
1. `Ticket.Create` chấp nhận priority âm/không hợp lệ (`result.IsSuccess.Should().BeTrue()`).
2. `Attachment.Create` ném `NullReferenceException` khi `contentType` là `null` (`act.Should().Throw<NullReferenceException>()`).
3. `User.Create` chấp nhận email sai cú pháp (`result.IsSuccess.Should().BeTrue()`).
4. `PasswordHasherService.VerifyPassword` ném unhandled `FormatException` (`act.Should().Throw<FormatException>()`).

> ⚠️ **CẢNH BÁO NGUY CƠ HỒI QUY CỰC LỚN**:  
> Nếu kỹ sư sửa mã (`worker`) chỉ tiến hành sửa mã nguồn trong `src/` mà **không đồng bộ các assertion trong `DomainBoundaryAndSecurityAdversarialTests.cs`**, thì ngay lập tức **chính xác 13 bài test sẽ bị FAILED**, làm tỷ lệ pass tụt dốc từ $100\%$ ($109/109$) xuống còn **`88.07%`** ($96/109$).  
> Do đó, việc khắc phục lỗi mã nguồn và đồng bộ test suite là **hai mặt bất khả phân ly** của một giao dịch nguyên tử.

---

## 2. MA TRẬN ĐỒNG BỘ 13 TEST CASES CỦA CHALLENGER_M1_2

Dưới đây là chi tiết kỹ thuật chính xác cho việc chuyển đổi 13 test cases từ trạng thái "vạch trần lỗi" sang trạng thái "nghiệm thu bản vá":

```
[Hiện trạng: Empirical Expose]                  [Bản vá Mã nguồn]                [Đồng bộ Test Assertion]
----------------------------------------------------------------------------------------------------------------
TicketCreate_WithNegativeOrUndefinedPriority   -> Ticket.Create kiểm tra       -> result.IsFailure.Should().BeTrue()
  Assert: IsSuccess == true (4 test cases)        !Enum.IsDefined(...)           Assert: Error.Code == "TICKET_PRIORITY_INVALID"

AttachmentCreate_WithNullContentType           -> Attachment.Create kiểm tra   -> result.IsFailure.Should().BeTrue()
  Assert: Throw NullReferenceException (1 test)   string.IsNullOrWhiteSpace      Assert: Error.Code == "ATTACH_CONTENT_TYPE_REQUIRED"

UserCreate_WithMalformedEmailFormat            -> User.Create kiểm tra         -> result.IsFailure.Should().BeTrue()
  Assert: IsSuccess == true (7 test cases)        MailAddress + Regex/NoDots     Assert: Error.Code == "USER_EMAIL_INVALID"

PasswordHasher_VerifyPassword_NonBase64        -> PasswordHasherService bọc    -> isValid.Should().BeFalse()
  Assert: Throw FormatException (1 test)          try-catch FormatException      (Fail-closed, không ném exception)
```

### 2.1. Nhóm 1: `TicketCreate_WithNegativeOrUndefinedPriority` (4 Test Cases)
- **Vị trí**: `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` (Dòng 41–65)
- **Dữ liệu đầu vào**: `rawPriority` $\in \{-1, 0, 999, -2147483648\}$
- **Hiện trạng**:
  ```csharp
  // BEFORE (Assert lỗi tồn tại):
  result.IsSuccess.Should().BeTrue("DEFECT: Ticket.Create currently lacks Enum.IsDefined check...");
  ((int)result.Value!.Priority).Should().Be(rawPriority);
  ```
- **Sau đồng bộ (Assertion chuẩn hóa)**:
  ```csharp
  // AFTER (Assert bảo vệ bất biến):
  [Theory]
  [InlineData(-1)]
  [InlineData(0)]
  [InlineData(999)]
  [InlineData(int.MinValue)]
  public void TicketCreate_WithNegativeOrUndefinedPriority_FailsValidation(int rawPriority)
  {
      var priority = (TicketPriority)rawPriority;

      var result = Ticket.Create(
          title: "Test Boundary Priority",
          description: "Adversarial testing negative or invalid enum priority",
          priority: priority,
          category: TicketCategory.Hardware,
          creatorId: 1
      );

      result.IsFailure.Should().BeTrue("Mức độ ưu tiên ngoài dải định nghĩa Enum phải bị từ chối");
      result.Error.Code.Should().Be("TICKET_PRIORITY_INVALID");
  }
  ```

### 2.2. Nhóm 2: `AttachmentCreate_WithNullContentType` (1 Test Case)
- **Vị trí**: `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` (Dòng 160–175)
- **Hiện trạng**:
  ```csharp
  // BEFORE:
  Action act = () => Attachment.Create(1, "guid.png", "orig.png", null!, 1024);
  act.Should().Throw<NullReferenceException>();
  ```
- **Sau đồng bộ (Assertion chuẩn hóa & Mở rộng biên)**:
  ```csharp
  // AFTER:
  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void AttachmentCreate_WithNullOrWhitespaceContentType_FailsValidation(string? invalidContentType)
  {
      var result = Attachment.Create(
          ticketId: 1,
          fileName: "guid-name.png",
          originalFileName: "screenshot.png",
          contentType: invalidContentType!,
          fileSize: 1024
      );

      result.IsFailure.Should().BeTrue("Kiểu nội dung MIME type không được null hoặc rỗng");
      result.Error.Code.Should().Be("ATTACH_CONTENT_TYPE_REQUIRED");
  }
  ```
  *(Lưu ý: Mở rộng Theory với 3 trường hợp null, rỗng, khoảng trắng nâng cao độ phủ kiểm thử).*

### 2.3. Nhóm 3: `UserCreate_WithMalformedEmailFormat` (7 Test Cases)
- **Vị trí**: `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` (Dòng 180–203)
- **Dữ liệu đầu vào**: `"plainaddress"`, `"missing_at_symbol.com"`, `"@missingusername.com"`, `"username@"`, `"username@.com"`, `"user name@domain.com"`, `"user@domain..com"`
- **Hiện trạng**:
  ```csharp
  // BEFORE:
  result.IsSuccess.Should().BeTrue("DEFECT: User.Create lacks email format validation...");
  ```
- **Sau đồng bộ (Assertion chuẩn hóa)**:
  ```csharp
  // AFTER:
  [Theory]
  [InlineData("plainaddress")]
  [InlineData("missing_at_symbol.com")]
  [InlineData("@missingusername.com")]
  [InlineData("username@")]
  [InlineData("username@.com")]
  [InlineData("user name@domain.com")]
  [InlineData("user@domain..com")]
  public void UserCreate_WithMalformedEmailFormat_FailsValidation(string malformedEmail)
  {
      var result = User.Create(
          username: "testuser",
          email: malformedEmail,
          passwordHash: "validhash",
          fullName: "Test User",
          role: UserRole.User
      );

      result.IsFailure.Should().BeTrue($"Định dạng email bất hợp lệ '{malformedEmail}' phải bị từ chối");
      result.Error.Code.Should().Be("USER_EMAIL_INVALID");
  }
  ```

### 2.4. Nhóm 4: `PasswordHasher_VerifyPassword_WithNonBase64MalformedHash` (1 Test Case)
- **Vị trí**: `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` (Dòng 286–298)
- **Hiện trạng**:
  ```csharp
  // BEFORE:
  Action act = () => _hasher.VerifyPassword("invalid_base64_hash_payload!@#$", "AnyPassword123");
  act.Should().Throw<FormatException>().WithMessage("*not a valid Base-64 string*");
  ```
- **Sau đồng bộ (Assertion chuẩn hóa)**:
  ```csharp
  // AFTER:
  [Fact]
  public void PasswordHasher_VerifyPassword_WithNonBase64MalformedHash_ReturnsFalseWithoutThrowing()
  {
      const string nonBase64Hash = "invalid_base64_hash_payload!@#$";

      // Act
      var isValid = _hasher.VerifyPassword(nonBase64Hash, "AnyPassword123");

      // Assert: Nguyên tắc Fail-Closed, từ chối an toàn mà không ném crash exception
      isValid.Should().BeFalse("Dịch vụ PasswordHasher phải xử lý chuỗi Base-64 sai định dạng và trả về false");
  }
  ```

---

## 3. PHÂN TÍCH RỦI RO HỒI QUY ĐỐI VỚI 96 TEST CASES HIỆN CÓ

Để bảo đảm việc vá lỗi không gây gãy vỡ (break) bất kỳ bài kiểm thử nào trong số 96 test cases còn lại, chúng tôi đã rà soát toàn bộ cây phụ thuộc và quan hệ dữ liệu:

### 3.1. Rủi ro 1: Thuật toán kiểm tra Email gây sập hệ thống do loại trừ tên miền nội bộ (`@local`)
- **Phát hiện quan trọng qua thực nghiệm**:
  Hệ thống sử dụng các email có tên miền nhãn đơn (`single-label domain`) như `@local` hoặc `@ittms.local` tại nhiều vị trí trọng yếu:
  - `PasswordHasherService.cs` (Dòng 13): Khởi tạo trường tĩnh `DummyUser`:
    ```csharp
    private static readonly User DummyUser = User.Create("dummy", "dummy@local", "dummyhash", "Dummy", Domain.Enums.UserRole.User).Value;
    ```
    *(Nếu `User.Create` từ chối `"dummy@local"`, lệnh truy cập `.Value` sẽ lập tức ném `InvalidOperationException`, làm sập dịch vụ băm mật khẩu ngay khi khởi động!)*
  - `AdversarialSlaStressTests.cs` (Dòng 176): Dùng `"u1@local"`.
  - `OptimisticConcurrencyTests.cs` (Dòng 42): Dùng `"author@local"`.
  - `AdversarialConcurrencyStressTests.cs` (Dòng 47–50): Dùng `"admin_test@local"`, `"leader_test@local"`, `"staff_test@local"`, `"user_test@local"`.
- **Hành vi thực nghiệm của `System.Net.Mail.MailAddress` trên .NET 9**:
  - `MailAddress.TryCreate("dummy@local", out var addr)`: **Trả về `true`**, `addr.Address == "dummy@local"`.
  - Do đó, nếu dùng `MailAddress.TryCreate`, các email `@local` hoàn toàn hợp lệ, không gây sập `DummyUser`.
- **Lỗ hổng kép nguy hiểm trong đề xuất gốc của `challenger_m1_2`**:
  Trong `challenger_m1_2/challenge_report.md` (Mục 3.4), đề xuất viết:
  ```csharp
  if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var parsedEmail) || parsedEmail.Address != email.Trim().ToLowerInvariant())
      return Result<User>.Failure(new Error("USER_EMAIL_INVALID", ...));
  ```
  Thực nghiệm F# trên .NET 9 đã chứng minh:
  - Chuỗi `"user@domain..com"` (có 2 dấu chấm liên tiếp) **VẪN ĐƯỢC `MailAddress.TryCreate` CHẤP NHẬN** (`parsedEmail.Address == "user@domain..com"`).
  - Nếu worker áp dụng nguyên văn đề xuất trên, test case `[InlineData("user@domain..com")]` sẽ **THẤT BẠI** vì hàm coi đó là email hợp lệ!
- **Giải pháp bảo vệ chống hồi quy tuyệt đối cho `User.cs`**:
  ```csharp
  // Bổ sung kiểm tra trimmedEmail.Contains("..")
  var trimmedEmail = email.Trim();
  if (trimmedEmail.Contains("..") || 
      !System.Net.Mail.MailAddress.TryCreate(trimmedEmail, out var parsedEmail) || 
      parsedEmail.Address != trimmedEmail.ToLowerInvariant())
  {
      return Result<User>.Failure(new Error("USER_EMAIL_INVALID", "Định dạng địa chỉ email không hợp lệ.", ErrorType.Validation));
  }
  ```
  Đoạn mã này đảm bảo:
  1. Loại bỏ 100% cả 7 định dạng sai (`plainaddress`, `missing_at_symbol.com`, `@missingusername.com`, `username@`, `username@.com`, `user name@domain.com`, `user@domain..com`).
  2. Bảo toàn 100% tính hợp lệ cho `dummy@local`, `admin_test@local`, `admin@ittms.local`, giúp toàn bộ test khởi tạo User và Seed vượt qua trơn tru.

### 3.2. Rủi ro 2: Khắc phục `TicketPriority` enum check ảnh hưởng đến SLA Tests
- **Rà soát**: Trong `UnitTests/Domain/AdversarialSlaStressTests.cs` (Dòng 95–104), có test:
  ```csharp
  [Theory]
  [InlineData((TicketPriority)0)]
  [InlineData((TicketPriority)99)]
  [InlineData((TicketPriority)(-1))]
  public void CalculateDueAt_UndefinedPriorityEnum_FallsBackToDefault24Hours(TicketPriority invalidPriority)
  {
      var createdAt = new DateTime(2026, 10, 25, 8, 0, 0, DateTimeKind.Utc);
      var dueAt = Ticket.CalculateDueAt(invalidPriority, createdAt);
      dueAt.Should().Be(createdAt.AddHours(24));
  }
  ```
- **Đánh giá**: Test này gọi trực tiếp phương thức tĩnh `Ticket.CalculateDueAt(invalidPriority, createdAt)`, không gọi qua `Ticket.Create`.
- **Kết luận**: Nhánh fallback `_ => createdAt.AddHours(24)` trong `CalculateDueAt` vẫn được duy trì nguyên vẹn. Việc thêm kiểm tra `!Enum.IsDefined(typeof(TicketPriority), priority)` tại `Ticket.Create` không gây ảnh hưởng đến phương thức `CalculateDueAt`. Mọi bài test SLA đều bảo toàn 100% kết quả PASS.

### 3.3. Rủi ro 3: Kiểm tra `Attachment.ContentType` rỗng/null
- **Rà soát**: Các bài test khác về Attachment gồm:
  - `AttachmentCreate_WithExactly16MB_Succeeds` (Dòng 110)
  - `AttachmentCreate_With16MBPlusOneByte_FailsValidation` (Dòng 126)
  - `AttachmentCreate_WithZeroOrNegativeSize_FailsValidation` (Dòng 142)
- **Đánh giá**: Tất cả các test trên đều truyền tham số hợp lệ `contentType: "image/png"`.
- **Kết luận**: Việc thêm kiểm tra `string.IsNullOrWhiteSpace(contentType)` chỉ chặn các trường hợp null/empty, hoàn toàn không chạm vào logic kiểm tra kích thước file 16MB. Không có rủi ro hồi quy.

### 3.4. Rủi ro 4: Bắt `FormatException` trong `PasswordHasherService`
- **Rà soát**:
  - `UnitTests/Security/PasswordHasherTests.cs` (5 tests): Kiểm tra băm và so sánh mật khẩu chuẩn.
  - `DomainBoundaryAndSecurityAdversarialTests.cs`:
    - `PasswordHasher_SaltUniqueness` (100 hashes)
    - `PasswordHasher_HandlesUnicodeAndSpecialCharacters` (5 cases)
    - `PasswordHasher_TimingResistance` (30 iterations)
    - `DatabaseMigrationAndSeederTests` & `DataSeeder_RepeatedExecutions_5Times`: Sử dụng `PasswordHasherService` để băm mật khẩu người dùng seed (`Admin@123`, `Staff@123`...).
- **Đánh giá**: Việc bắt `FormatException` và trả về `false` là cơ chế phòng vệ fail-closed tiêu chuẩn. Tất cả các chuỗi băm hợp lệ đều được Identity mã hóa Base-64 chuẩn nên không bao giờ phát sinh `FormatException`.
- **Kết luận**: Không có bất kỳ tác động tiêu cực nào tới các bài kiểm thử hiện có.

---

## 4. BẢNG TỔNG HỢP TRƯỚC VÀ SAU KHẮC PHỤC (TEST SYNC MATRIX)

| Test File | Phương thức kiểm thử | Mã lỗi / Assertion mong đợi | Trạng thái hiện tại | Trạng thái sau đồng bộ |
|---|---|---|:---:|:---:|
| `DomainBoundaryAndSecurityAdversarialTests.cs` | `TicketCreate_WithNegativeOrUndefinedPriority` (4 cases) | `TICKET_PRIORITY_INVALID` | Expects Success |  **Expects Failure** |
| `DomainBoundaryAndSecurityAdversarialTests.cs` | `AttachmentCreate_WithNullContentType` (1-3 cases) | `ATTACH_CONTENT_TYPE_REQUIRED` | Expects Exception |  **Expects Failure** |
| `DomainBoundaryAndSecurityAdversarialTests.cs` | `UserCreate_WithMalformedEmailFormat` (7 cases) | `USER_EMAIL_INVALID` | Expects Success |  **Expects Failure** |
| `DomainBoundaryAndSecurityAdversarialTests.cs` | `PasswordHasher_VerifyPassword_WithNonBase64MalformedHash` (1 case) | Trả về `false` an toàn | Expects Exception |  **Expects False** |
| *8 Files kiểm thử còn lại (96 cases)* | Toàn bộ 96 baseline & adversarial tests | Không đổi |  PASS |  **PASS (100%)** |

---

## 5. QUY TRÌNH THỰC THI KHUYẾN NGHỊ CHO REMEDIATION WORKER

Nhằm bảo đảm luồng tích hợp an toàn tuyệt đối và có thể xác minh tức thì qua từng bước:

```
[Bước 1: Triển khai Mã nguồn]
  ├── 1.1. Sửa PasswordHasherService.cs (bọc try-catch FormatException)
  ├── 1.2. Sửa Attachment.cs (kiểm tra string.IsNullOrWhiteSpace(contentType))
  ├── 1.3. Sửa Ticket.cs (kiểm tra Enum.IsDefined(typeof(TicketPriority), priority))
  └── 1.4. Sửa User.cs (kiểm tra trimmedEmail.Contains("..") || !MailAddress.TryCreate)

[Bước 2: Đồng bộ Test Suite]
  └── Cập nhật 4 phương thức tương ứng trong DomainBoundaryAndSecurityAdversarialTests.cs

[Bước 3: Thực thi Lệnh Kiểm chứng Độc lập]
  └── dotnet test --verbosity normal
      Kỳ vọng: Passed: 109 / Total: 109 (hoặc 111/111 nếu mở rộng contentType)
      Thời gian: < 35 giây
      Build: 0 Warning(s), 0 Error(s)
```

---

## 6. KẾT LUẬN

1. Test suite hiện tại với 109 tests hoạt động rất chính xác và phản ánh trung thực trạng thái của hệ thống.
2. Việc phát hiện ra nghịch lý "Empirical Expose" trong 13 test cases của `challenger_m1_2` giúp ngăn chặn trước một vụ đổ vỡ test hàng loạt khi tiến hành vá mã.
3. Kế hoạch bảo vệ chống hồi quy trên đây đã tính toán cặn kẽ hành vi biên của .NET 9 (đặc biệt là việc xử lý dấu chấm liên tiếp và tên miền nội bộ `@local`), bảo đảm đường đi thông suốt cho Milestone 1 chuyển sang Milestone 2.
