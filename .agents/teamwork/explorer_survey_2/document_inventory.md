# BÁO CÁO KHẢO SÁT TOÀN DIỆN TÀI LIỆU VÀ DANH MỤC HÌNH ẢNH IT-TMS (.NET 9)

> **Mã khảo sát**: EXP-SURVEY-02  
> **Tài liệu đối chiếu**:
> 1. `File 1`: `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx` (3,063,830 bytes)
> 2. `File 2`: `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx` (2,995,788 bytes)  
> **Thời điểm khảo sát**: 2026-10-26T09:25:00Z  
> **Chuẩn áp dụng**: `#Argon-Omega` Cognitive Kernel (BLUF-MECE, Zero Sycophancy, Forensic Precision)

---

## 1. TỔNG QUAN KHẢO SÁT & BLUF (BOTTOM LINE UP FRONT)

1. **Về danh mục tiêu đề (Headings) & Mục lục (TOC)**:
   - Cả hai tài liệu đều chứa **chính xác 149 paragraphs** được định dạng style `Heading *` (14 `Heading 1`, 62 `Heading 2`, 73 `Heading 3`).
   - Khảo sát mã nguồn XML phát hiện sự thật cấu trúc: **136 tiêu đề văn bản thực tế** (khớp 100% không lệch 1 ký tự với 136 mục trong Mục lục TOC) + **13 paragraph rỗng mang style Heading** do tác giả tiền nhiệm dùng làm container chứa hình vẽ inline shape (Hình 2.1, 2.2, 2.3, 2.4, 2.5, 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12).
   - Thứ tự, cấu trúc phân cấp và nội dung 149 tiêu đề trên cả 2 tài liệu hoàn toàn đồng nhất (`diff = 0`).

2. **Về Bảng phân công công việc Phụ lục 1 (Bảng PL.1)**:
   - Nằm ở cuối tài liệu (sau Mục Phụ lục, trước phần kết thúc; Bảng thứ 55 trong Doc 1, Bảng thứ 62 trong Doc 2).
   - Nội dung hiện tại mắc **4 khiếm khuyết cấu trúc nghiêm trọng**:
     - Phân công theo tầng ngang (DB, Architecture, UI) thay vì phân hệ nghiệp vụ độc lập.
     - Dính "Kiểm thử tự động" (Unit test) trong phần việc của Nguyễn Gia Thụ.
     - Ghép 2 phân hệ vào 1 thành viên (Hồ Thanh Tình làm Leader & Staff; Trần Thanh Danh làm User & Admin) dẫn đến chồng chéo mã nguồn và rủi ro bị động khi Hội đồng vấn đáp kiểm tra từng Controller.
     - Cần tái cấu trúc sang **4 Vertical Slices độc lập**: 1 Người = 1 Phân hệ = 1 Controller = 1 Mẫu thiết kế GoF = Bộ sơ đồ UML tương ứng.

3. **Về danh mục 41 hình ảnh trong tài liệu Word**:
   - Cả hai tài liệu đều xác định danh mục gồm **chính xác 41 hình ảnh** (từ `Hình 2.1` đến `Hình 6.1`; không có `Hình 1.x`).
   - Phân loại hiện trạng:
     - **6 hình BỊ THIẾU (MISSING - R2)**: `Hình 3.1` (ERD CSDL 8 bảng), `Hình 5.1` (BPMN Quy trình xử lý ticket), `Hình 5.2` (State Machine 7 trạng thái), `Hình 5.3` (Class Diagram tổng thể), `Hình 5.6` (Sequence UC09 Phân công), `Hình 6.1` (Class Diagram State Pattern). Tại các vị trí này chỉ có caption và chữ text, hoàn toàn thiếu đối tượng hình ảnh.
     - **13 hình CŨ MỜ / CHƯA ĐỒNG BỘ .NET 9 (OUTDATED - R3)**: 5 hình Use Case Chương 2 (`Hình 2.1` – `2.5`) và 8 hình Sequence Diagrams Chương 5 (`Hình 5.4`, `5.5`, `5.7`, `5.8`, `5.9`, `5.10`, `5.11`, `5.12`). Các hình này là tàn dư hệ thống cũ, định dạng EMF/JPG mờ, chưa thể hiện đúng tên Controller, Method và WebSocket SignalR của C# .NET 9.
     - **22 hình GIAO DIỆN LIVE .NET 9 (AVAILABLE)**: `Hình 4.1` đến `Hình 4.22`. Cả 22 hình đều đã được chụp tự động thực tế từ ứng dụng ASP.NET Core live ở độ phân giải cao 1582x848 (tỉ lệ 16:10), kích thước hiển thị chuẩn ~14.82 x 9.26 cm, nhúng `<wp:inline>`.
   - **Bất thường OpenXML giữa 2 file**: Trong File 2 (`BaoCao_ITTMS_TKPMHDT_DotNet9.docx`), còn sót **7 bảng tạm thừa `[CHÈN HÌNH]`** (Table #42, #43, #44, #45, #46, #47, #48) nằm xen giữa Heading và Hình ảnh tại các mục 4.2, 4.3, 4.8, 4.11, 4.14, 4.21, 4.22. Trong File 1 (`BaoCao_ITTMS_TKPMHDT (4).docx`), các bảng tạm này đã được dọn sạch hoàn toàn (File 1 chỉ có 55 bảng chuẩn so với 62 bảng của File 2).

---

## 2. SO SÁNH TỔNG THỂ CẤU TRÚC 2 TÀI LIỆU WORD

| Chỉ số cấu trúc OpenXML | File 1: `BaoCao_ITTMS_TKPMHDT (4).docx` | File 2: `BaoCao_ITTMS_TKPMHDT_DotNet9.docx` | Kết luận đối chiếu |
|---|---|---|---|
| **Dung lượng file** | 3,063,830 bytes (~2.92 MB) | 2,995,788 bytes (~2.85 MB) | Chênh lệch do cấu trúc bảng tạm và media |
| **Tổng số Paragraphs** | 769 paragraphs | 769 paragraphs | Khớp 100% về số lượng đoạn |
| **Paragraphs mang style Heading** | 149 paragraphs | 149 paragraphs | Khớp 100% (14 H1, 62 H2, 73 H3) |
| **Tiêu đề văn bản thực tế** | 136 tiêu đề | 136 tiêu đề | Khớp 100% với 136 mục TOC |
| **Heading rỗng chứa Drawing** | 13 paragraphs | 13 paragraphs | Khớp 100% vị trí (H2.1-2.5, H5.4, 5.5, 5.7-5.12) |
| **Mục trong Mục lục (TOC)** | 136 mục (P60 – P195) | 136 mục (P60 – P195) | Khớp 100% không lệch 1 ký tự |
| **Mục trong Danh mục Hình** | 41 hình (P198 – P238) | 41 hình (P198 – P238) | Khớp 100% (Hình 2.1 đến 6.1) |
| **Mục trong Danh mục Bảng** | 47 bảng (P241 – P287) | 47 bảng (P241 – P287) | Khớp 100% (Bảng 1.1 đến Bảng PL.1) |
| **Tổng số Bảng thực tế trong body** | **55 bảng** | **62 bảng** | **Lệch 7 bảng**: Doc2 còn 7 bảng tạm `[CHÈN HÌNH]` thừa |
| **Tổng số tệp media trong zip** | 34 files | 35 files | Doc1 dùng chung 1 ảnh cho H4.13 & H4.15 |
| **Bảng phân công Phụ lục 1** | Bảng thứ 55 (5 dòng x 3 cột) | Bảng thứ 62 (5 dòng x 3 cột) | Nội dung 2 bên trùng khớp nhau |

### Phát hiện pháp y cấu trúc OpenXML:
1. **Sự tồn tại của 7 bảng tạm thừa trong File 2**:
   - `Table #42`: `[CHÈN HÌNH] Hình 4.2 – Đăng nhập thất bại với tài khoản bị vô hiệu hóa`
   - `Table #43`: `[CHÈN HÌNH] Hình 4.3 – Từ chối truy cập trái quyền (403)`
   - `Table #44`: `[CHÈN HÌNH] Hình 4.8 – Thông báo gửi Leader khi có yêu cầu mới`
   - `Table #45`: `[CHÈN HÌNH] Hình 4.11 – Leader tự xử lý và phân công lại yêu cầu`
   - `Table #46`: `[CHÈN HÌNH] Hình 4.14 – Thao tác sai trạng thái bị từ chối`
   - `Table #47`: `[CHÈN HÌNH] Hình 4.21 – Sửa và xóa tài khoản`
   - `Table #48`: `[CHÈN HÌNH] Hình 4.22 – Nhật ký hệ thống`
   *Đánh giá*: Trong File 2, người biên soạn trước đã chèn ảnh chụp màn hình nhưng quên xóa bảng placeholder. File 1 đã xóa sạch các bảng này nên File 1 sạch sẽ và chuẩn mực hơn. Khi đồng bộ, cần xóa 7 bảng tạm thừa này ở File 2 để đồng nhất 55 bảng.

2. **Cơ chế tái sử dụng ảnh H4.13 và H4.15 trong File 1**:
   - `Hình 4.13` (Chi tiết yêu cầu – cập nhật trạng thái) và `Hình 4.15` (Chi tiết yêu cầu – khung trao đổi) đều trỏ tới relationship `rId25` (`word/media/image18.png`, 100,405 bytes).
   - Trong File 2, hai hình này trỏ tới 2 file độc lập (`image14.png` và `image15.png`), tuy nhiên 2 file này hoàn toàn trùng khớp từng byte nội dung.

---

## 3. KHẢO SÁT CHI TIẾT 149 TIÊU ĐỀ (HEADINGS) VÀ MỤC LỤC (TOC)

### 3.1. Phân bố phân cấp Heading
- **Heading 1**: 14 tiêu đề (100% có nội dung text).
- **Heading 2**: 62 paragraphs (61 tiêu đề có text, 1 paragraph rỗng là container ảnh Hình 2.1 tại P357).
- **Heading 3**: 73 paragraphs (61 tiêu đề có text, 12 paragraphs rỗng là container ảnh Hình 2.2–2.5 và Hình 5.4, 5.5, 5.7–5.12).
- **Tổng cộng**: 14 + 62 + 73 = **149 paragraphs mang style Heading**.
- **Số mục hiển thị trên Mục lục (TOC)**: 14 + 61 + 61 = **136 mục**. Do 13 paragraphs chứa ảnh không có text nên Microsoft Word tự động bỏ qua không đưa vào TOC hiển thị, giúp TOC hoàn toàn sạch và chuẩn mực.

### 3.2. Danh mục chi tiết toàn bộ 149 Heading-styled Paragraphs

| STT | Phân cấp | Style Name | Paragraph Index | Nội dung tiêu đề / Chức năng đoạn | Trang TOC | Ghi chú cấu trúc |
|:---:|:---:|:---|:---:|:---|:---:|:---|
| 001 | H1 | Heading 1 | P289 | TÓM TẮT ĐỒ ÁN | x | Tiêu đề đầu phần nội dung |
| 002 | H1 | Heading 1 | P294 | PHẦN MỞ ĐẦU | 1 | Phần mở đầu |
| 003 | H2 | Heading 2 | P295 | 1. LÝ DO CHỌN ĐỀ TÀI | 1 | Mục 1 mở đầu |
| 004 | H2 | Heading 2 | P298 | 2. MỤC TIÊU CỦA ĐỀ TÀI | 1 | Mục 2 mở đầu |
| 005 | H2 | Heading 2 | P307 | 3. ĐỐI TƯỢNG NGHIÊN CỨU | 2 | Mục 3 mở đầu |
| 006 | H2 | Heading 2 | P309 | 4. PHẠM VI NGHIÊN CỨU | 2 | Mục 4 mở đầu |
| 007 | H2 | Heading 2 | P312 | 5. PHƯƠNG PHÁP NGHIÊN CỨU | 2 | Mục 5 mở đầu |
| 008 | H2 | Heading 2 | P314 | 6. KẾT QUẢ DỰ KIẾN ĐẠT ĐƯỢC | 2 | Mục 6 mở đầu |
| 009 | H1 | Heading 1 | P316 | PHẦN NỘI DUNG | 3 | Tiêu đề phân đoạn |
| 010 | H1 | Heading 1 | P318 | CHƯƠNG 1. KHẢO SÁT HIỆN TRẠNG VÀ XÁC ĐỊNH YÊU CẦU | 4 | Chương 1 |
| 011 | H2 | Heading 2 | P319 | 1.1. MÔ TẢ BÀI TOÁN | 4 | Mục 1.1 |
| 012 | H2 | Heading 2 | P323 | 1.2. KHẢO SÁT HIỆN TRẠNG | 4 | Mục 1.2 |
| 013 | H2 | Heading 2 | P327 | 1.3. CÁC VAI TRÒ TRONG HỆ THỐNG (ACTORS) | 5 | Mục 1.3 |
| 014 | H2 | Heading 2 | P330 | 1.4. YÊU CẦU CHỨC NĂNG | 6 | Mục 1.4 |
| 015 | H3 | Heading 3 | P331 | 1.4.1. Xác thực và phân quyền (chung) | 6 | Mục 1.4.1 |
| 016 | H3 | Heading 3 | P334 | 1.4.2. Người dùng (User) | 6 | Mục 1.4.2 |
| 017 | H3 | Heading 3 | P337 | 1.4.3. Nhân viên kỹ thuật (Staff) | 6 | Mục 1.4.3 |
| 018 | H3 | Heading 3 | P340 | 1.4.4. Trưởng nhóm kỹ thuật (Leader) | 7 | Mục 1.4.4 |
| 019 | H3 | Heading 3 | P343 | 1.4.5. Quản trị viên (Admin) | 7 | Mục 1.4.5 |
| 020 | H2 | Heading 2 | P346 | 1.5. GOM NHÓM CHỨC NĂNG | 8 | Mục 1.5 |
| 021 | H2 | Heading 2 | P349 | 1.6. YÊU CẦU PHI CHỨC NĂNG | 8 | Mục 1.6 |
| 022 | H1 | Heading 1 | P352 | CHƯƠNG 2. PHÂN TÍCH VÀ MÔ HÌNH HÓA YÊU CẦU | 9 | Chương 2 |
| 023 | H2 | Heading 2 | P353 | 2.1. ACTOR VÀ CHỨC NĂNG | 9 | Mục 2.1 |
| 024 | H2 | Heading 2 | P356 | 2.2. LƯỢC ĐỒ USE CASE CHÍNH | 10 | Mục 2.2 |
| 025 | H2 | Heading 2 | P357 | *(Trống - Container chứa Hình 2.1)* | - | Image holder inline |
| 026 | H2 | Heading 2 | P359 | 2.3. LƯỢC ĐỒ USE CASE CHI TIẾT | 10 | Mục 2.3 |
| 027 | H3 | Heading 3 | P361 | 2.3.1. Actor User | 10 | Mục 2.3.1 |
| 028 | H3 | Heading 3 | P362 | *(Trống - Container chứa Hình 2.2)* | - | Image holder inline |
| 029 | H3 | Heading 3 | P364 | 2.3.2. Actor Staff | 10 | Mục 2.3.2 |
| 030 | H3 | Heading 3 | P365 | *(Trống - Container chứa Hình 2.3)* | - | Image holder inline |
| 031 | H3 | Heading 3 | P367 | 2.3.3. Actor Leader | 11 | Mục 2.3.3 |
| 032 | H3 | Heading 3 | P368 | *(Trống - Container chứa Hình 2.4)* | - | Image holder inline |
| 033 | H3 | Heading 3 | P370 | 2.3.4. Actor Admin | 11 | Mục 2.3.4 |
| 034 | H3 | Heading 3 | P371 | *(Trống - Container chứa Hình 2.5)* | - | Image holder inline |
| 035 | H2 | Heading 2 | P374 | 2.4. MÔ TẢ CHI TIẾT CÁC USE CASE | 11 | Mục 2.4 |
| 036 | H3 | Heading 3 | P376 | 2.4.1. UC01 – Đăng nhập (Login) | 11 | Mục 2.4.1 |
| 037 | H3 | Heading 3 | P379 | 2.4.2. UC02 – Xem danh sách yêu cầu cá nhân (View My Tickets) | 12 | Mục 2.4.2 |
| 038 | H3 | Heading 3 | P382 | 2.4.3. UC03 – Tạo yêu cầu mới (Create New Ticket) | 12 | Mục 2.4.3 |
| 039 | H3 | Heading 3 | P385 | 2.4.4. UC04 – Đánh giá & Feedback (Rate and Feedback) | 13 | Mục 2.4.4 |
| 040 | H3 | Heading 3 | P388 | 2.4.5. UC05 – Đăng xuất (Logout) | 13 | Mục 2.4.5 |
| 041 | H3 | Heading 3 | P391 | 2.4.6. UC06 – Trao đổi thông tin ticket | 14 | Mục 2.4.6 |
| 042 | H3 | Heading 3 | P394 | 2.4.7. UC07 – Cập nhật trạng thái xử lý (Update Ticket Status) | 15 | Mục 2.4.7 |
| 043 | H3 | Heading 3 | P397 | 2.4.8. UC08 – Xem chi tiết yêu cầu (View Ticket Detail) | 15 | Mục 2.4.8 |
| 044 | H3 | Heading 3 | P400 | 2.4.9. UC09 – Phân công yêu cầu (Assign Ticket) | 16 | Mục 2.4.9 |
| 045 | H3 | Heading 3 | P403 | 2.4.10. UC10 – Từ chối yêu cầu (Reject Ticket) | 16 | Mục 2.4.10 |
| 046 | H3 | Heading 3 | P406 | 2.4.11. UC11 – Xem tổng quan hoạt động (View Management Dashboard) | 17 | Mục 2.4.11 |
| 047 | H3 | Heading 3 | P409 | 2.4.12. UC12 – Xem & tra cứu tất cả yêu cầu (View All Tickets) | 17 | Mục 2.4.12 |
| 048 | H3 | Heading 3 | P412 | 2.4.13. UC13 – Quản lý người dùng & phân quyền (User Management) | 18 | Mục 2.4.13 |
| 049 | H3 | Heading 3 | P415 | 2.4.14. UC14 – Tạo tài khoản người dùng mới (Create New User) | 18 | Mục 2.4.14 |
| 050 | H3 | Heading 3 | P418 | 2.4.15. UC15 – Kích hoạt / Vô hiệu hóa tài khoản (Toggle User Status) | 19 | Mục 2.4.15 |
| 051 | H3 | Heading 3 | P421 | 2.4.16. UC16 – Xem lịch sử xử lý (View Processing History) | 19 | Mục 2.4.16 |
| 052 | H3 | Heading 3 | P424 | 2.4.17. UC17 – Đổi mật khẩu cá nhân (Change Own Password) | 20 | Mục 2.4.17 |
| 053 | H3 | Heading 3 | P427 | 2.4.18. UC18 – Đặt lại mật khẩu người dùng (Admin Reset Password) | 20 | Mục 2.4.18 |
| 054 | H3 | Heading 3 | P430 | 2.4.19. UC19 – Đóng / Mở lại yêu cầu (Close or Reopen Ticket) | 21 | Mục 2.4.19 |
| 055 | H3 | Heading 3 | P433 | 2.4.20. UC20 – Xem Dashboard cá nhân (View Personal Dashboard) | 21 | Mục 2.4.20 |
| 056 | H1 | Heading 1 | P436 | CHƯƠNG 3. THIẾT KẾ DỮ LIỆU | 22 | Chương 3 |
| 057 | H2 | Heading 2 | P438 | 3.1. LƯỢC ĐỒ QUAN HỆ (ERD) | 22 | Mục 3.1 |
| 058 | H2 | Heading 2 | P441 | 3.2. MÔ TẢ CÁC BẢNG | 22 | Mục 3.2 |
| 059 | H3 | Heading 3 | P442 | 3.2.1. Bảng User – Người dùng | 22 | Mục 3.2.1 |
| 060 | H3 | Heading 3 | P446 | 3.2.2. Bảng SystemLog – Nhật ký hệ thống | 23 | Mục 3.2.2 |
| 061 | H3 | Heading 3 | P450 | 3.2.3. Bảng Ticket – Yêu cầu hỗ trợ | 23 | Mục 3.2.3 |
| 062 | H3 | Heading 3 | P454 | 3.2.4. Bảng TicketStatus – Trạng thái ticket | 24 | Mục 3.2.4 |
| 063 | H3 | Heading 3 | P458 | 3.2.5. Bảng Attachment – Tệp đính kèm | 24 | Mục 3.2.5 |
| 064 | H3 | Heading 3 | P462 | 3.2.6. Bảng Comment – Trao đổi | 25 | Mục 3.2.6 |
| 065 | H3 | Heading 3 | P466 | 3.2.7. Bảng Feedback – Đánh giá dịch vụ | 25 | Mục 3.2.7 |
| 066 | H3 | Heading 3 | P470 | 3.2.8. Bảng Notification – Thông báo | 26 | Mục 3.2.8 |
| 067 | H2 | Heading 2 | P474 | 3.3. CÁC RÀNG BUỘC TOÀN VẸN | 26 | Mục 3.3 |
| 068 | H1 | Heading 1 | P478 | CHƯƠNG 4. THIẾT KẾ GIAO DIỆN | 28 | Chương 4 |
| 069 | H2 | Heading 2 | P480 | 4.1. MÀN HÌNH ĐĂNG NHẬP | 28 | Mục 4.1 |
| 070 | H2 | Heading 2 | P483 | 4.2. ĐĂNG NHẬP THẤT BẠI VỚI TÀI KHOẢN BỊ VÔ HIỆU HÓA | 28 | Mục 4.2 |
| 071 | H2 | Heading 2 | P486 | 4.3. TỪ CHỐI TRUY CẬP TRÁI QUYỀN (403) | 28 | Mục 4.3 |
| 072 | H2 | Heading 2 | P489 | 4.4. MÀN HÌNH ĐỔI MẬT KHẨU | 29 | Mục 4.4 |
| 073 | H2 | Heading 2 | P492 | 4.5. BẢNG TIN – DASHBOARD CỦA USER | 29 | Mục 4.5 |
| 074 | H2 | Heading 2 | P495 | 4.6. MÀN HÌNH TẠO YÊU CẦU HỖ TRỢ | 30 | Mục 4.6 |
| 075 | H2 | Heading 2 | P498 | 4.7. DASHBOARD LEADER – TỔNG QUAN HOẠT ĐỘNG | 30 | Mục 4.7 |
| 076 | H2 | Heading 2 | P501 | 4.8. THÔNG BÁO GỬI LEADER KHI CÓ YÊU CẦU MỚI | 30 | Mục 4.8 |
| 077 | H2 | Heading 2 | P504 | 4.9. MÀN HÌNH PHÂN CÔNG YÊU CẦU | 31 | Mục 4.9 |
| 078 | H2 | Heading 2 | P507 | 4.10. CHỌN STAFF ĐỂ PHÂN CÔNG YÊU CẦU | 31 | Mục 4.10 |
| 079 | H2 | Heading 2 | P510 | 4.11. LEADER TỰ XỬ LÝ VÀ PHÂN CÔNG LẠI YÊU CẦU | 31 | Mục 4.11 |
| 080 | H2 | Heading 2 | P513 | 4.12. DASHBOARD STAFF – HÀNG ĐỢI CÔNG VIỆC | 32 | Mục 4.12 |
| 081 | H2 | Heading 2 | P516 | 4.13. CHI TIẾT YÊU CẦU – CẬP NHẬT TRẠNG THÁI | 32 | Mục 4.13 |
| 082 | H2 | Heading 2 | P519 | 4.14. THAO TÁC SAI TRẠNG THÁI BỊ TỪ CHỐI | 33 | Mục 4.14 |
| 083 | H2 | Heading 2 | P522 | 4.15. CHI TIẾT YÊU CẦU – KHUNG TRAO ĐỔI | 33 | Mục 4.15 |
| 084 | H2 | Heading 2 | P525 | 4.16. MÀN HÌNH TẤT CẢ YÊU CẦU | 33 | Mục 4.16 |
| 085 | H2 | Heading 2 | P528 | 4.17. MÀN HÌNH ĐÁNH GIÁ HỖ TRỢ | 34 | Mục 4.17 |
| 086 | H2 | Heading 2 | P531 | 4.18. YÊU CẦU ĐÃ HOÀN THÀNH – CHI TIẾT KÈM ĐÁNH GIÁ | 34 | Mục 4.18 |
| 087 | H2 | Heading 2 | P534 | 4.19. DASHBOARD ADMIN – TỔNG HỢP HỆ THỐNG | 35 | Mục 4.19 |
| 088 | H2 | Heading 2 | P537 | 4.20. QUẢN LÝ NGƯỜI DÙNG – TẠO TÀI KHOẢN, MENU THAO TÁC | 35 | Mục 4.20 |
| 089 | H2 | Heading 2 | P540 | 4.21. SỬA VÀ XÓA TÀI KHOẢN | 35 | Mục 4.21 |
| 090 | H2 | Heading 2 | P543 | 4.22. NHẬT KÝ HỆ THỐNG | 36 | Mục 4.22 |
| 091 | H1 | Heading 1 | P546 | CHƯƠNG 5. THIẾT KẾ XỬ LÝ | 37 | Chương 5 |
| 092 | H2 | Heading 2 | P547 | 5.1. KIẾN TRÚC TỔNG THỂ | 37 | Mục 5.1 |
| 093 | H2 | Heading 2 | P549 | 5.2. QUY TRÌNH XỬ LÝ TICKET HOÀN THIỆN | 37 | Mục 5.2 |
| 094 | H2 | Heading 2 | P555 | 5.3. SƠ ĐỒ CHUYỂN TRẠNG THÁI CỦA TICKET | 38 | Mục 5.3 |
| 095 | H2 | Heading 2 | P558 | 5.4. LƯỢC ĐỒ LỚP TỔNG QUÁT | 39 | Mục 5.4 |
| 096 | H2 | Heading 2 | P561 | 5.5. LƯỢC ĐỒ TUẦN TỰ CỦA CÁC USE CASE CHÍNH | 39 | Mục 5.5 |
| 097 | H3 | Heading 3 | P563 | 5.5.1. UC01 – Đăng nhập | 39 | Mục 5.5.1 |
| 098 | H3 | Heading 3 | P564 | *(Trống - Container chứa Hình 5.4)* | - | Image holder inline |
| 099 | H3 | Heading 3 | P566 | 5.5.2. UC03 – Tạo yêu cầu mới | 40 | Mục 5.5.2 |
| 100 | H3 | Heading 3 | P567 | *(Trống - Container chứa Hình 5.5)* | - | Image holder inline |
| 101 | H3 | Heading 3 | P569 | 5.5.3. UC09 – Phân công yêu cầu | 40 | Mục 5.5.3 |
| 102 | H3 | Heading 3 | P571 | 5.5.4. UC07 – Cập nhật trạng thái xử lý | 41 | Mục 5.5.4 |
| 103 | H3 | Heading 3 | P572 | *(Trống - Container chứa Hình 5.7)* | - | Image holder inline |
| 104 | H3 | Heading 3 | P574 | 5.5.5. UC06 – Trao đổi thông tin ticket | 42 | Mục 5.5.5 |
| 105 | H3 | Heading 3 | P575 | *(Trống - Container chứa Hình 5.8)* | - | Image holder inline |
| 106 | H3 | Heading 3 | P577 | 5.5.6. UC10 – Từ chối yêu cầu | 43 | Mục 5.5.6 |
| 107 | H3 | Heading 3 | P578 | *(Trống - Container chứa Hình 5.9)* | - | Image holder inline |
| 108 | H3 | Heading 3 | P580 | 5.5.7. UC19, UC04 – Đóng / Mở lại yêu cầu và đánh giá | 44 | Mục 5.5.7 |
| 109 | H3 | Heading 3 | P581 | *(Trống - Container chứa Hình 5.10)* | - | Image holder inline |
| 110 | H3 | Heading 3 | P583 | 5.5.8. UC15 – Kích hoạt / Vô hiệu hóa tài khoản | 45 | Mục 5.5.8 |
| 111 | H3 | Heading 3 | P584 | *(Trống - Container chứa Hình 5.11)* | - | Image holder inline |
| 112 | H3 | Heading 3 | P586 | 5.5.9. UC11 – Xem tổng quan hoạt động (Dashboard Leader) | 45 | Mục 5.5.9 |
| 113 | H3 | Heading 3 | P587 | *(Trống - Container chứa Hình 5.12)* | - | Image holder inline |
| 114 | H1 | Heading 1 | P589 | CHƯƠNG 6. ÁP DỤNG CÁC MẪU THIẾT KẾ PHẦN MỀM | 46 | Chương 6 |
| 115 | H2 | Heading 2 | P591 | 6.1. TỔNG QUAN CÁC MẪU THIẾT KẾ | 46 | Mục 6.1 |
| 116 | H2 | Heading 2 | P594 | 6.2. STATE PATTERN – VÒNG ĐỜI TICKET | 47 | Mục 6.2 |
| 117 | H3 | Heading 3 | P595 | a) Áp dụng cho chức năng nào | 47 | Mục 6.2.a |
| 118 | H3 | Heading 3 | P630 | b) Vì sao chọn State Pattern (lợi ích) | 48 | Mục 6.2.b |
| 119 | H3 | Heading 3 | P635 | c) Hạn chế gặp phải | 48 | Mục 6.2.c |
| 120 | H2 | Heading 2 | P640 | 6.3. OBSERVER PATTERN – THÔNG BÁO VÀ NHẬT KÝ | 48 | Mục 6.3 |
| 121 | H3 | Heading 3 | P641 | a) Áp dụng cho chức năng nào | 48 | Mục 6.3.a |
| 122 | H3 | Heading 3 | P651 | b) Vì sao chọn Observer | 49 | Mục 6.3.b |
| 123 | H3 | Heading 3 | P655 | c) Hạn chế gặp phải | 49 | Mục 6.3.c |
| 124 | H2 | Heading 2 | P659 | 6.4. DECORATOR PATTERN – KIỂM SOÁT TRUY CẬP THEO VAI TRÒ | 49 | Mục 6.4 |
| 125 | H3 | Heading 3 | P660 | a) Áp dụng cho chức năng nào | 49 | Mục 6.4.a |
| 126 | H3 | Heading 3 | P675 | b) Vì sao chọn Decorator | 50 | Mục 6.4.b |
| 127 | H3 | Heading 3 | P679 | c) Hạn chế gặp phải | 50 | Mục 6.4.c |
| 128 | H2 | Heading 2 | P682 | 6.5. SINGLETON PATTERN – ĐỐI TƯỢNG DÙNG CHUNG | 50 | Mục 6.5 |
| 129 | H3 | Heading 3 | P683 | a) Áp dụng cho chức năng nào | 50 | Mục 6.5.a |
| 130 | H3 | Heading 3 | P685 | b) Vì sao chọn Singleton | 51 | Mục 6.5.b |
| 131 | H3 | Heading 3 | P688 | c) Hạn chế gặp phải | 51 | Mục 6.5.c |
| 132 | H2 | Heading 2 | P691 | 6.6. MVC VÀ BLUEPRINT – KIẾN TRÚC TỔNG THỂ | 51 | Mục 6.6 |
| 133 | H3 | Heading 3 | P692 | a) Áp dụng cho chức năng nào | 51 | Mục 6.6.a |
| 134 | H3 | Heading 3 | P694 | b) Vì sao chọn | 51 | Mục 6.6.b |
| 135 | H3 | Heading 3 | P697 | c) Hạn chế gặp phải | 52 | Mục 6.6.c |
| 136 | H2 | Heading 2 | P700 | 6.7. MÃ NGUỒN TRƯỚC VÀ SAU KHI ÁP DỤNG MẪU | 52 | Mục 6.7 |
| 137 | H1 | Heading 1 | P704 | CHƯƠNG 7. CÀI ĐẶT VÀ KẾT QUẢ THỰC NGHIỆM | 55 | Chương 7 |
| 138 | H2 | Heading 2 | P706 | 7.1. KIẾN TRÚC HỆ THỐNG | 55 | Mục 7.1 |
| 139 | H2 | Heading 2 | P710 | 7.2. CẤU TRÚC THƯ MỤC | 55 | Mục 7.2 |
| 140 | H2 | Heading 2 | P727 | 7.3. NỀN TẢNG CÔNG NGHỆ | 55 | Mục 7.3 |
| 141 | H2 | Heading 2 | P730 | 7.4. KỊCH BẢN KIỂM THỬ VÀ KẾT QUẢ THỰC NGHIỆM | 55 | Mục 7.4 |
| 142 | H1 | Heading 1 | P734 | PHẦN KẾT LUẬN | 57 | Phần kết luận |
| 143 | H2 | Heading 2 | P735 | 1. KẾT QUẢ ĐẠT ĐƯỢC | 57 | Mục 1 kết luận |
| 144 | H2 | Heading 2 | P742 | 2. ƯU ĐIỂM VÀ KHUYẾT ĐIỂM | 57 | Mục 2 kết luận |
| 145 | H2 | Heading 2 | P751 | 3. HƯỚNG PHÁT TRIỂN | 58 | Mục 3 kết luận |
| 146 | H1 | Heading 1 | P756 | TÀI LIỆU THAM KHẢO | 59 | Tài liệu tham khảo |
| 147 | H1 | Heading 1 | P760 | PHỤ LỤC | 60 | Phụ lục |
| 148 | H2 | Heading 2 | P761 | A. SẢN PHẨM NỘP KÈM BÁO CÁO | 60 | Phụ lục A |
| 149 | H1 | Heading 1 | P766 | BẢNG PHÂN CÔNG CÔNG VIỆC | 61 | Tiêu đề Bảng PL.1 |

---

## 4. KHẢO SÁT CHI TIẾT BẢNG PHÂN CÔNG CÔNG VIỆC (BẢNG PL.1)

### 4.1. Vị trí và Cấu trúc hiện tại
- **Vị trí**:
  - `BaoCao_ITTMS_TKPMHDT (4).docx`: Table thứ 55 (table cuối cùng của tài liệu), nằm ngay dưới tiêu đề `BẢNG PHÂN CÔNG CÔNG VIỆC` (P766).
  - `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`: Table thứ 62 (table cuối cùng), nằm dưới tiêu đề P766.
- **Kích thước bảng**: 5 dòng x 3 cột (`Thành viên`, `MSSV`, `Công việc`).

### 4.2. Nội dung nguyên văn hiện tại trong cả 2 tài liệu

```
+-----------------+----------+-------------------------------------------------------------------------------------------------------------------------------------+
| Thành viên      | MSSV     | Công việc                                                                                                                           |
+-----------------+----------+-------------------------------------------------------------------------------------------------------------------------------------+
| Nguyễn Gia Thụ  | 25810041 | Kiến trúc Clean Architecture (.NET 9), Domain Model, State Pattern (7 trạng thái), SignalR Real-time Hub & Kiểm thử tự động        |
| Nguyễn Mạnh Tiến| 25810043 | Thiết kế CSDL EF Core 9 (SQLite), Migrations, Concurrency Token, Observer Pattern (Notification & SystemLog)                        |
| Hồ Thanh Tình   | 25810044 | Phân hệ Leader & Staff (MVC Areas), Bộ lọc phân quyền RBAC/Anti-BOLA (Decorator Pattern), SLA Dashboard                              |
| Trần Thanh Danh | 25810009 | Phân hệ User & Admin (MVC Areas), Giao diện Bootstrap 5 (Dark/Light Mode), Đính kèm tệp, Đánh giá Feedback & Tài liệu báo cáo        |
+-----------------+----------+-------------------------------------------------------------------------------------------------------------------------------------+
```

### 4.3. Phân tích khiếm khuyết kỹ thuật & Lý do bắt buộc tái cấu trúc

1. **Phân chia theo tầng ngang (Horizontal Layer Splitting) thay vì Phân hệ dọc (Vertical Slices)**:
   - Cách chia hiện tại phân tách hệ thống theo hạ tầng kỹ thuật: Thụ làm Kiến trúc/Domain, Tiến làm CSDL/Migrations, Tình làm Controller/Dashboard, Danh làm Giao diện/Báo cáo.
   - **Hệ quả tiêu cực**: Khi Giảng viên/Hội đồng chấm thi môn *Thiết kế Phần mềm Hướng Đối tượng* vấn đáp từng sinh viên ("Ai code chức năng nào? Controller nào thuộc quyền sở hữu của em? Mẫu thiết kế nào em trực tiếp áp dụng vào nghiệp vụ của mình?"), sinh viên sẽ bị lúng túng, đổ lỗi hoặc không chứng minh được tính độc lập mã nguồn.
2. **Dính nội dung ngoài lề môn học ("Kiểm thử tự động / Unit test / DevOps")**:
   - Dòng của Nguyễn Gia Thụ có ghi `& Kiểm thử tự động`. Trong chương trình môn TKPMHDT, trọng tâm đánh giá là Mô hình hóa UML, Kiến trúc phần mềm và Áp dụng Mẫu thiết kế GoF. Việc ghi "Kiểm thử tự động" làm loãng trọng tâm nghiệp vụ và vi phạm yêu cầu loại bỏ DevOps/Unit test của đề bài.
3. **Chồng chéo và gộp 2 phân hệ vào 1 thành viên**:
   - Hồ Thanh Tình bị gán cả 2 phân hệ lớn: `Leader & Staff`.
   - Trần Thanh Danh bị gán cả 2 phân hệ lớn: `User & Admin`.
   - Trong khi đó, Nguyễn Gia Thụ và Nguyễn Mạnh Tiến lại không sở hữu phân hệ controller nào rõ ràng.

### 4.4. Ma trận phân công chuẩn hóa theo 4 Vertical Slices độc lập (Chuẩn R1)

Để bảo đảm **Zero Overlap**, mỗi thành viên làm chủ trọn vẹn 1 lát cắt dọc (Vertical Slice): **1 Người = 1 Phân hệ nghiệp vụ = 1 Controller = 1 Mẫu thiết kế GoF = Bộ sơ đồ kỹ thuật tương ứng**:

| Thành viên | MSSV | Phân hệ độc lập | Controller & Views sở hữu | Mẫu thiết kế GoF & Trọng tâm kỹ thuật | Sơ đồ kỹ thuật phụ trách |
|---|---|---|---|---|---|
| **Nguyễn Gia Thụ** | 25810041 | **Kỹ thuật viên (Staff)** | `StaffController.cs`<br>`Views/Staff/` *(Dashboard, Assigned, Details, History)* | **State Pattern** (Quản lý 7 trạng thái vòng đời ticket, đóng băng thao tác sai) | • Sơ đồ State Machine 7 trạng thái (**Hình 5.2**)<br>• Lược đồ lớp State Pattern (**Hình 6.1**)<br>• Lược đồ tuần tự UC07 Cập nhật trạng thái (**Hình 5.7**) |
| **Nguyễn Mạnh Tiến** | 25810043 | **Trưởng nhóm (Leader)** | `LeaderController.cs`<br>`Views/Leader/` *(Dashboard, NewQueue, Index, Details)* | **Factory Method / Flyweight Pattern** (`TicketStateFactory.cs`, cấp phát trạng thái tối ưu bộ nhớ) | • Quy trình xử lý BPMN hoàn chỉnh (**Hình 5.1**)<br>• Lược đồ tuần tự UC09 Phân công ticket (**Hình 5.6**)<br>• Lược đồ tuần tự UC10 Từ chối ticket (**Hình 5.9**) |
| **Hồ Thanh Tình** | 25810044 | **Người dùng (User)** | `TicketController.cs`<br>`Views/Ticket/` *(Dashboard, Index, Create, Details)* | **Observer Pattern** (Bắn thông báo tức thì qua SignalR `TicketHub` & ghi nhật ký kiểm toán SystemLog) | • Lược đồ tuần tự UC03 Tạo ticket (**Hình 5.5**)<br>• Lược đồ tuần tự UC06 Chat thời gian thực (**Hình 5.8**)<br>• Lược đồ tuần tự UC04/19 Đánh giá phản hồi (**Hình 5.10**) |
| **Trần Thanh Danh** | 25810009 | **Quản trị & Bảo mật (Admin & Auth)** | `AdminController.cs`<br>`AuthController.cs`<br>`Views/Admin/`<br>`Views/Auth/` | **Decorator Pattern** (`[AuthorizeRoles]`, RBAC)<br>**Singleton Pattern** (`PasswordHasherService`) | • Lược đồ ERD CSDL 8 bảng (**Hình 3.1**)<br>• Lược đồ lớp tổng quát Class Diagram (**Hình 5.3**)<br>• Toàn bộ 5 Lược đồ Use Case tổng thể (**Hình 2.1 – 2.5**) |

---

## 5. DANH MỤC TOÀN BỘ 41 HÌNH ẢNH TRONG TÀI LIỆU WORD

### 5.1. Bảng phân loại tổng hợp theo hiện trạng

```
TỔNG SỐ HÌNH ẢNH TRONG BÁO CÁO: 41 HÌNH
├── [1] HÌNH BỊ THIẾU CẦN VẼ MỚI & CHÈN (MISSING - R2): 6 HÌNH (14.6%)
│   ├── Hình 3.1: Lược đồ thực thể – quan hệ (ERD) 8 bảng CSDL
│   ├── Hình 5.1: Quy trình xử lý ticket từ lúc tạo đến khi kết thúc (BPMN)
│   ├── Hình 5.2: Sơ đồ chuyển trạng thái của ticket (State Machine 7 states)
│   ├── Hình 5.3: Lược đồ lớp tổng quát hệ thống ASP.NET Core MVC (Class Diagram)
│   ├── Hình 5.6: Lược đồ tuần tự UC09 – Phân công yêu cầu (Sequence Diagram)
│   └── Hình 6.1: Cấu trúc lớp State Pattern cho ticket (Class Diagram)
│
├── [2] HÌNH CŨ MỜ / TÀN DƯ CẦN HIỆN ĐẠI HÓA (OUTDATED - R3): 13 HÌNH (31.7%)
│   ├── 5 hình Use Case Chương 2: Hình 2.1, 2.2, 2.3, 2.4, 2.5
│   └── 8 hình Sequence Diagram Chương 5: Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12
│
└── [3] HÌNH ẢNH GIAO DIỆN LIVE .NET 9 ĐÃ CÓ SẴN (AVAILABLE): 22 HÌNH (53.7%)
    └── Toàn bộ 22 hình giao diện thực tế Chương 4: Hình 4.1 đến Hình 4.22 (1582x848 px, PNG chuẩn)
```

---

### 5.2. Bảng kiểm kê chi tiết 41 hình ảnh

| STT | Số hiệu | Tên Caption đầy đủ | Vị trí Chương / Mục | Trang TOC | Vị trí Para (Ảnh/Cap) | Tình trạng hiện tại | Định dạng & Size tệp | Độ phân giải Pixel | Kích thước hiển thị Word | Cách nhúng XML | Ghi chú kỹ thuật |
|:---:|:---:|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| 01 | **Hình 2.1** | Hình 2.1. Lược đồ use case chính của hệ thống IT-TMS | Chương 2 / 2.2 | 10 | P357 / P358 | **OUTDATED (R3)** | JPG (179,886 B) | 813 x 1136 | 15.41 x 21.53 cm | `<wp:inline>` | Sơ đồ use case tổng thể cũ, cần vẽ lại theo 4 vai trò .NET 9 |
| 02 | **Hình 2.2** | Hình 2.2. Use case chi tiết – Actor User | Chương 2 / 2.3.1 | 10 | P362 / P363 | **OUTDATED (R3)** | PNG (68,656 B) | 812 x 613 | 16.00 x 12.08 cm | `<wp:inline>` | Sơ đồ cũ, cần đồng bộ chuẩn UML và font chữ sắc nét |
| 03 | **Hình 2.3** | Hình 2.3. Use case chi tiết – Actor Staff | Chương 2 / 2.3.2 | 10 | P365 / P366 | **OUTDATED (R3)** | PNG (55,633 B) | 812 x 550 | 16.00 x 10.84 cm | `<wp:inline>` | Sơ đồ cũ, cần đồng bộ chuẩn UML hiện đại |
| 04 | **Hình 2.4** | Hình 2.4. Use case chi tiết – Actor Leader | Chương 2 / 2.3.3 | 11 | P368 / P369 | **OUTDATED (R3)** | PNG (74,276 B) | 878 x 680 | 16.00 x 12.39 cm | `<wp:inline>` | Sơ đồ cũ, cần đồng bộ chuẩn UML hiện đại |
| 05 | **Hình 2.5** | Hình 2.5. Use case chi tiết – Actor Admin | Chương 2 / 2.3.4 | 11 | P371 / P372 | **OUTDATED (R3)** | EMF (95,184 B) | Vector EMF | 16.00 x 12.29 cm | `<wp:inline>` | File vector cũ, cần thay thế sơ đồ UML chuẩn .NET 9 |
| 06 | **Hình 3.1** | Hình 3.1. Lược đồ thực thể – quan hệ (ERD) của IT-TMS | Chương 3 / 3.1 | 22 | Không có / P439 | **MISSING (R2)** | *Chưa có* | *Chưa có* | *Chưa có* | *Thiếu node ảnh* | **BẮT BUỘC VẼ MỚI**: ERD 8 bảng CSDL C# EF Core 9 |
| 07 | **Hình 4.1** | Hình 4.1. Màn hình Đăng nhập (UC01) | Chương 4 / 4.1 | 28 | P481 / P482 | **AVAILABLE** | PNG (36,453 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 08 | **Hình 4.2** | Hình 4.2. Đăng nhập thất bại với tài khoản bị vô hiệu hóa (UC01, FR-04) | Chương 4 / 4.2 | 28 | P484 / P485 | **AVAILABLE** | PNG (45,280 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 09 | **Hình 4.3** | Hình 4.3. Từ chối truy cập trái quyền (403) (FR-03) | Chương 4 / 4.3 | 28 | P487 / P488 | **AVAILABLE** | PNG (56,370 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 10 | **Hình 4.4** | Hình 4.4. Màn hình Đổi mật khẩu (UC17) | Chương 4 / 4.4 | 29 | P490 / P491 | **AVAILABLE** | PNG (53,252 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 11 | **Hình 4.5** | Hình 4.5. Bảng tin – Dashboard của User (UC20, UC02) | Chương 4 / 4.5 | 29 | P493 / P494 | **AVAILABLE** | PNG (98,591 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 12 | **Hình 4.6** | Hình 4.6. Màn hình Tạo yêu cầu hỗ trợ (UC03) | Chương 4 / 4.6 | 30 | P496 / P497 | **AVAILABLE** | PNG (81,051 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 13 | **Hình 4.7** | Hình 4.7. Dashboard Leader – Tổng quan hoạt động (UC11) | Chương 4 / 4.7 | 30 | P499 / P500 | **AVAILABLE** | PNG (133,778 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 14 | **Hình 4.8** | Hình 4.8. Thông báo gửi Leader khi có yêu cầu mới (UC03, Observer) | Chương 4 / 4.8 | 30 | P502 / P503 | **AVAILABLE** | PNG (141,535 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 15 | **Hình 4.9** | Hình 4.9. Màn hình Phân công yêu cầu (UC09, UC10) | Chương 4 / 4.9 | 31 | P505 / P506 | **AVAILABLE** | PNG (52,379 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 16 | **Hình 4.10** | Hình 4.10. Chọn Staff để phân công yêu cầu (UC09) | Chương 4 / 4.10 | 31 | P508 / P509 | **AVAILABLE** | PNG (102,637 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 17 | **Hình 4.11** | Hình 4.11. Leader tự xử lý và phân công lại yêu cầu (UC09) | Chương 4 / 4.11 | 31 | P511 / P512 | **AVAILABLE** | PNG (105,218 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 18 | **Hình 4.12** | Hình 4.12. Dashboard Staff – hàng đợi công việc (UC20) | Chương 4 / 4.12 | 32 | P514 / P515 | **AVAILABLE** | PNG (58,881 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 19 | **Hình 4.13** | Hình 4.13. Chi tiết yêu cầu – cập nhật trạng thái (UC07, UC08) | Chương 4 / 4.13 | 32 | P517 / P518 | **AVAILABLE** | PNG (100,405 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 20 | **Hình 4.14** | Hình 4.14. Thao tác sai trạng thái bị từ chối (UC07, FR-10) | Chương 4 / 4.14 | 33 | P520 / P521 | **AVAILABLE** | PNG (108,735 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 21 | **Hình 4.15** | Hình 4.15. Chi tiết yêu cầu – khung trao đổi (UC08, UC06) | Chương 4 / 4.15 | 33 | P523 / P524 | **AVAILABLE** | PNG (100,405 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 22 | **Hình 4.16** | Hình 4.16. Màn hình Tất cả yêu cầu (UC12) | Chương 4 / 4.16 | 33 | P526 / P527 | **AVAILABLE** | PNG (66,551 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 23 | **Hình 4.17** | Hình 4.17. Màn hình Đánh giá hỗ trợ (UC04) | Chương 4 / 4.17 | 34 | P529 / P530 | **AVAILABLE** | PNG (149,703 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 24 | **Hình 4.18** | Hình 4.18. Yêu cầu đã hoàn thành – chi tiết kèm đánh giá (UC08, UC19) | Chương 4 / 4.18 | 34 | P532 / P533 | **AVAILABLE** | PNG (104,716 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 25 | **Hình 4.19** | Hình 4.19. Dashboard Admin – tổng hợp hệ thống (UC20) | Chương 4 / 4.19 | 35 | P535 / P536 | **AVAILABLE** | PNG (146,783 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 26 | **Hình 4.20** | Hình 4.20. Quản lý người dùng – tạo tài khoản, menu thao tác (UC13, UC14, UC15, UC18) | Chương 4 / 4.20 | 35 | P538 / P539 | **AVAILABLE** | PNG (160,747 B) | 1582 x 848 | 14.82 x 9.26 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 27 | **Hình 4.21** | Hình 4.21. Sửa và xóa tài khoản (UC13) | Chương 4 / 4.21 | 35 | P541 / P542 | **AVAILABLE** | PNG (55,948 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 28 | **Hình 4.22** | Hình 4.22. Nhật ký hệ thống (NFR-05, Observer) | Chương 4 / 4.22 | 36 | P544 / P545 | **AVAILABLE** | PNG (148,121 B) | 1582 x 848 | 14.82 x 7.94 cm | `<wp:inline>` | Đã chụp live .NET 9, tỉ lệ 16:10 sắc nét |
| 29 | **Hình 5.1** | Hình 5.1. Quy trình xử lý ticket từ lúc tạo đến khi kết thúc | Chương 5 / 5.2 | 37 | Không có / P551 | **MISSING (R2)** | *Chưa có* | *Chưa có* | *Chưa có* | *Thiếu node ảnh* | **BẮT BUỘC VẼ MỚI**: BPMN Activity Flow qua 7 trạng thái |
| 30 | **Hình 5.2** | Hình 5.2. Sơ đồ chuyển trạng thái của ticket | Chương 5 / 5.3 | 38 | Không có / P557 | **MISSING (R2)** | *Chưa có* | *Chưa có* | *Chưa có* | *Thiếu node ảnh* | **BẮT BUỘC VẼ MỚI**: State Machine 7 trạng thái vòng đời |
| 31 | **Hình 5.3** | Hình 5.3. Lược đồ lớp tổng quát thể hiện các mẫu thiết kế | Chương 5 / 5.4 | 39 | Không có / P560 | **MISSING (R2)** | *Chưa có* | *Chưa có* | *Chưa có* | *Thiếu node ảnh* | **BẮT BUỘC VẼ MỚI**: Class Diagram tổng thể ASP.NET Core |
| 32 | **Hình 5.4** | Hình 5.4. Lược đồ tuần tự – UC01 – Đăng nhập | Chương 5 / 5.5.1 | 39 | P564 / P565 | **OUTDATED (R3)** | EMF (85,992 B) | Vector EMF | 13.40 x 13.89 cm | `<wp:inline>` | Sequence cũ, cần vẽ lại theo AuthController & PBKDF2 C# |
| 33 | **Hình 5.5** | Hình 5.5. Lược đồ tuần tự – UC03 – Tạo yêu cầu mới | Chương 5 / 5.5.2 | 40 | P567 / P568 | **OUTDATED (R3)** | EMF (88,420 B) | Vector EMF | 16.00 x 13.24 cm | `<wp:inline>` | Sequence cũ, cần vẽ lại TicketController & SLA DueAt |
| 34 | **Hình 5.6** | Hình 5.6. Lược đồ tuần tự – UC09 – Phân công yêu cầu | Chương 5 / 5.5.3 | 40 | Không có / P570 | **MISSING (R2)** | *Chưa có* | *Chưa có* | *Chưa có* | *Thiếu node ảnh* | **BẮT BUỘC VẼ MỚI**: Sequence UC09 (LeaderController -> DB) |
| 35 | **Hình 5.7** | Hình 5.7. Lược đồ tuần tự – UC07 – Cập nhật trạng thái xử lý | Chương 5 / 5.5.4 | 41 | P572 / P573 | **OUTDATED (R3)** | EMF (106,612 B) | Vector EMF | 16.00 x 11.86 cm | `<wp:inline>` | Sequence cũ, cần vẽ lại StaffController & State Pattern |
| 36 | **Hình 5.8** | Hình 5.8. Lược đồ tuần tự – UC06 – Trao đổi thông tin ticket | Chương 5 / 5.5.5 | 42 | P575 / P576 | **OUTDATED (R3)** | PNG (130,045 B) | 934 x 575 | 16.00 x 9.85 cm | `<wp:inline>` | Sequence cũ mờ (vẽ polling 3s), **bắt buộc vẽ lại SignalR Hub** |
| 37 | **Hình 5.9** | Hình 5.9. Lược đồ tuần tự – UC10 – Từ chối yêu cầu | Chương 5 / 5.5.6 | 43 | P578 / P579 | **OUTDATED (R3)** | EMF (114,308 B) | Vector EMF | 16.00 x 11.30 cm | `<wp:inline>` | Sequence cũ, cần vẽ lại LeaderController Reject logic |
| 38 | **Hình 5.10** | Hình 5.10. Lược đồ tuần tự – UC19, UC04 – Đóng / Mở lại yêu cầu và đánh giá | Chương 5 / 5.5.7 | 44 | P581 / P582 | **OUTDATED (R3)** | JPG (175,705 B) | 1273 x 1236 | 16.00 x 15.53 cm | `<wp:inline>` | Sequence cũ mờ JPG, cần vẽ lại theo C# Feedback entity |
| 39 | **Hình 5.11** | Hình 5.11. Lược đồ tuần tự – UC15 – Kích hoạt / Vô hiệu hóa tài khoản | Chương 5 / 5.5.8 | 45 | P584 / P585 | **OUTDATED (R3)** | JPG (165,460 B) | 1526 x 1030 | 16.00 x 10.80 cm | `<wp:inline>` | Sequence cũ mờ JPG, cần vẽ lại theo AdminController |
| 40 | **Hình 5.12** | Hình 5.12. Lược đồ tuần tự – UC11 – Xem tổng quan hoạt động (Dashboard Leader) | Chương 5 / 5.5.9 | 45 | P587 / P588 | **OUTDATED (R3)** | EMF (59,900 B) | Vector EMF | 15.39 x 10.44 cm | `<wp:inline>` | Sequence cũ, cần vẽ lại LeaderDashboard ViewModel C# |
| 41 | **Hình 6.1** | Hình 6.1. Cấu trúc State Pattern cho ticket | Chương 6 / 6.2 | 47 | Không có / P598 | **MISSING (R2)** | *Chưa có* | *Chưa có* | *Chưa có* | *Thiếu node ảnh* | **BẮT BUỘC VẼ MỚI**: Class Diagram State Pattern 7 classes |

---

## 6. QUY CÁCH VÀ TIÊU CHUẨN ĐỒ HỌA ĐỒNG BỘ HÓA

Để bảo đảm tài liệu báo cáo sau khi hoàn thiện đạt chất lượng ấn phẩm xuất bản chuyên nghiệp, các sơ đồ mới (R2) và sơ đồ vẽ lại (R3) bắt buộc phải tuân thủ nghiêm ngặt các quy chuẩn sau:

1. **Chuẩn kích thước và tỉ lệ hiển thị**:
   - Đối với sơ đồ chiều ngang (Landscape / Sequence / BPMN / ERD): Chiều rộng hiển thị trong Word cố định ở **16.00 cm** (~6.3 inches), chiều cao tương ứng tỉ lệ từ 9.8 cm đến 13.5 cm để nằm gọn trong 1 trang A4, không tràn lề.
   - Đối với sơ đồ chiều dọc / hình vuông (State Machine / State Pattern Class Diagram): Chiều rộng từ **14.5 cm đến 15.5 cm**, chiều cao không vượt quá **18.0 cm** để tránh bị đẩy caption sang trang sau.
2. **Định dạng và độ phân giải hình ảnh**:
   - Xuất file ảnh ở định dạng **PNG 24-bit sắc nét** hoặc **SVG/EMF vector**.
   - Độ phân giải tối thiểu cho ảnh bitmap (PNG): Chiều rộng `>= 1600 px`, DPI `>= 300` (đảm bảo khi in ấn hoặc xem trên màn hình 4K không bị vỡ hạt hay mờ nét).
3. **Phong cách thị giác (Visual Style & Semantics)**:
   - **Nền**: Nền trắng sáng tinh khiết (`#FFFFFF`), viền khung mỏng thanh lịch (`#E2E8F0` hoặc `#CBD5E1`).
   - **Bảng màu**: Chuẩn Enterprise Modern (Primary: `#0F172A` Carbon, Secondary: `#2563EB` Royal Blue, Accent: `#10B981` Emerald, Alert: `#EF4444` Crimson).
   - **Font chữ trong sơ đồ**: San-serif đồng nhất (`Segoe UI`, `Inter`, hoặc `Arial`), kích thước chữ tối thiểu `10pt` để đọc rõ ràng các tên phương thức, tên lớp và thuộc tính.
   - **Thuật ngữ C# .NET 9**: Tên lớp, phương thức, thuộc tính phải phản ánh 100% mã nguồn thực tế (ví dụ: `TicketHub`, `ReceiveComment`, `TicketStateBase`, `NewState`, `ItTmsDbContext`, `AuthorizeRolesAttribute`, `PBKDF2`, `Result<T>`). Tuyệt đối loại bỏ tàn dư Python/Flask hoặc cơ chế polling 3s.
4. **Cách thức nhúng trong Word OpenXML**:
   - Nhúng dạng **Inline Shape** (`<wp:inline>`) đặt trong một paragraph riêng biệt nằm ngay trước paragraph chứa caption (`Chú thích hình`).
   - Căn lề paragraph chứa ảnh: **Căn giữa (Center Alignment)**.
   - Bảo toàn thuộc tính `wp:extent cx="..." cy="..."` và `a:blip r:embed="rId..."` trong `word/_rels/document.xml.rels` với namespace OpenXML chuẩn mực, tuyệt đối không sinh tiền tố lạ `ns0/ns1`.

---

## 7. KẾT LUẬN & KIẾN NGHỊ HÀNH ĐỘNG CHO ORCHESTRATOR

1. **Về Bảng PL.1**:
   - Thay thế nội dung Bảng PL.1 ở cả 2 file bằng Ma trận 4 Vertical Slices độc lập như đã định nghĩa tại Mục 4.4.
   - Xóa bỏ triệt để cụm từ "Kiểm thử tự động" và các từ khóa devops/unit test.
2. **Về 6 hình bị thiếu (R2)**:
   - Kích hoạt agent thiết kế vẽ mới 6 sơ đồ kỹ thuật: `Hình 3.1` (ERD 8 bảng), `Hình 5.1` (BPMN 7 trạng thái), `Hình 5.2` (State Machine 7 trạng thái), `Hình 5.3` (Class Diagram tổng thể), `Hình 5.6` (Sequence UC09), `Hình 6.1` (Class Diagram State Pattern).
   - Chèn đúng node `<w:p><w:r><w:drawing><wp:inline>...</wp:inline></w:drawing></w:r></w:p>` ngay trước caption tương ứng.
3. **Về 13 hình cũ mờ (R3)**:
   - Vẽ lại 5 Use Case diagrams Chương 2 (`Hình 2.1 – 2.5`) và 8 Sequence diagrams Chương 5 (`Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12`) theo đúng kiến trúc .NET 9, SignalR và State Pattern.
   - Thay thế các media file tương ứng trong `word/media/`.
4. **Về làm sạch OpenXML File 2 (`BaoCao_ITTMS_TKPMHDT_DotNet9.docx`)**:
   - Xóa bỏ 7 bảng tạm `[CHÈN HÌNH]` thừa (`Table #42, #43, #44, #45, #46, #47, #48`) để cấu trúc bảng đồng nhất 55 bảng như File 1.
   - Bảo toàn nguyên vẹn 149 paragraphs mang style `Heading *` và 136 mục TOC.
