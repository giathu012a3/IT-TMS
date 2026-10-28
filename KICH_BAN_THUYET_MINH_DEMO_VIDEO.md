# 📱 KỊCH BẢN QUAY DEMO TÍNH NĂNG IT-TMS (5 – 7 PHÚT)
> **Cách quay**: Thao tác chuột từ tốn trên màn hình máy tính, điện thoại để trước mặt chỉ cần đọc đúng **1 - 2 câu ngắn gọn** cho mỗi tính năng. Không dài dòng, đi thẳng vào chức năng phần mềm.

---

### 🔑 TÀI KHOẢN ĐĂNG NHẬP (Chung mật khẩu: Tên@123)
* User: `user1` / `User@123`
* Leader: `leader` / `Leader@123`
* Staff: `staff1` / `Staff@123`
* Admin: `admin` / `Admin@123`

---

## ⏱️ CÁC TÍNH NĂNG CHÍNH CẦN QUAY

---

### 1️⃣ TÍNH NĂNG 1: ĐĂNG NHẬP & GIAO DIỆN CHÍNH (00:00 – 00:45)
* **Thao tác**: 
  - Mở trang `http://localhost:5050`.
  - Bấm thử nút đổi giao diện Sáng / Tối góc trên bên phải.
  - Đăng nhập tài khoản `user1` / `User@123`.
  - Lướt xem Dashboard cá nhân.
* **Lời nói**:
  > *"Hệ thống hỗ trợ chuyển đổi linh hoạt giữa giao diện Sáng và Tối. Sau khi đăng nhập, người dùng sẽ vào Dashboard cá nhân để theo dõi nhanh số lượng ticket của mình."*

---

### 2️⃣ TÍNH NĂNG 2: TẠO TICKET SỰ CỐ & TỰ TÍNH SLA (00:45 – 01:45)
* **Thao tác**:
  - Bấm **"Tạo yêu cầu mới"**.
  - Nhập tiêu đề: `"Lỗi phần mềm kế toán không kết nối được máy chủ"`.
  - Chọn danh mục: `Phần mềm`.
  - Chọn mức ưu tiên: `Cao` -> Rê chuột vào ô cam kết SLA bên dưới hiển thị 24 giờ.
  - Nhập mô tả và chọn 1 file ảnh lỗi đính kèm.
  - Bấm **"Gửi yêu cầu hỗ trợ"**.
* **Lời nói**:
  > *"Người dùng tạo yêu cầu hỗ trợ với đầy đủ thông tin: danh mục, mô tả và hình ảnh đính kèm. Khi chọn mức độ ưu tiên, hệ thống sẽ tự động tính toán thời hạn cam kết SLA để xử lý."*

---

### 3️⃣ TÍNH NĂNG 3: TRƯỞNG NHÓM PHÂN CÔNG CÔNG VIỆC (01:45 – 02:45)
* **Thao tác**:
  - Đăng xuất, đăng nhập tài khoản: `leader` / `Leader@123`.
  - Vào menu **"Hàng đợi tiếp nhận"**.
  - Bấm nút **"Phân công"** trên ticket vừa tạo.
  - Chọn kỹ thuật viên `staff1` -> Bấm **"Xác nhận Giao việc"**.
* **Lời nói**:
  > *"Trưởng nhóm IT có khu vực Hàng đợi tiếp nhận riêng. Tại đây, trưởng nhóm kiểm tra thông tin sự cố và phân công trực tiếp cho kỹ thuật viên phù hợp phụ trách."*

---

### 4️⃣ TÍNH NĂNG 4: CHAT TRỰC TIẾP REAL-TIME & TIẾP NHẬN XỬ LÝ (02:45 – 04:30)
> *(Chia đôi màn hình: Trái là `user1`, Phải là `staff1`)*

* **Thao tác**:
  - **Màn hình Staff (Phải)**: Bấm nút **"Bắt đầu Tiếp nhận"**.
    -> Chỉ chuột sang màn hình User thấy trạng thái tự đổi sang Đang xử lý không cần F5.
  - **Staff gõ chat**: *"Chào bạn, bạn kiểm tra lại dây mạng giúp mình nhé."* -> Nhấn Enter.
    -> Màn hình User nhận tin nhắn tức thì.
  - **User gõ chat trả lời**: *"Dây mạng cắm bình thường rồi ạ."* -> Nhấn Enter.
  - **Staff tích chọn "Ghi chú nội bộ"**: Gõ *"Đã reset lại cổng mạng switch"* -> Gửi.
    -> Chỉ chuột so sánh: Staff thấy ghi chú màu vàng, bên User hoàn toàn không thấy.
  - **Staff bấm**: **"Đã giải quyết"** -> Cả hai bên đổi trạng thái và tự khóa khung chat.
* **Lời nói**:
  > *"Hệ thống hỗ trợ cập nhật trạng thái và trao đổi thời gian thực qua SignalR WebSocket. Khi kỹ thuật viên bấm tiếp nhận hoặc gửi tin nhắn, phía người dùng sẽ thấy ngay lập tức mà không cần tải lại trang. Kỹ thuật viên cũng có thể gửi các ghi chú nội bộ để trao đổi riêng mà người dùng không nhìn thấy."*

---

### 5️⃣ TÍNH NĂNG 5: NGHIỆM THU ĐÁNH GIÁ & ĐÓNG TICKET (04:30 – 05:15)
* **Thao tác**:
  - Phóng to màn hình User.
  - Chọn đánh giá **5 sao**, nhập: *"Hỗ trợ rất nhanh, cảm ơn IT!"*.
  - Bấm nút **"Xác nhận & Đóng ticket"**.
* **Lời nói**:
  > *"Sau khi sự cố được khắc phục, người dùng kiểm tra kết quả, chấm điểm độ hài lòng và đóng ticket để hoàn tất vòng đời yêu cầu."*

---

### 6️⃣ TÍNH NĂNG 6: QUẢN TRỊ NGƯỜI DÙNG & NHẬT KÝ HỆ THỐNG (05:15 – 06:15)
* **Thao tác**:
  - Đăng xuất, đăng nhập tài khoản: `admin` / `Admin@123`.
  - Vào **"Quản lý người dùng"**: Lướt qua danh sách tài khoản và phân quyền vai trò.
  - Vào **"Nhật ký hệ thống" (Audit Logs)**: Cuộn xem các dòng lịch sử vừa diễn ra từ lúc tạo ticket, phân công, chat, đến lúc đóng.
* **Lời nói**:
  > *"Phân hệ quản trị cho phép Admin quản lý danh sách tài khoản, phân quyền vai trò và tra cứu toàn bộ lịch sử hoạt động của hệ thống qua nhật ký Audit Logs."*

---

### 7️⃣ KẾT THÚC (06:15 – 06:30)
* **Thao tác**: Quay lại trang chủ hoặc màn hình chính.
* **Lời nói**:
  > *"Đó là các tính năng chính trong quy trình tiếp nhận và xử lý sự cố kỹ thuật của hệ thống IT-TMS. Cảm ơn các bạn đã theo dõi!"*
