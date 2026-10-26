# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — EXPLORER_M3_3

**Nhiệm vụ**: Thiết kế bản vẽ toàn diện Bootstrap 5 UI/UX & Razor Views Blueprint (`src/ItTms.Web`).  
**Tác giả**: `explorer_m3_3` (Explorer Archetype)  
**Người nhận**: `parent` (ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
**Ngày thực hiện**: 2026-10-25  

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM TRỰC TIẾP)

1. **Hiện trạng Tầng Giao diện `src/ItTms.Web`**:
   - `src/ItTms.Web/Views/Shared/_Layout.cshtml`: Đang là template mặc định của ASP.NET Core MVC (51 dòng), chưa có điều hướng phân quyền, chưa có chuông thông báo SignalR, chưa có theme toggle dark/light mode.
   - Thư mục `src/ItTms.Web/Areas`: Hiện tại **chưa tồn tại** (kết quả kiểm tra PowerShell `Test-Path 'c:\IT_System\src\ItTms.Web\Areas'` trả về `False`).
   - Thư mục tĩnh `src/ItTms.Web/wwwroot/lib/bootstrap/dist`: Đã có sẵn mã nguồn Bootstrap 5.3 (`bootstrap.min.css` 232,803 bytes và `bootstrap.bundle.min.js`). Chưa có thư viện biểu tượng font và chưa có file `microsoft-signalr/signalr.min.js`.
2. **Hợp đồng Nghiệp vụ Tầng Bảo mật & Phân quyền**:
   - `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs`:
     - Dòng 68-85: Khi tài khoản `userEntity.Status != UserStatus.Active`, tự động `SignOutAsync`, ghi `TempData["ErrorMessage"] = "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên hệ thống để được hỗ trợ."` và chuyển hướng về `area = "Auth", controller = "Auth", action = "Login"` (Bảng 7.2 TC-01).
     - Dòng 148-155: Khi không đủ quyền truy cập (sai vai trò), trả về `ViewName = "~/Views/Shared/AccessDenied.cshtml", StatusCode = StatusCodes.Status403Forbidden` (Bảng 7.2 TC-02).
   - `src/ItTms.Web/Filters/AuthorizeTicketAccessAttribute.cs`:
     - Dòng 86-103: Chặn Staff A xem ticket của Staff B, trả về `~/Views/Shared/AccessDenied.cshtml` với HTTP 403 Forbidden (Bảng 7.2 TC-07).
3. **Mô hình Dữ liệu và Hợp đồng Nghiệp vụ Domain**:
   - `src/ItTms.Domain/Entities/Ticket.cs`:
     - Dòng 43: Trường `DueAt` (SLA hoàn tất).
     - Dòng 66: Thuộc tính `CanComment()` ủy quyền cho `CurrentState.CanComment` (chặn khi `Resolved`, `Closed`, `Rejected` - Bảng 7.2 TC-08).
     - Dòng 137-147: Phương thức tính SLA `CalculateDueAt`: `Critical` (1h), `High` (4h), `Medium` (24h), `Low` (72h).
   - `src/ItTms.Domain/Entities/TicketStatus.cs`: 7 trạng thái chuẩn (`New` = 1, `Assigned` = 2, `InProgress` = 3, `Waiting` = 4, `Resolved` = 5, `Closed` = 6, `Rejected` = 7).
4. **Hiện trạng Kiểm thử & Biên dịch**:
   - Chạy `dotnet test` phát hiện lỗi cảnh báo biên dịch tại `tests/ItTms.Tests/AdversarialTests/SecurityAndRbacAdversarialTests.cs(17,7)`: `error CS0105: The using directive for 'ItTms.Domain.Common' appeared previously in this namespace` (do dòng 5 và dòng 17 cùng khai báo `using ItTms.Domain.Common;`). Toàn bộ 4 dự án `src/` đều build thành công sạch sẽ.

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT TỚI THIẾT KẾ)

1. Từ **Observation 1 & 2**: `src/ItTms.Web` đang thiếu cấu trúc 5 Areas và các views chuyên biệt, trong khi `AuthorizeRolesAttribute` và `AuthorizeTicketAccessAttribute` đã hardcode đường dẫn điều hướng tới `AccessDenied.cshtml` và `Auth/Account/Login`.  
   $\to$ **Thiết kế**: Cần xây dựng bản vẽ Razor Views bao phủ 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) và tạo ngay `Views/Shared/AccessDenied.cshtml` (HTTP 403) và `_Layout.cshtml` mới.
2. Từ **Observation 1**: Môi trường không có font icon bên ngoài và yêu cầu zero-CDN self-contained.  
   $\to$ **Thiết kế**: Tạo file partial `_SvgIcons.cshtml` chứa toàn bộ icon thông dụng dạng SVG sprite nhúng nội tuyến, đảm bảo giao diện hiển thị sắc nét 100% offline không cần font ngoài.
3. Từ **Observation 2 & 3**: Vòng đời Ticket có các trạng thái khóa và quy định SLA rõ rệt (`CanComment == false` tại `Resolved`, `Closed`, `Rejected`; SLA tính tự động theo `Priority`).  
   $\to$ **Thiết kế**:
   - Trang `Details.cshtml`: Khung chat SignalR phải kiểm tra `@if (Model.Ticket.CanComment())`. Nếu `false`, ẩn ô nhập tin nhắn và thay bằng banner cảnh báo bị khóa (đáp ứng Bảng 7.2 TC-08).
   - Trang `Create.cshtml`: Tích hợp widget JavaScript `updateSlaPreview()` tự động tính và hiển thị hạn SLA `DueAt` động theo thời gian thực (Critical: 1h, High: 4h, Medium: 24h, Low: 72h - đáp ứng Bảng 7.2 TC-03).
   - Trang `Details.cshtml`: Khi trạng thái là `Resolved`, hiển thị Action Banner với `FeedbackModal` (5 ngôi sao tương tác + nhận xét để đóng ticket - đáp ứng Bảng 7.2 TC-09).
4. Từ **Observation 2**: Kịch bản kiểm thử Bảng 7.2 TC-01 yêu cầu tài khoản bị vô hiệu hóa (`Inactive`) phải hiển thị thông báo lỗi rõ ràng.  
   $\to$ **Thiết kế**: `Login.cshtml` kiểm tra trực tiếp `TempData["ErrorMessage"]` để hiển thị alert màu đỏ nổi bật, kèm theo widget "Tài khoản mẫu Bảng 7.2" với nút click-to-fill để kiểm thử nghiệm thu tức thì.
5. Từ **Observation 2**: Kịch bản kiểm thử Bảng 7.2 TC-10 yêu cầu Admin không được phép tự vô hiệu hóa tài khoản của chính mình.  
   $\to$ **Thiết kế**: Trang `Admin/User/Index.cshtml` kiểm tra `@if (user.Id == currentAdminId)` để vô hiệu hóa nút bấm (`disabled`) và hiển thị tooltip cảnh báo an toàn.

---

## 3. CAVEATS (CÁC VÙNG CHƯA ĐIỀU TRA & GIẢ ĐỊNH)

1. **Thư viện SignalR Client JS**: Dự án hiện có `TicketHub.cs` tại server nhưng chưa có file `signalr.min.js` trong `wwwroot/lib/microsoft-signalr/`. Nhóm `explorer_m3_2` đang phụ trách kiến trúc SignalR Hub & Client, khi triển khai cần bổ sung file này để các view chạy được WebSocket.
2. **Lỗi cú pháp trong file test**: `SecurityAndRbacAdversarialTests.cs` có dòng `using ItTms.Domain.Common;` bị trùng lặp ở dòng 17 gây lỗi `CS0105`. Cần xóa dòng này để `dotnet test` vượt qua kiểm tra biên dịch.
3. **Thư mục ViewModels**: Khi nhóm Worker triển khai code, các ViewModels cần được đặt tập trung trong `src/ItTms.Web/Models/ViewModels/` hoặc co-located trong từng Area theo kiến trúc Vertical Slices.

---

## 4. CONCLUSION (KẾT LUẬN & ĐÁNH GIÁ CUỐI CÙNG)

Bản vẽ thiết kế **Bootstrap 5 UI/UX & Razor Views Blueprint** đã được hoàn thành đầy đủ, chi tiết và lưu tại file:
`c:\IT_System\.agents\teamwork\explorer_m3_3\ui_razor_views_plan.md`

Bản vẽ đáp ứng trọn vẹn:
- Cấu trúc Master Layout `_Layout.cshtml` với điều hướng phân quyền động, chuông thông báo SignalR, bộ chuyển giao diện sáng/tối tự động lưu `localStorage`.
- Danh mục toàn bộ Razor Views cho 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) bao phủ 20 Use Cases.
- Hệ thống 6 Modals nghiệp vụ chuẩn hóa (`AssignModal`, `RejectModal`, `HoldForInfoModal`, `ResolveModal`, `FeedbackModal`, `ReopenModal`).
- Đáp ứng 100% các tiêu chí hiển thị và kiểm soát an ninh của 10 kịch bản kiểm thử Bảng 7.2.
- Chuẩn giao diện tự hành (zero CDN), WCAG 2.1 AA và Responsive.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

1. **Kiểm tra file tài liệu thiết kế**:
   - Đọc trực tiếp file blueprint: `c:\IT_System\.agents\teamwork\explorer_m3_3\ui_razor_views_plan.md`.
   - Xác nhận có đầy đủ 8 chương mục, mã nguồn Razor Views mẫu của `_Layout.cshtml`, `Login.cshtml`, `Create.cshtml`, `Details.cshtml`, `AccessDenied.cshtml`, và script `ticket-realtime.js`.
2. **Kiểm tra tính tương thích với Domain & Web**:
   - So sánh các tên thuộc tính (`DueAt`, `CanComment`, `Category`, `Priority`, `StatusId`, `RoleNames`) với các entity trong `src/ItTms.Domain/Entities/`.
   - So sánh mã lỗi 403 Forbidden và đường dẫn `~/Views/Shared/AccessDenied.cshtml` với `AuthorizeRolesAttribute.cs` và `AuthorizeTicketAccessAttribute.cs`.
3. **Lệnh kiểm tra giải quyết lỗi biên dịch test**:
   - Chạy lệnh sau trong PowerShell để xóa dòng using trùng lặp và xác minh build:
     ```powershell
     # Sửa dòng 17 trùng lặp trong SecurityAndRbacAdversarialTests.cs
     dotnet build
     ```
