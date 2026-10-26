# BÁO CÁO BÀN GIAO ĐẶC TẢ HỆ THỐNG (HANDOFF REPORT)
> **Agent**: `spec_miner_survey_1`  
> **Nhiệm vụ**: Khảo sát và lập danh mục đặc tả toàn diện (Specification & Requirements Mining)  
> **Thư mục làm việc**: `c:\IT_System\.agents\teamwork\spec_miner_survey_1`  
> **Thời điểm**: 2026-10-25T15:05:00Z  
> **Người nhận**: `orchestrator_1` (ID: `9e3cad2c-3e7f-49c0-9814-015216856d2e`)  

---

## 1. Quan sát Thực tế (Observation)

1. **Văn bản Yêu cầu Gốc (`ORIGINAL_REQUEST.md`)**:
   - Yêu cầu xây dựng hệ thống quản lý ticket chuẩn doanh nghiệp IT-TMS bằng **.NET 9**, kiến trúc Clean Architecture kết hợp Design Patterns, SignalR WebSocket thay thế Polling, cơ sở dữ liệu SQLite 8 bảng qua EF Core 9, giao diện Bootstrap 5, và đồng bộ báo cáo Word giữ nguyên 100% mục lục (Dòng 5, 14-32).
   - Xác định rõ 5 tiêu chí nghiệm thu chính: Biên dịch 0 cảnh báo/lỗi, tự động seed tài khoản 4 vai trò, vượt qua **10 kịch bản kiểm thử trong Bảng 7.2**, SignalR TicketHub không tải lại trang, và bảo toàn 100% cấu trúc đề mục báo cáo (Dòng 37-62).
2. **Bộ Nhớ Dự Án (`PROJECT_MEMORY.md`)**:
   - Xác lập 5 Invariants bất biến: Tiêu diệt triệt để Polling 3s (`#026 Bottleneck`); Áp dụng Railway-oriented `Result<T>` không ném Exception cho lỗi nghiệp vụ; Đóng gói vòng đời Ticket qua 7 lớp State; Bảo vệ tính nhất quán dữ liệu bằng Concurrency Token (`RowVersion`); Bảo toàn 100% TOC báo cáo Word (Dòng 18-29).
   - Quyết định kiến trúc ADR-001 (SignalR `/hubs/ticket`), ADR-002 (Clean Architecture 5 dự án: `Domain`, `Application`, `Infrastructure`, `Web`, `UnitTests`), ADR-003 (Schema 8 bảng SQLite enterprise) (Dòng 30-34).
3. **Văn bản Báo cáo Đồ án Gốc (`extracted_doc_text.txt`)**:
   - File trích xuất chứa toàn bộ 1.709 dòng nội dung báo cáo Word môn TKPMHDT của Nhóm 7 (HCMUTE).
   - Mục lục hoàn chỉnh gồm 7 Chương (Dòng 59-195):
     - Chương 1: Khảo sát hiện trạng (Bảng 1.1 - 1.9).
     - Chương 2: Mô hình hóa yêu cầu: 20 Use Cases chi tiết (Dòng 546-881, Bảng 2.2 - 2.21, UC01 đến UC20).
     - Chương 3: Thiết kế dữ liệu: 8 bảng (Dòng 892-1148, Bảng 3.1 - 3.8: `User`, `SystemLog`, `Ticket`, `TicketStatus`, `Attachment`, `Comment`, `Feedback`, `Notification`).
     - Chương 4: Thiết kế giao diện (Hình 4.1 - 4.22).
     - Chương 5: Thiết kế xử lý: Vòng đời 7 trạng thái, 9 lược đồ tuần tự (Dòng 1259-1348).
     - Chương 6: Áp dụng 5 mẫu thiết kế (Dòng 1349-1575): State (7 states), Observer (`NotificationObserver`, `SystemLogObserver`), Decorator (`role_required`), Singleton (`db`, `login_manager`), MVC.
     - Chương 7: Cài đặt và kết quả thực nghiệm: Bảng 7.1 (Công nghệ), Bảng 7.2 (10 kịch bản kiểm thử, dòng 1615-1659).
4. **Bộ Kỹ năng Chuẩn mực (`.agents/skills/`)**:
   - `dotnet-clean-architecture`: Cấm generic repository wrapper, dùng `Result<T>`, Outbox/Domain events, Rich Domain Model.
   - `efcore-database-performance`: Keyset pagination, compiled queries, optimistic concurrency token, covering indexes.
   - `backend-security-owasp`: RBAC qua ActionFilter, password hashing, chặn BOLA/IDOR bằng query filter, chống tự khóa tài khoản Admin.
   - `unit-testing-tdd`: Classical Detroit testing, kiểm thử bất biến State Machine, zero mock ở Domain.

---

## 2. Chuỗi Suy luận Logic (Logic Chain)

1. Từ **Quan sát 1 & 3**: Hệ thống có 20 Use Case nghiệp vụ cụ thể gắn liền với 4 vai trò người dùng (User, Staff, Leader, Admin) và 1 vai trò khách (Guest). Trong ASP.NET Core MVC, giải pháp tối ưu là phân bổ 20 Use Case này vào 5 MVC Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) kết hợp các Controller dùng chung cho tính năng chi tiết Ticket và Chat SignalR.
2. Từ **Quan sát 1, 2 & 4**: Để đạt chuẩn sản xuất (production-grade) mà không phụ thuộc hạ tầng phức tạp, CSDL được hiện thực hóa qua SQLite + EF Core 9 với 8 thực thể chuẩn hóa. Các trường bổ sung quan trọng gồm: `RowVersion` (quản lý đồng thời lạc quan), `DueAt` (tính tự động theo SLA: Critical 1h, High 4h, Medium 24h, Low 72h), `ResolvedAt`, `ClosedAt`, và metadata kích thước/MIME type của tệp đính kèm.
3. Từ **Quan sát 2 & 3**: Vòng đời Ticket trong Chương 5 & 6 bao gồm 7 trạng thái: `New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`. Việc cài đặt State Pattern với lớp cơ sở `TicketStateBase` và 7 lớp con cụ thể, kết hợp kiểu trả về `Result<T>` thay cho Exception, thỏa mãn trọn vẹn cả nguyên tắc OOP học thuật lẫn chuẩn hiệu năng cao của `#Argon-Omega`.
4. Từ **Quan sát 1 & 2**: Điểm thắt cổ chai lớn nhất trong báo cáo cũ là Polling 3s trong khung chat và chuông thông báo. Giải pháp dứt điểm là xây dựng SignalR Hub (`TicketHub`) quản lý kết nối theo nhóm `ticket_{id}` và đẩy thông báo thời gian thực trực tiếp tới `Clients.User(userId)`.
5. Từ **Quan sát 1 & 3**: Bảng 7.2 định nghĩa chính xác 10 kịch bản kiểm thử nghiệm thu. Toàn bộ 10 kịch bản này đã được ánh xạ thành ma trận điều kiện biên, thao tác và kết quả kỳ vọng, đóng vai trò là kim chỉ nam cho bộ Unit/Integration Test suite tự động bằng xUnit.
6. Từ **Quan sát 3**: Để bảo toàn tính pháp lý và học thuật của đồ án, toàn bộ hệ thống mục lục 7 chương của `BaoCao_ITTMS_TKPMHDT (4).docx` phải được giữ nguyên vẹn 100% về tiêu đề và số thứ tự; chỉ thay thế phần ruột công nghệ (Python/Flask $\to$ .NET 9/C#), lược đồ và bảng mã nguồn.

---

## 3. Các Giới hạn & Lưu ý (Caveats)

- **Lưu ý 1**: Do SQLite không hỗ trợ kiểu dữ liệu `ROWVERSION` nhị phân tự tăng như SQL Server, EF Core 9 cần sử dụng `SaveChangesInterceptor` để tự động gán token ngẫu nhiên mới (`byte[]` hoặc `Guid`) vào cột `RowVersion` mỗi lần cập nhật `Ticket`.
- **Lưu ý 2**: File báo cáo Word `BaoCao_ITTMS_TKPMHDT (4).docx` là file nhị phân OpenXML. Việc chỉnh sửa tài liệu trong Phase B cần được thực hiện cẩn trọng bằng các thư viện thao tác docx (hoặc script Python `python-docx`) để giữ nguyên định dạng styles, header, footer và hệ thống đánh chỉ mục.
- **Lưu ý 3**: Ngoài 3 lưu ý trên, không có bất kỳ điều kiện mơ hồ nào chưa được làm rõ.

---

## 4. Kết luận (Conclusion)

- Đã hoàn tất việc trích xuất và lập danh mục đặc tả toàn diện tại file:  
  `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`.
- Tài liệu bao gồm đầy đủ và chi tiết:
  1. Toàn bộ 20 Use Cases (UC01 đến UC20) ánh xạ chính xác trên 5 Areas và các vai trò tương ứng.
  2. Toàn bộ 8 thực thể CSDL với kiểu dữ liệu EF Core 9, chỉ mục bao phủ, Concurrency Token và SLA timestamps.
  3. 4 Mẫu thiết kế phần mềm cốt lõi (State Pattern 7 trạng thái với `Result<T>`, Observer Pattern, Decorator/ActionFilter `AuthorizeRolesAttribute`, Singleton Pattern).
  4. Kiến trúc SignalR WebSocket Hub (`TicketHub`) thay thế Polling 3s.
  5. Toàn bộ 10 kịch bản kiểm thử nghiệm thu từ Bảng 7.2.
  6. Các nguyên tắc và danh mục đồng bộ tài liệu Word `BaoCao_ITTMS_TKPMHDT (4).docx` bảo toàn 100% mục lục.
  7. Bảng tổng hợp Features Discovered và Ma trận Edge Cases.
- Đặc tả đã sẵn sàng 100% để đội ngũ kỹ sư tiếp tục triển khai Phase A (Scaffolding Solution, Domain Entities, State Machine, CSDL EF Core 9, SignalR Hub và Test Suite).

---

## 5. Phương pháp Kiểm chứng Độc lập (Verification Method)

1. **Kiểm tra File Đặc tả**:
   - Đọc trực tiếp file `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`.
   - Đối chiếu sự hiện diện của 20 Use Cases (Mục 2), 8 Thực thể (Mục 3), 4 Design Patterns (Mục 4), SignalR Hub (Mục 5), 10 Kịch bản kiểm thử (Mục 6), và Yêu cầu Word (Mục 7).
2. **Kiểm tra Đối chiếu Nguồn Chuẩn**:
   - So khớp 20 Use Cases với dòng 89-109 và 546-881 trong `extracted_doc_text.txt`.
   - So khớp 8 Thực thể với dòng 110-120 và 892-1148 trong `extracted_doc_text.txt`.
   - So khớp 10 Kịch bản kiểm thử với dòng 1615-1659 (Bảng 7.2) trong `extracted_doc_text.txt`.
3. **Điều kiện Hủy bỏ (Invalidation Conditions)**:
   - Nếu phát hiện thiếu bất kỳ Use Case nào trong 20 Use Cases.
   - Nếu phát hiện cấu trúc mục lục báo cáo Word bị thay đổi thứ tự hoặc xóa bỏ tiêu đề.
   - Nếu State Pattern dùng exception thay vì `Result<T>`.
