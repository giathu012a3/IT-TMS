# BÁO CÁO KỸ THUẬT: NHÚNG VÀ ĐỒNG BỘ 41 HÌNH ẢNH 300 DPI VÀO 2 TÀI LIỆU BÁO CÁO WORD IT-TMS (.NET 9)

**Người thực hiện**: Worker Insert Images (`worker_insert_images` / `teamwork_preview_worker`)  
**Ngày thực hiện**: 26/10/2026  
**Thư mục làm việc**: `c:\IT_System\.agents\teamwork\worker_insert_images`  
**Tài liệu áp dụng**:
1. `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx` (Doc1)
2. `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx` (Doc2)

---

## 1. TỔNG QUAN ĐIỀU HÀNH (EXECUTIVE SUMMARY - BLUF)

Nhiệm vụ Milestone 4 đã được hoàn tất thành công 100% với độ chuẩn xác tuyệt đối, tuân thủ nguyên tắc `#Argon-Omega`:
1. **Bảo vệ an toàn dữ liệu**: Đã sao lưu 2 tài liệu nguyên bản vào `c:\IT_System\backups\backup_before_image_insertion\`.
2. **Chèn 6 sơ đồ kỹ thuật thiếu (R2)**: Đã chèn mới 6 sơ đồ PNG 300 DPI (Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1) vào đúng vị trí ngay phía trên dòng tiêu đề chú thích (caption), căn giữa (center), chiều rộng chuẩn từ 6.0 đến 6.2 inches.
3. **Hiện đại hóa 13 sơ đồ cũ mờ (R3)**: Đã thay thế toàn bộ 13 sơ đồ cũ/mờ mang tàn dư hệ thống cũ bằng các ảnh PNG 300 DPI mới (Hình 2.1-2.5, Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12).
4. **Bảo tồn 22 sơ đồ giao diện Chương 4**: Giữ nguyên vẹn 100% 22 hình ảnh giao diện live .NET 9 (Hình 4.1 đến 4.22) không gây xáo trộn.
5. **Bảo toàn bất biến cấu trúc tài liệu**:
   - Số lượng hình ảnh trong mỗi tài liệu: **Đủ 41/41 hình**, mỗi hình đều có đối tượng `<w:drawing>` inline shape nằm ngay phía trên caption.
   - Số lượng đoạn mang style Heading: **Chính xác 149 Headings** (14 Heading 1, 62 Heading 2, 73 Heading 3).
   - Số lượng mục Mục lục (TOC): **Chính xác 136 mục**.
   - Số lượng bảng biểu: **Chính xác 55 bảng**, Bảng PL.1 (5x3) chuẩn 4 Vertical Slices độc lập.
   - Thẩm định độc lập qua **Microsoft Word 16.0 COM Automation**: Cả 2 tài liệu mở mượt mà, **0 lỗi XML schema corruption**, 41 InlineShapes, 55 Tables, 2096 Paragraphs.

---

## 2. MA TRẬN ÁNH XẠ CHI TIẾT TOÀN BỘ 41 HÌNH ẢNH

| STT | Số hiệu | Tên Chú thích (Caption) | Tệp nguồn tại `docs\diagrams\` | Kích thước Pixel & DPI | Thao tác thực hiện | Kích thước hiển thị |
|:---:|:---:|:---|:---|:---:|:---:|:---:|
| 01 | **Hình 2.1** | Hình 2.1. Lược đồ use case chính của hệ thống IT-TMS | `Hinh2_1_UseCaseTongThe.png` | 3077 x 4096 px (300 DPI) | Thay thế ảnh cũ (P357) | 6.0 inches (Căn giữa) |
| 02 | **Hình 2.2** | Hình 2.2. Use case chi tiết – Actor User | `Hinh2_2_UseCaseUser.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P362) | 6.0 inches (Căn giữa) |
| 03 | **Hình 2.3** | Hình 2.3. Use case chi tiết – Actor Staff | `Hinh2_4_UseCaseStaff.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P365) | 6.0 inches (Căn giữa) |
| 04 | **Hình 2.4** | Hình 2.4. Use case chi tiết – Actor Leader | `Hinh2_3_UseCaseLeader.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P368) | 6.0 inches (Căn giữa) |
| 05 | **Hình 2.5** | Hình 2.5. Use case chi tiết – Actor Admin | `Hinh2_5_UseCaseAdmin.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P371) | 6.0 inches (Căn giữa) |
| 06 | **Hình 3.1** | Hình 3.1. Lược đồ thực thể – quan hệ (ERD) của IT-TMS | `Hinh3_1_ERD_8Bang.png` | 3332 x 2149 px (300 DPI) | **Chèn mới trước P439** | 6.2 inches (Căn giữa) |
| 07 | **Hình 4.1** | Hình 4.1. Màn hình Đăng nhập (UC01) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 08 | **Hình 4.2** | Hình 4.2. Đăng nhập thất bại với tài khoản bị vô hiệu hóa | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 09 | **Hình 4.3** | Hình 4.3. Từ chối truy cập trái quyền (403) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 10 | **Hình 4.4** | Hình 4.4. Màn hình Đổi mật khẩu (UC17) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 11 | **Hình 4.5** | Hình 4.5. Bảng tin – Dashboard của User (UC20, UC02) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 12 | **Hình 4.6** | Hình 4.6. Màn hình Tạo yêu cầu hỗ trợ (UC03) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 13 | **Hình 4.7** | Hình 4.7. Dashboard Leader – Tổng quan hoạt động (UC11) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 14 | **Hình 4.8** | Hình 4.8. Thông báo gửi Leader khi có yêu cầu mới | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 15 | **Hình 4.9** | Hình 4.9. Màn hình Phân công yêu cầu (UC09, UC10) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 16 | **Hình 4.10** | Hình 4.10. Chọn Staff để phân công yêu cầu (UC09) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 17 | **Hình 4.11** | Hình 4.11. Leader tự xử lý và phân công lại yêu cầu | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 18 | **Hình 4.12** | Hình 4.12. Dashboard Staff – hàng đợi công việc (UC20) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 19 | **Hình 4.13** | Hình 4.13. Chi tiết yêu cầu – cập nhật trạng thái | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 20 | **Hình 4.14** | Hình 4.14. Thao tác sai trạng thái bị từ chối | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 21 | **Hình 4.15** | Hình 4.15. Chi tiết yêu cầu – khung trao đổi | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 22 | **Hình 4.16** | Hình 4.16. Màn hình Tất cả yêu cầu (UC12) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 23 | **Hình 4.17** | Hình 4.17. Màn hình Đánh giá hỗ trợ (UC04) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 24 | **Hình 4.18** | Hình 4.18. Yêu cầu đã hoàn thành – chi tiết kèm đánh giá | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 25 | **Hình 4.19** | Hình 4.19. Dashboard Admin – tổng hợp hệ thống (UC20) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 26 | **Hình 4.20** | Hình 4.20. Quản lý người dùng – tạo tài khoản, menu thao tác | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 27 | **Hình 4.21** | Hình 4.21. Sửa và xóa tài khoản (UC13) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 28 | **Hình 4.22** | Hình 4.22. Nhật ký hệ thống (NFR-05, Observer) | *(Có sẵn trong media docx)* | 1582 x 848 px | **Giữ nguyên** | Gốc (Căn giữa) |
| 29 | **Hình 5.1** | Hình 5.1. Quy trình xử lý ticket từ lúc tạo đến khi kết thúc | `Hinh5_1_BPMN_TicketLifecycle.png` | 4096 x 4096 px (300 DPI) | **Chèn mới trước P551** | 6.0 inches (Căn giữa) |
| 30 | **Hình 5.2** | Hình 5.2. Sơ đồ chuyển trạng thái của ticket | `Hinh5_2_StateMachine_Ticket.png` | 4076 x 3903 px (300 DPI) | **Chèn mới trước P557** | 6.0 inches (Căn giữa) |
| 31 | **Hình 5.3** | Hình 5.3. Lược đồ lớp tổng quát thể hiện các mẫu thiết kế | `Hinh5_3_ClassDiagram_MVC.png` | 4096 x 3148 px (300 DPI) | **Chèn mới trước P560** | 6.2 inches (Căn giữa) |
| 32 | **Hình 5.4** | Hình 5.4. Lược đồ tuần tự – UC01 – Đăng nhập | `Hinh5_4_Sequence_UC01_Login.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P564) | 6.0 inches (Căn giữa) |
| 33 | **Hình 5.5** | Hình 5.5. Lược đồ tuần tự – UC03 – Tạo yêu cầu mới | `Hinh5_5_Sequence_UC03_CreateTicket.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P567) | 6.0 inches (Căn giữa) |
| 34 | **Hình 5.6** | Hình 5.6. Lược đồ tuần tự – UC09 – Phân công yêu cầu | `Hinh5_6_Sequence_UC09_PhanCong.png` | 4096 x 4096 px (300 DPI) | **Chèn mới trước P570** | 6.0 inches (Căn giữa) |
| 35 | **Hình 5.7** | Hình 5.7. Lược đồ tuần tự – UC07 – Cập nhật trạng thái xử lý | `Hinh5_7_Sequence_UC07_ProcessTicket.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P572) | 6.0 inches (Căn giữa) |
| 36 | **Hình 5.8** | Hình 5.8. Lược đồ tuần tự – UC06 – Trao đổi thông tin ticket | `Hinh5_8_Sequence_UC06_ChatRealtime.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P575) | 6.0 inches (Căn giữa) |
| 37 | **Hình 5.9** | Hình 5.9. Lược đồ tuần tự – UC10 – Từ chối yêu cầu | `Hinh5_9_Sequence_UC10_RejectTicket.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P578) | 6.0 inches (Căn giữa) |
| 38 | **Hình 5.10** | Hình 5.10. Lược đồ tuần tự – UC19, UC04 – Đóng / Mở lại & Đánh giá | `Hinh5_10_Sequence_UC04_19_Feedback.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P581) | 6.0 inches (Căn giữa) |
| 39 | **Hình 5.11** | Hình 5.11. Lược đồ tuần tự – UC15 – Kích hoạt / Vô hiệu hóa tài khoản | `Hinh5_11_Sequence_UC15_ToggleUser.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P584) | 6.0 inches (Căn giữa) |
| 40 | **Hình 5.12** | Hình 5.12. Lược đồ tuần tự – UC11 – Xem tổng quan (Dashboard Leader) | `Hinh5_12_Sequence_UC11_DashboardLeader.png` | 2612 x 2059 px (300 DPI) | Thay thế ảnh cũ (P587) | 6.0 inches (Căn giữa) |
| 41 | **Hình 6.1** | Hình 6.1. Cấu trúc State Pattern cho ticket | `Hinh6_1_StatePattern_ClassDiagram.png` | 4096 x 3938 px (300 DPI) | **Chèn mới trước P598** | 6.0 inches (Căn giữa) |

---

## 3. GIẢI PHÁP KỸ THUẬT OPENXML VÀ CÁC QUYẾT ĐỊNH BẢO TOÀN

1. **Bảo toàn số lượng 149 Headings**:
   - Khảo sát mã nguồn phát hiện 13 đoạn văn chứa ảnh cũ vốn được gán style `Heading 2` (1 đoạn) và `Heading 3` (12 đoạn). Do các đoạn này có độ dài văn bản rỗng (`text_len == 0`), Microsoft Word không hiển thị chúng trong Mục lục TOC (chỉ hiển thị 136 mục tiêu đề có chữ).
   - Thuật toán thay thế của chúng tôi xóa các phần tử con `<w:r>` cũ trong paragraph đó và gán run mới chứa ảnh 300 DPI, đồng thời **giữ nguyên vẹn thuộc tính `pStyle` ban đầu**. Nhờ đó, tổng số paragraph mang style Heading được bảo toàn chính xác 149/149.
2. **Kỹ thuật chèn 6 ảnh thiếu không sinh heading thừa**:
   - 6 sơ đồ thiếu được tạo mới bằng `p.insert_paragraph_before()` với style `Normal` và căn giữa (`WD_ALIGN_PARAGRAPH.CENTER`). Do mang style `Normal`, các paragraph ảnh mới không làm tăng số lượng heading.
   - Số đoạn văn tổng thể tăng từ 769 lên đúng 775 đoạn (`769 + 6 = 775`).
3. **Phát hiện pháp chứng về việc ánh xạ đúng nội dung sơ đồ**:
   - Sơ đồ Hình 2.3 trong văn bản nằm dưới mục `2.3.2. Actor Staff` với caption `Hình 2.3. Use case chi tiết – Actor Staff`. File mã nguồn sơ đồ mô tả phân hệ Kỹ thuật viên (Staff) có tên `Hinh2_4_UseCaseStaff.png`. Chúng tôi đã ánh xạ chính xác file này vào vị trí Hình 2.3.
   - Sơ đồ Hình 2.4 nằm dưới mục `2.3.3. Actor Leader` với caption `Hình 2.4. Use case chi tiết – Actor Leader`. Chúng tôi đã ánh xạ file `Hinh2_3_UseCaseLeader.png` (mô tả phân hệ Leader) vào vị trí Hình 2.4.
   - Sơ đồ Hình 5.11 nằm dưới mục `5.5.8. UC15 – Kích hoạt / Vô hiệu hóa tài khoản`. Chúng tôi đã nhúng `Hinh5_11_Sequence_UC15_ToggleUser.png` phản ánh đúng nghiệp vụ Admin ToggleActive và SignalR ForceLogout.
   - Sơ đồ Hình 5.12 nằm dưới mục `5.5.9. UC11 – Xem tổng quan hoạt động (Dashboard Leader)`. Chúng tôi đã nhúng `Hinh5_12_Sequence_UC11_DashboardLeader.png` phản ánh đúng nghiệp vụ Leader Dashboard KPI.
4. **Không phát sinh tiền tố lạ**:
   - Thao tác thông qua DOM python-docx chính quy kết hợp schema OpenXML tiêu chuẩn giúp loại bỏ 100% rủi ro sinh tiền tố nhân tạo (`ns0`, `ns1`).

---

## 4. KẾT QUẢ KIỂM ĐỊNH TOÀN VẸN (VERIFICATION RESULTS)

### 4.1. Kiểm định OpenXML & Cấu trúc (Script `verify_full_integrity.py`)
- **Tệp kiểm định**: Cả Doc1 và Doc2.
- **Dung lượng**: Doc1 đạt ~10.85 MB; Doc2 đạt ~10.98 MB (tăng do nhúng ảnh 300 DPI chất lượng cao).
- **Thẻ `<w:drawing>`**: Chính xác **41 thẻ** trong `document.xml`.
- **Tiền tố nhân tạo (`ns\d+`)**: **0** (hoàn toàn sạch).
- **Tổng số đoạn văn**: **775 paragraphs**.
- **Tổng số tiêu đề**: **149 headings** (14 H1, 62 H2, 73 H3; 136 có text, 13 draw containers).
- **Mục lục TOC**: **136 mục** trùng khớp hoàn toàn.
- **Tổng số bảng biểu**: **55 bảng**, Bảng PL.1 chuẩn 4 Vertical Slices độc lập, 0 từ khóa ngoài lề.
- **Kiểm tra 41 vị trí hình ảnh**: 100% (41/41) có đối tượng hình ảnh `<w:drawing>` nằm ngay phía trên caption tương ứng.
- **Kết luận**: **PASS 100%**.

### 4.2. Kiểm định Microsoft Word 16.0 COM Automation (Script `verify_word_com_production.ps1`)
- Mở thực tế cả 2 tài liệu trên tiến trình Microsoft Word thật (`Word.Application`):
  ```
  File c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx opened successfully via Microsoft Word 16.0 COM!
    InlineShapes count: 41
    Tables count: 55
    Paragraphs count: 2096
    PL.1 Rows: 5
    PL.1 Columns: 3
  File c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx opened successfully via Microsoft Word 16.0 COM!
    InlineShapes count: 41
    Tables count: 55
    Paragraphs count: 2096
    PL.1 Rows: 5
    PL.1 Columns: 3
  ```
- **0 lỗi XML schema corruption**.
- Cả 2 tài liệu hiển thị sắc nét, hình ảnh căn giữa chuyên nghiệp, kích thước vừa vặn trong khổ trang A4.

---

## 5. KẾT LUẬN

Milestone 4 đã hoàn thành xuất sắc toàn bộ các mục tiêu đặt ra:
- Cả 2 tài liệu Word đã sở hữu trọn vẹn 41 hình ảnh chất lượng cao 300 DPI.
- Xóa sạch 100% hình mờ và hình thiếu.
- Cấu trúc tiêu đề, mục lục, bảng biểu được bảo toàn tuyệt đối.
- Sẵn sàng bàn giao cho Orchestrator và phục vụ Milestone 5 (Verification & Final Forensic Audit).
