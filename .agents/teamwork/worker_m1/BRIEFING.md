# BRIEFING — 2026-10-25T14:07:30Z

## Mission
Triển khai giải pháp Backend Realtime Engine & Events cho IT-TMS .NET 9: Phát sinh `CommentAddedEvent` trong `AddComment` trên 3 Controllers (`User`, `Staff`, `Leader`), hoàn thiện phản hồi Ajax trả về `commentDto`, cập nhật `SignalRNotificationDispatcher.HandleCommentAddedAsync` định tuyến group và thông báo chuông cho counterpart users (chặn User nhận internal comments), và viết kiểm thử tự động toàn diện.

## 🔒 My Identity
- Archetype: worker_m1
- Roles: implementer, qa, specialist
- Working directory: c:\IT_System\.agents\teamwork\worker_m1
- Original parent: ff0e71b2-5cba-4663-bd07-29f7639ba776
- Milestone: M1 Backend Realtime Engine & Events

## 🔒 Key Constraints
- File ownership nghiêm ngặt:
  - `src/ItTms.Web/Areas/User/Controllers/TicketController.cs`
  - `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs`
  - `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs`
  - `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs`
  - `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`
  - `tests/ItTms.Tests/UnitTests/CommentEventTests.cs`
  - TUYỆT ĐỐI không sửa file ngoài danh sách sở hữu.
- Integrity Mandate: Không hardcode test kết quả, không tạo facade rỗng, giữ vững tính toàn vẹn 20 Use Cases.
- Bắt buộc chạy `dotnet build` và `dotnet test` vượt qua 100% không lỗi.

## Current Parent
- Conversation ID: ff0e71b2-5cba-4663-bd07-29f7639ba776
- Updated: 2026-10-25T14:07:30Z

## Task Summary
- **What to build**:
  1. `AddComment` trên 3 controllers: tạo comment, lưu CSDL, gắn `CommentAddedEvent` vào `ticket.AddDomainEvent(...)`, lưu tiếp để dispatch observer/realtime, trả về Ajax JSON với DTO `comment`.
  2. `SignalRNotificationDispatcher.HandleCommentAddedAsync`: push `ReceiveComment` vào group `ticket_{id}` hoặc `ticket_{id}_internal`; push `ReceiveNotification` chuông đến counterpart users (bảo vệ BOLA: User tuyệt đối không nhận thông báo nội bộ).
  3. Viết Unit / Integration Tests kiểm tra đầy đủ event propagation và SignalR routing.
- **Success criteria**:
  - `dotnet build c:\IT_System\ItTms.sln` 0 warnings/errors.
  - `dotnet test c:\IT_System\ItTms.sln` all pass (342/342 tests).
  - Handoff report `handoff.md` chi tiết.

## Key Decisions Made
- [Phase 1/2 Save]: Lưu comment lần 1 để SQLite sinh identity Id, sau đó gắn `CommentAddedEvent(comment.Id, ...)` và gọi SaveChangesAsync lần 2 để kích hoạt ChangeTracker dispatch in-transaction Observers (Notification, SystemLog) và post-commit SignalR dispatch.
- [EF Core Translation]: Thay thế unmapped `u.IsActive` bằng `u.Status == UserStatus.Active` trong LINQ query của `SignalRNotificationDispatcher.HandleCommentAddedAsync`.
- [Dual-Layer Dispatcher]: Cập nhật cả `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs` và `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`.
- [Testing Architecture]: `CommentEventTests.cs` kiểm tra cả luồng HTTP Controller lẫn SignalR Dispatcher với `TestHubContext`.

## Artifact Index
- `c:\IT_System\.agents\teamwork\worker_m1\DISPATCH.md` — Nhiệm vụ được giao
- `c:\IT_System\.agents\teamwork\worker_m1\BRIEFING.md` — Trạng thái làm việc
- `c:\IT_System\.agents\teamwork\worker_m1\progress.md` — Tiến độ thực thi
- `c:\IT_System\.agents\teamwork\worker_m1\handoff.md` — Báo cáo nghiệm thu

## Change Tracker
- **Files modified**:
  - `src/ItTms.Web/Areas/User/Controllers/TicketController.cs`: Gắn CommentAddedEvent, trả CommentDto cho Ajax.
  - `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs`: Gắn CommentAddedEvent, hỗ trợ isInternal, trả CommentDto.
  - `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs`: Gắn CommentAddedEvent, hỗ trợ isInternal, trả CommentDto.
  - `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`: Định tuyến ReceiveComment và ReceiveNotification chuông, chặn User nhận ghi chú nội bộ.
  - `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs`: Đồng bộ phương thức HandleCommentAddedAsync.
  - `tests/ItTms.Tests/UnitTests/CommentEventTests.cs`: 6 bài test xUnit toàn diện.
- **Build status**: `dotnet build` PASSED (0 warnings, 0 errors).
- **Pending issues**: Không còn.

## Quality Status
- **Build/test result**: 342/342 tests PASSED (100%).
- **Lint status**: Clean (CA1305 tuân thủ đầy đủ).
- **Tests added/modified**: +6 tests mới trong `CommentEventTests.cs`.

## Loaded Skills
- N/A
