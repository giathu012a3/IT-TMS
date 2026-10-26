# Dispatch Instructions: challenger_m3_2

## Objective
Kiểm thử đối kháng và kiểm chứng thực nghiệm (Adversarial Testing) đối với 10 kịch bản kiểm nghiệm Bảng 7.2 (TC-01 đến TC-10) thông qua tầng Controller / MVC Endpoints và Security Filters.

## Input Files
- `c:\IT_System\ORIGINAL_REQUEST.md` (BẮT BUỘC ĐỌC)
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m3\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m3\handoff.md`

## Working Directory
`c:\IT_System\.agents\teamwork\challenger_m3_2\`

## Requirements & Scope
1. Viết bộ kiểm thử tích hợp chuyên sâu (Integration/Controller Tests) cho 10 kịch bản Bảng 7.2:
   - TC-01: Tài khoản bị vô hiệu hóa (`IsActive = false`) đăng nhập -> Bị chặn, redirect về Login báo lỗi.
   - TC-02: User truy cập route của Leader -> Bị từ chối (403 Forbidden / AccessDenied).
   - TC-03: User tạo Ticket -> Tạo thành công trạng thái New, tính đúng hạn cam kết SLA `DueAt`, phát thông báo tới Leader.
   - TC-04: Leader phân công Ticket cho Staff -> Chuyển trạng thái Assigned, phát thông báo cho Staff và User.
   - TC-05: Staff cố tình đóng Ticket ở trạng thái New -> Bị từ chối bởi State Pattern.
   - TC-06: Leader cố tình từ chối Ticket đang ở trạng thái InProgress -> Bị từ chối bởi State Pattern.
   - TC-07: Staff truy cập Ticket không thuộc phân công của mình -> Bị chặn bởi `AuthorizeTicketAccessAttribute` (403).
   - TC-08: Thêm bình luận vào Ticket đã Resolved -> Bị chặn bởi `CanComment == false`.
   - TC-09: Đóng Ticket Resolved kèm đánh giá Feedback -> Trạng thái Closed, lưu rating và ghi nhật ký hệ thống.
   - TC-10: Quản trị viên tự vô hiệu hóa tài khoản chính mình -> Bị chặn bởi logic nghiệp vụ.
2. Chạy `dotnet test c:\IT_System\ItTms.sln` và đảm bảo toàn bộ tests đều pass, build 0 warnings/0 errors.
3. Ghi nhận `handoff.md` kèm phán quyết: `APPROVE` hoặc `REQUEST_CHANGES`.
4. Gửi thông điệp kết quả về cho orchestrator.

## 2026-10-25T10:29:10Z
You are challenger_m3_2. Your working directory is c:\IT_System\.agents\teamwork\challenger_m3_2\.
FIRST: Read c:\IT_System\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\challenger_m3_2\DISPATCH.md, and c:\IT_System\.agents\teamwork\worker_m3\handoff.md.
Conduct empirical adversarial verification on the 10 scenarios from Table 7.2 (TC-01 to TC-10) through MVC Controllers and Security Filters:
- TC-01: Deactivated user login blocked.
- TC-02: User access Leader route blocked (403).
- TC-03: User creates Ticket -> New state, SLA DueAt calculated, notification to Leader.
- TC-04: Leader assigns Ticket -> Assigned state, notifications to Staff and User.
- TC-05: Staff closes New Ticket -> Rejected by State Pattern.
- TC-06: Leader rejects InProgress Ticket -> Rejected by State Pattern.
- TC-07: Staff opens other's ticket -> Rejected by AuthorizeTicketAccessAttribute (403).
- TC-08: Comment on Resolved ticket -> Rejected by CanComment == false.
- TC-09: Close Resolved ticket with feedback -> Closed state, rating saved, audit logged.
- TC-10: Admin self-deactivation -> Blocked by domain logic.
Write and execute comprehensive integration tests in `tests/ItTms.Tests/Controllers/`.
Ensure `dotnet build c:\IT_System\ItTms.sln` (0 warnings, 0 errors) and `dotnet test c:\IT_System\ItTms.sln` pass.
Write handoff.md with explicit verdict (APPROVE or REQUEST_CHANGES). Send completion message back to orchestrator.
