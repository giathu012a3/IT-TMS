# BRIEFING — 2026-10-25T15:00:00Z

## Mission
Khảo sát, trích xuất và lập danh mục đặc tả toàn diện (Specification Inventory) cho hệ thống IT-TMS (.NET 9) theo Clean Architecture và Design Patterns từ các tài liệu đặc tả chuẩn xác.

## 🔒 My Identity
- Archetype: specification_miner
- Roles: Teamwork specialist, external domain expert
- Working directory: c:\IT_System\.agents\teamwork\spec_miner_survey_1
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M1 — Exploration & Specification Survey

## 🔒 Key Constraints
- Ngôn ngữ: Tiếng Việt (Vietnamese) là ngôn ngữ giao tiếp chính, giữ nguyên thuật ngữ kỹ thuật và mã định danh code.
- Áp dụng nhân nhận thức `#Argon-Omega`: Mật độ thông tin cao, không lan man, thiết kế chuẩn production.
- Vai trò Specification Miner: Chỉ đọc (Read-only), không sinh code triển khai dự án, chỉ phát hiện và lập tài liệu đặc tả chi tiết.
- Real-time SignalR WebSocket Hub (TicketHub) cho bình luận ticket và thông báo (xóa bỏ triệt để cơ chế Polling 3s).
- Bảo toàn 100% mục lục, thứ tự tiêu đề và các bảng/hình trong `BaoCao_ITTMS_TKPMHDT (4).docx`.
- Duy trì Single Source of Truth tại `PROJECT_MEMORY.md`.
- Tuân thủ bộ kỹ năng tại `.agents/skills/` (`dotnet-clean-architecture`, `efcore-database-performance`, `backend-security-owasp`, `unit-testing-tdd`, `rest-api-design-standards`).

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: not yet

## Task Summary
- **What to build**: Trích xuất toàn bộ kho đặc tả hệ thống vào `spec_inventory.md` và lập báo cáo `handoff.md`.
- **Success criteria**: Lập danh mục đầy đủ 20 Use Cases (5 Areas), 8 Core Entities (EF Core 9, Concurrency Token, SLA DueAt/ResolvedAt), 4 Design Patterns (State 7 states + Result<T>, Observer, Decorator/ActionFilter, Singleton), SignalR TicketHub, 10 Kịch bản kiểm thử Bảng 7.2, và yêu cầu đồng bộ tài liệu Word.
- **Interface contracts**: `PROJECT_MEMORY.md`, `ORIGINAL_REQUEST.md`, `AGENTS.md`, `extracted_doc_text.txt`.
- **Code layout**: .NET 9 Clean Architecture + Vertical Slices (`ItTms.Domain`, `ItTms.Application`, `ItTms.Infrastructure`, `ItTms.Web`, `ItTms.UnitTests`).

## Loaded Skills
- **Source**: `c:\IT_System\.agents\skills\backend\dotnet-clean-architecture\SKILL.md`
  - **Local copy**: `.agents/skills/backend/dotnet-clean-architecture/SKILL.md`
  - **Core methodology**: Clean Architecture kết hợp Vertical Slices, Result<T> Railway-oriented handling, Domain Models giàu nghiệp vụ, không bọc Generic Repository.
- **Source**: `c:\IT_System\.agents\skills\backend\efcore-database-performance\SKILL.md`
  - **Local copy**: `.agents/skills/backend/efcore-database-performance/SKILL.md`
  - **Core methodology**: Tối ưu EF Core 9, Keyset pagination, Compiled queries, Optimistic Concurrency Token, Covering Indexes.
- **Source**: `c:\IT_System\.agents\skills\backend\backend-security-owasp\SKILL.md`
  - **Local copy**: `.agents/skills/backend/backend-security-owasp/SKILL.md`
  - **Core methodology**: Phòng thủ OWASP API Security Top 10, RBAC, Password Hashing, Query Filters.
- **Source**: `c:\IT_System\.agents\skills\testing-qa\unit-testing-tdd\SKILL.md`
  - **Local copy**: `.agents/skills/testing-qa\unit-testing-tdd\SKILL.md`
  - **Core methodology**: Classical testing, kiểm thử bất biến State Machine, không lạm dụng mock ở tầng Domain.

## Key Decisions Made
- [2026-10-25] Khảo sát triệt để `extracted_doc_text.txt` (báo cáo Word gốc 1.709 dòng).
- [2026-10-25] Lập bảng ánh xạ 20 Use Case tương ứng chính xác với 5 Areas của ASP.NET Core MVC (`Areas/Auth`, `Areas/User`, `Areas/Staff`, `Areas/Leader`, `Areas/Admin`).
- [2026-10-25] Tái thiết kế 8 thực thể dữ liệu EF Core 9 với SQLite: bổ sung `ConcurrencyToken` (byte[]/timestamp), `DueAt`, `ResolvedAt`, `FileSize`, `ContentType`, và các composite index.
- [2026-10-25] Đặc tả State Pattern với 7 concrete states kế thừa `TicketStateBase`, các method trả về `Result<T>` không ném Exception.
- [2026-10-25] Định nghĩa chi tiết WebSocket Hub `TicketHub` loại bỏ Polling 3s.
- [2026-10-25] Đặc tả 10 test case trong Bảng 7.2 phục vụ kiểm thử nghiệm thu tự động.

## Artifact Index
- `spec_inventory.md` — Authoritative specification inventory
- `handoff.md` — 5-component handoff report
- `progress.md` — Liveness & progress log
