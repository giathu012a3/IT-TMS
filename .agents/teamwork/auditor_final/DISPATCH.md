## 2026-10-25T15:14:52Z
Bạn là Forensic Integrity Auditor chuyên trách kiểm định liêm chính toàn diện hệ thống IT-TMS (.NET 9) theo tiêu chuẩn Integrity Forensics.
Thư mục làm việc của bạn: c:\IT_System\.agents\teamwork\auditor_final

Tài liệu cần đọc:
- c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md (mục ## 2026-10-25T14:44:33Z)
- c:\IT_System\PROJECT.md
- Toàn bộ báo cáo của Worker và Reviewers:
  * c:\IT_System\.agents\teamwork\worker_polish_ui\handoff.md
  * c:\IT_System\.agents\teamwork\reviewer_audit_1\handoff.md
  * c:\IT_System\.agents\teamwork\reviewer_audit_2\handoff.md

Nhiệm vụ kiểm định liêm chính (Zero Tolerance for Cheating):
1. Quét tĩnh (Static Forensics):
   - Kiểm tra mã nguồn tests tại `tests/ItTms.Tests`: quét tìm bất kỳ cờ skip nào `[Fact(Skip=...]` hoặc `[Theory(Skip=...]`.
   - Quét tìm khẳng định giả tạo (tautologies): `Assert.True(true)`, `Assert.Equal(1, 1)`, `true.Should().BeTrue()`.
   - Quét tìm hardcoded test results, facade logic rỗng trong các Controllers, Entities, State Pattern, Service classes.
   - Quét tệp Word `BaoCao_ITTMS_TKPMHDT_DotNet9.docx` và `BaoCao_ITTMS_TKPMHDT (4).docx`: kiểm tra có synthetic namespace prefixes `ns0:` hoặc `ns1:` không; đếm số headings và TOC entries (phải đủ đúng 373).
2. Kiểm định động (Runtime Tracing):
   - Chạy `dotnet test c:\IT_System\ItTms.sln` và xác minh rằng các bài test thực sự thực thi qua CSDL SQLite in-memory/file, State Pattern và SignalR Hub.
3. Xuất báo cáo kiểm định liêm chính chi tiết vào `handoff.md` trong thư mục làm việc của bạn với phán quyết nhị phân:
   `CLEAN` (nếu không có vi phạm) hoặc `INTEGRITY VIOLATION` (nếu phát hiện gian lận).
4. Gửi tin nhắn thông báo cho parent khi hoàn tất.
