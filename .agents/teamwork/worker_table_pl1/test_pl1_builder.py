import sys
import shutil
import docx
from docx.oxml import parse_xml, OxmlElement
from docx.oxml.ns import nsdecls, qn
from docx.shared import Pt, Inches, RGBColor

sys.stdout.reconfigure(encoding='utf-8')

test_path = "c:/IT_System/test_doc1.docx"
shutil.copy2("c:/IT_System/BaoCao_ITTMS_TKPMHDT (4).docx", test_path)

doc = docx.Document(test_path)
tbl = doc.tables[-1]

# Set column widths
col_widths = [2200, 1300, 5526] # dxa, total = 9026

members_data = [
    {
        "name": "Nguyễn Gia Thụ",
        "mssv": "25810041",
        "role": "Kỹ thuật viên (Staff)",
        "code": "StaffController.cs, Views/Staff/ (Dashboard, Assigned, Details, History)",
        "pattern": "State Pattern (quản lý 7 trạng thái vòng đời ticket: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected; ngăn chặn các chuyển trạng thái vi phạm nghiệp vụ qua Result<T>)",
        "diagrams": "Sơ đồ chuyển trạng thái (State Machine – Hình 5.2), Lược đồ lớp State Pattern (Hình 6.1), Lược đồ tuần tự UC07 – Cập nhật trạng thái xử lý (Hình 5.7)",
        "contrib": "100% (Hoàn thành xuất sắc)"
    },
    {
        "name": "Nguyễn Mạnh Tiến",
        "mssv": "25810043",
        "role": "Trưởng nhóm (Leader)",
        "code": "LeaderController.cs, Views/Leader/ (Dashboard, NewQueue, Index, Details)",
        "pattern": "Factory Method / Flyweight Pattern (TicketStateFactory.cs – khởi tạo và tái sử dụng đối tượng trạng thái, tối ưu bộ nhớ)",
        "diagrams": "Quy trình xử lý ticket hoàn thiện (BPMN – Hình 5.1), Lược đồ tuần tự UC09 – Phân công yêu cầu (Hình 5.6), Lược đồ tuần tự UC10 – Từ chối yêu cầu (Hình 5.9)",
        "contrib": "100% (Hoàn thành xuất sắc)"
    },
    {
        "name": "Hồ Thanh Tình",
        "mssv": "25810044",
        "role": "Người dùng (User)",
        "code": "TicketController.cs, Views/Ticket/ (Dashboard, Index, Create, Details)",
        "pattern": "Observer Pattern (phát Domain Events, đẩy thông báo thời gian thực qua SignalR WebSocket TicketHub và tự động ghi nhật ký kiểm toán SystemLog)",
        "diagrams": "Lược đồ tuần tự UC03 – Tạo yêu cầu mới (Hình 5.5), Lược đồ tuần tự UC06 – Trao đổi thông tin ticket (Hình 5.8), Lược đồ tuần tự UC19, UC04 – Đóng/Mở lại yêu cầu và đánh giá (Hình 5.10)",
        "contrib": "100% (Hoàn thành xuất sắc)"
    },
    {
        "name": "Trần Thanh Danh",
        "mssv": "25810009",
        "role": "Quản trị & Bảo mật (Admin & Auth)",
        "code": "AdminController.cs, AuthController.cs, Views/Admin/, Views/Auth/",
        "pattern": "Decorator Pattern ([AuthorizeRoles] kiểm soát truy cập RBAC và chặn tài khoản bị vô hiệu hóa), Singleton Pattern (PasswordHasherService băm và xác thực mật khẩu PBKDF2 HMAC-SHA512)",
        "diagrams": "Lược đồ thực thể – quan hệ ERD 8 bảng CSDL (Hình 3.1), Lược đồ lớp tổng quát hệ thống (Class Diagram – Hình 5.3), Toàn bộ 5 Lược đồ Use Case tổng thể và chi tiết (Hình 2.1 – Hình 2.5)",
        "contrib": "100% (Hoàn thành xuất sắc)"
    }
]

def format_cell_text(cell, lines, bold_first_word=False):
    # clear existing paragraphs
    p = cell.paragraphs[0]
    p.text = ""
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.line_spacing = 1.15
    
    for idx, item in enumerate(lines):
        if idx > 0:
            p = cell.add_paragraph()
            p.paragraph_format.space_before = Pt(0)
            p.paragraph_format.space_after = Pt(2)
            p.paragraph_format.line_spacing = 1.15
            
        if isinstance(item, tuple):
            prefix, text = item
            r_pre = p.add_run(prefix)
            r_pre.bold = True
            r_pre.font.size = Pt(11)
            r_text = p.add_run(text)
            r_text.bold = False
            r_text.font.size = Pt(11)
        else:
            r = p.add_run(item)
            r.bold = bold_first_word
            r.font.size = Pt(11)

# Format header row
header_titles = ["Thành viên", "MSSV", "Công việc"]
for c_idx, title in enumerate(header_titles):
    cell = tbl.rows[0].cells[c_idx]
    cell.text = ""
    p = cell.paragraphs[0]
    p.alignment = docx.enum.text.WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run(title)
    r.bold = True
    r.font.size = Pt(11.5)

# Format data rows
for r_idx, data in enumerate(members_data):
    row = tbl.rows[r_idx + 1]
    
    # Col 0: Name
    c0 = row.cells[0]
    c0.text = ""
    p0 = c0.paragraphs[0]
    p0.alignment = docx.enum.text.WD_ALIGN_PARAGRAPH.LEFT
    r0 = p0.add_run(data["name"])
    r0.bold = True
    r0.font.size = Pt(11)
    
    # Col 1: MSSV
    c1 = row.cells[1]
    c1.text = ""
    p1 = c1.paragraphs[0]
    p1.alignment = docx.enum.text.WD_ALIGN_PARAGRAPH.CENTER
    r1 = p1.add_run(data["mssv"])
    r1.bold = False
    r1.font.size = Pt(11)
    
    # Col 2: Work
    c2 = row.cells[2]
    work_lines = [
        ("• Phân hệ: ", data["role"]),
        ("• Mã nguồn: ", data["code"]),
        ("• Mẫu thiết kế GoF: ", data["pattern"]),
        ("• Sơ đồ kỹ thuật: ", data["diagrams"]),
        ("• Tỷ lệ đóng góp: ", data["contrib"])
    ]
    format_cell_text(c2, work_lines)

# Apply cell widths and padding
for row in tbl.rows:
    for c_idx, w in enumerate(col_widths):
        tcPr = row.cells[c_idx]._tc.get_or_add_tcPr()
        tcW = tcPr.find(qn('w:tcW'))
        if tcW is not None:
            tcW.set(qn('w:w'), str(w))
        else:
            tcPr.append(parse_xml(f'<w:tcW {nsdecls("w")} w:w="{w}" w:type="dxa"/>'))

doc.save(test_path)
print("Saved test_doc1.docx successfully!")
