# BÁO CÁO TRIỂN KHAI MILESTONE 2: DESIGN PATTERNS & BUSINESS LOGIC ENGINE
> **Dự án**: IT Ticket Management System (.NET 9 Clean Architecture) — IT-TMS  
> **Tác giả**: `worker_m2` (Implementer / QA / Specialist)  
> **Ngày hoàn thành**: 2026-10-25  
> **Trạng thái**: HOÀN THÀNH 100% — Tất cả 181/181 Tests Passed (0 warnings, 0 errors)

---

## 1. TỔNG QUAN KẾT QUẢ THỰC HIỆN

Trong khuôn khổ Milestone 2 (M2), toàn bộ động cơ nghiệp vụ cốt lõi và các mẫu thiết kế hướng đối tượng (GoF Design Patterns) đã được xây dựng và kiểm chứng độc lập theo chuẩn `#Argon-Omega`:

1. **State Pattern (7 Trạng thái)**:
   - Thư mục: `src/ItTms.Domain/States/`
   - Đóng gói trọn vẹn 7 trạng thái vòng đời của Ticket (`New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`).
   - Loại bỏ hoàn toàn exception trong domain business logic: 100% phương thức trả về `Result` hoặc `Result<T>` với danh mục lỗi chuẩn hóa `TicketErrors`.
   - Thuộc tính bất biến `CanComment`: `true` đối với `New`, `Assigned`, `InProgress`, `Waiting`; `false` đối với `Resolved`, `Closed`, `Rejected` (Bảng 7.2 TC-08).
   - Tái cấu trúc thực thể `Ticket.cs`: Ủy quyền toàn bộ hành vi chuyển đổi cho `CurrentState` thông qua `TicketStateFactory`, bảo toàn 100% tính tương thích ngược với các hàm gọi cũ từ Milestone 1.

2. **Observer Pattern & Two-Phase Domain Events Pipeline**:
   - Thư mục: `src/ItTms.Domain/Events/`, `src/ItTms.Application/Observers/`, `src/ItTms.Infrastructure/Events/`
   - Bổ sung `UserDeactivatedEvent` và chuẩn hóa giao diện `IHasDomainEvents`.
   - Triển khai `NotificationObserver`: Tự động gửi thông báo cho Leader khi có ticket mới (Bảng 7.2 TC-03), cho Staff & User khi phân công (Bảng 7.2 TC-04), cho User khi cập nhật tiến độ, bảo mật tuyệt đối không gửi thông báo cho User khi ghi chú là `IsInternal` (TC-08), và thông báo cho Staff khi đóng ticket (TC-09).
   - Triển khai `SystemLogObserver`: Ghi nhận nhật ký kiểm toán bất biến (Immutable Audit Trail) vào bảng `SystemLogs` cho tất cả biến động dữ liệu quan trọng.
   - Triển khai Bộ điều phối hai pha (`Two-Phase Domain Event Dispatcher`): Pha 1 nạp dữ liệu vào cùng DbContext trong Transaction; Pha 2 phát sóng thời gian thực qua SignalR WebSocket sau khi commit.

3. **Security Filters & Decorators (Lớp Bảo mật Web)**:
   - Thư mục: `src/ItTms.Web/Filters/`
   - `AuthorizeRolesAttribute`: Kiểm tra xác thực (Authentication), kiểm tra vai trò (RBAC - TC-02), và kiểm tra trạng thái tài khoản trong thời gian thực từ CSDL (`IsActive == true` - TC-01). Tự động thu hồi phiên đăng nhập khi tài khoản bị vô hiệu hóa.
   - `AuthorizeTicketAccessAttribute`: Chống lỗ hổng OWASP API1:2023 BOLA/IDOR (TC-07). Chặn đứng Staff truy cập ticket không được giao hoặc User truy cập ticket người khác, tự động ghi vết vi phạm an ninh vào `SystemLogs`.

4. **Bộ Test Suite Mở rộng (State Machine 49 Hoán vị & Security/Observer Tests)**:
   - `StateMachine49PermutationTests.cs`: 49 hoán vị ma trận $7 \times 7$ trạng thái và hành động, kiểm chứng tính bảo toàn trạng thái bất biến và các kịch bản nghiệm thu TC-05, TC-06, TC-08, TC-09.
   - `AuthorizeRolesAttributeTests.cs`: Kiểm thử tự động TC-01 và TC-02.
   - `AuthorizeTicketAccessFilterTests.cs`: Kiểm thử tự động chống BOLA/IDOR TC-07.
   - `ObserverPatternTests.cs`: Kiểm thử toàn diện 2 Observers và luồng Two-Phase Dispatching trong cùng một giao dịch SQLite ACID.

---

## 2. CHI TIẾT CÁC TỆP TIN ĐÃ TẠO VÀ SỬA ĐỔI

### 2.1. Mã Nguồn Mới Được Tạo
| Tệp tin | Vị trí | Mô tả chức năng |
|---------|--------|-----------------|
| `ITicketState.cs` | `src/ItTms.Domain/States/` | Hợp đồng giao diện GoF State Pattern cho Ticket |
| `TicketStateBase.cs` | `src/ItTms.Domain/States/` | Lớp cơ sở trừu tượng cung cấp cơ chế Fail-Closed mặc định |
| `TicketErrors.cs` | `src/ItTms.Domain/States/` | Bộ mã lỗi nghiệp vụ chuẩn hóa cho State Pattern |
| `NewState.cs` | `src/ItTms.Domain/States/` | Trạng thái Mới tạo (StatusId = 1) |
| `AssignedState.cs` | `src/ItTms.Domain/States/` | Trạng thái Đã phân công (StatusId = 2) |
| `InProgressState.cs` | `src/ItTms.Domain/States/` | Trạng thái Đang xử lý (StatusId = 3, chặn Reject TC-06) |
| `WaitingState.cs` | `src/ItTms.Domain/States/` | Trạng thái Chờ phản hồi (StatusId = 4) |
| `ResolvedState.cs` | `src/ItTms.Domain/States/` | Trạng thái Đã giải quyết (StatusId = 5, khóa CanComment TC-08) |
| `ClosedState.cs` | `src/ItTms.Domain/States/` | Trạng thái Đã đóng (StatusId = 6, Terminal State) |
| `RejectedState.cs` | `src/ItTms.Domain/States/` | Trạng thái Đã từ chối (StatusId = 7, Terminal State) |
| `TicketStateFactory.cs` | `src/ItTms.Domain/States/` | Factory khôi phục State instance theo StatusId |
| `IHasDomainEvents.cs` | `src/ItTms.Domain/Common/` | Giao diện thu thập Domain Events cho AggregateRoot |
| `UserDeactivatedEvent.cs` | `src/ItTms.Domain/Events/` | Sự kiện khi tài khoản bị vô hiệu hóa (TC-10) |
| `IDomainEventObserver.cs` | `src/ItTms.Application/Common/Interfaces/` | Giao diện Observer cho Domain Events |
| `IDomainEventDispatcher.cs` | `src/ItTms.Application/Common/Interfaces/` | Hợp đồng điều phối sự kiện hai pha |
| `INotificationDispatcher.cs` | `src/ItTms.Application/Common/Interfaces/` | Hợp đồng gửi thông báo thời gian thực |
| `NotificationObserver.cs` | `src/ItTms.Application/Observers/` | Observer tạo thông báo cho User, Staff, Leader |
| `SystemLogObserver.cs` | `src/ItTms.Application/Observers/` | Observer ghi nhật ký kiểm toán bất biến |
| `InMemoryDomainEventDispatcher.cs` | `src/ItTms.Infrastructure/Events/` | Bộ điều phối Domain Events hai pha |
| `SignalRNotificationDispatcher.cs` | `src/ItTms.Infrastructure/Realtime/` | Bộ phát sóng thông báo thời gian thực qua SignalR |
| `AuthorizeRolesAttribute.cs` | `src/ItTms.Web/Filters/` | Bộ lọc an ninh kiểm tra RBAC và tài khoản Active |
| `AuthorizeTicketAccessAttribute.cs` | `src/ItTms.Web/Filters/` | Bộ lọc an ninh chống BOLA/IDOR trên Ticket |
| `StateMachine49PermutationTests.cs` | `tests/ItTms.Tests/UnitTests/Domain/` | Bộ test 49 hoán vị ma trận State Machine |
| `AuthorizeRolesAttributeTests.cs` | `tests/ItTms.Tests/UnitTests/Security/` | Unit test bộ lọc phân quyền vai trò (TC-01, TC-02) |
| `AuthorizeTicketAccessFilterTests.cs` | `tests/ItTms.Tests/UnitTests/Security/` | Unit test bộ lọc chống BOLA/IDOR (TC-07) |
| `ObserverPatternTests.cs` | `tests/ItTms.Tests/UnitTests/Observers/` | Unit test Observer Pattern và Two-Phase Dispatcher |

### 2.2. Mã Nguồn Đã Refactor & Nâng cấp
| Tệp tin | Thay đổi chính |
|---------|----------------|
| `src/ItTms.Domain/Entities/Ticket.cs` | Tích hợp State Pattern: thêm `_currentState`, `_statusId`, `CurrentState`, ủy quyền toàn bộ các phương thức `Assign`, `Reassign`, `StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reopen`, `Reject`, `CanComment` sang `CurrentState`. |
| `src/ItTms.Domain/Entities/User.cs` | Chuyển sang kế thừa `AggregateRoot<int>`, phát sinh `UserDeactivatedEvent` khi gọi `Deactivate`. |
| `src/ItTms.Domain/Common/AggregateRoot.cs` | Triển khai giao diện `IHasDomainEvents`. |
| `src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs` | Tích hợp Two-Phase Dispatching vào `SaveChangesAsync`: Pha 1 in-transaction (ghi nhận Notification và SystemLog cùng transaction), Pha 2 post-commit. |
| `src/ItTms.Application/DependencyInjection.cs` | Đăng ký `NotificationObserver` và `SystemLogObserver` cho các Domain Events tương ứng. |
| `src/ItTms.Infrastructure/DependencyInjection.cs` | Đăng ký `INotificationDispatcher` và `IDomainEventDispatcher`. |

---

## 3. ĐỐI CHIẾU CÁC KỊCH BẢN NGHIỆM THU BẢNG 7.2

| Mã Kịch bản | Yêu cầu Kịch bản | Cơ chế Triển khai M2 | Kết quả Kiểm thử |
|-------------|------------------|----------------------|------------------|
| **TC-01** | Đăng nhập bằng tài khoản bị khóa bị từ chối | `AuthorizeRolesAttribute` kiểm tra `User.Status == UserStatus.Active` trong CSDL thời gian thực, hủy cookie và redirect Login kèm thông báo | ✅ PASSED (`AuthorizeRolesAttributeTests.OnActionExecutionAsync_WhenUserIsInactive_RejectsAccess_Table72TC01`) |
| **TC-02** | Truy cập trái quyền trả về 403 Forbidden | `AuthorizeRolesAttribute` so khớp vai trò người dùng với `_allowedRoles`, trả về HTTP 403 Forbidden | ✅ PASSED (`AuthorizeRolesAttributeTests.OnActionExecutionAsync_WhenUserHasWrongRole_Returns403Forbidden_Table72TC02`) |
| **TC-03** | Tạo ticket New gửi thông báo cho Leader | `NotificationObserver` bắt `TicketCreatedEvent`, tự động tạo bản ghi `Notification` cho các Leader Active | ✅ PASSED (`ObserverPatternTests.NotificationObserver_OnTicketCreated_NotifiesAllActiveLeaders_Table72TC03`) |
| **TC-04** | Phân công ticket gửi thông báo Staff & User | `NotificationObserver` bắt `TicketAssignedEvent`, tạo đồng thời 2 thông báo cho Staff được giao và User tạo yêu cầu | ✅ PASSED (`ObserverPatternTests.NotificationObserver_OnTicketAssigned_NotifiesStaffAndUser_Table72TC04`) |
| **TC-05** | Chặn đóng ticket đang ở trạng thái New | `NewState.Close(...)` trả về `INVALID_STATE_TRANSITION`, giữ nguyên trạng thái `New` | ✅ PASSED (`StateMachine49PermutationTests.StateMachine_TC05_NewTicketCannotBeClosedDirectly`) |
| **TC-06** | Leader không thể Reject ticket đang InProgress | `InProgressState.Reject(...)` trả về `CANNOT_REJECT_IN_PROGRESS`, giữ nguyên `InProgress` | ✅ PASSED (`StateMachine49PermutationTests.StateMachine_TC06_InProgressTicketCannotBeRejected`) |
| **TC-07** | Staff không thể truy cập ticket của người khác | `AuthorizeTicketAccessAttribute` kiểm tra `ticket.AssignedToId == currentStaffId`, trả về HTTP 403 Forbidden và ghi log cảnh báo | ✅ PASSED (`AuthorizeTicketAccessFilterTests.Staff_AccessingAnotherStaffTicket_Returns403Forbidden_Table72TC07`) |
| **TC-08** | Chặn bình luận trên ticket Resolved | `ResolvedState.CanComment` trả về `false`, khóa bình luận | ✅ PASSED (`StateMachine49PermutationTests.StateMachine_CanComment_MatchesStateRequirements`) |
| **TC-09** | Đóng và đánh giá ticket Resolved | `ResolvedState.Close(feedback)` chuyển sang `ClosedState`, lưu `Feedback`, phát sinh `TicketClosedEvent` | ✅ PASSED (`StateMachine49PermutationTests.StateMachine_TC09_CloseAndRateResolvedTicket_TransitionsToClosedWithFeedback`) |
| **TC-10** | Admin không thể tự khóa chính mình | `User.Deactivate(adminId)` chặn khi `Id == operatingAdminId`, phát sinh `UserDeactivatedEvent` khi khóa người khác | ✅ PASSED (`UserInvariantTests`, `ObserverPatternTests`) |

---

## 4. KẾT QUẢ XÁC MINH BUILD & TEST

### 4.1. Lệnh Biên dịch Solution
```powershell
dotnet build c:\IT_System\ItTms.sln
```
**Kết quả**:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:12.96
```

### 4.2. Lệnh Thực thi Test Suite Toàn cục
```powershell
dotnet test c:\IT_System\ItTms.sln
```
**Kết quả**:
```text
Test run for c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   181, Skipped:     0, Total:   181, Duration: 28 s - ItTms.Tests.dll (net9.0)
```
- **Tổng số tests**: 181 tests
- **Tests đạt (Passed)**: 181 tests (100%)
- **Tests thất bại (Failed)**: 0 tests
- **Tests bỏ qua (Skipped)**: 0 tests
- Tăng trưởng: +72 tests so với mốc baseline 109 tests của Milestone 1.

---

## 5. KẾT LUẬN & SẴN SÀNG CHO MILESTONE 3
Milestone 2 đã hoàn thành trọn vẹn, đáp ứng 100% các tiêu chuẩn thiết kế kiến trúc Clean Architecture, GoF Design Patterns, và bảo mật Zero-Trust OWASP. Nền tảng State Pattern, Observer Pattern, và Security Filters đã sẵn sàng để tích hợp vào Tầng Presentation MVC 5 Areas và SignalR Real-Time Hub trong Milestone 3.
