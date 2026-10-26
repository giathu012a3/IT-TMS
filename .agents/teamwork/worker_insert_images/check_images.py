import os
import sys
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')

diagram_dir = r"c:\IT_System\docs\diagrams"

# List of the 19 target images
images_r2 = [
    ("Hình 3.1", "Hinh3_1_ERD_8Bang.png", "Lược đồ thực thể – quan hệ (ERD) của IT-TMS"),
    ("Hình 5.1", "Hinh5_1_BPMN_TicketLifecycle.png", "Quy trình xử lý ticket từ lúc tạo đến khi kết thúc"),
    ("Hình 5.2", "Hinh5_2_StateMachine_Ticket.png", "Sơ đồ chuyển trạng thái của ticket"),
    ("Hình 5.3", "Hinh5_3_ClassDiagram_MVC.png", "Lược đồ lớp tổng quát thể hiện các mẫu thiết kế"),
    ("Hình 5.6", "Hinh5_6_Sequence_UC09_PhanCong.png", "Lược đồ tuần tự – UC09 – Phân công yêu cầu"),
    ("Hình 6.1", "Hinh6_1_StatePattern_ClassDiagram.png", "Cấu trúc State Pattern cho ticket")
]

images_r3 = [
    ("Hình 2.1", "Hinh2_1_UseCaseTongThe.png", "Lược đồ use case chính của hệ thống IT-TMS"),
    ("Hình 2.2", "Hinh2_2_UseCaseUser.png", "Use case chi tiết – Actor User"),
    ("Hình 2.3", "Hinh2_3_UseCaseStaff.png", "Use case chi tiết – Actor Staff"),
    ("Hình 2.4", "Hinh2_4_UseCaseLeader.png", "Use case chi tiết – Actor Leader"),
    ("Hình 2.5", "Hinh2_5_UseCaseAdmin.png", "Use case chi tiết – Actor Admin"),
    ("Hình 5.4", "Hinh5_4_Sequence_UC01_Login.png", "Lược đồ tuần tự – UC01 – Đăng nhập"),
    ("Hình 5.5", "Hinh5_5_Sequence_UC03_CreateTicket.png", "Lược đồ tuần tự – UC03 – Tạo yêu cầu mới"),
    ("Hình 5.7", "Hinh5_7_Sequence_UC07_ProcessTicket.png", "Lược đồ tuần tự – UC07 – Cập nhật trạng thái xử lý"),
    ("Hình 5.8", "Hinh5_8_Sequence_UC06_ChatRealtime.png", "Lược đồ tuần tự – UC06 – Trao đổi thông tin ticket"),
    ("Hình 5.9", "Hinh5_9_Sequence_UC10_RejectTicket.png", "Lược đồ tuần tự – UC10 – Từ chối yêu cầu"),
    ("Hình 5.10", "Hinh5_10_Sequence_UC04_19_Feedback.png", "Lược đồ tuần tự – UC19, UC04 – Đóng / Mở lại yêu cầu và đánh giá"),
    ("Hình 5.11", "Hinh5_11_Sequence_UC13_CreateUser.png", "Lược đồ tuần tự – UC15 – Kích hoạt / Vô hiệu hóa tài khoản"),
    ("Hình 5.12", "Hinh5_12_Sequence_UC16_ConfigSLA.png", "Lược đồ tuần tự – UC11 – Xem tổng quan hoạt động (Dashboard Leader)")
]

print("=== 6 SƠ ĐỒ THIẾU (R2) ===")
for fig, filename, caption in images_r2:
    path = os.path.join(diagram_dir, filename)
    with Image.open(path) as im:
        w, h = im.size
        dpi = im.info.get('dpi', (0, 0))
        ratio = w / h
        print(f"{fig:<10} | {filename:<36} | {w}x{h} px | DPI: {dpi} | Aspect: {ratio:.2f}")

print("\n=== 13 SƠ ĐỒ CŨ CẦN THAY THẾ (R3) ===")
for fig, filename, caption in images_r3:
    path = os.path.join(diagram_dir, filename)
    with Image.open(path) as im:
        w, h = im.size
        dpi = im.info.get('dpi', (0, 0))
        ratio = w / h
        print(f"{fig:<10} | {filename:<36} | {w}x{h} px | DPI: {dpi} | Aspect: {ratio:.2f}")

print("\nKiểm tra thêm 2 file thay thế đặc thù của R3 nếu cần:")
for alt_file in ["Hinh5_11_Sequence_UC15_ToggleUser.png", "Hinh5_12_Sequence_UC11_DashboardLeader.png"]:
    path = os.path.join(diagram_dir, alt_file)
    with Image.open(path) as im:
        w, h = im.size
        dpi = im.info.get('dpi', (0, 0))
        print(f"{alt_file:<42} | {w}x{h} px | DPI: {dpi}")
