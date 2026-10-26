# Dispatch Instructions: reviewer_m3_1

## Objective
Thẩm định độc lập (Code Review & Verification) tầng Trình diễn ASP.NET Core MVC (5 Areas: Auth, User, Staff, Leader, Admin) trong `src/ItTms.Web/Areas/` và các Controller liên quan, bao phủ trọn vẹn 20 Use Cases (UC01-UC20).

## Input Files
- `c:\IT_System\ORIGINAL_REQUEST.md` (BẮT BUỘC ĐỌC)
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m3\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m3\handoff.md`
- Source files: `src/ItTms.Web/Areas/`, `src/ItTms.Web/Controllers/`, `src/ItTms.Web/Program.cs`

## Working Directory
`c:\IT_System\.agents\teamwork\reviewer_m3_1\`

## Requirements & Scope
1. Kiểm tra 5 Areas: Auth, User, Staff, Leader, Admin.
2. Kiểm tra việc tuân thủ 20 Use Cases, các Attribute phân quyền `AuthorizeRolesAttribute`, `AuthorizeTicketAccessAttribute`, và anti-BOLA/IDOR.
3. Chạy lệnh kiểm tra độc lập:
   - `dotnet build c:\IT_System\ItTms.sln`
   - `dotnet test c:\IT_System\ItTms.sln`
4. Ghi nhận `handoff.md` theo chuẩn 5 phần (Observation, Logic Chain, Caveats, Conclusion, Verification Method) kèm phán quyết rõ ràng: `APPROVE` hoặc `REQUEST_CHANGES`.
5. Gửi thông điệp kết quả về cho orchestrator.


## 2026-10-25T10:29:10Z
You are reviewer_m3_1. Your working directory is c:\IT_System\.agents\teamwork\reviewer_m3_1\.
FIRST: Read c:\IT_System\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\reviewer_m3_1\DISPATCH.md, and c:\IT_System\.agents\teamwork\worker_m3\handoff.md.
Review the 5 Areas MVC controllers, routing, 20 Use Cases (UC01-UC20), anti-BOLA/IDOR protection, and security filters.
Run `dotnet build c:\IT_System\ItTms.sln` and `dotnet test c:\IT_System\ItTms.sln` independently.
Write handoff.md in your working directory with your 5-part report and explicit verdict (APPROVE or REQUEST_CHANGES). Send your completion message back to orchestrator.
