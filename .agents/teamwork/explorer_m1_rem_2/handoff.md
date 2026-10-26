# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — EXPLORER_M1_REM_2
## PHÂN TÍCH CHIẾN LƯỢC KHẮC PHỤC KHIẾM KHUYẾT DOMAIN: TICKET PRIORITY & ATTACHMENT CONTENT-TYPE

> **Agent**: `explorer_m1_rem_2`  
> **Working Directory**: `c:\IT_System\.agents\teamwork\explorer_m1_rem_2`  
> **Nhiệm vụ**: Phân tích giải pháp khắc phục cho:  
> 1. `Ticket.Create`: Kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)`.  
> 2. `Attachment.Create`: Kiểm tra `contentType` `null` / `whitespace`.  
> **Handoff Type**: Hard (Hoàn thành đầy đủ 5 thành phần chuẩn hóa).

---

### 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM TRỰC TIẾP)

- **Quan sát 1.1 - `Ticket.cs` thiếu kiểm tra Enum biên**:
  Tại `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs` dòng 52–75:
  ```csharp
  public static Result<Ticket> Create(
      string title,
      string description,
      TicketPriority priority,
      TicketCategory category,
      int creatorId,
      DateTime? currentTime = null)
  {
      if (string.IsNullOrWhiteSpace(title))
          return Result<Ticket>.Failure(new Error("TICKET_TITLE_REQUIRED", "Tiêu đề yêu cầu không được để trống.", ErrorType.Validation));
      if (title.Length > 150)
          return Result<Ticket>.Failure(new Error("TICKET_TITLE_TOO_LONG", "Tiêu đề không được vượt quá 150 ký tự.", ErrorType.Validation));
      if (string.IsNullOrWhiteSpace(description))
          return Result<Ticket>.Failure(new Error("TICKET_DESC_REQUIRED", "Mô tả chi tiết không được để trống.", ErrorType.Validation));
      if (creatorId <= 0)
          return Result<Ticket>.Failure(new Error("TICKET_CREATOR_INVALID", "Người tạo không hợp lệ.", ErrorType.Validation));

      var now = currentTime ?? DateTime.UtcNow;
      var dueAt = CalculateDueAt(priority, now);
  ```
  Không tồn tại bất kỳ dòng kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)`.

- **Quan sát 1.2 - Lưu giá trị corrupt vào DB qua EF Core**:
  Tại `c:\IT_System\src\ItTms.Infrastructure\Persistence\Configurations\TicketConfiguration.cs` dòng 23–26:
  ```csharp
  builder.Property(t => t.Priority)
      .IsRequired()
      .HasConversion<string>()
      .HasMaxLength(20);
  ```
  Khi truyền `(TicketPriority)(-1)`, `priority.ToString()` sinh ra chuỗi `"-1"`, được lưu trực tiếp vào cơ sở dữ liệu và chỉ mục `IX_Tickets_StatusId_Priority_CreatedAt`.

- **Quan sát 1.3 - `Attachment.cs` gọi trực tiếp `.Trim()` trên `contentType`**:
  Tại `c:\IT_System\src\ItTms.Domain\Entities\Attachment.cs` dòng 21–53:
  ```csharp
  public static Result<Attachment> Create(
      int ticketId,
      string fileName,
      string originalFileName,
      string contentType,
      long fileSize)
  {
      if (ticketId <= 0)
          return Result<Attachment>.Failure(new Error("ATTACH_TICKET_INVALID", "Mã ticket không hợp lệ.", ErrorType.Validation));
      if (string.IsNullOrWhiteSpace(fileName))
          return Result<Attachment>.Failure(new Error("ATTACH_FILENAME_REQUIRED", "Tên tệp lưu trữ không được để trống.", ErrorType.Validation));
      if (string.IsNullOrWhiteSpace(originalFileName))
          return Result<Attachment>.Failure(new Error("ATTACH_ORIGINAL_REQUIRED", "Tên tệp gốc không được để trống.", ErrorType.Validation));
      if (fileSize <= 0)
          return Result<Attachment>.Failure(new Error("ATTACH_FILE_EMPTY", "Tệp tin không có dung lượng.", ErrorType.Validation));
      if (fileSize > MaxFileSizeBytes)
          return Result<Attachment>.Failure(new Error("ATTACH_FILE_TOO_LARGE", "Dung lượng tệp tin vượt quá giới hạn cho phép (16MB).", ErrorType.Validation));

      var attachment = new Attachment
      {
          TicketId = ticketId,
          FileName = fileName.Trim(),
          OriginalFileName = originalFileName.Trim(),
          ContentType = contentType.Trim().ToLowerInvariant(),
          FileSize = fileSize,
          CreatedAt = DateTime.UtcNow
      };
      return Result<Attachment>.Success(attachment);
  }
  ```
  Tại dòng 48, `contentType.Trim()` được gọi mà không kiểm tra `contentType` có `null` hoặc whitespace hay không.

- **Quan sát 1.4 - Bài test đối kháng hiện tại kỳ vọng hành vi lỗi**:
  Tại `c:\IT_System\tests\ItTms.Tests\AdversarialTests\DomainBoundaryAndSecurityAdversarialTests.cs`:
  - Dòng 63: `result.IsSuccess.Should().BeTrue("DEFECT: Ticket.Create currently lacks Enum.IsDefined check...")`
  - Dòng 172: `act.Should().Throw<NullReferenceException>()`
  Bộ test của challenger đang assert rằng các khiếm khuyết này **tồn tại**. Khi fix code trong Domain, 2 test này sẽ FAIL nếu không được cập nhật.

- **Quan sát 1.5 - Kết quả chạy kiểm thử thực tế**:
  Lệnh `dotnet test` thực thi trên workspace hiện tại:
  `Passed! - Failed: 0, Passed: 109, Skipped: 0, Total: 109, Duration: 26 s - ItTms.Tests.dll (net9.0)`.

---

### 2. LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. Từ **Quan sát 1.1**: Vì C# cho phép ép kiểu số nguyên bất kỳ sang Enum, `Ticket.Create` chấp nhận các giá trị không hợp lệ (như `-1`, `0`, `999`).
2. Kết hợp với **Quan sát 1.2**: EF Core cấu hình lưu `Priority` dưới dạng `string` thông qua `.HasConversion<string>()`. Khi lưu giá trị không hợp lệ, chuỗi `"-1"` được ghi vào SQLite, gây nhiễm bẩn dữ liệu cơ sở dữ liệu và làm hỏng logic hiển thị/thống kê của UC17.
   $\rightarrow$ **Suy luận 1**: Cần phải thêm kiểm tra `!Enum.IsDefined(typeof(TicketPriority), priority)` trong `Ticket.Create` để trả về `Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", "Mức độ ưu tiên yêu cầu không hợp lệ.", ErrorType.Validation))`.
3. Từ **Quan sát 1.3**: Tại dòng 48 `Attachment.cs`, nếu `contentType` là `null`, lời gọi `.Trim()` sẽ quăng `NullReferenceException`. Trong kiến trúc Clean Architecture kết hợp ROP, Factory method của thực thể miền không bao giờ được phép để lọt unhandled exception mà phải bao đóng thành `Result.Failure`.
4. Nếu `contentType` là khoảng trắng `"   "`, `Trim()` trả về chuỗi rỗng `""`. Cấu hình `AttachmentConfiguration.cs` quy định `ContentType` là `IsRequired()` và có độ dài tối đa 100 ký tự.
   $\rightarrow$ **Suy luận 2**: Cần thêm kiểm tra `string.IsNullOrWhiteSpace(contentType)` trả về `"ATTACH_CONTENT_TYPE_REQUIRED"` và `contentType.Length > 100` trả về `"ATTACH_CONTENT_TYPE_TOO_LONG"`.
5. Từ **Quan sát 1.4**: Các bài kiểm thử trong `DomainBoundaryAndSecurityAdversarialTests.cs` được viết để chứng minh lỗi (asserting `IsSuccess == true` cho invalid priority và `Throw<NullReferenceException>()` cho null contentType).
   $\rightarrow$ **Suy luận 3**: Khi Worker thực thi sửa lỗi trong Domain, bộ kiểm thử này bắt buộc phải được đồng bộ để assert `IsFailure == true` và kiểm tra Error Code tương ứng, tránh làm gãy `dotnet test`.
6. Từ **Quan sát 1.5**: Bộ test hiện tại có 109 bài test xanh. Các bài test trong `AdversarialSlaStressTests.cs` kiểm tra `CalculateDueAt` trực tiếp (chứ không qua `Ticket.Create`), nên việc sửa `Ticket.Create` không làm ảnh hưởng đến fallback an toàn của `CalculateDueAt`.

---

### 3. CAVEATS (GIỚI HẠN & CÁC ĐIỂM CẦN LƯU Ý)

1. **Phạm vi quyền hạn**: Explorer hoạt động ở chế độ Read-Only, không trực tiếp sửa mã nguồn `src/` hoặc `tests/`. Kế hoạch sửa đổi cụ thể đã được lập tại `remediation_plan.md` để Worker thực thi.
2. **TicketCategory**: Ngoài `TicketPriority`, trường `category` cũng là một enum (`TicketCategory`). Mặc dù challenger chỉ yêu cầu kiểm tra `TicketPriority`, khuyến nghị Worker cũng nên bổ sung `Enum.IsDefined(typeof(TicketCategory), category)` để đảm bảo tính phòng thủ toàn diện.
3. **Phối hợp đồng bộ**: Cần sự phối hợp chặt chẽ với `explorer_m1_rem_3` (phụ trách Regression Guard & Test Synchronization) để đảm bảo cập nhật đồng thời cả mã nguồn miền và mã kiểm thử đối kháng.

---

### 4. CONCLUSION (KẾT LUẬN & ĐỀ XUẤT HÀNH ĐỘNG)

1. **Về `Ticket.Create`**: Bổ sung đoạn mã sau tại dòng 67 của `src/ItTms.Domain/Entities/Ticket.cs`:
   ```csharp
   if (!Enum.IsDefined(typeof(TicketPriority), priority))
       return Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", "Mức độ ưu tiên yêu cầu không hợp lệ.", ErrorType.Validation));
   ```
2. **Về `Attachment.Create`**: Bổ sung đoạn mã sau tại dòng 36 của `src/ItTms.Domain/Entities/Attachment.cs`:
   ```csharp
   if (string.IsNullOrWhiteSpace(contentType))
       return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_REQUIRED", "Kiểu nội dung tệp tin không được để trống.", ErrorType.Validation));

   if (contentType.Length > 100)
       return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_TOO_LONG", "Kiểu nội dung tệp tin không được vượt quá 100 ký tự.", ErrorType.Validation));
   ```
3. **Về Test Suite**: Cập nhật 2 bài test trong `DomainBoundaryAndSecurityAdversarialTests.cs` để assert `result.IsFailure.Should().BeTrue()` cùng mã lỗi tương ứng theo hướng dẫn tại Mục 4.2 của `remediation_plan.md`.

---

### 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

1. **Kiểm tra tệp phân tích**:
   Xem tệp kế hoạch chi tiết tại: `c:\IT_System\.agents\teamwork\explorer_m1_rem_2\remediation_plan.md`.
2. **Lệnh kiểm chứng sau khi Worker áp dụng**:
   - Biên dịch và chạy toàn bộ test suite:
     ```powershell
     dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --logger "console;verbosity=detailed"
     ```
   - Điều kiện pass: 109/109 tests đạt kết quả PASSED (100%), không có ngoại lệ `NullReferenceException` hoặc lỗi biên dịch.
3. **Điều kiện vô hiệu hóa (Invalidation Condition)**:
   Nếu `Ticket.Create` với `priority = -1` vẫn trả về `IsSuccess = true`, hoặc `Attachment.Create` với `contentType = null` ném `NullReferenceException`, giải pháp bị coi là thất bại.
