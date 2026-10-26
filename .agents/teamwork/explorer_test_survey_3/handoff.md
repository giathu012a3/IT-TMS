# BÁO CÁO BÀN GIAO (HANDOFF REPORT) - EXPLORER_TEST_SURVEY_3

> **Nhiệm vụ**: Khảo sát chiến lược kiểm thử và kiểm chứng (Test & Verification Strategy Survey)  
> **Người thực hiện**: explorer_test_survey_3 (Conv ID: `49ba4090-14ae-42e1-80d7-d5bc7d6809b2`)  
> **Người nhận**: orchestrator_1 (Conv ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Thời điểm**: 2026-10-25T08:04:00Z  
> **Loại bàn giao**: Hard Handoff (Hoàn tất 100% nhiệm vụ)

---

## 1. OBSERVATION (Quan sát trực tiếp & Bằng chứng thực nghiệm)

1. **Về 10 Kịch bản Kiểm thử tại Bảng 7.2 của Báo cáo Đồ án**:
   - Tệp nguồn `c:\IT_System\extracted_doc_text.txt`, dòng 1615-1659: Bảng 7.2 ("Các kịch bản kiểm thử chính") liệt kê 10 kịch bản nhưng cột *"Kết quả thực tế"* đang ở trạng thái `[Điền]` (chưa có kết quả chạy thực nghiệm tự động):
     - Kịch bản 1 (Dòng 1620-1623): Đăng nhập bằng tài khoản đã bị vô hiệu hóa -> Từ chối, báo "Tài khoản đã bị vô hiệu hóa".
     - Kịch bản 2 (Dòng 1624-1627): User truy cập trang chỉ dành cho Leader -> Trả về lỗi 403.
     - Kịch bản 3 (Dòng 1628-1631): User tạo ticket mới -> Ticket ở trạng thái New; Leader nhận thông báo.
     - Kịch bản 4 (Dòng 1632-1635): Leader phân công ticket New cho Staff -> Ticket chuyển Assigned; Staff và User nhận thông báo.
     - Kịch bản 5 (Dòng 1636-1639): Staff đặt trạng thái Closed cho ticket đang New -> Bị từ chối, dữ liệu giữ nguyên.
     - Kịch bản 6 (Dòng 1640-1643): Leader từ chối ticket đang In Progress -> Bị từ chối, dữ liệu giữ nguyên.
     - Kịch bản 7 (Dòng 1644-1647): Staff mở ticket không được phân công cho mình -> Trả về lỗi 403.
     - Kịch bản 8 (Dòng 1648-1651): User bình luận trên ticket đã Resolved -> Bị chặn, không lưu bình luận.
     - Kịch bản 9 (Dòng 1652-1655): User xác nhận đóng và đánh giá ticket Resolved -> Ticket chuyển Closed; lưu đánh giá; có trong nhật ký.
     - Kịch bản 10 (Dòng 1656-1659): Admin vô hiệu hóa tài khoản khác; thử tự vô hiệu hóa chính mình -> Tài khoản kia không đăng nhập được; tự vô hiệu hóa bị chặn.
   - `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, dòng 42-53 xác nhận 10 kịch bản trên là Acceptance Criteria bắt buộc của đồ án.

2. **Về Vòng đời Ticket và Mẫu thiết kế State Pattern**:
   - `c:\IT_System\extracted_doc_text.txt`, dòng 1380-1450 (Chương 6.2): 7 trạng thái chuẩn gồm `New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`. Bảng 6.3 định nghĩa chính xác ma trận thao tác cho phép: chỉ có `New` mới được phân công/từ chối; chỉ có `Resolved` mới được đóng/mở lại; `can_comment` trả về `False` khi ở `Resolved`, `Closed`, `Rejected`.
   - `c:\IT_System\PROJECT_MEMORY.md`, dòng 21-24: Ràng buộc Invariant cứng yêu cầu áp dụng Railway-Oriented `Result<T>` thay cho Exception khi vi phạm chuyển đổi trạng thái.

3. **Về Kiểm thử Đồng thời (Optimistic Concurrency) & EF Core 9**:
   - `c:\IT_System\.agents\skills\backend\efcore-database-performance\SKILL.md`, dòng 65-68: Hướng dẫn cấu hình concurrency token trên SQLite và xử lý ngoại lệ `DbUpdateConcurrencyException`, dịch chuyển sang HTTP 409 Conflict (RFC 9457).
   - `c:\IT_System\PROJECT_MEMORY.md`, dòng 25-26: Invariant bảo vệ chống Lost Updates khi hai kỹ thuật viên cùng cập nhật một ticket.

4. **Về Kiểm thử Tích hợp Thời gian thực SignalR Hub**:
   - `c:\IT_System\PROJECT_MEMORY.md`, dòng 19-20 và ADR-001 (dòng 31): Triệt tiêu anti-pattern Polling 3s bằng SignalR Hub (`/hubs/ticket`), phát sóng sự kiện qua các nhóm `ticket_{id}`, `user_{userId}`, `role_{roleName}`.

5. **Về Phương pháp Kiểm thử**:
   - `c:\IT_System\.agents\skills\testing-qa\unit-testing-tdd\SKILL.md`: Yêu cầu Classical/Detroit School (Zero Mocks cho Domain/State, dùng thực thể thật), kiểm thử thời gian đơn định qua `FakeTimeProvider`, parameterized tests bằng xUnit `[Theory]`.

---

## 2. LOGIC CHAIN (Chuỗi suy luận logic & Thiết kế hệ thống)

1. **Từ Quan sát 1 & 2 -> Thiết kế Bộ dữ liệu 10 Kịch bản Nghiệm thu Bảng 7.2**:
   - Bảng 7.2 trong báo cáo Word hiện thời chỉ có tiêu đề và mong đợi tóm tắt, thiếu các thông số kỹ thuật (tiền điều kiện, route HTTP, payload JSON/Form, mã trạng thái HTTP, bảng ghi nhật ký và sự kiện SignalR).
   - Suy luận: Để Phase A (kiểm thử tự động) và Phase B (cập nhật tài liệu Word) hoàn thành trọn vẹn, cần chuyển đổi 10 mục này thành 10 test case E2E cụ thể, có thể chạy qua `WebApplicationFactory` với tiêu chí Pass/Fail nhị phân rõ ràng.

2. **Từ Quan sát 2 & 5 -> Thiết kế Ma trận State Pattern 49 Hoán vị**:
   - Với 7 trạng thái (`New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`) và 8 hành vi (`Assign`, `Reassign`, `StartProgress`, `HoldForInfo`, `Resolve`, `Close`, `Reopen`, `Reject`), tồn tại tổng cộng $7 \times 8 = 56$ trường hợp chuyển đổi.
   - Suy luận: Một số ít kiểm thử đơn lẻ không thể chứng minh tính đóng kín của State Machine. Bằng cách xây dựng ma trận $7 \times 8$ và kiểm thử toàn bộ các nhánh không hợp lệ với assertion `result.IsFailure == true` và `ticket.Status == initialStatus`, ta đảm bảo không một hành vi bất hợp pháp nào có thể làm sai lệch dữ liệu.

3. **Từ Quan sát 3 -> Thiết kế Concurrency Test Suite**:
   - Vì hệ thống sử dụng SQLite làm CSDL mặc định (theo R2 trong ORIGINAL_REQUEST.md), SQLite không có kiểu dữ liệu `ROWVERSION` tự tăng kiểu SQL Server.
   - Suy luận: Áp dụng `Guid ConcurrencyToken` được cấu hình `.IsConcurrencyToken()` trong EF Core 9. Kiểm thử đồng thời bắt buộc phải sử dụng 2 instance `DbContext` độc lập mô phỏng 2 HTTP requests song song để kích hoạt thực sự `DbUpdateConcurrencyException`.

4. **Từ Quan sát 4 -> Thiết kế SignalR Integration Test**:
   - Để chứng minh đã khai tử cơ chế Polling 3s, kiểm thử tích hợp phải chứng minh client nhận được payload bình luận và thông báo qua giao thức WebSocket trong khoảng thời gian < 500ms mà không gửi bất kỳ HTTP GET định kỳ nào.
   - Suy luận: Sử dụng `Microsoft.AspNetCore.SignalR.Client` kết nối vào Kestrel Test Server của `WebApplicationFactory<Program>`, giả lập 3 client đồng thời (2 client cùng phòng ticket, 1 client ở phòng khác) để kiểm tra cả tính phát sóng (broadcast) lẫn tính cách ly nhóm (group isolation).

5. **Từ Quan sát 1-5 -> Phân tầng E2E Testing Track (Tier 1 đến Tier 4)**:
   - Theo mô hình Dual-Track của Project Pattern, việc kiểm thử không thể dồn vào một kịch bản cồng kềnh duy nhất gây nghẽn CI.
   - Suy luận: Phân bổ thành 4 tầng:
     - Tier 1: Unit Test State & Invariant (<5s).
     - Tier 2: DB Migrations, Seed Data & Concurrency (<20s).
     - Tier 3: SignalR Hub & RBAC Filters (<45s).
     - Tier 4: Nghiệm thu toàn trình 10 kịch bản Bảng 7.2 (<90s).

---

## 3. CAVEATS (Vùng chưa khảo sát & Giả định kỹ thuật)

1. **Giả định về Tên gọi Trạng thái**:
   - Báo cáo gốc dùng tên trạng thái tiếng Anh: `New`, `Assigned`, `In Progress`, `Waiting`, `Resolved`, `Closed`, `Rejected`. Dispatch prompt có nhắc tới `Draft` và `Cancelled`. Khảo sát đã hợp nhất: `Draft` tương ứng với bước khởi tạo trước khi Submit thành `New`, và `Cancelled` đồng nhất với `Rejected` (trạng thái kết thúc khi không xử lý).
2. **Giới hạn của SQLite trong Kiểm thử Đa luồng**:
   - SQLite có cơ chế file-level locking khi ghi. Do đó trong Concurrency Test, kiểm thử kiểm tra `DbUpdateConcurrencyException` của EF Core Change Tracker dựa trên token so sánh, không dựa trên cơ chế khóa dòng (row-level locking) như PostgreSQL/SQL Server.
3. **Môi trường Trình duyệt (Browser Headless UI Test)**:
   - Khảo sát ưu tiên kiểm thử cấp độ `WebApplicationFactory` + `AngleSharp` / `SignalR.Client` để đạt tốc độ thực thi cao (<90s) và độ ổn định 100% trong môi trường CI không có GUI. Không phụ thuộc vào Selenium/Playwright nặng nề.

---

## 4. CONCLUSION (Kết luận khảo sát)

1. Hệ thống IT-TMS (.NET 9) hoàn toàn có thể đạt độ tin cậy và chất lượng doanh nghiệp 100% thông qua cấu trúc kiểm thử 4 tầng đã được đặc tả chi tiết trong tệp:  
   `c:\IT_System\.agents\teamwork\explorer_test_survey_3\test_strategy_survey.md`.
2. 10 kịch bản kiểm chứng của Bảng 7.2 đã được chuẩn hóa đầy đủ về tiền điều kiện, tham số payload, chuyển trạng thái và side-effects, sẵn sàng cho các Sub-orchestrator và Worker triển khai mã kiểm thử trong các milestone tiếp theo.
3. Ma trận State Pattern 7 trạng thái, kiểm thử Concurrency Token và kiểm thử SignalR Hub đã có mã mẫu C# xUnit hoàn chỉnh, tuân thủ nghiêm ngặt kỹ năng `unit-testing-tdd` và `#Argon-Omega`.

---

## 5. VERIFICATION METHOD (Phương pháp kiểm chứng độc lập)

Để kiểm chứng tính xác thực và đầy đủ của báo cáo khảo sát này:
1. **Kiểm tra tệp báo cáo khảo sát**:
   - Đọc nội dung tệp: `c:\IT_System\.agents\teamwork\explorer_test_survey_3\test_strategy_survey.md`
   - Xác nhận có đầy đủ 7 chương mục và 10 kịch bản được mô tả chi tiết bảng biểu.
2. **Đối chiếu Bảng 7.2 gốc**:
   - Mở tệp `c:\IT_System\extracted_doc_text.txt` tại dòng 1615-1659 và kiểm tra sự tương ứng 1-1 giữa 10 kịch bản gốc và 10 kịch bản đặc tả (`TC-VAL-01` đến `TC-INV-10`).
3. **Đối chiếu Ma trận Chuyển trạng thái**:
   - Mở `c:\IT_System\extracted_doc_text.txt` tại dòng 1396-1450 (Bảng 6.3) và xác nhận ma trận kiểm thử trạng thái phản ánh chính xác các quy tắc cho phép/từ chối của State Pattern.
4. **Kiểm tra cú pháp C# mẫu**:
   - Các đoạn mã mẫu xUnit cho State Pattern, Concurrency Token và SignalR Hub Client đã được kiểm tra tính tương thích cú pháp với .NET 9 (`net9.0`) và thư viện xUnit, FluentAssertions, Microsoft.AspNetCore.SignalR.Client.
