# BRIEFING — 2026-10-25T08:08:00Z

## Mission
Thiết kế chi tiết C# Domain Entities (8 thực thể) và cấu hình EF Core 9 Fluent API tối ưu cho SQLite, concurrency control, SLA DueAt và covering indexes.

## 🔒 My Identity
- Archetype: explorer
- Roles: explorer, domain-designer
- Working directory: c:\IT_System\.agents\teamwork\explorer_m1_2
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M1 — Project Foundation & Domain Schema

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Design 8 Core Entities in src/ItTms.Domain (User, TicketStatus, Ticket, Comment, Feedback, Attachment, Notification, SystemLog)
- Design EF Core 9 Fluent API configurations in src/ItTms.Infrastructure
- Handle SQLite ConcurrencyToken compatibility (Guid application-managed / byte[] RowVersion)
- SLA DueAt calculation per priority & timestamps (CreatedAt, DueAt, ResolvedAt, ClosedAt)
- Covering indexes design for query performance
- Language: Tiếng Việt

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T08:08:00Z

## Investigation State
- **Explored paths**:
  - `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
  - `c:\IT_System\PROJECT.md`
  - `c:\IT_System\PROJECT_MEMORY.md`
  - `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`
  - `c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md`
  - `c:\IT_System\.agents\skills\backend\dotnet-clean-architecture\SKILL.md`
  - `c:\IT_System\.agents\skills\backend\efcore-database-performance\SKILL.md`
- **Key findings**:
  - Hoàn thiện định nghĩa mã C# cho 8 Core Entities với Rich Domain Model, Result<T>, private setters và factory methods.
  - Phân tích và đưa ra giải pháp SQLite Concurrency Token: do SQLite không có native auto-increment rowversion, kết hợp `byte[] RowVersion` (16 bytes) cùng `ConcurrencyTokenInterceptor` cập nhật token mới khi entity có trạng thái `Modified`.
  - Chuẩn hóa công thức SLA DueAt theo Priority: Critical (1h), High (4h), Medium (24h), Low (72h) tính trực tiếp từ CreatedAt.
  - Thiết kế 4 Covering & Composite Indexes chính trên bảng Tickets và các bảng liên quan phục vụ 20 Use Cases mà không gây Write Amplification.
- **Unexplored areas**:
  - Tầng State Machine cụ thể (7 concrete state classes) thuộc phạm vi của M2 (sẽ được implement chi tiết tiếp theo).

## Key Decisions Made
- `Ticket.RowVersion` sử dụng kiểu `byte[]` 16-byte (chuyển đổi từ `Guid.NewGuid().ToByteArray()`), được EF Core đánh dấu `.IsRowVersion().IsConcurrencyToken()`.
- Tạo `ConcurrencyTokenInterceptor` để tự động đổi token mới khi update trên SQLite.
- Tách biệt rõ ràng 8 file thực thể trong `ItTms.Domain/Entities` và 8 file cấu hình `IEntityTypeConfiguration<T>` trong `ItTms.Infrastructure/Persistence/Configurations`.

## Artifact Index
- `c:\IT_System\.agents\teamwork\explorer_m1_2\domain_schema_plan.md` — Báo cáo thiết kế chi tiết C# Entities, EF Core 9 Fluent API, Concurrency Token, SLA DueAt và Covering Indexes.
- `c:\IT_System\.agents\teamwork\explorer_m1_2\handoff.md` — Báo cáo bàn giao 5 phần theo Handoff Protocol.
- `c:\IT_System\.agents\teamwork\explorer_m1_2\progress.md` — Liveness heartbeat.
- `c:\IT_System\.agents\teamwork\explorer_m1_2\DISPATCH.md` — Lịch sử chỉ thị công việc.
