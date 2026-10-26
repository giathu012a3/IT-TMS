# 📋 KHO ĐẶC TẢ YÊU CẦU TOÀN DIỆN HỆ THỐNG IT-TMS (.NET 9)
> **Tài liệu đặc tả chuẩn xác (Authoritative Specification Inventory)**  
> **Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Phiên bản kiến trúc**: .NET 9 Enterprise Clean Architecture + SignalR WebSocket  
> **Người khai thác**: `spec_miner_survey_1` | **Ngày lập**: 2026-10-25  

---

## MỤC LỤC
1. [Tổng quan & Nguyên tắc Bất biến (#Argon-Omega)](#1-tổng-quan--nguyên-tắc-bất-biến-argon-omega)
2. [Danh mục 20 Use Cases trên 5 Areas & Ánh xạ Actor](#2-danh-mục-20-use-cases-trên-5-areas--ánh-xạ-actor)
3. [Mô hình Dữ liệu 8 Thực thể & Đặc tả EF Core 9 (SQLite)](#3-mô-hình-dữ-liệu-8-thực-thể--đặc-tả-ef-core-9-sqlite)
4. [Đặc tả 4 Mẫu Thiết kế Phần mềm (Design Patterns)](#4-đặc-tả-4-mẫu-thiết-kế-phần-mềm-design-patterns)
5. [Đặc tả Kiến trúc Real-time SignalR WebSocket Hub (`TicketHub`)](#5-đặc-tả-kiến-trúc-real-time-signalr-websocket-hub-tickethub)
6. [Đặc tả Chi tiết 10 Kịch bản Kiểm thử Nghiệm thu (Bảng 7.2)](#6-đặc-tả-chi-tiết-10-kịch-bản-kiểm-thử-nghiệm-thu-bảng-72)
7. [Yêu cầu Đồng bộ Tài liệu Báo cáo Word (`BaoCao_ITTMS_TKPMHDT (4).docx`)](#7-yêu-cầu-đồng-bộ-tài-liệu-báo-cáo-word-baocao_ittms_tkpmhdt-4docx)
8. [Bảng Tổng hợp Tính năng Được Khám phá (Features Discovered)](#8-bảng-tổng-hợp-tính-năng-được-khám-phá-features-discovered)
9. [Bảng Ma trận Trường hợp Biên & Ngoại lệ (Edge Cases)](#9-bảng-ma-trận-trường-hợp-biên--ngoại-lệ-edge-cases)

---

## 1. TỔNG QUAN & NGUYÊN TẮC BẤT BIẾN (#ARGON-OMEGA)

Hệ thống **IT Ticket Management System (IT-TMS)** được xây dựng nhằm chuẩn hóa toàn diện quy trình tiếp nhận, phân công, điều phối, xử lý, trao đổi thông tin, nghiệm thu và đánh giá các yêu cầu hỗ trợ kỹ thuật nội bộ trong doanh nghiệp.

### Các Nguyên tắc Bất biến (Hard Invariants)
1. **Tiêu diệt triệt để Anti-Pattern Polling 3 giây (`#026 Bottleneck`)**:
   - Khung chat/trao đổi và chuông thông báo hoạt động trên **ASP.NET Core SignalR WebSocket** hai chiều. Tuyệt đối không dùng client `setInterval` 3s gây lãng phí CPU, băng thông mạng và thắt cổ chai kết nối SQLite/Database.
2. **Xử lý Luồng Nghiệp vụ Đường sắt (Railway-Oriented `Result<T>`)**:
   - Không ném Exception (`throw`) đối với các vi phạm nghiệp vụ thông thường (sai chuyển trạng thái, không đủ quyền, validate dữ liệu). Toàn bộ thao tác trả về đối tượng bất biến `Result<T>` và `Error`. Exception chỉ dành riêng cho sự cố hạ tầng không lường trước.
3. **Bao đóng Nghiệp vụ Vòng đời Ticket bằng State Pattern (`#001 First-Principles`)**:
   - Vòng đời Ticket trải qua 7 trạng thái được điều phối bởi 7 lớp State cụ thể. Mọi nỗ lực chuyển trạng thái trái phép bị chặn đứng tại tầng Domain Model.
4. **An toàn Đồng thời Lạc quan & Bất biến Dữ liệu (`#035 Idempotency & Concurrency`)**:
   - Thực thể `Ticket` tích hợp Concurrency Token (`RowVersion`/`byte[]` hoặc Timestamp Token) nhằm phát hiện và ngăn chặn xung đột Lost Updates khi hai kỹ thuật viên cùng thao tác trên một ticket.
5. **Bảo toàn 100% Cấu trúc Mục lục Báo cáo Word**:
   - Quá trình đồng bộ hóa tài liệu báo cáo `BaoCao_ITTMS_TKPMHDT (4).docx` phải bảo toàn nguyên vẹn thứ tự, cấp độ tiêu đề (từ Phần Mở đầu, Chương 1 đến Chương 7, Kết luận, Phụ lục). Chỉ chuyển đổi nội dung công nghệ, bảng biểu CSDL và mã nguồn từ Python/Flask sang .NET 9 / C#.
6. **Kiến trúc Phân tầng Chuẩn mực (.NET 9 Clean Architecture + Vertical Slices)**:
   - Tách biệt rõ ràng: `ItTms.Domain` (Core nghiệp vụ, zero dependency), `ItTms.Application` (Feature Slices, interfaces, DTOs), `ItTms.Infrastructure` (EF Core 9, Auth, Notifications, Persistence), `ItTms.Web` (ASP.NET Core MVC 5 Areas, SignalR Hub, Bootstrap 5 UI), `ItTms.UnitTests` (xUnit, FluentAssertions).

---

## 2. DANH MỤC 20 USE CASES TRÊN 5 AREAS & ÁNH XẠ ACTOR

Hệ thống có 4 vai trò chính đã xác thực (**User**, **Staff**, **Leader**, **Admin**) và 1 vai trò chưa xác thực (**Khách / Guest**). Toàn bộ 20 Use Cases được tổ chức khoa học trong 5 Areas của ASP.NET Core MVC:
- `Areas/Auth`: Xác thực, thoát phiên, đổi mật khẩu.
- `Areas/User`: Cổng làm việc của người dùng yêu cầu hỗ trợ.
- `Areas/Staff`: Cổng làm việc của nhân viên kỹ thuật.
- `Areas/Leader`: Cổng điều phối, phân công, giám sát của trưởng nhóm.
- `Areas/Admin`: Cổng quản trị hệ thống, người dùng và nhật ký.
- *(Dịch vụ Dùng chung - Cross-cutting/Shared: Chi tiết ticket, Khung trao đổi SignalR, Dashboard phân quyền)*.

### 2.1. Bảng Tổng hợp Ánh xạ 20 Use Cases

| Mã UC | Tên Use Case | Tên Tiếng Anh | Actor Chính | MVC Area / Route | FR Liên Quan | Mục tiêu Nghiệp vụ |
|:---:|---|---|---|---|:---:|---|
| **UC01** | Đăng nhập | Login | Khách (Guest) | `Auth/Account/Login` | FR-01, FR-04 | Xác thực danh tính, kiểm tra tài khoản Active, cấp Cookie, chuyển về Dashboard theo vai trò. |
| **UC02** | Xem danh sách yêu cầu cá nhân | View My Tickets | User | `User/Ticket/Index` | FR-07 | Hiển thị danh sách ticket do chính User tạo, hỗ trợ lọc theo tab trạng thái và phân trang. |
| **UC03** | Tạo yêu cầu mới | Create New Ticket | User | `User/Ticket/Create` | FR-05, FR-06 | Nhập thông tin sự cố, chọn ưu tiên, tải file đính kèm (max 16MB), hệ thống tính SLA DueAt và khởi tạo trạng thái `New`. |
| **UC04** | Đánh giá & Feedback | Rate and Feedback | User | `User/Ticket/Feedback` | FR-11 | Gửi đánh giá 1-5 sao và nhận xét cho ticket `Resolved`, hoàn tất chuyển sang `Closed`. |
| **UC05** | Đăng xuất | Logout | All Roles | `Auth/Account/Logout` | FR-02 | Hủy Authentication Cookie, xóa phiên làm việc, chuyển hướng về màn hình Đăng nhập. |
| **UC06** | Trao đổi thông tin ticket | Ticket Discussion | All Roles | `Shared/TicketHub`, `Ticket/Comment` | FR-09, FR-13, FR-20 | Bình luận thời gian thực qua SignalR; hỗ trợ checkbox "Ghi chú nội bộ" cho Staff/Leader; tự chuyển ticket `Waiting` về `InProgress` khi User gửi phản hồi. |
| **UC07** | Cập nhật trạng thái xử lý | Update Ticket Status | Staff, Leader | `Staff/Ticket/UpdateStatus` | FR-16 | Staff cập nhật tiến độ (`InProgress`, `Waiting`, `Resolved`) tuân thủ nghiêm ngặt State Pattern. |
| **UC08** | Xem chi tiết yêu cầu | View Ticket Detail | All Roles | `Shared/Ticket/Details` | FR-08, FR-15, FR-21 | Hiển thị 3 khối: Nội dung + file, Thông tin SLA/kỹ thuật viên, Khung chat SignalR; kiểm tra quyền truy cập (BOLA). |
| **UC09** | Phân công yêu cầu | Assign Ticket | Leader | `Leader/Ticket/Assign` | FR-22 | Leader giao ticket `New` cho Staff; hỗ trợ tự xử lý ("Tự xử lý") và phân công lại (Reassign) khi Staff nghỉ/quá tải. |
| **UC10** | Từ chối yêu cầu | Reject Ticket | Leader | `Leader/Ticket/Reject` | FR-23 | Leader từ chối ticket `New` không hợp lệ, bắt buộc nhập lý do từ chối, chuyển sang `Rejected`. |
| **UC11** | Xem tổng quan hoạt động | View Management Dashboard | Leader | `Leader/Dashboard/Index` | FR-25 | Dashboard lãnh đạo: 4 thẻ thống kê ▲▼, cảnh báo vi phạm SLA, biểu đồ năng suất Staff, biểu đồ danh mục, bộ lọc thời gian. |
| **UC12** | Xem & tra cứu tất cả yêu cầu | View All Tickets | Leader, Admin | `Leader/Ticket/All`, `Admin/Ticket/All` | FR-21, FR-32 | Tra cứu toàn bộ ticket trong hệ thống, tìm kiếm từ khóa, lọc đa tiêu chí, phân trang keyset. |
| **UC13** | Quản lý người dùng & phân quyền | User Management | Admin | `Admin/User/Index` | FR-26, FR-31 | Xem danh sách tài khoản, vai trò, trạng thái, phòng ban; liên kết các thao tác quản trị. |
| **UC14** | Tạo tài khoản người dùng mới | Create New User | Admin | `Admin/User/Create` | FR-27 | Admin tạo người dùng mới, băm mật khẩu, gán vai trò, mặc định trạng thái `Active`. |
| **UC15** | Kích hoạt / Vô hiệu hóa tài khoản | Toggle User Status | Admin | `Admin/User/ToggleStatus` | FR-30 | Bật/tắt trạng thái Active ↔ Inactive; chặn Admin tự vô hiệu hóa chính mình; ngắt quyền tài khoản Inactive ngay lập tức. |
| **UC16** | Xem lịch sử xử lý | View Processing History | Staff | `Staff/Ticket/History` | FR-18, FR-19 | Staff tra cứu các ticket đã hoàn tất (`Resolved`, `Closed`) mà mình từng xử lý. |
| **UC17** | Đổi mật khẩu cá nhân | Change Own Password | All Roles | `Auth/Account/ChangePassword` | FR-34 | Người dùng tự đổi mật khẩu, bắt buộc xác thực đúng mật khẩu cũ. |
| **UC18** | Đặt lại mật khẩu người dùng | Admin Reset Password | Admin | `Admin/User/ResetPassword` | FR-36 | Admin cưỡng chế đổi mật khẩu mới cho người dùng quên mật khẩu hoặc có sự cố bảo mật. |
| **UC19** | Đóng / Mở lại yêu cầu | Close or Reopen Ticket | User | `User/Ticket/Close`, `User/Ticket/Reopen` | FR-11, FR-12 | Tại trạng thái `Resolved`: User xác nhận đóng (chuyển sang `Closed` kèm đánh giá) hoặc yêu cầu xử lý lại (chuyển về `InProgress`). |
| **UC20** | Xem Dashboard cá nhân | View Personal Dashboard | All Roles | `[Area]/Dashboard/Index` | FR-35 | Điều hướng tự động sau đăng nhập tới Dashboard chuyên biệt theo vai trò của người dùng. |

---

### 2.2. Đặc tả Chi tiết Từng Use Case

#### UC01 – Đăng nhập (Login)
- **Actor**: Khách (Guest)
- **Tiền điều kiện**: Người dùng chưa đăng nhập, đang ở trang `/Auth/Account/Login`.
- **Hậu điều kiện**: Thiết lập phiên đăng nhập (Cookie), chuyển hướng tới Dashboard theo vai trò; nếu thất bại giữ nguyên màn hình và báo lỗi.
- **Luồng chính**:
  1. Người dùng nhập Username và Password.
  2. Bấm "Đăng nhập".
  3. Hệ thống truy vấn `User` theo Username.
  4. Hệ thống kiểm tra mật khẩu qua `IPasswordHasher<User>`.
  5. Hệ thống kiểm tra `Status == "Active"`.
  6. Tạo các Claims (`NameIdentifier`, `Name`, `Role`, `Department`), thiết lập Authentication Cookie.
  7. Ghi nhận `SystemLog` hành động "Login Success".
  8. Chuyển hướng tới Dashboard tương ứng với vai trò.
- **Ngoại lệ**:
  - Sai Username hoặc Password: Báo lỗi "Tên đăng nhập hoặc mật khẩu không chính xác", ghi log "Login Failed".
  - Tài khoản có `Status == "Inactive"`: Từ chối đăng nhập, hiển thị thông báo "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên" (FR-04, Test Scenario 1).

#### UC02 – Xem danh sách yêu cầu cá nhân (View My Tickets)
- **Actor**: User
- **Tiền điều kiện**: Đã xác thực vai trò User.
- **Hậu điều kiện**: Hiển thị danh sách ticket do chính User tạo (`CreatorId == CurrentUserId`).
- **Luồng chính**:
  1. User truy cập menu "Yêu cầu của tôi".
  2. Hệ thống tải danh sách ticket với điều kiện `CreatorId == CurrentUserId`, sắp xếp giảm dần theo `CreatedAt`.
  3. Cung cấp các tab lọc: Đang xử lý (`New`, `Assigned`, `InProgress`, `Waiting`), Đã giải quyết (`Resolved`), Hoàn tất (`Closed`), Bị từ chối (`Rejected`), Tất cả.
  4. Hiển thị thông tin tóm tắt: Mã ticket (#ID), Tiêu đề, Danh mục, Mức độ ưu tiên, Trạng thái (badge màu), Thời gian tạo, Hạn SLA.

#### UC03 – Tạo yêu cầu mới (Create New Ticket)
- **Actor**: User
- **Tiền điều kiện**: Đã xác thực vai trò User.
- **Hậu điều kiện**: Bản ghi `Ticket` được lưu với trạng thái `New`, tính hạn SLA `DueAt`, lưu file đính kèm (nếu có), phát thông báo tới Leader.
- **Luồng chính**:
  1. User bấm nút "Tạo Ticket mới".
  2. Form hiển thị các trường: Tiêu đề (bắt buộc), Danh mục (Hardware, Software, Network, Account, Other), Mức ưu tiên (Low, Medium, High, Critical - mặc định Medium), Mô tả chi tiết (bắt buộc), Tệp đính kèm (tùy chọn, tối đa 16MB).
  3. User nhập thông tin và bấm "Gửi yêu cầu".
  4. Hệ thống tính hạn hoàn thành theo SLA:
     - `Critical`: 1 giờ (`DueAt = DateTime.UtcNow.AddHours(1)`)
     - `High`: 4 giờ (`DueAt = DateTime.UtcNow.AddHours(4)`)
     - `Medium`: 24 giờ (`DueAt = DateTime.UtcNow.AddHours(24)`)
     - `Low`: 72 giờ (`DueAt = DateTime.UtcNow.AddHours(72)`)
  5. Nếu có tệp đính kèm: Lưu file vào thư mục upload với tên GUID, lưu bản ghi vào bảng `Attachment` kèm metadata (`FileName`, `OriginalFileName`, `ContentType`, `FileSize`).
  6. Khởi tạo `Ticket` với `StatusId = Status.New`, `CreatorId = CurrentUserId`.
  7. Kích hoạt Observer: `NotificationObserver` gửi thông báo tới các Leader; `SystemLogObserver` ghi log tạo ticket.
  8. SignalR Hub phát sự kiện cập nhật số lượng ticket tới dashboard của Leader.
  9. Chuyển hướng User về danh sách ticket kèm Toast thông báo thành công.

#### UC04 – Đánh giá & Feedback (Rate and Feedback)
- **Actor**: User
- **Tiền điều kiện**: Đã đăng nhập vai trò User; ticket do User tạo đang ở trạng thái `Resolved`; chưa có đánh giá nào trước đó (`Feedback == null`).
- **Hậu điều kiện**: Bản ghi `Feedback` được tạo, ticket chuyển sang trạng thái `Closed`.
- **Luồng chính**:
  1. User truy cập trang chi tiết ticket `Resolved` hoặc bấm "Đánh giá ngay" từ danh sách cần đánh giá.
  2. Hệ thống hiển thị modal/màn hình đánh giá gồm: Chọn số sao (1 đến 5 sao, bắt buộc), Ý kiến góp ý (tùy chọn).
  3. User chọn số sao và bấm "Gửi đánh giá".
  4. Hệ thống kiểm tra ràng buộc: Rating trong khoảng 1-5, Ticket đang ở `Resolved`.
  5. Tạo bản ghi `Feedback` liên kết `TicketId`.
  6. Chuyển trạng thái ticket sang `Closed` (`ClosedAt = DateTime.UtcNow`).
  7. Observer tạo thông báo cho Staff đã xử lý ticket và ghi `SystemLog`.

#### UC05 – Đăng xuất (Logout)
- **Actor**: Mọi vai trò đã đăng nhập (User, Staff, Leader, Admin)
- **Tiền điều kiện**: Người dùng đang có phiên đăng nhập hợp lệ.
- **Hậu điều kiện**: Phiên làm việc bị hủy, Cookie bị xóa, chuyển về `/Auth/Account/Login`.
- **Luồng chính**:
  1. Người dùng bấm nút "Đăng xuất" trên Sidebar/Navbar.
  2. Hệ thống gọi `HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`.
  3. Ghi nhận `SystemLog` hành động "Logout".
  4. Chuyển hướng người dùng về trang Đăng nhập kèm thông báo.

#### UC06 – Trao đổi thông tin ticket (Ticket Discussion)
- **Actor**: Mọi vai trò có quyền truy cập ticket (User, Staff, Leader, Admin)
- **Tiền điều kiện**: Người dùng đang ở màn hình chi tiết ticket được phân quyền; ticket không ở trạng thái khóa (`Resolved`, `Closed`, `Rejected`).
- **Hậu điều kiện**: Bản ghi `Comment` được lưu vào CSDL; tin nhắn được broadcast ngay lập tức qua SignalR WebSocket mà không tải lại trang.
- **Luồng chính**:
  1. Người dùng nhập nội dung tại ô chat "Viết phản hồi tại đây...".
  2. (Nếu là Staff hoặc Leader): Có thể tích chọn checkbox "Ghi chú nội bộ" (`IsInternal = true`).
  3. Bấm "Gửi phản hồi".
  4. Hệ thống kiểm tra: Nội dung không rỗng; Ticket đang ở trạng thái cho phép bình luận (`ticket.CanComment()` trả về true).
  5. Lưu bản ghi `Comment` vào CSDL.
  6. **Tự động chuyển trạng thái**: Nếu ticket đang ở trạng thái `Waiting` và người bình luận là `User` (không phải nội bộ) -> Ticket tự động chuyển về `InProgress`, thông báo cho Staff tiếp tục xử lý.
  7. Server đẩy tin nhắn qua SignalR `TicketHub`:
     - Tin nhắn công khai: Broadcast tới nhóm `ticket_{id}` cho toàn bộ người xem.
     - Ghi chú nội bộ: Chỉ broadcast tới các thành viên có vai trò `Staff`, `Leader`, `Admin`.
- **Ngoại lệ**: Nếu ticket đang `Resolved`, `Closed` hoặc `Rejected`, ô nhập bình luận bị khóa hoàn toàn. Mọi nỗ lực gửi POST bị từ chối với lỗi nghiệp vụ (Test Scenario 8).

#### UC07 – Cập nhật trạng thái xử lý (Update Ticket Status)
- **Actor**: Staff (người được giao ticket) hoặc Leader (khi tự xử lý)
- **Tiền điều kiện**: Đã xác thực; ticket được phân công cho chính actor; ticket đang ở trạng thái `Assigned`, `InProgress`, hoặc `Waiting`.
- **Hậu điều kiện**: Trạng thái ticket được cập nhật, nhãn trạng thái đổi màu, gửi thông báo cho User.
- **Luồng chính**:
  1. Staff truy cập chi tiết ticket được phân công.
  2. Hộp dropdown hiển thị các trạng thái đích hợp lệ theo State Pattern:
     - Từ `Assigned`: cho phép sang `InProgress`, `Waiting`, `Resolved`.
     - Từ `InProgress`: cho phép sang `Waiting`, `Resolved`.
     - Từ `Waiting`: cho phép sang `InProgress`, `Resolved`.
  3. Staff chọn trạng thái đích và bấm "Cập nhật".
  4. Thực thể `Ticket` gọi `UpdateStatus(newStatus)`:
     - Nếu chuyển sang `Resolved`: gán `ResolvedAt = DateTime.UtcNow`.
     - Cập nhật `UpdatedAt = DateTime.UtcNow`.
  5. Kích hoạt Observer: gửi thông báo cập nhật tiến độ cho User; ghi `SystemLog`.
  6. SignalR Hub đẩy thông báo thời gian thực tới User.
- **Ngoại lệ**: Nếu gửi trạng thái không hợp lệ (ví dụ gán `Closed` cho ticket đang `Assigned`), State Pattern chặn lại và trả về `Result.Failure` (Test Scenario 5).

#### UC08 – Xem chi tiết yêu cầu (View Ticket Detail)
- **Actor**: All Roles (kiểm tra phân quyền BOLA)
- **Tiền điều kiện**: Người dùng đã đăng nhập.
- **Hậu điều kiện**: Hiển thị đầy đủ thông tin ticket, file đính kèm, lịch sử xử lý, khung chat; hoặc trả về 403 nếu không đủ quyền.
- **Kiểm soát Truy cập Phân cấp (Resource-Based Authorization)**:
  - `User`: Chỉ được xem ticket do chính mình tạo (`CreatorId == CurrentUserId`).
  - `Staff`: Chỉ được xem ticket được phân công cho mình (`AssignedToId == CurrentUserId`).
  - `Leader` & `Admin`: Được phép xem toàn bộ ticket trong hệ thống.
  - *Vi phạm*: Nếu Staff A cố tình mở ticket của Staff B, hệ thống trả về mã lỗi HTTP 403 Forbidden (Test Scenario 7).

#### UC09 – Phân công yêu cầu (Assign Ticket)
- **Actor**: Leader
- **Tiền điều kiện**: Đã xác thực vai trò Leader; ticket đang ở hàng đợi phân công (`Status == New`) hoặc cần phân công lại.
- **Hậu điều kiện**: Ticket chuyển trạng thái `Assigned`, gán `AssignedToId = staffId`, thông báo cho Staff và User.
- **Luồng chính**:
  1. Leader vào trang "Phân công yêu cầu" (`Leader/Ticket/AssignQueue`).
  2. Hệ thống hiển thị danh sách ticket `New` sắp xếp theo mức độ ưu tiên và thời hạn SLA.
  3. Leader chọn kỹ thuật viên từ dropdown (hiển thị số lượng ticket đang đảm nhiệm của từng Staff để cân bằng tải).
  4. Bấm "Giao việc".
  5. Thực thể gọi `ticket.Assign(staff)`: Chuyển trạng thái sang `Assigned`, gán `AssignedToId`.
  6. Kích hoạt Observer: Tạo thông báo cho Staff ("Bạn được phân công ticket #ID"), tạo thông báo cho User ("Ticket của bạn đã được phân công cho kỹ thuật viên X"), ghi `SystemLog`.
  7. Đẩy thông báo thời gian thực qua SignalR.
- **Mở rộng (Tự xử lý & Phân công lại)**:
  - *Tự xử lý (Self-Assign)*: Leader có thể bấm nút "Tự xử lý" để gán `AssignedToId = LeaderId`, ticket chuyển sang `Assigned`.
  - *Phân công lại (Reassign)*: Khi ticket đang ở `Assigned`, `InProgress`, hoặc `Waiting` mà Staff phụ trách nghỉ việc hoặc quá tải, Leader chọn Staff mới; ticket giữ nguyên trạng thái hiện tại; Staff cũ, Staff mới và User đều nhận thông báo.

#### UC10 – Từ chối yêu cầu (Reject Ticket)
- **Actor**: Leader
- **Tiền điều kiện**: Đã xác thực vai trò Leader; ticket đang ở trạng thái `New`.
- **Hậu điều kiện**: Ticket chuyển sang trạng thái `Rejected`, lưu `RejectionReason`, rời khỏi hàng đợi phân công.
- **Luồng chính**:
  1. Leader nhấn "Từ chối" trên dòng ticket `New` không hợp lệ/sai quy định.
  2. Modal hiển thị yêu cầu nhập lý do từ chối (bắt buộc).
  3. Leader nhập lý do và bấm xác nhận.
  4. Thực thể gọi `ticket.Reject(reason)`: Kiểm tra đang ở `NewState`, gán `RejectionReason`, chuyển sang `RejectedState`.
  5. Kích hoạt Observer: Gửi thông báo kèm lý do cho User, ghi `SystemLog`.
  6. Ticket bị đóng băng vĩnh viễn (không thể bình luận hay cập nhật tiếp).
- **Ngoại lệ**: Nếu ticket không ở trạng thái `New` (ví dụ đang `InProgress`), hành động bị từ chối với `Result.Failure` (Test Scenario 6).

#### UC11 – Xem tổng quan hoạt động (View Management Dashboard)
- **Actor**: Leader
- **Tiền điều kiện**: Đã xác thực vai trò Leader.
- **Hậu điều kiện**: Hiển thị bảng điều khiển phân tích số liệu toàn diện.
- **Nội dung hiển thị**:
  1. Banner cảnh báo SLA: Liệt kê số lượng ticket mới chưa phân công > 24h và ticket đang xử lý > 48h.
  2. 4 Thẻ KPI: Chờ phân công, Đang xử lý, Đã hoàn thành, Tổng tiếp nhận (kèm tỷ lệ biến động ▲▼ so với kỳ trước).
  3. Biểu đồ cột chồng: Năng suất xử lý của từng nhân viên kỹ thuật (Hoàn thành đúng hạn vs Quá hạn).
  4. Biểu đồ tròn: Phân bổ yêu cầu theo danh mục sự cố.
  5. Bộ lọc thời gian: Hôm nay, 7 ngày qua, Tháng này, Tháng trước, Tùy chọn ngày.

#### UC12 – Xem & tra cứu tất cả yêu cầu (View All Tickets)
- **Actor**: Leader, Admin
- **Tiền điều kiện**: Đã xác thực vai trò Leader hoặc Admin.
- **Hậu điều kiện**: Hiển thị bảng danh sách toàn bộ ticket theo bộ lọc.
- **Chức năng**:
  - Tìm kiếm theo từ khóa: Mã ticket (#ID), Tiêu đề, Tên người tạo, Tên kỹ thuật viên.
  - Lọc theo: Trạng thái, Độ ưu tiên, Danh mục sự cố, Khoảng thời gian.
  - Phân trang Keyset kết hợp sắp xếp đa chiều.

#### UC13 – Quản lý người dùng & phân quyền (User Management)
- **Actor**: Admin
- **Tiền điều kiện**: Đã xác thực vai trò Admin.
- **Hậu điều kiện**: Hiển thị danh sách toàn bộ tài khoản trong hệ thống kèm trạng thái và menu thao tác.
- **Quy tắc Nghiệp vụ**:
  - Hiển thị danh sách: Username, Họ tên, Vai trò (Badge màu), Phòng ban, Trạng thái (Active/Inactive), Ngày tạo.
  - Phân quyền: Admin có thể sửa đổi thông tin và vai trò của người dùng khác.
  - Admin không thể tự hạ quyền của chính mình (`user.Id == CurrentUserId`).
  - Xóa tài khoản (Hard Delete): Chỉ cho phép nếu tài khoản chưa từng phát sinh dữ liệu (chưa tạo ticket, chưa nhận ticket, chưa bình luận). Nếu đã phát sinh dữ liệu, hệ thống bắt buộc chuyển sang "Vô hiệu hóa" (Soft Toggle) để đảm bảo toàn vẹn khóa ngoại.

#### UC14 – Tạo tài khoản người dùng mới (Create New User)
- **Actor**: Admin
- **Tiền điều kiện**: Đã xác thực vai trò Admin.
- **Hậu điều kiện**: Bản ghi `User` mới được lưu với mật khẩu băm, trạng thái `Active`.
- **Luồng chính**:
  1. Admin nhập: Username (duy nhất), Mật khẩu (tối thiểu 6 ký tự), Họ tên, Phòng ban, Chọn vai trò (User, Staff, Leader, Admin).
  2. Bấm "Tạo tài khoản".
  3. Hệ thống kiểm tra: Username chưa tồn tại trong CSDL.
  4. Băm mật khẩu bằng `IPasswordHasher<User>`.
  5. Lưu vào CSDL với `Status = "Active"`, `CreatedAt = DateTime.UtcNow`.
  6. Ghi nhận `SystemLog` hành động "Create User".

#### UC15 – Kích hoạt / Vô hiệu hóa tài khoản (Toggle User Status)
- **Actor**: Admin
- **Tiền điều kiện**: Đã xác thực vai trò Admin; tài khoản mục tiêu khác tài khoản Admin hiện tại.
- **Hậu điều kiện**: Đảo trạng thái `Active` ↔ `Inactive`; tài khoản Inactive bị chặn truy cập ngay lập tức.
- **Quy tắc Bảo mật Bắt buộc**:
  - **Chặn Tự Vô hiệu hóa**: Nếu Admin cố tình gửi request vô hiệu hóa chính mình (`targetUser.Id == CurrentAdmin.Id`), hệ thống chặn đứng và trả về lỗi "Admin không thể tự vô hiệu hóa tài khoản của chính mình" (Test Scenario 10).
  - Khi tài khoản chuyển sang `Inactive`: Cookie phiên làm việc của tài khoản đó (nếu đang online) sẽ bị `AuthorizeRolesAttribute` từ chối ở request kế tiếp và buộc đăng xuất (Test Scenario 1 & 10).

#### UC16 – Xem lịch sử xử lý (View Processing History)
- **Actor**: Staff
- **Tiền điều kiện**: Đã xác thực vai trò Staff.
- **Hậu điều kiện**: Hiển thị danh sách các ticket mà Staff đã xử lý xong (`Status == Resolved` hoặc `Status == Closed`).
- **Nội dung**: Hiển thị mã ticket, tiêu đề, thời gian tiếp nhận, thời gian hoàn tất, đánh giá sao từ User (nếu có), phân trang dữ liệu.

#### UC17 – Đổi mật khẩu cá nhân (Change Own Password)
- **Actor**: Mọi vai trò đã đăng nhập
- **Tiền điều kiện**: Đang có phiên đăng nhập hợp lệ.
- **Hậu điều kiện**: Mật khẩu mới được băm và cập nhật; mật khẩu cũ bị thay thế.
- **Luồng chính**:
  1. Người dùng nhập: Mật khẩu hiện tại, Mật khẩu mới, Xác nhận mật khẩu mới.
  2. Hệ thống kiểm tra mật khẩu hiện tại bằng `VerifyHashedPassword`.
  3. Kiểm tra mật khẩu mới khớp với xác nhận và thỏa mãn độ phức tạp.
  4. Cập nhật `PasswordHash` và ghi `SystemLog`.

#### UC18 – Đặt lại mật khẩu người dùng (Admin Reset Password)
- **Actor**: Admin
- **Tiền điều kiện**: Đã xác thực vai trò Admin.
- **Hậu điều kiện**: Mật khẩu của tài khoản mục tiêu bị ghi đè bằng mật khẩu mới do Admin thiết lập.
- **Luồng chính**:
  1. Admin chọn tài khoản cần đặt lại mật khẩu từ danh sách.
  2. Nhập mật khẩu mới trong Modal xác nhận.
  3. Hệ thống băm mật khẩu mới và cập nhật cho `User` mục tiêu.
  4. Ghi nhận `SystemLog` hành động "Admin Reset Password".

#### UC19 – Đóng / Mở lại yêu cầu (Close or Reopen Ticket)
- **Actor**: User
- **Tiền điều kiện**: Đã xác thực vai trò User; ticket do chính User tạo đang ở trạng thái `Resolved`.
- **Hậu điều kiện**: Ticket chuyển sang `Closed` (kết thúc vòng đời) hoặc quay về `InProgress` (yêu cầu xử lý lại).
- **Phân nhánh Luồng**:
  - **Nhánh Đóng (Close Ticket)**:
    1. User nhấn "Xác nhận & Đóng ticket".
    2. Hệ thống điều hướng đến form Đánh giá dịch vụ (UC04).
    3. User có thể đánh giá 1-5 sao hoặc bấm "Bỏ qua đánh giá".
    4. Ticket chuyển trạng thái `Closed`, gán `ClosedAt = DateTime.UtcNow`.
  - **Nhánh Mở lại (Reopen Ticket)**:
    1. User thấy sự cố chưa được xử lý triệt để, nhấn nút "Yêu cầu xử lý lại".
    2. Modal hiển thị yêu cầu nhập lý do mở lại.
    3. Ticket gọi `ticket.Reopen()`: Trạng thái chuyển từ `Resolved` về `InProgress`.
    4. Kích hoạt Observer: Gửi thông báo khẩn tới Staff phụ trách, ghi `SystemLog`.

#### UC20 – Xem Dashboard cá nhân (View Personal Dashboard)
- **Actor**: Mọi vai trò đã đăng nhập
- **Tiền điều kiện**: Đã đăng nhập thành công.
- **Hậu điều kiện**: Hiển thị bảng điều khiển chuyên biệt theo vai trò:
  - `User Dashboard`: 4 Thẻ tóm tắt (Tổng yêu cầu, Đang xử lý, Đã giải quyết, Cần đánh giá), danh sách ticket cần đánh giá ưu tiên trên đầu, danh sách yêu cầu gần đây, nút tạo yêu cầu nhanh.
  - `Staff Dashboard`: Thẻ công việc đang đảm nhiệm, số tin nhắn chưa đọc, hàng đợi xử lý phân loại theo mức độ ưu tiên, cảnh báo quá hạn SLA nhấp nháy đỏ, danh sách trao đổi mới nhất.
  - `Leader Dashboard`: Toàn bộ nội dung của UC11 (KPIs, Biểu đồ nhân viên, Biểu đồ danh mục, Cảnh báo SLA).
  - `Admin Dashboard`: 4 Thẻ hệ thống (Tổng người dùng, Tổng ticket, Tỷ lệ hoàn thành, Điểm đánh giá trung bình toàn hệ thống), Biểu đồ tỷ lệ vai trò người dùng, Lịch sử `SystemLog` gần nhất.

---

## 3. MÔ HÌNH DỮ LIỆU 8 THỰC THỂ & ĐẶC TẢ EF CORE 9 (SQLITE)

Hệ thống triển khai 8 bảng thực thể quan hệ trên nền tảng **SQLite** thông qua **EF Core 9**, đảm bảo khả năng chạy tự hành (zero-setup), độc lập môi trường nhưng vẫn đạt tiêu chuẩn cơ sở dữ liệu doanh nghiệp.

### 3.1. Sơ đồ Thực thể Quan hệ (ERD - Physical Relational Schema)
```text
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│     User        │1     *│     Ticket      │*     1│  TicketStatus   │
├─────────────────┼───────┼─────────────────┼───────┼─────────────────┤
│ Id (PK)         │       │ Id (PK)         │       │ Id (PK)         │
│ Username (UQ)   │       │ Title           │       │ Name (UQ)       │
│ PasswordHash    │       │ Description     │       │ Label           │
│ FullName        │       │ Priority        │       │ ColorClass      │
│ Role            │       │ Category        │       └─────────────────┘
│ Status          │       │ CreatorId (FK)  │
│ Department      │       │ AssignedToId(FK)│
│ CreatedAt       │       │ StatusId (FK)   │
└────────┬────────┘       │ DueAt           │
         │                │ ResolvedAt      │
         │1               │ ClosedAt        │
         │                │ RejectionReason │
         │                │ CreatedAt       │
         │                │ UpdatedAt       │
         │                │ RowVersion (CT) │
         │                └────────┬────────┘
         │                         │
         │        ┌────────────────┼────────────────┬────────────────┐
         │       1│               1│               1│               1│
         │        ▼*               ▼*               ▼*               ▼0..1
┌────────┴────────┐      ┌─────────┴───────┐      ┌─┴───────────────┐ ┌────────┴───────┐
│    Comment      │      │   Attachment    │      │  Notification   │ │   Feedback     │
├─────────────────┤      ├─────────────────┤      ├─────────────────┤ ├────────────────┤
│ Id (PK)         │      │ Id (PK)         │      │ Id (PK)         │ │ Id (PK)        │
│ TicketId (FK)   │      │ TicketId (FK)   │      │ UserId (FK)     │ │ TicketId (FK,UQ│
│ UserId (FK)     │      │ FileName        │      │ Message         │ │ Rating (1..5)  │
│ Content         │      │ OriginalFileName│      │ Link            │ │ Comment        │
│ IsInternal      │      │ ContentType     │      │ IsRead          │ │ CreatedAt      │
│ CreatedAt       │      │ FileSize        │      │ CreatedAt       │ └────────────────┘
└─────────────────┘      │ CreatedAt       │      └─────────────────┘
                         └─────────────────┘
         ▲
         │1
         │*
┌────────┴────────┐
│   SystemLog     │
├─────────────────┤
│ Id (PK)         │
│ UserId (FK,NULL)│
│ Action          │
│ Details         │
│ IpAddress       │
│ CreatedAt       │
└─────────────────┘
```

---

### 3.2. Đặc tả Chi tiết 8 Thực thể trong EF Core 9

#### 1. Thực thể `User` (Bảng `Users`)
Quản lý tài khoản, thông tin định danh và quyền hạn của cán bộ nhân viên.
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `Username`: `nvarchar(80)`, NOT NULL, Unique Index (`IX_Users_Username`).
- `PasswordHash`: `nvarchar(255)`, NOT NULL (Băm chuẩn ASP.NET Core `IPasswordHasher<User>`).
- `FullName`: `nvarchar(120)`, NOT NULL.
- `Role`: `nvarchar(20)`, NOT NULL. Giá trị hợp lệ: `User`, `Staff`, `Leader`, `Admin`. Index (`IX_Users_Role`).
- `Status`: `nvarchar(20)`, NOT NULL, Mặc định `'Active'`. Giá trị hợp lệ: `Active`, `Inactive`.
- `Department`: `nvarchar(100)`, NULL.
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.
- **Quan hệ**:
  - `CreatedTickets`: $1 - N$ với `Ticket` qua `CreatorId`.
  - `AssignedTickets`: $1 - N$ với `Ticket` qua `AssignedToId`.
  - `Comments`: $1 - N$ với `Comment`.
  - `Notifications`: $1 - N$ với `Notification`.
  - `SystemLogs`: $1 - N$ với `SystemLog`.

#### 2. Thực thể `TicketStatus` (Bảng `TicketStatuses`)
Bảng danh mục tra cứu 7 trạng thái chuẩn của vòng đời ticket.
- `Id`: `int`, Khóa chính (PK).
- `Name`: `nvarchar(50)`, NOT NULL, Unique Index (`IX_TicketStatuses_Name`). Giá trị: `New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`.
- `Label`: `nvarchar(100)`, NOT NULL (Tiếng Việt: "Mới tạo", "Đã phân công", "Đang xử lý", "Chờ phản hồi", "Đã giải quyết", "Đã đóng", "Đã từ chối").
- `ColorClass`: `nvarchar(100)`, NOT NULL (CSS Bootstrap 5: `bg-primary`, `bg-info`, `bg-warning`, `bg-secondary`, `bg-success`, `bg-dark`, `bg-danger`).

#### 3. Thực thể `Ticket` (Bảng `Tickets`)
Thực thể trung tâm lưu trữ toàn bộ tiến trình của sự cố kỹ thuật.
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `Title`: `nvarchar(100)`, NOT NULL.
- `Description`: `nvarchar(max)`, NOT NULL.
- `Priority`: `nvarchar(20)`, NOT NULL, Mặc định `'Medium'`. Giá trị: `Low`, `Medium`, `High`, `Critical`.
- `Category`: `nvarchar(50)`, NOT NULL. Giá trị: `Hardware`, `Software`, `Network`, `Account`, `Other`.
- `CreatorId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `Users(Id)`.
- `AssignedToId`: `int`, NULL, Khóa ngoại FK $\to$ `Users(Id)`.
- `StatusId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `TicketStatuses(Id)`.
- `DueAt`: `datetime2`, NOT NULL. Hạn xử lý cam kết theo SLA (tính tự động theo Priority lúc tạo).
- `ResolvedAt`: `datetime2`, NULL. Thời điểm kỹ thuật viên chuyển trạng thái sang `Resolved`.
- `ClosedAt`: `datetime2`, NULL. Thời điểm User xác nhận đóng ticket (`Closed`).
- `RejectionReason`: `nvarchar(max)`, NULL. Bắt buộc có dữ liệu khi trạng thái là `Rejected`.
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.
- `UpdatedAt`: `datetime2`, NOT NULL, Tự động cập nhật mỗi khi bản ghi thay đổi.
- `RowVersion`: `blob / byte[]`, NOT NULL. **Optimistic Concurrency Token** (sử dụng `.IsRowVersion()` trong EF Core 9 hoặc Timestamp Interceptor cập nhật tự động).
- **Chỉ mục Hiệu năng (Covering & Composite Indexes)**:
  - `IX_Tickets_StatusId_Priority_DueAt`: Hỗ trợ truy vấn hàng đợi Leader và cảnh báo vi phạm SLA.
  - `IX_Tickets_CreatorId_CreatedAt`: Hỗ trợ nạp nhanh danh sách ticket của User.
  - `IX_Tickets_AssignedToId_StatusId`: Hỗ trợ nạp nhanh hàng đợi công việc của Staff.

#### 4. Thực thể `Comment` (Bảng `Comments`)
Lưu trữ diễn biến trao đổi thảo luận giữa người dùng và đội ngũ kỹ thuật.
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `TicketId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `Tickets(Id)` (ON DELETE CASCADE).
- `UserId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `Users(Id)`.
- `Content`: `nvarchar(max)`, NOT NULL.
- `IsInternal`: `bit / bool`, NOT NULL, Mặc định `false`. Nếu `true`, chỉ Staff/Leader/Admin nhìn thấy.
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.
- **Chỉ mục**: `IX_Comments_TicketId_CreatedAt`.

#### 5. Thực thể `Attachment` (Bảng `Attachments`)
Quản lý tệp tin và hình ảnh đính kèm minh họa sự cố.
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `TicketId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `Tickets(Id)` (ON DELETE CASCADE).
- `FileName`: `nvarchar(255)`, NOT NULL (Tên file lưu trên ổ đĩa, định dạng GUID).
- `OriginalFileName`: `nvarchar(255)`, NOT NULL (Tên file gốc do người dùng tải lên).
- `ContentType`: `nvarchar(100)`, NOT NULL (MIME type: `image/png`, `application/pdf`, ...).
- `FileSize`: `bigint`, NOT NULL (Kích thước tệp tính theo bytes, tối đa 16MB = 16,777,216 bytes).
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.

#### 6. Thực thể `Feedback` (Bảng `Feedbacks`)
Thu thập mức độ hài lòng của người dùng sau khi sự cố được khắc phục.
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `TicketId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `Tickets(Id)`, **Unique Index** `IX_Feedbacks_TicketId` (Đảm bảo mỗi ticket chỉ có tối đa 1 phản hồi).
- `Rating`: `int`, NOT NULL, Ràng buộc miền giá trị CHECK (`Rating >= 1 AND Rating <= 5`).
- `Comment`: `nvarchar(max)`, NULL.
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.

#### 7. Thực thể `Notification` (Bảng `Notifications`)
Thông báo cá nhân hóa (chuông thông báo) gửi tới người dùng.
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `UserId`: `int`, NOT NULL, Khóa ngoại FK $\to$ `Users(Id)` (ON DELETE CASCADE).
- `Message`: `nvarchar(255)`, NOT NULL.
- `Link`: `nvarchar(255)`, NOT NULL (Đường dẫn điều hướng khi người dùng nhấp vào chuông).
- `IsRead`: `bit / bool`, NOT NULL, Mặc định `false`.
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.
- **Chỉ mục**: `IX_Notifications_UserId_IsRead_CreatedAt`.

#### 8. Thực thể `SystemLog` (Bảng `SystemLogs`)
Truy vết toàn diện lịch sử thao tác hệ thống (Audit Trail).
- `Id`: `int`, Khóa chính (PK), Auto Increment.
- `UserId`: `int`, NULL, Khóa ngoại FK $\to$ `Users(Id)` (ON DELETE SET NULL).
- `Action`: `nvarchar(50)`, NOT NULL (Ví dụ: `Login`, `CreateTicket`, `AssignTicket`, `UpdateStatus`, `RejectTicket`, `CloseTicket`, `ReopenTicket`, `ToggleUserStatus`, `ResetPassword`).
- `Details`: `nvarchar(max)`, NOT NULL (Mô tả chi tiết nội dung thay đổi).
- `IpAddress`: `nvarchar(50)`, NULL (Địa chỉ IP của client gửi request).
- `CreatedAt`: `datetime2`, NOT NULL, Mặc định `DateTime.UtcNow`.
- **Chỉ mục**: `IX_SystemLogs_CreatedAt_Action`.

---

### 3.3. Cơ chế Kiểm soát Đồng thời Lạc quan (Optimistic Concurrency) trong SQLite

Trong môi trường hỗ trợ kỹ thuật, nguy cơ hai kỹ thuật viên cùng mở một ticket và cùng thao tác cập nhật (Lost Update) là rất cao:
- **Cấu hình EF Core 9**:
  ```csharp
  builder.Entity<Ticket>()
      .Property(t => t.RowVersion)
      .IsRowVersion()
      .IsConcurrencyToken();
  ```
- **Cơ chế Hoạt động với SQLite**: Vì SQLite không có kiểu dữ liệu `ROWVERSION` tự tăng gốc như SQL Server, `SaveChangesInterceptor` trong EF Core 9 sẽ tự động tạo một mảng byte mới ngẫu nhiên (`Guid.NewGuid().ToByteArray()`) gán vào `ticket.RowVersion` mỗi khi entity bị sửa đổi.
- **Bắt ngoại lệ**: Khi phát hiện xung đột (`DbUpdateConcurrencyException`), hệ thống chuyển đổi thành kết quả trả về `Result.Failure(TicketErrors.ConcurrencyConflict)` tương ứng mã HTTP 409 Conflict (RFC 9457 ProblemDetails), hướng dẫn người dùng làm mới dữ liệu để xem trạng thái mới nhất thay vì ghi đè âm thầm.

---

### 3.4. Dữ liệu Khởi tạo Mặc định (Seed Data)
Ứng dụng tự động kiểm tra và khởi tạo dữ liệu mẫu khi khởi động:
1. **7 Trạng thái Ticket (`TicketStatuses`)**:
   - `Id = 1`: `New`, Label: "Mới tạo", Color: `bg-primary`
   - `Id = 2`: `Assigned`, Label: "Đã phân công", Color: `bg-info text-dark`
   - `Id = 3`: `InProgress`, Label: "Đang xử lý", Color: `bg-warning text-dark`
   - `Id = 4`: `Waiting`, Label: "Chờ phản hồi", Color: `bg-secondary`
   - `Id = 5`: `Resolved`, Label: "Đã giải quyết", Color: `bg-success`
   - `Id = 6`: `Closed`, Label: "Đã đóng", Color: `bg-dark`
   - `Id = 7`: `Rejected`, Label: "Đã từ chối", Color: `bg-danger`
2. **4 Tài khoản Đại diện cho 4 Vai trò (Mật khẩu mặc định: `Password123!`)**:
   - Admin: `username = "admin"`, `role = "Admin"`, `fullname = "Quản trị viên Hệ thống"`, `status = "Active"`
   - Leader: `username = "leader"`, `role = "Leader"`, `fullname = "Trưởng nhóm Kỹ thuật"`, `status = "Active"`
   - Staff: `username = "staff"`, `role = "Staff"`, `fullname = "Kỹ thuật viên IT"`, `status = "Active"`
   - User: `username = "user"`, `role = "User"`, `fullname = "Người dùng Văn phòng"`, `status = "Active"`
   - *(Tài khoản thử nghiệm kiểm thử)*: `username = "inactive_user"`, `role = "User"`, `fullname = "Tài khoản Bị khóa"`, `status = "Inactive"` (Dùng cho Scenario 1).

---

## 4. ĐẶC TẢ 4 MẪU THIẾT KẾ PHẦN MỀM (DESIGN PATTERNS)

### 4.1. State Pattern – Điều phối Vòng đời Ticket

#### A. Mục tiêu & Vấn đề Giải quyết
Thay thế hoàn toàn cấu trúc câu lệnh rẽ nhánh `switch-case` hoặc `if-else` lặp đi lặp lại ở Controller/Service. Mỗi trạng thái của Ticket được đóng gói thành một lớp riêng biệt, tự quyết định các hành vi được phép và chuyển đổi sang trạng thái kế tiếp.

#### B. Ma trận Hành vi Cho phép theo Trạng thái (State Invariant Matrix)
| Trạng thái Hiện tại | Phân công (`Assign`) | Phân công lại (`Reassign`) | Từ chối (`Reject`) | Cập nhật tiến độ (`UpdateStatus`) | Đóng (`Close`) | Mở lại (`Reopen`) | Cho phép Chat (`CanComment`) |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **New** | ✅ Cho phép | ❌ Chặn | ✅ Cho phép | ❌ Chặn ("Chờ phân công") | ❌ Chặn | ❌ Chặn | ✅ Cho phép |
| **Assigned** | ❌ Chặn | ✅ Cho phép | ❌ Chặn | ✅ Cho phép (sang InProgress, Waiting, Resolved) | ❌ Chặn | ❌ Chặn | ✅ Cho phép |
| **InProgress** | ❌ Chặn | ✅ Cho phép | ❌ Chặn | ✅ Cho phép (sang Waiting, Resolved) | ❌ Chặn | ❌ Chặn | ✅ Cho phép |
| **Waiting** | ❌ Chặn | ✅ Cho phép | ❌ Chặn | ✅ Cho phép (sang InProgress, Resolved) | ❌ Chặn | ❌ Chặn | ✅ Cho phép (User chat tự về InProgress) |
| **Resolved** | ❌ Chặn | ❌ Chặn | ❌ Chặn | ❌ Chặn | ✅ Cho phép (Kèm đánh giá) | ✅ Cho phép (Về InProgress) | ❌ **Bị Khóa** (Chặn hoàn toàn) |
| **Closed** | ❌ Chặn | ❌ Chặn | ❌ Chặn | ❌ Chặn ("Đã khóa") | ❌ Chặn | ❌ Chặn | ❌ **Bị Khóa** |
| **Rejected** | ❌ Chặn | ❌ Chặn | ❌ Chặn | ❌ Chặn ("Đã khóa") | ❌ Chặn | ❌ Chặn | ❌ **Bị Khóa** |

#### C. Thiết kế Hướng Đối tượng C# với Railway-Oriented `Result<T>`
```csharp
// Giao diện trừu tượng đại diện cho một State
public abstract class TicketStateBase
{
    public abstract string Name { get; }
    
    public virtual Result Assign(Ticket ticket, User staff) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, "Assign"));

    public virtual Result Reassign(Ticket ticket, User staff) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, "Reassign"));

    public virtual Result Reject(Ticket ticket, string reason) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, "Reject"));

    public virtual Result UpdateStatus(Ticket ticket, TicketStatus newStatus) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, "UpdateStatus"));

    public virtual Result Close(Ticket ticket) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, "Close"));

    public virtual Result Reopen(Ticket ticket) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, "Reopen"));

    public virtual bool CanComment() => true;
}

// Ví dụ minh họa lớp NewState
public sealed class NewState : TicketStateBase
{
    public override string Name => "New";

    public override Result Assign(Ticket ticket, User staff)
    {
        ticket.AssignedToId = staff.Id;
        ticket.TransitionTo(new AssignedState(), "Assigned");
        return Result.Success();
    }

    public override Result Reject(Ticket ticket, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(TicketErrors.RejectionReasonRequired);

        ticket.RejectionReason = reason;
        ticket.TransitionTo(new RejectedState(), "Rejected");
        return Result.Success();
    }
}

// Ví dụ minh họa lớp ResolvedState
public sealed class ResolvedState : TicketStateBase
{
    public override string Name => "Resolved";
    public override bool CanComment() => false; // Khóa chức năng bình luận

    public override Result Close(Ticket ticket)
    {
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.TransitionTo(new ClosedState(), "Closed");
        return Result.Success();
    }

    public override Result Reopen(Ticket ticket)
    {
        ticket.TransitionTo(new InProgressState(), "Reopened");
        return Result.Success();
    }
}
```

---

### 4.2. Observer Pattern – Thông báo và Ghi Nhật ký Tự động

#### A. Mục tiêu & Vấn đề Giải quyết
Tách rời hoàn toàn logic cốt lõi của Ticket khỏi các tác vụ phụ như gửi thông báo và ghi lịch sử kiểm toán. Khi có sự kiện thay đổi trạng thái, thực thể `Ticket` (đóng vai trò Subject) phát ra Domain Event, các Observer đã đăng ký sẽ tự động xử lý.

#### B. Các Observer Thành phần
1. **`NotificationObserver`**:
   - Khi `TicketCreatedEvent`: Xác định danh sách Leader $\to$ Thêm bản ghi `Notification` $\to$ Đẩy thông báo tức thời qua SignalR tới nhóm Leader.
   - Khi `TicketAssignedEvent`: Tạo thông báo cho Staff được giao và User tạo ticket.
   - Khi `TicketStatusUpdatedEvent`: Tạo thông báo tiến độ mới cho User.
   - Khi `TicketRejectedEvent`: Tạo thông báo kèm lý do từ chối cho User.
   - Khi `TicketClosedEvent`: Tạo thông báo cho Staff phụ trách.
   - Khi `TicketReopenedEvent`: Tạo thông báo khẩn cho Staff phụ trách.
2. **`SystemLogObserver`**:
   - Tự động bắt mọi Domain Event $\to$ Trích xuất `UserId`, tên hành động (`Action`), mô tả chi tiết (`Details`), và địa chỉ IP $\to$ Lưu vào bảng `SystemLogs`.
- **Đảm bảo Tính Toàn vẹn (ACID)**: Thao tác chuyển trạng thái, tạo thông báo và ghi nhật ký được thực thi trong cùng một Database Transaction. Nếu có bất kỳ lỗi nào xảy ra, toàn bộ thay đổi bị rollback.

---

### 4.3. Decorator / ActionFilter Pattern – Kiểm soát Truy cập Phân quyền (RBAC)

#### A. Mục tiêu & Vấn đề Giải quyết
Tập trung hóa logic bảo mật tại một điểm duy nhất thay vì viết các đoạn mã kiểm tra quyền rải rác trong từng Action của Controller.

#### B. Cài đặt C# `AuthorizeRolesAttribute`
Triển khai dưới dạng một ActionFilter trong ASP.NET Core (`IAsyncActionFilter` hoặc `AuthorizeAttribute` kết hợp `IAuthorizationFilter`):
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class AuthorizeRolesAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _roles;

    public AuthorizeRolesAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        // 1. Kiểm tra xác thực
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "Auth" });
            return;
        }

        // 2. Chặn tài khoản bị vô hiệu hóa (FR-04)
        var userService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        var userStatus = await userService.GetCurrentUserStatusAsync();
        if (userStatus == "Inactive")
        {
            await context.HttpContext.SignOutAsync();
            context.Result = new RedirectToActionResult("Login", "Account", 
                new { area = "Auth", error = "Tài khoản của bạn đã bị vô hiệu hóa." });
            return;
        }

        // 3. Kiểm tra vai trò RBAC (FR-03)
        if (_roles.Length > 0 && !_roles.Any(role => user.IsInRole(role)))
        {
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/AccessDenied.cshtml",
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
```

---

### 4.4. Singleton Pattern – Quản lý Dịch vụ Dùng chung Toàn Cục

Trong ASP.NET Core 9, Singleton Pattern được hiện thực hóa chuẩn mực qua Dependency Injection container (`builder.Services.AddSingleton<T>()`):
1. **`IUserConnectionManager`**: Quản lý bản đồ kết nối SignalR (`ConcurrentDictionary<int, HashSet<string>>`) ánh xạ giữa `UserId` và danh sách các `ConnectionId` đang hoạt động của người dùng trên các tab trình duyệt.
2. **`IPasswordHasher<User>`**: Triển khai thuật toán băm mật khẩu PBKDF2/Argon2id an toàn, dùng chung cho toàn bộ ứng dụng.
3. **`TimeProvider`**: Sử dụng `TimeProvider.System` (đăng ký Singleton) phục vụ tính toán thời gian SLA chuẩn xác và cho phép thay thế bằng `FakeTimeProvider` trong Unit Test.

---

## 5. ĐẶC TẢ KIẾN TRÚC REAL-TIME SIGNALR WEBSOCKET HUB (`TicketHub`)

### 5.1. Loại bỏ Hoàn toàn Cơ chế Polling 3s
- **Hạn chế của Polling 3s cũ**: Cứ 3 giây một lần, hàng trăm trình duyệt gửi HTTP GET request liên tục lên server để hỏi "Có tin nhắn mới không?". Điều này gây lãng phí $99\%$ băng thông mạng, làm cạn kiệt Connection Pool của SQLite và làm chậm máy chủ Kestrel.
- **Giải pháp SignalR WebSocket**: Thiết lập kết nối hai chiều liên tục (Persistent Full-Duplex Connection) tại endpoint `/hubs/ticket`. Server chỉ đẩy dữ liệu xuống client khi có sự kiện thực sự phát sinh.

### 5.2. Nhóm Kết nối (Connection Groups)
SignalR phân bổ kết nối vào các nhóm logic:
1. **Nhóm Ticket cụ thể (`ticket_{ticketId}`)**: Khi người dùng mở xem chi tiết một ticket, client tự động gửi lệnh tham gia nhóm này.
2. **Nhóm Vai trò (`role_Staff`, `role_Leader`, `role_Admin`)**: Phục vụ phát sóng thông báo tới toàn thể kỹ thuật viên hoặc lãnh đạo.
3. **Kênh Cá nhân (`Clients.User(userId)`)**: Đẩy thông báo riêng tư tới chuông thông báo của từng cá nhân.

### 5.3. Hợp đồng Giao tiếp (Hub Methods & Client Events)

#### A. Client gọi Server (Inbound Methods)
- `Task JoinTicket(int ticketId)`: Thêm `Context.ConnectionId` vào nhóm `ticket_{ticketId}`.
- `Task LeaveTicket(int ticketId)`: Xóa `Context.ConnectionId` khỏi nhóm `ticket_{ticketId}`.
- `Task SendMessage(int ticketId, string message, bool isInternal)`: Gửi bình luận mới trực tiếp qua kết nối WebSocket.

#### B. Server đẩy xuống Client (Outbound Events)
- `ReceiveComment(CommentDto comment)`:
  - Nếu `isInternal == false`: Đẩy tới toàn bộ kết nối trong nhóm `ticket_{ticketId}`.
  - Nếu `isInternal == true`: Chỉ đẩy tới các kết nối của người dùng có vai trò `Staff`, `Leader`, hoặc `Admin`.
- `TicketStatusUpdated(int ticketId, string newStatus, string colorClass)`: Cập nhật huy hiệu trạng thái trên giao diện theo thời gian thực.
- `ReceiveNotification(NotificationDto notification)`: Đẩy trực tiếp tới chuông thông báo của người nhận (`Clients.User(userId)`).
- `UpdateUnreadNotificationCount(int unreadCount)`: Cập nhật số đếm màu đỏ trên biểu tượng quả chuông của Navbar.

---

## 6. ĐẶC TẢ CHI TIẾT 10 KỊCH BẢN KIỂM THỬ NGHIỆM THU (BẢNG 7.2)

Dưới đây là 10 kịch bản kiểm thử cốt lõi được trích xuất nguyên văn từ Bảng 7.2 của báo cáo đề tài, kèm theo điều kiện biên, thao tác và tiêu chí nghiệm thu tự động (.NET 9 Test Suite):

| STT | Tên Kịch bản Kiểm thử | Tiền điều kiện | Thao tác Thực hiện | Kết quả Mong đợi | Mã Lỗi / Trạng thái HTTP |
|:---:|---|---|---|---|:---:|
| **1** | Đăng nhập bằng tài khoản đã bị vô hiệu hóa | Tồn tại tài khoản `inactive_user` có `Status = "Inactive"`. | Người dùng nhập đúng username `inactive_user` và password hợp lệ, bấm "Đăng nhập". | Hệ thống từ chối đăng nhập; hiển thị thông báo lỗi "Tài khoản đã bị vô hiệu hóa"; không cấp Authentication Cookie; ghi log đăng nhập thất bại. | Chặn tại tầng Auth (Redirect kèm Error) |
| **2** | User truy cập trang chỉ dành cho Leader | Đã đăng nhập bằng tài khoản có vai trò `User`. | Người dùng nhập trực tiếp URL `/Leader/Dashboard` hoặc `/Leader/Ticket/AssignQueue` trên thanh địa chỉ trình duyệt. | `AuthorizeRolesAttribute` phát hiện sai vai trò, chặn truy cập và trả về trang báo lỗi 403 Forbidden. | **HTTP 403 Forbidden** |
| **3** | User tạo ticket mới | Đã đăng nhập bằng tài khoản `User`. | Điền form tạo ticket với tiêu đề, danh mục, ưu tiên `High`, mô tả chi tiết; bấm "Gửi yêu cầu". | Ticket được tạo ở trạng thái `New`; tính hạn SLA `DueAt = Now + 4h`; tạo bản ghi `Notification` cho toàn bộ Leader; phát tín hiệu chuông qua SignalR; ghi `SystemLog`. | Thành công (Redirect về danh sách) |
| **4** | Leader phân công ticket New cho Staff | Đã đăng nhập bằng tài khoản `Leader`; có ticket #1 đang ở trạng thái `New`. | Leader chọn Staff kỹ thuật viên từ dropdown và bấm "Giao việc". | Ticket #1 chuyển trạng thái sang `Assigned`; gán `AssignedToId = staff.Id`; gửi thông báo cho Staff và User; dòng ticket biến mất khỏi hàng đợi phân công của Leader. | Thành công (200 OK / SignalR Broadcast) |
| **5** | Staff đặt trạng thái Closed cho ticket đang New | Ticket #2 đang ở trạng thái `New` (chưa được phân công). | Staff gửi request cập nhật trạng thái `Closed` cho ticket #2 qua form POST hoặc API. | State Pattern chặn thao tác vì `NewState` không cho phép đóng; trả về `Result.Failure`; toàn bộ dữ liệu của ticket #2 trong CSDL giữ nguyên không đổi. | **400 Bad Request** / Domain Failure |
| **6** | Leader từ chối ticket đang In Progress | Ticket #3 đang ở trạng thái `InProgress` (Staff đang thực hiện). | Leader cố tình gửi lệnh từ chối ticket #3 kèm lý do từ chối. | State Pattern chặn thao tác vì `InProgressState` không cho phép từ chối (chỉ `NewState` mới được phép); dữ liệu giữ nguyên; trả về thông báo lỗi. | **400 Bad Request** / Domain Failure |
| **7** | Staff mở ticket không được phân công cho mình | Đã đăng nhập bằng tài khoản Staff A; tồn tại ticket #4 được phân công cho Staff B ($A \neq B$). | Staff A nhập trực tiếp URL `/Staff/Ticket/Details/4`. | Hệ thống kiểm tra quyền sở hữu tài nguyên (BOLA defense), phát hiện Staff A không phụ trách ticket này, từ chối hiển thị và trả về mã lỗi 403 Forbidden. | **HTTP 403 Forbidden** |
| **8** | User bình luận trên ticket đã Resolved | Ticket #5 do User tạo đang ở trạng thái `Resolved`. | User cố gắng nhập nội dung và bấm gửi bình luận trên ticket #5. | `ResolvedState.CanComment()` trả về `false`; hệ thống chặn thao tác gửi; không lưu bản ghi nào vào bảng `Comments`; hiển thị thông báo "Yêu cầu đã hoàn tất, không thể bình luận". | **400 Bad Request** / Domain Failure |
| **9** | User xác nhận đóng và đánh giá ticket Resolved | Ticket #6 đang ở trạng thái `Resolved`. | User gửi biểu mẫu đánh giá dịch vụ với số sao = 5 kèm ý kiến đóng góp. | Ticket #6 chuyển sang trạng thái `Closed`; tạo bản ghi mới trong bảng `Feedbacks` (Rating = 5); cập nhật `ClosedAt`; thông báo cho Staff; ghi `SystemLog`. | Thành công (Redirect kèm Toast) |
| **10** | Admin vô hiệu hóa tài khoản khác; thử tự vô hiệu hóa chính mình | Đã đăng nhập bằng tài khoản `Admin`. | (a) Admin thực hiện vô hiệu hóa tài khoản Staff X.<br>(b) Admin gửi lệnh tự vô hiệu hóa chính tài khoản Admin hiện tại. | (a) Tài khoản Staff X chuyển `Status = Inactive`, Staff X bị chặn truy cập ở request tiếp theo.<br>(b) Lệnh tự vô hiệu hóa bị hệ thống chặn đứng; báo lỗi "Admin không thể tự vô hiệu hóa chính mình"; tài khoản Admin giữ nguyên `Active`. | (a) Thành công<br>(b) **Chặn an toàn (Báo lỗi)** |

---

## 7. YÊU CẦU ĐỒNG BỘ TÀI LIỆU BÁO CÁO WORD (`BaoCao_ITTMS_TKPMHDT (4).docx`)

Để đảm bảo tính toàn vẹn của đồ án môn học, việc cập nhật file báo cáo Word phải tuân thủ nghiêm ngặt các yêu cầu sau:

### 7.1. Nguyên tắc Bất biến: Bảo toàn 100% Mục lục & Phân cấp Tiêu đề
- Tuyệt đối không xóa, không đổi tên và không xáo trộn bất kỳ đề mục nào trong danh mục mục lục hiện có (từ Phần Mở đầu, Chương 1 đến Chương 7, Phần Kết luận, Tài liệu Tham khảo, Phụ lục).
- Toàn bộ 20 tiểu mục Use Case (từ 2.4.1 đến 2.4.20) và 8 tiểu mục CSDL (từ 3.2.1 đến 3.2.8) phải được giữ nguyên vẹn vị trí đánh số.

### 7.2. Các Nội dung Cốt lõi Cần Chuyển đổi sang C# / .NET 9
1. **Nền tảng Công nghệ (Bảng 7.1 & Mục 7.3)**:
   - Cũ: Python 3.x, Flask, SQLAlchemy, Flask-Login, Jinja2 + Tailwind CSS.
   - Mới: **C# (.NET 9), ASP.NET Core MVC, EF Core 9, SQLite, SignalR WebSocket, Bootstrap 5 UI**.
2. **Kiến trúc Hệ thống (Mục 7.1 & 7.2)**:
   - Cập nhật sơ đồ cấu trúc thư mục từ cấu trúc Flask Blueprint sang mô hình **Clean Architecture 5 Projects**:
     - `ItTms.Domain`: Chứa các thực thể, State Pattern, Observer Events, Value Objects.
     - `ItTms.Application`: Chứa Feature Slices, Service Interfaces, DTOs, FluentValidation.
     - `ItTms.Infrastructure`: Chứa `ApplicationDbContext`, Migrations, Interceptors, Repositories, Security.
     - `ItTms.Web`: Chứa 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`), SignalR `TicketHub`, Controllers, Views.
     - `ItTms.UnitTests`: Chứa bộ kiểm thử xUnit nghiệm thu 10 kịch bản Bảng 7.2.
3. **Mô hình Dữ liệu (Chương 3)**:
   - Cập nhật kiểu dữ liệu bảng biểu sang C# / SQLite: Bổ sung trường `RowVersion` (Concurrency Token), `DueAt` (SLA), `ResolvedAt`, `ClosedAt`, metadata tệp đính kèm.
4. **Mẫu Thiết kế Phần mềm (Chương 6)**:
   - Thay thế toàn bộ code minh họa Python bằng mã nguồn **C# (.NET 9)** hoàn chỉnh:
     - 6.2: State Pattern với `TicketStateBase`, 7 lớp State và trả về `Result<T>`.
     - 6.3: Observer Pattern với Domain Events và `NotificationObserver`, `SystemLogObserver`.
     - 6.4: Decorator Pattern với `AuthorizeRolesAttribute` (ActionFilter).
     - 6.5: Singleton Pattern qua ASP.NET Core DI.
     - 6.6: MVC Areas thay cho Flask Blueprints.
     - 6.7: Bảng 6.4: Bảng đối chiếu mã nguồn trước và sau khi áp dụng mẫu (phiên bản C#).
5. **Kịch bản Kiểm thử & Kết quả Thực nghiệm (Bảng 7.2)**:
   - Điền kết quả thực tế cho 10 kịch bản kiểm thử: Tất cả 10 kịch bản đều đạt trạng thái **"Đạt (Pass)"** dựa trên kết quả chạy tự động của bộ kiểm thử xUnit.

---

## 8. BẢNG TỔNG HỢP TÍNH NĂNG ĐƯỢC KHÁM PHÁ (FEATURES DISCOVERED)

| # | Phân nhóm (Category) | Tính năng (Feature) | Mô tả Chi tiết | Đầu vào (Inputs) | Đầu ra (Outputs) | Xử lý Lỗi (Error Behavior) | Nguồn Phát hiện |
|:---:|---|---|---|---|---|---|:---:|
| 1 | Xác thực & Phiên | Đăng nhập tài khoản | Xác thực người dùng, kiểm tra trạng thái Active, cấp Cookie và chuyển về Dashboard | Username, Password | Auth Cookie, Chuyển hướng Dashboard | Báo sai thông tin hoặc tài khoản bị khóa | Doc §2.4.1, Bảng 1.3 |
| 2 | Xác thực & Phiên | Đăng xuất an toàn | Hủy phiên làm việc và xóa Cookie trên trình duyệt | Lệnh đăng xuất | Hủy cookie, Chuyển về màn hình đăng nhập | Xử lý an toàn nếu timeout | Doc §2.4.5, Bảng 1.3 |
| 3 | Xác thực & Phiên | Đổi mật khẩu cá nhân | Cho phép người dùng tự đổi mật khẩu với xác nhận mật khẩu cũ | Mật khẩu cũ, Mật khẩu mới, Xác nhận | Mật khẩu mới băm và lưu CSDL | Báo sai mật khẩu cũ hoặc không khớp | Doc §2.4.17, Bảng 1.3 |
| 4 | Bảo mật & Phân quyền | RBAC Authorization Filter | Chặn truy cập trái phép bằng `AuthorizeRolesAttribute` | Role trong Claim | Cho phép truy cập Action | Trả về HTTP 403 Forbidden | Doc §6.4, Bảng 7.2 |
| 5 | Bảo mật & Phân quyền | Chặn phiên tài khoản Inactive | Tự động hủy quyền truy cập của tài khoản vừa bị vô hiệu hóa | User ID | Hủy phiên, Redirect đăng nhập | Trả về thông báo tài khoản bị khóa | Doc §1.4.1 (FR-04), Bảng 7.2 |
| 6 | Quản lý Ticket | Tạo mới Ticket yêu cầu | User gửi yêu cầu hỗ trợ kỹ thuật kèm độ ưu tiên và file | Tiêu đề, Danh mục, Ưu tiên, Mô tả, File | Bản ghi Ticket (New), SLA DueAt | Báo thiếu thông tin hoặc file > 16MB | Doc §2.4.3, Bảng 1.4 |
| 7 | Quản lý Ticket | Tự động tính hạn SLA | Tính thời hạn hoàn tất theo 4 cấp độ ưu tiên | Độ ưu tiên (Low..Critical) | Giá trị `DueAt` chuẩn xác | Mặc định 24h nếu không xác định | Doc §2.4.3, §1.1 |
| 8 | Quản lý Ticket | Danh sách Ticket cá nhân | Hiển thị các ticket do User tạo kèm bộ lọc trạng thái | Tab lọc, Trang | Danh sách ticket kèm huy hiệu màu | Hiển thị thông báo nếu rỗng | Doc §2.4.2, Bảng 1.4 |
| 9 | Quản lý Ticket | Xem chi tiết Ticket | Hiển thị 3 khối thông tin, nội dung, file và khung chat | Ticket ID | Trang chi tiết Ticket | Báo lỗi 404 (không tìm thấy) hoặc 403 | Doc §2.4.8, Bảng 1.4 |
| 10 | Điều phối Vòng đời | State Pattern Ticket Lifecycle | Điều phối chuyển trạng thái qua 7 lớp State cụ thể | Thao tác chuyển trạng thái | Trạng thái mới, Result.Success | Trả về Result.Failure (giữ nguyên DB) | Doc §6.2, Bảng 6.3 |
| 11 | Điều phối Vòng đời | Phân công Ticket (Leader) | Leader giao ticket New cho kỹ thuật viên | Ticket ID, Staff ID | Trạng thái `Assigned`, Gán Staff | Chặn nếu ticket không còn ở `New` | Doc §2.4.9, Bảng 1.6 |
| 12 | Điều phối Vòng đời | Tự xử lý Ticket (Self-Assign) | Leader tự tiếp nhận ticket New để trực tiếp giải quyết | Ticket ID | Trạng thái `Assigned`, Gán Leader | Chặn nếu ticket không ở `New` | Doc §2.4.9, Hình 4.11 |
| 13 | Điều phối Vòng đời | Phân công lại (Reassign) | Leader đổi Staff phụ trách khi Staff cũ nghỉ/quá tải | Ticket ID, Staff mới | Gán Staff mới, giữ nguyên trạng thái | Chặn nếu ticket đã hoàn tất | Doc §2.4.9, Bảng 5.1 |
| 14 | Điều phối Vòng đời | Từ chối Ticket kèm lý do | Leader từ chối ticket New không hợp lệ | Ticket ID, Lý do | Trạng thái `Rejected`, Lưu lý do | Chặn nếu để trống lý do hoặc không New | Doc §2.4.10, Bảng 1.6 |
| 15 | Điều phối Vòng đời | Cập nhật tiến độ xử lý | Staff chuyển trạng thái sang InProgress, Waiting, Resolved | Ticket ID, Trạng thái mới | Trạng thái mới, Cập nhật UpdatedAt | Chặn nếu chuyển trái sơ đồ State | Doc §2.4.7, Bảng 1.5 |
| 16 | Điều phối Vòng đời | Tự động chuyển Waiting -> InProgress | Ticket Waiting tự chuyển về InProgress khi User chat | Bình luận mới từ User | Trạng thái `InProgress` | Không đổi nếu là ghi chú nội bộ | Doc §2.4.6, Bảng 5.1 |
| 17 | Điều phối Vòng đời | Xác nhận Đóng Ticket | User đóng ticket Resolved sau khi hài lòng | Ticket ID | Trạng thái `Closed`, Gán ClosedAt | Chặn nếu ticket không ở `Resolved` | Doc §2.4.19, Bảng 1.4 |
| 18 | Điều phối Vòng đời | Yêu cầu Xử lý lại (Reopen) | User yêu cầu mở lại ticket Resolved nếu chưa xong | Ticket ID, Lý do | Trạng thái `InProgress`, Báo Staff | Chặn nếu ticket không ở `Resolved` | Doc §2.4.19, Bảng 1.4 |
| 19 | Đánh giá & Phản hồi | Đánh giá sao & Ý kiến | User gửi đánh giá 1-5 sao và góp ý sau khi xử lý xong | Ticket ID, Rating (1..5), Góp ý | Bản ghi Feedback, Đóng ticket | Chặn nếu chưa chọn sao hoặc đã đánh giá | Doc §2.4.4, Bảng 1.4 |
| 20 | Đánh giá & Phản hồi | Bỏ qua Đánh giá (Skip Feedback) | User chọn đóng ticket ngay mà không cần gửi đánh giá | Ticket ID | Trạng thái `Closed` | Chặn nếu ticket không ở `Resolved` | Doc §2.4.4, §5.2 |
| 21 | Trao đổi Thời gian thực | SignalR Ticket Hub Chat | Gửi nhận tin nhắn tức thời không cần tải lại trang | Ticket ID, Nội dung, IsInternal | Broadcast tin nhắn qua WebSocket | Chặn gửi nếu nội dung rỗng hoặc ticket khóa | Doc §2.4.6, §7.1, MEMORY |
| 22 | Trao đổi Thời gian thực | Ghi chú Nội bộ IT (Internal Note) | Tin nhắn chỉ hiển thị cho Staff, Leader, Admin | Flag `IsInternal = true` | Broadcast chỉ cho nhóm kỹ thuật/quản trị | Ẩn hoàn toàn với tài khoản User | Doc §2.4.6, Bảng 3.6 |
| 23 | Thông báo & Sự kiện | Notification Observer | Tự động tạo thông báo khi trạng thái ticket thay đổi | Domain Event | Bản ghi Notification, Đẩy SignalR chuông | Đảm bảo transaction ACID | Doc §6.3, Bảng 3.8 |
| 24 | Nhật ký & Kiểm toán | SystemLog Observer | Tự động ghi lại hành vi nghiệp vụ quan trọng | Domain Event | Bản ghi SystemLog kèm User và IP | Không làm gián đoạn luồng chính | Doc §6.3, Bảng 3.2 |
| 25 | Quản trị Người dùng | Tạo người dùng mới | Admin tạo tài khoản nhân sự và phân quyền vai trò | Username, Password, Name, Role, Dept | Tài khoản mới (Active) | Báo lỗi nếu Username đã tồn tại | Doc §2.4.14, Bảng 1.7 |
| 26 | Quản trị Người dùng | Vô hiệu hóa / Kích hoạt tài khoản | Admin bật tắt trạng thái làm việc của nhân sự | User ID | Đổi trạng thái Active ↔ Inactive | Chặn Admin tự vô hiệu hóa chính mình | Doc §2.4.15, Bảng 1.7 |
| 27 | Quản trị Người dùng | Đặt lại mật khẩu người dùng | Admin đổi mật khẩu mới cho người dùng | User ID, Mật khẩu mới | Mật khẩu mới được băm và lưu | Báo lỗi nếu mật khẩu rỗng | Doc §2.4.18, Bảng 1.7 |
| 28 | Quản trị Người dùng | Xóa tài khoản an toàn | Xóa tài khoản chưa có dữ liệu; chặn xóa nếu đã có dữ liệu | User ID | Xóa bản ghi User | Chặn xóa và gợi ý vô hiệu hóa nếu có FK | Doc §2.4.13, Bảng 5.1 |
| 29 | Dashboard & Thống kê | Dashboard Lãnh đạo (Leader) | Hiển thị 4 thẻ KPI, cảnh báo vi phạm SLA, 2 biểu đồ | Khoảng thời gian | Giao diện số liệu và biểu đồ động | Báo lỗi kết nối nếu API thất bại | Doc §2.4.11, Hình 4.7 |
| 30 | Dashboard & Thống kê | Dashboard Phân quyền Cá nhân | Điều hướng và hiển thị thông tin riêng cho từng vai trò | Vai trò User | Dashboard theo vai trò | Điều hướng an toàn | Doc §2.4.20, Bảng 1.3 |
| 31 | Kiểm soát Đồng thời | Optimistic Concurrency Token | Phát hiện và ngăn chặn ghi đè khi sửa dữ liệu đồng thời | RowVersion token | Ghi dữ liệu nếu token khớp | Ném DbUpdateConcurrencyException (409) | MEMORY §2, efcore-skill |

---

## 9. BẢNG MA TRẬN TRƯỜNG HỢP BIÊN & NGOẠI LỆ (EDGE CASES)

| # | Tính năng (Feature) | Đầu vào Kiểm thử Biên (Input / Scenario) | Hành vi Quan sát & Cơ chế Xử lý Mong đợi |
|:---:|---|---|---|
| **EC01** | Đăng nhập (UC01) | Đăng nhập tài khoản có `Status = "Inactive"` dù đúng mật khẩu. | Bị từ chối tại `AccountController`, hiển thị "Tài khoản đã bị vô hiệu hóa", không tạo Session/Cookie (Kịch bản 1). |
| **EC02** | Phân quyền (UC08) | User gõ trực tiếp URL truy cập trang Leader `/Leader/Dashboard`. | `AuthorizeRolesAttribute` chặn lại, trả về mã trạng thái HTTP 403 Forbidden (Kịch bản 2). |
| **EC03** | Bảo mật BOLA (UC08) | Staff A nhập URL xem chi tiết ticket được giao cho Staff B. | Resource-based authorization kiểm tra `AssignedToId != CurrentStaffId`, trả về HTTP 403 Forbidden (Kịch bản 7). |
| **EC04** | Chuyển trạng thái (UC07) | Staff gửi lệnh đặt trạng thái `Closed` cho ticket đang ở `New`. | State Pattern chặn vì `NewState` không có thao tác `Close`, trả về `Result.Failure`, dữ liệu CSDL giữ nguyên (Kịch bản 5). |
| **EC05** | Từ chối ticket (UC10) | Leader gửi lệnh từ chối (`Reject`) trên ticket đang `InProgress`. | `InProgressState` từ chối thao tác, trả về `Result.Failure`, ticket giữ nguyên trạng thái `InProgress` (Kịch bản 6). |
| **EC06** | Bình luận (UC06) | User gửi bình luận trên ticket đang ở trạng thái `Resolved`. | `ResolvedState.CanComment()` trả về `false`, hệ thống chặn lưu, báo lỗi "Ticket đã giải quyết, không thể bình luận" (Kịch bản 8). |
| **EC07** | Quản trị tài khoản (UC15)| Admin click vô hiệu hóa chính tài khoản Admin đang đăng nhập. | Hệ thống kiểm tra `targetId == currentAdminId`, chặn thao tác, thông báo lỗi an toàn, trạng thái giữ nguyên `Active` (Kịch bản 10). |
| **EC08** | Xóa tài khoản (UC13) | Admin yêu cầu xóa tài khoản đã có 5 ticket và 10 bình luận. | Hệ thống kiểm tra ràng buộc khóa ngoại, chặn xóa vật lý và hướng dẫn chuyển sang trạng thái "Vô hiệu hóa". |
| **EC09** | Tự động chuyển (UC06) | Ticket đang `Waiting`, User gửi một bình luận phản hồi mới. | Hệ thống tự động chuyển ticket về `InProgress`, đổi badge màu, kích hoạt Observer báo cho Staff tiếp tục xử lý. |
| **EC10** | Tải tệp đính kèm (UC03) | User tải file dung lượng 20MB (vượt ngưỡng 16MB) hoặc định dạng `.exe`. | Tầng Web chặn và hiển thị thông báo lỗi validate: tệp vượt quá dung lượng tối đa cho phép hoặc sai định dạng. |
| **EC11** | Đánh giá dịch vụ (UC04) | User gửi đánh giá với số sao = 0 hoặc gửi đánh giá lần 2 trên cùng 1 ticket. | Ràng buộc CHECK `Rating >= 1` và Unique Index `IX_Feedbacks_TicketId` chặn thao tác, trả về lỗi nghiệp vụ. |
| **EC12** | Ghi chú nội bộ (UC06) | Staff viết ghi chú nội bộ (`IsInternal = true`) trên ticket. | Giao diện User không hiển thị bóng chat này; SignalR không broadcast tới client của User; chỉ Staff/Leader thấy. |
| **EC13** | Xung đột ghi đồng thời | Hai kỹ thuật viên cùng bấm cập nhật tiến độ trên cùng 1 ticket tại cùng 1 giây. | Một request thành công, request còn lại bị `DbUpdateConcurrencyException` do lệch `RowVersion`, trả về HTTP 409 Conflict. |
| **EC14** | Hàng đợi rỗng (UC09) | Leader truy cập trang phân công khi không có ticket nào ở trạng thái `New`. | Bảng danh sách ẩn đi, hiển thị banner thông báo thân thiện: "Hàng đợi trống! Tất cả yêu cầu đã được phân công hoàn tất." |
| **EC15** | Đổi mật khẩu (UC17) | Người dùng nhập sai mật khẩu hiện tại khi thực hiện đổi mật khẩu. | `VerifyHashedPassword` thất bại, hệ thống từ chối cập nhật, giữ nguyên mật khẩu cũ và hiển thị thông báo lỗi. |
