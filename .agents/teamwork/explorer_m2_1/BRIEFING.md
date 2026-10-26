# BRIEFING — 2026-10-25T09:11:30Z

## Mission
Thiết kế kiến trúc toàn diện cho State Pattern và 7 Concrete States trong `src/ItTms.Domain/States/` tích hợp thực thể `Ticket`, bảo đảm Railway-oriented Result<T>, thuộc tính CanComment và tuân thủ các kịch bản kiểm thử Bảng 7.2.

## 🔒 My Identity
- Archetype: explorer
- Roles: explorer, synthesizer
- Working directory: c:\IT_System\.agents\teamwork\explorer_m2_1
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M2 - Design Patterns & Business Logic Engine

## 🔒 Key Constraints
- Read-only investigation — do NOT implement code directly in src/ (chỉ thiết kế kiến trúc, viết kế hoạch và báo cáo)
- Thiết kế đầy đủ State Pattern trong `src/ItTms.Domain/States/`:
  - `ITicketState` interface & `TicketStateBase` abstract class
  - 7 Concrete States: `NewState`, `AssignedState`, `InProgressState`, `WaitingState`, `ResolvedState`, `ClosedState`, `RejectedState`
- Trả về Railway-oriented `Result<T>` và `Result` kết hợp `Error`, tuyệt đối zero exceptions cho business logic
- Thuộc tính `CanComment` (trả về false cho Resolved, Closed, Rejected)
- Tích hợp chặt chẽ với thực thể `Ticket` (phục hồi state từ StatusId, ủy quyền phương thức nghiệp vụ, phát domain events)
- Viết bản thiết kế chi tiết vào `state_pattern_plan.md` và bàn giao `handoff.md`
- Ngôn ngữ: Tiếng Việt (giữ nguyên mã định danh C# / technical terms)

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `ORIGINAL_REQUEST.md`, `PROJECT.md`, `spec_inventory.md`
  - `src/ItTms.Domain/Common/Result.cs`, `Error.cs`, `ErrorType.cs`
  - `src/ItTms.Domain/Entities/Ticket.cs`, `TicketStatus.cs`, `User.cs`, `Feedback.cs`
  - `src/ItTms.Domain/Events/*`
  - `tests/ItTms.Tests/UnitTests/Domain/TicketInvariantTests.cs`
- **Key findings**:
  - Toàn bộ 109 bài test hiện tại trong `ItTms.Tests` chạy pass 100%.
  - `Ticket.cs` cần refactor loại bỏ hoàn toàn các cấu trúc rẽ nhánh if/else phân tán, chuyển giao sang State Pattern.
  - Hoàn tất tài liệu thiết kế kiến trúc `state_pattern_plan.md` với đầy đủ mã nguồn C#, ma trận chuyển đổi 7x8, danh mục mã lỗi `TicketErrors`, cơ chế khôi phục từ SQLite qua `TicketStateFactory`, và kế hoạch kiểm thử xUnit đối chiếu Bảng 7.2.
- **Unexplored areas**:
  - Việc triển khai mã nguồn thực tế trong `src/ItTms.Domain/States/` sẽ do `implementer_m2_1` phụ trách theo đúng vai trò.

## Key Decisions Made
- Thiết kế State Pattern dạng Context-in-State (`TicketStateBase(Ticket ticket)`) cho phép State truy cập trực tiếp các trường dữ liệu và kích hoạt `TransitionTo` nội bộ, thỏa mãn 100% chữ ký phương thức không cần truyền `Ticket` từ ngoài vào.
- Thiết kế bộ mã lỗi `TicketErrors` trả về Railway-oriented `Result` (`INVALID_STATE_TRANSITION`, `CANNOT_REJECT_IN_PROGRESS`, `COMMENTS_BLOCKED_ON_RESOLVED`, `TICKET_ALREADY_CLOSED`, `TICKET_ALREADY_REJECTED`, `REJECTION_REASON_REQUIRED`, `REOPEN_REASON_REQUIRED`, `UNAUTHORIZED_STAFF`, `UNAUTHORIZED_USER`, `FEEDBACK_REQUIRED`).
- Tích hợp phương thức ủy quyền và nạp chồng (overload) trên `Ticket.cs` để vừa hỗ trợ Domain Entities (`User`, `Feedback`), vừa duy trì tương thích ngược 100% với các test hiện có (`int staffId`, `int leaderId`).

## Artifact Index
- `c:\IT_System\.agents\teamwork\explorer_m2_1\state_pattern_plan.md` — Bản thiết kế chi tiết kiến trúc State Pattern
- `c:\IT_System\.agents\teamwork\explorer_m2_1\handoff.md` — Báo cáo nghiệm thu và bàn giao 5 thành phần
- `c:\IT_System\.agents\teamwork\explorer_m2_1\progress.md` — Nhật ký tiến độ và heartbeat
