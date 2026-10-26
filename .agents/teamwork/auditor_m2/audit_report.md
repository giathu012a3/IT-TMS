# BÁO CÁO KIỂM TOÁN TÍNH TOÀN VẸN (FORENSIC INTEGRITY AUDIT REPORT) — MILESTONE 2

**Work Product**: Milestone 2 — Design Patterns & Business Logic Engine (.NET 9 Clean Architecture)  
**Auditor**: `auditor_m2` (Forensic Auditor / Critic / Specialist)  
**Profile**: General Project — Mode: Development (theo `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`)  
**Verdict**: **CLEAN**  

---

## 1. TỔNG QUAN PHÁN QUYẾT & BẰNG CHỨNG THỰC TẾ

| Tiêu chí Kiểm toán | Yêu cầu Kiểm tra | Kết quả Pháp chứng | Trạng thái |
|---|---|---|---|
| **Anti-Hardcoding** | Không có kết quả kiểm thử hay trạng thái nào bị hardcode để qua mặt test suite | Toàn bộ 7 State classes, Observers và Security Filters tính toán trạng thái động qua logic nghiệp vụ thực thụ | **PASS** |
| **State Pattern Authenticity** | Cả 7 concrete state classes thực sự chuyển dịch trạng thái, không trả về kết quả giả lập (no dummy/facade) | Triển khai mô hình Fail-Closed trên `TicketStateBase`; ghi đè chính xác các hành vi hợp lệ, chặn đứng thao tác bất hợp pháp | **PASS** |
| **Observer Persistence** | `NotificationObserver` & `SystemLogObserver` thực sự insert bản ghi vào DbContext | Trực tiếp gọi `_dbContext.Notifications.AddRangeAsync` và `_dbContext.SystemLogs.AddAsync`, commit trong cùng transaction SQLite | **PASS** |
| **Security Filter Integrity** | `AuthorizeRolesAttribute` truy vấn claim người dùng và trạng thái tài khoản thời gian thực từ CSDL | Truy vấn `db.Users.FirstOrDefaultAsync(u => u.Id == userId)`, kiểm tra `UserStatus.Active`, thu hồi Cookie đăng nhập khi tài khoản bị khóa | **PASS** |
| **BOLA / IDOR Defense** | `AuthorizeTicketAccessAttribute` kiểm tra quyền sở hữu vé thực sự | So sánh `AssignedToId` với Staff và `CreatorId` với User, chặn 403 Forbidden và ghi log cảnh báo an ninh vào `SystemLogs` | **PASS** |
| **No Pre-populated Artifacts** | Không có tệp log, output hoặc kết quả kiểm thử nào được dựng sẵn để ngụy tạo | Quét toàn bộ workspace: 0 tệp log ngụy tạo, 0 tệp result giả mạo | **PASS** |
| **Independent Build & Test** | Tự biên dịch từ mã nguồn và thực thi toàn bộ test suite | `dotnet build`: 0 Warning(s), 0 Error(s). `dotnet test`: 282/282 tests Passed (100%) | **PASS** |

**Phán quyết nhị phân (Binary Verdict)**: **CLEAN** (Không phát hiện bất kỳ vi phạm tính toàn vẹn nào).

---

## 2. CHI TIẾT KẾT QUẢ ĐIỀU TRA THEO TỪNG GIAI ĐOẠN

### 2.1. Pha 1: Phân tích Tĩnh Mã Nguồn (Static Code Forensic Analysis)

#### 1. Động cơ State Pattern (7 Trạng thái: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected)
- **Tệp tin khảo sát**: `src/ItTms.Domain/States/*.cs` (11 tệp tin) và `src/ItTms.Domain/Entities/Ticket.cs`.
- **Cơ chế Fail-Closed**: `TicketStateBase` định nghĩa mặc định tất cả phương thức trả về `Result.Failure(TicketErrors.InvalidTransition(Name, action))`.
- **Triển khai cụ thể**:
  - `NewState`: Chỉ cho phép `Assign` (gán `AssignedToId`, chuyển sang `AssignedState`, phát `TicketAssignedEvent`) và `Reject` (gán `RejectionReason`, chuyển sang `RejectedState`). Mọi thao tác khác như `Close` đều bị từ chối với mã lỗi `INVALID_STATE_TRANSITION`, tuân thủ nghiêm ngặt Bảng 7.2 TC-05.
  - `AssignedState`: Cho phép `Reassign` và `StartProgress` (kiểm tra `AssignedToId == staffId`, chuyển `InProgressState`).
  - `InProgressState`: Cho phép `HoldForInfo` (chuyển `WaitingState`) và `Resolve` (gán `ResolvedAt = DateTime.UtcNow`, chuyển `ResolvedState`). Đồng thời ghi đè rõ ràng phương thức `Reject` trả về `Result.Failure(TicketErrors.CannotRejectInProgress)`, tuân thủ bất biến Bảng 7.2 TC-06.
  - `WaitingState`: Cho phép `StartProgress` (chuyển `InProgressState`) và `Resolve` (chuyển `ResolvedState`).
  - `ResolvedState`: Thuộc tính `CanComment` trả về `false` (khóa bình luận theo Bảng 7.2 TC-08). Phương thức `Close` kiểm tra người đóng phải là `CreatorId`, bắt buộc có `Feedback`, gán `ClosedAt`, chuyển sang `ClosedState`, phát sinh `TicketClosedEvent` (Bảng 7.2 TC-09).
  - `ClosedState` & `RejectedState`: Là hai Terminal State; khóa vĩnh viễn mọi hành vi chuyển dịch và `CanComment = false`.
- **Tính bao đóng thực thể `Ticket.cs`**:
  - `Ticket.CurrentState` ủy quyền động qua `TicketStateFactory.CreateState(this, _statusId)`.
  - Phương thức `internal void TransitionTo(ITicketState nextState, int changedByUserId)` cập nhật `_statusId`, gán `_currentState`, làm mới `ConcurrencyToken`, và sinh `TicketStatusChangedEvent`.

#### 2. Hệ thống Observer Pattern & Two-Phase Pipeline
- **Tệp tin khảo sát**: `src/ItTms.Application/Observers/NotificationObserver.cs`, `SystemLogObserver.cs`, `src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs`, `src/ItTms.Infrastructure/Events/InMemoryDomainEventDispatcher.cs`.
- **Xác minh `NotificationObserver`**:
  - Bắt `TicketCreatedEvent`: Truy vấn danh sách Leader có trạng thái `UserStatus.Active` qua `_dbContext.Users.AsNoTracking()` và tạo bản ghi thông báo tương ứng (TC-03).
  - Bắt `TicketAssignedEvent`: Tạo đồng thời thông báo cho Staff được phân công và User tạo ticket (TC-04).
  - Bắt `CommentAddedEvent`: Kiểm tra cờ `IsInternal`; nếu là ghi chú nội bộ thì TUYỆT ĐỐI không gửi thông báo cho User (TC-08).
  - Bắt `TicketClosedEvent`: Gửi thông báo kèm điểm rating cho Staff phụ trách (TC-09).
- **Xác minh `SystemLogObserver`**:
  - Ghi nhận đầy đủ thông tin kiểm toán (Action, Details, UserId, IpAddress, CreatedAt) vào bảng `SystemLogs` cho tất cả biến động: `CreateTicket`, `AssignTicket`, `UpdateStatus`, `AddComment`, `AddInternalNote`, `CloseTicket`, `DeactivateUser`.
- **Xác minh Two-Phase Dispatcher trong `ItTmsDbContext.SaveChangesAsync`**:
  - Pha 1 (In-Transaction): Điều phối sự kiện tới các Observers để nạp `Notifications` và `SystemLogs` vào cùng instance DbContext.
  - Giao dịch SQLite ACID: Lưu toàn bộ thay đổi (`Ticket`, `Notification`, `SystemLog`) trong một lệnh `base.SaveChangesAsync` duy nhất.
  - Pha 2 (Post-Commit): Chỉ sau khi dữ liệu đã commit bền vững vào SQLite mới kích hoạt gửi thông báo ra SignalR WebSocket.

#### 3. Bộ lọc An ninh MVC (Security Action Filters)
- **Tệp tin khảo sát**: `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs`, `AuthorizeTicketAccessAttribute.cs`.
- **Xác minh `AuthorizeRolesAttribute`**:
  - Bỏ qua các endpoint có `IAllowAnonymous`.
  - Kiểm tra `user.Identity.IsAuthenticated`; nếu chưa đăng nhập -> Chuyển hướng về `/Auth/Login`.
  - Trích xuất `ClaimTypes.NameIdentifier` (UserId).
  - Truy vấn CSDL thời gian thực: `db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)`.
  - Kiểm tra trạng thái tài khoản: Nếu `userEntity.Status != UserStatus.Active`, lập tức gọi `SignOutAsync` thu hồi cookie phiên và từ chối truy cập với thông báo tài khoản bị khóa (Bảng 7.2 TC-01).
  - Kiểm tra vai trò RBAC: So khớp `userEntity.Role` và Claims với `_allowedRoles`; nếu không khớp -> Trả về HTTP 403 Forbidden (Bảng 7.2 TC-02).
- **Xác minh `AuthorizeTicketAccessAttribute` (Phòng chống OWASP BOLA/IDOR)**:
  - Tra cứu thông tin vé từ CSDL: `db.Tickets.Select(t => new { t.Id, t.CreatorId, t.AssignedToId })`.
  - Cho phép Admin và Leader toàn quyền giám sát.
  - Đối với Staff: Kiểm tra `ticket.AssignedToId == currentUserId`. Nếu không trùng -> Ghi vết vi phạm an ninh `BOLA_Violation_Attempt` vào `SystemLogs` và trả về HTTP 403 Forbidden (Bảng 7.2 TC-07).
  - Đối với User: Kiểm tra `ticket.CreatorId == currentUserId`. Nếu không trùng -> Ghi vết vi phạm an ninh và trả về HTTP 403 Forbidden.

---

### 2.2. Pha 2: Xác minh Thực thi Độc lập (Independent Behavioral Verification)

#### 1. Lệnh Biên dịch Mã nguồn (Independent Compilation)
```powershell
dotnet build c:\IT_System\ItTms.sln
```
**Bằng chứng công cụ (Raw Output)**:
```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  ItTms.Domain -> c:\IT_System\src\ItTms.Domain\bin\Debug\net9.0\ItTms.Domain.dll
  ItTms.Application -> c:\IT_System\src\ItTms.Application\bin\Debug\net9.0\ItTms.Application.dll
  ItTms.Infrastructure -> c:\IT_System\src\ItTms.Infrastructure\bin\Debug\net9.0\ItTms.Infrastructure.dll
  ItTms.Web -> c:\IT_System\src\ItTms.Web\bin\Debug\net9.0\ItTms.Web.dll
  ItTms.Tests -> c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:08.41
```
*Kết luận*: Giải pháp biên dịch hoàn toàn sạch sẽ, không có bất kỳ warning hay error nào.

#### 2. Lệnh Thực thi Bộ Kiểm thử Toàn diện (Independent Test Execution)
```powershell
dotnet test c:\IT_System\ItTms.sln
```
**Bằng chứng công cụ (Raw Output)**:
```text
Test run for c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   282, Skipped:     0, Total:   282, Duration: 30 s - ItTms.Tests.dll (net9.0)
```
*Kết luận*: 100% (282/282 tests) được thực thi và đạt kết quả Passed:
- 181 tests từ bộ kiểm thử của worker_m2 (bao gồm 59 tests ma trận State Machine, 55 tests bảo mật Security & RBAC, 5 tests Observer Pattern Two-Phase).
- 101 tests từ bộ kiểm thử nghịch đảo (Adversarial stress-tests) từ các challenger agents đều đã được thực thi và vượt qua thành công.

---

## 3. ĐỐI CHIẾU CÁC KỊCH BẢN BẢNG 7.2 THUỘC PHẠM VI M2

| Kịch bản | Yêu cầu Nghiệm thu | Mã nguồn Triển khai | Kết quả Kiểm toán |
|---|---|---|---|
| **TC-01** | Khóa tài khoản vô hiệu hóa đăng nhập | `AuthorizeRolesAttribute` kiểm tra `User.Status == UserStatus.Active` từ DB ở mỗi request | **CLEAN** — Đã xác minh qua `AuthorizeRolesAttributeTests` |
| **TC-02** | Phân quyền truy cập 403 Forbidden | `AuthorizeRolesAttribute` kiểm tra danh sách vai trò cho phép | **CLEAN** — Đã xác minh qua `AuthorizeRolesAttributeTests` |
| **TC-03** | Tạo ticket New thông báo Leader | `NotificationObserver.HandleAsync(TicketCreatedEvent)` chèn bản ghi thông báo cho Leader | **CLEAN** — Đã xác minh qua `ObserverPatternTests` |
| **TC-04** | Phân công thông báo Staff & User | `NotificationObserver.HandleAsync(TicketAssignedEvent)` chèn thông báo cho cả Staff và User | **CLEAN** — Đã xác minh qua `ObserverPatternTests` |
| **TC-05** | Chặn đóng ticket đang New | `NewState.Close` trả về `INVALID_STATE_TRANSITION`, giữ nguyên trạng thái `New` | **CLEAN** — Đã xác minh qua `StateMachine49PermutationTests` |
| **TC-06** | Leader không thể Reject khi InProgress | `InProgressState.Reject` trả về `CANNOT_REJECT_IN_PROGRESS`, giữ nguyên `InProgress` | **CLEAN** — Đã xác minh qua `StateMachine49PermutationTests` |
| **TC-07** | Staff không thể xem ticket của người khác | `AuthorizeTicketAccessAttribute` kiểm tra `AssignedToId == staffId`, chặn 403 và ghi nhật ký BOLA | **CLEAN** — Đã xác minh qua `AuthorizeTicketAccessFilterTests` |
| **TC-08** | Chặn bình luận trên ticket Resolved | `ResolvedState.CanComment` trả về `false` | **CLEAN** — Đã xác minh qua `StateMachine49PermutationTests` |
| **TC-09** | Đóng và đánh giá ticket Resolved | `ResolvedState.Close(feedback)` chuyển `ClosedState`, lưu `Feedback`, phát `TicketClosedEvent` | **CLEAN** — Đã xác minh qua `StateMachine49PermutationTests` |

---

## 4. KẾT LUẬN KIỂM TOÁN (AUDIT VERDICT)

Căn cứ trên các bằng chứng pháp chứng thu thập được:
1. Không phát hiện bất kỳ hành vi gian lận (hardcoded outputs, dummy implementations, facade classes, fabricated outputs).
2. Toàn bộ logic nghiệp vụ State Pattern, Observer Pattern, Two-Phase Event Pipeline và Security Filters đều được cài đặt thực thụ với chất lượng sản xuất cao (production-grade), tuân thủ nguyên tắc Clean Architecture và triết lý `#Argon-Omega`.
3. Bộ kiểm thử 282 tests chạy thực tế trên SQLite in-memory đạt tỷ lệ thành công 100%.

**PHÁN QUYẾT CHÍNH THỨC**: **CLEAN**.  
Sản phẩm bàn giao Milestone 2 được chấp thuận nghiệm thu hoàn toàn.
