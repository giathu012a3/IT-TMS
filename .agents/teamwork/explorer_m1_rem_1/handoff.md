# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — EXPLORER_M1_REM_1

> **Người thực hiện**: `explorer_m1_rem_1` (Remediation Explorer / Specialist)  
> **Người nhận**: `orchestrator_1` / `worker_m1` (Caller ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Chủ đề**: Phân tích giải pháp khắc phục khiếm khuyết bảo mật `PasswordHasherService` FormatException (Fail-Closed) & Kiểm tra định dạng Email tại `User.Create`.  
> **Kế hoạch chi tiết liên quan**: `c:\IT_System\.agents\teamwork\explorer_m1_rem_1\remediation_plan.md`  

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM TRỰC TIẾP)

1. **Khiếm khuyết PasswordHasherService.VerifyPassword ném FormatException**:
   - Tệp: `c:\IT_System\src\ItTms.Infrastructure\Security\PasswordHasherService.cs`, dòng 20–24:
     ```csharp
     public bool VerifyPassword(string hashedPassword, string providedPassword)
     {
         var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
         return result != PasswordVerificationResult.Failed;
     }
     ```
   - Khi truyền chuỗi băm phi Base-64 (`"invalid_base64_hash_payload!@#$"`), phương thức ném ngoại lệ:
     `System.FormatException: The input is not a valid Base-64 string as it contains a non-base 64 character, more than two padding characters, or an illegal character among the padding characters.`
   - Thử nghiệm trên .NET 9 chứng minh: Tất cả các dạng payload hỏng khác (rỗng `""`, khoảng trắng `" "`, độ dài ngắn hơn header `"AQAAAA=="`, header không hợp lệ `0xFF`) đều được `PasswordHasher<T>` tự động trả về `PasswordVerificationResult.Failed`. Duy nhất lỗi chuỗi ký tự phi Base-64 ném `FormatException`.

2. **Khiếm khuyết User.Create thiếu kiểm tra cú pháp Email**:
   - Tệp: `c:\IT_System\src\ItTms.Domain\Entities\User.cs`, dòng 47–52:
     ```csharp
     if (string.IsNullOrWhiteSpace(email))
         return Result<User>.Failure(new Error("USER_EMAIL_REQUIRED", "Email không được để trống.", ErrorType.Validation));

     if (email.Length > 150)
         return Result<User>.Failure(new Error("USER_EMAIL_TOO_LONG", "Email không vượt quá 150 ký tự.", ErrorType.Validation));
     ```
   - Không có bất kỳ kiểm tra định dạng email nào, chấp nhận các email sai cú pháp: `"plainaddress"`, `"missing_at_symbol.com"`, `"@missingusername.com"`, `"username@"`, `"username@.com"`, `"user name@domain.com"`, `"user@domain..com"`.
   - Thực nghiệm chạy `dotnet fsi` trên môi trường .NET 9:
     - `MailAddress.TryCreate` chấp nhận `"user name@domain.com"` bằng cách tách `DisplayName = "user"` và `Address = "name@domain.com"`.
     - `MailAddress.TryCreate` chấp nhận `"user@domain..com"` với `Host = "domain..com"` do cơ chế RFC cũ.
     - Nếu so sánh `parsedEmail.Address != email.Trim().ToLowerInvariant()` mà không chuẩn hóa trước chuỗi đưa vào `TryCreate`, chuỗi có chữ hoa như `"Admin@ittms.local"` sẽ bị từ chối oan do `parsedEmail.Address` giữ nguyên chữ hoa.

3. **Hiện trạng Test Suites**:
   - Lệnh thực thi: `dotnet test`
   - Kết quả: `Passed! - Failed: 0, Passed: 109, Skipped: 0, Total: 109, Duration: 26 s`
   - Trong `DomainBoundaryAndSecurityAdversarialTests.cs`, 2 test case đang được viết dưới dạng khẳng định sự tồn tại của lỗi:
     - Dòng 201: `result.IsSuccess.Should().BeTrue("DEFECT: User.Create lacks email format validation...");`
     - Dòng 294: `act.Should().Throw<FormatException>().WithMessage("*not a valid Base-64 string*");`
   - Do đó, khi áp dụng bản sửa lỗi vào `PasswordHasherService` và `User.cs`, 2 test case trên bắt buộc phải cập nhật assertion tương ứng, nếu không `dotnet test` sẽ báo fail vì lỗi đã được khắc phục.

4. **Dữ liệu Seed và Khởi tạo tĩnh**:
   - `DataSeeder.cs` (Dòng 70–95) chứa 8 địa chỉ email: `admin@ittms.local`, `leader@ittms.local`, `staff1@ittms.local`, `staff2@ittms.local`, `user1@ittms.local`, `user2@ittms.local`, `deactivated@ittms.local`, `inactive_user@ittms.local`.
   - `PasswordHasherService.cs` (Dòng 13) khởi tạo `DummyUser`: `dummy@local`.

---

## 2. LOGIC CHAIN (CHUỖI LẬP LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. Từ **Observation 1**:
   Vì `PasswordHasher<T>.VerifyHashedPassword` ném `FormatException` khi đầu vào không phải Base-64, một payload tấn công hoặc hash hỏng trên HTTP request gửi đến endpoint `/Auth/Login` sẽ gây sập luồng (HTTP 500 Unhandled Exception). Bổ sung khối `try ... catch (FormatException)` và guard `string.IsNullOrWhiteSpace` sẽ đảm bảo nguyên tắc Fail-Closed (từ chối xác thực, trả về `false`).

2. Từ **Observation 2**:
   Để chặn 100% các mẫu email dị dạng trong `DomainBoundaryAndSecurityAdversarialTests.cs` mà không phụ thuộc Regex nặng nề (tránh ReDoS):
   - Chuẩn hóa chuỗi bằng `email.Trim().ToLowerInvariant()` trước khi đưa vào `MailAddress.TryCreate`.
   - Kiểm tra đẳng thức `parsedEmail.Address == normalizedEmail` (loại bỏ hoàn toàn trường hợp display name có khoảng trắng `"user name@domain.com"`).
   - Kiểm tra các bất biến chấm biên: `!parsedEmail.Host.Contains("..")`, `!parsedEmail.Host.StartsWith('.')`, `!parsedEmail.Host.EndsWith('.')`, `!parsedEmail.User.StartsWith('.')`, `!parsedEmail.User.EndsWith('.')`, `!parsedEmail.User.Contains("..")` (loại bỏ `"user@domain..com"` và các chuỗi chấm kép).
   - Thuật toán này thỏa mãn hoàn toàn mã lỗi `USER_EMAIL_INVALID` với `ErrorType.Validation`.

3. Từ **Observation 4**:
   Các địa chỉ email seed (`admin@ittms.local`, v.v.) và `DummyUser` (`dummy@local`) đều có Host hợp lệ, User hợp lệ, không có khoảng trắng hoặc chấm kép. Do đó, logic kiểm tra mới này đảm bảo 100% tính tương thích ngược, không gây crash hoặc gián đoạn quy trình seed.

4. Từ **Observation 3**:
   Việc sửa code mà không đồng bộ hóa test assertion trong `DomainBoundaryAndSecurityAdversarialTests.cs` sẽ làm gãy pipeline kiểm thử. Cần cập nhật đồng thời cả code nghiệp vụ và test assertion.

---

## 3. CAVEATS (CÁC GIỚI HẠN VÀ GIẢ ĐỊNH)

1. **Phạm vi nhiệm vụ**:
   Báo cáo này tập trung chuyên biệt vào 2 lỗi (PasswordHasher FormatException và User Email Validation). 2 lỗi còn lại của `challenger_m1_2` (`Attachment.Create` null contentType và `Ticket.Create` invalid priority enum) nằm ngoài scope của agent này (thuộc trách nhiệm của các remediation explorer khác).
2. **Quyền hạn mã nguồn**:
   Theo vai trò `explorer` (read-only investigation), agent không trực tiếp chỉnh sửa mã nguồn của dự án mà cung cấp patch cụ thể, chi tiết từng dòng trong `remediation_plan.md` cho `worker_m1` thi hành.
3. **Giả định về định dạng tên miền**:
   Hệ thống chấp nhận các tên miền nội bộ (`.local`, `localhost`) phù hợp với kiến trúc hệ thống mạng nội bộ doanh nghiệp theo RFC 5321.

---

## 4. CONCLUSION (KẾT LUẬN & ĐỀ XUẤT HÀNH ĐỘNG)

1. Khiếm khuyết của `PasswordHasherService` và `User.Create` đã có giải pháp khắc phục triệt để, chuẩn mực theo Clean Architecture và OWASP Fail-Closed.
2. Bản vá không gây suy giảm hiệu năng (Zero-ReDoS, $O(N)$ string scan), bảo toàn tính toàn vẹn của Seeder và bộ test 109 test cases hiện tại.
3. Đề xuất bàn giao cho `worker_m1` thực hiện 4 bước áp dụng mã đã thiết kế chi tiết tại `c:\IT_System\.agents\teamwork\explorer_m1_rem_1\remediation_plan.md`.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

Để kiểm chứng tính đúng đắn của giải pháp, chạy các lệnh sau tại thư mục gốc `c:\IT_System`:

1. **Kiểm tra biên dịch và chuẩn mã**:
   ```powershell
   dotnet build --configuration Debug
   ```
   *Kỳ vọng*: Biên dịch thành công với `0 Warning(s)`, `0 Error(s)`.

2. **Kiểm tra đơn vị cho Password Hasher**:
   ```powershell
   dotnet test --filter "FullyQualifiedName~PasswordHasher"
   ```
   *Kỳ vọng*: Toàn bộ các test case liên quan tới PasswordHasher (bao gồm cả test case bắt Base64 malformed trả về `false`) đạt `Passed`.

3. **Kiểm tra đơn vị cho User Invariants & Email Boundary**:
   ```powershell
   dotnet test --filter "FullyQualifiedName~User"
   ```
   *Kỳ vọng*: Mọi test case với email sai cú pháp trả về `USER_EMAIL_INVALID`, email hợp lệ và seed accounts đạt `Passed`.

4. **Kiểm tra toàn diện hệ thống**:
   ```powershell
   dotnet test
   ```
   *Kỳ vọng*: Toàn bộ 109/109 tests đạt `Passed 100%`.
