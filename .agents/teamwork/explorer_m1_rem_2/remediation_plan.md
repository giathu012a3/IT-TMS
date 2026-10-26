# KẾ HOẠCH KHẮC PHỤC KHIẾM KHUYẾT DOMAIN (REMEDIATION PLAN)
## TICKET PRIORITY ENUM VALIDATION & ATTACHMENT CONTENT-TYPE GUARD

> **Tác giả**: `explorer_m1_rem_2` (Domain & Invariants Explorer)  
> **Phạm vi**:  
> 1. `Ticket.Create`: Kiểm tra tính hợp lệ của Enum `TicketPriority` bằng `Enum.IsDefined`.  
> 2. `Attachment.Create`: Kiểm tra `contentType` `null` / `whitespace` và giới hạn độ dài.  
> **Mục tiêu**: Loại bỏ triệt để unhandled exceptions, bảo vệ bất biến mô hình thực thể miền (Domain Invariants), bảo đảm nguyên tắc Fail-Closed và triết lý Railway-Oriented Programming (`Result<T>`).

---

## 1. TỔNG QUAN VÀ ĐÁNH GIÁ CỐT LÕI (BLUF)

Qua đối chiếu trực tiếp giữa báo cáo của `challenger_m1_2` (`challenge_report.md`), mã nguồn miền (`Ticket.cs`, `Attachment.cs`), cấu hình EF Core (`TicketConfiguration.cs`, `AttachmentConfiguration.cs`) và bộ kiểm thử đối kháng (`DomainBoundaryAndSecurityAdversarialTests.cs`):

1. **Khiếm khuyết 1 - `Ticket.Create`**: 
   - Trong C#, kiểu `enum` thực chất là một kiểu số nguyên (`int`), cho phép ép kiểu bất kỳ giá trị nào mà không gây lỗi runtime (`(TicketPriority)(-1)`).
   - Hàm `Ticket.Create` hiện chỉ kiểm tra `title`, `description`, `creatorId` mà hoàn toàn bỏ qua việc xác thực `priority`.
   - Khi nhận `priority` âm hoặc không định nghĩa, `CalculateDueAt` fallback về SLA mặc định 24 giờ, và thực thể `Ticket` được lưu với `Priority` không hợp lệ. Do `TicketConfiguration` sử dụng `.HasConversion<string>()`, chuỗi `"-1"` được ghi thẳng vào bảng `Tickets` của cơ sở dữ liệu SQLite, làm sai lệch chỉ mục `IX_Tickets_StatusId_Priority_CreatedAt` và gây crash/lỗi logic tại các tầng giao diện (badge render, filter UC17).
2. **Khiếm khuyết 2 - `Attachment.Create`**:
   - Dòng 48 của `Attachment.cs` gọi trực tiếp `contentType.Trim().ToLowerInvariant()`.
   - Khi người dùng gửi tệp tin không có header `Content-Type` hoặc hàm nhận giá trị `null`, mã nguồn ném trực tiếp `System.NullReferenceException` chưa được bắt.
   - Điều này vi phạm nguyên tắc bao đóng miền: phương thức Factory tĩnh phải trả về `Result<Attachment>.Failure` chứ không bao giờ để rò rỉ ngoại lệ runtime ra ngoài.
   - Ngoài ra, chuỗi chỉ chứa khoảng trắng `"   "` sẽ được `Trim()` thành chuỗi rỗng `""`, vi phạm ràng buộc cơ sở dữ liệu `builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100)`.

---

## 2. CHI TIẾT KHIẾM KHUYẾT 1: `Ticket.Create` & `TicketPriority`

### 2.1. Vị trí mã nguồn hiện tại
- **Tệp tin**: `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs`
- **Dòng hiện tại**: 52–75
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
    ...
```

### 2.2. Cơ chế thất bại & Tác động lan tỏa (Cascade Failure)
1. **Ép kiểu ngầm định của Enum**: `TicketPriority` có 4 giá trị hợp lệ: `Low = 1`, `Medium = 2`, `High = 3`, `Critical = 4`. Nếu client gửi số nguyên `-1`, `0`, `999` hoặc `int.MinValue`, phép ép kiểu `(TicketPriority)rawVal` diễn ra trơn tru mà không có ngoại lệ.
2. **SLA Fallback che giấu lỗi**: Trong `CalculateDueAt`:
   ```csharp
   public static DateTime CalculateDueAt(TicketPriority priority, DateTime createdAt)
   {
       return priority switch
       {
           TicketPriority.Critical => createdAt.AddHours(1),
           TicketPriority.High     => createdAt.AddHours(4),
           TicketPriority.Medium   => createdAt.AddHours(24),
           TicketPriority.Low      => createdAt.AddHours(72),
           _                       => createdAt.AddHours(24) // Fallback ngầm định
       };
   }
   ```
   Hàm tính SLA vẫn trả về `createdAt + 24h`, tạo cảm giác giả rằng dữ liệu hợp lệ.
3. **Nhiễm bẩn dữ liệu cơ sở dữ liệu (Database Data Corruption)**:
   Trong `TicketConfiguration.cs`:
   ```csharp
   builder.Property(t => t.Priority)
       .IsRequired()
       .HasConversion<string>()
       .HasMaxLength(20);
   ```
   Khi lưu vào SQLite, EF Core gọi `priority.ToString()`. Giá trị `-1` sẽ được chuyển đổi thành chuỗi `"-1"` và lưu vào cột `Priority` dạng text. Khi đọc lại, nếu tầng Web dùng `Enum.Parse<TicketPriority>` hoặc switch expression không có default, ứng dụng sẽ sập ngay lập tức.
4. **Phá vỡ chỉ mục**: Chỉ mục tổng hợp `IX_Tickets_StatusId_Priority_CreatedAt` chứa chuỗi rác, làm sai lệch kết quả thống kê báo cáo tại UC17.

### 2.3. Mã đề xuất khắc phục (Remediation Diff)
Bổ sung kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)` ngay sau kiểm tra `description`:

```csharp
<<<< CURRENT (src/ItTms.Domain/Entities/Ticket.cs:66-71)
        if (string.IsNullOrWhiteSpace(description))
            return Result<Ticket>.Failure(new Error("TICKET_DESC_REQUIRED", "Mô tả chi tiết không được để trống.", ErrorType.Validation));

        if (creatorId <= 0)
            return Result<Ticket>.Failure(new Error("TICKET_CREATOR_INVALID", "Người tạo không hợp lệ.", ErrorType.Validation));
==== PROPOSED
        if (string.IsNullOrWhiteSpace(description))
            return Result<Ticket>.Failure(new Error("TICKET_DESC_REQUIRED", "Mô tả chi tiết không được để trống.", ErrorType.Validation));

        if (!Enum.IsDefined(typeof(TicketPriority), priority))
            return Result<Ticket>.Failure(new Error("TICKET_PRIORITY_INVALID", "Mức độ ưu tiên yêu cầu không hợp lệ.", ErrorType.Validation));

        if (creatorId <= 0)
            return Result<Ticket>.Failure(new Error("TICKET_CREATOR_INVALID", "Người tạo không hợp lệ.", ErrorType.Validation));
>>>>
```

*(Khuyến nghị bổ sung ngoài lề)*: Cũng nên áp dụng kiểm tra tương tự cho `TicketCategory`:
```csharp
        if (!Enum.IsDefined(typeof(TicketCategory), category))
            return Result<Ticket>.Failure(new Error("TICKET_CATEGORY_INVALID", "Danh mục sự cố không hợp lệ.", ErrorType.Validation));
```

### 2.4. Ma trận trường hợp biên (Boundary Test Matrix)
| Đầu vào `priority` | Kiểu dữ liệu | Kết quả mong đợi | Mã lỗi (Error.Code) |
|---|---|---|---|
| `(TicketPriority)(-1)` | Số âm | `IsFailure = true` | `TICKET_PRIORITY_INVALID` |
| `(TicketPriority)0` | Dưới dải (Min-1) | `IsFailure = true` | `TICKET_PRIORITY_INVALID` |
| `(TicketPriority)999` | Vượt dải (Max+1) | `IsFailure = true` | `TICKET_PRIORITY_INVALID` |
| `(TicketPriority)int.MinValue` | Cực trị âm | `IsFailure = true` | `TICKET_PRIORITY_INVALID` |
| `(TicketPriority)int.MaxValue` | Cực trị dương | `IsFailure = true` | `TICKET_PRIORITY_INVALID` |
| `TicketPriority.Low` (1) | Hợp lệ | `IsSuccess = true` | `None` |
| `TicketPriority.Medium` (2) | Hợp lệ | `IsSuccess = true` | `None` |
| `TicketPriority.High` (3) | Hợp lệ | `IsSuccess = true` | `None` |
| `TicketPriority.Critical` (4) | Hợp lệ | `IsSuccess = true` | `None` |

---

## 3. CHI TIẾT KHIẾM KHUYẾT 2: `Attachment.Create` & `ContentType`

### 3.1. Vị trí mã nguồn hiện tại
- **Tệp tin**: `c:\IT_System\src\ItTms.Domain\Entities\Attachment.cs`
- **Dòng hiện tại**: 21–53
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

### 3.2. Cơ chế thất bại & Tác động lan tỏa
1. **Ném `NullReferenceException` chưa xử lý**:
   Tại dòng 48: `ContentType = contentType.Trim().ToLowerInvariant()`.
   Khi tham số `contentType` là `null`, lời gọi `.Trim()` lập tức ném ra ngoại lệ `System.NullReferenceException`. Trong mô hình Clean Architecture, Domain Entities phải đóng gói toàn bộ quy tắc bất biến và chỉ trả về monad `Result<T>`. Việc để văng `NullReferenceException` phá vỡ hoàn toàn nguyên tắc ROP và gây ra HTTP 500 nếu tầng Controller không bắt ngoại lệ.
2. **Cho phép chuỗi rỗng / khoảng trắng**:
   Nếu `contentType` là `"   "` (3 dấu cách), `string.IsNullOrWhiteSpace` chưa được kiểm tra, dẫn tới việc `contentType.Trim()` trả về `""` (chuỗi rỗng).
3. **Vi phạm ràng buộc cơ sở dữ liệu (`AttachmentConfiguration.cs`)**:
   ```csharp
   builder.Property(a => a.ContentType)
       .IsRequired()
       .HasMaxLength(100);
   ```
   Nếu `ContentType` là rỗng `""` hoặc dài hơn 100 ký tự (ví dụ: client gửi MIME type độc hại độ dài 500 ký tự), EF Core sẽ ném ngoại lệ `DbUpdateException` khi gọi `SaveChangesAsync`.

### 3.3. Mã đề xuất khắc phục (Remediation Diff)
Bổ sung kiểm tra `string.IsNullOrWhiteSpace(contentType)` và giới hạn độ dài `contentType.Length > 100`:

```csharp
<<<< CURRENT (src/ItTms.Domain/Entities/Attachment.cs:34-42)
        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result<Attachment>.Failure(new Error("ATTACH_ORIGINAL_REQUIRED", "Tên tệp gốc không được để trống.", ErrorType.Validation));

        if (fileSize <= 0)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_EMPTY", "Tệp tin không có dung lượng.", ErrorType.Validation));

        if (fileSize > MaxFileSizeBytes)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_TOO_LARGE", "Dung lượng tệp tin vượt quá giới hạn cho phép (16MB).", ErrorType.Validation));
==== PROPOSED
        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result<Attachment>.Failure(new Error("ATTACH_ORIGINAL_REQUIRED", "Tên tệp gốc không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(contentType))
            return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_REQUIRED", "Kiểu nội dung tệp tin không được để trống.", ErrorType.Validation));

        if (contentType.Length > 100)
            return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_TOO_LONG", "Kiểu nội dung tệp tin không được vượt quá 100 ký tự.", ErrorType.Validation));

        if (fileSize <= 0)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_EMPTY", "Tệp tin không có dung lượng.", ErrorType.Validation));

        if (fileSize > MaxFileSizeBytes)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_TOO_LARGE", "Dung lượng tệp tin vượt quá giới hạn cho phép (16MB).", ErrorType.Validation));
>>>>
```

### 3.4. Ma trận trường hợp biên (Boundary Test Matrix)
| Đầu vào `contentType` | Tình huống kiểm thử | Kết quả mong đợi | Mã lỗi (Error.Code) |
|---|---|---|---|
| `null` | Không có header Content-Type | `IsFailure = true` (không ném ngoại lệ) | `ATTACH_CONTENT_TYPE_REQUIRED` |
| `""` (Rỗng) | Chuỗi rỗng | `IsFailure = true` | `ATTACH_CONTENT_TYPE_REQUIRED` |
| `"   "` (Khoảng trắng) | Chỉ chứa khoảng trắng | `IsFailure = true` | `ATTACH_CONTENT_TYPE_REQUIRED` |
| Chuỗi 101 ký tự | Vượt giới hạn độ dài cột DB | `IsFailure = true` | `ATTACH_CONTENT_TYPE_TOO_LONG` |
| `"image/png"` | Chuỗi MIME chuẩn | `IsSuccess = true` | `None` |
| `"  IMAGE/PNG  "` | Chuỗi có khoảng trắng & chữ hoa | `IsSuccess = true`, lưu `"image/png"` | `None` |
| `"application/pdf"` | Tài liệu PDF | `IsSuccess = true` | `None` |

---

## 4. ĐỒNG BỘ HÓA BỘ KIỂM THỬ (TEST SUITE SYNCHRONIZATION)

### 4.1. Thực trạng trong `DomainBoundaryAndSecurityAdversarialTests.cs`
Trong tệp `DomainBoundaryAndSecurityAdversarialTests.cs` do `challenger_m1_2` thiết kế, các assertions hiện tại được viết nhằm **chứng minh sự tồn tại của khiếm khuyết**:
1. Dòng 63: `result.IsSuccess.Should().BeTrue("DEFECT: Ticket.Create currently lacks Enum.IsDefined check...")`
2. Dòng 172: `act.Should().Throw<NullReferenceException>()`

Nếu chúng ta sửa lỗi trong Domain mà không đồng bộ test suite, 2 bài kiểm tra trên sẽ báo **FAIL** vì:
- `Ticket.Create` lúc này sẽ trả về `IsFailure = true` (thay vì `IsSuccess = true`).
- `Attachment.Create` sẽ trả về `Result.Failure` (thay vì ném `NullReferenceException`).

### 4.2. Cập nhật mã kiểm thử đối kháng (Test Diff)
Chuyển đổi các bài kiểm tra từ trạng thái "Vạch trần lỗi" sang trạng thái "Bảo vệ hồi quy" (Regression Guard):

```csharp
// 1. Cập nhật TicketCreate_WithNegativeOrUndefinedPriority:
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

    result.IsFailure.Should().BeTrue("Ticket.Create phải từ chối các mức độ ưu tiên ngoài phạm vi định nghĩa của enum");
    result.Error.Code.Should().Be("TICKET_PRIORITY_INVALID");
    result.Error.Type.Should().Be(ErrorType.Validation);
}

// 2. Cập nhật AttachmentCreate_WithNullContentType:
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

    result.IsFailure.Should().BeTrue("Attachment.Create phải từ chối ContentType null hoặc rỗng thay vì ném NullReferenceException");
    result.Error.Code.Should().Be("ATTACH_CONTENT_TYPE_REQUIRED");
    result.Error.Type.Should().Be(ErrorType.Validation);
}

[Fact]
public void AttachmentCreate_WithOversizedContentType_FailsValidation()
{
    var longContentType = new string('x', 101);
    var result = Attachment.Create(
        ticketId: 1,
        fileName: "guid-name.png",
        originalFileName: "screenshot.png",
        contentType: longContentType,
        fileSize: 1024
    );

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("ATTACH_CONTENT_TYPE_TOO_LONG");
}
```

### 4.3. Kiểm tra tính tương thích hồi quy (Regression Guard)
1. **`DataSeeder.cs`**:
   - `ticket1`: `TicketPriority.High` (3) -> Hợp lệ.
   - `ticket2`: `TicketPriority.Critical` (4) -> Hợp lệ.
   - `ticket3`: `TicketPriority.Medium` (2) -> Hợp lệ.
   - Không chứa `Attachment` seeding.
   - **Kết luận**: Hoàn toàn an toàn, không phá vỡ dữ liệu mồi.
2. **`AdversarialSlaStressTests.cs`**:
   - Bài test `CalculateDueAt_UndefinedPriorityEnum_FallsBackToDefault24Hours` gọi trực tiếp `Ticket.CalculateDueAt(invalidPriority, createdAt)`. Vì phương thức `CalculateDueAt` vẫn duy trì nhánh fallback `_ => createdAt.AddHours(24)`, bài test này tiếp tục **PASS 100%**.
3. **`TicketInvariantTests.cs`**:
   - Toàn bộ các bài kiểm thử vòng đời (`FullLifecycle_FromNewToClosed_TransitionsCorrectly`, `CreateTicket_WithValidData_...`) đều dùng `TicketPriority` hợp lệ. Tiếp tục **PASS 100%**.

---

## 5. HƯỚNG DẪN THỰC THI CHO WORKER AGENT

1. Mở `src/ItTms.Domain/Entities/Ticket.cs`, thêm kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)` tại dòng 67 theo đúng mẫu diff tại Mục 2.3.
2. Mở `src/ItTms.Domain/Entities/Attachment.cs`, thêm kiểm tra `string.IsNullOrWhiteSpace(contentType)` và `contentType.Length > 100` tại dòng 36 theo đúng mẫu diff tại Mục 3.3.
3. Phối hợp với `explorer_m1_rem_3` để cập nhật `tests/ItTms.Tests/AdversarialTests/DomainBoundaryAndSecurityAdversarialTests.cs` như mô tả tại Mục 4.2.
4. Chạy lệnh `dotnet test` xác nhận 109+ bài kiểm thử đều chuyển sang màu xanh (Passed).
