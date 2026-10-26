# HANDOFF REPORT: KHẢO SÁT TOÀN DIỆN TÀI LIỆU VÀ HÌNH ẢNH IT-TMS

- **From**: Explorer Survey 2 (`teamwork_preview_explorer`)
- **To**: Orchestrator (`3cc6747f-b9cb-4239-907a-6853ec500168`)
- **Working Directory**: `c:\IT_System\.agents\teamwork\explorer_survey_2`
- **Date**: 2026-10-26T09:28:00Z
- **Handoff Type**: Hard Handoff (Investigation & Survey Complete)

---

## 1. OBSERVATION

1. **Khảo sát File 1 (`c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`, 3,063,830 bytes) và File 2 (`c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`, 2,995,788 bytes)**:
   - Cả 2 tài liệu đều có chính xác **769 đoạn văn (paragraphs)**.
   - Cả 2 tài liệu đều có chính xác **149 paragraphs** mang style `Heading *` (14 `Heading 1`, 62 `Heading 2`, 73 `Heading 3`).
   - Phân tích XML phát hiện:
     - **136 đoạn** chứa text tiêu đề thực tế.
     - **13 đoạn** mang style `Heading 2` hoặc `Heading 3` nhưng text rỗng (`text = ""`), chứa phần tử drawing inline `<wp:inline>` làm container cho các hình vẽ (P357 cho Hình 2.1, P362 cho Hình 2.2, P365 cho Hình 2.3, P368 cho Hình 2.4, P371 cho Hình 2.5, P564 cho Hình 5.4, P567 cho Hình 5.5, P572 cho Hình 5.7, P575 cho Hình 5.8, P578 cho Hình 5.9, P581 cho Hình 5.10, P584 cho Hình 5.11, P587 cho Hình 5.12).
   - Danh mục Mục lục (TOC, P60 đến P195) chứa chính xác **136 mục**, khớp 1-to-1 với 136 tiêu đề có text (mismatch count = 0).
   - Danh mục Hình (P198 đến P238) liệt kê chính xác **41 hình ảnh** (từ `Hình 2.1` đến `Hình 6.1`; hoàn toàn không có `Hình 1.x`).
   - Danh mục Bảng (P241 đến P287) liệt kê chính xác **47 bảng** (từ `Bảng 1.1` đến `Bảng PL.1`).

2. **Khảo sát Bảng phân công Phụ lục 1 (Bảng PL.1)**:
   - Vị trí: Bảng thứ 55 trong File 1, Bảng thứ 62 trong File 2 (nằm dưới tiêu đề `BẢNG PHÂN CÔNG CÔNG VIỆC` tại P766).
   - Kích thước: 5 dòng x 3 cột (`Thành viên`, `MSSV`, `Công việc`).
   - Nội dung nguyên văn hiện tại:
     - Dòng 1: `['Nguyễn Gia Thụ', '25810041', 'Kiến trúc Clean Architecture (.NET 9), Domain Model, State Pattern (7 trạng thái), SignalR Real-time Hub & Kiểm thử tự động']`
     - Dòng 2: `['Nguyễn Mạnh Tiến', '25810043', 'Thiết kế CSDL EF Core 9 (SQLite), Migrations, Concurrency Token, Observer Pattern (Notification & SystemLog)']`
     - Dòng 3: `['Hồ Thanh Tình', '25810044', 'Phân hệ Leader & Staff (MVC Areas), Bộ lọc phân quyền RBAC/Anti-BOLA (Decorator Pattern), SLA Dashboard']`
     - Dòng 4: `['Trần Thanh Danh', '25810009', 'Phân hệ User & Admin (MVC Areas), Giao diện Bootstrap 5 (Dark/Light Mode), Đính kèm tệp, Đánh giá Feedback & Tài liệu báo cáo']`
   - Khiếm khuyết trực quan:
     - Dính "Kiểm thử tự động" ở dòng của Nguyễn Gia Thụ.
     - Phân tầng ngang (Kiến trúc, DB, Controller, UI) thay vì phân hệ độc lập.
     - Ghép 2 phân hệ vào 1 thành viên (`Leader & Staff` cho Tình; `User & Admin` cho Danh), trong khi Thụ và Tiến không làm chủ phân hệ Controller nào.

3. **Khảo sát toàn bộ 41 hình ảnh trong tài liệu Word**:
   - **6 hình BỊ THIẾU (MISSING - R2)**:
     - `Hình 3.1` (P439): Lược đồ thực thể – quan hệ (ERD) của IT-TMS (Chương 3 / Mục 3.1). Hoàn toàn không có node ảnh trước caption.
     - `Hình 5.1` (P551): Quy trình xử lý ticket từ lúc tạo đến khi kết thúc (Chương 5 / Mục 5.2). Hoàn toàn không có node ảnh trước caption.
     - `Hình 5.2` (P557): Sơ đồ chuyển trạng thái của ticket (Chương 5 / Mục 5.3). Hoàn toàn không có node ảnh trước caption.
     - `Hình 5.3` (P560): Lược đồ lớp tổng quát thể hiện các mẫu thiết kế (Chương 5 / Mục 5.4). Hoàn toàn không có node ảnh trước caption.
     - `Hình 5.6` (P570): Lược đồ tuần tự – UC09 – Phân công yêu cầu (Chương 5 / Mục 5.5.3). Hoàn toàn không có node ảnh trước caption.
     - `Hình 6.1` (P598): Cấu trúc State Pattern cho ticket (Chương 6 / Mục 6.2). Hoàn toàn không có node ảnh trước caption.
   - **13 hình CŨ MỜ / TÀN DƯ CHƯA ĐỒNG BỘ C# .NET 9 (OUTDATED - R3)**:
     - 5 hình Use Case Chương 2: `Hình 2.1` (JPG, 813x1136), `Hình 2.2` (PNG, 812x613), `Hình 2.3` (PNG, 812x550), `Hình 2.4` (PNG, 878x680), `Hình 2.5` (Vector EMF, 95KB).
     - 8 hình Sequence Diagrams Chương 5: `Hình 5.4` (Vector EMF, 85KB), `Hình 5.5` (Vector EMF, 88KB), `Hình 5.7` (Vector EMF, 106KB), `Hình 5.8` (PNG, 934x575), `Hình 5.9` (Vector EMF, 114KB), `Hình 5.10` (JPG mờ, 1273x1236), `Hình 5.11` (JPG mờ, 1526x1030), `Hình 5.12` (Vector EMF, 59KB). Các hình này chưa thể hiện đúng tên Controller, Action, SignalR Hub và cấu trúc mã nguồn .NET 9.
   - **22 hình GIAO DIỆN LIVE .NET 9 ĐÃ CÓ SẴN (AVAILABLE)**:
     - `Hình 4.1` đến `Hình 4.22`: Đã chụp live thực tế từ ứng dụng ASP.NET Core MVC trên trình duyệt, định dạng PNG độ nét cao (1582 x 848 px, tỉ lệ 16:10), kích thước hiển thị Word ~14.82 x 9.26 cm, nhúng dạng `<wp:inline>`.
   - **Sự khác biệt bảng giữa File 1 và File 2**:
     - File 1 có 55 bảng, File 2 có 62 bảng.
     - File 2 còn tồn tại 7 bảng tạm `[CHÈN HÌNH]` thừa (`Table #42, #43, #44, #45, #46, #47, #48`) tại các mục 4.2, 4.3, 4.8, 4.11, 4.14, 4.21, 4.22. File 1 đã dọn sạch các bảng tạm này.

---

## 2. LOGIC CHAIN

1. **Từ quan sát về số lượng Headings (149 paragraphs) và TOC (136 mục)**:
   - Suy luận 1: Word đếm style `Heading *` thì thấy 149 đoạn văn. Tuy nhiên, 13 đoạn văn trong số đó thực chất là nơi đặt hình vẽ inline (do tác giả ban đầu gán style Heading cho dòng chứa ảnh).
   - Suy luận 2: Word chỉ hiển thị lên TOC những đoạn Heading có chứa text (`text.strip() != ""`). Do đó, TOC thực tế hiển thị chuẩn xác 136 mục mà không bị lỗi dòng trống.
   - Kết luận logic: Yêu cầu "Bảo toàn 100% 149 tiêu đề" đồng nghĩa với việc giữ nguyên toàn bộ 769 paragraphs và 149 styles Heading này, không được xóa bừa các dòng paragraph rỗng chứa ảnh.

2. **Từ quan sát Bảng PL.1**:
   - Suy luận 1: Việc phân chia việc theo hạ tầng (Domain, DB, Controller, UI) làm phá vỡ tính độc lập của đồ án. Khi Giảng viên hỏi "Controller của em đâu? Phân hệ nào em chịu trách nhiệm chính?", sinh viên sẽ không trả lời được vì mã nguồn bị dính chùm.
   - Suy luận 2: Cụm từ "Kiểm thử tự động" thuộc về DevOps/Testing, không nằm trong chuẩn mực báo cáo môn Thiết kế Phần mềm Hướng Đối tượng (HCMUTE).
   - Kết luận logic: Bắt buộc phải tái cấu trúc Bảng PL.1 thành 4 phân hệ dọc độc lập (Vertical Slices): Nguyễn Gia Thụ (Staff + State Pattern), Nguyễn Mạnh Tiến (Leader + Factory Pattern), Hồ Thanh Tình (User + Observer SignalR), Trần Thanh Danh (Admin/Auth + Decorator/Singleton), loại bỏ sạch từ khóa unit test.

3. **Từ quan sát danh mục 41 hình ảnh**:
   - Suy luận 1: 22 hình ảnh Chương 4 đã hoàn thành ở mức độ xuất sắc (ảnh PNG thực tế, chuẩn 16:10, 1582x848 px). Không cần chụp lại Chương 4.
   - Suy luận 2: 6 vị trí (Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1) hoàn toàn chưa có thẻ `<w:drawing>` nào trong XML, chỉ có dòng caption đứng bơ vơ. Cần tạo mới hình ảnh và chèn cặp `<w:p><w:r><w:drawing><wp:inline>...</wp:inline></w:drawing></w:r></w:p>` ngay trước caption.
   - Suy luận 3: 13 vị trí (Hình 2.1-2.5 và Hình 5.4, 5.5, 5.7-5.12) đã có sẵn container và file ảnh, nhưng nội dung ảnh cũ mờ, sai lệch kiến trúc .NET 9 (đặc biệt Hình 5.8 đang vẽ polling thay vì WebSocket SignalR). Cần tạo sơ đồ mới và thay thế media binary.

4. **Từ quan sát sự chênh lệch giữa File 1 và File 2**:
   - File 1 (`BaoCao_ITTMS_TKPMHDT (4).docx`) có cấu trúc bảng sạch hơn (55 bảng, không có bảng tạm thừa). File 2 (`BaoCao_ITTMS_TKPMHDT_DotNet9.docx`) bị thừa 7 bảng tạm `[CHÈN HÌNH]`.
   - Khi cập nhật Word, cần xóa sạch 7 bảng tạm thừa trong File 2 để cấu trúc XML đạt độ sạch 100% như File 1.

---

## 3. CAVEATS

1. Không thực hiện bất kỳ thao tác ghi hay sửa đổi nào lên file `.docx` trong phạm vi nhiệm vụ của Explorer (tuân thủ nghiêm ngặt nguyên tắc Read-only Explorer).
2. Toàn bộ thông số đo đạc về kích thước hiển thị trong Word được tính toán trực tiếp từ các thuộc tính `cx, cy` (EMU) trong OpenXML (`1 cm = 360,000 EMU`).
3. Mọi dữ liệu kiểm kê đã được lưu trữ vĩnh viễn và chi tiết tại `c:\IT_System\.agents\teamwork\explorer_survey_2\document_inventory.md`.

---

## 4. CONCLUSION

- Nhiệm vụ khảo sát toàn diện 2 tài liệu Word đã hoàn tất 100% với độ chính xác pháp y:
  1. Xác nhận và giải mã chính xác 149 tiêu đề (`Heading *`), tính toàn vẹn 136 mục TOC trên cả 2 tài liệu Word.
  2. Định vị, trích xuất và vạch rõ khiếm khuyết của Bảng PL.1; thiết lập ma trận chuẩn hóa 4 Vertical Slices độc lập (R1).
  3. Lập danh mục chi tiết toàn bộ 41 hình ảnh (vị trí, hiện trạng, định dạng, pixel, kích thước hiển thị Word, cách nhúng XML) và phân loại dứt khoát: 6 hình thiếu (R2), 13 hình cũ mờ (R3), 22 hình live UI có sẵn.
  4. Phát hiện bất thường 7 bảng tạm thừa trong File 2 và đưa ra giải pháp dọn dẹp OpenXML triệt để.

---

## 5. VERIFICATION METHOD

Để kiểm chứng độc lập toàn bộ kết quả khảo sát, người nhận bàn giao có thể chạy script python sau tại project root `c:\IT_System`:

```powershell
python -c "
import docx, zipfile, xml.etree.ElementTree as ET

for f in ['BaoCao_ITTMS_TKPMHDT (4).docx', 'BaoCao_ITTMS_TKPMHDT_DotNet9.docx']:
    doc = docx.Document(f)
    headings = [p for p in doc.paragraphs if 'Heading' in p.style.name]
    text_headings = [p for p in headings if p.text.strip()]
    draw_headings = [p for p in headings if not p.text.strip()]
    print(f'{f}: Paras={len(doc.paragraphs)}, Tables={len(doc.tables)}, Headings={len(headings)} (Text={len(text_headings)}, Draw={len(draw_headings)})')
"
```

**Kết quả mong đợi**:
- Cả 2 file đều trả về: `Paras=769, Headings=149 (Text=136, Draw=13)`.
- File 1 trả về: `Tables=55`.
- File 2 trả về: `Tables=62` (chênh lệch 7 bảng tạm thừa `[CHÈN HÌNH]`).
- Báo cáo chi tiết kiểm tra tại: `c:\IT_System\.agents\teamwork\explorer_survey_2\document_inventory.md`.
