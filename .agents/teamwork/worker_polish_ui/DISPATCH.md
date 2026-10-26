## 2026-10-25T15:01:08Z
Bạn là Worker chuyên trách thực thi tinh chỉnh giao diện & kiểm thử nghiệm thu toàn diện hệ thống IT-TMS (.NET 9).
Thư mục làm việc của bạn: c:\IT_System\.agents\teamwork\worker_polish_ui

Tài liệu tham chiếu:
- Yêu cầu gốc: c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md (mục ## 2026-10-25T14:44:33Z)
- Báo cáo Explorer 3: c:\IT_System\.agents\teamwork\survey_explorer_ui_ux\handoff.md
- Báo cáo Explorer 1: c:\IT_System\.agents\teamwork\survey_explorer_build_tests\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. An auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Nhiệm vụ:
1. File bạn được sở hữu chỉnh sửa: `src/ItTms.Web/wwwroot/css/site.css`.
   Bổ sung quy tắc CSS cho `.table > tbody > tr.table-light > td` để đảm bảo trong Dark Mode các hàng đề mục của bảng phân quyền (như tại `Areas/Admin/Views/Role/Index.cshtml`) có màu nền `var(--subtle) !important` và màu chữ `var(--text) !important`, không bị màu trắng sáng chói cục bộ.
2. Kiểm tra xem tiến trình `ItTms.Web` có đang chạy chiếm file DLL không (nếu có thì tắt/dừng tiến trình trước khi build).
3. Chạy `dotnet build c:\IT_System\ItTms.sln` - bảo đảm 0 Warning(s), 0 Error(s).
4. Chạy `dotnet test c:\IT_System\ItTms.sln` - bảo đảm 342/342 test cases PASS (100%), bao gồm trọn vẹn 10/10 kịch bản `Table72VerificationTests`.
5. Cập nhật `progress.md` và ghi báo cáo chi tiết vào `handoff.md` trong thư mục làm việc của bạn.
6. Khi hoàn tất, gửi tin nhắn thông báo cho parent.
