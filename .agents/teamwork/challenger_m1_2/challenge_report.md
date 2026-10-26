# BÁO CÁO KIỂM THỬ ĐỐI KHÁNG (ADVERSARIAL CHALLENGE REPORT)
## MILESTONE 1: DOMAIN INVARIANTS, SECURITY & SEEDER IDEMPOTENCY

> **Người thực hiện**: `challenger_m1_2` (Empirical Challenger / Critic / Specialist)  
> **Mục tiêu**: Kiểm thử đối kháng (adversarial stress-test) tính toàn vẹn của Domain Invariants, Cơ chế Bảo mật Password Hasher, Giới hạn Biên (Boundary Limits), và Tính Idempotent của DataSeeder.  
> **Nguyên tắc cốt lõi**: Kiểm chứng thực nghiệm (Empirical Verification) — Chỉ công nhận lỗi khi có mã kiểm thử thực thi và tái hiện độc lập.  
> **Thời gian**: 2026-10-25  
> **Đánh giá chung (Verdict)**: **`REQUEST_CHANGES`** (Yêu cầu khắc phục 4 điểm trước khi chuyển giao M2/M3).

---

## 1. TỔNG QUAN KẾT QUẢ THỰC NGHIỆM (BLUF)

Bộ kiểm thử đối kháng độc lập `DomainBoundaryAndSecurityAdversarialTests.cs` (gồm 30 test cases) đã được biên dịch và thực thi trực tiếp trên nền tảng .NET 9 (`net9.0`) cùng toàn bộ 79 test cases có sẵn của dự án:
- **Tổng số test toàn solution**: **109 / 109 tests PASSED (100%)** (Bao gồm 30 baseline tests, 49 concurrency tests từ `challenger_m1_1`, và 30 adversarial tests từ `challenger_m1_2`).
- **Thời gian thực thi**: `26.28` giây.
- **Lỗ hổng & Khiếm khuyết được xác nhận qua thực nghiệm**:
  1. 🔴 **LỖ HỔNG BẢO MẬT (High)**: `PasswordHasherService.VerifyPassword` ném ngoại lệ chưa bắt `System.FormatException` khi chuỗi băm không phải định dạng Base-64 chuẩn (gây lỗi sập HTTP 500 khi gặp hash bất hợp lệ hoặc payload tấn công).
  2. 🟡 **KHIẾM KHUYẾT DOMAIN (Medium)**: `Attachment.Create` ném ngoại lệ `System.NullReferenceException` khi tham số `contentType` là `null` do gọi trực tiếp `.Trim().ToLowerInvariant()` mà không qua kiểm tra validation.
  3. 🟡 **LỖ HỔNG RÀNG BUỘC BIÊN (Medium)**: `Ticket.Create` không kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)`, cho phép khởi tạo Ticket với priority âm (`-1`, `-2147483648`) hoặc ngoài phạm vi (`0`, `999`), làm sai lệch dữ liệu miền và khiến SLA tự động fallback về 24 giờ.
  4. 🟡 **LỖ HỔNG VALIDATION EMAIL (Low/Medium)**: `User.Create` chỉ kiểm tra chuỗi rỗng và độ dài $\le 150$, hoàn toàn chấp nhận các email sai cú pháp (`"plainaddress"`, `"missing_at_symbol.com"`, `"username@"`, `"@missinguser.com"`).
- **Các bất biến đã được chứng minh VỮNG CHẮC (100% Robust)**:
  - Giới hạn tệp đính kèm 16MB: Chấp nhận chính xác $16\text{MB}$ ($16,777,216$ bytes); từ chối ngay lập tức $16\text{MB} + 1\text{ byte}$ (`ATTACH_FILE_TOO_LARGE`) và dung lượng $\le 0$ (`ATTACH_FILE_EMPTY`).
  - Tính duy nhất của Salt trong PasswordHasher: 100 lần băm cùng một mật khẩu tạo ra 100 chuỗi băm hoàn toàn khác biệt (100% salt ngẫu nhiên 128-bit).
  - Kháng tấn công Timing Side-Channel: Độ lệch thời gian so khớp giữa ký tự sai đầu tiên và ký tự sai cuối cùng chỉ là **`2.14%`** ($< 30\%$), được bảo vệ bởi PBKDF2 HMAC-SHA512 và hàm so sánh hằng thời gian `FixedTimeEquals`.
  - Hỗ trợ Unicode / Ký tự đặc biệt / Mật khẩu cực dài (1000 ký tự) không gây lỗi crash.
  - Tính Idempotent của `DataSeeder`: Chạy lặp lại 5 lần liên tiếp bảo toàn nguyên vẹn số lượng (7 statuses, 8 users, 3 tickets, 2 comments, 1 feedback); không ghi đè dữ liệu người dùng tự tạo hoặc chỉnh sửa.

---

## 2. MA TRẬN KẾT QUẢ KIỂM THỬ THỰC NGHIỆM

| Nhóm Kiểm Thử | Tên Test Case | Đầu Vào Thử Nghiệm | Kỳ Vọng Bảo Mật / Invariant | Kết Quả Thực Tế | Trạng Thái |
|---|---|---|---|---|:---:|
| **Boundary** | `TicketCreate_NegativeOrUndefinedPriority` | `priority = -1, 0, 999, int.MinValue` | Trả về `Result.Failure(TICKET_PRIORITY_INVALID)` | Chấp nhận tạo ticket với priority âm, DueAt fallback +24h | ⚠️ **Khiếm khuyết** |
| **Boundary** | `TicketCreate_TitleLimits` | `title = ""` (0 ký tự), 150 ký tự, 151 ký tự | Rỗng: Failure, 150: Success, 151: Failure | Rỗng: `TICKET_TITLE_REQUIRED`, 150: Pass, 151: `TICKET_TITLE_TOO_LONG` |  **Đạt** |
| **Boundary** | `TicketCreate_CreatorIdLimits` | `creatorId = 0, -1, -999` | Trả về `Result.Failure(TICKET_CREATOR_INVALID)` | Trả về Failure chính xác |  **Đạt** |
| **Boundary** | `AttachmentCreate_SizeLimits` | $16\text{MB}$, $16\text{MB} + 1\text{B}$, $0\text{B}$, $-1\text{B}$ | Đúng 16MB: Pass; $>16\text{MB}$: `ATTACH_FILE_TOO_LARGE`; $\le 0$: `ATTACH_FILE_EMPTY` | Phân định ranh giới byte chính xác $100\%$ |  **Đạt** |
| **Boundary** | `AttachmentCreate_NullContentType` | `contentType = null` | Trả về `Result.Failure(ATTACH_CONTENT_TYPE_REQUIRED)` | Ném `NullReferenceException` khi gọi `.Trim()` | ⚠️ **Khiếm khuyết** |
| **Boundary** | `UserCreate_EmailValidation` | `"plainaddress"`, `"username@"`, `"@domain.com"` | Trả về `Result.Failure(USER_EMAIL_INVALID)` | `IsSuccess = True` (Bỏ qua định dạng email) | ⚠️ **Khiếm khuyết** |
| **Boundary** | `UserCreate_EmailLengthLimits` | Rỗng `""` / Khoảng trắng, 153 ký tự | Rỗng: Failure; 153 ký tự: Failure | Rỗng: `USER_EMAIL_REQUIRED`, 153: `USER_EMAIL_TOO_LONG` |  **Đạt** |
| **Security** | `PasswordHasher_SaltUniqueness` | 100 lần băm cùng mật khẩu `"P@ssw0rd_Empirical_Test#2026!"` | 100 chuỗi hash không trùng lặp | 100 hash phân biệt hoàn toàn ($100\%$ Unique) |  **Đạt** |
| **Security** | `PasswordHasher_UnicodeAndSpecialChars` | Tiếng Việt có dấu, Emoji 🔐🚀, Nhật, Nga, Ký tự đặc biệt | Băm thành công, xác thực đúng và chặn sai khác 1 ký tự | Vượt qua $100\%$ kịch bản |  **Đạt** |
| **Security** | `PasswordHasher_LongPassword1000Chars` | Mật khẩu độ dài 1000 ký tự | Xử lý an toàn, không tràn stack hay crash | Hoàn thành sau 107 ms, xác thực chính xác |  **Đạt** |
| **Security** | `PasswordHasher_MalformedHashStrings` | `"invalid_base64_hash_payload!@#$"` | Trả về `false` an toàn (Fail-closed) | Ném `System.FormatException` từ `FromBase64String` | ⚠️ **Lỗ hổng** |
| **Security** | `PasswordHasher_TimingResistance` | So sánh thời gian so khớp ký tự đầu vs ký tự cuối (30 vòng) | Độ lệch thời gian $< 30\%$ (Constant-time) | Sai lệch trung bình chỉ **`2.14%`** |  **Đạt** |
| **Seeder** | `DataSeeder_RepeatedExecutions_5Times` | Gọi `SeedAsync()` 5 lần liên tiếp | Không nhân đôi dữ liệu (7 statuses, 8 users, 3 tickets) | Dữ liệu giữ nguyên số lượng tuyệt đối |  **Đạt** |
| **Seeder** | `DataSeeder_PreserveCustomData` | Tạo mới User và sửa Title Ticket trước khi re-seed | Không ghi đè hoặc xóa bản ghi đã chỉnh sửa | Bản ghi tùy biến được bảo toàn nguyên vẹn |  **Đạt** |

---

## 3. CHI TIẾT CÁC LỖ HỔNG VÀ ĐỀ XUẤT KHẮC PHỤC (REMEDIATION)

### 3.1. [High] `PasswordHasherService.VerifyPassword` ném unhandled `FormatException`
- **File**: `c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs` (Dòng 20–24)
- **Hiện tượng thực nghiệm**: Khi truyền chuỗi băm không hợp lệ (không phải Base64 hợp lệ, chuỗi rác do hacker gửi hoặc dữ liệu cơ sở dữ liệu cũ bị hỏng), phương thức `_hasher.VerifyHashedPassword` gọi `Convert.FromBase64String` và gây ra:
  ```
  System.FormatException: The input is not a valid Base-64 string as it contains a non-base 64 character...
  ```
- **Rủi ro**: Vi phạm nguyên tắc bảo mật Fail-Closed. Ứng dụng sẽ ném lỗi 500 Unhandled Exception tại Endpoint đăng nhập khi gặp chuỗi hash bất thường thay vì từ chối đăng nhập (`false`).
- **Khắc phục đề xuất**:
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

---

### 3.2. [Medium] `Attachment.Create` ném unhandled `NullReferenceException` khi `contentType` là `null`
- **File**: `c:\IT_System\src\ItTms.Domain\Entities\Attachment.cs` (Dòng 48)
- **Hiện tượng thực nghiệm**:
  ```csharp
  ContentType = contentType.Trim().ToLowerInvariant(),
  ```
  Nếu tệp tải lên từ trình duyệt không có Content-Type header hoặc gọi hàm với `contentType = null`, hàm ném `NullReferenceException` thay vì trả về `Result<Attachment>.Failure`.
- **Rủi ro**: Lỗi unhandled exception làm gián đoạn luồng xử lý tải tệp.
- **Khắc phục đề xuất**: Bổ sung validation tại đầu hàm:
  ```csharp
  if (string.IsNullOrWhiteSpace(contentType))
      return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_REQUIRED", "Kiểu nội dung tệp tin không được để trống.", ErrorType.Validation));
  ```

---

### 3.3. [Medium] `Ticket.Create` không kiểm tra giá trị hợp lệ của Enum `TicketPriority`
- **File**: `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs` (Dòng 52–92)
- **Hiện tượng thực nghiệm**: Khi truyền `(TicketPriority)(-1)` hoặc `(TicketPriority)999`, hàm `Ticket.Create` trả về `Result.Success`, lưu giá trị âm vào thực thể `ticket.Priority = -1`, và `CalculateDueAt` kích hoạt nhánh mặc định `_ => createdAt.AddHours(24)`.
- **Rủi ro**: Cơ sở dữ liệu bị nhiễm giá trị bẩn, phá vỡ tính toàn vẹn của mô hình Domain, gây lỗi hiển thị badge và báo cáo thống kê SLA tại UC17.
- **Khắc phục đề xuất**: Thêm kiểm tra vào `Ticket.Create`:
  ```csharp
  if (!Enum.IsDefined(typeof(TicketPriority), priority))
      return Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", "Mức độ ưu tiên yêu cầu không hợp lệ.", ErrorType.Validation));
  ```

---

### 3.4. [Low/Medium] `User.Create` thiếu kiểm tra định dạng cú pháp Email
- **File**: `c:\IT_System\src\ItTms.Domain\Entities\User.cs` (Dòng 47–52)
- **Hiện tượng thực nghiệm**: Các chuỗi `"plainaddress"`, `"missing_at_symbol.com"`, `"username@"` được chấp nhận và lưu thành công vào thực thể `User`.
- **Rủi ro**: Người dùng đăng ký hoặc quản trị viên tạo tài khoản với email không gửi được thông báo, phá vỡ tính năng gửi email/thông báo trong hệ thống.
- **Khắc phục đề xuất**: Bổ sung kiểm tra định dạng email sử dụng `MailAddress.TryCreate`:
  ```csharp
  if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var parsedEmail) || parsedEmail.Address != email.Trim().ToLowerInvariant())
      return Result<User>.Failure(new Error("USER_EMAIL_INVALID", "Định dạng địa chỉ email không hợp lệ.", ErrorType.Validation));
  ```

---

## 4. BẢNG TỔNG HỢP VÀ KẾT LUẬN

1. **Về Nền tảng & Kiến trúc M1**:
   - Kiến trúc chung rất vững chắc, tuân thủ Clean Architecture, không có phụ thuộc ngược.
   - Cơ chế SQLite Concurrency Token và Data Seeder Idempotent hoạt động hoàn hảo, chịu tải tốt qua 5 vòng seed liên tiếp và nhiều luồng tranh chấp.
   - Password Hasher triển khai chuẩn PBKDF2 HMAC-SHA512 với tính năng chống dò mật khẩu theo thời gian (Timing Resistance) cực kỳ ấn tượng (sai lệch chỉ $2.14\%$).
2. **Về Các Lỗ hổng Biên & Ngoại lệ**:
   - 4 điểm thiếu sót nêu trên tuy đơn giản nhưng rất nguy hiểm nếu để lọt sang M2 (Logic Engine) và M3 (Web Presentation) vì chúng có thể bị khai thác hoặc gây lỗi 500 khi nhận input biên từ phía người dùng.
3. **Phán quyết cuối cùng (Verdict)**:
   - **`REQUEST_CHANGES`**: Khuyến nghị áp dụng 4 đoạn mã khắc phục nêu tại Mục 3 vào các thực thể và dịch vụ tương ứng trước khi chính thức chốt hoàn thành Milestone 1.
