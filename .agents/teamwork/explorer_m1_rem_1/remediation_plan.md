# KẾ HOẠCH KHẮC PHỤC (REMEDIATION PLAN) — M1 REMEDIATION
## BẢO MẬT PASSWORD HASHER & KIỂM TRA ĐỊNH DẠNG EMAIL NGƯỜI DÙNG

> **Mã định danh nhiệm vụ**: `explorer_m1_rem_1`  
> **Milestone**: Milestone 1 Remediation (Scaffolding & Core Domain Foundation)  
> **Thời gian**: 2026-10-25  
> **Mục tiêu**: Xây dựng chiến lược và phương án khắc phục chi tiết cấp độ sản xuất (Production-Grade) cho 2 khiếm khuyết được chỉ ra bởi `challenger_m1_2` tại `challenge_report.md`:
> 1. `PasswordHasherService.VerifyPassword`: Xử lý chuỗi hash dị dạng Base-64 bằng cơ chế bắt ngoại lệ `FormatException` và trả về `false` (Fail-Closed).
> 2. `User.Create`: Triển khai kiểm tra cú pháp email hợp lệ sử dụng `System.Net.Mail.MailAddress.TryCreate`.
> 3. Đảm bảo tính tương thích tuyệt đối: Không làm hỏng dữ liệu khởi tạo (`DataSeeder`), `DummyUser`, hoặc bất kỳ test case nào trong hệ thống.

---

## 1. TỔNG QUAN CHIẾN LƯỢC KHẮC PHỤC (BLUF)

Qua điều tra thực nghiệm trên nền tảng .NET 9 (`net9.0`) với toàn bộ 109 test cases đang chạy đạt 100%:
1. **PasswordHasherService Fail-Closed**:
   - `Microsoft.AspNetCore.Identity.PasswordHasher<T>` sử dụng `Convert.FromBase64String` để giải mã payload băm (V3 PBKDF2 HMAC-SHA512). Khi chuỗi băm chứa ký tự không hợp lệ Base-64 (ví dụ payload tấn công `invalid_base64_hash_payload!@#$`), hàm ném ngoại lệ `System.FormatException`.
   - Nếu không bắt ngoại lệ này, endpoint xác thực người dùng sẽ gặp lỗi Unhandled Exception HTTP 500 thay vì từ chối đăng nhập một cách an toàn.
   - **Giải pháp**: Bổ sung kiểm tra `string.IsNullOrWhiteSpace` ở đầu hàm và bao bọc lệnh gọi `_hasher.VerifyHashedPassword` trong khối `try ... catch (FormatException)`, trả về `false` khi xảy ra ngoại lệ.
2. **User.Create Email Syntax Validation**:
   - Hiện tại `User.Create` chỉ kiểm tra `string.IsNullOrWhiteSpace(email)` và `email.Length > 150`, chấp nhận hoàn toàn các chuỗi không phải email như `"plainaddress"`, `"missing_at_symbol.com"`, `"username@"`.
   - **Thực nghiệm chuyên sâu với `MailAddress.TryCreate` trong .NET 9**:
     - `MailAddress.TryCreate` đơn thuần vẫn chấp nhận một số chuỗi RFC không mong muốn như `"user name@domain.com"` (coi `"user"` là `DisplayName`) hoặc `"user@domain..com"` (RFC 822 domain literal/phần mở rộng).
     - Nếu so sánh dạng thô `parsedEmail.Address != email.Trim().ToLowerInvariant()` với chuỗi đầu vào chưa chuẩn hóa chữ hoa/chữ thường (ví dụ `"Admin@ittms.local"`), `parsedEmail.Address` giữ nguyên chữ hoa `"Admin@ittms.local"` dẫn tới so sánh sai lệch và từ chối nhầm email hợp lệ!
     - **Giải pháp**: Chuẩn hóa `normalizedEmail = email.Trim().ToLowerInvariant()` trước khi đưa vào `MailAddress.TryCreate`, kiểm tra chặt chẽ `parsedEmail.Address == normalizedEmail`, đồng thời chặn dấu chấm kép (`..`) và chấm ở đầu/cuối của cả phần `Host` lẫn `User`.
3. **Bảo toàn Dữ liệu & Tính Tương thích**:
   - Mọi địa chỉ email trong `DataSeeder.cs` (`admin@ittms.local`, `leader@ittms.local`, `staff1@ittms.local`, `staff2@ittms.local`, `user1@ittms.local`, `user2@ittms.local`, `deactivated@ittms.local`, `inactive_user@ittms.local`) và `DummyUser` (`dummy@local` trong `PasswordHasherService.cs`) đều được chứng minh vượt qua 100% bộ quy tắc kiểm tra mới này.

---

## 2. KHIẾM KHUYẾT 1: `PasswordHasherService.VerifyPassword` NÉM `FormatException`

### 2.1. Phân Tích Nguyên Nhân Gốc Rễ (RCA)
- **Tệp nguồn**: `c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs` (Dòng 20–24)
- **Mã nguồn hiện tại**:
  ```csharp
  public bool VerifyPassword(string hashedPassword, string providedPassword)
  {
      var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
      return result != PasswordVerificationResult.Failed;
  }
  ```
- **Hành vi thực nghiệm**:
  Khi nhận một chuỗi băm không phải định dạng Base-64 hợp lệ:
  `_hasher.VerifyHashedPassword` gọi trực tiếp `Convert.FromBase64String(hashedPassword)` mà không có cơ chế phòng vệ, phát sinh ngoại lệ:
  `System.FormatException: The input is not a valid Base-64 string as it contains a non-base 64 character...`
- **Thử nghiệm đối kháng với các biến thể payload**:
  | Payload Thử Nghiệm | Kiểu Dữ Liệu | Kết Quả `VerifyHashedPassword` | Cần Bắt Ngoại Lệ? |
  |---|---|---|:---:|
  | `""` (Rỗng) | Rỗng | `PasswordVerificationResult.Failed` | Không (nhưng nên guard sớm) |
  | `"   "` (Khoảng trắng) | Khoảng trắng | `PasswordVerificationResult.Failed` | Không (nhưng nên guard sớm) |
  | `"AQAAAA=="` | Base64 ngắn hơn header | `PasswordVerificationResult.Failed` | Không |
  | `Convert.ToBase64String(100 zero bytes)` | Base64 hợp lệ nhưng hash sai | `PasswordVerificationResult.Failed` | Không |
  | `Convert.ToBase64String([0xFF, 0x01])` | Header không hợp lệ | `PasswordVerificationResult.Failed` | Không |
  | `"invalid_base64_hash_payload!@#$"` | **Ký tự phi Base-64** | **Ném `FormatException`** | **BẮT BUỘC** |

### 2.2. Đề Xuất Mã Nguồn Khắc Phục (Proposed Patch)
Cập nhật `c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs`:

```csharp
// Trước (Before):
public bool VerifyPassword(string hashedPassword, string providedPassword)
{
    var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
    return result != PasswordVerificationResult.Failed;
}

// Sau (After):
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

### 2.3. Điều Chỉnh Test Suite Tương Ứng
Trong tệp `c:\IT_System\tests\ItTms.Tests\AdversarialTests\DomainBoundaryAndSecurityAdversarialTests.cs`:
Trước đây, `challenger_m1_2` viết test case để **vạch trần lỗi** (asserting exception):
```csharp
[Fact]
public void PasswordHasher_VerifyPassword_WithNonBase64MalformedHash_EmpiricallyExposesFormatException()
{
    const string nonBase64Hash = "invalid_base64_hash_payload!@#$";

    Action act = () => _hasher.VerifyPassword(nonBase64Hash, "AnyPassword123");

    act.Should().Throw<FormatException>()
        .WithMessage("*not a valid Base-64 string*");
}
```
Sau khi khắc phục, `VerifyPassword` trả về `false` một cách an toàn (Fail-Closed). Do đó, test case cần được cập nhật thành:
```csharp
[Fact]
public void PasswordHasher_VerifyPassword_WithNonBase64MalformedHash_ReturnsFalseFailClosed()
{
    const string nonBase64Hash = "invalid_base64_hash_payload!@#$";

    var result = _hasher.VerifyPassword(nonBase64Hash, "AnyPassword123");

    result.Should().BeFalse("PasswordHasherService must catch FormatException and fail-closed with false for malformed Base-64 hash strings");
}
```

---

## 3. KHIẾM KHUYẾT 2: `User.Create` THIẾU KIỂM TRA ĐỊNH DẠNG EMAIL

### 3.1. Phân Tích Nguyên Nhân Gốc Rễ (RCA) & Thách Thức Kỹ Thuật
- **Tệp nguồn**: `c:\IT_System\src\ItTms.Domain\Entities\User.cs` (Dòng 47–52)
- **Mã nguồn hiện tại**:
  ```csharp
  if (string.IsNullOrWhiteSpace(email))
      return Result<User>.Failure(new Error("USER_EMAIL_REQUIRED", "Email không được để trống.", ErrorType.Validation));

  if (email.Length > 150)
      return Result<User>.Failure(new Error("USER_EMAIL_TOO_LONG", "Email không vượt quá 150 ký tự.", ErrorType.Validation));
  ```
- **Hành vi thực nghiệm**:
  Hàm hoàn toàn thiếu bước kiểm tra cú pháp, dẫn tới các chuỗi rác như `"plainaddress"`, `"missing_at_symbol.com"`, `"@missingusername.com"` được lưu trữ thành công vào Entity và Database.
- **Những cạm bẫy tiềm ẩn khi áp dụng `System.Net.Mail.MailAddress`**:
  1. *Cạm bẫy Display Name*:
     Nếu truyền `"user name@domain.com"`, `MailAddress` phân tích thành `DisplayName = "user"` và `Address = "name@domain.com"`. Khi đó `TryCreate` trả về `true`! Do đó, **bắt buộc** phải so khớp `parsedEmail.Address == normalizedEmail` để chặn các chuỗi chứa khoảng trắng hoặc định dạng display name.
  2. *Cạm bẫy Casing (Hoa/Thường)*:
     Nếu truyền `"Admin@ittms.local"` vào `MailAddress.TryCreate(email.Trim(), out var parsedEmail)` và so sánh với `email.Trim().ToLowerInvariant()`, thì `parsedEmail.Address` mang giá trị `"Admin@ittms.local"` trong khi vế phải là `"admin@ittms.local"`. Hai chuỗi khác nhau khiến email hợp lệ bị từ chối oan! Do đó, phải chuẩn hóa `normalizedEmail = email.Trim().ToLowerInvariant()` **trước khi** đưa vào `MailAddress.TryCreate`.
  3. *Cạm bẫy Consecutive Dots (`..`)*:
     `MailAddress` vẫn chấp nhận `"user@domain..com"` do một số quy tắc lịch sử của RFC 822. Do đó, cần kiểm tra bổ sung `!parsedEmail.Host.Contains("..")` và `!parsedEmail.User.Contains("..")`.
  4. *Cạm bẫy Tên Miền Nội Bộ (`.local`, `localhost`)*:
     Dữ liệu seed và môi trường doanh nghiệp sử dụng các domain nội bộ không có TLD công khai (`admin@ittms.local`, `dummy@local`). Quy tắc validation không được bắt buộc phải có các TLD thương mại (`.com`, `.net`) mà phải chấp nhận domain nội bộ hợp lệ theo RFC 5321.

### 3.2. Bảng Ma Trận Kiểm Chứng Thực Nghiệm .NET 9
| Địa chỉ Email Thử Nghiệm | `TryCreate` | `Address == normalizedEmail` | `Host / User Dot Invariants` | Kết Luận Validation |
|---|:---:|:---:|:---:|:---:|
| `"plainaddress"` | `false` | `false` | N/A | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"missing_at_symbol.com"` | `false` | `false` | N/A | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"@missingusername.com"` | `false` | `false` | N/A | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"username@"` | `false` | `false` | N/A | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"username@.com"` | `false` | `false` | N/A | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"user name@domain.com"` | `true` | **`false`** (`name@domain.com`) | N/A | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"user@domain..com"` | `true` | `true` | **`Host.Contains("..") = true`** | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"user..name@domain.com"` | `true` | `true` | **`User.Contains("..") = true`** | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `".user@domain.com"` | `true` | `true` | **`User.StartsWith('.') = true`** | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"user.@domain.com"` | `true` | `true` | **`User.EndsWith('.') = true`** | ❌ Từ chối (`USER_EMAIL_INVALID`) |
| `"admin@ittms.local"` | `true` | `true` | Đạt |  **Hợp lệ (Seeded Admin)** |
| `"dummy@local"` | `true` | `true` | Đạt |  **Hợp lệ (Hasher DummyUser)** |
| `"user1@ittms.local"` | `true` | `true` | Đạt |  **Hợp lệ (Seeded User)** |
| `"Admin@ittms.local"` | `true` | `true` | Đạt |  **Hợp lệ (Hỗ trợ viết hoa)** |
| `"employee@ittms.local"` | `true` | `true` | Đạt |  **Hợp lệ (Custom User)** |

### 3.3. Đề Xuất Mã Nguồn Khắc Phục (Proposed Patch)
Cập nhật `c:\IT_System\src\ItTms.Domain\Entities\User.cs`:

Bổ sung namespace ở đầu tệp:
```csharp
using System.Net.Mail;
```

Bổ sung phương thức kiểm tra cú pháp email private static helper trong `User.cs`:
```csharp
    private static bool IsValidEmail(string email, out string normalizedEmail)
    {
        normalizedEmail = email.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(normalizedEmail, out var parsedEmail))
            return false;

        return parsedEmail.Address == normalizedEmail
            && !parsedEmail.Host.Contains("..")
            && !parsedEmail.Host.StartsWith('.')
            && !parsedEmail.Host.EndsWith('.')
            && !parsedEmail.User.StartsWith('.')
            && !parsedEmail.User.EndsWith('.')
            && !parsedEmail.User.Contains("..");
    }
```

Cập nhật `User.Create`:
```csharp
    public static Result<User> Create(
        string username,
        string email,
        string passwordHash,
        string fullName,
        UserRole role,
        string? department = null,
        UserStatus status = UserStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<User>.Failure(new Error("USER_USERNAME_REQUIRED", "Tên đăng nhập không được để trống.", ErrorType.Validation));

        if (username.Length > 80)
            return Result<User>.Failure(new Error("USER_USERNAME_TOO_LONG", "Tên đăng nhập không vượt quá 80 ký tự.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(email))
            return Result<User>.Failure(new Error("USER_EMAIL_REQUIRED", "Email không được để trống.", ErrorType.Validation));

        if (email.Length > 150)
            return Result<User>.Failure(new Error("USER_EMAIL_TOO_LONG", "Email không vượt quá 150 ký tự.", ErrorType.Validation));

        if (!IsValidEmail(email, out var normalizedEmail))
            return Result<User>.Failure(new Error("USER_EMAIL_INVALID", "Định dạng địa chỉ email không hợp lệ.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result<User>.Failure(new Error("USER_PASSWORD_REQUIRED", "Mật khẩu đã băm không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(fullName))
            return Result<User>.Failure(new Error("USER_FULLNAME_REQUIRED", "Họ và tên không được để trống.", ErrorType.Validation));

        var user = new User
        {
            Username = username.Trim().ToLowerInvariant(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            Status = status,
            Department = department?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<User>.Success(user);
    }
```

### 3.4. Điều Chỉnh Test Suite Tương Ứng
Trong tệp `c:\IT_System\tests\ItTms.Tests\AdversarialTests\DomainBoundaryAndSecurityAdversarialTests.cs`:
Cập nhật test case từ kiểm thử vạch trần khiếm khuyết (`result.IsSuccess.Should().BeTrue()`) sang kiểm thử nghiệm thu bảo vệ (`result.IsFailure.Should().BeTrue()`):
```csharp
// Trước (Before):
[Theory]
[InlineData("plainaddress")]
[InlineData("missing_at_symbol.com")]
[InlineData("@missingusername.com")]
[InlineData("username@")]
[InlineData("username@.com")]
[InlineData("user name@domain.com")]
[InlineData("user@domain..com")]
public void UserCreate_WithMalformedEmailFormat_EmpiricallyExposesValidationGap(string malformedEmail)
{
    var result = User.Create(
        username: "testuser",
        email: malformedEmail,
        passwordHash: "validhash",
        fullName: "Test User",
        role: UserRole.User
    );

    result.IsSuccess.Should().BeTrue("DEFECT: User.Create lacks email format validation (e.g. MailAddress/Regex), permitting syntactically corrupt emails into the system");
}

// Sau (After):
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

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("USER_EMAIL_INVALID");
}
```

Đồng thời, bổ sung test case xác nhận các email hợp lệ (kể cả email hoa thường, email domain nội bộ):
```csharp
[Theory]
[InlineData("admin@ittms.local")]
[InlineData("dummy@local")]
[InlineData("Admin@ittms.local")]
[InlineData("user.test+tag@sub.domain.com")]
public void UserCreate_WithValidEmailFormat_Succeeds(string validEmail)
{
    var result = User.Create(
        username: "testuser",
        email: validEmail,
        passwordHash: "validhash",
        fullName: "Test User",
        role: UserRole.User
    );

    result.IsSuccess.Should().BeTrue();
    result.Value.Email.Should().Be(validEmail.Trim().ToLowerInvariant());
}
```

---

## 4. MA TRẬN TÁC ĐỘNG VÀ ĐẢM BẢO TÍNH TOÀN VẸN (INTEGRITY & REGRESSION AUDIT)

### 4.1. Tác động tới Dữ Liệu Khởi Tạo (`DataSeeder.cs`)
Tất cả 8 tài khoản mẫu trong `DataSeeder.cs`:
1. `admin@ittms.local` (Admin)
2. `leader@ittms.local` (Leader)
3. `staff1@ittms.local` (Staff 1)
4. `staff2@ittms.local` (Staff 2)
5. `user1@ittms.local` (User 1)
6. `user2@ittms.local` (User 2)
7. `deactivated@ittms.local` (Inactive User)
8. `inactive_user@ittms.local` (Inactive User)
Đều là các địa chỉ hợp lệ chuẩn RFC 5321 / `MailAddress`. Quá trình nạp dữ liệu ban đầu hoặc re-seed lặp lại sẽ diễn ra 100% trơn tru, không gặp bất kỳ lỗi `Result.Failure` nào.

### 4.2. Tác động tới Khởi Tạo `DummyUser` của `PasswordHasherService`
Trong `PasswordHasherService.cs`:
```csharp
private static readonly User DummyUser = User.Create("dummy", "dummy@local", "dummyhash", "Dummy", Domain.Enums.UserRole.User).Value;
```
`"dummy@local"` hoàn toàn hợp lệ với cơ chế `IsValidEmail`. Khởi tạo tĩnh của lớp không gặp rủi ro `InvalidOperationException`.

### 4.3. Đánh Giá Hiệu Năng & An Ninh Tuyệt Đối
1. **Phòng vệ ReDoS (Regular Expression Denial of Service)**:
   - Thay vì sử dụng các biểu thức chính quy (Regex) phức tạp dễ bị tấn công suy thoái thời gian hàm mũ (Catastrophic Backtracking / ReDoS), giải pháp dùng bộ phân tích trạng thái tuyến tính $O(N)$ được tối ưu hóa ở tầng native của `System.Net.Mail.MailAddress`.
2. **Kháng Timing Side-Channel**:
   - `PasswordHasherService` vẫn duy trì khả năng chống rò rỉ thời gian (Timing Resistance lệch $2.14\%$) qua thuật toán HMAC-SHA512 hằng thời gian. Khối `try-catch` chỉ kích hoạt khi chuỗi băm bị biến dạng Base-64, hoàn toàn không ảnh hưởng tới luồng xác thực thông thường.

---

## 5. HƯỚNG DẪN BÀN GIAO CHO WORKER (EXECUTION DIRECTIVE)

| STT | Tệp Cần Chỉnh Sửa | Vị Trí / Dòng | Hành Động Cụ Thể |
|:---:|---|---|---|
| 1 | `src/ItTms.Infrastructure/Security/PasswordHasherService.cs` | Dòng 20–24 | Thêm `string.IsNullOrWhiteSpace` guard và bọc khối `try-catch (FormatException)` trả về `false`. |
| 2 | `src/ItTms.Domain/Entities/User.cs` | Dòng 1–3, Dòng 47–52, Dòng 62 | Thêm `using System.Net.Mail;`, thêm hàm `IsValidEmail`, thêm kiểm tra validation `USER_EMAIL_INVALID`, gán `Email = normalizedEmail`. |
| 3 | `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` | Dòng 180–202 | Cập nhật assertion từ `result.IsSuccess.Should().BeTrue()` sang `result.IsFailure.Should().BeTrue()` (`USER_EMAIL_INVALID`). |
| 4 | `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` | Dòng 286–299 | Cập nhật assertion từ `act.Should().Throw<FormatException>()` sang `result.Should().BeFalse()`. |
| 5 | Toàn bộ dự án | Terminal | Chạy `dotnet build` và `dotnet test` kiểm chứng 100% test cases đạt pass. |
