# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — EXPLORER_M1_REM_3
## ĐỒNG BỘ TEST SUITE VÀ BẢO VỆ CHỐNG HỒI QUY (109+ TESTS)

> **Agent**: `explorer_m1_rem_3` (Explorer / Investigator / Synthesizer)  
> **Thư mục làm việc**: `c:\IT_System\.agents\teamwork\explorer_m1_rem_3`  
> **Người nhận**: Orchestrator / Remediation Worker (`9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Tài liệu bàn giao**: `c:\IT_System\.agents\teamwork\explorer_m1_rem_3\regression_guard_plan.md`  
> **Ngôn ngữ**: Tiếng Việt  
> **Loại bàn giao**: **Hard Handoff** (Nhiệm vụ hoàn tất đầy đủ 5 cấu phần)

---

### 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM TRỰC TIẾP)

1. **Kết quả thực thi hiện tại của toàn bộ bộ kiểm thử**:
   Lệnh thực thi trực tiếp trên hệ thống:
   `dotnet test --verbosity normal` tại thư mục gốc `c:\IT_System`.
   Kết quả trả về:
   ```
   Test Run Successful.
   Total tests: 109
        Passed: 109
    Total time: 29.8489 Seconds
   Build succeeded.
       0 Warning(s)
       0 Error(s)
   ```
   Toàn bộ 109 bài kiểm thử thuộc 9 files nguồn (`UnitTests`, `IntegrationTests`, `AdversarialTests`) hiện đang vượt qua $100\%$.

2. **Cấu trúc và hành vi kiểm thử trong `DomainBoundaryAndSecurityAdversarialTests.cs`**:
   - File: `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` (Tổng cộng 437 dòng, 44 test cases).
   - Dòng 41–65 (`TicketCreate_WithNegativeOrUndefinedPriority_EmpiricallyExposesInvariantGap`):
     Thực thi với `InlineData(-1, 0, 999, int.MinValue)` (4 tests) với assertion:
     `result.IsSuccess.Should().BeTrue("DEFECT: Ticket.Create currently lacks Enum.IsDefined check...");`
   - Dòng 160–175 (`AttachmentCreate_WithNullContentType_EmpiricallyExposesNullReferenceException`):
     Thực thi với `contentType: null!` (1 test) với assertion:
     `act.Should().Throw<NullReferenceException>();`
   - Dòng 180–203 (`UserCreate_WithMalformedEmailFormat_EmpiricallyExposesValidationGap`):
     Thực thi với 7 mẫu email sai cú pháp (7 tests) với assertion:
     `result.IsSuccess.Should().BeTrue("DEFECT: User.Create lacks email format validation...");`
   - Dòng 286–298 (`PasswordHasher_VerifyPassword_WithNonBase64MalformedHash_EmpiricallyExposesFormatException`):
     Thực thi với `"invalid_base64_hash_payload!@#$"` (1 test) với assertion:
     `act.Should().Throw<FormatException>().WithMessage("*not a valid Base-64 string*");`

3. **Hiện tượng thực nghiệm về định dạng Email trên .NET 9**:
   - Thực nghiệm qua trình thông dịch .NET 9 F# Interactive:
     - `MailAddress.TryCreate("dummy@local")`: Trả về `Success = true`, `Address = "dummy@local"`.
     - `MailAddress.TryCreate("user name@domain.com")`: Trả về `Success = true`, nhưng `Address = "name@domain.com"` (khác chuỗi ban đầu do tách DisplayName).
     - `MailAddress.TryCreate("user@domain..com")`: **Trả về `Success = true`**, `Address = "user@domain..com"`.
   - Vị trí phụ thuộc trong mã nguồn:
     `src/ItTms.Infrastructure/Security/PasswordHasherService.cs` (Dòng 13):
     `private static readonly User DummyUser = User.Create("dummy", "dummy@local", "dummyhash", "Dummy", Domain.Enums.UserRole.User).Value;`
     Nếu `User.Create` từ chối `@local`, việc truy cập `.Value` của kết quả thất bại sẽ ném `InvalidOperationException` ngay khi khởi tạo kiểu dữ liệu `PasswordHasherService`.

---

### 2. LOGIC CHAIN (CHUỖI LÝ LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. **Từ Quan sát 2 $\to$ Nguy cơ sụp đổ 13 test cases khi vá lỗi**:
   - Đặc phái viên `challenger_m1_2` đã viết 13 bài test asserting rằng lỗi hiện đang xảy ra (`IsSuccess == true` hoặc ném ngoại lệ).
   - Khi kỹ sư áp dụng các bản vá trong `src/` (trả về `Result.Failure` hoặc bắt ngoại lệ và trả về `false`), 13 bài test này sẽ **lập tức chuyển sang FAILED** vì kết quả thực tế không còn khớp với kỳ vọng cũ của bài test.
   - Do đó, nếu chỉ sửa mã nguồn mà không đồng bộ test suite, tỷ lệ đạt của solution sẽ giảm từ $109/109$ ($100\%$) xuống $96/109$ ($88\%$).

2. **Từ Quan sát 3 $\to$ Yêu cầu bắt buộc về thuật toán xác thực email trong `User.cs`**:
   - Nếu áp dụng đề xuất sơ sài của `challenger_m1_2` (`!MailAddress.TryCreate(...) || parsedEmail.Address != email`):
     Chuỗi `"user@domain..com"` sẽ vượt qua kiểm tra vì `MailAddress` của .NET 9 cho phép dấu chấm liên tiếp trong domain. Điều này sẽ khiến test case `[InlineData("user@domain..com")]` bị FAILED khi đồng bộ assertion sang `result.IsFailure.Should().BeTrue()`.
   - Ngược lại, nếu áp dụng quy tắc quá chặt (ví dụ bắt buộc domain phải có dấu chấm hoặc có TLD $\ge 2$ ký tự), chuỗi `"dummy@local"` tại `PasswordHasherService.cs:13` sẽ bị từ chối, làm sập dịch vụ băm mật khẩu và kéo theo sự sụp đổ của toàn bộ test suite.
   - Suy ra: Thuật toán bắt buộc phải kiểm tra `trimmedEmail.Contains("..")` kết hợp với `MailAddress.TryCreate` và so khớp `parsedEmail.Address == trimmedEmail.ToLowerInvariant()`. Thuật toán này vừa chặn được dấu chấm kép, vừa cho phép tên miền nội bộ `@local`.

3. **Từ Quan sát 1 và Rà soát 96 test cases còn lại $\to$ Tính tương thích và an toàn tuyệt đối**:
   - `Ticket.CalculateDueAt`: Giữ nguyên nhánh `_ => createdAt.AddHours(24)`. Bài test `AdversarialSlaStressTests.CalculateDueAt_UndefinedPriorityEnum_FallsBackToDefault24Hours` gọi trực tiếp hàm này nên tiếp tục PASS.
   - `Attachment.Create`: 3 test cases về kích thước 16MB đều truyền `contentType: "image/png"` hợp lệ nên không bị ảnh hưởng.
   - `PasswordHasherService`: Việc bắt `FormatException` trả về `false` là cơ chế phòng thủ chuẩn mực, không can thiệp vào các chuỗi băm hợp lệ của 8 người dùng mẫu và cơ chế tính salt ngẫu nhiên.

---

### 3. CAVEATS (GIỚI HẠN VÀ KHUYẾN CÁO)

1. **Giới hạn quyền hạn**: Explorer tuân thủ nghiêm ngặt nguyên tắc chỉ đọc (`Read-Only Investigation`), không trực tiếp sửa đổi các file trong `src/` và `tests/`. Kỹ sư sửa mã (`worker`) có trách nhiệm áp dụng các đoạn mã sửa đổi chính xác như mô tả trong tài liệu `regression_guard_plan.md`.
2. **Quy ước tên test case**: Khi chuyển đổi từ kịch bản "vạch trần khiếm khuyết" sang "nghiệm thu bản vá", khuyến nghị đổi tên phương thức (ví dụ: `_EmpiricallyExposesInvariantGap` đổi thành `_FailsValidation`) để phản ánh đúng ý nghĩa nghiệp vụ lâu dài của test suite.

---

### 4. CONCLUSION (KẾT LUẬN VÀ CHỈ DẪN HÀNH ĐỘNG)

1. **Số lượng test cases**: Toàn bộ **109 test cases** sẽ tiếp tục duy trì trạng thái **100% PASSED** sau khi áp dụng 4 bản vá mã nguồn và đồng bộ 13 assertions tương ứng. (Nếu worker mở rộng `AttachmentCreate` cho cả chuỗi rỗng và khoảng trắng, tổng số test sẽ là **111 test cases**).
2. **Quy trình thực hiện tuần tự không hồi quy**:
   - **Bước 1**: Áp dụng bản vá mã nguồn trong `src/`:
     - `PasswordHasherService.cs`: Bọc `try-catch (FormatException) { return false; }`.
     - `Attachment.cs`: Thêm kiểm tra `string.IsNullOrWhiteSpace(contentType)` $\to$ `ATTACH_CONTENT_TYPE_REQUIRED`.
     - `Ticket.cs`: Thêm kiểm tra `!Enum.IsDefined(typeof(TicketPriority), priority)` $\to$ `TICKET_PRIORITY_INVALID`.
     - `User.cs`: Thêm kiểm tra `trimmedEmail.Contains("..") || !MailAddress.TryCreate(...)` $\to$ `USER_EMAIL_INVALID`.
   - **Bước 2**: Đồng bộ hóa 4 phương thức tương ứng trong `DomainBoundaryAndSecurityAdversarialTests.cs`.
   - **Bước 3**: Chạy kiểm chứng toàn bộ solution.

---

### 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Để kiểm chứng tính đúng đắn độc lập của kết luận này, thực hiện các bước sau:

1. **Lệnh thực thi**:
   ```powershell
   dotnet test --verbosity normal
   ```
2. **Điều kiện thành công (Pass Criteria)**:
   - Solution biên dịch thành công: `0 Warning(s), 0 Error(s)`.
   - Kết quả kiểm thử: `Passed: 109` (hoặc `111`), `Failed: 0`, `Skipped: 0`.
   - Thời gian thực thi: Dưới 35 giây.
3. **Điều kiện phát hiện vi phạm (Invalidation Conditions)**:
   - Nếu xuất hiện lỗi `InvalidOperationException` tại `PasswordHasherService`: Kiểm tra xem xác thực email có vô tình loại trừ `@local` hay không.
   - Nếu test `user@domain..com` bị fail: Kiểm tra xem đã thêm `trimmedEmail.Contains("..")` hay chưa.
