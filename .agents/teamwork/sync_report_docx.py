import shutil
import zipfile
import xml.etree.ElementTree as ET
import os
import re

DOCX_PATH = r"C:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx"
ALT_DOCX_PATH = r"C:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx"
BACKUP_PATH = r"C:\IT_System\.agents\teamwork\BaoCao_ITTMS_TKPMHDT (4).backup.docx"

W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ns = {"w": W_NS}


def get_p_text(p_elem):
    return "".join(t.text for t in p_elem.iter(f"{{{W_NS}}}t") if t.text)


def set_p_text(p_elem, new_text):
    """Replace text inside a paragraph while preserving its w:pPr and first run's w:rPr."""
    pPr = p_elem.find(f"{{{W_NS}}}pPr")
    first_r = p_elem.find(f"{{{W_NS}}}r")
    first_rPr = first_r.find(f"{{{W_NS}}}rPr") if first_r is not None else None

    # Remove all child elements except w:pPr
    for child in list(p_elem):
        if child.tag != f"{{{W_NS}}}pPr":
            p_elem.remove(child)

    r_elem = ET.SubElement(p_elem, f"{{{W_NS}}}r")
    if first_rPr is not None:
        r_elem.append(first_rPr)
    t_elem = ET.SubElement(r_elem, f"{{{W_NS}}}t")
    if new_text.startswith(" ") or new_text.endswith(" ") or "  " in new_text:
        t_elem.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
    t_elem.text = new_text


def set_cell_text(tc_elem, new_text):
    """Set text of a table cell <w:tc>, preserving <w:tcPr> and first paragraph's <w:pPr>/<w:rPr>."""
    paragraphs = tc_elem.findall(f"{{{W_NS}}}p")
    if not paragraphs:
        p = ET.SubElement(tc_elem, f"{{{W_NS}}}p")
        set_p_text(p, new_text)
        return
    # Keep first paragraph, remove extra paragraphs if any
    set_p_text(paragraphs[0], new_text)
    for extra_p in paragraphs[1:]:
        tc_elem.remove(extra_p)


def update_table_row(tbl_elem, row_idx, cell_values):
    rows = tbl_elem.findall(f"{{{W_NS}}}tr")
    if row_idx >= len(rows):
        return
    cells = rows[row_idx].findall(f"{{{W_NS}}}tc")
    for c_idx, val in enumerate(cell_values):
        if c_idx < len(cells) and val is not None:
            set_cell_text(cells[c_idx], val)


def main():
    if not os.path.exists(BACKUP_PATH):
        shutil.copy2(DOCX_PATH, BACKUP_PATH)
        print(f"Created backup at: {BACKUP_PATH}")

    # Always read from clean backup so script is idempotent
    with zipfile.ZipFile(BACKUP_PATH, "r") as zin:
        xml_bytes = zin.read("word/document.xml")
        all_files = {name: zin.read(name) for name in zin.namelist()}

    orig_xml_str = xml_bytes.decode("utf-8")
    open_tag_match = re.match(r"^.*?<w:document[^>]*>", orig_xml_str, re.DOTALL)
    assert open_tag_match is not None, "Could not find <w:document> opening tag"
    orig_preamble_and_open_tag = open_tag_match.group(0)

    # Register 100% of namespaces declared anywhere in the document (including embedded DrawingML a:, pic:, etc.)
    all_ns_pairs = []
    seen_prefixes = set()
    for prefix, uri in re.findall(r'xmlns:([A-Za-z0-9_-]+)="([^"]+)"', orig_xml_str):
        ET.register_namespace(prefix, uri)
        if prefix not in seen_prefixes:
            seen_prefixes.add(prefix)
            all_ns_pairs.append((prefix, uri))

    # Ensure orig_preamble_and_open_tag declares all namespaces (including child DrawingML namespaces moved to root by ET)
    extra_ns_decls = ""
    for prefix, uri in all_ns_pairs:
        if f'xmlns:{prefix}="' not in orig_preamble_and_open_tag:
            extra_ns_decls += f' xmlns:{prefix}="{uri}"'
    if extra_ns_decls:
        orig_preamble_and_open_tag = orig_preamble_and_open_tag[:-1] + extra_ns_decls + ">"

    root = ET.fromstring(xml_bytes)
    body = root.find("w:body", ns)
    tables = root.findall(".//w:tbl", ns)

    # 1. Update Cover Table 0 & Table 61 (Team members & Work assignment)
    update_table_row(tables[0], 3, ["Hồ Thanh Tình", "25810044"])
    update_table_row(tables[0], 4, ["Trần Thanh Danh", "25810009"])

    update_table_row(tables[61], 1, [
        "Nguyễn Gia Thụ",
        "25810041",
        "Kiến trúc Clean Architecture (.NET 9), Domain Model, State Pattern (7 trạng thái), SignalR Real-time Hub & Kiểm thử tự động"
    ])
    update_table_row(tables[61], 2, [
        "Nguyễn Mạnh Tiến",
        "25810043",
        "Thiết kế CSDL EF Core 9 (SQLite), Migrations, Concurrency Token, Observer Pattern (Notification & SystemLog)"
    ])
    update_table_row(tables[61], 3, [
        "Hồ Thanh Tình",
        "25810044",
        "Phân hệ Leader & Staff (MVC Areas), Bộ lọc phân quyền RBAC/Anti-BOLA (Decorator Pattern), SLA Dashboard"
    ])
    update_table_row(tables[61], 4, [
        "Trần Thanh Danh",
        "25810009",
        "Phân hệ User & Admin (MVC Areas), Giao diện Bootstrap 5 (Dark/Light Mode), Đính kèm tệp, Đánh giá Feedback & Tài liệu báo cáo"
    ])

    # 2. Update Table 10 (Bảng 1.7 - Yêu cầu phi chức năng)
    update_table_row(tables[10], 1, [
        "NFR-01",
        "Hiệu suất & Thời gian thực",
        "Phản hồi tức thì qua kết nối WebSocket hai chiều (ASP.NET Core SignalR Hub /hubs/ticket) cho bình luận và thông báo đẩy, kết hợp AJAX/Fetch ở phân công, lọc, dashboard"
    ])
    update_table_row(tables[10], 2, [
        "NFR-02",
        "Bảo mật",
        "Mật khẩu được băm chuẩn PBKDF2-SHA256 kèm Salt 128-bit (PasswordHasherService); bảo vệ route bằng ActionFilter kiểm tra đăng nhập, trạng thái Active và RBAC/Anti-BOLA; Admin không thể tự vô hiệu hóa chính mình"
    ])
    update_table_row(tables[10], 4, [
        "NFR-04",
        "Bảo trì & mở rộng",
        "Kiến trúc Clean Architecture kết hợp ASP.NET Core MVC 5 Areas; thêm trạng thái/quy tắc mới qua State & Observer Pattern mà ít ảnh hưởng mã hiện có"
    ])

    # 3. Update Chapter 3 Database Tables (Tables 32-41)
    update_table_row(tables[32], 0, [
        "Lược đồ ERD chuẩn hóa gồm 8 bảng thực thể trong EF Core 9 (SQLite): Users, TicketStatuses, Tickets, Comments, Feedbacks, Attachments, Notifications, SystemLogs. "
        "Tất cả khóa chính (PK), khóa ngoại (FK), ràng buộc duy nhất (UNIQUE), chỉ mục bao phủ (Covering Indexes) và ConcurrencyToken được cấu hình qua Fluent API (IEntityTypeConfiguration<T>). "
        "Quan hệ: User 1–n Ticket (CreatorId); User 1–n Ticket (AssignedToId, nullable); TicketStatus 1–n Ticket; Ticket 1–n Comment; Ticket 1–n Attachment; Ticket 1–0..1 Feedback; User 1–n Comment; User 1–n Notification; User 1–n SystemLog."
    ])
    update_table_row(tables[33], 3, [
        "PasswordHash",
        "String(256)",
        "NOT NULL",
        "Mật khẩu đã băm PBKDF2-SHA256 kèm Salt (PasswordHasherService)"
    ])
    update_table_row(tables[35], 9, [
        "DueAt",
        "DateTime",
        "NOT NULL",
        "Hạn hoàn tất theo SLA (Critical: 1h, High: 4h, Medium: 24h, Low: 72h)"
    ])
    update_table_row(tables[35], 12, [
        "UpdatedAt / ConcurrencyToken",
        "DateTime / Guid",
        "NOT NULL, ConcurrencyCheck",
        "Thời điểm cập nhật cuối (kèm ResolvedAt, ClosedAt) và Token chống xung đột ghi đồng thời"
    ])
    update_table_row(tables[36], 4, [
        "ColorClass / SortOrder",
        "String(50) / Int",
        "NOT NULL",
        "Lớp màu CSS hiển thị huy hiệu (Bootstrap 5 badge) và thứ tự sắp xếp"
    ])
    update_table_row(tables[37], 2, [
        "FileName",
        "String(255)",
        "NOT NULL",
        "Tên tệp vật lý lưu trên máy chủ (UUID mã hóa tránh trùng lặp)"
    ])
    update_table_row(tables[37], 3, [
        "OriginalFileName / ContentType / FileSize",
        "String(255) / String(100) / Long",
        "NOT NULL, <= 16MB",
        "Tên tệp gốc, định dạng MIME type và dung lượng tệp (bytes)"
    ])
    update_table_row(tables[41], 6, [
        "Nghiệp vụ – trạng thái",
        "Ticket chỉ chuyển trạng thái theo đồ thị hợp lệ (Hình 5.2); ticket Resolved/Closed/Rejected không được bình luận; ticket Closed/Rejected bị khóa hoàn toàn",
        "Tầng Domain (State Pattern – 7 lớp trạng thái)"
    ])
    update_table_row(tables[41], 7, [
        "Nghiệp vụ – phân quyền",
        "User chỉ xem ticket của mình; Staff chỉ xem ticket được giao; chỉ Leader được phân công/từ chối; chỉ Admin quản lý tài khoản; Admin không tự vô hiệu hóa chính mình; tài khoản inactive bị chặn tức thì",
        "AuthorizeRolesAttribute + AuthorizeTicketAccessAttribute"
    ])
    update_table_row(tables[41], 10, [
        "Toàn vẹn giao dịch & Đồng thời",
        "Đổi trạng thái, ghi thông báo và ghi nhật ký cùng thực thi trong một giao dịch ACID của EF Core kèm kiểm tra ConcurrencyToken: lỗi thì rollback toàn bộ",
        "EF Core ItTmsDbContext (SaveChangesAsync)"
    ])

    # 4. Update Chapter 5 Diagram boxes (Tables 49, 51, 52, 53)
    update_table_row(tables[49], 0, [
        "Quy trình xử lý ticket khép kín trên 4 làn (Swimlane): User / Leader / Staff / Hệ thống (.NET 9 Backend & SignalR). "
        "User tạo ticket -> Hệ thống gán trạng thái New, tính SLA DueAt và bắn sự kiện TicketCreatedEvent -> Leader nhận thông báo thời gian thực: "
        "nếu không hợp lệ -> Reject (nhập lý do) -> Rejected; nếu hợp lệ -> Assign cho Staff -> Assigned. "
        "Staff tiếp nhận (StartProgress -> InProgress), trao đổi qua SignalR WebSocket (chuyển Waiting khi cần bổ sung thông tin) -> Resolve -> Resolved. "
        "User nghiệm thu: hài lòng -> CloseAndRate (1-5 sao) -> Closed; chưa hài lòng -> Reopen -> InProgress. Mọi bước đều tự động lưu Notification và SystemLog."
    ])
    update_table_row(tables[51], 0, [
        "Sơ đồ chuyển trạng thái vòng đời Ticket gồm 7 trạng thái trong CSDL và Domain: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected. "
        "Các phép chuyển hợp lệ: [*] -> New (Ticket.Create, User); New -> Assigned (Assign, Leader); New -> Rejected (Reject, Leader); "
        "Assigned -> InProgress (StartProgress, Staff); Assigned / InProgress / Waiting -> Assigned (Reassign, Leader); "
        "InProgress <-> Waiting (HoldForInfo / StartProgress, Staff & User phản hồi); InProgress / Waiting -> Resolved (Resolve, Staff); "
        "Resolved -> Closed (Close / CloseAndRate, User); Resolved -> InProgress (Reopen, User). Closed và Rejected là trạng thái kết thúc."
    ])
    update_table_row(tables[52], 0, [
        "Lược đồ lớp tổng quát theo kiến trúc Clean Architecture (.NET 9): "
        "Ticket (Aggregate Root / Context + Subject), ITicketState & TicketStateBase (State abstraction), 7 lớp trạng thái cụ thể, "
        "IDomainEventObserver<T> (Observer interface), NotificationObserver, SystemLogObserver, SignalRNotificationDispatcher, "
        "User, TicketStatus, Comment, Feedback, Attachment, Notification, SystemLog, "
        "AuthorizeRolesAttribute & AuthorizeTicketAccessAttribute (Decorator / ActionFilter), PasswordHasherService & TicketStateFactory (Singleton), "
        "và hệ thống Controllers trong 5 Areas MVC (Auth, User, Staff, Leader, Admin)."
    ])
    update_table_row(tables[53], 0, [
        "Lược đồ tuần tự UC09 – Phân công yêu cầu (LeaderTicketController.Assign): "
        "1. Leader -> LeaderTicketController: POST /Leader/LeaderTicket/Assign (TicketId, StaffId) qua bộ lọc [AuthorizeRoles(RoleNames.Leader)]. "
        "2. LeaderTicketController -> ItTmsDbContext: Tải thực thể Ticket (kèm Status) và kiểm tra Staff hợp lệ. "
        "3. LeaderTicketController -> Ticket (Context): Gọi ticket.Assign(staffId, leaderId). "
        "4. Ticket (Context) -> CurrentState (NewState): Ủy quyền thực thi Assign(staffId, leaderId). "
        "[alt: Ticket không ở trạng thái New] -> Trả về Result.Failure(TicketErrors.InvalidStateTransition), giữ nguyên dữ liệu và báo lỗi. "
        "[else: Ticket đang ở trạng thái New] -> Cập nhật AssignedToId = staffId, StatusId = TicketStatus.Assigned, phát sinh TicketAssignedEvent, trả về Result.Success(). "
        "5. LeaderTicketController -> ItTmsDbContext: Gọi SaveChangesAsync() -> Kích hoạt NotificationObserver & SystemLogObserver trong cùng giao dịch ACID và đẩy thông báo tức thì qua SignalRNotificationDispatcher."
    ])

    # 5. Update Chapter 6 Tables (Tables 54, 55, 56, 58)
    update_table_row(tables[54], 1, [
        "State",
        "Hành vi",
        "Vòng đời ticket: phân công, điều chuyển, từ chối, cập nhật trạng thái, đóng/mở lại, quyền bình luận (UC06, 07, 09, 10, 19)",
        "Ticket (Context), ITicketState, TicketStateBase, TicketStateFactory, NewState, AssignedState, InProgressState, WaitingState, ResolvedState, ClosedState, RejectedState"
    ])
    update_table_row(tables[54], 2, [
        "Observer",
        "Hành vi",
        "Tự động tạo thông báo đẩy thời gian thực (SignalR) và ghi nhật ký kiểm toán khi ticket thay đổi (FR-10, NFR-05)",
        "Ticket (Subject), IDomainEvent, IDomainEventObserver<T>, NotificationObserver, SystemLogObserver, SignalRNotificationDispatcher"
    ])
    update_table_row(tables[54], 3, [
        "Decorator",
        "Cấu trúc",
        "Bảo vệ Controller/Action: bắt buộc đăng nhập, kiểm tra tài khoản Active thời gian thực, đúng vai trò RBAC và chống BOLA/IDOR (FR-03, FR-04)",
        "AuthorizeRolesAttribute, AuthorizeTicketAccessAttribute (IAsyncActionFilter)"
    ])
    update_table_row(tables[54], 4, [
        "Singleton",
        "Khởi tạo",
        "Các dịch vụ băm mật khẩu, kiểm tra xung đột đồng thời và nhà máy trạng thái dùng chung toàn ứng dụng qua DI Container",
        "PasswordHasherService, ConcurrencyTokenInterceptor, TicketStateFactory (Flyweight Singleton)"
    ])
    update_table_row(tables[54], 5, [
        "MVC (+ Areas)",
        "Kiến trúc",
        "Tổ chức toàn hệ thống theo Clean Architecture & ASP.NET Core MVC chia thành 5 phân vùng (Areas) theo vai trò",
        "Domain Entities, ViewModels, Razor Views (Bootstrap 5), 5 Areas: Auth, User, Staff, Leader, Admin"
    ])

    update_table_row(tables[55], 0, [
        "Cấu trúc State Pattern của Ticket trong C# (.NET 9): "
        "Ticket (Context) sở hữu thuộc tính CurrentState (khôi phục qua TicketStateFactory từ StatusId) và các phương thức nghiệp vụ trả về Result: "
        "Assign(staffId, leaderId), Reassign(staffId, leaderId), Reject(reason, leaderId), StartProgress(staffId), HoldForInfo(reason, staffId), Resolve(staffId), Close(feedback), Reopen(reason), CanComment(). "
        "Giao diện ITicketState và lớp cơ sở trừu tượng TicketStateBase định nghĩa hành vi mặc định trả về Result.Failure(TicketErrors.InvalidStateTransition). "
        "7 lớp trạng thái cụ thể (NewState, AssignedState, InProgressState, WaitingState, ResolvedState, ClosedState, RejectedState) kế thừa TicketStateBase và chỉ ghi đè các thao tác hợp lệ."
    ])

    update_table_row(tables[56], 1, [
        "Context",
        "Ticket (src/ItTms.Domain/Entities/Ticket.cs) – giữ CurrentState hiện tại, ủy quyền toàn bộ thao tác Assign/Reject/StartProgress/HoldForInfo/Resolve/Close/Reopen/CanComment cho đối tượng State"
    ])
    update_table_row(tables[56], 2, [
        "State",
        "ITicketState & TicketStateBase (src/ItTms.Domain/States/) – giao diện và lớp cơ sở chung; mặc định chặn mọi thao tác không hợp lệ bằng Result.Failure(TicketErrors.InvalidStateTransition)"
    ])
    update_table_row(tables[56], 3, [
        "ConcreteState",
        "NewState, AssignedState, InProgressState, WaitingState, ResolvedState, ClosedState, RejectedState – chỉ ghi đè các phép chuyển trạng thái hợp lệ và phát sinh Domain Events"
    ])

    update_table_row(tables[58], 1, [
        "State",
        "StaffTicketController, LeaderTicketController, User/TicketController & ItTms.Domain/States/*.cs",
        "Gán trực tiếp ticket.StatusId = ... và kiểm tra rải rác bằng nhiều lệnh if/switch trong Controller",
        "Controller gọi ticket.Assign(), ticket.StartProgress(), ticket.Resolve(), ticket.Close(), ticket.Reject(); các lớp ConcreteState kiểm soát hợp lệ và trả về Result<T>"
    ])
    update_table_row(tables[58], 2, [
        "Observer",
        "ItTms.Domain/Events/*.cs, NotificationObserver.cs, SystemLogObserver.cs, SignalRNotificationDispatcher.cs",
        "Mỗi Action trong Controller phải tự tạo đối tượng Notification và SystemLog thủ công gây trùng lặp mã",
        "Ticket phát sinh DomainEvent khi đổi trạng thái; ItTmsDbContext.SaveChangesAsync tự động kích hoạt NotificationObserver, SystemLogObserver và đẩy thông báo qua SignalR"
    ])
    update_table_row(tables[58], 3, [
        "Decorator",
        "src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs & AuthorizeTicketAccessAttribute.cs",
        "Mỗi hàm Action phải tự viết lệnh kiểm tra User.Identity, kiểm tra trạng thái Active và kiểm tra Role",
        "Gắn thuộc tính [AuthorizeRoles(...)] ở mức BaseController của từng Area và [AuthorizeTicketAccess] cho chi tiết ticket; tự động chặn tài khoản Inactive và trả về HTTP 403 khi trái quyền"
    ])
    update_table_row(tables[58], 4, [
        "Singleton",
        "src/ItTms.Infrastructure/DependencyInjection.cs & TicketStateFactory.cs",
        "Khởi tạo lại dịch vụ băm mật khẩu và đối tượng trạng thái ở nhiều nơi gây tốn bộ nhớ",
        "Đăng ký AddSingleton<IPasswordHasherService, PasswordHasherService>(), ConcurrencyTokenInterceptor và dùng Flyweight Singleton trong TicketStateFactory"
    ])
    update_table_row(tables[58], 5, [
        "MVC + Areas",
        "src/ItTms.Web/Areas/{Auth,User,Staff,Leader,Admin} & Clean Architecture 4 tầng",
        "Trộn lẫn truy vấn CSDL, quy tắc nghiệp vụ và xử lý giao diện trong cùng một lớp",
        "Tách biệt rõ Domain – Application – Infrastructure – Web (5 Areas MVC); nghiệp vụ nằm trọn trong Domain Model"
    ])

    # 6. Update Chapter 7 Tables: Table 59 (Bảng 7.1) & Table 60 (Bảng 7.2)
    update_table_row(tables[59], 1, ["Ngôn ngữ & Runtime", "C# 13 / .NET 9 SDK (net9.0)"])
    update_table_row(tables[59], 2, ["Web Framework & Thời gian thực", "ASP.NET Core 9 MVC (5 Areas) & ASP.NET Core SignalR WebSockets (TicketHub)"])
    update_table_row(tables[59], 3, ["CSDL / ORM", "SQLite (WAL Mode), Entity Framework Core 9, EF Core Migrations, ConcurrencyToken"])
    update_table_row(tables[59], 4, ["Bảo mật & Xác thực", "PBKDF2-SHA256 (PasswordHasherService), Cookie Authentication, AuthorizeRoles & Anti-BOLA Filters"])
    update_table_row(tables[59], 5, ["Giao diện & Kiểm thử", "Razor Views (.cshtml) + Bootstrap 5 (Dark/Light Mode) + xUnit Automated Test Suite (336 tests)"])

    # Table 60 (Bảng 7.2 - 10 kịch bản kiểm thử nghiệm thu)
    tc_results = [
        "Đạt (Pass) – AuthController & AuthorizeRolesAttribute chặn đăng nhập, hiển thị thông báo 'Tài khoản của bạn đã bị vô hiệu hóa' và ghi SystemLog (TC01 passed)",
        "Đạt (Pass) – AuthorizeRolesAttribute trả về HTTP 403 Forbidden (trang AccessDenied) khi User truy cập /Leader/* (TC02 passed)",
        "Đạt (Pass) – Ticket tạo mới ở trạng thái New, tự động tính hạn SLA DueAt; NotificationObserver gửi thông báo cho tất cả Leader đang Active (TC03 passed)",
        "Đạt (Pass) – Ticket chuyển sang trạng thái Assigned; cả Staff được giao và User tạo ticket đều nhận thông báo tức thì qua NotificationObserver & SignalR (TC04 passed)",
        "Đạt (Pass) – NewState từ chối phép chuyển sang Closed/Resolved (trả về Result.Failure), dữ liệu giữ nguyên trạng thái New trong CSDL (TC05 passed)",
        "Đạt (Pass) – InProgressState chặn thao tác Reject của Leader (trả về lỗi INVALID_STATE_TRANSITION), ticket giữ nguyên trạng thái InProgress (TC06 passed)",
        "Đạt (Pass) – AuthorizeTicketAccessAttribute và TicketHub chặn Staff không được phân công, trả về HTTP 403 Forbidden và ghi cảnh báo BOLA_Violation_Attempt (TC07 passed)",
        "Đạt (Pass) – ResolvedState.CanComment trả về false; khung bình luận bị khóa trên giao diện và bị chặn tại Controller/TicketHub, không lưu vào CSDL (TC08 passed)",
        "Đạt (Pass) – Ticket chuyển sang Closed, lưu bản ghi Feedback (1–5 sao) vào bảng Feedbacks và ghi nhận vào bảng SystemLogs (TC09 passed)",
        "Đạt (Pass) – Tài khoản bị Admin vô hiệu hóa bị thu hồi phiên và không thể đăng nhập; thao tác Admin tự vô hiệu hóa chính mình bị chặn hoàn toàn (TC10 passed)"
    ]
    for idx, res_text in enumerate(tc_results, start=1):
        rows = tables[60].findall(f"{{{W_NS}}}tr")
        cells = rows[idx].findall(f"{{{W_NS}}}tc")
        set_cell_text(cells[3], res_text)

    # 7. Update Body Paragraphs by exact index / content matching
    p_updates = {
        324: (
            "Hệ thống được xây dựng trên nền tảng .NET 9 (C# 13) theo kiến trúc Clean Architecture kết hợp ASP.NET Core MVC (5 Areas), "
            "Entity Framework Core 9 ORM (SQLite), giao tiếp thời gian thực hai chiều qua SignalR WebSockets và giao diện Razor Views + Bootstrap 5. "
            "Trong báo cáo này, nhóm phân tích và thiết kế hệ thống theo hướng đối tượng, đặc biệt chú trọng các mẫu thiết kế (design pattern) giúp phần mềm linh động và dễ mở rộng: "
            "vòng đời của ticket được thiết kế bằng State Pattern, kết hợp Observer, Decorator, Singleton và MVC Areas."
        ),
        469: (
            "Hệ thống dùng cơ sở dữ liệu quan hệ SQLite (tương thích hoàn toàn với SQL Server), ánh xạ qua Entity Framework Core 9 (EF Core 9) ORM. "
            "Thực thể trung tâm là Ticket, liên kết với User (người tạo và người được phân công), Comment, Feedback, Attachment và TicketStatus; "
            "ngoài ra có Notification (thông báo) và SystemLog (nhật ký hệ thống) để đảm bảo tính minh bạch và truy vết kiểm toán."
        ),
        516: (
            "Khóa chính, khóa ngoại, UNIQUE, chỉ mục bao phủ (Covering Indexes) và ConcurrencyToken đã khai báo trong các lớp cấu hình Fluent API (IEntityTypeConfiguration<T>) của EF Core 9. "
            "Các ràng buộc miền giá trị và UNIQUE trên Feedback.TicketId được quản lý qua EF Core Migrations và kiểm tra chặt chẽ ở tầng Domain Model."
        ),
        521: (
            "Giao diện render phía server bằng ASP.NET Core Razor Views kết hợp Bootstrap 5 (responsive, hỗ trợ giao diện Sáng/Tối), "
            "bố cục gồm thanh điều hướng phân tách theo 5 Areas vai trò (Đổi mật khẩu, Đăng xuất), bộ lọc tìm kiếm, khung chat trực tuyến và chuông thông báo thời gian thực SignalR. "
            "Các hình dưới đây minh họa các màn hình chính của hệ thống."
        ),
        655: (
            "Tích hợp với Domain Model hiện có: Ticket vẫn lưu trạng thái ở cột StatusId (liên kết bảng TicketStatuses), "
            "thuộc tính Ticket.CurrentState tra cứu StatusId thông qua TicketStateFactory (áp dụng Flyweight) để trả về đối tượng ITicketState tương ứng. "
            "Nhờ vậy cấu trúc CSDL hoàn toàn tách biệt với logic hành vi."
        ),
        656: "Mã minh họa (C# / .NET 9, rút gọn từ src/ItTms.Domain/States/):",
        657: "public abstract class TicketStateBase : ITicketState {",
        658: "    public abstract int StatusId { get; }",
        659: "    public abstract string StatusName { get; }",
        660: "    public virtual bool CanComment => true;",
        661: "    public virtual Result Assign(Ticket t, int staffId, int leaderId) => Fail(\"phân công\");",
        662: "    public virtual Result Reject(Ticket t, string reason, int leaderId) => Fail(\"từ chối\");",
        663: "    public virtual Result StartProgress(Ticket t, int staffId) => Fail(\"bắt đầu xử lý\");",
        664: "    public virtual Result Resolve(Ticket t, int staffId) => Fail(\"giải quyết\");",
        665: "    public virtual Result Close(Ticket t, Feedback fb) => Fail(\"đóng\");",
        666: "    protected Result Fail(string action) =>",
        667: "        Result.Failure(TicketErrors.InvalidStateTransition(StatusName, action));",
        668: "}",
        669: "public sealed class NewState : TicketStateBase {",
        670: "    public override int StatusId => TicketStatus.New;",
        671: "    public override string StatusName => \"New\";",
        672: "    public override Result Assign(Ticket ticket, int staffId, int leaderId) {",
        673: "        ticket.SetAssignedTo(staffId, TicketStatus.Assigned);",
        674: "        ticket.AddDomainEvent(new TicketAssignedEvent(ticket.Id, staffId, leaderId, DateTime.UtcNow));",
        675: "        return Result.Success();",
        676: "    }",
        677: "}",
        678: "public sealed class ResolvedState : TicketStateBase {",
        679: "    public override int StatusId => TicketStatus.Resolved; public override string StatusName => \"Resolved\";",
        680: "    public override bool CanComment => false; // Chặn bình luận khi đã Resolved (TC-08)",
        690: "Ánh xạ với ORM: EF Core 9 lưu StatusId dưới dạng khóa ngoại số nguyên, còn CurrentState là thuộc tính [NotMapped] được dựng qua TicketStateFactory nên đồng bộ tức thì khi nạp/lưu.",
        693: (
            "Khi ticket được tạo, phân công, đổi trạng thái, bị từ chối, đóng hoặc mở lại, các bên liên quan phải nhận được thông báo tức thì (FR-10) và thao tác phải được ghi vào nhật ký (NFR-05). "
            "Ticket đóng vai trò Subject phát sinh các sự kiện miền (IDomainEvent); NotificationObserver tạo bản ghi Notification cho đúng người nhận và phối hợp cùng SignalRNotificationDispatcher đẩy thông báo qua WebSocket; "
            "SystemLogObserver tự động ghi bản ghi SystemLog (hành động, người thực hiện, chi tiết, thời gian)."
        ),
        694: "public class NotificationObserver : IDomainEventObserver<TicketAssignedEvent> {",
        695: "    private readonly IItTmsDbContext _db;",
        696: "    public NotificationObserver(IItTmsDbContext db) => _db = db;",
        697: "    public async Task HandleAsync(TicketAssignedEvent ev, CancellationToken ct = default) {",
        698: "        var staffNotif = Notification.Create(ev.AssignedToId, $\"Bạn được giao Ticket #{ev.TicketId}\", $\"/Staff/StaffTicket/Details/{ev.TicketId}\").Value;",
        699: "        await _db.Notifications.AddAsync(staffNotif, ct);",
        700: "        // Thực thi trong cùng giao dịch SaveChangesAsync của ItTmsDbContext",
        701: "    }\n}",
        712: (
            "FR-03 và FR-04: mọi route phải kiểm tra người dùng đã đăng nhập, tài khoản còn ở trạng thái Active trong thời gian thực và đúng vai trò "
            "(ví dụ chỉ Leader được phân công/từ chối, chỉ Admin quản lý người dùng, Staff chỉ xem được ticket giao cho mình). "
            "Hệ thống triển khai các thuộc tính bộ lọc ActionFilter trong ASP.NET Core: [AuthorizeRoles] và [AuthorizeTicketAccess]."
        ),
        713: "[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]",
        714: "public class AuthorizeRolesAttribute : Attribute, IAsyncActionFilter {",
        715: "    private readonly string[] _allowedRoles;",
        716: "    public AuthorizeRolesAttribute(params string[] roles) => _allowedRoles = roles;",
        717: "    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next) {",
        718: "        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);",
        719: "        if (user == null || user.Status != UserStatus.Active) { HandleInactiveAccount(ctx); return; }",
        720: "        if (!_allowedRoles.Contains(user.Role.ToString(), StringComparer.OrdinalIgnoreCase)) { HandleForbidden(ctx); return; }",
        721: "        await next();",
        722: "    }\n}",
        723: "[Area(\"Leader\")]",
        724: "[AuthorizeRoles(RoleNames.Leader, RoleNames.Admin)]",
        725: "public abstract class LeaderBaseController : BaseController { }",
        731: "Thứ tự thực thi trong pipeline Middleware/Filter của ASP.NET Core cần được cấu hình chuẩn xác (UseAuthentication → UseAuthorization → ActionFilter).",
        732: "Kiểm tra quyền sở hữu theo từng tài nguyên (User chỉ xem ticket của mình, Staff chỉ xem ticket được phân công) được tách thành bộ lọc chuyên biệt [AuthorizeTicketAccess] truy vấn trực tiếp TicketId từ RouteData.",
        735: (
            "Các dịch vụ dùng chung không lưu trạng thái phiên như PasswordHasherService (băm mật khẩu PBKDF2-SHA256), ConcurrencyTokenInterceptor (kiểm soát xung đột đồng thời) "
            "và TicketStateFactory (bộ đệm các đối tượng trạng thái Flyweight) được đăng ký với vòng đời Singleton trong Dependency Injection Container của ASP.NET Core và dùng chung toàn ứng dụng."
        ),
        737: "Chỉ khởi tạo một thể hiện duy nhất cho các dịch vụ dùng chung (PasswordHasherService, TicketStateFactory), tối ưu hóa bộ nhớ cấp phát (Zero-allocation cho State).",
        738: "Mọi Area (Auth, User, Staff, Leader, Admin) đều tiêm (inject) cùng một cấu hình dịch vụ thông qua Constructor Injection chuẩn mực.",
        740: "Nếu lạm dụng Singleton cho các đối tượng giữ trạng thái (stateful) sẽ gây xung đột đa luồng; do đó ItTmsDbContext được đăng ký Scoped theo từng HTTP Request, chỉ các dịch vụ không trạng thái mới dùng Singleton.",
        741: "Việc tiêm phụ thuộc qua giao diện (IPasswordHasherService, IItTmsDbContext) trong .NET 9 giúp khắc phục hoàn toàn hạn chế che giấu phụ thuộc và hỗ trợ viết Unit Test độc lập dễ dàng.",
        744: (
            "Toàn bộ hệ thống được tổ chức theo kiến trúc Clean Architecture kết hợp ASP.NET Core MVC: "
            "Model (Domain Entities & ViewModels) – View (Razor Views .cshtml + Bootstrap 5) – Controller. "
            "Tầng Web được chia module theo đúng 5 vai trò bằng cơ chế ASP.NET Core MVC Areas (Auth, User, Staff, Leader, Admin), đóng vai trò tương đương với khái niệm Blueprint trong thiết kế phân vùng."
        ),
        747: "Mỗi Area (Auth, User, Staff, Leader, Admin) tương ứng một phân hệ độc lập của một vai trò, có BaseController và thư mục Views riêng, rất dễ định vị và mở rộng chức năng.",
        749: "Controller dễ phình to nếu đặt quy tắc nghiệp vụ trong Action; nhóm khắc phục triệt để bằng cách đưa toàn bộ quy tắc chuyển trạng thái vào Domain Model (State Pattern) và xử lý thông báo qua Observer.",
        750: "Razor Views chỉ hiển thị nút bấm dựa trên thuộc tính Model.CanComment và trạng thái từ State Pattern, đảm bảo đồng bộ hoàn toàn giữa giao diện và tầng nghiệp vụ.",
        752: "Bảng sau chỉ rõ vị trí trong mã nguồn (.NET 9 Clean Architecture) nơi từng mẫu thiết kế được áp dụng và sự cải tiến so với cách viết thủ công ban đầu. Đây là cơ sở để đối chiếu trực tiếp với mã nguồn dự án.",
        759: "Backend (Clean Architecture & MVC): Xây dựng trên .NET 9 và ASP.NET Core MVC với 5 Areas (Auth, User, Staff, Leader, Admin); xác thực bằng Cookie Authentication kết hợp PBKDF2-SHA256; Entity Framework Core 9 (SQLite WAL mode) làm ORM kèm cơ chế tự động Migration và Seeding khi khởi động.",
        760: "Frontend (View): Render phía server bằng công nghệ Razor Views (.cshtml), tích hợp Bootstrap 5 (responsive, hỗ trợ chế độ Sáng/Tối và bộ biểu tượng SVG nội bộ không phụ thuộc CDN bên ngoài).",
        761: "Thời gian thực hai chiều (Real-time SignalR WebSockets): Khung trao đổi bình luận và quả chuông thông báo sử dụng ASP.NET Core SignalR Hub (/hubs/ticket) kết nối WebSocket liên tục theo nhóm phòng (ticket_{id}, user_{id}, role_{role}), thay thế hoàn toàn cơ chế polling 3 giây gây tốn tài nguyên.",
        764: "ItTms.sln (.NET 9 Clean Architecture)",
        765: "├── src/",
        766: "│   ├── ItTms.Domain/            # Tầng Domain: Thực thể cốt lõi & Quy tắc nghiệp vụ",
        767: "│   │   ├── Entities/            # 8 thực thể: User, Ticket, TicketStatus, Comment, Feedback, Attachment, Notification, SystemLog",
        768: "│   │   ├── States/              # * State Pattern: ITicketState, TicketStateBase, TicketStateFactory + 7 lớp trạng thái",
        769: "│   │   └── Events/              # * Domain Events: TicketCreated, TicketAssigned, TicketStatusChanged, CommentAdded...",
        770: "│   ├── ItTms.Application/       # Tầng Application: Giao diện & Observers",
        771: "│   │   └── Observers/           # * Observer Pattern: NotificationObserver, SystemLogObserver",
        772: "│   ├── ItTms.Infrastructure/    # Tầng Infrastructure: EF Core 9 SQLite, Security & Seed",
        773: "│   │   ├── Persistence/         # ItTmsDbContext, Configurations, Migrations, ConcurrencyTokenInterceptor",
        774: "│   │   └── Security/            # PasswordHasherService (Singleton PBKDF2-SHA256)",
        775: "│   └── ItTms.Web/               # Tầng Trình diễn: ASP.NET Core MVC & SignalR",
        776: "│       ├── Areas/               # * 5 Areas: Auth, User, Staff, Leader, Admin (Controllers + Razor Views)",
        777: "│       ├── Filters/             # * Decorator Pattern: AuthorizeRolesAttribute, AuthorizeTicketAccessAttribute",
        778: "│       └── Hubs/                # * Real-time WebSocket: TicketHub (/hubs/ticket) & SignalRNotificationDispatcher",
        784: "Nhóm thực hiện kiểm thử kết hợp cả kiểm thử tự động bằng bộ 336 bài test xUnit (bao gồm bộ kiểm thử đầu-cuối Table72VerificationTests) và kiểm thử thực nghiệm trên giao diện Web, tập trung vào phân quyền RBAC/Anti-BOLA, quy tắc chuyển trạng thái State Pattern, thông báo thời gian thực SignalR và tính toàn vẹn dữ liệu.",
        791: "Cài đặt: hiện thực hóa trọn vẹn thiết kế thành ứng dụng thực tế bằng C# (.NET 9), ASP.NET Core MVC với cấu trúc 5 Areas, EF Core 9 SQLite và SignalR WebSockets; giao diện Bootstrap 5 hiện đại, responsive.",
        795: "Yêu cầu phi chức năng: thời gian thực (SignalR WebSocket hai chiều triệt tiêu polling), bảo mật (băm mật khẩu PBKDF2-SHA256, bảo vệ route bằng AuthorizeRoles & AuthorizeTicketAccess), khả dụng (UI Bootstrap 5 thân thiện), bảo trì (Clean Architecture 4 tầng).",
        803: "Đã xây dựng bộ 336 bài kiểm thử tự động (xUnit Unit Tests, Integration Tests, Concurrency Stress Tests, SignalR Tests và 10 kịch bản E2E Bảng 7.2); hướng tới mở rộng thêm kiểm thử giao diện tự động bằng Playwright.",
        812: "[2] Microsoft Learn, ASP.NET Core 9 MVC & SignalR Real-time Documentation, https://learn.microsoft.com/aspnet/core, 2025.",
        813: "[3] Microsoft Learn, Entity Framework Core 9 Documentation, https://learn.microsoft.com/ef/core, 2025.",
        817: "Cơ sở dữ liệu & Migrations: CSDL SQLite (ittms.db) được khởi tạo và nạp dữ liệu mẫu tự động thông qua EF Core 9 Migrations & DataSeeder khi khởi chạy ứng dụng."
    }

    for p_idx, new_val in p_updates.items():
        if p_idx < len(body) and body[p_idx].tag == f"{{{W_NS}}}p":
            set_p_text(body[p_idx], new_val)

    # Also scan all paragraphs for any stray legacy terms in non-heading paragraphs
    for i, elem in enumerate(body):
        if elem.tag != f"{{{W_NS}}}p":
            continue
        txt = get_p_text(elem)
        if not txt:
            continue
        pStyle = elem.find(".//w:pStyle", ns)
        style_val = pStyle.attrib.get(f"{{{W_NS}}}val", "") if pStyle is not None else ""
        # Never touch headings or TOC entries!
        if style_val.startswith("Heading") or style_val.startswith("TOC"):
            continue
        updated_txt = txt
        if "user.id == current_user.id" in updated_txt:
            updated_txt = updated_txt.replace("user.id == current_user.id", "user.Id == CurrentUserId")
        if "gọi API /leader/dashboard?time_range=… qua AJAX" in updated_txt:
            updated_txt = updated_txt.replace("/leader/dashboard?time_range=…", "/Leader/LeaderDashboard?days=…")
        if updated_txt != txt:
            set_p_text(elem, updated_txt)

    # Serialize modified XML and restore exact original XML declaration + <w:document ...> opening tag
    serialized_str = ET.tostring(root, encoding="unicode")
    serialized_body_onwards = re.sub(r"^<w:document[^>]*>", "", serialized_str, count=1, flags=re.DOTALL)
    final_xml_str = orig_preamble_and_open_tag + serialized_body_onwards

    assert "xmlns:ns" not in final_xml_str and "<ns0:" not in final_xml_str and "<ns1:" not in final_xml_str, "Synthetic namespace prefix detected!"
    new_xml_bytes = final_xml_str.encode("utf-8")
    all_files["word/document.xml"] = new_xml_bytes

    with zipfile.ZipFile(ALT_DOCX_PATH, "w", zipfile.ZIP_DEFLATED) as zout:
        for name, content in all_files.items():
            zout.writestr(name, content)
    print(f"Successfully synchronized report to: {ALT_DOCX_PATH}")

    try:
        with zipfile.ZipFile(DOCX_PATH, "w", zipfile.ZIP_DEFLATED) as zout:
            for name, content in all_files.items():
                zout.writestr(name, content)
        print(f"Successfully synchronized report in-place to: {DOCX_PATH}")
    except PermissionError:
        print(f"NOTE: {DOCX_PATH} is currently open in Microsoft Word; saved clean copy at {ALT_DOCX_PATH}")


if __name__ == "__main__":
    main()
