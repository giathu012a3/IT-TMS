# BÁO CÁO KIỂM THỬ ĐỐI KHÁNG VÀ ÁP LỰC (CHALLENGE REPORT) — STATE PATTERN
> **Người thực hiện**: `challenger_m2_1` (Empirical Challenger)  
> **Mục tiêu**: Adversarial stress-testing và falsification động cơ State Pattern (.NET 9)  
> **Thời điểm**: 2026-10-25  
> **Phán quyết (Verdict)**: **`APPROVE`** (Chấp thuận hoàn toàn)

---

## 1. TỔNG QUAN ĐÁNH GIÁ RỦI RO (CHALLENGE SUMMARY)

**Mức độ rủi ro tổng thể (Overall Risk Assessment)**: **LOW** (Rủi ro cực thấp — Kiến trúc đạt chuẩn sản xuất vững chắc)

Bộ kiểm thử đối kháng chuyên sâu `StatePatternAdversarialChallengeTests.cs` (80 test cases mới) đã được thiết kế và thực thi trực tiếp trên .NET 9 nhằm công kích toàn diện State Pattern của Milestone 2 trên các khía cạnh:
1. **Ma trận toàn diện 49 hoán vị trạng thái ($7 \times 7$)**: Khảo sát cả 12 bước chuyển hợp lệ và 37 bước chuyển bất hợp pháp, kiểm chứng tính bảo toàn trạng thái bất biến 100% khi thao tác thất bại.
2. **Kịch bản biên & Corner Cases**: Thử nghiệm mở lại ticket đã đóng (`Closed`), từ chối (`Reject`) trên các trạng thái khác `New`, đóng ticket với đánh giá `null`/sai dải điểm, thao tác trái quyền bởi Staff hoặc User khác.
3. **Áp lực đồng thời (High-Concurrency Race Condition)**: Kích hoạt 10 tác vụ đồng thời qua `Barrier` trên cơ sở dữ liệu SQLite thực với Optimistic Concurrency Control (`ConcurrencyToken`).
4. **Fuzzing ngẫu nhiên (Property-Based Randomized Fuzzing)**: Thực thi 1,000 chu kỳ hoán vị ngẫu nhiên trạng thái và hành động nhằm phát hiện rò rỉ ngoại lệ (Zero Exception Invariant).

---

## 2. KẾT QUẢ THỰC NGHIỆM ĐỐI KHÁNG (STRESS TEST RESULTS)

| # | Kịch bản Thử nghiệm | Kỳ vọng (Expected) | Thực tế (Actual) | Kết quả |
|---|---------------------|---------------------|------------------|---------|
| 1 | **49 Hoán vị State Machine**: 12 chuyển đổi hợp lệ (`New->Assigned`, `New->Rejected`, `Assigned->InProgress`, `InProgress->Waiting`, `InProgress->Resolved`, `Waiting->InProgress`, `Waiting->Resolved`, `Resolved->Closed`, `Resolved->InProgress`, và 3 Reassign). | Trả về `Result.Success()`, trạng thái chuyển dịch đúng, phát sinh `TicketStatusChangedEvent`, cấp mới `ConcurrencyToken`. | 100% chuyển đổi hợp lệ thành công, chuyển đúng State, phát sinh Domain Event, đổi token. | **PASS** |
| 2 | **49 Hoán vị State Machine**: 37 chuyển đổi bất hợp pháp (ví dụ `New->Closed`, `Assigned->Resolve`, `Closed->StartProgress`, `Rejected->Assign`...). | Trả về `Result.Failure` kèm mã lỗi rõ ràng; bảo toàn 100% `StatusId`, `CurrentState`, `ConcurrencyToken`, `UpdatedAt`; 0 domain event ảo; không đổi các trường phụ trợ. | 100% bị từ chối an toàn, dữ liệu không biến đổi, 0 ngoại lệ phát sinh. | **PASS** |
| 3 | **Mở lại Ticket đã Closed** (`ticket.Reopen(...)`). | Bị từ chối với mã lỗi `TICKET_ALREADY_CLOSED`; giữ nguyên trạng thái `Closed`; không xóa `ClosedAt`. | Trả về `Failure("TICKET_ALREADY_CLOSED")`, `ClosedAt` nguyên vẹn. | **PASS** |
| 4 | **Từ chối (Reject) ở các trạng thái non-New**: `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`. | Bị từ chối với `CANNOT_REJECT_IN_PROGRESS` đối với InProgress (Bảng 7.2 TC-06), `TICKET_ALREADY_CLOSED`, `TICKET_ALREADY_REJECTED`, hoặc `INVALID_STATE_TRANSITION`. | 100% chặn đứng chính xác, đúng mã lỗi đặc tả của từng trạng thái. | **PASS** |
| 5 | **Đóng Ticket với Feedback null** (`ticket.Close(feedback: null!)`). | Không văng `NullReferenceException`, trả về `Result.Failure("FEEDBACK_REQUIRED")`, giữ nguyên trạng thái `Resolved`. | Trả về thất bại an toàn, không có lỗi unhandled exception. | **PASS** |
| 6 | **Tạo Feedback với Rating ngoài dải [1..5]** (`0`, `-1`, `6`, `100`). | Bị từ chối tại Value Object với mã `FEEDBACK_RATING_RANGE`. | 100% dải điểm không hợp lệ bị chặn đứng ngay tại Value Object. | **PASS** |
| 7 | **Mở lại (Reopen) với lý do rỗng/khoảng trắng** (`""`, `"   "`, `null`). | Bị từ chối với mã `REOPEN_REASON_REQUIRED`, giữ nguyên trạng thái `Resolved`. | Trả về thất bại chuẩn xác, không làm thay đổi trạng thái. | **PASS** |
| 8 | **Từ chối (Reject) trên New với lý do rỗng** (`""`, `"   "`, `null`). | Bị từ chối với mã `REJECTION_REASON_REQUIRED`, giữ nguyên trạng thái `New`. | Trả về thất bại chuẩn xác, `RejectionReason` giữ giá trị null. | **PASS** |
| 9 | **Phân công (Assign) với StaffId không hợp lệ** (`0`, `-1`). | Bị từ chối với mã `STAFF_ID_INVALID`, giữ nguyên trạng thái `New`, `AssignedToId` null. | Bị từ chối chuẩn xác, không ghi nhận phân công ảo. | **PASS** |
| 10 | **Staff không được giao cố tình thao tác** (`StartProgress`, `HoldForInfo`, `Resolve`). | Bị từ chối với mã `UNAUTHORIZED_STAFF`, không thay đổi trạng thái Ticket. | Trả về `UNAUTHORIZED_STAFF`, trạng thái được giữ nguyên. | **PASS** |
| 11 | **User không phải người tạo cố tình thao tác** (`Close`, `Reopen`). | Bị từ chối với mã `UNAUTHORIZED_USER`, không thay đổi trạng thái Ticket. | Trả về `UNAUTHORIZED_USER`, trạng thái được giữ nguyên. | **PASS** |
| 12 | **Ràng buộc bất biến `CanComment` trên cả 7 trạng thái**: `New`, `Assigned`, `InProgress`, `Waiting` trả về `true`; `Resolved`, `Closed`, `Rejected` trả về `false`. | `CanComment` khớp 100% đặc tả (Bảng 7.2 TC-08). | Khớp hoàn hảo cả trên thuộc tính của State lẫn Entity. | **PASS** |
| 13 | **Đua đồng thời 10 tác vụ từ `NewState` trên SQLite**: 4 tác vụ Assign, 2 tác vụ Reject, 4 tác vụ bất hợp pháp (Close, Resolve) kích hoạt cùng lúc qua `Barrier`. | 4 tác vụ bất hợp pháp bị chặn ngay tại Domain; đúng 1 tác vụ hợp lệ ghi thành công xuống CSDL; 5 tác vụ còn lại gặp `DbUpdateConcurrencyException`. | Thành công: 1, Xung đột: 5, Thất bại nghiệp vụ: 4, Lỗi bất thường: 0. | **PASS** |
| 14 | **Đua đồng thời 10 tác vụ từ `InProgressState` trên SQLite**: 3 tác vụ Reject (TC-06), 4 tác vụ Resolve, 3 tác vụ PutOnHold. | 3 tác vụ Reject bị chặn đứng ngay lập tức với `CANNOT_REJECT_IN_PROGRESS`; đúng 1 tác vụ Resolve/Hold ghi thành công; 6 tác vụ còn lại nhận `DbUpdateConcurrencyException`. | Chặn Reject: 3, Thành công: 1, Xung đột: 6, Lỗi bất thường: 0. Trạng thái cuối trong DB là Resolved hoặc Waiting hợp lệ. | **PASS** |
| 15 | **Fuzzing ngẫu nhiên 1,000 chu kỳ hoán vị**: Gọi ngẫu nhiên 7 hành động trên 7 trạng thái với dữ liệu ngẫu nhiên. | Không có ngoại lệ không mong muốn; nếu trạng thái đổi thì nằm trong [1..7] và token được đổi mới; nếu thất bại thì bảo toàn trạng thái. | 1,000/1,000 chu kỳ vượt qua trọn vẹn, 0 unhandled exception. | **PASS** |

---

## 3. CHI TIẾT CÁC THÁCH THỨC VÀ BIỆN PHÁP PHÒNG THỦ (CHALLENGES & MITIGATIONS)

### [Low Risk] Thách thức 1: Tính bảo toàn trạng thái (State Immutability) khi thao tác thất bại
- **Giả định bị thách thức**: Khi gọi phương thức chuyển trạng thái không hợp lệ, entity có thể bị thay đổi dở dang (ví dụ cập nhật `UpdatedAt`, đổi `ConcurrencyToken`, sinh domain event giả hoặc ghi đè thuộc tính trước khi trả về lỗi).
- **Kịch bản tấn công**: Thực thi toàn bộ 37 hoán vị cấm và 14 trường hợp tham số không hợp lệ; đối chiếu từng trường (`StatusId`, `CurrentState`, `ConcurrencyToken`, `UpdatedAt`, `DomainEvents`, `AssignedToId`, `ResolvedAt`, `ClosedAt`, `RejectionReason`, `Feedback`).
- **Phát hiện thực nghiệm**: Lớp cơ sở `TicketStateBase` áp dụng mẫu Fail-Closed hoàn hảo. Khi một hành vi không được phép, hàm trả về ngay `Result.Failure(TicketErrors.InvalidTransition)` mà không hề chạm vào bất kỳ thuộc tính nào của `Ticket`.
- **Đánh giá**: **BẢO VỆ VỮNG CHẮC (ROBUST)**.

### [Low Risk] Thách thức 2: Tránh Lost Updates dưới tải truy cập đồng thời trên SQLite
- **Giả định bị thách thức**: SQLite trong môi trường web đa luồng có thể bị xung đột khóa (database locked) hoặc bị ghi đè dữ liệu trạng thái nếu hai nhân viên kỹ thuật cùng thao tác một lúc.
- **Kịch bản tấn công**: Dùng `Barrier(10)` đồng bộ chính xác thời điểm gọi `SaveChangesAsync()` trên 10 DbContext khác nhau cho cùng một bản ghi Ticket.
- **Phát hiện thực nghiệm**: Hệ thống áp dụng `ConcurrencyTokenInterceptor` kết hợp với `ConcurrencyToken` kiểu `Guid`. Khi một tác vụ commit thành công, token thay đổi, khiến tất cả các tác vụ cạnh tranh khác lập tức bị chặn bởi `DbUpdateConcurrencyException` của EF Core 9 mà không xảy ra tình trạng Lost Update hay treo database (`busy_timeout` và `WAL mode` được cấu hình chuẩn xác).
- **Đánh giá**: **BẢO VỆ VỮNG CHẮC (ROBUST)**.

---

## 4. VÙNG KHÔNG KHẢO SÁT (UNCHALLENGED AREAS)
- **Tầng Giao diện Người dùng và Client-side WebSocket JavaScript**: Thuộc phạm vi của Milestone 3 (Presentation Layer, 5 Areas MVC & SignalR Client).
- **Hạ tầng CSDL phân tán (PostgreSQL/SQL Server)**: Dự án đặc tả sử dụng SQLite tự quản (zero-setup execution). Kiểm thử đồng thời đã chạy trên real SQLite instance với chế độ WAL.

---

## 5. PHÁN QUYẾT CUỐI CÙNG (FINAL VERDICT)

**VERDICT: `APPROVE`**

Triển khai State Pattern của `worker_m2` đạt chất lượng xuất sắc theo tiêu chuẩn `#Argon-Omega`:
1. Tuyệt đối tuân thủ Clean Architecture và GoF State Pattern.
2. 100% không phát sinh ngoại lệ cho logic nghiệp vụ (Railway-oriented `Result`).
3. Khả năng phòng vệ đồng thời và bảo toàn trạng thái bất biến đạt 100% trong toàn bộ 80 bài kiểm thử đối kháng mới.
4. Toàn bộ Solution biên dịch 0 warnings, 0 errors và vượt qua 282/282 tests.
