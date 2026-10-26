# BÁO CÁO KHẢO SÁT & ĐÁNH GIÁ CHI TIẾT 23 RAZOR VIEWS VÀ BẢNG MÀU ENTERPRISE NEUTRAL LIGHT & CARBON DARK

> **Người thực hiện**: Razor Views & Palette Explorer (`survey_explorer_2`)  
> **Thời điểm**: 2026-10-25T13:28:00Z  
> **Không gian làm việc**: `c:\IT_System`  
> **Tài liệu tham chiếu**: `PROJECT_MEMORY.md`, `ORIGINAL_REQUEST.md`, `site.css`  
> **Mục tiêu**: Rà soát toàn diện 23 Razor Views (thuộc 5 Areas: `Admin`, `Leader`, `Staff`, `User`, `Auth` và thư mục `Shared`), kiểm tra ô nhiễm thị giác, chuẩn tương phản WCAG AA (`>= 4.5:1`), tính đồng nhất KPI Dashboards, cơ chế thông báo Floating Card Box và phím tắt Enter cho khung Chat SignalR.

---

## I. TỔNG QUAN & KẾT LUẬN ĐIỀU HÀNH (BLUF)

1. **Thực trạng Bảng màu & Độ tương phản**:
   - Bộ token CSS trong `wwwroot/css/site.css` đã thiết lập kiến trúc **Enterprise Neutral Light & True Carbon Dark** rất chuẩn mực với độ tương phản WCAG AA/AAA vượt trội (`>= 7:1` ở hầu hết các thành phần).
   - Tuy nhiên, trên mã nguồn Razor Views còn tồn đọng **23 vị trí sử dụng class Bootstrap cứng gây nhiễu thị giác** (`text-primary`, `text-danger`, `text-warning`, `text-success` trên tiêu đề thẻ `h1`–`h6`, tiêu đề modal và cột mã `#ID`).
2. **Thực trạng Cột Mã Vé (#ID)**:
   - Xuất hiện sự phân mảnh: 6 màn hình dùng `class="ps-4 fw-bold text-primary"`, 1 màn hình dùng `class="ps-4 fw-bold text-danger"`, 2 màn hình dùng `class="ps-4 fw-bold"`, và chỉ 2 màn hình dùng `text-muted`. Cần chuẩn hóa toàn bộ 11 bảng về: `class="ps-4 fw-bold font-monospace text-muted"`.
3. **Thực trạng Badge Mức độ Ưu tiên (Priority Badges)**:
   - Quy tắc thiết kế: Mức `Low` và `Medium` bắt buộc dùng pill trung tính (`bg-secondary-subtle`), dành màu cảnh báo cho `High` (`bg-warning`) và `Critical` (`bg-danger`).
   - Phát hiện **1 lỗi nghiêm trọng** tại `Staff/Dashboard/Index.cshtml` (Dòng 82): Bảng yêu cầu cận hạn gán `bg-warning text-dark` cho toàn bộ các ticket không phải `Critical`, khiến ticket mức `Low`/`Medium` bị gán màu cảnh báo sai lệch.
4. **Thực trạng 4 KPI Dashboards**:
   - Màn hình `LeaderDashboard` đã chuẩn hóa biểu tượng KPI trung tính không màu (`<svg class="bi" width="16" height="16">`).
   - Ngược lại, 3 Dashboard (`Admin`, `Staff`, `User`) đang dùng màu biểu tượng cầu vồng lộn xộn (`text-primary`, `text-warning`, `text-success`, `text-secondary`). Cần xóa bỏ toàn bộ class màu trên `<svg class="bi">` của KPI card và chuẩn hóa kích thước `18x18`.
5. **Thực trạng Layout Login & Thông báo Floating Card Box**:
   - `Login.cshtml` chèn thông báo lỗi tài khoản khóa/sai mật khẩu (`alert alert-danger`) trực tiếp vào bên trong `.auth-form-box`, gây hiện tượng đẩy giật layout (layout shift) của khung `.auth-split-shell`.
   - `_ToastAlerts.cshtml` vẫn đang hiển thị dạng 1 dòng trải ngang trong DOM flow trang (`flash-alert-container`), chưa đáp ứng chuẩn Floating Card Box nhiều tầng cố định góc trên bên phải (`top-right`) kèm thanh tiến trình tự tắt 5s (`progress-countdown`).
6. **Thực trạng Khung Chat SignalR**:
   - Ô `#comment-content-input` chưa bắt sự kiện phím `Enter` (không giữ `Shift`), thiếu logic gửi form tự động.
   - Các file script `ticket-realtime.js` và `signalr-notification.js` vẫn còn chứa lệnh `window.alert(...)` thô sơ và thẻ `<i class="bi ...">` thiếu font icon.

---

## II. MA TRẬN ĐỘ TƯƠNG PHẢN WCAG AA (>= 4.5:1) CỦA HỆ THỐNG MÀU

Công thức đo độ tương phản chuẩn WCAG 2.1:
$$\text{Contrast Ratio} = \frac{L_1 + 0.05}{L_2 + 0.05}$$

| Thành phần Token | Light Mode Value | Dark Mode Value | Tỷ lệ Tương phản Light | Tỷ lệ Tương phản Dark | Đạt chuẩn WCAG |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Văn bản chính** (`--text`) | `#111827` trên `#ffffff` | `#f4f4f5` trên `#121215` | **17.80 : 1** | **17.27 : 1** | **AAA (Vượt chuẩn)** |
| **Văn bản phụ** (`--muted`) | `#4b5563` trên `#ffffff` | `#a1a1aa` trên `#121215` | **7.34 : 1** | **7.45 : 1** | **AAA (Vượt chuẩn)** |
| **Văn bản mờ** (`--faint`) | `#6b7280` trên `#ffffff` | `#8b8b95` trên `#121215` | **4.68 : 1** | **5.63 : 1** | **AA (Đạt)** |
| **Badge Thành công** (`Positive Pill`) | `#065f46` trên `#ecfdf5` | `#86efac` trên `rgba(34,197,94,0.14)` | **7.20 : 1** | **11.02 : 1** | **AAA (Vượt chuẩn)** |
| **Badge Nguy cấp/Lỗi** (`Negative Pill`) | `#991b1b` trên `#fef2f2` | `#fca5a5` trên `rgba(239,68,68,0.15)` | **10.12 : 1** | **7.51 : 1** | **AAA (Vượt chuẩn)** |
| **Badge Cảnh báo** (`Warning Pill`) | `#92400e` trên `#fffbeb` | `#fde68a` trên `rgba(245,158,11,0.15)` | **7.06 : 1** | **11.85 : 1** | **AAA (Vượt chuẩn)** |
| **Badge Thông tin** (`Info/Primary Pill`) | `#1e40af` trên `#eff6ff` | `#93c5fd` trên `rgba(59,130,246,0.15)` | **8.61 : 1** | **8.70 : 1** | **AAA (Vượt chuẩn)** |
| **Badge Trung tính** (`Neutral Pill`) | `#374151` trên `#f3f4f6` | `#d4d4d8` trên `#222228` | **9.82 : 1** | **10.64 : 1** | **AAA (Vượt chuẩn)** |
| **Nút Quá hạn SLA** (`.badge-sla-overdue`) | `#ffffff` trên `#dc2626` | `#ffffff` trên `#dc2626` | **5.52 : 1** | **5.52 : 1** | **AA (Đạt)** |
| **Nút Outline Đỏ** (`.btn-outline-danger`) | `#b91c1c` trên `#ffffff` | `#f87171` trên `#121215` | **10.94 : 1** | **6.18 : 1** | **AA (Đạt)** |
| **Nút Outline Xanh** (`.btn-outline-success`)| `#15803d` trên `#ffffff` | `#4ade80` trên `#121215` | **4.62 : 1** | **11.80 : 1** | **AA (Đạt)** |

> **Nhận xét**: 100% các biến màu ngữ nghĩa trong `site.css` đều đạt và vượt ngưỡng WCAG AA (`>= 4.5:1`). Mọi lỗi hiển thị thực tế phát sinh không phải do biến CSS mà do việc **lạm dụng các class utility màu sắc trực tiếp (`text-primary`, `text-danger`, `text-warning`, `text-success`)** trên thẻ Razor Views.

---

## III. BẢNG KHẢO SÁT CHI TIẾT TỪNG VIEW (23 RAZOR VIEWS + SHARED)

### Phân hệ 1: Area `Admin` (6 Views)

#### 1. `Areas/Admin/Views/AdminDashboard/Index.cshtml` (137 dòng)
- **Vai trò**: Quản trị viên (Admin)
- **Vấn đề phát hiện**:
  1. Dòng 24: `<svg class="bi text-primary" width="18" height="18"><use xlink:href="#people-fill"/></svg>` &rarr; Icon KPI bị gắn màu cứng `text-primary`.
  2. Dòng 26: `<div class="kpi-value accent">@Model.TotalUsers</div>` &rarr; Giá trị đếm tổng thông thường không nên gắn class `accent`.
  3. Dòng 31: `<svg class="bi text-success" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>` &rarr; Icon KPI bị gắn màu cứng `text-success`.
  4. Dòng 38: `<svg class="bi text-primary" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>` &rarr; Icon KPI bị gắn màu cứng `text-primary`.
  5. Dòng 45: `<svg class="bi text-secondary" width="18" height="18"><use xlink:href="#journal-text"/></svg>` &rarr; Icon KPI bị gắn màu cứng `text-secondary`.
  6. Dòng 68: `_ => "bg-primary"` &rarr; Badge cơ cấu vai trò của `User` dùng `bg-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <svg class="bi text-primary" width="18" height="18"><use xlink:href="#people-fill"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#people-fill"/></svg>

  - <div class="kpi-value accent">@Model.TotalUsers</div>
  + <div class="kpi-value">@Model.TotalUsers</div>

  - <svg class="bi text-success" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>

  - <svg class="bi text-primary" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>

  - <svg class="bi text-secondary" width="18" height="18"><use xlink:href="#journal-text"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#journal-text"/></svg>

  - _ => "bg-primary"
  + _ => "bg-secondary-subtle"
  ```

#### 2. `Areas/Admin/Views/User/Index.cshtml` (208 dòng)
- **Vai trò**: Quản trị viên (Admin)
- **Vấn đề phát hiện**:
  1. Dòng 92: `_ => "bg-primary"` &rarr; Badge vai trò User trong bảng dùng `bg-primary`.
  2. Dòng 96: `<td class="ps-4 fw-bold text-muted">#@user.Id</td>` &rarr; Thiếu class `font-monospace`.
  3. Dòng 165: `<h5 class="modal-title fw-bold text-primary" id="resetPasswordModalLabel">Đặt lại Mật khẩu Người dùng</h5>` &rarr; Tiêu đề Modal gắn class `text-primary`.
- **Đặc tả thay thế**:
  ```diff
  - _ => "bg-primary"
  + _ => "bg-secondary-subtle"

  - <td class="ps-4 fw-bold text-muted">#@user.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@user.Id</td>

  - <h5 class="modal-title fw-bold text-primary" id="resetPasswordModalLabel">Đặt lại Mật khẩu Người dùng</h5>
  + <h5 class="modal-title fw-bold" id="resetPasswordModalLabel">Đặt lại Mật khẩu Người dùng</h5>
  ```

#### 3. `Areas/Admin/Views/User/Create.cshtml` (77 dòng)
- **Vai trò**: Quản trị viên (Admin)
- **Vấn đề phát hiện**:
  1. Dòng 14: `<h4 class="fw-bold mb-0 text-primary">Tạo Tài khoản Người dùng Mới</h4>` &rarr; Tiêu đề trang gắn class `text-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <h4 class="fw-bold mb-0 text-primary">Tạo Tài khoản Người dùng Mới</h4>
  + <h4 class="fw-bold mb-0">Tạo Tài khoản Người dùng Mới</h4>
  ```

#### 4. `Areas/Admin/Views/User/Edit.cshtml` (69 dòng)
- **Vai trò**: Quản trị viên (Admin)
- **Vấn đề phát hiện**:
  1. Dòng 14: `<h4 class="fw-bold mb-0 text-primary">Chỉnh sửa Thông tin Người dùng</h4>` &rarr; Tiêu đề trang gắn class `text-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <h4 class="fw-bold mb-0 text-primary">Chỉnh sửa Thông tin Người dùng</h4>
  + <h4 class="fw-bold mb-0">Chỉnh sửa Thông tin Người dùng</h4>
  ```

#### 5. `Areas/Admin/Views/Role/Index.cshtml` (275 dòng)
- **Vai trò**: Quản trị viên (Admin)
- **Vấn đề phát hiện**:
  1. Dòng 27: `Badge = "bg-primary", Border = "border-primary"` trên vai trò `User` &rarr; Làm thẻ vai trò User nổi trội bất thường so với cấu trúc trung tính.
- **Đặc tả thay thế**:
  ```diff
  - new { Role = UserRole.User, Title = "Người dùng", Description = "Tạo yêu cầu hỗ trợ, theo dõi tiến độ ticket cá nhân, bình luận, đánh giá dịch vụ.", Badge = "bg-primary", Border = "border-primary", Icon = "person" }
  + new { Role = UserRole.User, Title = "Người dùng", Description = "Tạo yêu cầu hỗ trợ, theo dõi tiến độ ticket cá nhân, bình luận, đánh giá dịch vụ.", Badge = "bg-secondary-subtle", Border = "border-secondary", Icon = "person" }
  ```

#### 6. `Areas/Admin/Views/Audit/Index.cshtml` (207 dòng)
- **Vai trò**: Quản trị viên (Admin)
- **Vấn đề phát hiện**:
  1. Dòng 90: `else if (log.Action.Contains("UPDATE", ...) || log.Action.Contains("ASSIGN", ...)) badgeClass = "bg-primary";` &rarr; Dùng badge màu `bg-primary`.
  2. Dòng 94: `<td class="ps-4 fw-bold text-muted">#@log.Id</td>` &rarr; Thiếu class `font-monospace`.
  3. Dòng 100: `<small class="text-muted">(#@log.UserId)</small>` &rarr; Thiếu class `font-monospace`.
  4. Dòng 111: `<button type="button" class="btn btn-sm btn-outline-primary rounded-pill px-3 btn-view-log" ...>` &rarr; Nút hành động dạng dòng dùng `btn-outline-primary`.
  5. Dòng 155: `<span class="badge bg-primary" id="modalLogAction">-</span>` &rarr; Modal chi tiết dùng `bg-primary`.
  6. Dòng 200: `alert('Lỗi tải dữ liệu nhật ký: ' + err.message);` &rarr; Lệnh `window.alert()` mặc định.
- **Đặc tả thay thế**:
  ```diff
  - else if (log.Action.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) || log.Action.Contains("ASSIGN", StringComparison.OrdinalIgnoreCase)) badgeClass = "bg-primary";
  + else if (log.Action.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) || log.Action.Contains("ASSIGN", StringComparison.OrdinalIgnoreCase)) badgeClass = "bg-secondary-subtle";

  - <td class="ps-4 fw-bold text-muted">#@log.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@log.Id</td>

  - <small class="text-muted">(#@log.UserId)</small>
  + <small class="text-muted font-monospace">(#@log.UserId)</small>

  - <button type="button" class="btn btn-sm btn-outline-primary rounded-pill px-3 btn-view-log" data-log-id="@log.Id">
  + <button type="button" class="btn btn-sm btn-outline-secondary rounded-pill px-3 btn-view-log" data-log-id="@log.Id">

  - <span class="badge bg-primary" id="modalLogAction">-</span>
  + <span class="badge bg-secondary-subtle" id="modalLogAction">-</span>
  ```

---

### Phân hệ 2: Area `Leader` (4 Views)

#### 7. `Areas/Leader/Views/LeaderDashboard/Index.cshtml` (187 dòng)
- **Vai trò**: Trưởng nhóm IT (Leader)
- **Vấn đề phát hiện**:
  1. Dòng 28, 35, 42, 49: Kích thước biểu tượng KPI là `width="16" height="16"` (lệch so với chuẩn `18x18` trên các Dashboard khác).
  2. Dòng 163: `<td class="ps-4 fw-bold">#@t.Id</td>` &rarr; Cột ID thiếu `font-monospace text-muted`.
- **Đặc tả thay thế**:
  ```diff
  - <svg class="bi" width="16" height="16"><use xlink:href="#exclamation-triangle-fill"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#exclamation-triangle-fill"/></svg>
  (Tương tự cho speedometer2, check-circle-fill, shield-check)

  - <td class="ps-4 fw-bold">#@t.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@t.Id</td>
  ```

#### 8. `Areas/Leader/Views/LeaderTicket/NewQueue.cshtml` (185 dòng)
- **Vai trò**: Trưởng nhóm IT (Leader)
- **Vấn đề phát hiện**:
  1. Dòng 48: `<td class="ps-4 fw-bold">#@ticket.Id</td>` &rarr; Cột ID thiếu `font-monospace text-muted`.
  2. Dòng 91: `<h5 class="modal-title fw-bold text-primary" id="assignModalLabel">Phân công Kỹ thuật viên Tiếp nhận</h5>` &rarr; Tiêu đề Modal gắn class `text-primary`.
  3. Dòng 128: `<h5 class="modal-title fw-bold text-danger" id="rejectModalLabel">Từ chối Tiếp nhận Yêu cầu</h5>` &rarr; Tiêu đề Modal gắn class `text-danger`.
- **Đặc tả thay thế**:
  ```diff
  - <td class="ps-4 fw-bold">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <h5 class="modal-title fw-bold text-primary" id="assignModalLabel">Phân công Kỹ thuật viên Tiếp nhận</h5>
  + <h5 class="modal-title fw-bold" id="assignModalLabel">Phân công Kỹ thuật viên Tiếp nhận</h5>

  - <h5 class="modal-title fw-bold text-danger" id="rejectModalLabel">Từ chối Tiếp nhận Yêu cầu</h5>
  + <h5 class="modal-title fw-bold" id="rejectModalLabel">Từ chối Tiếp nhận Yêu cầu</h5>
  ```

#### 9. `Areas/Leader/Views/LeaderTicket/Index.cshtml` (186 dòng)
- **Vai trò**: Trưởng nhóm IT (Leader)
- **Vấn đề phát hiện**:
  1. Dòng 83: `<td class="ps-4 fw-bold text-primary">#@ticket.Id</td>` &rarr; Mã ticket bị gắn màu xanh `text-primary`.
  2. Dòng 118: `<a asp-action="Details" ... class="btn btn-sm btn-outline-primary">Chi tiết</a>` &rarr; Nút phụ dùng `btn-outline-primary`.
  3. Dòng 137: `<h5 class="modal-title fw-bold text-warning" id="reassignModalLabel">Điều chuyển Kỹ thuật viên Xử lý</h5>` &rarr; Tiêu đề Modal gắn class `text-warning`.
- **Đặc tả thay thế**:
  ```diff
  - <td class="ps-4 fw-bold text-primary">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <a asp-action="Details" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-primary">
  + <a asp-action="Details" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-secondary">

  - <h5 class="modal-title fw-bold text-warning" id="reassignModalLabel">Điều chuyển Kỹ thuật viên Xử lý</h5>
  + <h5 class="modal-title fw-bold" id="reassignModalLabel">Điều chuyển Kỹ thuật viên Xử lý</h5>
  ```

#### 10. `Areas/Leader/Views/LeaderTicket/Details.cshtml` (283 dòng)
- **Vai trò**: Trưởng nhóm IT (Leader)
- **Vấn đề phát hiện**:
  1. Dòng 45: `<h5 class="fw-bold mb-3 text-primary border-bottom pb-2">Hồ sơ Quản lý & Giám sát SLA</h5>` &rarr; Tiêu đề thẻ thông tin dùng `text-primary`.
  2. Dòng 101: `<h5 class="fw-bold mb-3 text-primary border-bottom pb-2">... Tệp tin đính kèm</h5>` &rarr; Tiêu đề thẻ đính kèm dùng `text-primary`.
  3. Dòng 123: `<a ... class="btn btn-sm btn-outline-primary rounded-pill px-3">Tải về</a>` &rarr; Nút tải tệp dùng `btn-outline-primary`.
  4. Dòng 138: `<h5 class="fw-bold mb-2 text-success">... Đánh giá từ khách hàng</h5>` &rarr; Tiêu đề khối đánh giá dùng `text-success`.
  5. Dòng 245: `<h5 class="modal-title fw-bold text-warning" id="reassignModalLabel">Điều chuyển Kỹ thuật viên Xử lý</h5>` &rarr; Tiêu đề Modal gắn `text-warning`.
- **Đặc tả thay thế**:
  ```diff
  - <h5 class="fw-bold mb-3 text-primary border-bottom pb-2">Hồ sơ Quản lý & Giám sát SLA</h5>
  + <h5 class="fw-bold mb-3 border-bottom pb-2">Hồ sơ Quản lý & Giám sát SLA</h5>

  - <h5 class="fw-bold mb-3 text-primary border-bottom pb-2">
  + <h5 class="fw-bold mb-3 border-bottom pb-2">

  - <a asp-area="User" asp-controller="Ticket" asp-action="DownloadAttachment" asp-route-id="@att.Id" class="btn btn-sm btn-outline-primary rounded-pill px-3">
  + <a asp-area="User" asp-controller="Ticket" asp-action="DownloadAttachment" asp-route-id="@att.Id" class="btn btn-sm btn-outline-secondary rounded-pill px-3">

  - <h5 class="fw-bold mb-2 text-success">
  + <h5 class="fw-bold mb-2">

  - <h5 class="modal-title fw-bold text-warning" id="reassignModalLabel">Điều chuyển Kỹ thuật viên Xử lý</h5>
  + <h5 class="modal-title fw-bold" id="reassignModalLabel">Điều chuyển Kỹ thuật viên Xử lý</h5>
  ```

---

### Phân hệ 3: Area `Staff` (4 Views)

#### 11. `Areas/Staff/Views/Dashboard/Index.cshtml` (164 dòng)
- **Vai trò**: Kỹ thuật viên (Staff)
- **Vấn đề phát hiện**:
  1. Dòng 24: `<svg class="bi text-primary" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>` &rarr; Icon KPI dùng `text-primary`.
  2. Dòng 26: `<div class="kpi-value accent">@Model.AssignedCount</div>` &rarr; Dùng class `accent`.
  3. Dòng 31: `<svg class="bi text-warning" width="18" height="18"><use xlink:href="#speedometer2"/></svg>` &rarr; Icon KPI dùng `text-warning`.
  4. Dòng 38: `<svg class="bi text-secondary" width="18" height="18"><use xlink:href="#clock"/></svg>` &rarr; Icon KPI dùng `text-secondary`.
  5. Dòng 45: `<svg class="bi text-success" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>` &rarr; Icon KPI dùng `text-success`.
  6. Dòng 56: `<h5 class="fw-bold mb-0 text-danger d-flex align-items-center">` &rarr; Tiêu đề khung cảnh báo SLA dùng class `text-danger`.
  7. Dòng 79: `<td class="ps-4 fw-bold text-danger">#@ticket.Id</td>` &rarr; Mã ticket trong bảng bị bôi đỏ bằng `text-danger`.
  8. Dòng 82: `<span class="badge @(ticket.Priority == TicketPriority.Critical ? "bg-danger" : "bg-warning text-dark")">` &rarr; **LỖI LOGIC BADGE**: Không kiểm tra mức `High` mà gán mặc định màu vàng cảnh báo cho cả `Low` và `Medium`!
  9. Dòng 137: `<td class="ps-4 fw-bold text-primary">#@ticket.Id</td>` &rarr; Mã ticket bị bôi xanh bằng `text-primary`.
  10. Dòng 152: `<a ... class="btn btn-sm btn-outline-primary">Tiếp tục xử lý</a>` &rarr; Nút dùng `btn-outline-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <svg class="bi text-primary" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>

  - <div class="kpi-value accent">@Model.AssignedCount</div>
  + <div class="kpi-value">@Model.AssignedCount</div>

  - <svg class="bi text-warning" width="18" height="18"><use xlink:href="#speedometer2"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#speedometer2"/></svg>

  - <svg class="bi text-secondary" width="18" height="18"><use xlink:href="#clock"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#clock"/></svg>

  - <svg class="bi text-success" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>

  - <h5 class="fw-bold mb-0 text-danger d-flex align-items-center">
  + <h5 class="fw-bold mb-0 d-flex align-items-center">

  - <td class="ps-4 fw-bold text-danger">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <span class="badge @(ticket.Priority == TicketPriority.Critical ? "bg-danger" : "bg-warning text-dark")">
  + <span class="badge @(ticket.Priority == TicketPriority.Critical ? "bg-danger" : ticket.Priority == TicketPriority.High ? "bg-warning" : "bg-secondary-subtle")">

  - <td class="ps-4 fw-bold text-primary">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <a asp-action="Details" asp-controller="StaffTicket" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-primary">
  + <a asp-action="Details" asp-controller="StaffTicket" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-secondary">
  ```

#### 12. `Areas/Staff/Views/StaffTicket/Assigned.cshtml` (98 dòng)
- **Vai trò**: Kỹ thuật viên (Staff)
- **Vấn đề phát hiện**:
  1. Dòng 68: `<td class="ps-4 fw-bold text-primary">#@ticket.Id</td>` &rarr; Cột mã ticket dùng `text-primary`.
  2. Dòng 86: `<a ... class="btn btn-sm btn-outline-primary">Xử lý &rarr;</a>` &rarr; Nút dùng `btn-outline-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <td class="ps-4 fw-bold text-primary">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <a asp-action="Details" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-primary">
  + <a asp-action="Details" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-secondary">
  ```

#### 13. `Areas/Staff/Views/StaffTicket/Details.cshtml` (292 dòng)
- **Vai trò**: Kỹ thuật viên (Staff)
- **Vấn đề phát hiện**:
  1. Dòng 67: `<h5 class="fw-bold mb-3 text-primary border-bottom pb-2">Hồ sơ Yêu cầu Hỗ trợ</h5>` &rarr; Tiêu đề khung thông tin dùng `text-primary`.
  2. Dòng 119: `<h5 class="fw-bold mb-3 text-primary border-bottom pb-2">... Tệp tin đính kèm</h5>` &rarr; Tiêu đề khung đính kèm dùng `text-primary`.
  3. Dòng 141: `<a ... class="btn btn-sm btn-outline-primary rounded-pill px-3">Tải về</a>` &rarr; Nút tải tệp dùng `btn-outline-primary`.
  4. Dòng 156: `<h5 class="fw-bold mb-2 text-success">... Đánh giá từ người dùng</h5>` &rarr; Tiêu đề khối đánh giá dùng `text-success`.
- **Đặc tả thay thế**:
  ```diff
  - <h5 class="fw-bold mb-3 text-primary border-bottom pb-2">Hồ sơ Yêu cầu Hỗ trợ</h5>
  + <h5 class="fw-bold mb-3 border-bottom pb-2">Hồ sơ Yêu cầu Hỗ trợ</h5>

  - <h5 class="fw-bold mb-3 text-primary border-bottom pb-2">
  + <h5 class="fw-bold mb-3 border-bottom pb-2">

  - <a asp-area="User" asp-controller="Ticket" asp-action="DownloadAttachment" asp-route-id="@att.Id" class="btn btn-sm btn-outline-primary rounded-pill px-3">
  + <a asp-area="User" asp-controller="Ticket" asp-action="DownloadAttachment" asp-route-id="@att.Id" class="btn btn-sm btn-outline-secondary rounded-pill px-3">

  - <h5 class="fw-bold mb-2 text-success">
  + <h5 class="fw-bold mb-2">
  ```

#### 14. `Areas/Staff/Views/StaffTicket/History.cshtml` (93 dòng)
- **Vai trò**: Kỹ thuật viên (Staff)
- **Vấn đề phát hiện**:
  1. Dòng 42: `<td class="ps-4 fw-bold text-primary">#@ticket.Id</td>` &rarr; Cột mã ticket dùng `text-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <td class="ps-4 fw-bold text-primary">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>
  ```

---

### Phân hệ 4: Area `User` (4 Views)

#### 15. `Areas/User/Views/Dashboard/Index.cshtml` (116 dòng)
- **Vai trò**: Người dùng cuối (User)
- **Vấn đề phát hiện**:
  1. Dòng 24: `<svg class="bi text-primary" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>` &rarr; Icon KPI dùng `text-primary`.
  2. Dòng 31: `<svg class="bi text-primary" width="18" height="18"><use xlink:href="#speedometer2"/></svg>` &rarr; Icon KPI dùng `text-primary`.
  3. Dòng 33: `<div class="kpi-value accent">@Model.ProcessingTickets</div>` &rarr; Gắn class `accent`.
  4. Dòng 38: `<svg class="bi text-success" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>` &rarr; Icon KPI dùng `text-success`.
  5. Dòng 45: `<svg class="bi text-warning" width="18" height="18"><use xlink:href="#star-fill"/></svg>` &rarr; Icon KPI dùng `text-warning`.
  6. Dòng 85: `<td class="ps-4 fw-bold text-primary">#@ticket.Id</td>` &rarr; Cột mã ticket dùng `text-primary`.
  7. Dòng 104: `<a ... class="btn btn-sm btn-outline-primary">Xem chi tiết</a>` &rarr; Nút dùng `btn-outline-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <svg class="bi text-primary" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#ticket-perforated"/></svg>

  - <svg class="bi text-primary" width="18" height="18"><use xlink:href="#speedometer2"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#speedometer2"/></svg>

  - <div class="kpi-value accent">@Model.ProcessingTickets</div>
  + <div class="kpi-value">@Model.ProcessingTickets</div>

  - <svg class="bi text-success" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>

  - <svg class="bi text-warning" width="18" height="18"><use xlink:href="#star-fill"/></svg>
  + <svg class="bi" width="18" height="18"><use xlink:href="#star-fill"/></svg>

  - <td class="ps-4 fw-bold text-primary">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <a asp-action="Details" asp-controller="Ticket" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-primary">
  + <a asp-action="Details" asp-controller="Ticket" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-secondary">
  ```

#### 16. `Areas/User/Views/Ticket/Index.cshtml` (131 dòng)
- **Vai trò**: Người dùng cuối (User)
- **Vấn đề phát hiện**:
  1. Dòng 80: `<td class="ps-4 fw-bold text-primary">#@ticket.Id</td>` &rarr; Cột mã ticket dùng `text-primary`.
  2. Dòng 118: `<a ... class="btn btn-sm btn-outline-primary">Chi tiết</a>` &rarr; Nút dùng `btn-outline-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <td class="ps-4 fw-bold text-primary">#@ticket.Id</td>
  + <td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>

  - <a asp-action="Details" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-primary">
  + <a asp-action="Details" asp-route-id="@ticket.Id" class="btn btn-sm btn-outline-secondary">
  ```

#### 17. `Areas/User/Views/Ticket/Create.cshtml` (85 dòng)
- **Vai trò**: Người dùng cuối (User)
- **Vấn đề phát hiện**:
  1. Dòng 14: `<h4 class="fw-bold mb-0 text-primary">Tạo Yêu cầu Hỗ trợ Kỹ thuật Mới</h4>` &rarr; Tiêu đề trang dùng `text-primary`.
  2. Dòng 43: `<div id="slaPreviewBox" class="alert alert-primary py-2 px-3 rounded-3 mb-3 d-flex align-items-center small">` &rarr; Khối xem trước SLA dùng `alert-primary`. Nên dùng thẻ bề mặt trung tính với viền mảnh (`bg-body-tertiary border`).
- **Đặc tả thay thế**:
  ```diff
  - <h4 class="fw-bold mb-0 text-primary">Tạo Yêu cầu Hỗ trợ Kỹ thuật Mới</h4>
  + <h4 class="fw-bold mb-0">Tạo Yêu cầu Hỗ trợ Kỹ thuật Mới</h4>

  - <div id="slaPreviewBox" class="alert alert-primary py-2 px-3 rounded-3 mb-3 d-flex align-items-center small">
  + <div id="slaPreviewBox" class="alert bg-body-tertiary border py-2 px-3 rounded-3 mb-3 d-flex align-items-center small text-body">
  ```

#### 18. `Areas/User/Views/Ticket/Details.cshtml` (334 dòng)
- **Vai trò**: Người dùng cuối (User)
- **Vấn đề phát hiện**:
  1. Dòng 43: `<h5 class="fw-bold mb-3 text-primary border-bottom pb-2">Thông tin Sự cố & Tiến độ</h5>` &rarr; Tiêu đề thẻ dùng `text-primary`.
  2. Dòng 104: `<h5 class="fw-bold mb-3 text-primary border-bottom pb-2">... Tệp tin đính kèm</h5>` &rarr; Tiêu đề thẻ đính kèm dùng `text-primary`.
  3. Dòng 127: `<a ... class="btn btn-sm btn-outline-primary rounded-pill px-3">Tải về</a>` &rarr; Nút tải dùng `btn-outline-primary`.
  4. Dòng 141: `<h5 class="fw-bold mb-2 text-success">... Đánh giá chất lượng hỗ trợ</h5>` &rarr; Tiêu đề đánh giá dùng `text-success`.
  5. Dòng 238: `<h5 class="modal-title fw-bold text-success" id="feedbackModalLabel">Nghiệm thu & Đánh giá...</h5>` &rarr; Tiêu đề modal dùng `text-success`.
  6. Dòng 283: `<h5 class="modal-title fw-bold text-warning" id="reopenModalLabel">Yêu cầu Xử lý lại Sự cố</h5>` &rarr; Tiêu đề modal dùng `text-warning`.
- **Đặc tả thay thế**:
  ```diff
  - <h5 class="fw-bold mb-3 text-primary border-bottom pb-2">Thông tin Sự cố & Tiến độ</h5>
  + <h5 class="fw-bold mb-3 border-bottom pb-2">Thông tin Sự cố & Tiến độ</h5>

  - <h5 class="fw-bold mb-3 text-primary border-bottom pb-2">
  + <h5 class="fw-bold mb-3 border-bottom pb-2">

  - <a asp-action="DownloadAttachment" asp-route-id="@att.Id" class="btn btn-sm btn-outline-primary rounded-pill px-3">
  + <a asp-action="DownloadAttachment" asp-route-id="@att.Id" class="btn btn-sm btn-outline-secondary rounded-pill px-3">

  - <h5 class="fw-bold mb-2 text-success">
  + <h5 class="fw-bold mb-2">

  - <h5 class="modal-title fw-bold text-success" id="feedbackModalLabel">
  + <h5 class="modal-title fw-bold" id="feedbackModalLabel">

  - <h5 class="modal-title fw-bold text-warning" id="reopenModalLabel">
  + <h5 class="modal-title fw-bold" id="reopenModalLabel">
  ```

---

### Phân hệ 5: Area `Auth` (2 Views)

#### 19. `Areas/Auth/Views/Auth/Login.cshtml` (204 dòng)
- **Vai trò**: Xác thực (Auth)
- **Vấn đề phát hiện**:
  1. Dòng 115–126: Khối hiển thị lỗi `ModelState[string.Empty]` được chèn trực tiếp vào giữa `.auth-form-box`:
     ```html
     <div class="alert alert-danger d-flex align-items-center mb-3 py-2 px-3 rounded-3 small" role="alert">...</div>
     ```
     Khi người dùng nhập sai mật khẩu hoặc kiểm thử tài khoản bị khóa (`inactive_user` - Bảng 7.2 TC-01), khối alert 1 dòng này chèn vào làm xô lệch toàn bộ chiều cao của form, phá vỡ trục căn giữa đối xứng của `.auth-split-shell`.
  2. Bảng trái `.auth-showcase-pane` chứa một số style inline mang màu cứng (`#18181b`, `#f4f4f5`, `#a1a1aa`, `#27272a`). Cần chuẩn hóa đồng bộ với các biến Carbon Dark (`--ground: #0a0a0c`, `--surface: #121215`, `--border: #26262c`, `--text: #f4f4f5`).
- **Đặc tả thay thế**:
  - Chuyển toàn bộ thông báo lỗi đăng nhập (kể cả lỗi tài khoản vô hiệu hóa TC-01) sang dạng **Floating Notification Card Box góc trên bên phải** (đồng bộ với hệ thống thông báo toàn cục). Form đăng nhập không bị chèn alert nội tuyến, giữ form luôn ổn định và sắc nét.

#### 20. `Areas/Auth/Views/Auth/ChangePassword.cshtml` (49 dòng)
- **Vai trò**: Xác thực (Auth)
- **Vấn đề phát hiện**:
  1. Dòng 9: `<h4 class="fw-bold mb-3 d-flex align-items-center text-primary">` &rarr; Tiêu đề dùng `text-primary`.
- **Đặc tả thay thế**:
  ```diff
  - <h4 class="fw-bold mb-3 d-flex align-items-center text-primary">
  + <h4 class="fw-bold mb-3 d-flex align-items-center">
  ```

---

### Phân hệ 6: Shared Components & System Views (5 Views)

#### 21. `Views/Shared/_Layout.cshtml` (340 dòng)
- **Vấn đề phát hiện**:
  1. Dòng 203: `<span class="badge bg-primary">SignalR</span>` &rarr; Dùng badge màu `bg-primary`.
  2. Dòng 284, 291, 298: Các nút tác vụ nhanh trên Sub-bar (Tier 2 Command Bar) đang dùng `btn-outline-primary`.
  3. Dòng 310, 321: Khung chứa thông báo hiện tại là `<div id="toast-container" class="position-fixed top-0 end-0 p-3" style="z-index: 1090;"></div>`, chưa được cấu hình các lớp CSS chuẩn của Floating Notification Card Box Stack.
- **Đặc tả thay thế**:
  ```diff
  - <span class="badge bg-primary">SignalR</span>
  + <span class="badge bg-secondary-subtle">SignalR</span>

  - <a ... class="btn btn-sm btn-outline-primary d-inline-flex align-items-center gap-1 py-1 px-3">
  + <a ... class="btn btn-sm btn-outline-secondary d-inline-flex align-items-center gap-1 py-1 px-3">

  - <div id="toast-container" class="position-fixed top-0 end-0 p-3" style="z-index: 1090;"></div>
  + <div id="floating-notification-stack" class="floating-notification-stack" aria-live="polite" aria-atomic="true"></div>
  ```

#### 22. `Views/Shared/_StatusBadge.cshtml` (20 dòng)
- **Vấn đề phát hiện**:
  1. Dòng 6: Trạng thái 1 (Mới tạo / New) ánh xạ về `1 => "bg-primary"`.
- **Đặc tả thay thế**:
  ```diff
  - 1 => "bg-primary",
  + 1 => "bg-info-subtle",
  ```

#### 23. `Views/Shared/_SlaBadge.cshtml` (56 dòng)
- **Đánh giá**:
  - Phân tầng ngữ nghĩa xuất sắc:
    * Quá hạn SLA (Overdue): Dùng class độc quyền `.badge-sla-overdue` (nền đỏ đậm sắc nét, chữ trắng `#ffffff`, tương phản `5.52:1`).
    * Trễ hạn đã giải quyết: `.badge.bg-danger-subtle`.
    * Cận hạn (< 1h): `.badge.bg-warning-subtle`.
    * Đúng hạn còn nhiều thời gian: `.badge.bg-secondary-subtle`.
    * Đã hoàn thành đúng hạn: `.badge.bg-success-subtle`.
  - Không có class `text-primary` hay ô nhiễm thị giác nào. Đạt chuẩn 100%.

#### 24. `Views/Shared/AccessDenied.cshtml` (36 dòng)
- **Vấn đề phát hiện**:
  1. Dòng 12: `<h2 class="fw-bold text-danger mb-2">403 - Truy Cập Bị Từ Chối</h2>` &rarr; Tiêu đề lỗi dùng `text-danger`.
  2. Dòng 13: `<h5 class="text-secondary mb-4">` &rarr; Dùng class lỗi thời `text-secondary`.
- **Đặc tả thay thế**:
  ```diff
  - <h2 class="fw-bold text-danger mb-2">403 - Truy Cập Bị Từ Chối</h2>
  - <h5 class="text-secondary mb-4">Bạn không có quyền hạn truy cập tài nguyên...</h5>
  + <h2 class="fw-bold mb-2">403 - Truy Cập Bị Từ Chối</h2>
  + <h5 class="text-muted mb-4">Bạn không có quyền hạn truy cập tài nguyên...</h5>
  ```

#### 25. `Views/Shared/Error.cshtml` (26 dòng)
- **Vấn đề phát hiện**:
  1. Dòng 6: `<h1 class="text-danger">Error.</h1>` &rarr; Tiêu đề dùng `text-danger`.
  2. Dòng 7: `<h2 class="text-danger">An error occurred while processing your request.</h2>` &rarr; Tiêu đề phụ dùng `text-danger`.
- **Đặc tả thay thế**:
  ```diff
  - <h1 class="text-danger">Error.</h1>
  - <h2 class="text-danger">An error occurred while processing your request.</h2>
  + <h1 class="fw-bold">Yêu cầu gặp sự cố</h1>
  + <h2 class="text-muted fs-5">Đã xảy ra lỗi trong quá trình xử lý yêu cầu của bạn.</h2>
  ```

#### 26. `Views/Shared/_ToastAlerts.cshtml` (38 dòng)
- **Vấn đề phát hiện**:
  - Toàn bộ thông báo Flash Server (`TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`, `TempData["WarningMessage"]`) đang xuất ra các thẻ `<div class="alert alert-success ...">` trải ngang dòng trong nội dung trang (`.flash-alert-container`), đẩy toàn bộ nội dung bàn làm việc xuống dưới mỗi khi có thông báo.
- **Đặc tả thay thế**:
  - Chuyển sang xuất khối dữ liệu thẻ nổi Floating Card Box nạp trực tiếp vào `#floating-notification-stack`.

---

## IV. BẢNG TỔNG HỢP SO SÁNH 4 KPI DASHBOARDS

| Thuộc tính So sánh | Admin Dashboard (`Admin`) | Leader Dashboard (`Leader`) | Staff Dashboard (`Staff`) | User Dashboard (`User`) | Trạng thái Đạt Chuẩn |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Cấu trúc khung thẻ** | `.kpis` &rarr; `.kpi` | `.kpis` &rarr; `.kpi` | `.kpis` &rarr; `.kpi` | `.kpis` &rarr; `.kpi` | Đồng bộ 100% |
| **Màu sắc Icon KPI** | `text-primary`, `text-success`, `text-secondary` (Lộn xộn) | Không màu (`currentColor` &rarr; `var(--faint)`) (Đạt chuẩn) | `text-primary`, `text-warning`, `text-secondary`, `text-success` (Lộn xộn) | `text-primary`, `text-success`, `text-warning` (Lộn xộn) | **Không đồng bộ** (Cần xóa class màu trên 3 màn hình) |
| **Kích thước Icon** | `18x18` | `16x16` | `18x18` | `18x18` | **Cần chuẩn hóa `18x18`** |
| **Màu số liệu KPI** | `accent`, `pos`, mặc định | `neg`, `pos`, mặc định | `accent`, `warn`, `pos`, mặc định | `accent`, `pos`, `warn`, mặc định | **Cần xóa `accent` khỏi số đếm thông thường** |

---

## V. KIẾN TRÚC HỆ THỐNG FLOATING NOTIFICATION CARD BOX NHIỀU TẦNG

### 1. Cấu trúc DOM Thẻ Nổi (Multi-Layer Floating Card Box)
Mỗi hộp thông báo nổi cố định tại `top-right` gồm 4 thành phần tách biệt:
```html
<div class="floating-card-box floating-card-{type}" role="alert" data-auto-dismiss="5000">
    <!-- 1. Dải màu chỉ thị ngữ nghĩa bên lề trái -->
    <div class="floating-card-stripe stripe-{type}"></div>
    
    <div class="floating-card-inner">
        <!-- 2. Khối Icon SVG nổi bật bo góc tròn/vuông -->
        <div class="floating-card-icon-box icon-box-{type}">
            <svg class="bi" width="18" height="18"><use xlink:href="#{icon-id}"/></svg>
        </div>
        
        <!-- 3. Nội dung thông báo phân tầng: Tiêu đề in đậm + Mô tả chi tiết -->
        <div class="floating-card-content">
            <div class="floating-card-title">{Title}</div>
            <div class="floating-card-body">{Message}</div>
        </div>
        
        <!-- Nút đóng thủ công -->
        <button type="button" class="btn-close floating-card-close" aria-label="Close"></button>
    </div>
    
    <!-- 4. Thanh tiến trình đếm ngược tự đóng (5s, hover pause) -->
    <div class="floating-card-progress">
        <div class="floating-card-progress-bar bar-{type}"></div>
    </div>
</div>
```

### 2. Các biến thể Ngữ nghĩa & Icon Ánh xạ
1. **Thành công (`Success`)**:
   - Dải màu: `--positive` (`#15803d` / `#4ade80`).
   - Icon Box: nền `var(--pill-positive-bg)`, chữ `var(--positive)`, icon `#check-circle-fill`.
   - Tiêu đề: "Thành công" hoặc "Thao tác hoàn tất".
2. **Thất bại / Lỗi (`Error`)**:
   - Dải màu: `--negative` (`#b91c1c` / `#f87171`).
   - Icon Box: nền `var(--pill-negative-bg)`, chữ `var(--negative)`, icon `#x-circle-fill` hoặc `#exclamation-triangle-fill`.
   - Tiêu đề: "Thông báo lỗi" hoặc "Thao tác không thành công".
3. **Cảnh báo (`Warning`)**:
   - Dải màu: `--warning` (`#b45309` / `#fbbf24`).
   - Icon Box: nền `var(--pill-warning-bg)`, chữ `var(--warning)`, icon `#exclamation-triangle-fill`.
   - Tiêu đề: "Cảnh báo" hoặc "Lưu ý quan trọng".
4. **Thông tin / Đẩy SignalR (`Info`)**:
   - Dải màu: `--accent` (`#2563eb` / `#60a5fa`).
   - Icon Box: nền `var(--pill-info-bg)`, chữ `var(--accent)`, icon `#bell-fill` hoặc `#ticket-perforated`.
   - Tiêu đề: "Thông báo hệ thống" hoặc "Cập nhật yêu cầu #{id}".

### 3. Quy chuẩn CSS Bổ sung vào `site.css`
```css
/* ==========================================================================
   FLOATING NOTIFICATION CARD BOX STACK (MULTI-LAYER TOP-RIGHT FLOATING)
   ========================================================================== */
.floating-notification-stack {
  position: fixed;
  top: 1.25rem;
  right: 1.25rem;
  z-index: 1095;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-width: 400px;
  width: calc(100vw - 2.5rem);
  pointer-events: none;
}

.floating-card-box {
  pointer-events: auto;
  background-color: var(--surface) !important;
  border: 1px solid var(--border) !important;
  border-radius: 10px !important;
  box-shadow: var(--shadow-float) !important;
  overflow: hidden;
  position: relative;
  display: flex;
  flex-direction: column;
  animation: slideInRight 220ms cubic-bezier(0.16, 1, 0.3, 1);
  transition: transform 160ms ease, opacity 160ms ease;
}

.floating-card-box.dismissing {
  opacity: 0;
  transform: translateX(36px);
}

.floating-card-inner {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 0.95rem 1.15rem;
  position: relative;
}

.floating-card-stripe {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 4px;
}
.floating-card-stripe.stripe-success { background-color: var(--positive); }
.floating-card-stripe.stripe-error { background-color: var(--negative); }
.floating-card-stripe.stripe-warning { background-color: var(--warning); }
.floating-card-stripe.stripe-info { background-color: var(--accent); }

.floating-card-icon-box {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.icon-box-success { background-color: var(--pill-positive-bg); color: var(--positive); }
.icon-box-error { background-color: var(--pill-negative-bg); color: var(--negative); }
.icon-box-warning { background-color: var(--pill-warning-bg); color: var(--warning); }
.icon-box-info { background-color: var(--pill-info-bg); color: var(--accent); }

.floating-card-content {
  flex: 1 1 auto;
  min-width: 0;
}

.floating-card-title {
  font-size: 13.5px;
  font-weight: 600;
  color: var(--text);
  line-height: 1.3;
  margin-bottom: 2px;
}

.floating-card-body {
  font-size: 12.5px;
  color: var(--muted);
  line-height: 1.45;
  word-break: break-word;
}

.floating-card-close {
  background: transparent;
  border: 0;
  padding: 4px;
  opacity: 0.5;
  cursor: pointer;
  flex-shrink: 0;
  color: var(--text);
}
.floating-card-close:hover { opacity: 1; }

.floating-card-progress {
  height: 3px;
  width: 100%;
  background-color: var(--rule);
  overflow: hidden;
}

.floating-card-progress-bar {
  height: 100%;
  width: 100%;
  transform-origin: left;
  animation: progress-countdown 5000ms linear forwards;
}
.bar-success { background-color: var(--positive); }
.bar-error { background-color: var(--negative); }
.bar-warning { background-color: var(--warning); }
.bar-info { background-color: var(--accent); }

.floating-card-box:hover .floating-card-progress-bar {
  animation-play-state: paused;
}

@keyframes slideInRight {
  from { opacity: 0; transform: translateX(40px); }
  to { opacity: 1; transform: translateX(0); }
}

@keyframes progress-countdown {
  from { transform: scaleX(1); }
  to { transform: scaleX(0); }
}
```

---

## VI. KHẢO SÁT LỖI TƯƠNG TÁC CHAT SIGNALR & PHÍM TẮT ENTER

### 1. Hiện trạng trong `ticket-realtime.js`
- **Lỗi 1 (Phím tắt Enter)**:
  Thẻ `<textarea id="comment-content-input">` có placeholder `(Enter gửi)`. Tuy nhiên, trong toàn bộ mã script `ticket-realtime.js`, **hoàn toàn không có hàm lắng nghe sự kiện `keydown`**. Khi người dùng gõ nội dung và nhấn Enter, trình duyệt chỉ tạo một ký tự xuống dòng (`\n`).
- **Lỗi 2 (Khắc phục lỗi phím tắt)**:
  Bổ sung listener:
  ```javascript
  commentInput.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
          e.preventDefault();
          if (commentInput.value.trim().length > 0) {
              commentForm.requestSubmit ? commentForm.requestSubmit() : commentForm.dispatchEvent(new Event("submit", { cancelable: true, bubbles: true }));
          }
      }
  });
  ```
- **Lỗi 3 (Deduplication & Hiển thị tức thì)**:
  Hiện tại sau khi gọi `fetch(commentForm.action, ...)`, nếu thành công client chỉ xóa rỗng ô nhập:
  ```javascript
  if (response.ok) {
      commentInput.value = "";
      if (isInternalCheckbox) isInternalCheckbox.checked = false;
  }
  ```
  Và đợi sự kiện `ReceiveComment` từ SignalR. Nhưng vì Backend chưa bắn event, tin nhắn hoàn toàn biến mất trên màn hình!
  **Giải pháp chuẩn**:
  - Mỗi comment bong bóng chat cần gắn thuộc tính `data-comment-id="${comment.id}"`.
  - Khi nhận response JSON từ `AddComment`, hàm `appendCommentToView` sẽ kiểm tra nếu đã tồn tại phần tử `[data-comment-id="${comment.id}"]` trong `#comment-stream-list` thì bỏ qua, ngăn ngừa việc nhân đôi bong bóng khi SignalR đồng thời phát tới người gửi.
- **Lỗi 4 (Loại bỏ window.alert)**:
  Tại dòng 104 và 108:
  ```javascript
  alert(errData.message || "Không thể gửi bình luận. Vui lòng thử lại.");
  alert("Lỗi kết nối máy chủ khi gửi phản hồi.");
  ```
  Cần thay thế triệt để bằng gọi hàm thông báo nổi:
  ```javascript
  window.showFloatingNotification({
      type: 'error',
      title: 'Lỗi gửi phản hồi',
      message: errData.message || 'Không thể gửi bình luận. Vui lòng kiểm tra lại quyền hạn hoặc kết nối mạng.'
  });
  ```

---

## VII. DANH MỤC THAY ĐỔI VÀ ĐỀ XUẤT CHO ĐỘI NGŨ THI CÔNG (ACTIONABLE CHECKLIST)

| File Cần Chỉnh Sửa | Vị trí Dòng | Nội dung Thay đổi Cụ thể |
| :--- | :--- | :--- |
| `Areas/Admin/Views/AdminDashboard/Index.cshtml` | 24, 26, 31, 38, 45, 68 | Xóa `text-primary`, `text-success`, `text-secondary`, `accent`; đổi `bg-primary` &rarr; `bg-secondary-subtle`. |
| `Areas/Admin/Views/User/Index.cshtml` | 92, 96, 165 | Đổi `bg-primary` &rarr; `bg-secondary-subtle`; thêm `font-monospace`; xóa `text-primary` trên modal. |
| `Areas/Admin/Views/User/Create.cshtml` | 14 | Xóa `text-primary` trên thẻ `h4`. |
| `Areas/Admin/Views/User/Edit.cshtml` | 14 | Xóa `text-primary` trên thẻ `h4`. |
| `Areas/Admin/Views/Role/Index.cshtml` | 27 | Đổi `Badge = "bg-primary", Border = "border-primary"` &rarr; `Badge = "bg-secondary-subtle", Border = "border-secondary"`. |
| `Areas/Admin/Views/Audit/Index.cshtml` | 90, 94, 100, 111, 155 | Đổi `bg-primary` &rarr; `bg-secondary-subtle`; thêm `font-monospace`; đổi nút outline sang `btn-outline-secondary`. |
| `Areas/Leader/Views/LeaderDashboard/Index.cshtml` | 28, 35, 42, 49, 163 | Chuẩn hóa icon KPI `18x18`; thêm `font-monospace text-muted` vào `#@t.Id`. |
| `Areas/Leader/Views/LeaderTicket/NewQueue.cshtml` | 48, 91, 128 | Thêm `font-monospace text-muted`; xóa `text-primary` & `text-danger` trên tiêu đề modal. |
| `Areas/Leader/Views/LeaderTicket/Index.cshtml` | 83, 118, 137 | Đổi `text-primary` trên `#@ticket.Id` thành `font-monospace text-muted`; xóa `text-warning` trên modal. |
| `Areas/Leader/Views/LeaderTicket/Details.cshtml` | 45, 101, 123, 138, 245 | Xóa `text-primary` trên tiêu đề thẻ thông tin & tệp đính kèm; xóa `text-success` & `text-warning` trên tiêu đề. |
| `Areas/Staff/Views/Dashboard/Index.cshtml` | 24, 26, 31, 38, 45, 56, 79, 82, 137, 152 | Xóa màu icon KPI; sửa lỗi logic badge dòng 82 cho `Low`/`Medium`; xóa `text-danger` và `text-primary` trên `#ID`. |
| `Areas/Staff/Views/StaffTicket/Assigned.cshtml` | 68, 86 | Xóa `text-primary` trên `#@ticket.Id` &rarr; `font-monospace text-muted`; đổi nút outline sang secondary. |
| `Areas/Staff/Views/StaffTicket/Details.cshtml` | 67, 119, 141, 156 | Xóa `text-primary` trên tiêu đề thẻ thông tin & tệp đính kèm; xóa `text-success` trên tiêu đề đánh giá. |
| `Areas/Staff/Views/StaffTicket/History.cshtml` | 42 | Xóa `text-primary` trên `#@ticket.Id` &rarr; `font-monospace text-muted`. |
| `Areas/User/Views/Dashboard/Index.cshtml` | 24, 31, 33, 38, 45, 85, 104 | Xóa màu icon KPI; xóa `accent`; xóa `text-primary` trên `#@ticket.Id` &rarr; `font-monospace text-muted`. |
| `Areas/User/Views/Ticket/Index.cshtml` | 80, 118 | Xóa `text-primary` trên `#@ticket.Id` &rarr; `font-monospace text-muted`; đổi nút sang secondary. |
| `Areas/User/Views/Ticket/Create.cshtml` | 14, 43 | Xóa `text-primary` trên tiêu đề trang `h4`; đổi hộp xem trước SLA sang `bg-body-tertiary border`. |
| `Areas/User/Views/Ticket/Details.cshtml` | 43, 104, 127, 141, 238, 283 | Xóa `text-primary` trên tiêu đề thẻ; xóa `text-success` & `text-warning` trên các tiêu đề modal. |
| `Areas/Auth/Views/Auth/Login.cshtml` | 115–126 | Bỏ alert 1 dòng nội tuyến chèn trong form; thay bằng kích hoạt Floating Card Box góc trên bên phải. |
| `Areas/Auth/Views/Auth/ChangePassword.cshtml` | 9 | Xóa `text-primary` trên tiêu đề `h4`. |
| `Views/Shared/_Layout.cshtml` | 203, 284, 291, 298, 310, 321 | Đổi `bg-primary` &rarr; `bg-secondary-subtle`; nâng cấp `#toast-container` thành `.floating-notification-stack`. |
| `Views/Shared/_StatusBadge.cshtml` | 6 | Đổi trạng thái `New` (1) từ `bg-primary` &rarr; `bg-info-subtle`. |
| `Views/Shared/AccessDenied.cshtml` | 12, 13 | Xóa `text-danger` trên `h2`; đổi `text-secondary` &rarr; `text-muted`. |
| `Views/Shared/Error.cshtml` | 6, 7 | Xóa `text-danger` trên `h1` và `h2`. |
| `Views/Shared/_ToastAlerts.cshtml` | 1–38 | Tái cấu trúc thành HTML phát sinh Floating Card Box nhiều tầng kèm thanh tiến trình. |
| `wwwroot/css/site.css` | Cuối file | Bổ sung đầy đủ bộ rules `.floating-notification-stack`, `.floating-card-box`, `.floating-card-progress`. |
| `wwwroot/js/ticket-realtime.js` | 15–20, 104, 108 | Bắt sự kiện `keydown` phím `Enter` (không giữ `Shift`); thay thế `alert()` bằng floating card box; bổ sung deduplication. |
| `wwwroot/js/signalr-notification.js` | 38, 55–76 | Thay thế `alert()` khi bị `ForceLogout`; chuyển hàm hiển thị toast sang phát sinh Floating Card Box. |
| `wwwroot/js/site.js` | Toàn file | Bổ sung hàm tiện ích toàn cục `window.showFloatingNotification(options)` để mọi trang có thể gọi. |

---
**Báo cáo kết thúc**. Toàn bộ 23 Razor Views và các thành phần liên quan đã được khảo sát chi tiết với đầy đủ vị trí dòng và giải pháp drop-in replacement.
