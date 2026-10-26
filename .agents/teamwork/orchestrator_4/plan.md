# MASTER PLAN: ĐỒNG BỘ TOÀN DIỆN TÀI LIỆU WORD IT-TMS (.NET 9)

## 1. Mục tiêu và phạm vi
Đồng bộ hóa tuyệt đối nội dung tài liệu báo cáo kỹ thuật phần mềm hướng đối tượng IT-TMS (.NET 9 ASP.NET Core MVC) trên cả 2 tài liệu Word (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`), đảm bảo:
- **R1**: Bảng phân công công việc Phụ lục 1 (Bảng PL.1) tuân thủ 100% 4 Vertical Slices độc lập (Thụ: Staff/State, Tiến: Leader/Factory-Flyweight, Tình: User/Observer-SignalR, Danh: Admin-Auth/Decorator-Singleton). Loại bỏ devops/unit-test ngoài lề. Bảo toàn 149/149 headings.
- **R2**: Thiết kế mới và chèn đầy đủ 6 sơ đồ đang thiếu: Hình 3.1 (ERD 8 bảng), Hình 5.1 (BPMN Ticket Lifecycle), Hình 5.2 (State Machine 7 trạng thái), Hình 5.3 (Class Diagram tổng quát), Hình 5.6 (Sequence UC09 Phân công ticket), Hình 6.1 (Class Diagram State Pattern).
- **R3**: Hiện đại hóa và đồng bộ toàn bộ sơ đồ Use Case (Hình 2.1-2.5) và Sequence (Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12) sang chuẩn kiến trúc .NET 9 (đúng class, controller, method, SignalR `TicketHub`).
- Đảm bảo toàn bộ 41 hình ảnh trong tài liệu đồng bộ hoàn hảo, sắc nét, thẩm mỹ cao.
- Kiểm tra tính toàn vẹn XML, 0 lỗi corrupt, 149/149 heading count.

## 2. Kế hoạch phân kỳ thực hiện
### Phase 0: Survey & Codebase/Document Mapping (Khảo sát)
- Explorer 1: Khảo sát chi tiết codebase .NET 9 hiện tại (`Models/Entities`, `Controllers/`, `Services/`, `Hubs/TicketHub.cs`, `Views/`). Liệt kê chính xác tên class, method, endpoint, 8 bảng DB, 7 trạng thái ticket.
- Explorer 2: Khảo sát tài liệu Word hiện tại (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`). Trích xuất mục lục 149 headings, bảng PL.1 hiện tại, vị trí và caption của tất cả 41 hình ảnh.
- Explorer 3: Khảo sát công cụ sinh sơ đồ và tài nguyên đồ họa (PlantUML, Python script, python-docx, mermaid-cli, graphviz) sẵn có trong môi trường để đảm bảo render ảnh độ phân giải cao (300 DPI, nền sáng).

### Phase 1: Chuẩn hóa Bảng phân công PL.1 (Milestone 1)
- Triển khai Worker cập nhật Bảng PL.1 trong cả 2 tài liệu Word.
- Reviewer & Challenger kiểm tra: cấu trúc 4 vertical slices, xóa devops/unit test, đếm 149 headings, so sánh XML schema.

### Phase 2: Thiết kế mới 6 sơ đồ kỹ thuật thiếu (Milestone 2)
- Worker thiết kế mã nguồn sơ đồ (PlantUML/Python) và render thành ảnh PNG sắc nét (Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1).
- Reviewer & Challenger kiểm tra tính chính xác kỹ thuật với code C# .NET 9.

### Phase 3: Hiện đại hóa các sơ đồ Use Case & Sequence cũ (Milestone 3)
- Worker tạo mã nguồn và render toàn bộ các sơ đồ Use Case (Hình 2.1-2.5) và Sequence (Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12).
- Reviewer & Challenger kiểm tra sự ăn khớp tuyệt đối giữa sơ đồ và implementation C# .NET 9.

### Phase 4: Nhúng hình ảnh vào Word & Định dạng toàn diện (Milestone 4)
- Worker thay thế/chèn toàn bộ 41 hình ảnh mới vào đúng vị trí caption trong cả 2 tài liệu Word.
- Căn chỉnh lề, kích thước ảnh, giữ nguyên định dạng caption và style Word.

### Phase 5: Verification, XML Integrity & Forensic Audit (Milestone 5)
- Reviewer kiểm tra số lượng 41 hình ảnh, 149 headings.
- Challenger kiểm tra mở file Word không báo lỗi XML/corrupt.
- Auditor thực hiện kiểm tra liêm chính (không dùng ảnh giả/placeholder, không làm hỏng văn bản, không hardcode ảo).
- Báo cáo Sentinel.
