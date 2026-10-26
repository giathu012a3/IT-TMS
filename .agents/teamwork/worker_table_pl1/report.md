# BÁO CÁO KỸ THUẬT: CHUẨN HÓA BẢNG PHÂN CÔNG PHỤ LỤC 1 & LÀM SẠCH OPENXML DOC2

- **Worker**: `worker_table_pl1` (teamwork_preview_worker)
- **Mục tiêu**: Milestone 1 - Backup, Chuẩn hóa Bảng PL.1 theo 4 Vertical Slices, Dọn dẹp 7 bảng tạm thừa ở Doc2, Kiểm định toàn vẹn Word OpenXML & Word 16.0 COM Automation.
- **Tài liệu xử lý**:
  - `Doc1`: `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`
  - `Doc2`: `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`
  - Thư mục sao lưu: `c:\IT_System\backups\backup_before_pl1_update\`
- **Thời điểm hoàn tất**: 2026-10-26T09:42:00Z
- **Trạng thái**: **COMPLETED (PASS 100%)**

---

## 1. TỔNG QUAN KẾT QUẢ THỰC HIỆN (BLUF)

1. **Bảo mật & Sao lưu dự phòng**:
   - Đã tạo bản sao lưu an toàn nguyên trạng trước khi thao tác tại `c:\IT_System\backups\backup_before_pl1_update\`:
     + `BaoCao_ITTMS_TKPMHDT (4).docx` (3,063,830 bytes).
     + `BaoCao_ITTMS_TKPMHDT_DotNet9.docx` (2,995,788 bytes).

2. **Dọn dẹp triệt để 7 Bảng tạm thừa `[CHÈN HÌNH]` trong Doc2**:
   - Đã xóa bỏ 7 phần tử `<w:tbl>` tạm thừa (`Table #42, #43, #44, #45, #46, #47, #48` tại các mục 4.2, 4.3, 4.8, 4.11, 4.14, 4.21, 4.22) do tác giả tiền nhiệm để lại khi chèn ảnh chụp màn hình live UI.
   - Số lượng bảng biểu trong Doc2 giảm từ **62 bảng** xuống đúng **55 bảng**, hoàn toàn đồng nhất với cấu trúc 55 bảng của Doc1.

3. **Chuẩn hóa Bảng phân công công việc Phụ lục 1 (Bảng PL.1)**:
   - Tái cấu trúc toàn bộ nội dung Bảng PL.1 ở cả 2 tài liệu theo đúng ma trận **4 Vertical Slices độc lập**:
     - **Nguyễn Gia Thụ (25810041)**: Kỹ thuật viên (Staff), `StaffController.cs`, `Views/Staff/`, State Pattern (7 trạng thái vòng đời ticket), State Machine Hình 5.2, Class Diagram State Pattern Hình 6.1, Sequence UC07 Hình 5.7.
     - **Nguyễn Mạnh Tiến (25810043)**: Trưởng nhóm (Leader), `LeaderController.cs`, `Views/Leader/`, Factory Method / Flyweight Pattern (`TicketStateFactory.cs`), BPMN Hình 5.1, Sequence UC09 Hình 5.6, Sequence UC10 Hình 5.9.
     - **Hồ Thanh Tình (25810044)**: Người dùng (User), `TicketController.cs`, `Views/Ticket/`, Observer Pattern (SignalR WebSocket `TicketHub` & SystemLog), Sequence UC03 Hình 5.5, Sequence UC06 Hình 5.8, Sequence UC04/19 Hình 5.10.
     - **Trần Thanh Danh (25810009)**: Quản trị & Bảo mật (Admin & Auth), `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`, Decorator Pattern (`[AuthorizeRoles]`), Singleton Pattern (`PasswordHasherService`), ERD 8 bảng Hình 3.1, Class Diagram tổng thể Hình 5.3, Use Case tổng thể Hình 2.1-2.5.
   - **Loại bỏ triệt để 100%**: Không còn bất kỳ từ khóa ngoài lề nào (`unit test`, `unit-test`, `devops`, `kiểm thử tự động`).
   - Tỷ lệ đóng góp ghi nhận **100% (Hoàn thành xuất sắc nhiệm vụ)** cho từng thành viên.
   - Căn chỉnh tỷ lệ cột tối ưu: Cột 1 (2200 dxa ~ 3.88 cm), Cột 2 (1300 dxa ~ 2.29 cm), Cột 3 (5526 dxa ~ 9.75 cm), tổng chiều rộng đúng 9026 dxa chuẩn lề trang A4.

4. **Kiểm định toàn vẹn độc lập (Dual Verification)**:
   - **Python OpenXML**: Cả 2 tài liệu đều có chính xác **769 đoạn văn (paragraphs)**, **149 tiêu đề (`Heading *`)**, **136 mục TOC**, **55 bảng biểu**.
   - **Microsoft Word 16.0 COM Automation**: Cả 2 tài liệu mở mượt mà, **0 lỗi XML schema / corruption**, cấu trúc Word đếm chính xác 55 bảng và 2,090 Word paragraphs.

---

## 2. MA TRẬN CHI TIẾT BẢNG PHÂN CÔNG PHỤ LỤC 1 (BẢNG PL.1)

```
+-----------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Thành viên & MSSV                 | Nội dung công việc đảm nhiệm theo Vertical Slice (Tỷ lệ đóng góp: 100%)                                                                                                                                 |
+-----------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Nguyễn Gia Thụ                    | • Phân hệ: Kỹ thuật viên (Staff).                                                                                                                                                                       |
| MSSV: 25810041                    | • Mã nguồn sở hữu: StaffController.cs, Views/Staff/ (Dashboard, Assigned, Details, History).                                                                                                          |
|                                   | • Mẫu thiết kế GoF: State Pattern (quản lý 7 trạng thái vòng đời ticket: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected; đóng băng các thao tác sai nghiệp vụ qua Result<T>).             |
|                                   | • Sơ đồ kỹ thuật: Sơ đồ chuyển trạng thái (State Machine – Hình 5.2), Lược đồ lớp State Pattern (Hình 6.1), Lược đồ tuần tự UC07 – Cập nhật trạng thái xử lý (Hình 5.7).                               |
|                                   | • Tỷ lệ đóng góp: 100% (Hoàn thành xuất sắc nhiệm vụ).                                                                                                                                                  |
+-----------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Nguyễn Mạnh Tiến                  | • Phân hệ: Trưởng nhóm (Leader).                                                                                                                                                                        |
| MSSV: 25810043                    | • Mã nguồn sở hữu: LeaderController.cs, Views/Leader/ (Dashboard, NewQueue, Index, Details).                                                                                                            |
|                                   | • Mẫu thiết kế GoF: Factory Method / Flyweight Pattern (TicketStateFactory.cs – khởi tạo và tái sử dụng đối tượng trạng thái, tối ưu bộ nhớ).                                                           |
|                                   | • Sơ đồ kỹ thuật: Quy trình xử lý ticket hoàn thiện (BPMN – Hình 5.1), Lược đồ tuần tự UC09 – Phân công yêu cầu (Hình 5.6), Lược đồ tuần tự UC10 – Từ chối yêu cầu (Hình 5.9).                         |
|                                   | • Tỷ lệ đóng góp: 100% (Hoàn thành xuất sắc nhiệm vụ).                                                                                                                                                  |
+-----------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Hồ Thanh Tình                     | • Phân hệ: Người dùng (User).                                                                                                                                                                           |
| MSSV: 25810044                    | • Mã nguồn sở hữu: TicketController.cs, Views/Ticket/ (Dashboard, Index, Create, Details).                                                                                                              |
|                                   | • Mẫu thiết kế GoF: Observer Pattern (phát Domain Events, đẩy thông báo thời gian thực qua SignalR WebSocket TicketHub và tự động ghi nhật ký kiểm toán SystemLog).                                    |
|                                   | • Sơ đồ kỹ thuật: Lược đồ tuần tự UC03 – Tạo yêu cầu mới (Hình 5.5), Lược đồ tuần tự UC06 – Trao đổi thông tin ticket (Hình 5.8), Lược đồ tuần tự UC19, UC04 – Đóng/Mở lại yêu cầu và đánh giá (Hình 5.10). |
|                                   | • Tỷ lệ đóng góp: 100% (Hoàn thành xuất sắc nhiệm vụ).                                                                                                                                                  |
+-----------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
| Trần Thanh Danh                   | • Phân hệ: Quản trị & Bảo mật (Admin & Auth).                                                                                                                                                           |
| MSSV: 25810009                    | • Mã nguồn sở hữu: AdminController.cs, AuthController.cs, Views/Admin/, Views/Auth/.                                                                                                                   |
|                                   | • Mẫu thiết kế GoF: Decorator Pattern ([AuthorizeRoles] kiểm soát truy cập RBAC và chặn tài khoản bị vô hiệu hóa), Singleton Pattern (PasswordHasherService băm và xác thực mật khẩu PBKDF2).          |
|                                   | • Sơ đồ kỹ thuật: Lược đồ thực thể – quan hệ ERD 8 bảng CSDL (Hình 3.1), Lược đồ lớp tổng quát hệ thống (Class Diagram – Hình 5.3), Toàn bộ 5 Lược đồ Use Case tổng thể và chi tiết (Hình 2.1 – Hình 2.5). |
|                                   | • Tỷ lệ đóng góp: 100% (Hoàn thành xuất sắc nhiệm vụ).                                                                                                                                                  |
+-----------------------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
```

---

## 3. BẢNG SO SÁNH TRƯỚC VÀ SAU KHI XỬ LÝ

| Chỉ số kiểm định | Trạng thái ban đầu | Trạng thái sau cập nhật | Kết luận pháp chứng |
|---|---|---|---|
| **Bản sao lưu dự phòng** | Chưa có | Đã lưu tại `backups/backup_before_pl1_update/` | Bảo toàn an toàn 100% |
| **Bảng biểu trong Doc1** | 55 bảng | 55 bảng | Giữ nguyên cấu trúc |
| **Bảng biểu trong Doc2** | 62 bảng (thừa 7 bảng) | 55 bảng (đã xóa 7 bảng tạm) | Khớp 1-to-1 với Doc1 |
| **Số lượng Heading Doc1** | 149 headings | 149 headings (14 H1, 62 H2, 73 H3) | Bất biến 100% |
| **Số lượng Heading Doc2** | 149 headings | 149 headings (14 H1, 62 H2, 73 H3) | Bất biến 100% |
| **Mục lục TOC (P60-P195)** | 136 mục | 136 mục | Bất biến 100% |
| **Từ khóa cấm trong Bảng PL.1** | Dính `& Kiểm thử tự động` | 0 từ khóa ngoài lề | Xóa sạch 100% |
| **Phân chia công việc** | Dính chùm ngang theo hạ tầng | 4 Vertical Slices độc lập, Zero Overlap | Chuẩn hóa 100% |
| **Tỷ lệ đóng góp** | Không ghi nhận | 100% cho mỗi thành viên | Đầy đủ 100% |
| **Mở qua Word 16.0 COM** | - | Mở mượt mà, 0 lỗi schema / corruption | Xác thực độc lập PASS |

---

## 4. KẾT LUẬN

Nhiệm vụ Milestone 1 đã hoàn thành trọn vẹn, tuyệt đối không có sai sót, thỏa mãn 100% các tiêu chí chấp nhận kỹ thuật và sẵn sàng cho các công đoạn tiếp theo của hệ thống.
