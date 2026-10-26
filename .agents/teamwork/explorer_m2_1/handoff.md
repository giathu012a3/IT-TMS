# BÁO CÁO BÀN GIAO THIẾT KẾ STATE PATTERN (HANDOFF REPORT)

> **Báo cáo 5 thành phần chuẩn mực (5-Component Handoff Protocol)**  
> **Người thực hiện**: `explorer_m2_1`  
> **Người nhận**: `parent` (ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Nhiệm vụ**: M2 - Explorer 1: State Pattern & 7 Concrete States Blueprint  
> **Ngày hoàn thành**: 2026-10-25  

---

## 1. OBSERVATION (Quan sát Trực tiếp)

1. **Khảo sát Yêu cầu Giao việc (`DISPATCH.md`)**:
   - `c:\IT_System\.agents\teamwork\explorer_m2_1\DISPATCH.md`:
     - Dòng 4-8: Thiết kế `ITicketState` interface và `TicketStateBase` abstract class với các phương thức: `Assign(User staff)`, `Reassign(User staff)`, `StartProgress()`, `HoldForInfo(string reason)`, `Resolve()`, `Close(Feedback feedback)`, `Reopen(string reason)`, `Reject(string reason)`, thuộc tính `bool CanComment { get; }`. Trả về Railway-oriented `Result` với `Error`, zero exceptions.
     - Dòng 9-16: 7 Concrete States (`NewState`, `AssignedState`, `InProgressState`, `WaitingState`, `ResolvedState`, `ClosedState`, `RejectedState`). `CanComment = false` cho Resolved (TC-08), Closed, Rejected. Chặn Reject khi InProgress (TC-06), chặn Close khi New (TC-05).
     - Dòng 17-20: Tích hợp `Ticket.CurrentState` phục hồi từ `Ticket.StatusId`, ủy quyền và phát sinh domain events.
2. **Khảo sát Hiện trạng Codebase Thực thể Ticket (`Ticket.cs`)**:
   - `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs`:
     - Dòng 43-49: `CanComment()` được viết dạng phương thức kiểm tra `StatusId != TicketStatus.Resolved && StatusId != TicketStatus.Closed && StatusId != TicketStatus.Rejected`.
     - Dòng 114-261: Các phương thức nghiệp vụ (`Assign`, `StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reopen`, `Reject`) đang sử dụng các khối `if (StatusId != ...)` rẽ nhánh thủ công và ném lỗi `INVALID_STATE_TRANSITION`, `CANNOT_REJECT_IN_PROGRESS`.
3. **Khảo sát Sự kiện Miền (`Domain Events`)**:
   - `c:\IT_System\src\ItTms.Domain\Events\TicketAssignedEvent.cs`: Ghi nhận `(TicketId, AssignedToId, LeaderId, OccurredOn)`.
   - `c:\IT_System\src\ItTms.Domain\Events\TicketStatusChangedEvent.cs`: Ghi nhận `(TicketId, OldStatusId, NewStatusId, ChangedByUserId, OccurredOn)`.
   - `c:\IT_System\src\ItTms.Domain\Events\TicketClosedEvent.cs`: Ghi nhận `(TicketId, ClosedByUserId, Rating, Comment, OccurredOn)`.
4. **Khảo sát Kết quả Kiểm thử Hiện tại**:
   - Lệnh thực thi: `dotnet test` tại thư mục gốc `c:\IT_System`.
   - Kết quả: `Passed! - Failed: 0, Passed: 109, Skipped: 0, Total: 109, Duration: 29 s - ItTms.Tests.dll (net9.0)`. Toàn bộ 109 bài test hiện tại đều đạt 100%.

---

## 2. LOGIC CHAIN (Chuỗi Suy luận & Lập luận Kiến trúc)

1. **Từ Quan sát 1 & 2 $\to$ Đóng gói Trạng thái theo GoF State Pattern**:
   - Hiện tại `Ticket.cs` ôm đồm toàn bộ kiểm tra `StatusId` bằng câu lệnh rẽ nhánh, vi phạm Open/Closed và Single Responsibility.
   - Để thỏa mãn chữ ký phương thức không cần truyền `Ticket` từ ngoài vào (`Assign(User staff)`, `Close(Feedback feedback)`), lớp trừu tượng `TicketStateBase` cần giữ tham chiếu `protected readonly Ticket Ticket` (mô hình Context-in-State).
2. **Từ Quan sát 1 & 2 $\to$ Triết lý Fail-Closed Default**:
   - Trong `TicketStateBase`, tất cả các phương thức mặc định trả về `Result.Failure(TicketErrors.InvalidTransition(Name, action))`.
   - Mỗi Concrete State chỉ cần `override` các hành vi được phép trong pha vòng đời của mình. Bất kỳ hành vi nào không được định nghĩa đều mặc định bị chặn đứng an toàn mà không cần viết code từ chối lặp lại.
3. **Từ Quan sát 1 & 3 $\to$ Quy chuẩn Railway-Oriented Programming & Zero Exceptions**:
   - Toàn bộ lỗi nghiệp vụ được chuẩn hóa thành `Error` có `Code` và `Description` tường minh: `INVALID_STATE_TRANSITION`, `CANNOT_REJECT_IN_PROGRESS`, `COMMENTS_BLOCKED_ON_RESOLVED`, `TICKET_ALREADY_CLOSED`, `TICKET_ALREADY_REJECTED`, `REJECTION_REASON_REQUIRED`, `REOPEN_REASON_REQUIRED`, `UNAUTHORIZED_STAFF`, `UNAUTHORIZED_USER`, `FEEDBACK_REQUIRED`.
   - Không có bất kỳ khối lệnh `throw new Exception` nào trong luồng nghiệp vụ.
4. **Từ Quan sát 2 & 4 $\to$ Cơ chế Tương thích Ngược & Khôi phục Entity từ EF Core 9**:
   - Khi EF Core tải `Ticket` từ SQLite, nó gán thuộc tính `StatusId` (int). Để không làm ảnh hưởng tới mapping của EF Core, `CurrentState` được thiết kế dưới dạng computed property: `_currentState ??= TicketStateFactory.CreateState(this, StatusId)`.
   - Trên `Ticket.cs`, ngoài các phương thức mới nhận `User` và `Feedback`, thiết kế bổ sung các overload nhận `int staffId`, `int leaderId`, `int userId` nhằm giữ 100% tương thích ngược với 109 bài test hiện có trong `ItTms.Tests`.

---

## 3. CAVEATS (Vấn đề Biên & Giới hạn Khảo sát)

1. **Phạm vi Explorer**:
   - Nhiệm vụ của `explorer_m2_1` là thiết kế kiến trúc chuẩn mực (blueprint), không trực tiếp sửa đổi mã nguồn trong `src/` (tuân thủ nguyên tắc Read-only investigation). Việc áp dụng mã nguồn sẽ do `implementer_m2_1` thực hiện dựa trên tài liệu `state_pattern_plan.md`.
2. **Xử lý Đăng ký Observer trong M2**:
   - Các Domain Events (`TicketAssignedEvent`, `TicketStatusChangedEvent`, `TicketClosedEvent`) được đăng ký thông qua `Ticket.AddDomainEvent(...)`. Việc Observer (`NotificationObserver`, `SystemLogObserver`) lắng nghe và xử lý các sự kiện này sẽ được triển khai ở nhiệm vụ Observer Pattern tiếp theo trong M2.
3. **Giả định về SQLite Concurrency**:
   - Mỗi lần chuyển trạng thái qua `TransitionTo`, `ConcurrencyToken` được tạo mới (`Guid.NewGuid()`), bảo đảm đồng bộ với cơ chế Optimistic Concurrency đã xây dựng tại Milestone 1.

---

## 4. CONCLUSION (Kết luận & Bàn giao)

1. Đã hoàn thành bản thiết kế chi tiết toàn diện State Pattern 7 Concrete States tại tệp:  
   `c:\IT_System\.agents\teamwork\explorer_m2_1\state_pattern_plan.md`.
2. Bản thiết kế bao gồm:
   - Giao diện `ITicketState` và lớp trừu tượng Fail-Closed `TicketStateBase`.
   - 7 Concrete States: `NewState`, `AssignedState`, `InProgressState`, `WaitingState`, `ResolvedState`, `ClosedState`, `RejectedState`.
   - Danh mục mã lỗi `TicketErrors` trả về Railway-oriented `Result`.
   - Thuộc tính `CanComment` chặn tại `Resolved` (TC-08), `Closed`, `Rejected`.
   - Bộ khôi phục trạng thái `TicketStateFactory`.
   - Cơ chế tích hợp và ủy quyền trong `Ticket.cs` bảo đảm tương thích ngược tuyệt đối.
   - Kế hoạch kiểm thử xUnit cho State Pattern đối chiếu Bảng 7.2 (TC-05, TC-06, TC-08, TC-09).
3. Sẵn sàng bàn giao cho `implementer_m2_1` để tiến hành lập trình và bổ sung unit test.

---

## 5. VERIFICATION METHOD (Phương thức Xác minh Độc lập)

Người nhận (Caller / Orchestrator) và Implementer có thể xác minh tính toàn vẹn của thiết kế theo các bước sau:

1. **Kiểm tra sự tồn tại và nội dung tài liệu thiết kế**:
   - Xem tệp: `c:\IT_System\.agents\teamwork\explorer_m2_1\state_pattern_plan.md`.
   - Xác nhận có đủ 10 chương mục với mã nguồn C# 13 hoàn chỉnh, ma trận chuyển trạng thái 7x8 và danh mục mã lỗi `TicketErrors`.
2. **Kiểm tra trạng thái Codebase hiện tại**:
   - Chạy lệnh:
     ```powershell
     dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj
     ```
   - Xác nhận 109/109 tests hiện tại passed 100%.
3. **Tiêu chí Nghiệm thu khi Implementer hoàn tất**:
   - Toàn bộ 7 tệp State được tạo trong `src/ItTms.Domain/States/`.
   - `Ticket.cs` ủy quyền toàn bộ cho `CurrentState`.
   - Tạo mới `tests/ItTms.Tests/UnitTests/States/TicketStatePatternTests.cs` kiểm chứng 100% các ô trong ma trận chuyển trạng thái.
   - Chạy `dotnet test` đạt tối thiểu 130+ tests passed và không có test nào bị fail.
