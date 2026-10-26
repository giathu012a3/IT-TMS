# 🎨 THIẾT KẾ TOÀN DIỆN BOOTSTRAP 5 UI/UX & RAZOR VIEWS BLUEPRINT (SRC/ITTMS.WEB)

> **Dự án**: Hệ thống Quản lý Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Nền tảng**: .NET 9 ASP.NET Core MVC (5 Areas) + SignalR WebSocket + Bootstrap 5.3  
> **Tác giả**: `explorer_m3_3` (UI/UX & Razor Views Explorer)  
> **Tiêu chuẩn thiết kế**: Enterprise Clean Architecture, WCAG 2.1 AA Accessibility, Zero-CDN Self-Contained.

---

## 1. KIẾN TRÚC TỔNG THỂ UI/UX & CẤU TRÚC THƯ MỤC RAZOR VIEWS

Hệ thống giao diện người dùng của **IT-TMS** được xây dựng trên nền tảng **Bootstrap 5.3** kết hợp cơ chế nạp tĩnh hoàn toàn cục bộ (`wwwroot/lib`), không dựa vào bất kỳ mạng phân phối nội dung bên ngoài (CDN) nào để đảm bảo tính sẵn sàng cao, bảo mật mạng nội bộ doanh nghiệp và vận hành tự hành.

### 1.1. Cấu trúc Cây Thư mục Views trong `src/ItTms.Web`
```
src/ItTms.Web/
├── Areas/
│   ├── Auth/
│   │   ├── Controllers/ (AccountController.cs)
│   │   └── Views/
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml
│   │       └── Account/
│   │           ├── Login.cshtml                 # UC01, TC-01 (Hiển thị rõ lỗi tài khoản bị khóa)
│   │           └── ChangePassword.cshtml        # UC17 (Đổi mật khẩu cá nhân)
│   ├── User/
│   │   ├── Controllers/ (DashboardController.cs, TicketController.cs)
│   │   └── Views/
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml
│   │       ├── Dashboard/
│   │       │   └── Index.cshtml                 # UC20 (Dashboard cá nhân người dùng)
│   │       └── Ticket/
│   │           ├── Index.cshtml                 # UC02 (Danh sách ticket của tôi + bộ lọc tab)
│   │           ├── Create.cshtml                # UC03, TC-03 (Tạo ticket, SLA Preview, Upload)
│   │           └── Details.cshtml               # UC06, UC08, UC09, TC-08, TC-09 (Chat, Đánh giá)
│   ├── Staff/
│   │   ├── Controllers/ (DashboardController.cs, TicketController.cs)
│   │   └── Views/
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml
│   │       ├── Dashboard/
│   │       │   └── Index.cshtml                 # UC20 (Dashboard kỹ thuật viên, cảnh báo SLA)
│   │       └── Ticket/
│   │           ├── Assigned.cshtml              # UC13, TC-07 (Danh sách được giao, chặn BOLA)
│   │           ├── Details.cshtml               # UC07, UC10..12 (Nút chuyển State, Chat nội bộ)
│   │           └── History.cshtml               # UC16 (Lịch sử đã xử lý, điểm đánh giá)
│   ├── Leader/
│   │   ├── Controllers/ (DashboardController.cs, TicketController.cs)
│   │   └── Views/
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml
│   │       ├── Dashboard/
│   │       │   └── Index.cshtml                 # UC11, UC17 (SLA Analytics, Biểu đồ năng suất)
│   │       └── Ticket/
│   │           ├── NewQueue.cshtml              # UC09, UC10, TC-04, TC-06 (Hàng đợi phân công)
│   │           ├── Index.cshtml                 # UC12, UC16 (Tất cả ticket, Reassign)
│   │           └── Details.cshtml               # Giám sát toàn cục chi tiết ticket
│   └── Admin/
│       ├── Controllers/ (DashboardController.cs, UserController.cs, AuditController.cs)
│       └── Views/
│           ├── _ViewImports.cshtml
│           ├── _ViewStart.cshtml
│           ├── Dashboard/
│           │   └── Index.cshtml                 # UC20 (Dashboard quản trị hệ thống)
│           ├── User/
│           │   ├── Index.cshtml                 # UC13, UC15, TC-10 (Bảng tài khoản, ToggleActive)
│           │   ├── Create.cshtml                # UC14 (Tạo tài khoản mới)
│           │   └── Edit.cshtml                  # Cập nhật thông tin & vai trò
│           └── Audit/
│               └── Index.cshtml                 # UC20 (Nhật ký kiểm toán SystemLogs + bộ lọc)
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml                       # Master Layout (Responsive Navbar, Bell, Theme)
│   │   ├── _ValidationScriptsPartial.cshtml     # jQuery Validation Unobtrusive
│   │   ├── _ToastAlerts.cshtml                  # Partial thông báo TempData Toast
│   │   ├── _SvgIcons.cshtml                     # Thư viện biểu tượng SVG nội tuyến
│   │   ├── _SlaBadge.cshtml                     # Partial hiển thị Badge hạn cam kết SLA
│   │   ├── _StatusBadge.cshtml                  # Partial hiển thị Badge trạng thái ticket
│   │   ├── AccessDenied.cshtml                  # HTTP 403 Forbidden (TC-02, TC-07)
│   │   └── Error.cshtml                         # HTTP 500 / Ngoại lệ hệ thống
│   ├── Home/
│   │   └── Index.cshtml                         # Landing Page điều hướng theo vai trò
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── Models/ViewModels/                           # Thư mục ViewModels chuẩn hóa dữ liệu hiển thị
└── wwwroot/
    ├── css/
    │   ├── site.css                             # Custom styles, responsive tweaks, animation
    │   └── theme-dark.css                       # Các tinh chỉnh màu sắc cho chế độ Dark Mode
    ├── js/
    │   ├── site.js                              # Theme toggle, toast initializer, auto-dismiss
    │   ├── ticket-realtime.js                   # SignalR WebSocket client, chat appending
    │   └── sla-calculator.js                    # Tính toán và hiển thị trước SLA DueAt trên form
    └── lib/                                     # Thư viện cục bộ (bootstrap, jquery, signalr)
```

---

## 2. MASTER LAYOUT `_Layout.cshtml` & THÀNH PHẦN ĐIỀU HƯỚNG DÙNG CHUNG

### 2.1. Thiết kế Giao diện Thanh điều hướng (Responsive Navbar)
- **Cấu trúc Topbar Cố định (`sticky-top`)**:
  - **Logo & Thương hiệu**: Logo IT Shield SVG kèm nhãn chữ `IT-TMS` và huy hiệu phiên bản `.NET 9 Enterprise`.
  - **Menu Điều hướng Phân quyền Động (Role-Based Dynamic Navigation)**:
    - Sử dụng `@User.IsInRole(...)` để chỉ kết xuất các liên kết tương ứng với thẩm quyền được cấp phép:
      - **Khách (Chưa đăng nhập)**: Liên kết `Trang chủ`, `Đăng nhập`.
      - **User (Nhân viên yêu cầu)**:
        - `Bàn làm việc` (`/User/Dashboard`)
        - `Tạo yêu cầu mới` (`/User/Ticket/Create`) kèm nút nhấn nổi bật `btn-primary`.
        - `Yêu cầu của tôi` (`/User/Ticket/Index`) kèm bộ đếm số lượng ticket đang xử lý.
      - **Staff (Kỹ thuật viên)**:
        - `Dashboard Kỹ thuật` (`/Staff/Dashboard`)
        - `Công việc được giao` (`/Staff/Ticket/Assigned`) kèm huy hiệu số ticket `Assigned/InProgress`.
        - `Lịch sử hoàn tất` (`/Staff/Ticket/History`).
      - **Leader (Trưởng nhóm IT)**:
        - `Tổng quan & SLA` (`/Leader/Dashboard`).
        - `Hàng đợi phân công` (`/Leader/Ticket/NewQueue`) kèm badge cảnh báo màu đỏ số lượng ticket `New`.
        - `Tất cả yêu cầu` (`/Leader/Ticket/Index`).
      - **Admin (Quản trị viên)**:
        - `Dashboard Hệ thống` (`/Admin/Dashboard`).
        - `Quản lý Người dùng` (`/Admin/User/Index`).
        - `Nhật ký Hệ thống` (`/Admin/Audit/Index`).

### 2.2. Chuông Thông báo Thời gian thực (SignalR Notification Bell Dropdown)
- **Thành phần**:
  - Biểu tượng quả chuông với badge đếm màu đỏ `#notificationCountBadge`.
  - Dropdown menu `#notificationDropdownMenu` hiển thị danh sách tối đa 5 thông báo mới nhất.
  - Mỗi mục thông báo gồm: Biểu tượng sự kiện, nội dung vắn tắt, nhãn thời gian tương đối ("5 phút trước"), liên kết điều hướng trực tiếp tới ticket.
  - Chân dropdown: Nút "Đánh dấu tất cả đã đọc" (AJAX POST) và liên kết "Xem tất cả thông báo".
- **Cơ chế Real-time qua SignalR**:
  - Khi client kết nối `/hubs/ticket`, server gửi sự kiện `UpdateUnreadNotificationCount(int count)`.
  - Khi có thông báo mới, server phát `ReceiveNotification(NotificationDto dto)`, client phát âm thanh thông báo nhẹ (Web Audio API), cập nhật số đếm và chèn dòng thông báo mới lên đầu dropdown menu kèm hiệu ứng chuyển động mượt mà.

### 2.3. Bộ Chuyển đổi Giao diện Sáng / Tối (Theme Toggle: Light / Dark Mode)
- **Tận dụng Tính năng Gốc của Bootstrap 5.3 (`data-bs-theme`)**:
  - Thuộc tính `data-bs-theme="light"` hoặc `data-bs-theme="dark"` được gắn trực tiếp trên thẻ `<html>`.
  - Nút chuyển đổi giao diện trên Navbar hiển thị icon Mặt trời (khi đang ở Dark) và Mặt trăng (khi đang ở Light).
  - Trạng thái được lưu tự động vào `localStorage.getItem('it-tms-theme')`.
  - Một đoạn script nhỏ được đặt ngay đầu thẻ `<head>` để ngăn ngừa hiện tượng giật sáng màn hình (FOUC - Flash of Unstyled Content) khi tải lại trang:
    ```javascript
    (function() {
      const storedTheme = localStorage.getItem('it-tms-theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
      document.documentElement.setAttribute('data-bs-theme', storedTheme);
    })();
    ```

### 2.4. Hồ sơ Người dùng & Đăng xuất An toàn
- Hiển thị Avatar chữ cái đầu (Avatar Placeholder tròn) + Họ tên người dùng + Badge màu vai trò:
  - `Admin`: `<span class="badge bg-danger">Quản trị viên</span>`
  - `Leader`: `<span class="badge bg-info text-dark">Trưởng nhóm</span>`
  - `Staff`: `<span class="badge bg-warning text-dark">Kỹ thuật viên</span>`
  - `User`: `<span class="badge bg-secondary">Nhân viên</span>`
- Dropdown menu:
  - `Đổi mật khẩu` (`/Auth/Account/ChangePassword`)
  - Đường kẻ phân cách (`<hr class="dropdown-divider">`)
  - `Đăng xuất`: Triển khai bằng thẻ `<form>` ẩn phương thức POST có nhúng `@Html.AntiForgeryToken()` tới `/Auth/Account/Logout` để chống tấn công CSRF, không dùng liên kết thẻ `<a>` GET đơn thuần.

### 2.5. Hệ thống Flash Toast Thông báo
- Sử dụng Bootstrap 5 Toast Container góc trên bên phải màn hình (`position-fixed top-0 end-0 p-3`):
  - `TempData["SuccessMessage"]`: Toast nền xanh lá (`text-bg-success`).
  - `TempData["ErrorMessage"]`: Toast nền đỏ (`text-bg-danger`).
  - `TempData["WarningMessage"]`: Toast nền vàng cam (`text-bg-warning`).
  - Tự động đóng sau 5 giây (`data-bs-delay="5000"`).

---

## 3. CHI TIẾT BẢN VẼ THIẾT KẾ RAZOR VIEWS CHO 5 AREAS

### 3.1. Area `Auth` (Xác thực & Bảo mật Phiên)

#### A. `Areas/Auth/Views/Account/Login.cshtml`
- **Mục tiêu Nghiệp vụ**: Thực hiện UC01, vượt qua kịch bản kiểm thử nghiệm thu **Bảng 7.2 TC-01** (Đăng nhập bằng tài khoản đã bị vô hiệu hóa bị từ chối với thông báo lỗi rõ ràng).
- **Thiết kế Bố cục**:
  - Giao diện Card tập trung ở giữa màn hình (kích thước tối đa `420px`), nền sáng bóng đổ mềm (`shadow-sm rounded-4`).
  - Header: Logo doanh nghiệp, tiêu đề "Đăng nhập Hệ thống IT-TMS", phụ đề "Cổng tiếp nhận & xử lý sự cố kỹ thuật".
  - **Khu vực Hiển thị Cảnh báo Tài khoản Bị Khóa (TC-01)**:
    - Khi tài khoản có trạng thái `Inactive`, bộ lọc `AuthorizeRolesAttribute` hoặc phương thức `AccountController.Login` thiết lập `TempData["ErrorMessage"]` hoặc nạp lỗi vào `ModelState`.
    - View kết xuất khối Alert đỏ nổi bật kèm icon tam giác cảnh báo:
      ```html
      @if (TempData["ErrorMessage"] != null)
      {
          <div class="alert alert-danger d-flex align-items-center mb-4 border-0 shadow-sm rounded-3" role="alert">
              <svg class="bi flex-shrink-0 me-3" width="24" height="24" role="img" aria-label="Danger:">
                  <use xlink:href="#exclamation-triangle-fill"/>
              </svg>
              <div>
                  <strong>Truy cập bị từ chối:</strong><br />
                  @TempData["ErrorMessage"]
              </div>
          </div>
      }
      ```
  - **Form Đăng nhập**:
    - Trường `Username`: Input kèm icon người dùng, focus tự động lúc nạp trang.
    - Trường `Password`: Input mật khẩu kèm nút icon con mắt bật/tắt hiển thị mật khẩu.
    - Checkbox `Ghi nhớ phiên đăng nhập`.
    - Nút bấm `Đăng nhập` (Full-width, `btn btn-primary btn-lg fw-semibold shadow-sm`).
  - **Hộp Thông tin Tài khoản Thử nghiệm (Demo Credentials Widget)**:
    - Bảng thông tin nhỏ phía dưới giúp hội đồng nghiệm thu và kiểm thử viên kiểm tra nhanh 4 vai trò và tài khoản bị vô hiệu hóa theo Bảng 7.2:
      - `admin` / `Password123!` (Admin)
      - `leader` / `Password123!` (Leader)
      - `staff` / `Password123!` (Staff)
      - `user` / `Password123!` (User)
      - `inactive_user` / `Password123!` (Inactive - Kiểm tra TC-01)
    - Nhấp vào từng tài khoản mẫu sẽ tự động điền giá trị vào form (Quick Fill JS).

#### B. `Areas/Auth/Views/Account/ChangePassword.cshtml`
- **Mục tiêu**: Thực hiện UC17 (Đổi mật khẩu cá nhân).
- **Thành phần**:
  - Form nhập: Mật khẩu hiện tại, Mật khẩu mới, Nhập lại mật khẩu mới.
  - Thanh đo độ mạnh mật khẩu (Password Strength Meter) theo thời gian thực (Đỏ: Yếu, Vàng: Trung bình, Xanh: An toàn).
  - Gợi ý quy chuẩn mật khẩu (tối thiểu 6 ký tự, có chữ in hoa, chữ số và ký tự đặc biệt).

---

### 3.2. Area `User` (Cổng Người dùng Yêu cầu Hỗ trợ)

#### A. `Areas/User/Views/Dashboard/Index.cshtml`
- **Mục tiêu**: UC20 (Dashboard cá nhân người dùng).
- **Bố cục & Thành phần**:
  - **Banner Chào mừng & Hành động Nhanh**:
    - Lời chào cá nhân hóa theo thời gian trong ngày ("Chào buổi sáng / chiều, [FullName]").
    - Nút lớn: `+ Tạo Ticket Yêu cầu Mới` dẫn thẳng tới `/User/Ticket/Create`.
  - **4 Thẻ Chỉ số KPI Cá nhân**:
    1. *Tổng yêu cầu đã gửi* (Icon tổng hợp, màu xanh dương).
    2. *Đang được xử lý* (Gồm `New`, `Assigned`, `InProgress`, `Waiting` - Icon bánh răng quay, màu vàng).
    3. *Đã giải quyết xong* (Trạng thái `Resolved` hoặc `Closed` - Icon tích xanh, màu xanh lá).
    4. *Chờ bạn đánh giá* (Trạng thái `Resolved` chưa gửi feedback - Icon ngôi sao, màu cam, kèm hiệu ứng nhấp nháy thôi thúc).
  - **Banner Nhắc nhở Nghiệm thu khẩn cấp**:
    - Nếu có ticket ở trạng thái `Resolved`, hiển thị thanh thông báo viền vàng cam: "Bạn có X yêu cầu đã được kỹ thuật viên xử lý xong. Vui lòng xác nhận đóng và đánh giá chất lượng dịch vụ để chúng tôi hoàn thiện hơn." kèm nút "Đánh giá ngay".
  - **Bảng Yêu cầu Gần đây (Recent Tickets Table)**:
    - 5 ticket tạo gần nhất kèm mã ID, tiêu đề, trạng thái, thời gian tạo và nút xem chi tiết.

#### B. `Areas/User/Views/Ticket/Create.cshtml`
- **Mục tiêu**: Thực hiện UC03, đạt kịch bản nghiệm thu **Bảng 7.2 TC-03** (Tạo ticket mới ở trạng thái `New`, tính đúng SLA `DueAt`, phát thông báo cho Leader).
- **Thành phần Form & Widget Dự đoán SLA**:
  1. **Tiêu đề Yêu cầu (`Title`)**: Ô nhập ngắn gọn, bắt buộc, tối đa 150 ký tự.
  2. **Danh mục Sự cố (`Category`)**: Dropdown lựa chọn:
     - Phần cứng (Hardware)
     - Phần mềm (Software)
     - Mạng & Kết nối (Network)
     - Tài khoản & Phân quyền (Account)
     - Khác (Other)
  3. **Mức độ Ưu tiên (`Priority`) & Widget Dự đoán SLA Thông minh**:
     - Radio group hoặc dropdown 4 mức độ: `Critical`, `High`, `Medium` (Mặc định), `Low`.
     - **Widget Xem trước Hạn SLA (`#slaPreviewBox`)**:
       - Tự động lắng nghe sự kiện `change` trên trường `Priority`.
       - Tính toán thời gian hoàn tất dự kiến dựa trên thời gian thực tế của máy client:
         - `Critical`: "Cam kết xử lý trong **1 giờ** (Dự kiến xong trước: **[Now + 1h]**)" - Card cảnh báo đỏ.
         - `High`: "Cam kết xử lý trong **4 giờ** (Dự kiến xong trước: **[Now + 4h]**)" - Card cảnh báo vàng cam.
         - `Medium`: "Cam kết xử lý trong **24 giờ** (Dự kiến xong trước: **[Now + 24h]**)" - Card xanh lam tiêu chuẩn.
         - `Low`: "Cam kết xử lý trong **72 giờ** (Dự kiến xong trước: **[Now + 72h]**)" - Card xám thông thường.
  4. **Mô tả Chi tiết Sự cố (`Description`)**:
     - Textarea rộng 6 dòng với bộ đếm ký tự. Placeholder hướng dẫn mô tả: Vị trí xảy ra lỗi, phần mềm đang dùng, mã lỗi hiển thị trên màn hình.
  5. **Tải Tệp Đính kèm Minh họa (`Attachments`)**:
     - Hỗ trợ chọn nhiều file (Ảnh chụp màn hình, tài liệu PDF, DOCX, XLSX).
     - Kiểm tra kích thước ở client: Chặn file > 16MB trước khi submit (EC10).
     - Danh sách file đã chọn hiển thị dung lượng và icon tương ứng.
  6. **Nút Gửi Yêu cầu**:
     - Nút `Gửi yêu cầu hỗ trợ` kèm trạng thái loading spinner (`disabled` khi submit để chống bấm nhiều lần gây duplicate ticket).

#### C. `Areas/User/Views/Ticket/Index.cshtml`
- **Mục tiêu**: Thực hiện UC02 (Xem danh sách yêu cầu cá nhân).
- **Bộ lọc Trạng thái (Filter Tabs)**:
  - Tab 1: `Tất cả`
  - Tab 2: `Đang xử lý` (Trạng thái `New`, `Assigned`, `InProgress`, `Waiting`)
  - Tab 3: `Đã giải quyết` (Trạng thái `Resolved` - cần đánh giá)
  - Tab 4: `Hoàn tất` (Trạng thái `Closed`)
  - Tab 5: `Bị từ chối` (Trạng thái `Rejected`)
- **Bảng Dữ liệu Responsive**:
  - Cột 1: Mã Ticket (`#00123`).
  - Cột 2: Tiêu đề & Danh mục.
  - Cột 3: Mức độ Ưu tiên (Badge màu theo SLA).
  - Cột 4: Trạng thái (Badge màu theo 7 trạng thái chuẩn).
  - Cột 5: Thời hạn SLA (`DueAt` kèm biểu tượng đồng hồ; đổi sang màu đỏ nhấp nháy nếu quá hạn).
  - Cột 6: Thời gian gửi.
  - Cột 7: Thao tác (Xem chi tiết, Đánh giá ngay nếu đang `Resolved`).

#### D. `Areas/User/Views/Ticket/Details.cshtml`
- **Mục tiêu**: Thực hiện UC06, UC08, UC09; đáp ứng nghiệm thu **Bảng 7.2 TC-08** (Chặn bình luận khi Resolved/Closed/Rejected) và **TC-09** (Đóng & Đánh giá ticket Resolved).
- **Cấu trúc 3 Cột / 2 Khối Độc lập**:
  - **Khối 1: Thông tin Sự cố & Tệp Đính kèm (Bên Trái - Col 7)**:
    - Thẻ chi tiết: Tiêu đề lớn, danh mục, thời gian tạo, mô tả sự cố đầy đủ.
    - Danh sách tệp đính kèm: Card nhỏ có tên file gốc, dung lượng MB, nút tải về an toàn `/User/Ticket/DownloadAttachment?id=...`.
    - Dòng thời gian tiến độ (Timeline Vertical Stepper): Mới tạo $\to$ Đã phân công $\to$ Đang xử lý $\to$ Đã giải quyết $\to$ Đã đóng.
  - **Khối 2: Khung Trao đổi Trực tuyến SignalR (Bên Phải - Col 5)**:
    - Hộp chat thời gian thực cuộn độc lập (`max-height: 480px`).
    - Bóng chat (Message Bubbles): Người dùng bên phải (nền xanh), Kỹ thuật viên bên trái (nền xám nhạt).
    - **Cơ chế Khóa Bình luận theo State (Bảng 7.2 TC-08)**:
      - Sử dụng kiểm tra `@if (Model.CanComment)`:
        - Nếu `true`: Hiển thị ô nhập tin nhắn và nút "Gửi phản hồi" kết nối SignalR `TicketHub`.
        - Nếu `false` (Ticket ở `Resolved`, `Closed`, hoặc `Rejected`):
          - Ẩn hoàn toàn ô nhập liệu hoặc thiết lập `disabled`.
          - Hiển thị Alert màu vàng xám:
            ```html
            <div class="alert alert-secondary text-center py-2 mb-0 border-0 rounded-3">
                <i class="bi bi-lock-fill me-1"></i> Yêu cầu này đã hoàn tất hoặc bị khóa. Không thể gửi thêm bình luận.
            </div>
            ```
  - **Khối 3: Nghiệm thu & Đánh giá Dịch vụ (Bảng 7.2 TC-09)**:
    - Nếu ticket đang ở trạng thái `Resolved`:
      - Hiển thị Action Banner kêu gọi nghiệm thu với 2 nút:
        1. `Xác nhận & Đánh giá chất lượng` (Mở `FeedbackModal`).
        2. `Yêu cầu xử lý lại` (Mở `ReopenModal`).

---

### 3.3. Area `Staff` (Bàn Làm việc Kỹ thuật viên IT)

#### A. `Areas/Staff/Views/Dashboard/Index.cshtml`
- **Mục tiêu**: UC20 (Dashboard kỹ thuật viên).
- **Thành phần**:
  - **Thanh Cảnh báo Khẩn cấp Vi phạm SLA**:
    - Danh sách các ticket sắp hết hạn (< 30 phút) hoặc đã vượt quá `DueAt`.
  - **Thống kê Công việc**:
    - Thẻ `Chờ tiếp nhận (Assigned)`, `Đang thực hiện (InProgress)`, `Chờ khách phản hồi (Waiting)`, `Đã hoàn thành hôm nay`.
  - **Bảng Nhiệm vụ Ưu tiên Cao Nhất**:
    - Liệt kê các ticket `Critical` và `High` được phân công cần giải quyết ngay lập tức.

#### B. `Areas/Staff/Views/Ticket/Assigned.cshtml`
- **Mục tiêu**: Thực hiện UC13, phòng thủ an ninh BOLA đáp ứng **Bảng 7.2 TC-07** (Staff chỉ xem danh sách và chi tiết ticket được giao cho chính mình).
- **Thành phần**:
  - Bảng danh sách ticket phân công: Lọc theo trạng thái `Assigned`, `InProgress`, `Waiting`.
  - Cột tiến độ SLA: Hiển thị đồng hồ đếm ngược thời gian còn lại trước hạn `DueAt`.
  - Nút thao tác nhanh: Xem chi tiết.

#### C. `Areas/Staff/Views/Ticket/Details.cshtml`
- **Mục tiêu**: Thực hiện UC07, UC10, UC11, UC12 (Cập nhật tiến độ xử lý theo State Pattern).
- **Thanh Điều hướng Nghiệp vụ (State Pattern Workflow Action Bar)**:
  - Kiểm tra trạng thái hiện tại của ticket để hiển thị đúng các nút thao tác hợp lệ:
    - **Khi Ticket đang `Assigned`**:
      - Nút `Bắt đầu xử lý (Start Progress)`: Chuyển ticket sang `InProgress` (gọi POST `/Staff/Ticket/StartProgress`).
    - **Khi Ticket đang `InProgress`**:
      - Nút `Tạm dừng chờ thông tin (Put on Hold)`: Mở `HoldForInfoModal` (nhập lý do cần người dùng làm rõ, chuyển ticket sang `Waiting`).
      - Nút `Hoàn tất xử lý (Resolve)`: Mở `ResolveModal` (nhập nội dung tóm tắt giải pháp khắc phục, chuyển ticket sang `Resolved`, ghi nhận `ResolvedAt = Now`).
    - **Khi Ticket đang `Waiting`**:
      - Nút `Tiếp tục xử lý (Resume)`: Chuyển ticket quay lại `InProgress`.
  - **Khung Chat SignalR dành cho Staff**:
    - Bổ sung ô checkbox: `[ ] Ghi chú nội bộ (Chỉ IT nhìn thấy)`:
      - Khi chọn, tin nhắn sẽ có cờ `isInternal = true`. Giao diện hiển thị bóng chat màu vàng viền nét đứt kèm nhãn `[Nội bộ IT]`. Người dùng thông thường sẽ không nhìn thấy tin nhắn này.

#### D. `Areas/Staff/Views/Ticket/History.cshtml`
- **Mục tiêu**: Thực hiện UC16 (Xem lịch sử xử lý).
- **Thành phần**:
  - Bảng tổng hợp các sự cố Staff đã giải quyết thành công (`Resolved`, `Closed`).
  - Cột thời gian xử lý thực tế (từ lúc tạo đến khi Resolved).
  - Cột Đánh giá Sao từ người dùng (1-5 sao vàng) kèm nhận xét đánh giá để Staff tự theo dõi chất lượng phục vụ.

---

### 3.4. Area `Leader` (Điều phối Kỹ thuật & Báo cáo SLA)

#### A. `Areas/Leader/Views/Dashboard/Index.cshtml`
- **Mục tiêu**: Thực hiện UC11, UC17 (Xem tổng quan hoạt động & Báo cáo SLA).
- **Thành phần Trực quan Hóa Dữ liệu**:
  1. **Banner Cảnh báo Vi phạm SLA (SLA Violation Alerts)**:
     - Số lượng ticket `New` chưa phân công > 24 giờ.
     - Số lượng ticket `InProgress` đang xử lý > 48 giờ chưa hoàn thành.
  2. **4 Thẻ Chỉ số Quản lý**:
     - *Chờ phân công*: Số ticket `New`.
     - *Đang xử lý*: Số ticket `Assigned` + `InProgress` + `Waiting`.
     - *Đã hoàn thành*: Số ticket `Resolved` + `Closed`.
     - *Tỷ lệ Đúng hạn SLA*: Phần trăm hoàn thành đúng hạn cam kết (Mục tiêu: $\ge 95\%$).
  3. **Biểu đồ Cột Chồng: Năng suất Kỹ thuật viên (Staff Performance Chart)**:
     - Trục hoành: Tên các kỹ thuật viên Staff.
     - Cột xanh: Số ticket hoàn thành đúng hạn SLA.
     - Cột đỏ: Số ticket hoàn thành trễ hạn hoặc đang quá hạn SLA.
  4. **Biểu đồ Tròn: Phân bổ Sự cố theo Danh mục (Category Distribution Chart)**:
     - Tỷ lệ các loại lỗi: Phần cứng, Phần mềm, Mạng, Tài khoản, Khác.
  5. **Bộ Lọc Khoảng Thời Gian**:
     - Nút bấm nhanh: Hôm nay, 7 ngày qua, Tháng này, Tháng trước, Tùy chọn ngày.

#### B. `Areas/Leader/Views/Ticket/NewQueue.cshtml`
- **Mục tiêu**: Thực hiện UC09, UC10, đáp ứng nghiệm thu **Bảng 7.2 TC-04** (Phân công ticket New cho Staff) và **TC-06** (Leader từ chối ticket New kèm lý do, chặn từ chối khi InProgress).
- **Giao diện Hàng đợi Phân công**:
  - Bảng danh sách các ticket có trạng thái `New` được sắp xếp ưu tiên theo `Priority` và `CreatedAt`.
  - Dòng thông tin nổi bật: Tiêu đề sự cố, người tạo, phòng ban, mức độ ưu tiên, hạn SLA.
  - **Các Thao tác trên Dòng**:
    1. **Nút "Giao việc" (Assign)**: Mở `AssignModal`.
    2. **Nút "Tự xử lý" (Self-Assign)**: Giao thẳng cho Leader hiện tại, chuyển sang `Assigned`.
    3. **Nút "Từ chối" (Reject)**: Mở `RejectModal` (bắt buộc nhập lý do từ chối - TC-06).

#### C. `Areas/Leader/Views/Ticket/Index.cshtml` (Tất cả Yêu cầu & Điều chuyển)
- **Mục tiêu**: Thực hiện UC12, UC16 (Tra cứu toàn bộ ticket & Tái phân công).
- **Thành phần**:
  - Thanh tìm kiếm từ khóa đa năng: Mã ID, Tiêu đề, Tên người yêu cầu, Tên kỹ thuật viên.
  - Bộ lọc kết hợp: Trạng thái, Danh mục, Mức ưu tiên, Kỹ thuật viên phụ trách.
  - Thao tác: Hỗ trợ nút `Điều chuyển (Reassign)` cho các ticket đang xử lý khi Staff nghỉ ốm hoặc quá tải.

---

### 3.5. Area `Admin` (Quản trị Hệ thống & Nhật ký Kiểm toán)

#### A. `Areas/Admin/Views/Dashboard/Index.cshtml`
- **Mục tiêu**: UC20 (Dashboard quản trị hệ thống).
- **Thành phần**:
  - 4 Thẻ hệ thống: Tổng người dùng, Tài khoản hoạt động, Tổng ticket toàn hệ thống, Tổng bản ghi nhật ký kiểm toán.
  - Biểu đồ phân bổ cơ cấu người dùng theo 4 vai trò (Admin, Leader, Staff, User).
  - Bảng 10 sự kiện nhật ký bảo mật mới nhất (`SystemLogs`).

#### B. `Areas/Admin/Views/User/Index.cshtml`
- **Mục tiêu**: Thực hiện UC13, UC15, UC18; đáp ứng nghiệm thu **Bảng 7.2 TC-10** (Vô hiệu hóa tài khoản khác và chặn Admin tự vô hiệu hóa chính mình).
- **Bảng Quản lý Danh sách Người dùng**:
  - Các cột: Mã ID, Tên đăng nhập, Họ và tên, Email, Phòng ban, Vai trò (Badge màu), Trạng thái (`Active` xanh lá / `Inactive` xám đỏ), Ngày tạo.
  - Nút thêm mới: `+ Tạo Tài khoản Mới` dẫn tới `/Admin/User/Create`.
  - **Cơ chế Bảo vệ Chặn Tự Vô hiệu hóa (Bảng 7.2 TC-10)**:
    - Trong vòng lặp kết xuất từng dòng:
      ```html
      @if (user.Id == currentAdminId)
      {
          <button class="btn btn-sm btn-outline-secondary" disabled 
                  title="Bạn không thể tự vô hiệu hóa tài khoản của chính mình">
              <i class="bi bi-slash-circle me-1"></i> Khóa
          </button>
      }
      else
      {
          <form asp-action="ToggleActive" asp-controller="User" asp-area="Admin" method="post" class="d-inline">
              @Html.AntiForgeryToken()
              <input type="hidden" name="id" value="@user.Id" />
              <button type="submit" class="btn btn-sm @(user.IsActive ? "btn-outline-danger" : "btn-outline-success")"
                      onclick="return confirm('Bạn có chắc muốn @(user.IsActive ? "vô hiệu hóa" : "kích hoạt lại") tài khoản này?');">
                  <i class="bi @(user.IsActive ? "bi-lock" : "bi-unlock") me-1"></i>
                  @(user.IsActive ? "Vô hiệu hóa" : "Kích hoạt")
              </button>
          </form>
      }
      ```
  - **Nút "Đặt lại Mật khẩu"**: Mở `ResetPasswordModal` cho phép Admin đổi mật khẩu mới cho người dùng (UC18).

#### C. `Areas/Admin/Views/Audit/Index.cshtml`
- **Mục tiêu**: Thực hiện UC20 (Xem nhật ký hệ thống `SystemLogs`).
- **Thành phần**:
  - Bộ lọc tìm kiếm: Theo loại hành vi (`Action`: Login, CreateTicket, AssignTicket, UpdateStatus, ToggleUserStatus...), theo Người thực hiện (`UserId`), theo Địa chỉ IP, theo Khoảng ngày.
  - Bảng dữ liệu kiểm toán: Thời điểm (UTC & Local), Người thực hiện, Địa chỉ IP, Hành vi, Tóm tắt chi tiết.
  - Nút xem chi tiết: Mở Modal hiển thị toàn bộ payload dữ liệu JSON trong trường `Details`.

---

## 4. HỆ THỐNG MODALS NGHIỆP VỤ CHUẨN HÓA

Toàn bộ các thao tác nghiệp vụ quan trọng đều được đóng gói trong các Bootstrap 5 Modals có cấu trúc ngữ nghĩa rõ ràng, hỗ trợ phím `Esc`, tự động focus ô nhập liệu đầu tiên và có Anti-Forgery Token bảo vệ.

### 4.1. Modal Phân công Ticket (`AssignModal`)
- **Vị trí**: Dùng trong `Leader/Ticket/NewQueue.cshtml` và `Leader/Ticket/Index.cshtml`.
- **Nội dung**:
  - Tiêu đề: "Phân công xử lý Yêu cầu #[ID] - [Title]".
  - Dropdown chọn Kỹ thuật viên (Staff):
    - Hiển thị danh sách kèm theo số lượng ticket đang đảm nhiệm: `Nguyễn Văn A (Đang xử lý: 2 ticket)`.
    - Đánh dấu màu xanh cho các kỹ thuật viên có tải công việc thấp nhất (Cân bằng tải thông minh).
  - Nút bấm: "Hủy bỏ" và "Xác nhận giao việc" (POST tới `/Leader/Ticket/Assign`).

### 4.2. Modal Từ chối Ticket (`RejectModal`)
- **Vị trí**: Dùng trong `Leader/Ticket/NewQueue.cshtml`.
- **Nghiệp vụ**: Bắt buộc nhập lý do từ chối (Bảng 7.2 TC-06).
- **Nội dung**:
  - Cảnh báo: "Thao tác này sẽ từ chối và đóng băng vĩnh viễn yêu cầu #[ID]. Hành động này không thể hoàn tác."
  - Textarea: "Lý do từ chối yêu cầu (Bắt buộc)" - kiểm tra validation tối thiểu 10 ký tự.
  - Nút bấm: "Hủy" và "Xác nhận Từ chối" (`btn-danger`).

### 4.3. Modal Đánh giá & Hoàn tất Ticket (`FeedbackModal`)
- **Vị trí**: Dùng trong `User/Ticket/Details.cshtml` khi ticket có trạng thái `Resolved` (Bảng 7.2 TC-09).
- **Nội dung**:
  - Tiêu đề: "Đánh giá chất lượng hỗ trợ sự cố #[ID]".
  - **Bộ chọn 5 Ngôi sao Tương tác (Interactive 5-Star Rating)**:
    - 5 ngôi sao SVG hover và click mượt mà.
    - Click chọn số sao sẽ tự động điền giá trị 1..5 vào trường `<input type="hidden" name="Rating" />`.
    - Nhãn tương ứng: 1 sao ("Rất không hài lòng"), 2 sao ("Không hài lòng"), 3 sao ("Bình thường"), 4 sao ("Hài lòng"), 5 sao ("Rất hài lòng").
  - Ô góp ý (`Comment`): Textarea tùy chọn để người dùng để lại lời cảm ơn hoặc ý kiến đóng góp.
  - Nút bấm: "Gửi đánh giá & Hoàn tất đóng" (`btn-success`) và "Bỏ qua đánh giá & Đóng ngay" (`btn-outline-secondary`).

### 4.4. Modal Yêu cầu Xử lý lại (`ReopenModal`)
- **Vị trí**: Dùng trong `User/Ticket/Details.cshtml` khi ticket ở trạng thái `Resolved` nhưng sự cố chưa được khắc phục triệt để.
- **Nội dung**:
  - Textarea: "Mô tả lý do yêu cầu xử lý lại (Điểm nào chưa đạt?)".
  - Nút bấm: "Gửi yêu cầu mở lại" (`btn-warning`). Trạng thái ticket sẽ chuyển từ `Resolved` về `InProgress`.

### 4.5. Modal Tạm dừng chờ thông tin (`HoldForInfoModal`)
- **Vị trí**: Dùng trong `Staff/Ticket/Details.cshtml`.
- **Nội dung**:
  - Textarea: "Lý do tạm dừng / Thông tin cần Người dùng cung cấp thêm".
  - Nút bấm: "Xác nhận Tạm dừng" (`btn-secondary`). Trạng thái ticket chuyển từ `InProgress` sang `Waiting`.

### 4.6. Modal Hoàn tất Xử lý (`ResolveModal`)
- **Vị trí**: Dùng trong `Staff/Ticket/Details.cshtml`.
- **Nội dung**:
  - Textarea: "Ghi chú giải pháp khắc phục (Cách thức giải quyết sự cố)".
  - Nút bấm: "Xác nhận Hoàn thành" (`btn-success`). Trạng thái ticket chuyển sang `Resolved`, lưu `ResolvedAt = Now`.

---

## 5. HỆ THỐNG HUY HIỆU (BADGES), SLA COUNTDOWN & THƯ VIỆN SVG ICONS TỰ HÀNH

### 5.1. Ma trận 7 Trạng thái Ticket & Huy hiệu Bootstrap 5 Chuẩn hóa
Tuân thủ 100% định nghĩa trong bảng CSDL `TicketStatuses`:

| ID | Tên Mã (`Name`) | Nhãn Tiếng Việt (`Label`) | Lớp CSS Bootstrap 5 | Ý nghĩa Hiển thị |
|:---:|---|---|---|---|
| **1** | `New` | Mới tạo | `badge bg-primary` | Yêu cầu mới gửi, chờ Leader tiếp nhận và giao việc. |
| **2** | `Assigned` | Đã phân công | `badge bg-info text-dark` | Đã chỉ định Staff phụ trách, chờ bắt đầu xử lý. |
| **3** | `InProgress` | Đang xử lý | `badge bg-warning text-dark` | Staff đang trực tiếp kiểm tra và khắc phục sự cố. |
| **4** | `Waiting` | Chờ phản hồi | `badge bg-secondary` | Đang chờ người dùng bổ sung thông tin chi tiết. |
| **5** | `Resolved` | Đã giải quyết | `badge bg-success` | Kỹ thuật viên đã xử lý xong, chờ User nghiệm thu. |
| **6** | `Closed` | Đã đóng | `badge bg-dark` | Kết thúc vòng đời ticket kèm đánh giá hài lòng. |
| **7** | `Rejected` | Đã từ chối | `badge bg-danger` | Yêu cầu không hợp lệ hoặc sai quy định bị Leader hủy. |

### 5.2. Ma trận Mức độ Ưu tiên & Cam kết SLA
| Mức độ Ưu tiên | Thời hạn SLA Cam kết | Huy hiệu Hiển thị | Màu sắc |
|---|:---:|---|---|
| `Critical` | **1 giờ** | `<span class="badge bg-danger"><i class="bi bi-lightning-fill me-1"></i>Khẩn cấp (1h)</span>` | Đỏ rực |
| `High` | **4 giờ** | `<span class="badge bg-warning text-dark"><i class="bi bi-arrow-up-circle-fill me-1"></i>Cao (4h)</span>` | Vàng cam |
| `Medium` | **24 giờ** | `<span class="badge bg-primary"><i class="bi bi-dash-circle-fill me-1"></i>Trung bình (24h)</span>` | Xanh dương |
| `Low` | **72 giờ** | `<span class="badge bg-secondary"><i class="bi bi-arrow-down-circle-fill me-1"></i>Thấp (72h)</span>` | Xám |

### 5.3. Dynamic SLA Countdown & Visual Indicators
Trong các bảng danh sách và trang chi tiết, thời hạn hoàn tất được hiển thị thông minh dựa trên chênh lệch giữa `DueAt` và thời gian hiện tại (`DateTime.UtcNow`):
1. **Còn nhiều thời gian ($> 25\%$ thời lượng SLA)**:
   - `<span class="badge bg-success-subtle text-success border border-success"><i class="bi bi-clock me-1"></i>Còn 18 giờ</span>`
2. **Sắp hết hạn ($< 25\%$ thời lượng SLA)**:
   - `<span class="badge bg-warning-subtle text-warning border border-warning"><i class="bi bi-hourglass-split me-1"></i>Sắp hết hạn (còn 40 phút)</span>`
3. **Đã vi phạm SLA (`DueAt < DateTime.UtcNow` và chưa hoàn tất)**:
   - `<span class="badge bg-danger text-white border border-danger fw-bold animate-pulse"><i class="bi bi-exclamation-triangle-fill me-1"></i>Quá hạn SLA</span>`

### 5.4. Thư viện SVG Icons Nội tuyến Tự hành (`_SvgIcons.cshtml`)
Để loại bỏ hoàn toàn rủi ro mất kết nối CDN font, toàn bộ các biểu tượng thông dụng được đóng gói trong một file Partial SVG Sprite `Views/Shared/_SvgIcons.cshtml`, bao gồm:
- Biểu tượng Khiên bảo mật (`#shield-check`, `#shield-lock`)
- Biểu tượng Quả chuông (`#bell-fill`)
- Biểu tượng Mặt trời & Mặt trăng (`#sun-fill`, `#moon-fill`)
- Biểu tượng Ngôi sao đánh giá (`#star-fill`, `#star`)
- Biểu tượng Trạng thái (`#clock-history`, `#check-circle-fill`, `#exclamation-triangle-fill`, `#x-circle-fill`)
- Biểu tượng Gửi tin nhắn (`#send-fill`)
- Biểu tượng File đính kèm (`#paperclip`, `#file-earmark-text`, `#download`)

---

## 6. MÃ NGUỒN CỤ THỂ CÁC RAZOR VIEW BLUEPRINTS CHỦ CHỐT

### 6.1. Master Layout: `src/ItTms.Web/Views/Shared/_Layout.cshtml`
```html
@using ItTms.Domain.Enums
<!DOCTYPE html>
<html lang="vi" data-bs-theme="light">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - IT-TMS Hỗ trợ Kỹ thuật</title>
    
    <!-- Ngăn ngừa FOUC Theme Dark/Light -->
    <script>
        (function() {
            const theme = localStorage.getItem('it-tms-theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
            document.documentElement.setAttribute('data-bs-theme', theme);
        })();
    </script>

    <!-- Bootstrap 5 CSS Cục bộ (Zero CDN) -->
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    @await RenderSectionAsync("Styles", required: false)
</head>
<body class="d-flex flex-column min-vh-100 bg-body-tertiary">
    <!-- Nạp Thư viện SVG Sprite -->
    @await Html.PartialAsync("_SvgIcons")

    <!-- HEADER & NAVBAR -->
    <header class="sticky-top shadow-sm">
        <nav class="navbar navbar-expand-lg bg-body border-bottom">
            <div class="container-fluid px-3 px-lg-4">
                <!-- Brand Logo -->
                <a class="navbar-brand d-flex align-items-center fw-bold text-primary" asp-area="" asp-controller="Home" asp-action="Index">
                    <svg class="bi me-2 text-primary" width="28" height="28"><use xlink:href="#shield-check"/></svg>
                    <span>IT-TMS</span>
                    <span class="badge bg-primary-subtle text-primary border border-primary-subtle ms-2 fs-8">.NET 9</span>
                </a>

                <!-- Mobile Toggle Button -->
                <button class="navbar-toggler border-0" type="button" data-bs-toggle="collapse" data-bs-target="#navbarContent"
                        aria-controls="navbarContent" aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon"></span>
                </button>

                <!-- Navigation Content -->
                <div class="collapse navbar-collapse" id="navbarContent">
                    <!-- Left Menu (Role-Based Dynamic Navigation) -->
                    <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                        @if (User.Identity?.IsAuthenticated == true)
                        {
                            @if (User.IsInRole(RoleNames.User))
                            {
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="User" asp-controller="Dashboard" asp-action="Index">
                                        <svg class="bi me-1" width="16" height="16"><use xlink:href="#speedometer2"/></svg>Bàn làm việc
                                    </a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="User" asp-controller="Ticket" asp-action="Index">
                                        <svg class="bi me-1" width="16" height="16"><use xlink:href="#ticket-perforated"/></svg>Yêu cầu của tôi
                                    </a>
                                </li>
                                <li class="nav-item ms-lg-2">
                                    <a class="btn btn-sm btn-primary d-inline-flex align-items-center mt-1 mt-lg-0" 
                                       asp-area="User" asp-controller="Ticket" asp-action="Create">
                                        <svg class="bi me-1" width="16" height="16"><use xlink:href="#plus-circle"/></svg>Tạo Ticket mới
                                    </a>
                                </li>
                            }
                            @if (User.IsInRole(RoleNames.Staff))
                            {
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Staff" asp-controller="Dashboard" asp-action="Index">Dashboard Kỹ thuật</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Staff" asp-controller="Ticket" asp-action="Assigned">Việc được giao</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Staff" asp-controller="Ticket" asp-action="History">Lịch sử xử lý</a>
                                </li>
                            }
                            @if (User.IsInRole(RoleNames.Leader))
                            {
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Leader" asp-controller="Dashboard" asp-action="Index">Tổng quan SLA</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Leader" asp-controller="Ticket" asp-action="NewQueue">
                                        Hàng đợi phân công <span id="navNewQueueBadge" class="badge rounded-pill bg-danger d-none">0</span>
                                    </a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Leader" asp-controller="Ticket" asp-action="Index">Tất cả yêu cầu</a>
                                </li>
                            }
                            @if (User.IsInRole(RoleNames.Admin))
                            {
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Admin" asp-controller="Dashboard" asp-action="Index">Dashboard Quản trị</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Admin" asp-controller="User" asp-action="Index">Người dùng</a>
                                </li>
                                <li class="nav-item">
                                    <a class="nav-link" asp-area="Admin" asp-controller="Audit" asp-action="Index">Nhật ký Hệ thống</a>
                                </li>
                            }
                        }
                    </ul>

                    <!-- Right Controls: Theme Toggle, Notifications, User Menu -->
                    <div class="d-flex align-items-center gap-2">
                        <!-- Theme Toggle Button -->
                        <button class="btn btn-outline-secondary btn-sm rounded-circle p-2 d-flex align-items-center justify-content-center" 
                                id="btnThemeToggle" title="Chuyển đổi giao diện Sáng/Tối" style="width: 36px; height: 36px;">
                            <svg class="bi" width="18" height="18" id="themeIcon"><use xlink:href="#moon-fill"/></svg>
                        </button>

                        @if (User.Identity?.IsAuthenticated == true)
                        {
                            <!-- Notification Bell Dropdown (SignalR Realtime) -->
                            <div class="dropdown">
                                <button class="btn btn-outline-secondary btn-sm rounded-circle position-relative p-2 d-flex align-items-center justify-content-center"
                                        id="notificationBellBtn" data-bs-toggle="dropdown" aria-expanded="false" style="width: 36px; height: 36px;">
                                    <svg class="bi" width="18" height="18"><use xlink:href="#bell-fill"/></svg>
                                    <span id="notificationCountBadge" class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger d-none">
                                        0
                                    </span>
                                </button>
                                <div class="dropdown-menu dropdown-menu-end shadow-lg p-0 border-0 rounded-3 mt-2" style="width: 340px;">
                                    <div class="d-flex justify-content-between align-items-center px-3 py-2 border-bottom bg-body-tertiary rounded-top-3">
                                        <h6 class="mb-0 fw-semibold fs-7">Thông báo mới</h6>
                                        <button class="btn btn-link btn-sm text-decoration-none p-0 fs-8" id="btnMarkAllAsRead">Đã đọc tất cả</button>
                                    </div>
                                    <div class="list-group list-group-flush overflow-auto" id="notificationList" style="max-height: 320px;">
                                        <div class="text-center py-4 text-muted fs-8" id="noNotificationPlaceholder">
                                            Chưa có thông báo nào mới
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- User Profile Dropdown -->
                            <div class="dropdown ms-2">
                                <a href="#" class="d-flex align-items-center text-decoration-none dropdown-toggle text-body" data-bs-toggle="dropdown" aria-expanded="false">
                                    <div class="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold me-2 fs-7" style="width: 34px; height: 34px;">
                                        @(User.Identity.Name?.Substring(0, 1).ToUpper() ?? "U")
                                    </div>
                                    <div class="d-none d-md-block text-start lh-1">
                                        <div class="fw-semibold fs-7">@User.Identity.Name</div>
                                        <small class="text-muted fs-8">@(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value)</small>
                                    </div>
                                </a>
                                <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0 rounded-3 mt-2">
                                    <li><h6 class="dropdown-header">Tài khoản: @User.Identity.Name</h6></li>
                                    <li><a class="dropdown-item fs-7" asp-area="Auth" asp-controller="Account" asp-action="ChangePassword">
                                        <svg class="bi me-2 text-muted" width="16" height="16"><use xlink:href="#key-fill"/></svg>Đổi mật khẩu
                                    </a></li>
                                    <li><hr class="dropdown-divider"></li>
                                    <li>
                                        <!-- Đăng xuất an toàn bằng POST Form chống CSRF -->
                                        <form asp-area="Auth" asp-controller="Account" asp-action="Logout" method="post" class="px-2">
                                            @Html.AntiForgeryToken()
                                            <button type="submit" class="btn btn-outline-danger btn-sm w-100 d-flex align-items-center justify-content-center">
                                                <svg class="bi me-2" width="16" height="16"><use xlink:href="#box-arrow-right"/></svg>Đăng xuất
                                            </button>
                                        </form>
                                    </li>
                                </ul>
                            </div>
                        }
                        else
                        {
                            <a class="btn btn-primary btn-sm px-3" asp-area="Auth" asp-controller="Account" asp-action="Login">Đăng nhập</a>
                        }
                    </div>
                </div>
            </div>
        </nav>
    </header>

    <!-- TOAST NOTIFICATION CONTAINER -->
    @await Html.PartialAsync("_ToastAlerts")

    <!-- MAIN BODY -->
    <main role="main" class="flex-grow-1 container-fluid px-3 px-lg-4 py-4">
        @RenderBody()
    </main>

    <!-- FOOTER -->
    <footer class="footer mt-auto py-3 bg-body border-top text-muted fs-8">
        <div class="container-fluid px-3 px-lg-4 d-flex justify-content-between align-items-center">
            <div>
                &copy; 2026 <strong>IT-TMS</strong> - Hệ thống Quản lý Yêu cầu Hỗ trợ Kỹ thuật IT (.NET 9 Enterprise)
            </div>
            <div>
                Thời gian máy chủ: <span id="serverTimeClock">@DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm") UTC</span>
            </div>
        </div>
    </footer>

    <!-- Scripts Cục bộ (Zero CDN) -->
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/lib/microsoft-signalr/signalr.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @if (User.Identity?.IsAuthenticated == true)
    {
        <script src="~/js/ticket-realtime.js" asp-append-version="true"></script>
    }
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

### 6.2. Màn hình Đăng nhập: `src/ItTms.Web/Areas/Auth/Views/Account/Login.cshtml`
```html
@model ItTms.Web.Models.ViewModels.LoginViewModel
@{
    ViewData["Title"] = "Đăng nhập";
    Layout = "_Layout";
}

<div class="row justify-content-center align-items-center min-vh-75 py-5">
    <div class="col-12 col-sm-10 col-md-8 col-lg-5 col-xl-4">
        <div class="card border-0 shadow rounded-4 overflow-hidden">
            <div class="card-body p-4 p-sm-5">
                <!-- Brand Header -->
                <div class="text-center mb-4">
                    <div class="d-inline-flex p-3 rounded-circle bg-primary-subtle text-primary mb-3">
                        <svg class="bi" width="36" height="36"><use xlink:href="#shield-lock"/></svg>
                    </div>
                    <h3 class="fw-bold mb-1">Đăng nhập IT-TMS</h3>
                    <p class="text-muted fs-7">Cổng tiếp nhận và xử lý sự cố kỹ thuật nội bộ</p>
                </div>

                <!-- Bảng 7.2 TC-01: Hiển thị lỗi tài khoản bị vô hiệu hóa rõ ràng -->
                @if (TempData["ErrorMessage"] != null || !ViewData.ModelState.IsValid)
                {
                    <div class="alert alert-danger d-flex align-items-start mb-4 border-0 shadow-sm rounded-3" role="alert">
                        <svg class="bi flex-shrink-0 me-2 mt-1" width="20" height="20"><use xlink:href="#exclamation-triangle-fill"/></svg>
                        <div>
                            @if (TempData["ErrorMessage"] != null)
                            {
                                <div class="fw-semibold">@TempData["ErrorMessage"]</div>
                            }
                            <div asp-validation-summary="All" class="text-danger small mb-0"></div>
                        </div>
                    </div>
                }

                <!-- Form Đăng nhập -->
                <form asp-area="Auth" asp-controller="Account" asp-action="Login" asp-route-returnurl="@ViewData["ReturnUrl"]" method="post">
                    @Html.AntiForgeryToken()

                    <div class="mb-3">
                        <label asp-for="Username" class="form-label fw-semibold fs-7">Tên đăng nhập</label>
                        <div class="input-group">
                            <span class="input-group-text bg-body-tertiary border-end-0"><svg class="bi text-muted" width="16" height="16"><use xlink:href="#person"/></svg></span>
                            <input asp-for="Username" class="form-control border-start-0" placeholder="Nhập username" autofocus required />
                        </div>
                        <span asp-validation-for="Username" class="text-danger fs-8"></span>
                    </div>

                    <div class="mb-3">
                        <label asp-for="Password" class="form-label fw-semibold fs-7">Mật khẩu</label>
                        <div class="input-group">
                            <span class="input-group-text bg-body-tertiary border-end-0"><svg class="bi text-muted" width="16" height="16"><use xlink:href="#key"/></svg></span>
                            <input asp-for="Password" type="password" class="form-control border-start-0" id="loginPassword" placeholder="Nhập mật khẩu" required />
                            <button class="btn btn-outline-secondary border-start-0" type="button" id="btnTogglePassword">
                                <svg class="bi" width="16" height="16" id="eyeIcon"><use xlink:href="#eye"/></svg>
                            </button>
                        </div>
                        <span asp-validation-for="Password" class="text-danger fs-8"></span>
                    </div>

                    <div class="d-flex justify-content-between align-items-center mb-4">
                        <div class="form-check">
                            <input asp-for="RememberMe" class="form-check-input" type="checkbox" id="rememberMeCheck" />
                            <label class="form-check-label fs-7 text-muted" for="rememberMeCheck">Ghi nhớ đăng nhập</label>
                        </div>
                    </div>

                    <button type="submit" class="btn btn-primary btn-lg w-100 fw-semibold shadow-sm">
                        Đăng nhập
                    </button>
                </form>

                <!-- Bảng Tài khoản Mẫu phục vụ Nghiệm thu Bảng 7.2 -->
                <div class="mt-4 pt-4 border-top">
                    <div class="text-muted fs-8 text-center mb-2 fw-semibold">Tài khoản kiểm thử nghiệm thu (Bảng 7.2):</div>
                    <div class="d-flex flex-wrap gap-1 justify-content-center">
                        <button type="button" class="btn btn-sm btn-outline-secondary fs-8 quick-fill-btn" data-user="admin" data-pass="Password123!">Admin</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary fs-8 quick-fill-btn" data-user="leader" data-pass="Password123!">Leader</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary fs-8 quick-fill-btn" data-user="staff" data-pass="Password123!">Staff</button>
                        <button type="button" class="btn btn-sm btn-outline-secondary fs-8 quick-fill-btn" data-user="user" data-pass="Password123!">User</button>
                        <button type="button" class="btn btn-sm btn-outline-danger fs-8 quick-fill-btn" data-user="inactive_user" data-pass="Password123!" title="Kiểm thử Kịch bản 1">Inactive (TC-01)</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
        // Toggle ẩn/hiện mật khẩu
        document.getElementById('btnTogglePassword')?.addEventListener('click', function() {
            const pwd = document.getElementById('loginPassword');
            const icon = document.getElementById('eyeIcon').querySelector('use');
            if (pwd.type === 'password') {
                pwd.type = 'text';
                icon.setAttribute('xlink:href', '#eye-slash');
            } else {
                pwd.type = 'password';
                icon.setAttribute('xlink:href', '#eye');
            }
        });

        // Quick fill tài khoản mẫu
        document.querySelectorAll('.quick-fill-btn').forEach(btn => {
            btn.addEventListener('click', function() {
                document.getElementById('Username').value = this.dataset.user;
                document.getElementById('loginPassword').value = this.dataset.pass;
            });
        });
    </script>
}
```

---

### 6.3. Tạo Ticket mới & Xem trước SLA: `src/ItTms.Web/Areas/User/Views/Ticket/Create.cshtml`
```html
@model ItTms.Web.Models.ViewModels.CreateTicketViewModel
@using ItTms.Domain.Enums
@{
    ViewData["Title"] = "Tạo Yêu cầu Hỗ trợ Mới";
    Layout = "_Layout";
}

<div class="row justify-content-center">
    <div class="col-12 col-lg-8">
        <div class="card border-0 shadow-sm rounded-4">
            <div class="card-header bg-body border-bottom py-3 px-4">
                <div class="d-flex align-items-center">
                    <a asp-action="Index" class="btn btn-sm btn-outline-secondary rounded-circle me-3" style="width: 32px; height: 32px; display: inline-flex; align-items: center; justify-content: center;">
                        <svg class="bi" width="16" height="16"><use xlink:href="#arrow-left"/></svg>
                    </a>
                    <div>
                        <h5 class="mb-0 fw-bold">Tạo Yêu cầu Hỗ trợ Kỹ thuật</h5>
                        <small class="text-muted">Nhập thông tin sự cố để đội ngũ IT kịp thời tiếp nhận và xử lý</small>
                    </div>
                </div>
            </div>
            <div class="card-body p-4">
                <form asp-action="Create" method="post" enctype="multipart/form-data" id="createTicketForm">
                    @Html.AntiForgeryToken()

                    <!-- Tiêu đề sự cố -->
                    <div class="mb-3">
                        <label asp-for="Title" class="form-label fw-semibold">Tiêu đề yêu cầu <span class="text-danger">*</span></label>
                        <input asp-for="Title" class="form-control form-control-lg fs-6" placeholder="Ví dụ: Máy tính phòng Kế toán không kết nối được máy in mạng" maxlength="150" required />
                        <span asp-validation-for="Title" class="text-danger fs-8"></span>
                    </div>

                    <div class="row g-3 mb-3">
                        <!-- Danh mục sự cố -->
                        <div class="col-12 col-md-6">
                            <label asp-for="Category" class="form-label fw-semibold">Danh mục sự cố <span class="text-danger">*</span></label>
                            <select asp-for="Category" class="form-select" asp-items="Html.GetEnumSelectList<TicketCategory>()">
                            </select>
                            <span asp-validation-for="Category" class="text-danger fs-8"></span>
                        </div>

                        <!-- Mức độ ưu tiên -->
                        <div class="col-12 col-md-6">
                            <label asp-for="Priority" class="form-label fw-semibold">Mức độ ưu tiên <span class="text-danger">*</span></label>
                            <select asp-for="Priority" class="form-select" id="ticketPrioritySelect" asp-items="Html.GetEnumSelectList<TicketPriority>()">
                            </select>
                            <span asp-validation-for="Priority" class="text-danger fs-8"></span>
                        </div>
                    </div>

                    <!-- WIDGET DỰ ĐOÁN HẠN SLA THÔNG MINH (Interactive Real-time SLA DueAt Preview) -->
                    <div class="card mb-4 border-0 bg-body-tertiary rounded-3" id="slaPreviewCard">
                        <div class="card-body py-3 px-4 d-flex align-items-center">
                            <div class="rounded-circle p-2 me-3 d-flex align-items-center justify-content-center text-white" id="slaIconBg" style="width: 44px; height: 44px; background-color: #0d6efd;">
                                <svg class="bi" width="22" height="22"><use xlink:href="#clock-history"/></svg>
                            </div>
                            <div class="flex-grow-1">
                                <div class="d-flex align-items-center gap-2">
                                    <span class="fw-semibold fs-7" id="slaLevelName">Mức độ: Trung bình (Medium)</span>
                                    <span class="badge bg-primary" id="slaTimeSpanBadge">Cam kết: 24 giờ</span>
                                </div>
                                <div class="text-muted fs-8 mt-1" id="slaDueAtText">
                                    Dự kiến hoàn thành trước: <strong>Đang tính toán...</strong>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Mô tả chi tiết -->
                    <div class="mb-3">
                        <label asp-for="Description" class="form-label fw-semibold">Mô tả chi tiết sự cố <span class="text-danger">*</span></label>
                        <textarea asp-for="Description" class="form-control" rows="5" placeholder="Mô tả cụ thể: Dấu hiệu sự cố, các phần mềm đang chạy, thao tác dẫn đến lỗi, thông báo lỗi xuất hiện..." required></textarea>
                        <span asp-validation-for="Description" class="text-danger fs-8"></span>
                    </div>

                    <!-- Upload tệp đính kèm (EC10: Max 16MB) -->
                    <div class="mb-4">
                        <label class="form-label fw-semibold">Tệp đính kèm minh họa (Hình ảnh, tài liệu sự cố)</label>
                        <input type="file" name="attachments" id="fileAttachments" class="form-control" multiple accept=".jpg,.jpeg,.png,.pdf,.docx,.xlsx,.txt" />
                        <div class="form-text fs-8">Hỗ trợ các định dạng: PNG, JPG, PDF, DOCX, XLSX. Dung lượng tối đa: 16MB/tệp.</div>
                        <div id="fileListPreview" class="mt-2 d-flex flex-wrap gap-2"></div>
                    </div>

                    <!-- Nút thao tác -->
                    <div class="d-flex justify-content-end gap-2 pt-3 border-top">
                        <a asp-action="Index" class="btn btn-outline-secondary px-4">Hủy</a>
                        <button type="submit" class="btn btn-primary px-4 fw-semibold" id="btnSubmitTicket">
                            <svg class="bi me-1" width="16" height="16"><use xlink:href="#send-fill"/></svg> Gửi yêu cầu hỗ trợ
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script>
        // =========================================================================
        // SCRIPT TÍNH TOÁN VÀ DỰ ĐOÁN HẠN SLA THEO THỜI GIAN THỰC
        // Critical: 1h | High: 4h | Medium: 24h | Low: 72h
        // =========================================================================
        function updateSlaPreview() {
            const priorityVal = parseInt(document.getElementById('ticketPrioritySelect').value);
            let hours = 24;
            let levelName = "Trung bình (Medium)";
            let badgeClass = "bg-primary";
            let iconBg = "#0d6efd";

            switch(priorityVal) {
                case 4: // Critical
                    hours = 1;
                    levelName = "Khẩn cấp (Critical)";
                    badgeClass = "bg-danger";
                    iconBg = "#dc3545";
                    break;
                case 3: // High
                    hours = 4;
                    levelName = "Cao (High)";
                    badgeClass = "bg-warning text-dark";
                    iconBg = "#ffc107";
                    break;
                case 2: // Medium
                    hours = 24;
                    levelName = "Trung bình (Medium)";
                    badgeClass = "bg-primary";
                    iconBg = "#0d6efd";
                    break;
                case 1: // Low
                    hours = 72;
                    levelName = "Thấp (Low)";
                    badgeClass = "bg-secondary";
                    iconBg = "#6c757d";
                    break;
            }

            const targetTime = new Date(Date.now() + hours * 3600 * 1000);
            const formattedTime = targetTime.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) + ' - ' + targetTime.toLocaleDateString('vi-VN');

            document.getElementById('slaLevelName').innerText = "Mức độ: " + levelName;
            const badge = document.getElementById('slaTimeSpanBadge');
            badge.className = "badge " + badgeClass;
            badge.innerText = "Cam kết: " + hours + " giờ";
            document.getElementById('slaIconBg').style.backgroundColor = iconBg;
            document.getElementById('slaDueAtText').innerHTML = "Dự kiến hoàn thành trước: <strong>" + formattedTime + "</strong>";
        }

        document.getElementById('ticketPrioritySelect').addEventListener('change', updateSlaPreview);
        updateSlaPreview();

        // Kiểm tra dung lượng file tải lên (Tối đa 16MB)
        document.getElementById('fileAttachments').addEventListener('change', function(e) {
            const preview = document.getElementById('fileListPreview');
            preview.innerHTML = '';
            const maxBytes = 16 * 1024 * 1024; // 16MB

            for (const file of this.files) {
                if (file.size > maxBytes) {
                    alert(`Tệp "${file.name}" vượt quá dung lượng tối đa 16MB.`);
                    this.value = '';
                    preview.innerHTML = '';
                    return;
                }
                const badge = document.createElement('span');
                badge.className = 'badge bg-body-secondary text-body border p-2 fs-8';
                badge.innerText = `${file.name} (${(file.size / (1024*1024)).toFixed(2)} MB)`;
                preview.appendChild(badge);
            }
        });
    </script>
}
```

---

### 6.4. Trang Chi tiết Ticket & Khung Chat SignalR: `src/ItTms.Web/Areas/User/Views/Ticket/Details.cshtml`
```html
@model ItTms.Web.Models.ViewModels.TicketDetailViewModel
@using ItTms.Domain.Entities
@{
    ViewData["Title"] = $"Yêu cầu #{Model.Ticket.Id} - {Model.Ticket.Title}";
    Layout = "_Layout";
}

<!-- TIÊU ĐỀ & THANH ĐIỀU HƯỚNG -->
<div class="d-flex flex-wrap justify-content-between align-items-center mb-4 gap-2">
    <div class="d-flex align-items-center">
        <a asp-action="Index" class="btn btn-outline-secondary btn-sm me-3 rounded-circle" style="width: 32px; height: 32px; display: inline-flex; align-items: center; justify-content: center;">
            <svg class="bi" width="16" height="16"><use xlink:href="#arrow-left"/></svg>
        </a>
        <div>
            <h4 class="mb-0 fw-bold">Yêu cầu #@Model.Ticket.Id: @Model.Ticket.Title</h4>
            <div class="text-muted fs-8 mt-1">
                Tạo bởi <strong>@Model.Ticket.Creator.FullName</strong> vào lúc @Model.Ticket.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            </div>
        </div>
    </div>
    <div class="d-flex align-items-center gap-2">
        <partial name="_StatusBadge" model="Model.Ticket.Status" />
        <partial name="_SlaBadge" model="Model.Ticket" />
    </div>
</div>

<!-- KHỐI HÀNH ĐỘNG NGHIỆM THU CHO TICKET RESOLVED (BẢNG 7.2 TC-09) -->
@if (Model.Ticket.StatusId == TicketStatus.Resolved)
{
    <div class="alert alert-success border-0 shadow-sm rounded-4 p-4 mb-4 d-flex flex-wrap justify-content-between align-items-center gap-3">
        <div>
            <h5 class="fw-bold mb-1"><svg class="bi me-2 text-success" width="22" height="22"><use xlink:href="#check-circle-fill"/></svg>Kỹ thuật viên đã xử lý hoàn tất sự cố này!</h5>
            <p class="mb-0 fs-7 text-body-secondary">Vui lòng kiểm tra lại thiết bị/hệ thống và đánh giá mức độ hài lòng để chính thức đóng yêu cầu.</p>
        </div>
        <div class="d-flex gap-2">
            <button class="btn btn-warning text-dark fw-semibold" data-bs-toggle="modal" data-bs-target="#reopenModal">
                <svg class="bi me-1" width="16" height="16"><use xlink:href="#arrow-counterclockwise"/></svg> Yêu cầu xử lý lại
            </button>
            <button class="btn btn-success fw-semibold px-4 shadow-sm" data-bs-toggle="modal" data-bs-target="#feedbackModal">
                <svg class="bi me-1" width="16" height="16"><use xlink:href="#star-fill"/></svg> Đóng & Đánh giá dịch vụ
            </button>
        </div>
    </div>
}

<div class="row g-4">
    <!-- CỘT TRÁI: NỘI DUNG SỰ CỐ, TỆP ĐÍNH KÈM & LỊCH SỬ (Col 7) -->
    <div class="col-12 col-lg-7">
        <!-- Thông tin mô tả -->
        <div class="card border-0 shadow-sm rounded-4 mb-4">
            <div class="card-header bg-body border-bottom py-3 px-4">
                <h6 class="mb-0 fw-bold">Mô tả sự cố</h6>
            </div>
            <div class="card-body p-4">
                <p class="text-body mb-0" style="white-space: pre-line;">@Model.Ticket.Description</p>
            </div>
        </div>

        <!-- Tệp đính kèm -->
        @if (Model.Ticket.Attachments.Any())
        {
            <div class="card border-0 shadow-sm rounded-4 mb-4">
                <div class="card-header bg-body border-bottom py-3 px-4">
                    <h6 class="mb-0 fw-bold">Tệp đính kèm (@Model.Ticket.Attachments.Count)</h6>
                </div>
                <div class="card-body p-4">
                    <div class="list-group list-group-flush">
                        @foreach (var file in Model.Ticket.Attachments)
                        {
                            <div class="list-group-item px-0 d-flex justify-content-between align-items-center">
                                <div class="d-flex align-items-center">
                                    <svg class="bi me-2 text-primary" width="20" height="20"><use xlink:href="#file-earmark-text"/></svg>
                                    <div>
                                        <div class="fw-semibold fs-7">@file.OriginalFileName</div>
                                        <small class="text-muted fs-8">@((file.FileSize / 1024.0 / 1024.0).ToString("0.00")) MB</small>
                                    </div>
                                </div>
                                <a href="@Url.Action("DownloadAttachment", "Ticket", new { id = file.Id })" class="btn btn-sm btn-outline-secondary">
                                    <svg class="bi" width="14" height="14"><use xlink:href="#download"/></svg> Tải về
                                </a>
                            </div>
                        }
                    </div>
                </div>
            </div>
        }

        <!-- Đánh giá đã gửi (Nếu đã đóng) -->
        @if (Model.Ticket.Feedback != null)
        {
            <div class="card border-0 shadow-sm rounded-4 bg-body-tertiary mb-4">
                <div class="card-body p-4">
                    <h6 class="fw-bold mb-2">Đánh giá từ người dùng:</h6>
                    <div class="d-flex align-items-center mb-2">
                        @for (int i = 1; i <= 5; i++)
                        {
                            <svg class="bi me-1 @(i <= Model.Ticket.Feedback.Rating ? "text-warning" : "text-secondary")" width="18" height="18">
                                <use xlink:href="@(i <= Model.Ticket.Feedback.Rating ? "#star-fill" : "#star")"/>
                            </svg>
                        }
                        <span class="ms-2 fw-semibold fs-7">(@Model.Ticket.Feedback.Rating / 5 sao)</span>
                    </div>
                    @if (!string.IsNullOrWhiteSpace(Model.Ticket.Feedback.Comment))
                    {
                        <p class="text-muted fs-7 mb-0 fst-italic">"@Model.Ticket.Feedback.Comment"</p>
                    }
                </div>
            </div>
        }
    </div>

    <!-- CỘT PHẢI: KHUNG TRAO ĐỔI SIGNALR THỜI GIAN THỰC (Col 5) -->
    <div class="col-12 col-lg-5">
        <div class="card border-0 shadow-sm rounded-4 h-100 d-flex flex-column">
            <div class="card-header bg-body border-bottom py-3 px-4 d-flex justify-content-between align-items-center">
                <h6 class="mb-0 fw-bold d-flex align-items-center">
                    <svg class="bi me-2 text-primary" width="18" height="18"><use xlink:href="#chat-dots"/></svg>
                    Trao đổi trực tiếp
                </h6>
                <span class="badge bg-success-subtle text-success border border-success-subtle fs-8" id="liveIndicator">
                    <span class="spinner-grow spinner-grow-sm me-1" role="status"></span>SignalR Trực tuyến
                </span>
            </div>

            <!-- Khung danh sách tin nhắn cuộn -->
            <div class="card-body p-3 overflow-auto flex-grow-1" id="messageContainer" style="max-height: 480px; min-height: 360px;">
                @foreach (var c in Model.Ticket.Comments.OrderBy(x => x.CreatedAt))
                {
                    var isMe = c.UserId == Model.CurrentUserId;
                    <div class="d-flex flex-column mb-3 @(isMe ? "align-items-end" : "align-items-start")">
                        <div class="d-flex align-items-center gap-1 mb-1 fs-8 text-muted">
                            <span class="fw-semibold">@c.User.FullName</span>
                            <span>&bull;</span>
                            <span>@c.CreatedAt.ToString("HH:mm")</span>
                        </div>
                        <div class="p-3 rounded-4 fs-7 @(isMe ? "bg-primary text-white" : "bg-body-secondary text-body")" style="max-width: 85%;">
                            @c.Content
                        </div>
                    </div>
                }
            </div>

            <!-- Khung nhập tin nhắn (BẢNG 7.2 TC-08: CHẶN KHI CANCOMMENT == FALSE) -->
            <div class="card-footer bg-body border-top p-3">
                @if (Model.Ticket.CanComment())
                {
                    <form id="commentForm" class="d-flex gap-2">
                        <input type="hidden" id="ticketIdHidden" value="@Model.Ticket.Id" />
                        <input type="text" id="commentInput" class="form-control rounded-pill px-3" placeholder="Nhập phản hồi trao đổi..." autocomplete="off" required />
                        <button type="submit" class="btn btn-primary rounded-circle d-flex align-items-center justify-content-center flex-shrink-0" style="width: 40px; height: 40px;" id="btnSendComment">
                            <svg class="bi" width="16" height="16"><use xlink:href="#send-fill"/></svg>
                        </button>
                    </form>
                }
                else
                {
                    <!-- Hiển thị khóa chức năng bình luận khi Resolved/Closed/Rejected -->
                    <div class="alert alert-secondary py-2 px-3 mb-0 text-center rounded-3 fs-8 border-0" role="alert">
                        <svg class="bi me-1" width="14" height="14"><use xlink:href="#lock-fill"/></svg>
                        Yêu cầu này đã kết thúc hoặc bị khóa. Chức năng bình luận đã đóng.
                    </div>
                }
            </div>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- MODAL ĐÁNH GIÁ & ĐÓNG TICKET (BẢNG 7.2 TC-09)                              -->
<!-- ========================================================================= -->
<div class="modal fade" id="feedbackModal" tabindex="-1" aria-labelledby="feedbackModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg rounded-4">
            <form asp-action="CloseAndRate" method="post">
                @Html.AntiForgeryToken()
                <input type="hidden" name="ticketId" value="@Model.Ticket.Id" />
                <input type="hidden" name="rating" id="selectedRating" value="5" />

                <div class="modal-header border-bottom py-3 px-4">
                    <h5 class="modal-title fw-bold" id="feedbackModalLabel">Đánh giá chất lượng hỗ trợ</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body p-4 text-center">
                    <p class="text-muted fs-7 mb-3">Bạn cảm thấy dịch vụ xử lý sự cố #[@Model.Ticket.Id] như thế nào?</p>

                    <!-- Star Rating Interactive Widget -->
                    <div class="d-flex justify-content-center gap-2 mb-2" id="starContainer">
                        @for (int i = 1; i <= 5; i++)
                        {
                            <span class="star-rating-item fs-2 text-warning cursor-pointer" data-star="@i" style="cursor: pointer;">
                                ★
                            </span>
                        }
                    </div>
                    <div class="fw-bold fs-7 text-primary mb-4" id="ratingTextDesc">5 sao - Rất hài lòng</div>

                    <div class="text-start mb-3">
                        <label class="form-label fw-semibold fs-7">Ý kiến đóng góp thêm (Không bắt buộc)</label>
                        <textarea name="comment" class="form-control" rows="3" placeholder="Nhận xét về thái độ, thời gian và giải pháp của kỹ thuật viên..."></textarea>
                    </div>
                </div>
                <div class="modal-footer border-top py-3 px-4">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Đóng</button>
                    <button type="submit" class="btn btn-success fw-semibold px-4">Gửi đánh giá & Hoàn tất</button>
                </div>
            </form>
        </div>
    </div>
</div>

<!-- MODAL YÊU CẦU XỬ LÝ LẠI (REOPEN) -->
<div class="modal fade" id="reopenModal" tabindex="-1" aria-labelledby="reopenModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg rounded-4">
            <form asp-action="Reopen" method="post">
                @Html.AntiForgeryToken()
                <input type="hidden" name="ticketId" value="@Model.Ticket.Id" />
                <div class="modal-header border-bottom py-3 px-4">
                    <h5 class="modal-title fw-bold text-warning" id="reopenModalLabel">Yêu cầu xử lý lại sự cố</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body p-4">
                    <p class="text-muted fs-7">Nếu sự cố vẫn chưa được khắc phục triệt để, vui lòng cho kỹ thuật viên biết điểm chưa đạt:</p>
                    <div class="mb-3">
                        <label class="form-label fw-semibold fs-7">Lý do yêu cầu mở lại <span class="text-danger">*</span></label>
                        <textarea name="reason" class="form-control" rows="3" placeholder="Mô tả cụ thể sự cố vẫn còn tiếp diễn..." required></textarea>
                    </div>
                </div>
                <div class="modal-footer border-top py-3 px-4">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Hủy</button>
                    <button type="submit" class="btn btn-warning text-dark fw-semibold px-4">Mở lại ticket</button>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <script>
        // Star Rating Selection JS
        const starItems = document.querySelectorAll('.star-rating-item');
        const ratingInput = document.getElementById('selectedRating');
        const ratingText = document.getElementById('ratingTextDesc');
        const ratingLabels = {
            1: "1 sao - Rất không hài lòng",
            2: "2 sao - Không hài lòng",
            3: "3 sao - Bình thường",
            4: "4 sao - Hài lòng",
            5: "5 sao - Rất hài lòng"
        };

        starItems.forEach(star => {
            star.addEventListener('click', function() {
                const val = parseInt(this.dataset.star);
                ratingInput.value = val;
                ratingText.innerText = ratingLabels[val];
                starItems.forEach((s, idx) => {
                    s.innerText = idx < val ? '★' : '☆';
                });
            });
        });

        // Tự động cuộn xuống đáy khung chat khi nạp trang
        const msgContainer = document.getElementById('messageContainer');
        if (msgContainer) msgContainer.scrollTop = msgContainer.scrollHeight;
    </script>
}
```

---

### 6.5. Màn hình Lỗi Phân quyền: `src/ItTms.Web/Views/Shared/AccessDenied.cshtml`
```html
@{
    ViewData["Title"] = "Từ chối Truy cập (403 Forbidden)";
    Layout = "_Layout";
}

<div class="row justify-content-center align-items-center min-vh-75 py-5 text-center">
    <div class="col-12 col-md-8 col-lg-6">
        <div class="card border-0 shadow-sm rounded-4 p-5">
            <div class="mb-4">
                <div class="d-inline-flex p-4 rounded-circle bg-danger-subtle text-danger mb-3">
                    <svg class="bi" width="56" height="56"><use xlink:href="#shield-slash"/></svg>
                </div>
                <h1 class="display-5 fw-bold text-danger mb-2">403</h1>
                <h3 class="fw-bold mb-2">Truy cập bị từ chối!</h3>
                <p class="text-muted fs-6 mb-4">
                    Tài khoản của bạn không có đủ thẩm quyền để truy cập vào tài nguyên hoặc chức năng này (Quy định phân quyền RBAC & Kiểm soát an ninh BOLA).
                </p>
            </div>
            <div class="d-flex justify-content-center gap-3">
                <a href="javascript:history.back()" class="btn btn-outline-secondary px-4 fw-semibold">
                    <svg class="bi me-1" width="16" height="16"><use xlink:href="#arrow-left"/></svg> Quay lại
                </a>
                <a asp-area="" asp-controller="Home" asp-action="Index" class="btn btn-primary px-4 fw-semibold">
                    Về Trang chủ
                </a>
            </div>
        </div>
    </div>
</div>
```

---

### 6.6. File Client Script Realtime: `src/ItTms.Web/wwwroot/js/ticket-realtime.js`
```javascript
/**
 * ticket-realtime.js: Xử lý giao tiếp hai chiều SignalR WebSocket
 * Tuyệt đối không sử dụng Anti-Pattern setInterval Polling 3s.
 */
document.addEventListener('DOMContentLoaded', function () {
    const ticketIdInput = document.getElementById('ticketIdHidden');
    const commentForm = document.getElementById('commentForm');
    const commentInput = document.getElementById('commentInput');
    const messageContainer = document.getElementById('messageContainer');
    const bellBadge = document.getElementById('notificationCountBadge');
    const notifList = document.getElementById('notificationList');

    // 1. Khởi tạo kết nối SignalR Hub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/ticket")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // 2. Lắng nghe thông báo cá nhân (Notification Bell Event)
    connection.on("ReceiveNotification", function (notif) {
        if (bellBadge) {
            let current = parseInt(bellBadge.innerText) || 0;
            current++;
            bellBadge.innerText = current;
            bellBadge.classList.remove('d-none');
        }

        // Chèn vào dropdown thông báo
        if (notifList) {
            const noPlaceholder = document.getElementById('noNotificationPlaceholder');
            if (noPlaceholder) noPlaceholder.remove();

            const item = document.createElement('a');
            item.href = notif.link || "#";
            item.className = "list-group-item list-group-item-action px-3 py-2 border-0 border-bottom fs-8";
            item.innerHTML = `
                <div class="d-flex w-100 justify-content-between">
                    <strong class="text-primary mb-1">Thông báo mới</strong>
                    <small class="text-muted">Vừa xong</small>
                </div>
                <div class="text-body">${notif.message}</div>
            `;
            notifList.insertBefore(item, notifList.firstChild);
        }
    });

    connection.on("UpdateUnreadNotificationCount", function (count) {
        if (bellBadge) {
            if (count > 0) {
                bellBadge.innerText = count;
                bellBadge.classList.remove('d-none');
            } else {
                bellBadge.classList.add('d-none');
            }
        }
    });

    // 3. Nếu đang ở trang chi tiết Ticket: Tham gia nhóm ticket_{id}
    if (ticketIdInput) {
        const ticketId = parseInt(ticketIdInput.value);

        connection.on("ReceiveComment", function (comment) {
            if (!messageContainer) return;

            const isMe = comment.sender === document.body.dataset.currentUser;
            const msgBox = document.createElement('div');
            msgBox.className = `d-flex flex-column mb-3 ${isMe ? 'align-items-end' : 'align-items-start'}`;
            msgBox.innerHTML = `
                <div class="d-flex align-items-center gap-1 mb-1 fs-8 text-muted">
                    <span class="fw-semibold">${comment.sender}</span>
                    <span>&bull;</span>
                    <span>${new Date(comment.createdAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
                </div>
                <div class="p-3 rounded-4 fs-7 ${isMe ? 'bg-primary text-white' : 'bg-body-secondary text-body'}" style="max-width: 85%;">
                    ${comment.content}
                </div>
            `;
            messageContainer.appendChild(msgBox);
            messageContainer.scrollTop = messageContainer.scrollHeight;
        });

        connection.on("TicketStatusUpdated", function (data) {
            // Cập nhật huy hiệu trạng thái trên UI theo thời gian thực
            const statusBadge = document.getElementById('ticketStatusBadge');
            if (statusBadge && data.ticketId === ticketId) {
                statusBadge.className = `badge ${data.colorClass}`;
                statusBadge.innerText = data.label;
            }
        });
    }

    // 4. Bắt đầu kết nối
    connection.start().then(function () {
        console.log("✅ SignalR TicketHub connected.");
        if (ticketIdInput) {
            const ticketId = parseInt(ticketIdInput.value);
            connection.invoke("JoinTicketGroup", ticketId).catch(err => console.error(err));
        }
    }).catch(function (err) {
        console.error("❌ SignalR Connection Error: ", err.toString());
    });

    // 5. Gửi bình luận qua SignalR + Form Submit
    if (commentForm && commentInput && ticketIdInput) {
        commentForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const text = commentInput.value.trim();
            if (!text) return;

            const ticketId = parseInt(ticketIdInput.value);
            connection.invoke("SendComment", ticketId, text).then(() => {
                commentInput.value = '';
            }).catch(err => {
                console.error("Gửi bình luận thất bại: ", err);
                // Fallback POST nếu websocket đứt đoạn
                commentForm.submit();
            });
        });
    }
});
```

---

## 7. BẢNG ĐỐI CHIẾU 10 KỊCH BẢN KIỂM THỬ BẢNG 7.2 & ĐÁP ỨNG UI/UX

| Mã TC | Kịch bản Bảng 7.2 | Cơ chế Giao diện (UI/UX Handling) | View / Partial phụ trách |
|:---:|---|---|---|
| **TC-01** | Đăng nhập tài khoản bị vô hiệu hóa | Hiển thị Alert đỏ thông báo rõ ràng "Tài khoản đã bị vô hiệu hóa", không tạo Cookie | `Areas/Auth/Views/Account/Login.cshtml` |
| **TC-02** | User truy cập trang Leader | Chặn tại `AuthorizeRolesAttribute`, trả về giao diện mã lỗi 403 Forbidden | `Views/Shared/AccessDenied.cshtml` |
| **TC-03** | User tạo ticket mới | Form tính SLA DueAt Preview động theo thời gian thực, gửi tín hiệu chuông SignalR cho Leader | `Areas/User/Views/Ticket/Create.cshtml` |
| **TC-04** | Leader phân công ticket cho Staff | `AssignModal` kèm hiển thị tải công việc Staff, chuyển trạng thái và phát SignalR | `Areas/Leader/Views/Ticket/NewQueue.cshtml` |
| **TC-05** | Staff đóng ticket đang New | Ẩn hoàn toàn nút "Đóng" trên giao diện Staff khi trạng thái là `New` | `Areas/Staff/Views/Ticket/Details.cshtml` |
| **TC-06** | Leader từ chối ticket InProgress | Ẩn nút "Từ chối" trên ticket `InProgress`, chỉ hiển thị trên ticket `New` trong hàng đợi | `Areas/Leader/Views/Ticket/NewQueue.cshtml` |
| **TC-07** | Staff mở ticket người khác | Chặn BOLA tại `AuthorizeTicketAccessAttribute`, trả về 403 Forbidden | `Views/Shared/AccessDenied.cshtml` |
| **TC-08** | Bình luận trên ticket Resolved | Ẩn ô nhập chat, hiển thị banner màu vàng xám: "Yêu cầu đã kết thúc, chức năng bình luận bị khóa" | `Areas/User/Views/Ticket/Details.cshtml` |
| **TC-09** | Đóng và đánh giá ticket Resolved | Banner nghiệm thu nổi bật, `FeedbackModal` với 5 ngôi sao tương tác và ô nhận xét | `Areas/User/Views/Ticket/Details.cshtml` |
| **TC-10** | Admin tự vô hiệu hóa chính mình | Nút Vô hiệu hóa bị `disabled` trên dòng tài khoản của chính Admin kèm tooltip giải thích | `Areas/Admin/Views/User/Index.cshtml` |

---

## 8. KẾT LUẬN & ĐỀ XUẤT TRIỂN KHAI

Bản vẽ thiết kế **Bootstrap 5 UI/UX & Razor Views Blueprint** đã giải quyết triệt để và toàn diện các yêu cầu kỹ thuật:
1. Đảm bảo cấu trúc phân tầng 5 Areas chuẩn mực, bao phủ toàn bộ 20 Use Cases.
2. Triệt tiêu hoàn toàn anti-pattern Polling 3s bằng luồng giao tiếp SignalR WebSocket hai chiều.
3. Đáp ứng 100% các tiêu chí hiển thị và xác thực của 10 kịch bản kiểm thử nghiệm thu Bảng 7.2.
4. Tương thích chuẩn WCAG 2.1 AA, Responsive đa thiết bị (Mobile/Tablet/Desktop), và hỗ trợ Dark/Light Theme tự động.
5. Vận hành 100% offline tự hành (zero CDN dependency) qua các tài nguyên tĩnh cục bộ trong `wwwroot/lib`.
