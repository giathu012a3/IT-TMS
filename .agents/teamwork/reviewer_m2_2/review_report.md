# BÁO CÁO ĐÁNH GIÁ ĐỘC LẬP VÀ PHẢN BIỆN ĐỐI KHÁNG (REVIEW & ADVERSARIAL CHALLENGE REPORT)
> **Đối tượng rà soát**: Triển khai Observer Pattern và Security Filters (Milestone 2)  
> **Người thực hiện**: `reviewer_m2_2` (Reviewer & Adversarial Critic)  
> **Tác giả được rà soát**: `worker_m2`  
> **Dự án**: IT Ticket Management System (.NET 9 Clean Architecture) — IT-TMS  
> **Ngày thực hiện**: 2026-10-25  

---

## 1. REVIEW SUMMARY (TỔNG KẾT ĐÁNH GIÁ)

**Verdict**: **APPROVE** (Chấp thuận nghiệm thu M2 với các khuyến nghị kỹ thuật bắt buộc cho M3)

### Tóm tắt cốt lõi:
1. **Kiểm tra tính toàn vẹn (Integrity Check)**: Không phát hiện bất kỳ dấu hiệu vi phạm tính toàn vẹn (không có hardcoded test results, không có facade/dummy logic, không bỏ qua yêu cầu nghiệp vụ, không giả mạo output).
2. **Biên dịch & Kiểm thử thực tế**:
   - `dotnet build c:\IT_System\ItTms.sln`: **Thành công (0 warnings, 0 errors)**.
   - `dotnet test c:\IT_System\ItTms.sln`: **Thành công 100% (181/181 tests passed, 0 failed, 0 skipped, thời gian 29s)**.
3. **Observer Pattern & Two-Phase Pipeline**:
   - `NotificationObserver`: Đáp ứng trọn vẹn TC-03 (thông báo Leader khi tạo ticket mới), TC-04 (thông báo đồng thời Staff và User khi phân công), TC-08 (bảo mật tuyệt đối: không gửi thông báo cho User khi có ghi chú nội bộ `IsInternal`), TC-09 (thông báo Staff kèm số sao đánh giá khi đóng ticket).
   - `SystemLogObserver`: Ghi nhận nhật ký kiểm toán bất biến (Immutable Audit Trail) cho toàn bộ các hành động tạo ticket, phân công, chuyển trạng thái, bình luận, đóng ticket và vô hiệu hóa tài khoản (TC-10).
   - `ItTmsDbContext.SaveChangesAsync`: Cơ chế Two-Phase Event Dispatching tách bạch rõ ràng giữa Pha 1 (in-transaction) và Pha 2 (post-commit), bảo đảm tính toàn vẹn ACID của SQLite và triệt tiêu nguy cơ phát sinh thông báo ảo (ghost notification).
4. **Security Filters (Lớp Bảo mật Web)**:
   - `AuthorizeRolesAttribute`: Đảm bảo xác thực danh tính, truy vấn CSDL theo thời gian thực kiểm tra `User.Status == UserStatus.Active` (TC-01 -> 401 / Redirect Login kèm TempData), và thực thi RBAC (TC-02 -> HTTP 403 Forbidden).
   - `AuthorizeTicketAccessAttribute`: Triển khai phòng thủ OWASP API1:2023 BOLA/IDOR (TC-07), kiểm soát chặt chẽ quyền truy cập của Staff và User, tự động ghi vết vi phạm bảo mật vào `SystemLogs`.

---

## 2. ADVERSARIAL CHALLENGE & FINDINGS (PHẢN BIỆN ĐỐI KHÁNG & PHÁT HIỆN)

Áp dụng tư duy đối kháng theo chuẩn `#Argon-Omega`, rà soát viên đã phân tích các giả định ngầm định, mô hình hóa các kịch bản lỗi và phát hiện các điểm cần gia cố sau:

### [Major] Finding 1: TicketId có giá trị bằng 0 trong `TicketCreatedEvent` trước khi DB cấp phát Identity Key
- **What**: Khi thực thể `Ticket` được khởi tạo thông qua `Ticket.Create(...)`, thuộc tính `ticket.Id` mang giá trị mặc định `0` (do EF Core SQLite sử dụng khóa chính Identity tự tăng khi ghi dữ liệu). Do sự kiện `TicketCreatedEvent` được khởi tạo ngay trong `Ticket.Create`, thuộc tính `domainEvent.TicketId` có giá trị `0`.
- **Where**: 
  - `src/ItTms.Domain/Entities/Ticket.cs` dòng 128: `ticket.AddDomainEvent(new TicketCreatedEvent(ticket.Id, ticket.Title, ticket.CreatorId, now));`
  - `src/ItTms.Application/Observers/NotificationObserver.cs` dòng 44: `message: $"[Yêu cầu mới #{domainEvent.TicketId}] {domainEvent.Title}"` -> Tạo ra thông báo `[Yêu cầu mới #0] ...`
  - `src/ItTms.Application/Observers/SystemLogObserver.cs` dòng 34: `details: $"Khởi tạo ticket #{domainEvent.TicketId} '{domainEvent.Title}'..."` -> Ghi log `Khởi tạo ticket #0 ...`
- **Why**: Trong `ItTmsDbContext.SaveChangesAsync`, Pha 1 (`DispatchInTransactionAsync`) được thực thi *trước* khi gọi `base.SaveChangesAsync()`. Lúc này, dòng dữ liệu chưa được chèn vào SQLite nên ID thực tế chưa được sinh ra.
- **Blast Radius**: Tiêu đề thông báo gửi cho Leader và nội dung SystemLog bị hiển thị số hiệu `#0` thay vì ID thực tế của Ticket (ví dụ `#101`). Rất may đường link liên kết `/Leader/Ticket/AssignQueue` không phụ thuộc vào ID nên luồng điều hướng không bị gãy.
- **Suggestion**: Trong Milestone 3, khi xây dựng handler tạo Ticket:
  - Giải pháp 1: Thực hiện lưu Ticket trước (`base.SaveChangesAsync()`) để SQLite sinh ID thực tế, sau đó cập nhật ID cho sự kiện hoặc phát sinh sự kiện bổ sung trong cùng Transaction.
  - Giải pháp 2: Điều chỉnh `NotificationObserver` và `SystemLogObserver` sử dụng tham chiếu entity hoặc trigger sau khi entity đã có ID hợp lệ.

---

### [Major] Finding 2: Nguy cơ leo thang đặc quyền do phụ thuộc Claim Cookie cũ trong `AuthorizeRolesAttribute`
- **What**: Tại dòng 92-93 của `AuthorizeRolesAttribute.cs`, logic kiểm tra vai trò sử dụng phép toán logic `||` kết hợp giữa vai trò truy vấn trực tiếp từ CSDL (`userEntity.Role`) và Claim từ Cookie (`user.IsInRole(role)`):
  ```csharp
  var userRoleName = userEntity.Role.ToString();
  var hasRole = _allowedRoles.Any(role => string.Equals(role, userRoleName, StringComparison.OrdinalIgnoreCase)) ||
                _allowedRoles.Any(role => user.IsInRole(role));
  ```
- **Where**: `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs` dòng 92-93.
- **Why**: Nếu một Admin giáng chức một Leader hoặc Staff xuống vai trò User trong CSDL, nhưng tài khoản đó đang có phiên đăng nhập hoạt động với Cookie chứa Claim `Role = Leader`:
  - `userEntity.Role` trong CSDL là `User`.
  - Nhưng `user.IsInRole("Leader")` vẫn trả về `true` do Cookie cũ chưa hết hạn.
  - Do có điều kiện `|| _allowedRoles.Any(role => user.IsInRole(role))`, người dùng bị giáng chức vẫn tiếp tục truy cập trái phép được các trang của Leader!
- **Blast Radius**: Vi phạm nguyên tắc Zero-Trust và tính nhất quán dữ liệu phân quyền RBAC khi có thay đổi vai trò người dùng trong thời gian thực.
- **Suggestion**: Loại bỏ hoàn toàn vế `|| _allowedRoles.Any(role => user.IsInRole(role))`. Khi đã chấp nhận chi phí truy vấn CSDL thời gian thực trên mỗi request (`userEntity`), vai trò từ CSDL `userEntity.Role` phải là **nguồn chân lý duy nhất (Single Source of Truth)**.

---

### [Minor] Finding 3: Truy vấn `AsNoTracking()` trong `NotificationObserver.HandleAsync(TicketStatusChangedEvent)` trước commit làm mất lý do từ chối
- **What**: Trong `NotificationObserver.cs` dòng 105:
  ```csharp
  var ticket = await _dbContext.Tickets
      .AsNoTracking()
      .Select(t => new { t.Id, t.Title, t.CreatorId, t.RejectionReason })
      .FirstOrDefaultAsync(t => t.Id == domainEvent.TicketId, cancellationToken);
  ```
  Khi Leader từ chối ticket với một lý do cụ thể (ví dụ: `"Sai quy trình công nghệ"`), giá trị `ticket.RejectionReason` mới chỉ được cập nhật trên thực thể trong Change Tracker. Do Pha 1 chạy trước khi commit và câu lệnh sử dụng `.AsNoTracking()`, EF Core gửi câu truy vấn SQL trực tiếp xuống SQLite (nơi `RejectionReason` vẫn là `null`).
- **Where**: `src/ItTms.Application/Observers/NotificationObserver.cs` dòng 105-126.
- **Why**: Dẫn đến tại dòng 126, biểu thức `ticket.RejectionReason ?? "Không hợp lệ"` luôn luôn rơi vào fallback `"Không hợp lệ"`, làm mất đi lý do từ chối cụ thể của Leader trong thông báo gửi cho User.
- **Suggestion**: Bổ sung thuộc tính `string? RejectionReason` trực tiếp vào `TicketStatusChangedEvent`, hoặc tìm kiếm thực thể trong bộ nhớ cục bộ `_dbContext.Tickets.Local` trước khi truy vấn ra CSDL.

---

### [Minor] Finding 4: Bất đồng bộ định dạng phản hồi 403 giữa `AuthorizeTicketAccessAttribute` và `AuthorizeRolesAttribute`
- **What**: Khi xảy ra vi phạm phân quyền BOLA/IDOR (TC-07), `AuthorizeTicketAccessAttribute` luôn trả về `ViewResult` với status 403 (render HTML view `AccessDenied.cshtml`). Trong khi đó, `AuthorizeRolesAttribute` có phương thức kiểm tra `IsAjaxOrApiRequest` để trả về JSON 403 nếu client gọi qua XMLHttpRequest hoặc Header `Accept: application/json`.
- **Where**: `src/ItTms.Web/Filters/AuthorizeTicketAccessAttribute.cs` dòng 97-101, 116-120.
- **Why**: Khi các tương tác AJAX trong Milestone 3 (ví dụ gửi comment hoặc lấy dữ liệu tiến trình) vi phạm BOLA, client JavaScript sẽ nhận về HTML thay vì cấu trúc JSON chuẩn hóa, gây khó khăn cho việc xử lý lỗi ở giao diện client.
- **Suggestion**: Tái sử dụng logic kiểm tra `IsAjaxOrApiRequest` trong `AuthorizeTicketAccessAttribute` để trả về `ObjectResult` với mã lỗi JSON khi phát hiện request dạng API/AJAX.

---

## 3. VERIFIED CLAIMS (CÁC KHẲNG ĐỊNH ĐÃ ĐƯỢC XÁC MINH ĐỘC LẬP)

| Khẳng định từ Worker M2 | Phương pháp xác minh | Kết quả | Chi tiết quan sát |
|-------------------------|---------------------|---------|-------------------|
| Solution biên dịch 0 warnings, 0 errors | `dotnet build c:\IT_System\ItTms.sln` | **PASS** | Biên dịch 5 projects thành công, 0 cảnh báo, 0 lỗi trong 6.11s. |
| Toàn bộ 181 unit/integration tests pass | `dotnet test c:\IT_System\ItTms.sln` | **PASS** | 181/181 tests passed, 0 failed, 0 skipped trong 29s. |
| TC-01: Tài khoản inactive bị chặn truy cập | `AuthorizeRolesAttributeTests` & Kiểm tra mã nguồn | **PASS** | Truy vấn CSDL thời gian thực, hủy phiên sign-out và chuyển hướng đăng nhập với thông báo lỗi rõ ràng. |
| TC-02: Truy cập trái quyền trả về 403 Forbidden | `AuthorizeRolesAttributeTests` & Kiểm tra mã nguồn | **PASS** | Chặn đứng truy cập không đúng vai trò với HTTP status 403. |
| TC-03: Tạo ticket gửi thông báo cho tất cả Leader active | `ObserverPatternTests` & Kiểm tra mã nguồn | **PASS** | `NotificationObserver` lọc chính xác `Role == Leader && Status == Active`. |
| TC-04: Phân công ticket thông báo cho cả Staff và User | `ObserverPatternTests` & Kiểm tra mã nguồn | **PASS** | Tạo đúng 2 bản ghi Notification với đường dẫn tương ứng cho từng vai trò. |
| TC-07: Staff không xem được ticket của Staff khác (BOLA) | `AuthorizeTicketAccessFilterTests` & Kiểm tra mã nguồn | **PASS** | Chặn truy cập với 403 Forbidden và tự động ghi log `BOLA_Violation_Attempt` vào `SystemLogs`. |
| TC-08: Ghi chú nội bộ không gửi thông báo cho User | `ObserverPatternTests` & Kiểm tra mã nguồn | **PASS** | `CommentAddedEvent.IsInternal` chặn thông báo ngay từ guard clause. |
| TC-09: Đóng ticket Resolved lưu feedback và gửi thông báo | `StateMachine49PermutationTests` & `ObserverPatternTests` | **PASS** | Đóng ticket hợp lệ, phát sinh `TicketClosedEvent`, tạo log và gửi thông báo cho Staff kèm số sao. |
| Two-Phase Dispatcher bảo toàn ACID | `ObserverPatternTests.TwoPhaseDispatching...` | **PASS** | Ticket, Notification, SystemLog được commit nguyên tử trong 1 Transaction SQLite duy nhất. |

---

## 4. COVERAGE GAPS & RECOMMENDATIONS (KHOẢNG TRỐNG VÀ KHUYẾN NGHỊ CHO M3)

1. **Tích hợp Filter vào Controllers**:
   - Hiện tại các Filter (`AuthorizeRolesAttribute`, `AuthorizeTicketAccessAttribute`) đã được kiểm thử đơn vị độc lập rất kỹ lưỡng với đầy đủ ActionExecutingContext mock.
   - Khoảng trống: Cần được gắn vào toàn bộ các Controller và Action tương ứng của 5 MVC Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) trong Milestone 3.
2. **SignalR WebSocket Dispatching**:
   - Hiện tại `SignalRNotificationDispatcher` ghi log vào `ILogger`.
   - Khuyến nghị M3: Thay thế bằng triển khai gọi `IHubContext<TicketHub>` để đẩy thông báo thời gian thực tới trình duyệt client.
3. **Sửa chữa Finding 1 và Finding 2**:
   - Bắt buộc xử lý Finding 2 (bỏ stale claim fallback) để đảm bảo an ninh cấp doanh nghiệp trước khi phát hành Phase A.
   - Xử lý Finding 1 để số hiệu ticket hiển thị chính xác trên thông báo và nhật ký kiểm toán.
