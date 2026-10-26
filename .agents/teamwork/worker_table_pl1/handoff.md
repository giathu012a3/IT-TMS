# BÁO CÁO BÀN GIAO: CHUẨN HÓA BẢNG PL.1 & LÀM SẠCH DOC2 (HANDOFF REPORT)

- **From**: Worker Table PL1 (`worker_table_pl1` / `teamwork_preview_worker`)
- **To**: Orchestrator (`3cc6747f-b9cb-4239-907a-6853ec500168`)
- **Working Directory**: `c:\IT_System\.agents\teamwork\worker_table_pl1`
- **Date**: 2026-10-26T09:42:00Z
- **Handoff Type**: Hard Handoff (Milestone 1 Complete)

---

## 1. OBSERVATION

1. **Sao lưu an toàn**:
   - Thư mục backup: `c:\IT_System\backups\backup_before_pl1_update\`
   - Tệp đã lưu:
     - `BaoCao_ITTMS_TKPMHDT (4).docx` (3,063,830 bytes)
     - `BaoCao_ITTMS_TKPMHDT_DotNet9.docx` (2,995,788 bytes)
   - Lệnh thực thi:
     `Copy-Item -Path "c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx" -Destination "c:\IT_System\backups\backup_before_pl1_update\" -Force`
     `Copy-Item -Path "c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx" -Destination "c:\IT_System\backups\backup_before_pl1_update\" -Force`

2. **Dọn dẹp 7 bảng tạm thừa `[CHÈN HÌNH]` trong Doc2**:
   - Ban đầu trong `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`: Đếm được 62 bảng (`len(doc.tables) == 62`). 7 bảng tại các vị trí index 42, 43, 44, 45, 46, 47, 48 chứa văn bản bắt đầu bằng `[CHÈN HÌNH]` (tại các mục 4.2, 4.3, 4.8, 4.11, 4.14, 4.21, 4.22).
   - Thực hiện xóa 7 phần tử `<w:tbl>` này khỏi OpenXML body.
   - Kết quả sau khi xóa: Số bảng giảm từ 62 xuống còn đúng **55 bảng**, khớp 1-to-1 với Doc1 (`BaoCao_ITTMS_TKPMHDT (4).docx` có 55 bảng).

3. **Chuẩn hóa Bảng phân công công việc Phụ lục 1 (Bảng PL.1)**:
   - Trước khi sửa: Dòng của Nguyễn Gia Thụ chứa `& Kiểm thử tự động`, phân công dính chùm ngang theo hạ tầng kỹ thuật (DB, Kiến trúc, UI), ghép 2 phân hệ vào 1 thành viên.
   - Đã cập nhật trên cả Doc1 và Doc2 thành ma trận 4 Vertical Slices độc lập:
     - Dòng 1: Nguyễn Gia Thụ (25810041) - Kỹ thuật viên (Staff), `StaffController.cs`, `Views/Staff/`, State Pattern (7 trạng thái), Sơ đồ State Machine Hình 5.2, Class Diagram State Pattern Hình 6.1, Sequence UC07 Hình 5.7, Tỷ lệ đóng góp: 100%.
     - Dòng 2: Nguyễn Mạnh Tiến (25810043) - Trưởng nhóm (Leader), `LeaderController.cs`, `Views/Leader/`, Factory Method / Flyweight Pattern, BPMN Hình 5.1, Sequence UC09 Hình 5.6, Sequence UC10 Hình 5.9, Tỷ lệ đóng góp: 100%.
     - Dòng 3: Hồ Thanh Tình (25810044) - Người dùng (User), `TicketController.cs`, `Views/Ticket/`, Observer Pattern (SignalR WebSocket `TicketHub` & SystemLog), Sequence UC03 Hình 5.5, Sequence UC06 Hình 5.8, Sequence UC04/19 Hình 5.10, Tỷ lệ đóng góp: 100%.
     - Dòng 4: Trần Thanh Danh (25810009) - Quản trị & Bảo mật (Admin & Auth), `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`, Decorator Pattern (`[AuthorizeRoles]`), Singleton Pattern (`PasswordHasherService`), ERD 8 bảng Hình 3.1, Class Diagram tổng thể Hình 5.3, Use Case tổng thể Hình 2.1-2.5, Tỷ lệ đóng góp: 100%.
   - Loại bỏ hoàn toàn: 0 từ khóa ngoài lề môn học (`unit test`, `unit-test`, `devops`, `kiểm thử tự động`).
   - Căn chỉnh độ rộng cột chuẩn: 2200 dxa, 1300 dxa, 5526 dxa (tổng 9026 dxa).

4. **Kết quả kiểm định tự động (Verification)**:
   - Script python `verify_word_docs.py`:
     - Doc1: Paragraphs = 769, Headings = 149 (Text = 136, Draw = 13), TOC = 136, Tables = 55 -> **PASS**.
     - Doc2: Paragraphs = 769, Headings = 149 (Text = 136, Draw = 13), TOC = 136, Tables = 55 -> **PASS**.
   - Microsoft Word 16.0 COM Automation (`task-134`):
     - Mở cả Doc1 và Doc2: Không phát sinh bất kỳ lỗi XML schema hay cảnh báo corruption nào (`File opened successfully! Zero XML schema / corruption errors`).
     - Word Paragraphs count: 2090 (cả 2 file trùng khớp).
     - Word Tables count: 55 (cả 2 file trùng khớp).
     - Table PL.1 Rows: 5, Cols: 3 (cả 2 file trùng khớp).

---

## 2. LOGIC CHAIN

1. **Từ Quan sát 1 (Tạo bản sao lưu)**:
   - Suy luận: Thao tác trực tiếp trên tệp Word nhị phân mang tính rủi ro One-Way Door. Việc tạo bản sao lưu tại `backups/backup_before_pl1_update/` chuyển đổi quy trình thành Two-Way Door an toàn và có thể khôi phục tức thì nếu có bất kỳ sai lệch nào.
2. **Từ Quan sát 2 (Xóa 7 bảng tạm Doc2)**:
   - Suy luận: 7 bảng tạm `[CHÈN HÌNH]` là các placeholder hướng dẫn nội bộ từ phiên bản trước đã chèn ảnh xong nhưng chưa xóa bảng. Việc xóa các bảng này ở tầng XML tree không ảnh hưởng tới các đoạn văn và hình ảnh xung quanh, giúp Doc2 đạt tính đồng nhất cấu trúc 55 bảng như Doc1.
3. **Từ Quan sát 3 (Chuẩn hóa Bảng PL.1)**:
   - Suy luận: Thay thế phân chia theo tầng ngang bằng 4 Vertical Slices độc lập đảm bảo mỗi thành viên làm chủ trọn vẹn 1 lát cắt kiến trúc (1 Vai trò = 1 Controller = 1 Mẫu thiết kế GoF = Bộ sơ đồ kỹ thuật tương ứng). Loại bỏ triệt để từ khóa DevOps/Unit Test giúp tài liệu tập trung 100% vào nghiệp vụ Thiết kế phần mềm hướng đối tượng theo chuẩn đề cương môn học. Tỷ lệ đóng góp 100% xác nhận công bằng và đầy đủ cho cả 4 thành viên.
4. **Từ Quan sát 4 (Kiểm định độc lập qua Python & Word COM)**:
   - Suy luận: Việc cả 2 công cụ (Python OpenXML parser và Microsoft Word 16.0 native engine) đều xác nhận chính xác 149 Headings, 136 mục TOC, 55 bảng và mở file không lỗi chứng minh tính toàn vẹn tuyệt đối của cấu trúc tệp tin.

---

## 3. CAVEATS

- "No caveats." Mọi mục tiêu của Milestone 1 đều đã được thực thi và kiểm chứng thực nghiệm độc lập.

---

## 4. CONCLUSION

- Nhiệm vụ Milestone 1 đã hoàn thành xuất sắc 100%:
  1. Đã sao lưu 2 file Word vào `backups/backup_before_pl1_update/`.
  2. Đã chuẩn hóa Bảng PL.1 ở cả 2 tài liệu theo 4 Vertical Slices độc lập, 100% tỷ lệ đóng góp, 0 từ khóa ngoài lề.
  3. Đã dọn sạch 7 bảng tạm thừa trong Doc2, đưa số lượng bảng ở cả 2 file về đúng 55 bảng.
  4. Đã bảo toàn nguyên vẹn 149 tiêu đề (`Heading *`) và 136 mục Mục lục (TOC).
  5. Đã xác thực thành công trên Microsoft Word 16.0 COM Automation với 0 lỗi schema.

---

## 5. VERIFICATION METHOD

Người nhận bàn giao hoặc Forensic Auditor có thể kiểm chứng độc lập kết quả bằng 2 lệnh sau:

### Cách 1: Xác minh tính toàn vẹn cấu trúc bằng Python OpenXML
```powershell
python c:\IT_System\.agents\teamwork\worker_table_pl1\verify_word_docs.py
```
*Kết quả kỳ vọng*: In ra `[VERDICT] Doc1: PASS`, `[VERDICT] Doc2: PASS` và `TẤT CẢ CÁC BÀI KIỂM THỬ XÁC MINH TRÊN PYTHON ĐỀU THÀNH CÔNG RỰC RỠ!`.

### Cách 2: Xác minh trực tiếp bằng Microsoft Word 16.0 COM Automation
```powershell
$word = New-Object -ComObject Word.Application; $word.Visible = $false
try {
    foreach ($f in @("c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx", "c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx")) {
        $doc = $word.Documents.Open($f, [Type]::Missing, $true)
        Write-Host "File $f opened: Tables =" $doc.Tables.Count ", PL.1 Rows =" $doc.Tables.Item($doc.Tables.Count).Rows.Count
        $doc.Close([ref]$false)
    }
} finally {
    $word.Quit(); [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
```
*Kết quả kỳ vọng*: Cả 2 file đều mở mượt mà, trả về `Tables = 55 , PL.1 Rows = 5`.
