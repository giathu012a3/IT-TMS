# BRIEFING — 2026-10-25T09:50:00Z

## Mission
Thiết kế kiến trúc SignalR WebSocket Hub & Real-time Client cho hệ thống IT-TMS (.NET 9) loại bỏ triệt để anti-pattern polling 3s, hỗ trợ TicketHub tại `/hubs/ticket`, quản lý group (`ticket_{id}`, `user_{userId}`, `role_{roleName}`), real-time comment streaming, notification broadcasting và standalone client JS.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: c:\IT_System\.agents\teamwork\explorer_m3_2
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M3

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Ngôn ngữ giao tiếp: Tiếng Việt, bảo toàn các thuật ngữ kỹ thuật và định danh mã nguồn
- Áp dụng Argon-Omega cognitive kernel (BLUF, MECE, no-fluff, first-principles)
- Thư viện SignalR client phải là standalone local file trong `wwwroot/lib/microsoft-signalr/`, không dùng external CDN
- Báo cáo và kế hoạch xuất ra `signalr_realtime_plan.md` và `handoff.md`

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T09:50:00Z

## Investigation State
- **Explored paths**:
  - `src/ItTms.Web/Hubs/TicketHub.cs` (Hiện tại chỉ là skeleton cơ bản)
  - `src/ItTms.Web/Program.cs` (Đã có `AddSignalR()`, `MapHub<TicketHub>("/hubs/ticket")`)
  - `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs` (Hiện tại là logging stub)
  - `src/ItTms.Infrastructure/Events/InMemoryDomainEventDispatcher.cs` (Đã hỗ trợ 2 pha In-transaction & Post-commit)
  - `src/ItTms.Application/Common/Interfaces/INotificationDispatcher.cs` (Đã có 4 method)
  - `src/ItTms.Application/Observers/NotificationObserver.cs` (Xử lý tạo DB Notification trong transaction)
  - `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs` & `AuthorizeTicketAccessAttribute.cs` (Khai thác logic phân quyền claims & BOLA)
  - `src/ItTms.Web/wwwroot/` (Chưa có `microsoft-signalr` library)
- **Key findings**:
  - Tách bạch 2 pha: Observer lưu Notification trong DbContext (ACID), sau commit thì `InMemoryDomainEventDispatcher` gọi `SignalRNotificationDispatcher` phát sóng WebSocket.
  - Phân quyền BOLA/IDOR trên SignalR Hub: Cần kiểm tra quyền trước khi cho gia nhập `ticket_{id}` để tránh nghe lén.
  - Bảo mật ghi chú nội bộ: Phân tách phòng `ticket_{id}_internal` chỉ dành cho Staff/Leader/Admin.
  - Xử lý deactivation: Bắt `UserDeactivatedEvent` đẩy `ForceLogout` qua SignalR để ngắt phiên tức thì.
  - Khắc phục Circular Dependency: Triển khai `SignalRNotificationDispatcher` tại tầng Web hoặc chuyển `TicketHub` về Infrastructure.
- **Unexplored areas**: Không có, phạm vi M3.2 đã được điều tra và thiết kế toàn diện.

## Key Decisions Made
- Lựa chọn mô hình `SignalRNotificationDispatcher` tại `src/ItTms.Web/Services/` để inject trực tiếp `IHubContext<TicketHub>` mà không gây phụ thuộc vòng.
- Tách luồng phát sóng ghi chú nội bộ qua nhóm `ticket_{ticketId}_internal`.
- Standalone JS: Lưu trữ `signalr.min.js` cục bộ trong `wwwroot/lib/microsoft-signalr/`.

## Artifact Index
- `c:\IT_System\.agents\teamwork\explorer_m3_2\BRIEFING.md` — Persistent working memory
- `c:\IT_System\.agents\teamwork\explorer_m3_2\progress.md` — Liveness heartbeat
- `c:\IT_System\.agents\teamwork\explorer_m3_2\signalr_realtime_plan.md` — Detailed architecture & design plan
- `c:\IT_System\.agents\teamwork\explorer_m3_2\handoff.md` — 5-component handoff report
