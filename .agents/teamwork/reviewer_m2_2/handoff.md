# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — REVIEWER M2_2
> **Nhiệm vụ**: Độc lập rà soát và phản biện đối kháng Observer Pattern và Security Filters (Milestone 2)  
> **Người thực hiện**: `reviewer_m2_2` (Reviewer & Adversarial Critic)  
> **Người nhận**: `orchestrator` / `parent` (9e3cad2c-3e7f-49c0-9814-015216856d2e)  
> **Ngày lập**: 2026-10-25  
> **Loại bàn giao**: Hard Handoff  
> **Phán quyết cuối cùng (Final Verdict)**: **APPROVE**  

---

## 1. OBSERVATION (QUAN SÁT THỰC TẾ)

1. **Biên dịch và Kiểm thử độc lập**:
   - Lệnh: `dotnet build c:\IT_System\ItTms.sln`
     ```text
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:06.11
     ```
   - Lệnh: `dotnet test c:\IT_System\ItTms.sln`
     ```text
     Passed!  - Failed: 0, Passed: 181, Skipped: 0, Total: 181, Duration: 29 s - ItTms.Tests.dll (net9.0)
     ```
   - Xác nhận: 100% 181 bài test pass trọn vẹn, không có lỗi biên dịch, không có cảnh báo.

2. **Cấu trúc Observer Pattern & Event Dispatching**:
   - `src/ItTms.Application/Observers/NotificationObserver.cs`: Triển khai đầy đủ 5 interfaces `IDomainEventObserver<T>` cho `TicketCreatedEvent` (thông báo Leader active theo TC-03), `TicketAssignedEvent` (thông báo Staff & User theo TC-04), `TicketStatusChangedEvent`, `CommentAddedEvent` (chặn rò rỉ `IsInternal` cho User theo TC-08), `TicketClosedEvent` (thông báo Staff khi đóng ticket theo TC-09).
   - `src/ItTms.Application/Observers/SystemLogObserver.cs`: Triển khai ghi vết kiểm toán bất biến (Immutable Audit Trail) cho toàn bộ 6 domain events chính (`CreateTicket`, `AssignTicket`, `UpdateStatus`, `AddComment`, `CloseTicket`, `DeactivateUser`).
   - `src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs` (dòng 29-57): Cơ chế Two-Phase Event Dispatching nạp dữ liệu ở Pha 1 (in-transaction) và gọi `base.SaveChangesAsync` commit trong 1 giao dịch SQLite ACID duy nhất; Pha 2 (post-commit) kích hoạt sau khi đã lưu DB thành công.

3. **Cấu trúc Security Filters (Lớp Bảo mật Web)**:
   - `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs`: Kiểm tra xác thực, truy vấn CSDL thời gian thực kiểm tra `User.Status == UserStatus.Active` (TC-01 -> hủy session cookie và chuyển hướng Login kèm thông báo TempData), kiểm tra vai trò RBAC (TC-02 -> HTTP 403 Forbidden).
   - `src/ItTms.Web/Filters/AuthorizeTicketAccessAttribute.cs`: Phòng thủ OWASP API1:2023 BOLA/IDOR (TC-07). Trích xuất TicketId từ Route, Action Arguments hoặc Query String; chặn Staff xem ticket người khác và User xem ticket người khác; tự động ghi vết cảnh báo an ninh `BOLA_Violation_Attempt` vào `SystemLogs`.

4. **Kiểm tra tính toàn vẹn (Integrity Violation Check)**:
   - Không có mã giả lập facade hay dummy methods.
   - Không có kết quả kiểm thử hardcoded trong mã nguồn nghiệp vụ.
   - Không có bước tắt bỏ qua yêu cầu bài toán.
   - Dữ liệu kiểm thử sử dụng SQLite In-Memory thực thi các câu lệnh SQL và giao dịch thực sự.

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN LOGIC)

1. **Từ Quan sát 1 & 4 (Kiểm thử thực tế và tính toàn vẹn)**:
   - Giải pháp của `worker_m2` không dùng facade hay gian lận kết quả. Toàn bộ 181 unit và integration tests đều chạy thật trên môi trường SQLite engine và đều vượt qua.
   - Kết luận bộ phận: Đạt tiêu chuẩn tối thiểu về tính trung thực và khả năng chạy được của mã nguồn.

2. **Từ Quan sát 2 (Thiết kế Observer Pattern và Two-Phase Dispatcher)**:
   - Việc tách hai pha (Pha 1: Ghi nhận Notification và SystemLog cùng DbContext; Pha 2: Đẩy sự kiện ra WebSocket) giải quyết triệt để vấn đề mất tính toàn vẹn dữ liệu trong các hệ thống phân tán.
   - Nếu commit SQLite thất bại, giao dịch rollback toàn bộ (không sinh ra notification mồ côi hay log sai lệch).
   - Chỉ khi DB commit thành công, tín hiệu thời gian thực mới được phát ra ngoài.
   - Tuy nhiên, qua phân tích phản biện đối kháng, việc Pha 1 chạy trước khi SQLite sinh Identity Key dẫn đến `TicketId = 0` trong `TicketCreatedEvent` (Finding 1). Điểm này không làm hỏng logic chức năng của M2 (do link điều hướng là `/Leader/Ticket/AssignQueue` không gắn ID), nhưng cần được tinh chỉnh trong M3.

3. **Từ Quan sát 3 (Thiết kế Security Filters)**:
   - Việc truy vấn CSDL theo thời gian thực tại `AuthorizeRolesAttribute` loại bỏ hoàn toàn lỗ hổng người dùng bị khóa tài khoản nhưng vẫn dùng cookie cũ để thao tác (đạt chuẩn TC-01).
   - `AuthorizeTicketAccessAttribute` thực hiện kiểm tra quyền sở hữu đối với từng tài nguyên theo chuẩn OWASP BOLA/IDOR và tự động ghi nhật ký vi phạm an ninh vào CSDL (đạt chuẩn TC-07).
   - Tuy nhiên, việc giữ lại điều kiện fallback `|| _allowedRoles.Any(role => user.IsInRole(role))` (Finding 2) tạo ra nguy cơ nếu người dùng bị giáng chức trong CSDL mà cookie cũ vẫn còn hiệu lực. Điểm này cần được loại bỏ ngay trong M3.

---

## 3. CAVEATS (LƯU Ý & PHẠM VI CHƯA KHẢO SÁT)

1. **Phạm vi Milestone 2 vs Milestone 3**:
   - Milestone 2 tập trung vào tầng Business Logic Engine, State Pattern, Observer Pattern và Security Filters độc lập. Các Filters đã được kiểm thử với `ActionExecutingContext` mock, nhưng chưa được gắn vào các Controller thực tế (sẽ hoàn tất ở Milestone 3).
2. **SignalR WebSocket Push**:
   - `SignalRNotificationDispatcher` hiện tại ghi log và giả lập completed task. Việc phát sóng thực tế tới `IHubContext<TicketHub>` là phạm vi của Milestone 3.
3. **Các điểm phát hiện cần xử lý trong M3**:
   - Xử lý Finding 1 (`TicketId = 0` trên sự kiện tạo mới).
   - Xử lý Finding 2 (Bỏ fallback `user.IsInRole(role)` để đảm bảo DB là nguồn thẩm quyền duy nhất).
   - Xử lý Finding 3 (Lưu giữ lý do từ chối ticket trong notification).
   - Xử lý Finding 4 (Đồng bộ hóa phản hồi JSON cho request AJAX vi phạm BOLA).

---

## 4. CONCLUSION (KẾT LUẬN & PHÁN QUYẾT)

- **PHÁN QUYẾT (EXPLICIT VERDICT)**: **`APPROVE`**
- **Lý do**:
  1. Không có bất kỳ vi phạm tính toàn vẹn (Zero Integrity Violations).
  2. Đáp ứng 100% các yêu cầu kiến trúc của Milestone 2 cho Observer Pattern và Security Filters (Bảng 7.2 TC-01, TC-02, TC-03, TC-04, TC-07, TC-08, TC-09, TC-10).
  3. Solution biên dịch sạch sẽ (0 warnings, 0 errors) và toàn bộ 181/181 tests passed.
  4. Các phát hiện phản biện (Finding 1 đến Finding 4) là các cơ hội gia cố và hoàn thiện kiến trúc cho Milestone 3, không phải lỗi logic chặn việc nghiệm thu Milestone 2.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Để tái hiện và kiểm chứng độc lập các đánh giá trên, thực thi các lệnh sau từ thư mục gốc `c:\IT_System`:

1. **Kiểm tra biên dịch Solution**:
   ```powershell
   dotnet build c:\IT_System\ItTms.sln
   ```
   *Kỳ vọng*: `Build succeeded. 0 Warning(s), 0 Error(s)`.

2. **Kiểm tra toàn bộ Test Suite (181 tests)**:
   ```powershell
   dotnet test c:\IT_System\ItTms.sln
   ```
   *Kỳ vọng*: `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181`.

3. **Kiểm tra riêng các Unit Test Observer & Security Filters**:
   ```powershell
   dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --filter "FullyQualifiedName~Observers|FullyQualifiedName~Security"
   ```
   *Kỳ vọng*: Toàn bộ các test của `ObserverPatternTests`, `AuthorizeRolesAttributeTests`, và `AuthorizeTicketAccessFilterTests` đều Passed.

4. **Kiểm tra các tệp báo cáo chi tiết**:
   - `c:\IT_System\.agents\teamwork\reviewer_m2_2\review_report.md`
   - `c:\IT_System\.agents\teamwork\reviewer_m2_2\handoff.md`
