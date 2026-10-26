# BÁO CÁO ĐÁNH GIÁ ĐỘC LẬP & PHẢN BIỆN ADVERSARIAL (REVIEW REPORT)
> **Mã tác vụ**: Reviewer M2_1 — State Pattern & Domain Architecture Review  
> **Người thực hiện**: `reviewer_m2_1` (Reviewer & Adversarial Critic)  
> **Đối tượng đánh giá**: Gói công việc Milestone 2 do `worker_m2` bàn giao  
> **Ngày thực hiện**: 2026-10-25  
> **Tiêu chuẩn áp dụng**: `#Argon-Omega` Cognitive Kernel, BLUF-MECE, Clean Architecture .NET 9, GoF State Pattern, Table 7.2 Acceptance Scenarios  

---

## 1. REVIEW SUMMARY (TỔNG KẾT ĐÁNH GIÁ)

**Phán quyết (Verdict)**: **`APPROVE`**

Toàn bộ gói triển khai GoF State Pattern của `worker_m2` đạt chất lượng kỹ thuật xuất sắc (production-grade), tuân thủ 100% các nguyên lý thiết kế hướng đối tượng, Clean Architecture, và đáp ứng chuẩn xác toàn bộ các kịch bản nghiệm thu Bảng 7.2.

### Điểm nhấn chất lượng:
1. **7 Trạng thái cụ thể (Concrete States)**: Tách bạch rõ ràng trong `src/ItTms.Domain/States/`, kế thừa lớp trừu tượng cơ sở `TicketStateBase` với cơ chế Fail-Closed mặc định.
2. **Zero Exceptions cho Business Logic**: Áp dụng triệt để mô hình Railway-Oriented Programming với monad `Result` / `Result<T>` và struct lỗi `Error` có cấu trúc; 0 exception bị ném ra cho các luồng nghiệp vụ.
3. **Ràng buộc bất biến `CanComment`**: Trả về `false` bất biến tại `ResolvedState`, `ClosedState`, và `RejectedState`; trả về `true` tại `NewState`, `AssignedState`, `InProgressState`, `WaitingState` (đáp ứng trọn vẹn Bảng 7.2 TC-08).
4. **Các kịch bản nghiệm thu Bảng 7.2 (TC-05, TC-06, TC-08, TC-09)**: Triển khai chặt chẽ, kiểm thử tự động toàn diện qua ma trận 49 hoán vị ($7 \times 7$) và kiểm thử biên.
5. **Kiểm tra tính liêm chính (Integrity Check)**: Không phát hiện bất kỳ hành vi gian lận mã nguồn, hardcode kết quả, facade giả lập, hay tự chứng nhận kết quả kiểm thử. Toàn bộ 181/181 tests được biên dịch và thực thi độc lập thành công.

---

## 2. KẾT QUẢ XÁC MINH ĐỘC LẬP CÁC TUYÊN BỐ CỐT LÕI (VERIFIED CLAIMS)

### 2.1. Xác minh Biên dịch & Kiểm thử Hệ thống
| Lệnh kiểm thử độc lập | Kết quả đo đạc thực tế | Trạng thái |
|---|---|---|
| `dotnet build c:\IT_System\ItTms.sln` | `Build succeeded. 0 Warning(s), 0 Error(s). Time Elapsed 00:00:11.10` | ✅ PASSED |
| `dotnet test c:\IT_System\ItTms.sln` | `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181, Duration: 31 s` | ✅ PASSED |

### 2.2. Kiểm tra Cấu trúc & Hợp đồng State Pattern
- **Thư mục `src/ItTms.Domain/States/`**: Hiện diện đầy đủ 11 tệp tin mã nguồn:
  - `ITicketState.cs`: Hợp đồng chuẩn cho 8 hành vi chuyển đổi trạng thái và 4 thuộc tính.
  - `TicketStateBase.cs`: Triển khai mặc định Fail-Closed trả về `Result.Failure(TicketErrors.InvalidTransition)`.
  - `TicketErrors.cs`: Danh mục mã lỗi nghiệp vụ chuẩn (`INVALID_STATE_TRANSITION`, `CANNOT_REJECT_IN_PROGRESS`, `COMMENTS_BLOCKED_ON_RESOLVED`, v.v.).
  - `TicketStateFactory.cs`: Factory chuyển đổi `statusId` (1..7) thành Concrete State tương ứng khi nạp từ EF Core.
  - 7 Concrete States: `NewState.cs`, `AssignedState.cs`, `InProgressState.cs`, `WaitingState.cs`, `ResolvedState.cs`, `ClosedState.cs`, `RejectedState.cs`.

### 2.3. Kiểm tra Nguyên lý Zero Exceptions & Railway-Oriented Programming
- Qua lệnh tìm kiếm tĩnh (`grep_search`) trên toàn bộ thư mục `src/ItTms.Domain/States/`, không có bất kỳ lệnh `throw` ngoại lệ nào trong logic nghiệp vụ. Chỉ có duy nhất lệnh bảo vệ constructor `ArgumentNullException` khi `ticket == null` trong `TicketStateBase` và `TicketStateFactory`.
- Tất cả các phương thức rẽ nhánh nghiệp vụ đều trả về `Result` hoặc `Result<T>` với mã lỗi tường minh.

### 2.4. Kiểm tra Ràng buộc `CanComment` (Bảng 7.2 TC-08)
Xác minh trực tiếp giá trị thuộc tính `CanComment` trên từng State:
- `NewState.CanComment` => `true`
- `AssignedState.CanComment` => `true`
- `InProgressState.CanComment` => `true`
- `WaitingState.CanComment` => `true`
- `ResolvedState.CanComment` => `false` (TC-08)
- `ClosedState.CanComment` => `false` (Terminal State)
- `RejectedState.CanComment` => `false` (Terminal State)
Tại `Ticket.cs`, cả phương thức `CanComment()` và thuộc tính `CanCommentProperty` đều ủy quyền trực tiếp cho `CurrentState.CanComment`. Đã được kiểm chứng qua 7 test case `StateMachine_CanComment_MatchesStateRequirements` và `CanComment_ReturnsExpectedResultBasedOnStatus`.

### 2.5. Kiểm tra Các Kịch bản Nghiệm thu Bảng 7.2
1. **TC-05 (Chặn đóng ticket đang ở New)**:
   - `NewState` không ghi đè phương thức `Close`, kế thừa hành vi Fail-Closed từ `TicketStateBase`.
   - Lệnh gọi `ticket.Close(...)` trả về lỗi `INVALID_STATE_TRANSITION` (`ErrorType.Conflict`), trạng thái ticket giữ nguyên `TicketStatus.New`.
   - Kiểm chứng độc lập: Test `StateMachine_TC05_NewTicketCannotBeClosedDirectly` passed.
2. **TC-06 (Chặn Leader Reject ticket đang InProgress)**:
   - `InProgressState` ghi đè trực tiếp phương thức `Reject(string reason, ...)` và trả về `Result.Failure(TicketErrors.CannotRejectInProgress)` với mã `CANNOT_REJECT_IN_PROGRESS`.
   - Trạng thái ticket giữ nguyên `TicketStatus.InProgress`.
   - Kiểm chứng độc lập: Tests `StateMachine_TC06_InProgressTicketCannotBeRejected` và `Reject_WhenTicketIsInProgress_ReturnsFailure_Table72TC06` passed.
3. **TC-08 (Chặn bình luận trên ticket Resolved)**:
   - `ResolvedState.CanComment` trả về `false`.
   - Kiểm chứng độc lập: Tests `StateMachine_CanComment_MatchesStateRequirements` passed.
4. **TC-09 (Đóng và lưu Feedback khi ticket ở Resolved)**:
   - `ResolvedState.Close(Feedback feedback, ...)` kiểm tra quyền sở hữu `Ticket.CreatorId == userId`, kiểm tra `feedback != null`, gán `Ticket.Feedback = feedback`, thiết lập `Ticket.ClosedAt = DateTime.UtcNow`, chuyển sang `ClosedState`, và kích hoạt `TicketClosedEvent`.
   - Trạng thái ticket chuyển dịch thành `TicketStatus.Closed`.
   - Kiểm chứng độc lập: Test `StateMachine_TC09_CloseAndRateResolvedTicket_TransitionsToClosedWithFeedback` passed.

---

## 3. ADVERSARIAL STRESS-TEST & PHẢN BIỆN THỬ THÁCH (CRITIC ATTACK)

### 3.1. Thử thách Ma trận 49 Hoán vị Trạng thái ($7 \times 7$)
- **Kịch bản**: Duyệt qua toàn bộ tích Descartes giữa 7 trạng thái vòng đời và 7 hành động nghiệp vụ (`Assign`, `StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reopen`, `Reject`).
- **Kết quả Stress-test**:
  - 100% các chuyển đổi hợp lệ (ví dụ: New -> Assigned, Assigned -> InProgress, InProgress -> Resolved, Resolved -> Closed) chuyển dịch trạng thái chính xác.
  - 100% các chuyển đổi bất hợp pháp (ví dụ: New -> Close, InProgress -> Reject, Closed -> Any, Rejected -> Any) bị từ chối dứt khoát với `Result.IsFailure == true`.
  - **Bảo toàn trạng thái bất biến (State Invariant Conservation)**: Trong mọi trường hợp chuyển đổi thất bại, `ticket.StatusId` không bị thay đổi hoặc corrupt.

### 3.2. Thử thách Khôi phục Thực thể (Reconstitution via EF Core)
- **Kịch bản**: Khi EF Core materialize thực thể `Ticket` từ SQLite, nó gán trực tiếp giá trị vào thuộc tính `StatusId`. Liệu `CurrentState` có bị lệch pha với `StatusId` trong database?
- **Phân tích mã nguồn**:
  Trong `Ticket.cs` (dòng 24-35):
  ```csharp
  public int StatusId
  {
      get => _statusId;
      set
      {
          if (_statusId != value)
          {
              _statusId = value;
              _currentState = null; // Khôi phục đúng State khi EF Core hoặc tests gán giá trị
          }
      }
  }
  public ITicketState CurrentState => _currentState ??= TicketStateFactory.CreateState(this, _statusId);
  ```
- **Kết quả**: Cơ chế Lazy Invalidation tự động hủy instance cũ và `TicketStateFactory` tái tạo chính xác đối tượng State tương ứng với `_statusId`. Thử nghiệm thay đổi `ticket.StatusId = TicketStatus.Resolved` lập tức biến đổi `ticket.CanComment()` từ `true` sang `false` thành công mà không cần can thiệp thủ công.

### 3.3. Thử thách Tranh chấp Đồng thời & Chống Lost Updates (Concurrency Attack)
- **Kịch bản**: Hai kỹ thuật viên hoặc Leader cùng thao tác chuyển trạng thái ticket đồng thời.
- **Phân tích mã nguồn**:
  Mỗi lần gọi `TransitionTo`, thực thể gọi phương thức `RenewConcurrencyToken()` sinh một `Guid.NewGuid()` mới và cập nhật `UpdatedAt = DateTime.UtcNow`.
  Khi lưu qua `ItTmsDbContext`, bộ lọc `ConcurrencyTokenInterceptor` và cấu hình SQLite kiểm tra Optimistic Concurrency Token, ném ra `DbUpdateConcurrencyException` nếu token không khớp.
- **Kết quả**: An toàn trước rủi ro Lost Updates.

### 3.4. Thử thách Tham số Biên (Edge Case Mining)
- **Kịch bản A**: Gọi `Assign` với `staffId <= 0`.
  - `NewState` & `AssignedState` kiểm tra `if (staffId <= 0) return Result.Failure(TicketErrors.InvalidStaff);` -> Bị từ chối an toàn với mã `STAFF_ID_INVALID`.
- **Kịch bản B**: Gọi `Reject` với lý do rỗng (`""` hoặc `"   "`).
  - `NewState` kiểm tra `if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(TicketErrors.RejectionReasonRequired);` -> Bị từ chối an toàn với mã `REJECTION_REASON_REQUIRED`.
- **Kịch bản C**: Người dùng lạ (không phải Creator) cố tình gọi `Close` hoặc `Reopen` trên ticket `Resolved`.
  - `ResolvedState` kiểm tra `if (Ticket.CreatorId != userId) return Result.Failure(TicketErrors.UnauthorizedUser);` -> Bị từ chối an toàn với mã lỗi `UNAUTHORIZED_USER` (ErrorType.Forbidden).

---

## 4. FINDINGS & OBSERVATIONS (GHI NHẬN ĐÁNH GIÁ)

### 4.1. Critical Findings: Không có (0)
Không phát hiện lỗi nghiêm trọng, không có vi phạm an toàn logic, không có vi phạm bảo mật hay rò rỉ ngoại lệ.

### 4.2. Major Findings: Không có (0)
Các ràng buộc nghiệp vụ Bảng 7.2 đều được bảo đảm toàn vẹn.

### 4.3. Minor Observations (Quan sát khuyến nghị tối ưu cho tương lai):
1. **Kiểm tra tham số lý do trong `InProgressState.HoldForInfo`**:
   - *Hiện trạng*: Trong `InProgressState.HoldForInfo(string reason, int staffId)`, hệ thống kiểm tra `UnauthorizedStaff` nhưng chưa kiểm tra `string.IsNullOrWhiteSpace(reason)`, dù trong `TicketErrors.cs` đã khai báo sẵn `HoldReasonRequired`.
   - *Đánh giá rủi ro*: Thấp (Low). Phương thức `PutOnHold(staffId)` trên `Ticket.cs` tự động truyền chuỗi mặc định `"Tạm dừng chờ phản hồi"`.
   - *Khuyến nghị*: Trong Milestone 3 hoặc đợt tái cấu trúc tiếp theo, có thể bổ sung kiểm tra `if (string.IsNullOrWhiteSpace(reason)) return Result.Failure(TicketErrors.HoldReasonRequired);` để tăng cường độ chặt chẽ tuyệt đối khi API trực tiếp nhận input từ người dùng.

---

## 5. INTEGRITY VERIFICATION (XÁC MINH TÍNH LIÊM CHÍNH)

| Tiêu chí kiểm tra | Phát hiện | Đánh giá |
|---|---|---|
| Hardcoded test results / expected outputs trong mã nguồn | Không có | HỢP LỆ |
| Dummy hoặc Facade implementations thiếu logic thực tế | Không có | HỢP LỆ |
| Đi tắt (Bypass) nghiệp vụ State Pattern | Không có | HỢP LỆ |
| Báo cáo kết quả kiểm thử ngụy tạo | Không có (181 tests chạy độc lập xác nhận) | HỢP LỆ |
| Tự chứng thực (Self-certification) không qua kiểm chứng | Không có (Reviewer chạy độc lập từ CLI) | HỢP LỆ |

---

## 6. KẾT LUẬN CUỐI CÙNG

- **Quyết định thẩm định**: **`APPROVE`**
- Gói triển khai State Pattern của Milestone 2 hoàn toàn hợp lệ, đạt độ tin cậy và hoàn thiện cao, sẵn sàng phục vụ tầng Web Presentation và SignalR Hub của Milestone 3.
