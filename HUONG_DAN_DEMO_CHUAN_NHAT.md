# 🚀 HƯỚNG DẪN DEMO TÍNH NĂNG CHUẨN NHẤT (CHUẨN BỊ 1 LẦN - QUAY LÀ ĐƯỢC NGAY)

> **Mục tiêu**: Demo mượt mà từ đầu đến cuối, không vấp, không lỗi, phô diễn trọn vẹn 5 tính năng cốt lõi trong **5 – 6 phút**.

---

## 🛠️ BƯỚC 0: CHUẨN BỊ TRƯỚC KHI QUAY (1 PHÚT)

### 1. Khởi động ứng dụng
Mở Terminal gõ:
```powershell
dotnet run --project src/ItTms --urls=http://localhost:5050
```

### 2. Mở sẵn 2 cửa sổ trình duyệt (Chia đôi màn hình)
- **Cửa sổ 1 (Bình thường)**: Mở `http://localhost:5050` -> Bấm phím **`Win + Mũi tên Trái ⬅️`** (chiếm nửa trái màn hình).
- **Cửa sổ 2 (Ẩn danh - Ctrl+Shift+N)**: Mở `http://localhost:5050` -> Bấm phím **`Win + Mũi tên Phải ➡️`** (chiếm nửa phải màn hình).

> 💡 *Lúc bắt đầu quay, bạn chỉ cần phóng to Cửa sổ 1 lên toàn màn hình. Đến Cảnh 4 (SignalR) thì bấm lại `Win + Trái/Phải` là xong!*

### 3. Bảng tài khoản (Quy tắc mật khẩu: Viết hoa chữ đầu + `@123`)
* **User**: `user1` / `User@123`
* **Leader**: `leader` / `Leader@123`
* **Staff**: `staff1` / `Staff@123`
* **Admin**: `admin` / `Admin@123`

---

## 🎬 5 BƯỚC DEMO CHUẨN XÁC (CLICK ĐÂU - NÓI ĐÓ)

---

### BƯỚC 1: GIAO DIỆN & TẠO YÊU CẦU (Phút 00:00 – 01:30)
* **Thao tác chuột**:
  1. Ở Cửa sổ 1: Bấm nút **Mặt trăng / Mặt trời** góc trên bên phải để khoe tính năng đổi Theme Sáng/Tối.
  2. Đăng nhập: `user1` / `User@123`.
  3. Bấm nút **"Tạo yêu cầu mới"** (nút xanh ở Dashboard).
  4. Nhập nhanh:
     - Tiêu đề: `"Lỗi máy trạm kế toán không kết nối được phần mềm MISA"`
     - Danh mục: `Phần mềm`
     - Độ ưu tiên: `Cao` -> Rê chuột vào ô bên dưới: hệ thống tự tính SLA hoàn thành trong **24 giờ**.
     - Mô tả: `"MISA báo lỗi Network Timeout."`
     - Đính kèm: Chọn 1 ảnh bất kỳ trên máy.
  5. Bấm **"Gửi yêu cầu hỗ trợ"**.
  6. Màn hình chuyển vào chi tiết ticket: Chỉ chuột vào huy hiệu màu xanh **`New`**.
* **Lời nói (chỉ 2 câu)**:
  > *"Hệ thống hỗ trợ chuyển đổi linh hoạt giao diện Sáng - Tối. Người dùng tạo yêu cầu hỗ trợ với đầy đủ danh mục, mức ưu tiên, mô tả và tệp đính kèm. Hệ thống sẽ tự động tính toán thời hạn cam kết SLA và đưa ticket vào trạng thái New."*

---

### BƯỚC 2: TRƯỞNG NHÓM PHÂN CÔNG (Phút 01:30 – 02:30)
* **Thao tác chuột**:
  1. Bấm Đăng xuất, đăng nhập tài khoản: `leader` / `Leader@123`.
  2. Vào menu **"Hàng đợi tiếp nhận"** (thanh menu trên cùng).
  3. Tìm dòng ticket sự cố MISA vừa tạo -> Bấm nút xanh **"Phân công"**.
  4. Chọn kỹ thuật viên: `staff1` -> Bấm nút **"Xác nhận Giao việc"**.
  5. Trạng thái ticket đổi sang **`Assigned`**.
* **Lời nói (chỉ 2 câu)**:
  > *"Trưởng nhóm IT theo dõi các yêu cầu mới tại Hàng đợi tiếp nhận và nắm bắt khối lượng công việc của từng kỹ thuật viên. Tại đây, trưởng nhóm tiến hành phân công ticket cho Kỹ thuật viên 1 phụ trách."*

---

### BƯỚC 3: [ĐỈNH NHẤT] CHAT REAL-TIME SIGNALR (Phút 02:30 – 04:30)
> *Bấm `Win + Mũi tên Trái` (Cửa sổ 1) và `Win + Mũi tên Phải` (Cửa sổ 2 để ẩn danh)*

* **Thao tác chuột**:
  - **Cửa sổ Trái (User)**: Đăng nhập lại `user1` / `User@123` -> Mở chi tiết ticket lúc nãy.
  - **Cửa sổ Phải (Staff)**: Đăng nhập `staff1` / `Staff@123` -> Mở đúng chi tiết ticket đó.
  1. **Nhận việc**: Bên Staff (Phải) bấm nút vàng **"Bắt đầu Tiếp nhận"**.
     -> Chỉ chuột sang bên User (Trái): Trạng thái tự nhảy sang **`In Progress`** tức thì (0ms, không cần F5).
  2. **Chat 2 chiều**:
     - Staff (Phải) gõ: *"Chào bạn, bạn kiểm tra lại địa chỉ IP máy chủ giúp mình nhé."* -> Enter.
     - User (Trái) nhận tin ngay -> Gõ trả lời: *"Dạ em kiểm tra rồi, đúng IP rồi ạ."* -> Enter.
  3. **Ghi chú nội bộ**:
     - Staff (Phải) tích vào ô **"Ghi chú nội bộ"** -> Gõ: *"Cần lên phòng kế toán kiểm tra trực tiếp switch mạng."* -> Gửi.
     - So sánh 2 bên: Bên Staff có tin nhắn viền vàng nội bộ, bên User **hoàn toàn không thấy**.
  4. **Giải quyết**:
     - Staff (Phải) bấm nút xanh **"Đã giải quyết"** -> Bấm OK.
     - Cả 2 bên tự đổi sang trạng thái **`Resolved`** và khung chat tự động khóa lại.
* **Lời nói (chỉ 2-3 câu)**:
  > *"Đây là tính năng tương tác thời gian thực bằng SignalR WebSocket. Khi kỹ thuật viên bấm tiếp nhận hoặc gửi tin nhắn, phía người dùng sẽ nhận được ngay lập tức mà không cần tải lại trang. Kỹ thuật viên cũng có thể gửi các ghi chú nội bộ để trao đổi riêng mà người dùng không nhìn thấy. Khi sự cố được xử lý xong, hệ thống chuyển sang Resolved và tự động khóa khung bình luận."*

---

### BƯỚC 4: NGHIỆM THU ĐÓNG TICKET (Phút 04:30 – 05:15)
* **Thao tác chuột**:
  1. Phóng to Cửa sổ Trái (User `user1`) lên toàn màn hình.
  2. Ở phần đánh giá: Bấm chọn **5 sao ⭐⭐⭐⭐⭐**, gõ nhận xét: *"Hỗ trợ rất nhanh, cảm ơn IT!"*.
  3. Bấm **"Xác nhận & Đóng ticket"**.
  4. Trạng thái chuyển thành màu đen **`Closed`**.
* **Lời nói (chỉ 1 câu)**:
  > *"Người dùng kiểm tra kết quả khắc phục sự cố, đánh giá mức độ hài lòng 5 sao và đóng ticket để hoàn tất trọn vẹn quy trình."*

---

### BƯỚC 5: QUẢN TRỊ ADMIN & NHẬT KÝ KIỂM TOÁN (Phút 05:15 – 06:15)
* **Thao tác chuột**:
  1. Đăng xuất, đăng nhập: `admin` / `Admin@123`.
  2. Vào menu **"Nhật ký hệ thống" (Audit Logs)**: Cuộn xem các dòng log vừa diễn ra (Tạo -> Phân công -> Tiếp nhận -> Chat -> Hoàn thành -> Đóng).
  3. Bấm vào menu **"Quản lý người dùng"**: Lướt xem danh sách các tài khoản và vai trò RBAC.
* **Lời nói (chỉ 2 câu)**:
  > *"Tại phân hệ quản trị, Admin có thể tra cứu toàn bộ lịch sử hoạt động của hệ thống qua nhật ký Audit Logs, mọi thao tác đều được lưu vết minh bạch. Ngoài ra, Admin quản lý danh sách tài khoản và phân quyền vai trò cho toàn bộ nhân sự."*

---

### 🏁 KẾT THÚC (Phút 06:15 – 06:30)
* **Thao tác**: Mở Terminal gõ `dotnet test` -> Hiện dòng màu xanh lá **`Passed! 342 test(s)`**.
* **Lời nói (1 câu)**:
  > *"Hệ thống đã hoàn thiện với 342 bài kiểm thử tự động đạt 100% kết quả tốt. Cảm ơn Thầy và các bạn đã theo dõi!"*
