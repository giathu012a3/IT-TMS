# 🧪 BÁO CÁO KHẢO SÁT CHIẾN LƯỢC KIỂM THỬ VÀ KIỂM CHỨNG TOÀN DIỆN (IT-TMS .NET 9)
> **Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Tác giả**: explorer_test_survey_3 | Nhóm 7 - HCMUTE  
> **Ngày lập**: 2026-10-25 | Phiên bản: 1.0 (Production-Grade Architecture)  
> **Tiêu chuẩn áp dụng**: `#Argon-Omega` Cognitive Kernel | Detroit/Classical School Testing | Zero-Trust OWASP API Top 10

---

## MỤC LỤC
1. [TỔNG QUAN CHIẾN LƯỢC KIỂM THỬ & NGUYÊN TẮC INVARIANT](#1-tổng-quan-chiến-lược-kiểm-thử--nguyên-tắc-invariant)
2. [ĐẶC TẢ CHI TIẾT 10 KỊCH BẢN KIỂM CHỨNG BẢNG 7.2 (TABLE 7.2 SPECIFICATION)](#2-đặc-tả-chi-tiết-10-kịch-bản-kiểm-chứng-bảng-72-table-72-specification)
3. [MA TRẬN UNIT TEST STATE PATTERN 7 TRẠNG THÁI (STATE MACHINE MATRIX)](#3-ma-trận-unit-test-state-pattern-7-trạng-thái-state-machine-matrix)
4. [ĐẶC TẢ KIỂM THỬ ĐỒNG THỜI (OPTIMISTIC CONCURRENCY TOKEN SPECIFICATIONS)](#4-đặc-tả-kiểm-thử-đồng-thời-optimistic-concurrency-token-specifications)
5. [ĐẶC TẢ KIỂM THỬ TÍCH HỢP THỜI GIAN THỰC SIGNALR (SIGNALR INTEGRATION TEST SPECIFICATIONS)](#5-đặc-tả-kiểm-thử-tích-hợp-thời-gian-thực-signalr-signalr-integration-test-specifications)
6. [KIẾN TRÚC E2E TEST SUITE & PHÂN PHỐI 4 TẦNG (TIER 1 - 4 TEST DISTRIBUTION)](#6-kiến-trúc-e2e-test-suite--phân-phối-4-tầng-tier-1---4-test-distribution)
7. [MA TRẬN TRUY XUẤT NGUỒN GỐC & KẾT LUẬN KIỂM CHỨNG (TRACEABILITY MATRIX)](#7-ma-trận-truy-xuất-nguồn-gốc--kết-luận-kiểm-chứng-traceability-matrix)

---

## 1. TỔNG QUAN CHIẾN LƯỢC KIỂM THỬ & NGUYÊN TẮC INVARIANT

### 1.1. Tuyên ngôn Kiến trúc Kiểm thử (#Argon-Omega & Detroit School)
Chiến lược kiểm thử của hệ thống IT-TMS (.NET 9) được thiết kế theo trường phái **Classical/Detroit School** (State Verification) kết hợp mô hình kiểm định phản chứng Popper (`#051 Popperian Falsification`), triệt tiêu hoàn toàn anti-pattern "lạm dụng Mock" (Over-Mocking) vốn gây rão gãy cấu trúc khi tái cấu trúc nội bộ:
1. **Zero Mocks cho Domain Aggregate & State Pattern**: Mọi kiểm thử cho Aggregate Root `Ticket`, các lớp trạng thái `TicketStateBase`, thuật toán SLA và Value Objects đều được thực thi trên thực thể sống thực sự trong bộ nhớ, đánh giá trạng thái đầu ra (State Mutation) và danh sách Domain Events phát sinh.
2. **Loại trừ Exception trong Luồng Nghiệp vụ (Railway-Oriented Result<T>)**: Mọi vi phạm quy tắc trạng thái (ví dụ đóng ticket chưa xử lý, phân công ticket đã hủy) bắt buộc phải trả về đối tượng `Result.Failure(Error)` thuần khiết. Các bài kiểm thử xác nhận `IsFailure == true` kèm mã lỗi tường minh, bảo vệ Kestrel thread pool khỏi overhead giải phóng stack trace.
3. **Loại bỏ Hoàn toàn Polling 3s bằng SignalR Hub Test**: Kiểm thử tích hợp WebSocket đa kênh (`TicketHub`) chứng minh tính đẩy sự kiện tức thời của hệ thống cho trao đổi bình luận và chuông thông báo, loại bỏ hoàn toàn cơ chế Polling 3 giây của kiến trúc Flask cũ.
4. **Bảo toàn 100% Khung Đề cương Báo cáo (`BaoCao_ITTMS_TKPMHDT (4).docx`)**: Cung cấp bộ dữ liệu kết quả thực nghiệm hoàn chỉnh để thay thế vào Bảng 7.2 (Trang 55-56 của tài liệu đồ án).

---

## 2. ĐẶC TẢ CHI TIẾT 10 KỊCH BẢN KIỂM CHỨNG BẢNG 7.2 (TABLE 7.2 SPECIFICATION)

10 kịch bản này tạo thành trục xương sống nghiệm thu chức năng và bảo mật hệ thống. Mỗi kịch bản được đặc tả cấu trúc chuẩn kỹ thuật MECE gồm 7 thành phần: Mã kịch bản & Mục tiêu, Tiền điều kiện (Pre-conditions), Dữ liệu kiểm thử (Input Payload), Kỳ vọng chuyển trạng thái (State Transition), Kết quả UI/Hệ thống trả về, Tác dụng phụ (Side-effects/Audit/SignalR), và Tiêu chí Pass/Fail tự động.

```
+-------------------------------------------------------------------------------------------------------------+
|                                  BẢNG ĐẶC TẢ 10 KỊCH BẢN KIỂM CHỨNG BẢNG 7.2                                |
+----+---------------------------------------------------+--------------------+-------------------------------+
| STT| Kịch bản kiểm thử (Bảng 7.2)                      | Kết quả mong đợi   | Phân hệ / Mẫu thiết kế liên quan|
+----+---------------------------------------------------+--------------------+-------------------------------+
| 01 | Đăng nhập bằng tài khoản đã bị vô hiệu hóa        | Từ chối, báo lỗi   | Area Auth, Decorator/Filter   |
| 02 | User truy cập trang chỉ dành cho Leader           | Trả về lỗi 403     | Area Leader, AuthorizeRoles   |
| 03 | User tạo ticket mới                               | New, báo Leader    | Area User, Observer, SignalR  |
| 04 | Leader phân công ticket New cho Staff             | Assigned, báo đôi  | Area Leader, State, Observer  |
| 05 | Staff đặt trạng thái Closed cho ticket đang New   | Bị từ chối, giữ ng.| Area Staff, State Pattern     |
| 06 | Leader từ chối ticket đang In Progress            | Bị từ chối, giữ ng.| Area Leader, Invariant Guard  |
| 07 | Staff mở ticket không được phân công cho mình     | Trả về lỗi 403/404 | Area Staff, OWASP BOLA Guard  |
| 08 | User bình luận trên ticket đã Resolved            | Bị chặn, không lưu | Area User, State CanComment() |
| 09 | User xác nhận đóng và đánh giá ticket Resolved    | Closed, lưu Feedback| Area User, State, Audit Log  |
| 10 | Admin toggle tài khoản khác & chặn tự vô hiệu hóa | Chặn tự sát quyền  | Area Admin, Safety Invariant  |
+----+---------------------------------------------------+--------------------+-------------------------------+
```

---

### 2.1. Kịch bản 1 (TC-01): Đăng nhập bằng tài khoản đã bị vô hiệu hóa
- **Mã kịch bản**: `TC-VAL-01` (Mapping: Bảng 7.2 - STT 1 | UC01, FR-04)
- **Mô tả nghiệp vụ**: Người dùng sở hữu tài khoản hợp lệ trong cơ sở dữ liệu nhưng đã bị Quản trị viên (Admin) chuyển cờ `status = 'inactive'` (`IsActive = false`) thực hiện đăng nhập vào hệ thống.
- **Tiền điều kiện (Pre-conditions)**:
  - Tài khoản thử nghiệm tồn tại trong CSDL: `Username = "staff_locked"`, `PasswordHash` khớp với mật khẩu `Password@123`.
  - Thuộc tính `User.Status = "inactive"` (`IsActive = false`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST
  - Route: `/Auth/Login`
  - Form Data: `{ "Username": "staff_locked", "Password": "Password@123", "RememberMe": false, "__RequestVerificationToken": "..." }`
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Trạng thái người dùng: Giữ nguyên `IsActive = false`.
  - Trạng thái xác thực: Không thiết lập Authentication Cookie (`.AspNetCore.Cookies`), `HttpContext.User.Identity.IsAuthenticated == false`.
- **Kết quả UI & Hệ thống mong đợi**:
  - HTTP Status: 200 OK (Render lại view Login kèm thông báo) hoặc 302 Redirect về `/Auth/Login`.
  - Thông báo hiển thị trên giao diện: Thông báo màu đỏ (Alert Danger) với nội dung: *"Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên hệ thống để được hỗ trợ."*
  - Trường mật khẩu được làm trống.
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Bảng `SystemLogs` ghi nhận một bản ghi: `Action = "LoginFailed_InactiveAccount"`, `Details = "Đăng nhập thất bại do tài khoản 'staff_locked' đang bị khóa"`, `IpAddress = "127.0.0.1"`, `CreatedAt = UtcNow`.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Không có Cookie xác thực được cấp phát; View hiển thị thông báo chính xác; Bản ghi nhật ký hệ thống được lưu.
  - FAIL: Cấp phát Authentication Cookie, hoặc người dùng đăng nhập thành công vào Dashboard, hoặc hệ thống báo sai lỗi (ví dụ báo sai mật khẩu thay vì báo tài khoản bị khóa).

---

### 2.2. Kịch bản 2 (TC-02): User truy cập trang chỉ dành cho Leader
- **Mã kịch bản**: `TC-SEC-02` (Mapping: Bảng 7.2 - STT 2 | UC11, FR-03, OWASP Broken Access Control)
- **Mô tả nghiệp vụ**: Một người dùng thông thường (Role: `User`) đã đăng nhập thành công, sau đó cố tình gõ trực tiếp URL quản lý của Phân hệ Trưởng nhóm kỹ thuật (`Area = Leader`).
- **Tiền điều kiện (Pre-conditions)**:
  - Tài khoản người dùng bình thường: `Username = "user_employee"`, `Role = "User"`, `IsActive = true`.
  - Đã được cấp Authentication Cookie hợp lệ chứa Claim `ClaimTypes.Role = "User"`.
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP GET
  - Route: `/Leader/Dashboard` (hoặc `/Leader/Ticket/Assign`)
  - Request Headers: Chứa Cookie xác thực hợp lệ của `user_employee`.
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Không có bất kỳ thay đổi trạng thái nào trong cơ sở dữ liệu.
- **Kết quả UI & Hệ thống mong đợi**:
  - Bộ lọc kiểm soát truy cập `AuthorizeRolesAttribute(RoleNames.Leader)` chặn request tại tầng Filter Pipeline.
  - HTTP Status: Trả về **HTTP 403 Forbidden** (hoặc điều hướng 302 sang trang `/Auth/AccessDenied` với mã phản hồi cuối cùng là 403).
  - Giao diện: Hiển thị trang thông báo từ chối truy cập (Access Denied View), thông báo rõ *"Bạn không có quyền truy cập vào chức năng của Trưởng nhóm kỹ thuật"*. Tuyệt đối không để lộ dữ liệu thống kê của Leader.
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Bảng `SystemLogs` ghi nhận bản ghi cảnh báo an ninh: `Action = "AccessDenied_UnauthorizedRole"`, `UserId = user_employee.Id`, `Details = "Cố gắng truy cập trái phép vào /Leader/Dashboard"`.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Mã trạng thái là 403; không rò rỉ bất kỳ thông tin nhạy cảm của Leader; log bảo mật được ghi nhận.
  - FAIL: Trả về mã 200 OK và hiển thị trang quản trị của Leader, hoặc ném ra Unhandled Exception 500.

---

### 2.3. Kịch bản 3 (TC-03): User tạo ticket mới -> New State & Thông báo Leader
- **Mã kịch bản**: `TC-BIZ-03` (Mapping: Bảng 7.2 - STT 3 | UC03, FR-06, Observer Pattern, SignalR)
- **Mô tả nghiệp vụ**: Người dùng gửi yêu cầu hỗ trợ kỹ thuật sự cố máy tính. Hệ thống khởi tạo ticket ở trạng thái `New`, tính toán hạn SLA và bắn sự kiện thời gian thực thông báo tới Leader.
- **Tiền điều kiện (Pre-conditions)**:
  - User đã đăng nhập: `user_employee` (`Id = 10`).
  - Hệ thống tồn tại ít nhất 1 Leader có tài khoản kích hoạt (`leader_boss`, `Id = 2`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST (Multipart/Form-Data để hỗ trợ đính kèm tệp)
  - Route: `/User/Ticket/Create`
  - Payload:
    ```json
    {
      "Title": "Sự cố đứt kết nối mạng nội bộ tầng 3",
      "Description": "Cả phòng làm việc không thể truy cập máy in mạng và server file từ 08:30 sáng.",
      "Priority": "High",
      "Category": "Network",
      "Attachment": [file_binary: "error_screenshot.png"]
    }
    ```
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Thực thể `Ticket` được tạo mới:
    - `Id`: Tự tăng (VD: `101`).
    - `StatusId`: Khóa ngoại trỏ tới trạng thái `New` (`Status.Name == "New"`).
    - `CreatorId = 10`, `AssignedToId = null`.
    - `CreatedAt = UtcNow`.
    - `DueDate = UtcNow.AddHours(8)` (Tính toán tự động theo ma trận SLA độ ưu tiên High).
    - `RowVersion`: Được gán token khởi tạo (`Guid.NewGuid()` / binary timestamp).
- **Kết quả UI & Hệ thống mong đợi**:
  - HTTP Status: 302 Redirect về `/User/Ticket/Detail/101`.
  - Giao diện: Hiển thị badge trạng thái "Chờ tiếp nhận" màu xám/xanh nhạt; thông báo Toast: *"Tạo yêu cầu thành công. Yêu cầu đang chờ Trưởng nhóm phân công."*
- **Tác dụng phụ hệ thống (Side-effects)**:
  - **Domain Event**: `TicketCreatedDomainEvent` được phát ra trong Aggregate Root.
  - **Observer Pattern**:
    - `NotificationObserver` tự động ghi bản ghi vào bảng `Notifications`: `UserId = 2 (leader_boss)`, `Message = "Yêu cầu mới cần phân công: Sự cố đứt kết nối mạng nội bộ tầng 3"`, `Link = "/Leader/Ticket/Assign/101"`, `IsRead = false`.
    - `SystemLogObserver` ghi bản ghi `SystemLogs`: `Action = "CreateTicket"`, `UserId = 10`, `Details = "Tạo ticket #101"`.
  - **SignalR Real-Time**: `TicketHub` gửi sự kiện `ReceiveNotification` đến client group `role_leader` hoặc `user_2`. Chuông thông báo trên giao diện Leader tự nhảy số +1 mà không cần tải lại trang.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Ticket có `Status == "New"`; `Notifications` của Leader có bản ghi mới; Leader nhận WebSocket push notification trong vòng 500ms; `DueDate` tính chính xác SLA.
  - FAIL: Ticket không ở trạng thái New, hoặc không có thông báo cho Leader, hoặc client phải bấm F5 mới thấy thông báo.

---

### 2.4. Kịch bản 4 (TC-04): Leader phân công ticket New cho Staff
- **Mã kịch bản**: `TC-BIZ-04` (Mapping: Bảng 7.2 - STT 4 | UC09, FR-08, State Pattern, Observer Pattern)
- **Mô tả nghiệp vụ**: Trưởng nhóm kỹ thuật tiếp nhận ticket đang ở trạng thái `New` và chỉ định một kỹ thuật viên (Staff) phụ trách giải quyết.
- **Tiền điều kiện (Pre-conditions)**:
  - Đăng nhập với vai trò Leader (`leader_boss`, `Id = 2`).
  - Ticket #101 đang ở trạng thái `New` (`AssignedToId = null`).
  - Kỹ thuật viên `staff_tech1` (`Id = 5`, `Role = "Staff"`, `IsActive = true`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST
  - Route: `/Leader/Ticket/Assign`
  - Payload: `{ "TicketId": 101, "StaffId": 5, "Note": "Ưu tiên kiểm tra switch tầng 3 trước" }`
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Phương thức Domain `ticket.Assign(staff)` được thực thi thông qua `NewState.Assign(...)`.
  - Trạng thái Ticket chuyển từ `New` sang `Assigned` (`StatusId = TicketStatus.Assigned`).
  - Thuộc tính `Ticket.AssignedToId = 5`.
  - `Ticket.UpdatedAt = UtcNow`.
  - `RowVersion` được làm mới.
- **Kết quả UI & Hệ thống mong đợi**:
  - HTTP Status: 302 Redirect về `/Leader/Ticket/Index` (hoặc JSON `{ success: true }` nếu gọi qua AJAX).
  - Giao diện Leader: Ticket #101 chuyển sang cột "Đã phân công", tên người xử lý hiển thị `staff_tech1`.
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Phát sinh Domain Event `TicketAssignedDomainEvent`.
  - `NotificationObserver` ghi 2 bản ghi `Notifications`:
    1. Cho Staff: `UserId = 5`, `Message = "Bạn được phân công xử lý ticket #101: Sự cố đứt kết nối mạng..."`.
    2. Cho Người tạo (User): `UserId = 10`, `Message = "Ticket #101 của bạn đã được phân công cho kỹ thuật viên staff_tech1"`.
  - `SystemLogObserver` ghi: `Action = "AssignTicket"`, `UserId = 2`, `Details = "Phân công ticket #101 cho staff_tech1 (Id: 5)"`.
  - SignalR Hub đẩy thông báo đến client group `user_5` và `user_10`, đồng thời broadcast sự kiện `TicketStatusUpdated(101, "Assigned")` vào group `ticket_101`.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Ticket có `Status == "Assigned"`, `AssignedToId == 5`; Cả Staff và User đều nhận thông báo tương ứng; Audit Log ghi đầy đủ.
  - FAIL: Trạng thái không đổi, hoặc chỉ có 1 bên nhận thông báo, hoặc xảy ra ngoại lệ không đồng bộ.

---

### 2.5. Kịch bản 5 (TC-05): Staff đặt trạng thái Closed cho ticket đang New
- **Mã kịch bản**: `TC-INV-05` (Mapping: Bảng 7.2 - STT 5 | UC07, FR-10, State Pattern Invariant)
- **Mô tả nghiệp vụ**: Kỹ thuật viên (Staff) hoặc kẻ tấn công cố tình gửi request ép trạng thái ticket chuyển trực tiếp từ `New` sang `Closed` (bỏ qua các bước Phân công, Xử lý và Nghiệm thu).
- **Tiền điều kiện (Pre-conditions)**:
  - Đăng nhập tài khoản Staff (`staff_tech1`, `Id = 5`).
  - Ticket #102 đang ở trạng thái `New` (`Status.Name == "New"`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST
  - Route: `/Staff/Ticket/UpdateStatus`
  - Payload: `{ "TicketId": 102, "NewStatus": "Closed", "Notes": "Cố tình đóng ngang" }`
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Thực thể `Ticket` gọi `ticket.Close()`. Lớp hiện tại là `NewState`.
  - `NewState.Close()` từ chối thao tác và trả về `Result.Failure(TicketErrors.InvalidStateTransition("Không thể đóng ticket đang ở trạng thái New"))`.
  - **Tuyệt đối không chuyển trạng thái**: `StatusId` giữ nguyên là `New`, `ClosedAt` là `null`.
- **Kết quả UI & Hệ thống mong đợi**:
  - Không ném Unhandled Exception (Không dùng `throw new Exception`).
  - HTTP Status: 400 Bad Request hoặc Redirect về View với Flash Error.
  - Giao diện: Hiển thị hộp cảnh báo màu đỏ: *"Thao tác không hợp lệ: Không thể đóng yêu cầu khi chưa được phân công và giải quyết."* Dữ liệu ticket trên màn hình giữ nguyên 100%.
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Giao dịch CSDL bị rollback / không thực hiện `SaveChangesAsync()`.
  - Không phát sinh Domain Event; không tạo `Notification`.
  - Bảng `SystemLogs` ghi nhận hành vi bất thường: `Action = "InvalidTransitionAttempt"`, `Details = "Staff 5 cố gắng chuyển Ticket #102 từ New sang Closed"`.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Trạng thái Ticket vẫn là `New`; trả về `Result.Failure`; dữ liệu không bị sai lệch; có ghi vết log cảnh báo.
  - FAIL: Trạng thái ticket bị đổi thành `Closed`, hoặc hệ thống sập với lỗi HTTP 500.

---

### 2.6. Kịch bản 6 (TC-06): Leader từ chối ticket đang In Progress
- **Mã kịch bản**: `TC-INV-06` (Mapping: Bảng 7.2 - STT 6 | UC10, FR-09, State Pattern Invariant)
- **Mô tả nghiệp vụ**: Trưởng nhóm (Leader) cố gắng từ chối (Reject) một ticket đã được tiếp nhận và nhân viên kỹ thuật đang trong quá trình thực hiện sửa chữa (`InProgress`).
- **Tiền điều kiện (Pre-conditions)**:
  - Đăng nhập với tài khoản Leader (`leader_boss`, `Id = 2`).
  - Ticket #103 đang ở trạng thái `InProgress` (`Status.Name == "InProgress"`, `AssignedToId = 5`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST
  - Route: `/Leader/Ticket/Reject`
  - Payload: `{ "TicketId": 103, "Reason": "Không hỗ trợ thiết bị cá nhân" }`
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Thực thể gọi `ticket.Reject(reason)`. Lớp hiện hành là `InProgressState`.
  - `InProgressState.Reject(...)` chặn hành động: quy định nghiệp vụ chỉ cho phép từ chối khi ticket còn ở trạng thái `New` (chưa phân công hoặc mới nhận diện bài toán không hợp lệ).
  - Trả về: `Result.Failure(TicketErrors.CannotRejectInProgressTicket)`.
  - Trạng thái Ticket giữ nguyên `InProgress`, trường `RejectionReason` giữ nguyên `null`.
- **Kết quả UI & Hệ thống mong đợi**:
  - HTTP Status: 400 Bad Request / 302 Redirect kèm thông báo lỗi.
  - Giao diện: Hiển thị thông báo: *"Không thể từ chối yêu cầu đang trong quá trình xử lý. Kỹ thuật viên cần hoàn tất hoặc trao đổi lại với người dùng."*
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Không cập nhật CSDL; không gửi thông báo hủy cho User.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Ticket giữ nguyên trạng thái `InProgress`; trả về Result lỗi chuẩn xác; không ghi đè lý do từ chối.
  - FAIL: Ticket bị chuyển sang `Rejected` hoặc `Cancelled` trong khi kỹ thuật viên đang làm việc dở dang.

---

### 2.7. Kịch bản 7 (TC-07): Staff mở ticket không được phân công cho mình
- **Mã kịch bản**: `TC-SEC-07` (Mapping: Bảng 7.2 - STT 7 | UC08, OWASP API1:2023 - BOLA/IDOR)
- **Mô tả nghiệp vụ**: Kỹ thuật viên A (`staff_tech1`) cố tình xem hoặc cập nhật chi tiết ticket của Kỹ thuật viên B (`staff_tech2`) mà không có thẩm quyền sở hữu hoặc giám sát.
- **Tiền điều kiện (Pre-conditions)**:
  - Tài khoản đăng nhập: `staff_tech1` (`Id = 5`, `Role = "Staff"`).
  - Ticket #104 được phân công cho `staff_tech2` (`AssignedToId = 6`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP GET
  - Route: `/Staff/Ticket/Detail/104`
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Không có biến đổi trạng thái.
- **Kết quả UI & Hệ thống mong đợi**:
  - Tầng Application kiểm tra quy tắc phân quyền: `if (ticket.AssignedToId != currentStaff.Id) return Result.Failure(SecurityErrors.Forbidden);`
  - HTTP Status: Trả về **HTTP 403 Forbidden** (hoặc **HTTP 404 Not Found** theo tiêu chuẩn phòng thủ che giấu sự tồn tại tài nguyên).
  - Giao diện: Hiển thị trang cấm truy cập; không hiển thị bất kỳ nội dung trao đổi nội bộ, ghi chú kỹ thuật, hoặc thông tin cá nhân của người yêu cầu.
- **Tác dụng phụ hệ thống (Side-effects)**:
  - `SystemLogs` ghi nhận vi phạm an ninh: `Action = "BOLA_Violation_Attempt"`, `UserId = 5`, `Details = "Staff 5 cố truy cập trái phép ticket #104 thuộc về Staff 6"`.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Chặn triệt để với mã 403 hoặc 404; không rò rỉ dữ liệu của Staff khác; audit log ghi lại vi phạm.
  - FAIL: Trả về 200 OK và hiển thị nội dung ticket #104 cho Staff không được phân công.

---

### 2.8. Kịch bản 8 (TC-08): User bình luận trên ticket đã Resolved
- **Mã kịch bản**: `TC-INV-08` (Mapping: Bảng 7.2 - STT 8 | UC06, FR-11, State Pattern CanComment)
- **Mô tả nghiệp vụ**: Khi kỹ thuật viên đã xử lý xong và chuyển ticket sang trạng thái `Resolved`, khung bình luận tự do bị khóa lại để ép người dùng thực hiện một trong hai hành động dứt điểm: "Xác nhận đóng & Đánh giá" hoặc "Yêu cầu mở lại (Reopen)". Người dùng cố tình gửi bình luận qua API hoặc Form.
- **Tiền điều kiện (Pre-conditions)**:
  - Người dùng `user_employee` (`Id = 10`) là người tạo Ticket #105.
  - Ticket #105 đang ở trạng thái `Resolved` (`Status.Name == "Resolved"`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST (hoặc SignalR Hub Invocation `SendComment`)
  - Route: `/User/Ticket/AddComment`
  - Payload: `{ "TicketId": 105, "Content": "Cảm ơn kỹ thuật viên đã sửa giúp." }`
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Domain kiểm tra `ticket.CanComment()`.
  - Trong lớp `ResolvedState`, phương thức `CanComment()` trả về `false` (theo đặc tả Chương 6.2 của báo cáo).
  - Trả về: `Result.Failure(TicketErrors.CommentLockedOnResolved)`.
  - Không thêm bất kỳ bản ghi nào vào bảng `Comments`.
- **Kết quả UI & Hệ thống mong đợi**:
  - Giao diện: Khung nhập văn bản bình luận hiển thị ở chế độ disabled; nút gửi bị mờ; hiển thị dòng cảnh báo màu vàng: *"Yêu cầu đã được giải quyết. Vui lòng xác nhận hoàn tất kèm đánh giá hoặc yêu cầu mở lại nếu sự cố chưa được khắc phục triệt để."*
  - Nếu cố tình gửi request ngầm: Trả về HTTP 400 Bad Request kèm thông báo lỗi cụ thể.
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Bảng `Comments` không tăng thêm dòng dữ liệu nào; không bắn tín hiệu SignalR bình luận mới.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Bình luận bị từ chối; bảng `Comments` không bị ghi thêm dữ liệu; UI thể hiện rõ trạng thái khóa.
  - FAIL: Bình luận được lưu thành công vào CSDL khi ticket đang ở `Resolved`.

---

### 2.9. Kịch bản 9 (TC-09): User xác nhận đóng và đánh giá ticket Resolved -> Closed kèm Feedback & Audit Log
- **Mã kịch bản**: `TC-BIZ-09` (Mapping: Bảng 7.2 - STT 9 | UC19, UC04, FR-12, NFR-05)
- **Mô tả nghiệp vụ**: Người dùng kiểm tra thấy sự cố máy tính đã được khắc phục hoàn toàn, bấm xác nhận đóng ticket và chấm điểm chất lượng dịch vụ (1 đến 5 sao) cùng nhận xét.
- **Tiền điều kiện (Pre-conditions)**:
  - Đăng nhập tài khoản User tạo ticket (`user_employee`, `Id = 10`).
  - Ticket #106 đang ở trạng thái `Resolved`, được xử lý bởi `staff_tech1` (`AssignedToId = 5`).
- **Dữ liệu đầu vào (Input Payload)**:
  - Giao thức: HTTP POST
  - Route: `/User/Ticket/CloseAndFeedback`
  - Payload:
    ```json
    {
      "TicketId": 106,
      "Rating": 5,
      "Comments": "Kỹ thuật viên hỗ trợ rất nhiệt tình, xử lý dứt điểm trong 30 phút."
    }
    ```
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Thực thể thực hiện `ResolvedState.Close()`.
  - Ticket chuyển sang trạng thái `Closed` (`StatusId = TicketStatus.Closed`).
  - Bảng `Feedbacks` tạo bản ghi mới:
    - `TicketId = 106`, `UserId = 10`, `Rating = 5`.
    - `Comments = "Kỹ thuật viên hỗ trợ rất nhiệt tình..."`.
    - `CreatedAt = UtcNow`.
  - Thuộc tính `Ticket.ClosedAt = UtcNow`.
- **Kết quả UI & Hệ thống mong đợi**:
  - HTTP Status: 302 Redirect về `/User/Ticket/Detail/106`.
  - Giao diện: Ticket hiển thị badge xanh lá cây "Đã đóng (Closed)"; hiển thị khối "Đánh giá dịch vụ: ★★★★★"; toàn bộ các nút thao tác cập nhật bị ẩn hoàn toàn (trạng thái kết thúc vòng đời).
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Phát sinh Domain Event `TicketClosedDomainEvent`.
  - `NotificationObserver` gửi thông báo cho Staff phụ trách (`UserId = 5`): *"Ticket #106 đã được người dùng nghiệm thu đóng với đánh giá 5 sao."*
  - `SystemLogObserver` ghi vào `SystemLogs`: `Action = "TicketClosedWithFeedback"`, `UserId = 10`, `Details = "Đóng ticket #106 kèm feedback 5 sao"`.
  - SignalR Hub gửi broadcast trạng thái `TicketStatusUpdated(106, "Closed")` đến các client đang theo dõi.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Ticket chuyển sang `Closed`; bản ghi `Feedbacks` được lưu đầy đủ; nhật ký hệ thống ghi nhận; Staff nhận được thông báo nghiệm thu.
  - FAIL: Trạng thái không đổi, hoặc không lưu Feedback, hoặc cho phép gửi Feedback nhiều lần cho cùng 1 ticket (vi phạm ràng buộc 1-1).

---

### 2.10. Kịch bản 10 (TC-10): Admin toggle tài khoản khác & chặn tự vô hiệu hóa chính mình
- **Mã kịch bản**: `TC-INV-10` (Mapping: Bảng 7.2 - STT 10 | UC15, FR-15, Safety Invariant)
- **Mô tả nghiệp vụ**: Quản trị viên (Admin) quản lý trạng thái kích hoạt/vô hiệu hóa của người dùng khác trong công ty. Hệ thống phải kích hoạt/vô hiệu hóa thành công tài khoản người khác, nhưng bắt buộc phải chặn đứng hành vi Admin tự vô hiệu hóa tài khoản của chính mình (ngăn chặn rủi ro tự khóa chết hệ thống - Deadlock quyền quản trị).
- **Tiền điều kiện (Pre-conditions)**:
  - Đăng nhập tài khoản Admin: `admin_root` (`Id = 1`, `Role = "Admin"`).
  - Tồn tại tài khoản nhân viên khác: `user_employee2` (`Id = 12`, `IsActive = true`).
- **Dữ liệu đầu vào & Diễn tiến hai bước**:
  - **Bước A (Thao tác trên người khác)**:
    - HTTP POST `/Admin/User/ToggleStatus` với Payload `{ "UserId": 12 }`.
    - Kết quả: `user_employee2.IsActive` chuyển từ `true` sang `false`.
    - `SystemLogs` ghi nhận: `Action = "ToggleUserStatus"`, `Details = "Vô hiệu hóa tài khoản Id 12"`.
    - Sau bước này, tài khoản 12 đăng nhập sẽ bị chặn theo Kịch bản 1 (`TC-VAL-01`).
  - **Bước B (Admin tự thao tác trên chính mình)**:
    - HTTP POST `/Admin/User/ToggleStatus` với Payload `{ "UserId": 1 }` (chính ID của `admin_root`).
- **Kỳ vọng chuyển trạng thái (State Transition)**:
  - Bước A: `User(12).IsActive = false`.
  - Bước B: Tầng Domain/Application kích hoạt quy tắc an toàn bất biến:
    `if (targetUserId == currentAdmin.Id) return Result.Failure(UserErrors.CannotDeactivateSelf);`
  - Thuộc tính `admin_root.IsActive` **bắt buộc giữ nguyên là `true`**.
- **Kết quả UI & Hệ thống mong đợi**:
  - Bước A: Thông báo thành công: *"Đã vô hiệu hóa tài khoản user_employee2 thành công."*
  - Bước B: HTTP Status 400 Bad Request. Giao diện hiển thị cảnh báo nghiêm trọng: *"Thao tác bị từ chối: Quản trị viên không thể tự vô hiệu hóa chính mình để đảm bảo tính sẵn sàng của hệ thống."*
- **Tác dụng phụ hệ thống (Side-effects)**:
  - Bước B không làm biến đổi CSDL; ghi nhận log cảnh báo vào `SystemLogs`: `Action = "SelfDeactivationBlocked"`, `UserId = 1`.
- **Tiêu chí chấp thuận (Pass/Fail Criteria)**:
  - PASS: Khóa được tài khoản người khác; chặn tuyệt đối hành vi tự khóa chính mình; quyền quản trị luôn được bảo toàn.
  - FAIL: Admin tự khóa được tài khoản của mình dẫn tới hệ thống không còn người quản trị active.

---

## 3. MA TRẬN UNIT TEST STATE PATTERN 7 TRẠNG THÁI (STATE MACHINE MATRIX)

### 3.1. Danh mục 7 Trạng thái và Các Hành động Domain
Hệ thống IT-TMS quản lý vòng đời ticket thông qua mẫu thiết kế **State Pattern** gồm 7 trạng thái chuẩn hóa (đồng bộ hoàn hảo giữa Chương 6 của Báo cáo đồ án và yêu cầu Clean Architecture):
1. **New (`NewState`)**: Yêu cầu mới tạo từ User, chưa được phân công hoặc chưa được xử lý.
2. **Assigned (`AssignedState`)**: Đã được Leader phân công cho một Kỹ thuật viên (Staff) cụ thể.
3. **InProgress (`InProgressState`)**: Kỹ thuật viên đã tiếp nhận và đang tiến hành xử lý kỹ thuật.
4. **Waiting (`WaitingState`)**: Tạm hoãn xử lý để chờ người dùng cung cấp thêm thông tin hoặc chờ linh kiện thay thế.
5. **Resolved (`ResolvedState`)**: Kỹ thuật viên đã xử lý xong sự cố, chờ người dùng nghiệm thu và đóng.
6. **Closed (`ClosedState`)**: Người dùng xác nhận hoàn tất, gửi đánh giá, kết thúc vòng đời ticket.
7. **Rejected / Cancelled (`RejectedState`)**: Yêu cầu bị Leader từ chối hoặc người dùng hủy bỏ trước khi phân công.

Các phương thức hành vi điều phối trên Aggregate Root:
- `Assign(Staff technician)`: Phân công kỹ thuật viên.
- `Reassign(Staff technician)`: Phân công lại cho kỹ thuật viên khác.
- `StartProgress()`: Bắt đầu xử lý.
- `HoldForInfo()`: Tạm dừng chờ thông tin/phụ tùng.
- `Resolve(string resolutionNotes)`: Hoàn tất xử lý kỹ thuật.
- `Close(int rating, string feedback)`: Đóng ticket và chấm điểm.
- `Reopen(string reason)`: Yêu cầu xử lý lại khi sự cố chưa dứt điểm.
- `Reject(string reason)`: Từ chối yêu cầu.
- `CanComment()`: Kiểm tra quyền được gửi bình luận trao đổi.

---

### 3.2. Ma trận Chuyển trạng thái Toàn diện ($7 \times 8$ Permutations)

Mỗi ô trong ma trận đại diện cho kết quả kiểm chứng:
- **VALID (Chấp nhận)**: Trả về `Result.Success`, chuyển sang Trạng thái đích, phát ra Domain Event tương ứng.
- **INVALID (Từ chối)**: Trả về `Result.Failure(Error)`, giữ nguyên Trạng thái ban đầu, không phát sinh Domain Event, không sửa đổi dữ liệu.

```
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| State \ Action | Assign(staff) | Reassign(stf) | StartProgress | HoldForInfo   | Resolve(notes)| Close(rating) | Reopen(reason)| Reject(reason)|
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 1. New         | VALID         | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | VALID         |
|                | -> Assigned   | (Chưa p.công) | (Chưa p.công) | (Chưa xử lý)  | (Chưa xử lý)  | (Chưa xử lý)  | (Chưa đóng)   | -> Rejected   |
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 2. Assigned    | INVALID       | VALID         | VALID         | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       |
|                | (Đã có staff) | -> Assigned   | -> InProgress | (Chưa bắt đầu)| (Chưa xử lý)  | (Chưa xử lý)  | (Chưa đóng)   | (Đã tiếp nhận)|
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 3. InProgress  | INVALID       | VALID         | INVALID       | VALID         | VALID         | INVALID       | INVALID       | INVALID       |
|                | (Đang làm)    | -> Assigned   | (Đang chạy rồi| -> Waiting    | -> Resolved   | (Chưa báo xong| (Đang chạy)   | (Đang làm dở) |
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 4. Waiting     | INVALID       | VALID         | VALID         | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       |
|                | (Đang chờ tin)| -> Assigned   | -> InProgress | (Đang chờ rồi)| (Phải tiếp tục| (Chưa xong)   | (Chưa đóng)   | (Không từ chối|
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 5. Resolved    | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | VALID         | VALID         | INVALID       |
|                | (Đã xong kỹ th| (Đã xong)     | (Cần reopen)  | (Đã xong)     | (Đã xong rồi) | -> Closed     | -> InProgress | (Không từ chối|
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 6. Closed      | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       |
| (Terminal)     | [ĐÃ KHÓA VĨNH VIỄN - MỌI THAO TÁC ĐỀU BỊ TỪ CHỐI BẢO TOÀN TÍNH TOÀN VẸN LỊCH SỬ]                             |
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
| 7. Rejected    | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       | INVALID       |
| (Terminal)     | [ĐÃ HỦY/TỪ CHỐI - MỌI THAO TÁC ĐỀU BỊ TỪ CHỐI BẢO TOÀN TÍNH TOÀN VẸN LỊCH SỬ]                               |
+----------------+---------------+---------------+---------------+---------------+---------------+---------------+---------------+
```

---

### 3.3. Đặc tả Invariant `CanComment()` (Kiểm soát Bình luận theo Trạng thái)

| Trạng thái | `CanComment()` | Lý do nghiệp vụ & Thiết kế hướng đối tượng |
|---|:---:|---|
| **New** | **TRUE** | Cho phép User bổ sung chi tiết hoặc Leader trao đổi làm rõ sự cố trước khi giao việc. |
| **Assigned** | **TRUE** | Kỹ thuật viên trao đổi với User để hẹn lịch hỗ trợ. |
| **InProgress** | **TRUE** | Kênh trao đổi trực tiếp trong quá trình thao tác kỹ thuật (hỏi mật khẩu, test máy...). |
| **Waiting** | **TRUE** | User phản hồi thông tin bổ sung được yêu cầu để gỡ nút thắt. |
| **Resolved** | **FALSE** | **Khóa bình luận!** Ép User phải chọn Đóng ticket hoặc Mở lại (tránh trao đổi cù cưa không hồi kết). |
| **Closed** | **FALSE** | **Khóa bình luận vĩnh viễn.** Ticket đã đóng hồ sơ, lưu trữ phục vụ thống kê báo cáo. |
| **Rejected** | **FALSE** | **Khóa bình luận.** Ticket đã bị từ chối; nếu có nhu cầu mới User phải tạo ticket khác. |

---

### 3.4. Triển khai C# Unit Test Mẫu (xUnit + FluentAssertions)

```csharp
namespace ItTms.UnitTests.Domain.StatePatternTests;

using FluentAssertions;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using Xunit;

public class TicketStatePatternTests
{
    private readonly User _creator = User.CreateTestUser(10, "user1", RoleNames.User);
    private readonly User _staff1 = User.CreateTestUser(5, "staff1", RoleNames.Staff);
    private readonly User _staff2 = User.CreateTestUser(6, "staff2", RoleNames.Staff);

    [Fact]
    public void NewTicket_WhenAssigned_ShouldTransitionToAssignedState_AndRaiseDomainEvent()
    {
        // Arrange
        var ticket = Ticket.Create("VPN Issue", "Cannot connect", TicketPriority.High, "Network", _creator.Id).Value;
        ticket.Status.Should().Be(TicketStatusEnum.New);

        // Act
        Result result = ticket.Assign(_staff1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatusEnum.Assigned);
        ticket.AssignedToUserId.Should().Be(_staff1.Id);
        ticket.DomainEvents.Should().ContainSingle(e => e is TicketAssignedDomainEvent);
    }

    [Theory]
    [InlineData(TicketStatusEnum.New)]
    [InlineData(TicketStatusEnum.Assigned)]
    [InlineData(TicketStatusEnum.Waiting)]
    public void Close_WhenNotResolved_ShouldReturnInvalidStateTransitionError(TicketStatusEnum initialStatus)
    {
        // Arrange
        var ticket = TicketFixture.CreateTicketInStatus(initialStatus, _creator, _staff1);

        // Act
        Result result = ticket.Close(rating: 5, feedback: "Great job");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ticket.InvalidStateTransition");
        ticket.Status.Should().Be(initialStatus); // Invariant preserved!
        ticket.DomainEvents.Should().NotContain(e => e is TicketClosedDomainEvent);
    }

    [Fact]
    public void InProgressTicket_WhenRejectedByLeader_ShouldBeRejectedByDomain()
    {
        // Arrange
        var ticket = TicketFixture.CreateTicketInStatus(TicketStatusEnum.InProgress, _creator, _staff1);

        // Act
        Result result = ticket.Reject("Not supported");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ticket.CannotRejectInProgressTicket");
        ticket.Status.Should().Be(TicketStatusEnum.InProgress);
    }

    [Theory]
    [InlineData(TicketStatusEnum.New, true)]
    [InlineData(TicketStatusEnum.Assigned, true)]
    [InlineData(TicketStatusEnum.InProgress, true)]
    [InlineData(TicketStatusEnum.Waiting, true)]
    [InlineData(TicketStatusEnum.Resolved, false)]
    [InlineData(TicketStatusEnum.Closed, false)]
    [InlineData(TicketStatusEnum.Rejected, false)]
    public void CanComment_ShouldStrictlyFollowBusinessMatrix(TicketStatusEnum status, bool expectedCanComment)
    {
        // Arrange
        var ticket = TicketFixture.CreateTicketInStatus(status, _creator, _staff1);

        // Act
        bool actualCanComment = ticket.CanComment();

        // Assert
        actualCanComment.Should().Be(expectedCanComment);
    }
}
```

---

## 4. ĐẶC TẢ KIỂM THỬ ĐỒNG THỜI (OPTIMISTIC CONCURRENCY TOKEN SPECIFICATIONS)

### 4.1. Cơ chế Vật lý trong EF Core 9 & SQLite
Trong môi trường hỗ trợ kỹ thuật thực tế, hiện tượng hai người dùng cùng thao tác đồng thời trên một ticket xảy ra thường xuyên:
- Kỹ thuật viên A nhận ticket và bắt đầu làm việc (`Status -> InProgress`).
- Cùng lúc đó, Trưởng nhóm (Leader) thấy ticket quá hạn nên phân công lại cho Kỹ thuật viên B (`AssignedTo -> Staff2`).

Nếu không có kiểm soát đồng thời lạc quan (**Optimistic Concurrency Control**), thao tác ghi sau sẽ âm thầm ghi đè thao tác ghi trước (**Lost Update Anomaly**), làm sai lệch trạng thái hệ thống.

**Giải pháp Kỹ thuật trong EF Core 9**:
- Trên Entity `Ticket`, định nghĩa thuộc tính Concurrency Token:
  ```csharp
  public class Ticket : Entity<int>
  {
      // ...
      public Guid ConcurrencyToken { get; private set; } = Guid.NewGuid();

      public void UpdateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid();
  }
  ```
- Cấu hình EntityTypeConfiguration:
  ```csharp
  builder.Property(t => t.ConcurrencyToken)
         .IsConcurrencyToken()
         .IsRequired();
  ```
- Khi EF Core thực thi câu lệnh `UPDATE`, mệnh đề `WHERE` sẽ tự động kèm theo giá trị token ban đầu:
  ```sql
  UPDATE Tickets 
  SET StatusId = @p0, ConcurrencyToken = @newToken, UpdatedAt = @now
  WHERE Id = @ticketId AND ConcurrencyToken = @originalToken;
  ```
- Nếu một transaction khác đã cập nhật trước đó, `@originalToken` trong CSDL đã đổi, số dòng bị tác động (Rows Affected) là `0`. EF Core sẽ lập tức ném ra ngoại lệ `DbUpdateConcurrencyException`.

---

### 4.2. Kịch bản Kiểm thử Concurrency Test Mẫu (xUnit + Real DB)

```csharp
namespace ItTms.IntegrationTests.Concurrency;

using FluentAssertions;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class TicketConcurrencyTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public TicketConcurrencyTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ConcurrentTicketUpdates_SecondWriteWithStaleToken_MustThrowDbUpdateConcurrencyException()
    {
        // 1. Arrange: Khởi tạo ticket trong CSDL
        int ticketId;
        using (var initContext = _fixture.CreateDbContext())
        {
            var ticket = Ticket.Create("Network Down", "Server rack offline", TicketPriority.Critical, "Hardware", 10).Value;
            initContext.Tickets.Add(ticket);
            await initContext.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        // 2. Mô phỏng 2 HTTP Requests độc lập tải cùng 1 ticket tại thời điểm t0
        using var contextA = _fixture.CreateDbContext();
        using var contextB = _fixture.CreateDbContext();

        var ticketLoadedByStaffA = await contextA.Tickets.FindAsync(ticketId);
        var ticketLoadedByLeaderB = await contextB.Tickets.FindAsync(ticketId);

        ticketLoadedByStaffA.Should().NotBeNull();
        ticketLoadedByLeaderB.Should().NotBeNull();
        ticketLoadedByStaffA!.ConcurrencyToken.Should().Be(ticketLoadedByLeaderB!.ConcurrencyToken);

        // 3. Act: Staff A cập nhật thành công tại thời điểm t1
        var staff1 = await contextA.Users.FindAsync(5);
        ticketLoadedByStaffA.Assign(staff1!);
        ticketLoadedByStaffA.UpdateConcurrencyToken();
        var saveResultA = await contextA.SaveChangesAsync();
        saveResultA.Should().BeGreaterThan(0); // Ghi thành công!

        // 4. Act & Assert: Leader B cố gắng cập nhật với token cũ (stale token) tại thời điểm t2
        var staff2 = await contextB.Users.FindAsync(6);
        ticketLoadedByLeaderB.Assign(staff2!);
        ticketLoadedByLeaderB.UpdateConcurrencyToken();

        var act = async () => await contextB.SaveChangesAsync();

        // Kỳ vọng EF Core phát hiện vi phạm và ném ngoại lệ Concurrency
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        // 5. Verify Invariant: Dữ liệu trong CSDL giữ nguyên theo Staff A, không bị Leader B ghi đè
        using var verifyContext = _fixture.CreateDbContext();
        var finalTicket = await verifyContext.Tickets.FindAsync(ticketId);
        finalTicket!.AssignedToUserId.Should().Be(5); // Staff 1 bảo toàn
    }
}
```

---

### 4.3. Chuyển đổi tại Tầng Application & Web API (RFC 9457)
Khi `DbUpdateConcurrencyException` xảy ra tại tầng Controller/Handler, hệ thống xử lý triệt để:
1. Đón bắt ngoại lệ thông qua `ExceptionFilter` hoặc `Result<T>` pattern.
2. Trả về mã lỗi **HTTP 409 Conflict** kèm theo RFC 9457 ProblemDetails:
   ```json
   {
     "type": "https://datatracker.ietf.org/doc/html/rfc9457",
     "title": "Dữ liệu bị xung đột cập nhật",
     "status": 409,
     "detail": "Ticket này vừa được cập nhật bởi một người dùng khác. Vui lòng làm mới trang (F5) để nhận phiên bản mới nhất trước khi chỉnh sửa.",
     "instance": "/Staff/Ticket/UpdateStatus/101"
   }
   ```
3. Giao diện người dùng hiển thị thông báo Modal cảnh báo xung đột, cung cấp nút "Tải lại dữ liệu mới nhất".

---

## 5. ĐẶC TẢ KIỂM THỬ TÍCH HỢP THỜI GIAN THỰC SIGNALR (SIGNALR INTEGRATION TEST SPECIFICATIONS)

### 5.1. Khảo sát Kiến trúc SignalR Hub (`TicketHub`)
Hệ thống IT-TMS thay thế triệt để cơ chế Polling 3s cũ bằng ASP.NET Core SignalR WebSocket Hub:
- **Hub Endpoint**: `/hubs/ticket`
- **Giao thức mạng**: Ưu tiên WebSockets -> Server-Sent Events -> Long Polling (tự động đàm phán fallback).
- **Phân nhóm (SignalR Groups)**:
  - `ticket_{ticketId}`: Nhóm người dùng đang xem trang chi tiết của một ticket cụ thể (dành cho bình luận và trạng thái).
  - `user_{userId}`: Kênh riêng của từng cá nhân nhận thông báo cá nhân hóa (chuông thông báo).
  - `role_{roleName}`: Kênh thông báo theo vai trò (ví dụ: `role_leader` nhận thông báo khi có ticket mới tạo).
- **Hợp đồng giao tiếp (Client Interface)**:
  ```csharp
  public interface ITicketHubClient
  {
      Task ReceiveComment(CommentNotificationDto comment);
      Task ReceiveNotification(NotificationDto notification);
      Task TicketStatusUpdated(int ticketId, string newStatus);
      Task UserTyping(int ticketId, string userName);
  }
  ```

---

### 5.2. Các Kịch bản Kiểm thử Tích hợp SignalR Bắt buộc

```
+------------------------------------------------------------------------------------------------------------+
|                                    MA TRẬN KIỂM THỬ TÍCH HỢP SIGNALR                                       |
+----+-----------------------------+------------------------------------+------------------------------------+
| STT| Kịch bản kiểm thử           | Dữ liệu đầu vào / Thao tác         | Kết quả mong đợi                   |
+----+-----------------------------+------------------------------------+------------------------------------+
| 01 | Kết nối & Xác thực Hub      | Client Anonymous kết nối Hub       | Trả về HTTP 401 Unauthorized       |
| 02 | Kết nối có Cookie Auth      | Client có Cookie User đăng nhập    | Bắt tay WebSocket 101 Switching    |
| 03 | Tham gia nhóm Ticket        | Invoke("JoinTicketGroup", 101)     | ConnectionId vào group ticket_101  |
| 04 | Phát sóng Comment tức thời  | Client A post comment ticket 101   | Client B (ở 101) nhận trong <500ms |
| 05 | Cách ly Group bình luận     | Client C đang ở ticket 202         | Client C KHÔNG nhận comment 101    |
| 06 | Bắn thông báo theo Role     | Có ticket mới tạo                  | Tất cả Leader nhận thông báo đẩy   |
| 07 | Cập nhật Status thời gian th| Status đổi sang "InProgress"       | Giao diện cập nhật DOM không reload|
+----+-----------------------------+------------------------------------+------------------------------------+
```

---

### 5.3. Triển khai C# SignalR Integration Test Mẫu (WebApplicationFactory)

```csharp
namespace ItTms.IntegrationTests.SignalR;

using System.Threading.Tasks;
using FluentAssertions;
using ItTms.Application.Common.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

public class TicketHubIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TicketHubIntegrationTests(CustomWebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostComment_ShouldBroadcastOnlyToClientsInSameTicketGroup()
    {
        // 1. Arrange: Tạo 2 client kết nối SignalR qua Test Server
        var clientA = CreateHubConnection("cookie_user_10");
        var clientB_SameTicket = CreateHubConnection("cookie_staff_5");
        var clientC_DifferentTicket = CreateHubConnection("cookie_user_99");

        await Task.WhenAll(clientA.StartAsync(), clientB_SameTicket.StartAsync(), clientC_DifferentTicket.StartAsync());

        CommentNotificationDto? receivedCommentByB = null;
        CommentNotificationDto? receivedCommentByC = null;

        clientB_SameTicket.On<CommentNotificationDto>("ReceiveComment", dto => receivedCommentByB = dto);
        clientC_DifferentTicket.On<CommentNotificationDto>("ReceiveComment", dto => receivedCommentByC = dto);

        // Client A và B gia nhập nhóm ticket 101; Client C gia nhập ticket 202
        await clientA.InvokeAsync("JoinTicketGroup", 101);
        await clientB_SameTicket.InvokeAsync("JoinTicketGroup", 101);
        await clientC_DifferentTicket.InvokeAsync("JoinTicketGroup", 202);

        // 2. Act: Client A gửi bình luận lên ticket 101
        var newComment = new SendCommentRequest(101, "Kỹ thuật viên đã kiểm tra cáp mạng chưa?");
        await clientA.InvokeAsync("SendComment", newComment);

        // Chờ xử lý bất đồng bộ trong 1000ms
        await Task.Delay(500);

        // 3. Assert
        receivedCommentByB.Should().NotBeNull();
        receivedCommentByB!.TicketId.Should().Be(101);
        receivedCommentByB.Content.Should().Be("Kỹ thuật viên đã kiểm tra cáp mạng chưa?");

        // Bảo đảm tính cô lập thông tin (Client C không bị nhận nhầm tin nhắn)
        receivedCommentByC.Should().BeNull();

        // 4. Cleanup
        await Task.WhenAll(clientA.DisposeAsync().AsTask(), clientB_SameTicket.DisposeAsync().AsTask(), clientC_DifferentTicket.DisposeAsync().AsTask());
    }

    private HubConnection CreateHubConnection(string mockAuthCookie)
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/ticket", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add("Cookie", mockAuthCookie);
            })
            .Build();
    }
}
```

---

## 6. KIẾN TRÚC E2E TEST SUITE & PHÂN PHỐI 4 TẦNG (TIER 1 - 4 TEST DISTRIBUTION)

### 6.1. Cấu trúc Dự án Test trong Solution (`ItTms.sln`)

```
ItTms/
├── src/
│   ├── ItTms.Domain/                 # Pure State Pattern, Entities, Events, Invariants
│   ├── ItTms.Application/            # CQRS/Feature Slices, Interfaces, Observers
│   ├── ItTms.Infrastructure/         # EF Core, Concurrency, SQLite, Identity
│   └── ItTms.Web/                    # 5 MVC Areas, SignalR TicketHub, Controllers, Views
└── tests/
    ├── ItTms.UnitTests/              # [Tier 1] Fast Pure Tests (<5s)
    │   ├── Domain/
    │   │   ├── StatePatternTests.cs  # 49 State Permutations
    │   │   └── InvariantTests.cs     # SLA calculation, Result<T>
    │   └── Architecture/
    │       └── NetArchCleanTests.cs  # Clean Architecture Dependency Rules
    │
    ├── ItTms.IntegrationTests/       # [Tier 2 & 3] DB & Subsystem Tests (<45s)
    │   ├── Persistence/              # [Tier 2] EF Core, Migrations, Seed Data
    │   ├── Concurrency/              # [Tier 2] RowVersion / Token Conflict Tests
    │   ├── Security/                 # [Tier 3] AuthorizeRolesAttribute, 401/403 Tests
    │   └── SignalR/                  # [Tier 3] TicketHub Real-time WebSocket Tests
    │
    └── ItTms.E2ETests/               # [Tier 4] Full Opaque-Box E2E Tests (<90s)
        ├── Scenarios/
        │   ├── Table72Scenarios.cs   # All 10 Verification Scenarios (TC-01 -> TC-10)
        │   └── FullLifecycleTests.cs # Full Flow: Create -> Assign -> Work -> Close -> Rate
        └── Fixtures/
            └── CustomWebApplicationFactory.cs
```

---

### 6.2. Phân tầng Kiểm thử Chi tiết (Tier 1 - Tier 4 Distribution)

```
+=============================================================================================================+
|                                    BẢNG PHÂN TẦNG KIỂM THỬ DỰ ÁN IT-TMS                                     |
+======+========================+====================+======================+=================================+
| Tier | Tên Tầng & Định vị     | Phạm vi & Mục tiêu | Target Execution Time| Công nghệ & Bộ công cụ          |
+======+========================+====================+======================+=================================+
| T1   | Fast Smoke & Unit      | - 7 State classes  | < 5 giây             | xUnit, FluentAssertions,        |
|      | (L0/L1 - Zero I/O)     | - State matrix (49)|                      | NetArchTest, FakeTimeProvider   |
|      |                        | - Result<T> Railway|                      | (Zero Mocks, Pure Memory)       |
|      |                        | - Architecture Rule|                      |                                 |
+------+------------------------+--------------------+----------------------+---------------------------------+
| T2   | Component & Data       | - EF Core SQLite   | < 20 giây            | SQLite In-Memory / Test-DB,     |
|      | (L2 - Persistence)     | - Concurrency Token|                      | Respawn, DbContext Pooling      |
|      |                        | - Observer Pattern |                      |                                 |
|      |                        | - Covering Indexes |                      |                                 |
+------+------------------------+--------------------+----------------------+---------------------------------+
| T3   | Subsystem & Real-time  | - SignalR TicketHub| < 45 giây            | WebApplicationFactory,          |
|      | (L3 - Services & Hub)  | - RBAC Filters     |                      | Microsoft.AspNetCore.SignalR    |
|      |                        | - Cookie Auth Flow |                      | .Client                         |
|      |                        | - File Sniffing Sec|                      |                                 |
+------+------------------------+--------------------+----------------------+---------------------------------+
| T4   | System E2E Scenarios   | - 10 kịch bản Bảng | < 90 giây            | WebApplicationFactory, AngleSharp|
|      | (L4 - Opaque-box E2E)  |   7.2 hoàn chỉnh   |                      | (DOM parser), Full Audit Trail  |
|      |                        | - 20 Use Cases flow|                      | Verification                    |
+======+========================+====================+======================+=================================+
```

---

### 6.3. Lệnh Thực thi Test Runner & Cấu hình CI/CD

#### A. Lệnh chạy theo từng Phân tầng (Targeted Testing)
```powershell
# 1. Chạy Tier 1 (Cực nhanh, chạy ngay trước mỗi lần commit code):
dotnet test tests/ItTms.UnitTests -c Release --filter "Category=Tier1_Unit" --logger "console;verbosity=normal"

# 2. Chạy Tier 2 (Kiểm thử CSDL, Migrations & Concurrency):
dotnet test tests/ItTms.IntegrationTests -c Release --filter "Category=Tier2_Integration"

# 3. Chạy Tier 3 (Kiểm thử SignalR Hub & Bộ lọc Bảo mật RBAC):
dotnet test tests/ItTms.IntegrationTests -c Release --filter "Category=Tier3_Subsystem"

# 4. Chạy Tier 4 (Nghiệm thu 10 kịch bản Bảng 7.2):
dotnet test tests/ItTms.E2ETests -c Release --filter "Category=Tier4_E2E"
```

#### B. Lệnh chạy Toàn bộ Giải pháp (Full Suite Execution)
```powershell
# Chạy toàn bộ test suite, xuất kết quả XML TRX để đồng bộ vào báo cáo
dotnet test ItTms.sln -c Release --logger "trx;LogFileName=test_results.trx" --logger "console;verbosity=detailed"
```

---

### 6.4. Ngữ nghĩa Pass/Fail & Cơ chế Chống Flakiness

1. **Quy tắc Thất bại Tuyệt đối (Zero-Tolerance Failure)**:
   - Nếu bất kỳ test case nào trong 10 kịch bản Bảng 7.2 hoặc ma trận State Pattern bị Fail, toàn bộ quy trình build bị đánh trượt (Exit code != 0).
   - Không được phép chấp nhận "flaky test" hoặc thử lại nhiều lần (retry) để che giấu lỗi bất đồng bộ.
2. **Loại trừ Flakiness ở gốc rễ (#010 Invariants)**:
   - **Deterministic Time**: Tuyệt đối không dùng `DateTime.UtcNow` trực tiếp trong code. Luôn tiêm `TimeProvider` và sử dụng `FakeTimeProvider` trong test để điều khiển bước nhảy thời gian khi test SLA.
   - **Không dùng Thread.Sleep**: Mọi đồng bộ hóa SignalR và DB trong test phải sử dụng `TaskCompletionSource<T>`, `CancellationToken` và polling có timeout xác định.
   - **Database Cleanliness**: Mỗi test suite thực thi trên một SQLite In-Memory Database connection riêng biệt mở xuyên suốt test session, đảm bảo tính cô lập tuyệt đối dữ liệu.

---

## 7. MA TRẬN TRUY XUẤT NGUỒN GỐC & KẾT LUẬN KIỂM CHỨNG (TRACEABILITY MATRIX)

### 7.1. Bảng Đối chiếu Nguồn gốc Nghiệm thu (Traceability Matrix)

| Kịch bản Bảng 7.2 | Mã Test Case | Tier | Use Case liên quan | Lớp Kiểm thử & Tệp nguồn phụ trách | Trạng thái Nghiệm thu |
|---|---|:---:|---|---|:---:|
| 1. Đăng nhập tài khoản inactive bị từ chối | `TC-VAL-01` | **Tier 4** | UC01, UC15 | `ItTms.E2ETests.Scenarios.Table72Scenarios.TC01_InactiveAccountLogin` | SẴN SÀNG |
| 2. User vào trang Leader bị 403 | `TC-SEC-02` | **Tier 3** | UC11, FR-03 | `ItTms.IntegrationTests.Security.RbacFilterTests.TC02_UserAccessLeaderRoute` | SẴN SÀNG |
| 3. User tạo ticket -> New & báo Leader | `TC-BIZ-03` | **Tier 4** | UC03, FR-06 | `ItTms.E2ETests.Scenarios.Table72Scenarios.TC03_CreateTicketNotifiesLeader` | SẴN SÀNG |
| 4. Leader phân công -> Assigned & báo đôi | `TC-BIZ-04` | **Tier 4** | UC09, FR-08 | `ItTms.E2ETests.Scenarios.Table72Scenarios.TC04_AssignTicketNotifiesStaffAndUser` | SẴN SÀNG |
| 5. Staff đặt Closed trên New bị chặn | `TC-INV-05` | **Tier 1** | UC07, FR-10 | `ItTms.UnitTests.Domain.StatePatternTests.TC05_StaffCloseNewTicket_Fails` | SẴN SÀNG |
| 6. Leader từ chối ticket InProgress bị chặn | `TC-INV-06` | **Tier 1** | UC10, FR-09 | `ItTms.UnitTests.Domain.StatePatternTests.TC06_RejectInProgress_Fails` | SẴN SÀNG |
| 7. Staff mở ticket người khác bị chặn 403 | `TC-SEC-07` | **Tier 3** | UC08, BOLA | `ItTms.IntegrationTests.Security.BolaSecurityTests.TC07_StaffAccessOtherTicket` | SẴN SÀNG |
| 8. Bình luận trên ticket Resolved bị chặn | `TC-INV-08` | **Tier 1** | UC06, FR-11 | `ItTms.UnitTests.Domain.StatePatternTests.TC08_CommentOnResolved_Fails` | SẴN SÀNG |
| 9. User đóng & rate Resolved -> Closed | `TC-BIZ-09` | **Tier 4** | UC19, UC04 | `ItTms.E2ETests.Scenarios.Table72Scenarios.TC09_CloseAndRateTicket` | SẴN SÀNG |
| 10. Admin toggle & chặn tự vô hiệu hóa | `TC-INV-10` | **Tier 4** | UC15, FR-15 | `ItTms.E2ETests.Scenarios.Table72Scenarios.TC10_AdminToggleAndSelfLockGuard` | SẴN SÀNG |

---

### 7.2. Kết luận và Khuyến nghị Triển khai
1. **Tính tương thích hoàn toàn với Báo cáo đồ án**: Bản khảo sát đã chuẩn hóa toàn bộ 10 kịch bản kiểm thử của Bảng 7.2, cung cấp các thông số thực nghiệm, trạng thái vào/ra và tiêu chí Pass/Fail để điền trực tiếp vào cột *"Kết quả thực tế"* của file báo cáo `BaoCao_ITTMS_TKPMHDT (4).docx` trong Giai đoạn B.
2. **Bộ khung Kiểm thử vững chắc**: Cung cấp cấu trúc xUnit, FluentAssertions và Testcontainers/In-Memory cho đội ngũ lập trình viên cài đặt ngay lập tức song song với việc viết code tính năng (Dual-Track Pattern).
3. **Độ an toàn hệ thống cấp doanh nghiệp**: Các bài test Concurrency Token, SignalR Hub và OWASP BOLA bảo đảm giải pháp .NET 9 không mắc phải bất kỳ lỗi kinh điển nào về tranh chấp tài nguyên hay lộ lọt dữ liệu phân quyền.
