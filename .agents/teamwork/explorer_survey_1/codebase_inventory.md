# BÁO CÁO KIỂM KÊ TOÀN DIỆN MÃ NGUỒN HỆ THỐNG IT-TMS (.NET 9 ASP.NET CORE MVC)

**Người thực hiện**: Explorer Survey 1 (`teamwork_preview_explorer`)  
**Ngày thực hiện**: 26/10/2026  
**Thư mục làm việc**: `c:\IT_System\.agents\teamwork\explorer_survey_1`  
**Dự án**: Hệ thống Quản lý Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
**Khóa học**: Thiết kế Phần mềm Hướng Đối tượng (TKPMHDT) - HCMUTE  
**Nhóm sinh viên thực hiện dự án**: Nhóm 7  
- Nguyễn Gia Thụ (25810041) - Phân hệ Kỹ thuật viên (Staff)
- Nguyễn Mạnh Tiến (25810043) - Phân hệ Trưởng nhóm (Leader)
- Hồ Thanh Tình (25810044) - Phân hệ Người dùng (User)
- Trần Thanh Danh (25810009) - Phân hệ Quản trị & Bảo mật (Admin & Auth)

---

## TỔNG QUAN KIẾN TRÚC HỆ THỐNG (BLUF)
- **Kiến trúc giải pháp**: Hệ thống đã được quy hoạch phẳng hóa về **1 Project ASP.NET Core MVC kinh điển (`src/ItTms`)** kết hợp với dự án kiểm thử xUnit (`tests/ItTms.Tests`).
- **Nền tảng**: .NET 9 (`net9.0`), C# 13, ASP.NET Core MVC, Entity Framework Core 9 (SQLite WAL mode), SignalR WebSocket.
- **Tính toàn vẹn kiểm thử**: Vượt qua **342/342 automated tests (100% PASS)** qua lệnh `dotnet test c:\IT_System\ItTms.sln`, bao gồm trọn vẹn 10 kịch bản nghiệm thu nghiệp vụ cốt lõi tại Bảng 7.2.
- **Thời gian thực**: Triệt tiêu hoàn toàn anti-pattern Polling 3s, thay thế bằng **SignalR WebSocket Hub** tại endpoint `/hubs/ticket`.

---

## 1. KHẢO SÁT 8 THỰC THỂ CƠ SỞ DỮ LIỆU / ENTITIES TRONG DBCONTEXT

Hệ thống quản lý dữ liệu thông qua `ItTmsDbContext` (`src/ItTms/Data/ItTmsDbContext.cs`) kế thừa `DbContext` và `IItTmsDbContext`. Cơ sở dữ liệu SQLite hoạt động ở chế độ `PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;`.

### 1.1. Bảng `Users` (Người dùng hệ thống)
- **File Entity**: `src/ItTms/Models/Entities/User.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/UserConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng - `ValueGeneratedOnAdd`)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã định danh người dùng |
  | `Username` | `string` | Not Null, Max 80, Unique Index (`IX_Users_Username`) | Tên đăng nhập chuẩn hóa viết thường |
  | `Email` | `string` | Not Null, Max 150, Unique Index (`IX_Users_Email`) | Địa chỉ thư điện tử |
  | `PasswordHash` | `string` | Not Null, Max 255 | Mật khẩu băm PBKDF2 HMAC-SHA512 |
  | `FullName` | `string` | Not Null, Max 120 | Họ và tên đầy đủ |
  | `Role` | `UserRole` (enum) | Not Null, string conversion, Max 20, Index (`IX_Users_Role`) | Vai trò: `Admin`, `Leader`, `Staff`, `User` |
  | `Status` | `UserStatus` (enum) | Not Null, string conversion, Max 20, Default: `Active` | Trạng thái tài khoản: `Active`, `Inactive` |
  | `Department` | `string?` | Nullable, Max 100 | Phòng ban / Bộ phận công tác |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Thời điểm tạo tài khoản |
  | `UpdatedAt` | `DateTime?` | Nullable, UTC | Thời điểm cập nhật thông tin gần nhất |
- **Quan hệ (Relationships)**:
  - `CreatedTickets` (`1 - n`): Một người dùng có thể tạo nhiều Ticket (`HasForeignKey(t => t.CreatorId).OnDelete(DeleteBehavior.Restrict)`).
  - `AssignedTickets` (`1 - n`): Một kỹ thuật viên có thể được phân công nhiều Ticket (`HasForeignKey(t => t.AssignedToId).OnDelete(DeleteBehavior.SetNull)`).
  - `Comments` (`1 - n`): Một người dùng có thể viết nhiều bình luận (`HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict)`).
  - `Notifications` (`1 - n`): Một người dùng có thể nhận nhiều thông báo (`HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade)`).
  - `SystemLogs` (`1 - n`): Một người dùng liên kết với các bản ghi nhật ký kiểm toán (`HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull)`).

---

### 1.2. Bảng `TicketStatuses` (Danh mục trạng thái yêu cầu)
- **File Entity**: `src/ItTms/Models/Entities/TicketStatus.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/TicketStatusConfiguration.cs`
- **Khóa chính**: `Id` (int, `ValueGeneratedNever` - Cố định từ 1 đến 7 tương ứng với 7 trạng thái của State Pattern)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Cố định (1..7) | Mã định danh trạng thái |
  | `Name` | `string` | Not Null, Max 50, Unique Index (`IX_TicketStatuses_Name`) | Mã code tiếng Anh: `New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected` |
  | `Label` | `string` | Not Null, Max 100 | Nhãn tiếng Việt: Mới tạo, Đã phân công, Đang xử lý, Chờ phản hồi, Đã giải quyết, Đã đóng, Đã từ chối |
  | `ColorClass` | `string` | Not Null, Max 100 | Class chỉ định giao diện badge CSS |
  | `SortOrder` | `int` | Not Null, Default 0 | Thứ tự hiển thị trên bộ lọc |
  | `Description`| `string?` | Nullable, Max 255 | Mô tả ý nghĩa nghiệp vụ trạng thái |
- **Quan hệ**:
  - `Tickets` (`1 - n`): Mỗi trạng thái chứa nhiều Ticket thuộc về nó (`HasForeignKey(t => t.StatusId).OnDelete(DeleteBehavior.Restrict)`).

---

### 1.3. Bảng `Tickets` (Yêu cầu hỗ trợ kỹ thuật)
- **File Entity**: `src/ItTms/Models/Entities/Ticket.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/TicketConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng - `ValueGeneratedOnAdd`)
- **Khóa ngoại (Foreign Keys)**:
  - `CreatorId` -> `Users.Id` (Restrict)
  - `AssignedToId` -> `Users.Id` (SetNull, Nullable)
  - `StatusId` -> `TicketStatuses.Id` (Restrict)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã định danh Ticket |
  | `Title` | `string` | Not Null, Max 150 | Tiêu đề ngắn gọn của sự cố kỹ thuật |
  | `Description` | `string` | Not Null | Mô tả chi tiết vấn đề gặp phải |
  | `Priority` | `TicketPriority` (enum) | Not Null, string conversion, Max 20 | Mức độ ưu tiên: `Low`, `Medium`, `High`, `Critical` |
  | `Category` | `TicketCategory` (enum) | Not Null, string conversion, Max 50 | Phân loại: `Hardware`, `Software`, `Network`, `Account`, `Other` |
  | `CreatorId` | `int` | FK -> `Users(Id)`, Not Null | Người tạo yêu cầu |
  | `AssignedToId`| `int?` | FK -> `Users(Id)`, Nullable | Kỹ thuật viên chịu trách nhiệm xử lý |
  | `StatusId` | `int` | FK -> `TicketStatuses(Id)`, Not Null | Trạng thái vòng đời hiện tại (1..7) |
  | `DueAt` | `DateTime` | Not Null, UTC | Thời hạn cam kết SLA (tự tính theo Priority) |
  | `ResolvedAt` | `DateTime?` | Nullable, UTC | Thời điểm kỹ thuật viên xử lý hoàn tất |
  | `ClosedAt` | `DateTime?` | Nullable, UTC | Thời điểm người dùng nghiệm thu và đóng |
  | `RejectionReason`| `string?`| Nullable, Max 500 | Lý do từ chối (nếu Leader Reject) |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Thời điểm tạo ticket |
  | `UpdatedAt` | `DateTime` | Not Null, UTC | Thời điểm thay đổi trạng thái gần nhất |
  | `ConcurrencyToken`| `Guid` | Not Null, IsConcurrencyToken | Token kiểm soát xung đột đồng thời Optimistic Concurrency |
- **Chỉ mục hiệu năng cao (Covering Indexes)**:
  - `IX_Tickets_CreatorId_CreatedAt`: Tối ưu hóa truy vấn danh sách ticket cá nhân của User (UC05).
  - `IX_Tickets_AssignedToId_StatusId`: Tối ưu hóa hàng đợi công việc của Kỹ thuật viên (UC13).
  - `IX_Tickets_StatusId_DueAt`: Giám sát và cảnh báo vi phạm cam kết SLA (UC17).
  - `IX_Tickets_StatusId_Priority_CreatedAt`: Tra cứu đa tiêu chí cho Trưởng nhóm và Quản trị viên.
- **Quan hệ**:
  - `Comments` (`1 - n`): Danh sách các thảo luận trao đổi trên ticket.
  - `Attachments` (`1 - n`): Các tệp tin đính kèm minh chứng lỗi.
  - `Feedback` (`1 - 0..1`): Đánh giá chất lượng sau khi hoàn thành.

---

### 1.4. Bảng `Comments` (Thảo luận & Ghi chú nội bộ)
- **File Entity**: `src/ItTms/Models/Entities/Comment.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/CommentConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng)
- **Khóa ngoại**:
  - `TicketId` -> `Tickets.Id` (`OnDelete(DeleteBehavior.Cascade)`)
  - `UserId` -> `Users.Id` (`OnDelete(DeleteBehavior.Restrict)`)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã bình luận |
  | `TicketId` | `int` | FK -> `Tickets(Id)`, Not Null | Mã ticket liên quan |
  | `UserId` | `int` | FK -> `Users(Id)`, Not Null | Tác giả bình luận |
  | `Content` | `string` | Not Null | Nội dung trao đổi |
  | `IsInternal` | `bool` | Not Null, Default `false` | Cờ ghi chú nội bộ (chỉ Staff, Leader, Admin thấy) |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Thời điểm gửi |
- **Chỉ mục**: `IX_Comments_TicketId_CreatedAt` (Tối ưu tải luồng hội thoại theo thời gian).

---

### 1.5. Bảng `Feedbacks` (Đánh giá chất lượng hỗ trợ)
- **File Entity**: `src/ItTms/Models/Entities/Feedback.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/FeedbackConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng)
- **Khóa ngoại**: `TicketId` -> `Tickets.Id` (`OnDelete(DeleteBehavior.Cascade)`)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã bản ghi đánh giá |
  | `TicketId` | `int` | FK -> `Tickets(Id)`, Not Null, Unique Index (`IX_Feedbacks_TicketId`) | Quan hệ 1 - 1 với Ticket |
  | `Rating` | `int` | Not Null (khoảng từ 1 đến 5 sao) | Điểm số hài lòng của khách hàng |
  | `Comment` | `string?` | Nullable, Max 1000 | Nhận xét chi tiết của người dùng |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Thời điểm đánh giá và đóng ticket |

---

### 1.6. Bảng `Attachments` (Tệp tin đính kèm)
- **File Entity**: `src/ItTms/Models/Entities/Attachment.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/AttachmentConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng)
- **Khóa ngoại**: `TicketId` -> `Tickets.Id` (`OnDelete(DeleteBehavior.Cascade)`)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã tệp đính kèm |
  | `TicketId` | `int` | FK -> `Tickets(Id)`, Not Null | Ticket sở hữu tệp đính kèm |
  | `FileName` | `string` | Not Null, Max 255 | Tên tệp vật lý lưu trên ổ đĩa (GUID định danh) |
  | `OriginalFileName` | `string` | Not Null, Max 255 | Tên tệp gốc người dùng tải lên |
  | `ContentType` | `string` | Not Null, Max 100 | Định dạng MIME (VD: `image/png`, `application/pdf`) |
  | `FileSize` | `long` | Not Null (tối đa 16MB) | Dung lượng tệp tính bằng byte |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Thời điểm tải lên |
- **Chỉ mục**: `IX_Attachments_TicketId`.

---

### 1.7. Bảng `Notifications` (Thông báo người dùng)
- **File Entity**: `src/ItTms/Models/Entities/Notification.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/NotificationConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng)
- **Khóa ngoại**: `UserId` -> `Users.Id` (`OnDelete(DeleteBehavior.Cascade)`)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã thông báo |
  | `UserId` | `int` | FK -> `Users(Id)`, Not Null | Người nhận thông báo |
  | `Message` | `string` | Not Null, Max 255 | Nội dung thông báo hiển thị |
  | `Link` | `string` | Not Null, Max 255 | Đường dẫn điều hướng khi bấm vào thông báo |
  | `IsRead` | `bool` | Not Null, Default `false` | Trạng thái đã đọc |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Thời điểm phát sinh thông báo |
- **Chỉ mục**: `IX_Notifications_UserId_IsRead_CreatedAt` (Covering Index cho chuông thông báo & huy hiệu badge chưa đọc).

---

### 1.8. Bảng `SystemLogs` (Nhật ký kiểm toán hệ thống)
- **File Entity**: `src/ItTms/Models/Entities/SystemLog.cs`
- **File Cấu hình EF Core**: `src/ItTms/Data/Configurations/SystemLogConfiguration.cs`
- **Khóa chính**: `Id` (int, tự tăng)
- **Khóa ngoại**: `UserId` -> `Users.Id` (`OnDelete(DeleteBehavior.SetNull)`, Nullable)
- **Các trường chính**:
  | Tên trường | Kiểu dữ liệu | Ràng buộc | Mô tả |
  |---|---|---|---|
  | `Id` | `int` | PK, Tự tăng | Mã bản ghi nhật ký |
  | `UserId` | `int?` | FK -> `Users(Id)`, Nullable | Người dùng thực hiện thao tác (null nếu là hệ thống) |
  | `Action` | `string` | Not Null, Max 50 | Tên hành động nghiệp vụ (VD: `CreateTicket`, `Login Success`, `Ticket Resolved`...) |
  | `Details` | `string` | Not Null | Chi tiết nội dung biến động |
  | `IpAddress` | `string?` | Nullable, Max 50 | Địa chỉ IP của máy khách |
  | `CreatedAt` | `DateTime` | Not Null, UTC | Dấu thời gian bất biến (Immutable Timestamp) |
- **Chỉ mục**: `IX_SystemLogs_CreatedAt_Action` (Tối ưu tìm kiếm và kiểm toán cho Admin).

---

## 2. KHẢO SÁT 4 PHÂN HỆ DỌC (VERTICAL SLICES) THEO 4 THÀNH VIÊN

### 2.1. Phân hệ Kỹ thuật viên (Staff - Nguyễn Gia Thụ, MSSV: 25810041)
- **Mã nguồn sở hữu**:
  - Controller: `src/ItTms/Controllers/StaffController.cs` (474 dòng, kế thừa `BaseController`, áp dụng `[AuthorizeRoles(RoleNames.Staff, RoleNames.Leader, RoleNames.Admin)]`)
  - Giao diện Razor Views (`src/ItTms/Views/Staff/`):
    - `Dashboard.cshtml`: Bảng điều khiển KPI cá nhân (vé được giao, đang xử lý, chờ thông tin, đã giải quyết hôm nay, danh sách vé khẩn cấp).
    - `Assigned.cshtml`: Hàng đợi vé được giao, hỗ trợ tab lọc (`active`, `resolved`, `closed`, `all`).
    - `Details.cshtml`: Trang chi tiết xử lý sự cố, khung chat thời gian thực, nút thao tác chuyển trạng thái (`StartProgress`, `HoldForInfo`, `Resolve`), ô ghi chú nội bộ `IsInternal`.
    - `History.cshtml`: Lịch sử các ticket kỹ thuật viên đã hoàn thành kèm điểm đánh giá sao từ khách hàng.
- **Hiện thực Mẫu thiết kế GoF: State Pattern**:
  - Thư mục: `src/ItTms/Models/States/`
  - Hợp đồng giao diện `ITicketState`:
    - `int StatusId { get; }`
    - `string Name { get; }`
    - `string DisplayLabel { get; }`
    - `bool CanComment { get; }`
    - `Result Assign(...)`, `Result Reassign(...)`, `Result StartProgress(...)`, `Result HoldForInfo(...)`, `Result Resolve(...)`, `Result Close(...)`, `Result Reopen(...)`, `Result Reject(...)`.
  - Lớp cơ sở trừu tượng `TicketStateBase`: Cài đặt cơ chế **Fail-Closed**, mọi hành vi mặc định trả về `Result.Failure(TicketErrors.InvalidTransition(Name, action))`.
  - **7 Trạng thái cụ thể trong State Machine**:
    1. `NewState` (StatusId = 1): "Mới tạo". `CanComment = true`. Chỉ cho phép `Assign` (Leader giao việc) hoặc `Reject` (Leader từ chối). Chặn các hành vi khác.
    2. `AssignedState` (StatusId = 2): "Đã phân công". `CanComment = true`. Cho phép `StartProgress` (Kỹ thuật viên bắt đầu xử lý) hoặc `Reassign` (Điều chuyển người khác).
    3. `InProgressState` (StatusId = 3): "Đang xử lý". `CanComment = true`. Cho phép:
       - `HoldForInfo`: Tạm dừng sang `WaitingState`.
       - `Resolve`: Khắc phục xong, chuyển sang `ResolvedState`, ghi nhận `ResolvedAt`.
       - `Reassign`: Điều chuyển sang kỹ thuật viên khác.
       - **Ràng buộc bất biến Bảng 7.2 TC-06**: Chặn đứng tuyệt đối hành vi `Reject`.
    4. `WaitingState` (StatusId = 4): "Chờ phản hồi". `CanComment = true`. Cho phép:
       - `StartProgress`: Tiếp tục xử lý sau khi nhận thông tin.
       - `Resolve`: Hoàn tất xử lý.
       - `Reassign`: Điều chuyển kỹ thuật viên.
    5. `ResolvedState` (StatusId = 5): "Đã giải quyết".
       - **Ràng buộc bất biến Bảng 7.2 TC-08**: `CanComment = false` (Khóa chức năng gửi bình luận).
       - Cho phép User nghiệm thu đóng ticket (`Close` kèm `Feedback` - Bảng 7.2 TC-09).
       - Cho phép User mở lại (`Reopen`) nếu sự cố chưa dứt điểm, đưa ticket quay về `InProgressState`.
    6. `ClosedState` (StatusId = 6): "Đã đóng". `CanComment = false`. **Terminal State** - Mọi phương thức chuyển trạng thái đều trả về `Result.Failure(TicketErrors.TicketAlreadyClosed)`.
    7. `RejectedState` (StatusId = 7): "Đã từ chối". `CanComment = false`. **Terminal State** - Mọi phương thức chuyển trạng thái đều trả về `Result.Failure(TicketErrors.TicketAlreadyRejected)`.
- **Ràng buộc an ninh BOLA / IDOR (Bảng 7.2 TC-07)**:
  - Trong `StaffController.Assigned`, truy vấn bị giới hạn nghiêm ngặt: `query = _dbContext.Tickets.Where(t => t.AssignedToId == staffId)`.
  - Trong `StaffController.Details`, `StartProgress`, `HoldForInfo`, `Resolve`: Kiểm tra `if (ticket.AssignedToId != CurrentUserId.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin)) return Forbid();`. Kỹ thuật viên không thể truy cập hoặc thao tác trên ticket của kỹ thuật viên khác.

---

### 2.2. Phân hệ Trưởng nhóm (Leader - Nguyễn Mạnh Tiến, MSSV: 25810043)
- **Mã nguồn sở hữu**:
  - Controller: `src/ItTms/Controllers/LeaderController.cs` (503 dòng, kế thừa `BaseController`, áp dụng `[AuthorizeRoles(RoleNames.Leader, RoleNames.Admin)]`)
  - Giao diện Razor Views (`src/ItTms/Views/Leader/`):
    - `Dashboard.cshtml`: Trung tâm chỉ huy đội ngũ IT Helpdesk: tỷ lệ tuân thủ cam kết SLA (`SlaComplianceRate`), số vé vi phạm SLA (`BreachedSlaCount`), biểu đồ phân bổ loại sự cố (`CategoryDistribution`), tải công việc từng nhân viên (`StaffWorkloads`).
    - `NewQueue.cshtml`: Hàng đợi yêu cầu mới tạo cần duyệt và phân công, hiển thị danh sách kỹ thuật viên đang rảnh để giao việc.
    - `Index.cshtml`: Tra cứu đa tiêu chí toàn bộ ticket trong hệ thống (theo từ khóa, trạng thái, phân loại, kỹ thuật viên phụ trách).
    - `Details.cshtml`: Xem chi tiết toàn diện ticket, điều chuyển kỹ thuật viên (`Reassign`), từ chối ticket (`Reject`), gửi ghi chú nội bộ (`IsInternal = true`).
- **Hiện thực Mẫu thiết kế GoF: Factory Method / Flyweight Pattern**:
  - File: `src/ItTms/Models/States/TicketStateFactory.cs`
  - Phương thức: `TicketStateFactory.CreateState(Ticket ticket, int statusId)`.
  - Đóng vai trò là bộ khởi tạo và tái sử dụng hành vi trạng thái tương ứng với `StatusId`. Giúp EF Core tái lập chính xác đối tượng trạng thái nghiệp vụ khi load từ CSDL mà không làm lộ các class trạng thái cụ thể ra bên ngoài.
- **Quy trình nghiệp vụ cốt lõi (BPMN Workflow)**:
  - Phân công yêu cầu (`Assign`): Nhận `AssignTicketViewModel`, kiểm tra kỹ thuật viên hợp lệ và đang `Active`, gọi `ticket.Assign(staffId, leaderId)` -> chuyển sang `AssignedState`, kích hoạt `TicketAssignedEvent` (bắn SignalR thông báo cho Staff và User).
  - Từ chối tiếp nhận (`Reject`): Nhận `RejectTicketViewModel`, gọi `ticket.Reject(reason, leaderId)` -> chuyển sang `RejectedState`. Ràng buộc TC-06 được bảo vệ bởi Domain Model: Nếu ticket đang `InProgress`, lệnh Reject bị từ chối trả về `TicketErrors.CannotRejectInProgress`.
  - Điều chuyển (`Reassign`): Giao lại ticket cho kỹ thuật viên khác, kích hoạt sự kiện phân công mới.

---

### 2.3. Phân hệ Người dùng (User - Hồ Thanh Tình, MSSV: 25810044)
- **Mã nguồn sở hữu**:
  - Controller: `src/ItTms/Controllers/TicketController.cs` (510 dòng, kế thừa `BaseController`, áp dụng `[Authorize]`)
  - Giao diện Razor Views (`src/ItTms/Views/Ticket/`):
    - `Dashboard.cshtml`: Bảng điều khiển cá nhân của User: tổng số yêu cầu đã gửi, số yêu cầu đang xử lý, số yêu cầu đã giải quyết chờ đánh giá.
    - `Index.cshtml`: Danh sách yêu cầu cá nhân của User, hỗ trợ tab lọc (`all`, `active`, `resolved`, `closed`, `rejected`).
    - `Create.cshtml`: Biểu mẫu gửi yêu cầu hỗ trợ kỹ thuật, chọn danh mục, độ ưu tiên, đính kèm tệp tin (ảnh/tài liệu tối đa 16MB).
    - `Details.cshtml`: Màn hình theo dõi tiến độ thời gian thực, khung chat SignalR WebSocket hai chiều, form nghiệm thu & đánh giá 5 sao (`CloseAndRate`), nút yêu cầu xử lý lại (`Reopen`).
- **Hiện thực Mẫu thiết kế GoF: Observer Pattern**:
  - Các thành phần cốt lõi:
    - `IDomainEvent` / `IHasDomainEvents`: Hợp đồng sự kiện miền.
    - `IDomainEventDispatcher` / `InMemoryDomainEventDispatcher.cs`: Bộ điều phối 2 pha:
      - **Pha 1 (In-Transaction)**: Kích hoạt `NotificationObserver` và `SystemLogObserver` ghi dữ liệu vào CSDL trước khi commit. Nếu có lỗi, rollback toàn bộ giao dịch.
      - **Pha 2 (Post-Commit)**: Sau khi CSDL commit thành công, kích hoạt `SignalRNotificationDispatcher` đẩy dữ liệu qua WebSocket.
    - `NotificationObserver.cs`:
      - `HandleAsync(TicketCreatedEvent)`: Thông báo cho toàn bộ Leader khi có yêu cầu mới (TC-03).
      - `HandleAsync(TicketAssignedEvent)`: Thông báo cho Staff được giao và User tạo yêu cầu (TC-04).
      - `HandleAsync(TicketStatusChangedEvent)`: Cập nhật tiến độ cho User khi kỹ thuật viên xử lý.
      - `HandleAsync(CommentAddedEvent)`: Bắn thông báo có bình luận mới (bảo mật: không gửi cho User nếu là ghi chú nội bộ).
      - `HandleAsync(TicketClosedEvent)`: Thông báo kết quả đánh giá sao cho Kỹ thuật viên (TC-09).
    - `SystemLogObserver.cs`: Ghi vết kiểm toán bất biến (Immutable Audit Trail) vào bảng `SystemLogs`.
- **Kênh truyền thông thời gian thực SignalR WebSocket (`TicketHub.cs`)**:
  - Endpoint: `/hubs/ticket`
  - Quản lý phòng: `ticket_{id}`, `user_{userId}`, `role_{role}`.
  - Các sự kiện đẩy: `ReceiveComment`, `TicketStatusChanged`, `ReceiveNotification`, `UpdateUnreadNotificationCount`, `ForceLogout`.
  - Phía Client (`ticket-realtime.js`): Bắt sự kiện phím tắt `Enter` (`!e.isComposing`), hiển thị tức thì (Optimistic display), chống trùng lặp `data-comment-id`.

---

### 2.4. Phân hệ Quản trị & Bảo mật (Admin & Auth - Trần Thanh Danh, MSSV: 25810009)
- **Mã nguồn sở hữu**:
  - Controllers:
    - `src/ItTms/Controllers/AdminController.cs` (395 dòng, kế thừa `BaseController`, áp dụng `[AuthorizeRoles(RoleNames.Admin)]`)
    - `src/ItTms/Controllers/AuthController.cs` (218 dòng, kế thừa `BaseController`)
  - Giao diện Razor Views:
    - `src/ItTms/Views/Admin/`: `Dashboard.cshtml`, `Users.cshtml`, `CreateUser.cshtml`, `EditUser.cshtml`, `Roles.cshtml`, `Audit.cshtml`.
    - `src/ItTms/Views/Auth/`: `Login.cshtml`, `ChangePassword.cshtml`.
    - `src/ItTms/Views/Shared/`: `AccessDenied.cshtml`, `_ToastAlerts.cshtml`, `_Layout.cshtml`.
- **Hiện thực Mẫu thiết kế GoF: Decorator Pattern / ActionFilter (`AuthorizeRolesAttribute`)**:
  - File: `src/ItTms/Filters/AuthorizeRolesAttribute.cs`
  - Kế thừa: `Attribute`, hiện thực `IAsyncActionFilter`.
  - Hoạt động theo mô hình Decorator bọc quanh các Action / Controller:
    1. Kiểm tra `[AllowAnonymous]` để bỏ qua.
    2. Kiểm tra Authentication: Nếu chưa đăng nhập, chuyển hướng về `Login` (hoặc trả 401 nếu là AJAX).
    3. **Kiểm tra trạng thái Inactive thời gian thực (Bảng 7.2 TC-01)**: Truy vấn trực tiếp CSDL SQLite. Nếu `Status != UserStatus.Active`, lập tức thu hồi phiên làm việc (`SignOutAsync`), chặn truy cập và thông báo tài khoản bị vô hiệu hóa.
    4. **Kiểm tra phân quyền RBAC (Bảng 7.2 TC-02)**: Đối chiếu vai trò người dùng với `_allowedRoles`. Nếu không khớp, trả về **HTTP 403 Forbidden** và hiển thị view `AccessDenied.cshtml`.
- **Hiện thực Mẫu thiết kế GoF: Singleton Pattern (`PasswordHasherService`)**:
  - File: `src/ItTms/Services/PasswordHasherService.cs`
  - Đăng ký DI: `builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();` trong `Program.cs` (dòng 43).
  - Cung cấp cơ chế băm và xác thực mật khẩu an toàn theo thuật toán PBKDF2 HMAC-SHA512.
- **Nghiệp vụ bảo mật và quản trị cốt lõi**:
  - Đăng nhập / Đăng xuất: Cookie Authentication (thời hạn 8 giờ, sliding expiration, SameSite Lax, HttpOnly).
  - Quản trị tài khoản (UC01): Thêm mới, phân vai trò, đặt lại mật khẩu.
  - **Bật/tắt kích hoạt tài khoản & Chặn tự vô hiệu hóa (Bảng 7.2 TC-10)**:
    - Trong `AdminController.ToggleActive`: `if (id == CurrentUserId.Value) { TempData["ErrorMessage"] = "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình."; return RedirectToAction(nameof(Users)); }`.
    - Trong Domain Model `User.Deactivate`: `if (Id == operatingAdminId) return Result.Failure(new Error("ADMIN_CANNOT_DEACTIVATE_SELF", ...));`.
    - Khi Admin vô hiệu hóa tài khoản khác, kích hoạt `UserDeactivatedEvent` đẩy SignalR `ForceLogout` ép đăng xuất ngay lập tức từ xa.
  - Nhật ký kiểm toán hệ thống (`Audit`): Xem và tra cứu 100 bản ghi nhật ký mới nhất theo người dùng, loại thao tác và mốc thời gian.

---

## 3. BẢNG TỔNG HỢP TOÀN BỘ CONTROLLERS, ACTIONS, ROUTES, METHODS & PERMISSIONS

### 3.1. `AuthController` (`src/ItTms/Controllers/AuthController.cs`)
| Action | HTTP Method | Route | Tham số chính | Quyền hạn | Mô tả chức năng |
|---|---|---|---|---|---|
| `Login` | `GET` | `/Auth/Login`, `/Auth/Auth/Login` | `returnUrl` | `[AllowAnonymous]` | Hiển thị form đăng nhập |
| `Login` | `POST` | `/Auth/Login`, `/Auth/Auth/Login` | `LoginViewModel model` | `[AllowAnonymous]` | Xác thực thông tin, kiểm tra Inactive (TC-01), cấp Cookie |
| `Logout` | `GET`, `POST` | `/Auth/Logout`, `/Auth/Auth/Logout` | - | `[AllowAnonymous]` | Hủy phiên làm việc Cookie, ghi SystemLog |
| `ChangePassword` | `GET` | `/Auth/ChangePassword` | - | `[Authorize]` | Hiển thị form đổi mật khẩu cá nhân |
| `ChangePassword` | `POST` | `/Auth/ChangePassword` | `ChangePasswordViewModel model` | `[Authorize]` | Kiểm tra mật khẩu cũ, băm mật khẩu mới |
| `AccessDenied` | `GET` | `/Auth/AccessDenied` | - | `[AllowAnonymous]` | Trả về HTTP 403 và hiển thị trang AccessDenied |

---

### 3.2. `TicketController` (`src/ItTms/Controllers/TicketController.cs`)
| Action | HTTP Method | Route | Tham số chính | Quyền hạn | Mô tả chức năng |
|---|---|---|---|---|---|
| `Dashboard` | `GET` | `/Ticket/Dashboard`, `/User/Dashboard` | - | `[Authorize]` | Xem dashboard chỉ số cá nhân của người dùng |
| `Index` | `GET` | `/Ticket`, `/Ticket/Index`, `/User/Ticket` | `string tab = "all"` | `[Authorize]` | Danh sách ticket cá nhân theo tab trạng thái |
| `Create` | `GET` | `/Ticket/Create`, `/User/Ticket/Create` | - | `[Authorize]` | Hiển thị form tạo ticket mới |
| `Create` | `POST` | `/Ticket/Create`, `/User/Ticket/Create` | `CreateTicketViewModel model` | `[Authorize]` | Tạo ticket New, tính SLA DueAt, lưu file đính kèm, bắn event |
| `Details` | `GET` | `/Ticket/Details/{id}`, `/User/Ticket/Details/{id}`| `int id` | `[Authorize]` | Chi tiết ticket, bảo vệ BOLA, lọc bỏ comment nội bộ |
| `AddComment` | `POST` | `/Ticket/AddComment` | `int id, string content` | `[Authorize]` | Thêm bình luận, chặn nếu Resolved/Closed (TC-08), phát SignalR |
| `CloseAndRate`| `POST` | `/Ticket/CloseAndRate` | `CloseAndRateViewModel model` | `[Authorize]` | Đóng và đánh giá ticket Resolved (TC-09) |
| `Reopen` | `POST` | `/Ticket/Reopen` | `int id, string reason` | `[Authorize]` | Mở lại ticket từ Resolved về InProgress |
| `DownloadAttachment` | `GET` | `/Ticket/DownloadAttachment` | `int id` | `[Authorize]` | Tải tệp đính kèm với cơ chế phân quyền bảo mật |

---

### 3.3. `StaffController` (`src/ItTms/Controllers/StaffController.cs`)
| Action | HTTP Method | Route | Tham số chính | Quyền hạn | Mô tả chức năng |
|---|---|---|---|---|---|
| `Dashboard` | `GET` | `/Staff/Dashboard`, `/Staff` | - | Staff, Leader, Admin | Dashboard kỹ thuật viên: KPI cá nhân, SLA khẩn cấp |
| `Assigned` | `GET` | `/Staff/Assigned`, `/Staff/StaffTicket/Assigned` | `string tab = "active"` | Staff, Leader, Admin | Hàng đợi vé phân công cho mình (TC-07 BOLA check) |
| `Details` | `GET` | `/Staff/Details/{id}`, `/Staff/StaffTicket/Details/{id}`| `int id` | Staff, Leader, Admin | Chi tiết ticket, kiểm tra quyền sở hữu vé |
| `StartProgress` | `POST` | `/Staff/StartProgress` | `int id` | Staff, Leader, Admin | Chuyển trạng thái sang InProgress, ghi SystemLog |
| `HoldForInfo` | `POST` | `/Staff/HoldForInfo` | `int id, string reason` | Staff, Leader, Admin | Chuyển trạng thái sang Waiting (chờ phản hồi) |
| `Resolve` | `POST` | `/Staff/Resolve` | `int id` | Staff, Leader, Admin | Đánh dấu sự cố đã khắc phục thành công (Resolved) |
| `AddComment` | `POST` | `/Staff/AddComment` | `int id, string content, bool isInternal` | Staff, Leader, Admin | Bình luận hoặc lưu ghi chú nội bộ (IsInternal) |
| `History` | `GET` | `/Staff/History`, `/Staff/StaffTicket/History` | - | Staff, Leader, Admin | Xem lại các ticket đã hoàn tất cùng điểm đánh giá |

---

### 3.4. `LeaderController` (`src/ItTms/Controllers/LeaderController.cs`)
| Action | HTTP Method | Route | Tham số chính | Quyền hạn | Mô tả chức năng |
|---|---|---|---|---|---|
| `Dashboard` | `GET` | `/Leader/Dashboard`, `/Leader` | - | Leader, Admin | Bảng chỉ huy IT: SLA rate, tải nhân viên, phân loại lỗi |
| `NewQueue` | `GET` | `/Leader/NewQueue`, `/Leader/LeaderTicket/NewQueue` | - | Leader, Admin | Hàng đợi tiếp nhận ticket New, tải danh sách Staff |
| `Assign` | `POST` | `/Leader/Assign` | `AssignTicketViewModel model` | Leader, Admin | Phân công ticket New cho Staff, phát event (TC-04) |
| `Reject` | `POST` | `/Leader/Reject` | `RejectTicketViewModel model` | Leader, Admin | Từ chối ticket New; chặn từ chối nếu InProgress (TC-06) |
| `Reassign` | `POST` | `/Leader/Reassign` | `ReassignTicketViewModel model`| Leader, Admin | Điều chuyển ticket sang kỹ thuật viên khác |
| `Index` | `GET` | `/Leader/Index` | `keyword, statusId, category, staffId` | Leader, Admin | Tra cứu và lọc đa tiêu chí toàn bộ ticket |
| `Details` | `GET` | `/Leader/Details/{id}`, `/Leader/LeaderTicket/Details/{id}`| `int id` | Leader, Admin | Xem chi tiết, điều chuyển hoặc từ chối tại chỗ |
| `AddComment` | `POST` | `/Leader/AddComment` | `int id, string content, bool isInternal` | Leader, Admin | Trưởng nhóm bình luận hoặc ghi chú nội bộ |

---

### 3.5. `AdminController` (`src/ItTms/Controllers/AdminController.cs`)
| Action | HTTP Method | Route | Tham số chính | Quyền hạn | Mô tả chức năng |
|---|---|---|---|---|---|
| `Dashboard` | `GET` | `/Admin/Dashboard`, `/Admin` | - | Admin | Tổng quan hệ thống: số lượng user, ticket, log kiểm toán |
| `Users` | `GET` | `/Admin/Users`, `/Admin/User` | `search, role, status` | Admin | Quản lý danh sách người dùng, sắp xếp theo ID tăng dần |
| `CreateUser` | `GET` | `/Admin/CreateUser`, `/Admin/User/Create` | - | Admin | Hiển thị form tạo tài khoản mới |
| `CreateUser` | `POST` | `/Admin/CreateUser`, `/Admin/User/Create` | `CreateUserViewModel model` | Admin | Khởi tạo tài khoản mới, băm mật khẩu, ghi nhật ký |
| `EditUser` | `GET` | `/Admin/EditUser` | `int id` | Admin | Hiển thị form chỉnh sửa thông tin người dùng |
| `EditUser` | `POST` | `/Admin/EditUser` | `EditUserViewModel model` | Admin | Cập nhật họ tên, phòng ban, vai trò của người dùng |
| `ToggleActive` | `POST` | `/Admin/ToggleActive` | `int id` | Admin | Bật/tắt trạng thái tài khoản; CHẶN tự khóa mình (TC-10) |
| `ResetPassword`| `POST` | `/Admin/ResetPassword` | `ResetPasswordViewModel model`| Admin | Đặt lại mật khẩu mới cho người dùng |
| `Roles` | `GET` | `/Admin/Roles`, `/Admin/Role` | - | Admin | Thống kê số lượng người dùng theo từng vai trò |
| `Audit` | `GET` | `/Admin/Audit`, `/Admin/AuditLog` | `actionType, userId, fromDate, toDate` | Admin | Tra cứu và phân tích nhật ký kiểm toán hệ thống |

---

### 3.6. `HomeController` (`src/ItTms/Controllers/HomeController.cs`)
| Action | HTTP Method | Route | Quyền hạn | Mô tả chức năng |
|---|---|---|---|---|
| `Index` | `GET` | `/` | Anonymous / Authenticated | Tự động điều hướng về Dashboard đúng vai trò sau đăng nhập |
| `Error` | `GET` | `/Home/Error` | Anonymous | Hiển thị trang báo lỗi hệ thống |

---

## 4. BẢNG TỔNG HỢP CÁC SỰ KIỆN SIGNALR & WEBSOCKET

Hub Endpoint: `/hubs/ticket` (`src/ItTms/Hubs/TicketHub.cs`)  
Bộ phân phối: `SignalRNotificationDispatcher` (`src/ItTms/Services/SignalRNotificationDispatcher.cs`)

### 4.1. Sự kiện từ Client gọi lên Server (Hub Invocations)
| Tên phương thức | Tham số | Nhóm / Phạm vi | Mô tả |
|---|---|---|---|
| `JoinTicketGroup` | `int ticketId` | Ghi danh client vào phòng `ticket_{id}` (và `ticket_{id}_internal` nếu là Staff/Leader/Admin) | Tham gia trao đổi ticket, có kiểm tra BOLA/IDOR |
| `LeaveTicketGroup`| `int ticketId` | Rời khỏi phòng `ticket_{id}` | Gọi khi rời trang chi tiết ticket |
| `SendComment` | `int ticketId, string message, bool isInternal` | Gửi bình luận qua WebSocket | Kiểm tra quyền CanComment, lưu DB và broadcast tức thì |

### 4.2. Sự kiện từ Server đẩy xuống Client (Server Broadcasts)
| Tên sự kiện | Payload DTO | Nhóm nhận (Target Group) | Mục đích nghiệp vụ |
|---|---|---|---|
| `ReceiveComment` | `CommentDto` (`Id`, `TicketId`, `AuthorUserId`, `AuthorName`, `AuthorRole`, `Content`, `IsInternal`, `CreatedAt`) | `ticket_{id}` hoặc `ticket_{id}_internal` | Đẩy tin nhắn hội thoại tức thì mà không cần reload trang |
| `TicketStatusChanged` | `TicketStatusChangedDto` (`TicketId`, `OldStatusId`, `NewStatusId`, `NewStatusName`, `NewStatusLabel`, `ColorClass`, `CanComment`, `UpdatedAt`) | `ticket_{id}` | Cập nhật nhãn trạng thái và tự động khóa ô chat nếu vé chuyển sang Resolved/Closed/Rejected |
| `ReceiveNotification` | `NotificationDto` (`Id`, `UserId`, `Message`, `Link`, `IsRead`, `CreatedAt`) | `user_{userId}` hoặc `role_{roleName}` | Đẩy Floating Card Box thông báo ở góc trên bên phải màn hình |
| `UpdateUnreadNotificationCount` | `int unreadCount` | `user_{userId}` | Cập nhật số lượng thông báo chưa đọc trên biểu tượng chuông |
| `ForceLogout` | `string reason` | `user_{userId}` | Cưỡng chế đăng xuất ngay lập tức khi tài khoản bị Admin vô hiệu hóa |

---

## 5. BẢNG TỔNG HỢP CÁC DỊCH VỤ HỆ THỐNG (SERVICES & DI REGISTRATIONS)

| Tên Interface / Service | Kiểu vòng đời (Lifetime) | File triển khai | Mục đích chính trong hệ thống |
|---|---|---|---|
| `IItTmsDbContext` / `ItTmsDbContext` | Scoped | `src/ItTms/Data/ItTmsDbContext.cs` | Kết nối CSDL SQLite WAL mode, cấu hình EF Core 9, quản lý 8 bảng |
| `ConcurrencyTokenInterceptor` | Singleton | `src/ItTms/Data/Interceptors/ConcurrencyTokenInterceptor.cs` | Tự động cập nhật ConcurrencyToken phục vụ Optimistic Concurrency |
| `ICurrentUserService` / `CurrentUserService` | Scoped | `src/ItTms/Services/CurrentUserService.cs` | Trích xuất thông tin người dùng hiện tại từ ClaimsPrincipal |
| `IPasswordHasherService` / `PasswordHasherService` | **Singleton** (GoF Singleton Pattern) | `src/ItTms/Services/PasswordHasherService.cs` | Mã hóa và xác thực mật khẩu an toàn theo chuẩn PBKDF2 HMAC-SHA512 |
| `IDataSeeder` / `DataSeeder` | Scoped | `src/ItTms/Data/DataSeeder.cs` | Tự động nạp 7 trạng thái, 8 tài khoản kiểm thử và 3 ticket demo khi khởi động |
| `IDomainEventDispatcher` / `InMemoryDomainEventDispatcher` | Scoped | `src/ItTms/Services/InMemoryDomainEventDispatcher.cs` | Điều phối Domain Events 2 pha (In-Transaction & Post-Commit) |
| `INotificationDispatcher` / `SignalRNotificationDispatcher` | Scoped | `src/ItTms/Services/SignalRNotificationDispatcher.cs` | Đẩy thông báo thời gian thực và sự kiện miền qua SignalR WebSocket |
| `NotificationObserver` | Scoped (GoF Observer Pattern) | `src/ItTms/Services/Observers/NotificationObserver.cs` | Lắng nghe Domain Events để tạo bản ghi thông báo người dùng |
| `SystemLogObserver` | Scoped (GoF Observer Pattern) | `src/ItTms/Services/Observers/SystemLogObserver.cs` | Lắng nghe Domain Events để ghi vết kiểm toán bất biến vào SystemLogs |
| `AuthorizeRolesAttribute` | Action Filter (GoF Decorator Pattern) | `src/ItTms/Filters/AuthorizeRolesAttribute.cs` | Bộ lọc kiểm soát phân quyền RBAC và kiểm tra trạng thái kích hoạt thời gian thực |
| `AuthorizeTicketAccessAttribute` | Action Filter | `src/ItTms/Filters/AuthorizeTicketAccessAttribute.cs` | Bộ lọc bảo vệ truy cập ticket theo tài nguyên (Resource-Based Authorization) |

---

## 6. ĐỐI SOÁT 10 KỊCH BẢN NGHIỆM THU CỐT LÕI (BẢNG 7.2)

Hệ thống được xác minh tự động thông qua bộ kiểm thử E2E xUnit tại `tests/ItTms.Tests/E2EVerificationTests/Table72VerificationTests.cs`. Kết quả chạy thực tế: **10/10 PASS**.

| Mã TC | Kịch bản kiểm thử | Hành vi kỳ vọng | Vị trí hiện thực trong Codebase | Kết quả kiểm thử |
|---|---|---|---|---|
| **TC-01** | Đăng nhập tài khoản bị vô hiệu hóa (Inactive) | Bị từ chối đăng nhập với thông báo lỗi rõ ràng; nếu có phiên đăng nhập cũ, bị thu hồi ngay lập tức. | `AuthController.cs:57`, `AuthorizeRolesAttribute.cs:68` | **PASS** |
| **TC-02** | Truy cập đường dẫn vượt thẩm quyền vai trò | Trả về mã lỗi HTTP 403 Forbidden và hiển thị trang AccessDenied. | `AuthorizeRolesAttribute.cs:99` | **PASS** |
| **TC-03** | Khởi tạo ticket mới | Ticket được khởi tạo ở trạng thái New, tính hạn cam kết SLA DueAt và tự động thông báo cho Leader. | `Ticket.cs:121`, `TicketController.cs:157`, `NotificationObserver.cs:31` | **PASS** |
| **TC-04** | Trưởng nhóm phân công ticket | Trạng thái chuyển sang Assigned, kích hoạt thông báo tức thì cho cả Staff được giao và User tạo yêu cầu. | `NewState.cs:28`, `LeaderController.cs:173`, `NotificationObserver.cs:63` | **PASS** |
| **TC-05** | Chuyển đổi trạng thái không hợp lệ (Staff đóng vé New) | Bị chặn đứng ngay tại Domain Model và trả về lỗi đường sắt `Result.Failure`. | `NewState.cs` (kế thừa `TicketStateBase:58`), `TicketErrors.cs:22` | **PASS** |
| **TC-06** | Trưởng nhóm từ chối vé đang xử lý (InProgress) | Bị chặn đứng với lỗi `CannotRejectInProgress`, bảo toàn tính toàn vẹn trạng thái. | `InProgressState.cs:76`, `LeaderController.cs:205` | **PASS** |
| **TC-07** | Kỹ thuật viên truy cập vé không được phân công cho mình | Bị chặn đứng bởi bộ lọc an ninh BOLA / IDOR, trả về HTTP 403 Forbidden. | `StaffController.cs:102, 171`, `TicketHub.cs:85` | **PASS** |
| **TC-08** | Gửi bình luận trên ticket đã giải quyết / đóng | Bị chặn đứng ngay lập tức do `CanComment = false` tại Resolved, Closed, Rejected. | `ResolvedState.cs:21`, `TicketController.cs:325`, `TicketHub.cs:152` | **PASS** |
| **TC-09** | Người dùng đóng và đánh giá vé đã giải quyết (Resolved) | Chuyển sang Closed, lưu đánh giá sao vào Feedbacks, ghi SystemLog và thông báo cho Staff. | `ResolvedState.cs:23`, `TicketController.cs:418`, `NotificationObserver.cs:187` | **PASS** |
| **TC-10** | Quản trị viên kích hoạt / vô hiệu hóa tài khoản | Cho phép bật/tắt tài khoản khác; CHẶN TUYỆT ĐỐI hành vi tự vô hiệu hóa chính mình. | `AdminController.cs:230`, `User.cs:135` | **PASS** |

---

## 7. KẾT LUẬN VÀ KIẾN NGHỊ CHO CÔNG TÁC LẬP BÁO CÁO WORD
1. **Tính nhất quán kiến trúc**: Hệ thống đã được tổ chức chuẩn mực theo mô hình 4 phân hệ độc lập tương ứng với 4 thành viên trong nhóm, mã nguồn hoàn toàn tách biệt theo từng Controller và thư mục View riêng, không hề có sự chồng chéo.
2. **Khớp nối 100% tài liệu**: Tất cả các tên lớp, phương thức, thuộc tính, sự kiện SignalR, bảng CSDL và mã kiểm thử trong báo cáo kiểm kê này đều là các thực thể đang hoạt động thực tế trên máy, sẵn sàng đưa vào các hình vẽ UML (ERD, BPMN, State Machine, Class Diagram, Sequence Diagrams) và các bảng phân công công việc của báo cáo Word mà không sợ sai lệch.
