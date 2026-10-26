# Tiến độ Công việc - Worker Diagrams R3

- **Trạng thái**: Hoàn tất 100% thiết kế mã nguồn PlantUML và render ảnh PNG 300 DPI
- **Bắt đầu**: 2026-10-26T09:28:45Z
- **Hoàn thành**: 2026-10-26T09:38:30Z
- **Cập nhật gần nhất**: 2026-10-26T09:38:30Z

## Bảng theo dõi tiến độ chi tiết:
| # | Tên tệp sơ đồ | Loại sơ đồ | Phân hệ / Actor | Kích thước Pixel | DPI | Trạng thái |
|---|---------------|------------|-----------------|------------------|-----|------------|
| 1 | `Hinh2_1_UseCaseTongThe` | Use Case | Tổng thể (4 Actors: User, Staff, Leader, Admin) | 3077 x 4096 | 300 | HOÀN TẤT |
| 2 | `Hinh2_2_UseCaseUser` | Use Case | Phân hệ Người dùng (User) | 2612 x 2059 | 300 | HOÀN TẤT |
| 3 | `Hinh2_3_UseCaseLeader` | Use Case | Phân hệ Trưởng nhóm (Leader) | 4096 x 2059 | 300 | HOÀN TẤT |
| 4 | `Hinh2_4_UseCaseStaff` | Use Case | Phân hệ Kỹ thuật viên (Staff) | 4096 x 2309 | 300 | HOÀN TẤT |
| 5 | `Hinh2_5_UseCaseAdmin` | Use Case | Phân hệ Quản trị & Bảo mật (Admin) | 4096 x 2152 | 300 | HOÀN TẤT |
| 6 | `Hinh5_4_Sequence_UC01_Login` | Sequence | UC01 Đăng nhập & Xác thực (`AuthController`) | 4096 x 3633 | 300 | HOÀN TẤT |
| 7 | `Hinh5_5_Sequence_UC03_CreateTicket` | Sequence | UC03 Tạo yêu cầu SLA (`TicketController`, Observer) | 4096 x 3582 | 300 | HOÀN TẤT |
| 8 | `Hinh5_7_Sequence_UC07_ProcessTicket` | Sequence | UC07 Tiếp nhận & Chuyển InProgress (`StaffController`) | 4096 x 3310 | 300 | HOÀN TẤT |
| 9 | `Hinh5_8_Sequence_UC06_ChatRealtime` | Sequence | UC06 Chat thời gian thực (`TicketController`, `TicketHub`) | 4096 x 2975 | 300 | HOÀN TẤT |
| 10 | `Hinh5_9_Sequence_UC10_RejectTicket` | Sequence | UC10 Từ chối yêu cầu (`LeaderController`, TC-06 Check) | 4096 x 3276 | 300 | HOÀN TẤT |
| 11 | `Hinh5_10_Sequence_UC04_19_Feedback` | Sequence | UC04/19 Nghiệm thu & Đóng (`TicketController`, TC-09) | 4096 x 3217 | 300 | HOÀN TẤT |
| 12 | `Hinh5_11_Sequence_UC13_CreateUser` | Sequence | UC13/14 Tạo người dùng mới (`AdminController`, Decorator) | 4096 x 2597 | 300 | HOÀN TẤT |
| 13 | `Hinh5_11_Sequence_UC15_ToggleUser` | Sequence | UC15 Bật/tắt trạng thái User (`AdminController`, TC-10) | 4096 x 3115 | 300 | HOÀN TẤT (Alias) |
| 14 | `Hinh5_12_Sequence_UC16_ConfigSLA` | Sequence | UC16 Giám sát & Quản lý SLA (`LeaderController`) | 4096 x 2634 | 300 | HOÀN TẤT |
| 15 | `Hinh5_12_Sequence_UC11_DashboardLeader` | Sequence | UC11 Dashboard chỉ huy Leader (`LeaderController`) | 4096 x 2700 | 300 | HOÀN TẤT (Alias) |

- **Kết quả nghiệm thu**: 15 file `.puml` và 15 file `.png` sắc nét chuẩn in ấn 300 DPI sẵn sàng để Milestone 4 chèn/thay thế vào 2 file DOCX.
