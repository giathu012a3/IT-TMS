# IT-TMS - Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT

> **Đồ án môn học**: Thiết kế Phần mềm Hướng Đối tượng (TKPMHĐT)  
> **Nền tảng**: .NET 9 (C# 13) - ASP.NET Core MVC  
> **Cơ sở dữ liệu**: SQLite (EF Core 9 - Tự động khởi tạo & Seed dữ liệu mẫu khi chạy)  
> **Thời gian thực**: SignalR WebSocket (Trao đổi tin nhắn & Thông báo đẩy tức thì)

---

## 📌 1. Giới thiệu tổng quan
**IT-TMS (IT Ticket Management System)** là giải pháp quản lý toàn diện quy trình tiếp nhận, phân công, xử lý và đánh giá chất lượng các sự cố/yêu cầu công nghệ thông tin trong nội bộ doanh nghiệp. Hệ thống được thiết kế hướng đối tượng với các mẫu thiết kế GoF chuẩn mực:
- **State Pattern**: Điều phối vòng đời Ticket qua 7 trạng thái nghiêm ngặt (`New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`).
- **Observer Pattern**: Bắn thông báo và ghi nhật ký hệ thống (`SystemLog`) tự động khi có sự kiện phát sinh.
- **Decorator / Action Filter**: Xác thực quyền truy cập phân tầng (RBAC) và kiểm tra quyền sở hữu ticket.
- **Real-time SignalR WebSocket**: Cập nhật trạng thái và khung chat realtime hai chiều (0s delay).

---

## ⚙️ 2. Yêu cầu môi trường
- **.NET 9 SDK** (phiên bản `9.0.x` trở lên) — [Tải tại đây](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Công cụ phát triển** (tùy chọn một trong các IDE sau):
  - **Visual Studio 2022** (bản 17.12 trở lên) có cài gói *ASP.NET and web development*.
  - **Visual Studio Code** (cài extension C# Dev Kit).
  - Hoặc chạy trực tiếp qua **Terminal / Command Prompt / PowerShell**.

---

## 🚀 3. Hướng dẫn cài đặt & Khởi chạy

### Cách 1: Khởi chạy bằng Visual Studio 2022 (Khuyên dùng)
1. Mở file giải pháp **`ItTms.sln`** bằng Visual Studio 2022.
2. Tại thanh công cụ trên cùng, chọn cấu hình khởi chạy **`http`** hoặc **`ItTms`**.
3. Nhấn phím **`F5`** (hoặc tổ hợp **`Ctrl + F5`**) để bắt đầu chạy.
4. Trình duyệt sẽ tự động mở trang web tại địa chỉ: **`http://localhost:5050`**.

---

### Cách 2: Khởi chạy bằng dòng lệnh (.NET CLI)
Mở cửa sổ **Terminal / PowerShell** tại thư mục gốc của dự án và chạy các lệnh sau:

```bash
# 1. Khôi phục các gói thư viện
dotnet restore

# 2. Biên dịch dự án
dotnet build

# 3. Khởi chạy ứng dụng Web
dotnet run --project src/ItTms
```

Sau khi terminal hiển thị:
```text
Now listening on: http://localhost:5050
Application started. Press Ctrl+C to shut down.
```
Bạn mở trình duyệt web bất kỳ (Chrome, Edge, Firefox) và truy cập: **`http://localhost:5050`**.

> [!NOTE]
> Database SQLite (`ittms.db`) cùng toàn bộ danh mục và các tài khoản mẫu đã được cấu hình tự động khởi tạo (Auto-migration & Seed data) ngay lần đầu tiên ứng dụng khởi động. Bạn **không cần cài đặt thêm SQL Server hay cấu hình connection string phức tạp**.

---

## 👥 4. Danh sách tài khoản thử nghiệm (Demo Accounts)

Hệ thống đã chuẩn bị sẵn các tài khoản tương ứng với từng vai trò trong quy trình:

| Vai trò | Tên đăng nhập | Mật khẩu | Chức năng chính |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `Admin@123` | Quản lý người dùng, phân quyền, xem nhật ký kiểm toán hệ thống (`Audit Log`) |
| **Trưởng nhóm IT (Leader)** | `leader` | `Leader@123` | Tiếp nhận yêu cầu mới, phân công kỹ thuật viên, từ chối yêu cầu, xem Dashboard báo cáo SLA |
| **Kỹ thuật viên 1 (Staff)** | `staff1` | `Staff@123` | Hàng đợi công việc cá nhân, cập nhật trạng thái (`InProgress`/`Waiting`/`Resolved`), chat hỗ trợ |
| **Kỹ thuật viên 2 (Staff)** | `staff2` | `Staff@123` | Hàng đợi công việc cá nhân, hỗ trợ sự cố phần mềm |
| **Người dùng 1 (User)** | `user1` | `User@123` | Tạo yêu cầu mới, theo dõi tiến độ, chat trao đổi, đóng ticket và đánh giá dịch vụ (1-5 sao) |
| **Người dùng 2 (User)** | `user2` | `User@123` | Nhân viên phòng ban gửi yêu cầu hỗ trợ |

---

## 📂 5. Cấu trúc thư mục mã nguồn

Dự án được tinh giản theo đúng chuẩn **1 Project ASP.NET Core MVC kinh điển**:

```text
IT-TMS/
├── ItTms.sln                     # File Solution chính của hệ thống
├── README.md                     # Tài liệu hướng dẫn cài đặt & vận hành
├── .gitignore                    # Bộ lọc loại bỏ file rác, file build tạm
└── src/
    └── ItTms/                    # Project ASP.NET Core MVC duy nhất
        ├── Controllers/          # Điều khiển luồng: Admin, Auth, Leader, Staff, Ticket...
        ├── Models/               # Thực thể nghiệp vụ, ViewModels & State Pattern (7 trạng thái)
        │   └── States/           # Cài đặt chi tiết 7 trạng thái của Ticket theo State Pattern
        ├── Views/                # Giao diện người dùng Razor Views (.cshtml) theo vai trò
        ├── Data/                 # ItTmsDbContext (EF Core) và bộ DataSeeder nạp dữ liệu mẫu
        ├── Hubs/                 # TicketHub quản lý kết nối SignalR WebSocket thời gian thực
        ├── Services/             # Dịch vụ mã hóa mật khẩu PBKDF2, thông báo, nhật ký
        ├── Filters/              # Bộ lọc phân quyền Claims-Based [AuthorizeRoles]
        ├── wwwroot/              # File tĩnh: CSS, JavaScript, icons, thư viện Bootstrap & SignalR
        ├── appsettings.json      # Cấu hình chuỗi kết nối và thông số ứng dụng
        ├── Program.cs            # Điểm khởi đầu cấu hình DI Container và Middleware pipeline
        └── ItTms.csproj          # File định nghĩa project và các package dependencies
```

---

## 🛠️ 6. Các tính năng nổi bật
1. **Phân quyền đa tầng (RBAC)**: Tự động điều hướng đúng trang Dashboard tùy theo vai trò sau khi đăng nhập (`/Admin`, `/Leader`, `/Staff`, `/User`).
2. **Khung trao đổi thời gian thực**: Trao đổi tin nhắn hai chiều giữa User và Staff qua SignalR WebSocket, hỗ trợ phím tắt `Enter` gửi tin và chống trùng lặp tin nhắn.
3. **Cảnh báo thông minh**: Hệ thống Floating Toast Cards góc trên bên phải hiển thị thông báo tức thì khi được giao việc hoặc cập nhật trạng thái.
4. **Vòng đời Ticket khép kín**: Ngăn chặn tuyệt đối các thao tác nhảy cóc sai quy trình nhờ áp dụng mẫu thiết kế State Pattern.
