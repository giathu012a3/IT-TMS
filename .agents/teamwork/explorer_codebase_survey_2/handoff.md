# 🤝 HANDOFF REPORT — CODEBASE & ARCHITECTURE SURVEY
**Agent**: `explorer_codebase_survey_2`  
**Recipient**: `orchestrator_1` (Conversation ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
**Task**: Codebase & Architecture Investigation for IT-TMS (.NET 9)  
**Timestamp**: 2026-10-25T08:02:00Z  
**Handoff Type**: Hard (Task Complete)  

---

## 1. OBSERVATION (QUAN SÁT TRỰC TIẾP)

1. **Khảo sát cây thư mục hệ thống tại `c:\IT_System`**:
   - Chạy lệnh `list_dir(c:\IT_System)`:
     - Chỉ có 1 thư mục con `.agents` và 4 file: `AGENTS.md` (771 bytes), `BaoCao_ITTMS_TKPMHDT (4).docx` (2,399,463 bytes), `PROJECT_MEMORY.md` (4,653 bytes), `extracted_doc_text.txt` (104,177 bytes).
   - Chạy lệnh `find_by_name(SearchDirectory="c:\IT_System", Pattern="*.sln")` -> `Found 0 results`.
   - Chạy lệnh `find_by_name(SearchDirectory="c:\IT_System", Pattern="*.csproj")` -> `Found 0 results`.
   - Chạy lệnh `find_by_name(SearchDirectory="c:\IT_System", Pattern="*.cs")` -> `Found 0 results`.
   - Chạy lệnh `find_by_name(SearchDirectory="c:\IT_System", Pattern="*.py")` -> `Found 0 results`.
   - Chạy lệnh `find_by_name(SearchDirectory="c:\IT_System", Pattern="*.db")` -> `Found 0 results`.
   - Chạy lệnh `git status` -> `fatal: not a git repository (or any of the parent directories): .git`.

2. **Khảo sát môi trường runtime .NET SDK**:
   - Chạy lệnh `dotnet --info`:
     - `.NET SDK: Version 9.0.317 [C:\Program Files\dotnet\sdk]`.
     - `OS Name: Windows, OS Version: 10.0.26200, RID: win-x64`.
     - `.NET runtimes installed`:
       - `Microsoft.AspNetCore.App 9.0.19`
       - `Microsoft.NETCore.App 9.0.19`
       - `Microsoft.WindowsDesktop.App 9.0.19`

3. **Khảo sát tài liệu di sản đồ án (`extracted_doc_text.txt`)**:
   - Dòng 8-9: Đề tài môn Thiết kế Phần mềm Hướng Đối tượng (TKPMHDT) - HCMUTE: "HỆ THỐNG QUẢN LÝ TICKET YÊU CẦU HỖ TRỢ KỸ THUẬT TỪ NGƯỜI DÙNG (IT Ticket Management System – IT-TMS)".
   - Dòng 59-220: Mục lục đầy đủ 7 chương, 20 Use Cases (UC01 đến UC20), 8 bảng CSDL, 4 mẫu thiết kế (State, Observer, Decorator, Singleton).
   - Dòng 1581: Hiện trạng cũ sử dụng Polling 3s: `"khung trao đổi cập nhật bằng polling (/ticket/<id>/comments/poll)"`.
   - Dòng 1615-1659: Bảng 7.2 gồm 10 kịch bản kiểm thử chính (Đăng nhập tài khoản vô hiệu hóa, 403 Forbidden trái quyền, tạo ticket New gửi Leader, phân công Assigned gửi Staff & User, chặn chuyển sai trạng thái, chặn Leader reject InProgress, chặn Staff xem ticket người khác, chặn comment ticket Resolved, đóng & rating ticket Resolved, Admin không thể tự vô hiệu hóa).

4. **Khảo sát bộ quy tắc và kỹ năng chuyên sâu (`c:\IT_System\.agents`)**:
   - Kỹ năng `.agents/skills/backend/dotnet-clean-architecture/SKILL.md`: Chuẩn kiến trúc Pragmatic Clean Architecture kết hợp Vertical Slices, loại bỏ Generic Repository, áp dụng `Result<T>` thay vì throw exception, Outbox Pattern.
   - Kỹ năng `.agents/skills/backend/efcore-database-performance/SKILL.md`: Concurrency Token (`RowVersion`), Covering Indexes, Keyset Pagination, Compiled Queries.
   - Kỹ năng `.agents/skills/backend/backend-security-owasp/SKILL.md`: Chống BOLA/IDOR, RBAC, kiểm tra trạng thái tài khoản.
   - Kỹ năng `.agents/skills/testing-qa/unit-testing-tdd/SKILL.md`: Classical testing, zero-mock domain invariants, TimeProvider.

---

## 2. LOGIC CHAIN (CHUỖI LẬP LUẬN LOGIC)

1. **Từ Quan sát 1 & 2**:
   - `c:\IT_System` hoàn toàn chưa có bất kỳ file giải pháp hoặc mã nguồn .NET nào (0 file `.sln`, `.csproj`, `.cs`).
   - Môi trường máy chủ đã cài đặt sẵn .NET SDK `9.0.317` và ASP.NET Core Runtime `9.0.19`.
   - $\rightarrow$ **Suy luận**: Dự án ở trạng thái Greenfield thuần túy. Toàn bộ kiến trúc và mã nguồn phải được xây dựng mới từ đầu. Không có xung đột với mã nguồn cũ và không có nợ kỹ thuật (technical debt) từ codebase trước đó.

2. **Từ Quan sát 3**:
   - Đồ án gốc trong báo cáo Word được viết bằng Python/Flask và sử dụng Polling 3s.
   - User request và `PROJECT_MEMORY.md` đặt ra yêu cầu bất biến (Hard Invariant): Chuyển đổi toàn diện sang C# .NET 9 Clean Architecture + SignalR WebSockets, đồng thời giữ nguyên 100% cấu trúc tiêu đề các chương mục trong `BaoCao_ITTMS_TKPMHDT (4).docx`.
   - Bảng 7.2 chứa 10 kịch bản kiểm thử then chốt làm cơ sở nghiệm thu cho Phase A.
   - $\rightarrow$ **Suy luận**: Cần phân chia nhiệm vụ thành 2 pha tuần tự chặt chẽ:
     - **Phase A**: Triển khai giải pháp .NET 9 hoàn chỉnh, chạy sạch build 0 cảnh báo/0 lỗi, vượt qua 100% 10 kịch bản Bảng 7.2.
     - **Phase B**: Đồng bộ hóa file báo cáo docx, thay thế các đoạn code Python và bảng biểu bằng C# .NET 9 và kết quả thực nghiệm thực tế đã kiểm chứng.

3. **Từ Quan sát 4 kết hợp Yêu cầu bài toán**:
   - Để thỏa mãn 20 Use Cases, 8 bảng CSDL, 4 Design Patterns, SignalR và bảo mật chống BOLA:
     - Giải pháp tối ưu nhất là mô hình Clean Architecture gồm 5 dự án: `ItTms.Domain` (chứa 8 Entities, State Pattern với 7 states, Result<T>, Domain Events), `ItTms.Application` (Interfaces, DTOs, Observers), `ItTms.Infrastructure` (EF Core DbContext, SQLite migration, Seeder, PasswordHasher), `ItTms.Web` (5 Areas MVC: Auth, User, Staff, Leader, Admin, SignalR `TicketHub`, `AuthorizeRolesAttribute`, Bootstrap 5), `ItTms.UnitTests` (xUnit tests cho State Machine và 10 kịch bản Bảng 7.2).
   - $\rightarrow$ **Suy luận**: Thiết kế chi tiết này đã được đặc tả hoàn chỉnh và lưu tại `codebase_assessment.md`.

---

## 3. CAVEATS (CÁC ĐIỂM CẦN LƯU Ý & GIẢ ĐỊNH)

1. **Giả định về Git**: Thư mục `c:\IT_System` hiện chưa phải là git repository (`git status` báo fatal). Trong quá trình triển khai, nếu Orchestrator muốn quản lý phiên bản cục bộ, có thể khởi tạo `git init` nếu cần.
2. **SignalR phía Client**: Trình duyệt client cần thư viện JavaScript `@microsoft/signalr`. Do máy chủ có thể hoạt động trong môi trường mạng nội bộ hoặc không có npm trực tiếp, script `signalr.min.js` nên được cung cấp sẵn trong `wwwroot/lib/signalr/` của dự án Web để đảm bảo khả năng chạy offline không phụ thuộc CDN bên ngoài.
3. **EF Core SQLite Concurrency**: SQLite xử lý concurrency bằng file lock; để kiểm thử tính năng Optimistic Concurrency Token (`RowVersion`), cấu hình Fluent API cần sử dụng cột `byte[] RowVersion` hoặc triggers/version counter tương thích hoàn toàn với SQLite provider.

---

## 4. CONCLUSION (KẾT LUẬN CUỐI CÙNG)

1. Hiện trạng codebase tại `c:\IT_System` là **Greenfield**, sẵn sàng để bắt đầu triển khai ngay từ Milestone 1 (Khởi tạo Solution & Domain Core Foundation).
2. Nền tảng kỹ thuật mục tiêu đã được xác định rõ ràng, chặt chẽ:
   - Target Framework: **.NET 9 (`net9.0`)**.
   - Cấu trúc: **5 projects Clean Architecture (`ItTms.sln`)**.
   - Database: **EF Core 9 SQLite** với 8 bảng thực thể, Concurrency Token, Covering Indexes, SLA tracking và Auto-seeder.
   - Giao diện: **ASP.NET Core MVC với 5 Areas** (`Auth`, `User`, `Staff`, `Leader`, `Admin`) trên nền Bootstrap 5, bao phủ trọn vẹn 20 Use Cases.
   - Giao tiếp thời gian thực: **SignalR WebSocket (`TicketHub`)** tại `/hubs/ticket`.
   - Mẫu thiết kế: **State Pattern (7 states với Result<T>)**, **Observer Pattern (Domain Events)**, **Decorator (`AuthorizeRolesAttribute`)**, **Singleton (DI Services)**.
   - Nghiệm thu: **Bộ xUnit tests vượt qua 10 kịch bản kiểm thử trong Bảng 7.2**.
3. Toàn bộ phân tích sâu rộng đã được lập thành tài liệu chuẩn hóa tại:
   `c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md`.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

Để kiểm chứng độc lập các quan sát và kết luận trong báo cáo này:

1. **Kiểm chứng trạng thái Greenfield**:
   - Chạy lệnh PowerShell:
     ```powershell
     Get-ChildItem -Path c:\IT_System -Recurse -Filter *.sln
     Get-ChildItem -Path c:\IT_System -Recurse -Filter *.csproj
     ```
     -> Kết quả xác nhận: Không tìm thấy bất kỳ file `.sln` hay `.csproj` nào.
2. **Kiểm chứng môi trường .NET SDK**:
   - Chạy lệnh:
     ```powershell
     dotnet --version
     ```
     -> Kết quả xác nhận: `9.0.317`.
3. **Kiểm tra tài liệu đánh giá**:
   - Đọc trực tiếp file báo cáo đánh giá hiện trạng:
     `c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md`.
