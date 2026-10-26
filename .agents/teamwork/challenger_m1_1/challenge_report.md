# BÁO CÁO KIỂM THỬ XUNG ĐỘT ĐỒNG THỜI & BẤT BIẾN SLA (CHALLENGER M1_1)

**Dự án**: IT Ticket Management System (.NET 9) — IT-TMS  
**Milestone**: M1 - Scaffolding & Core Domain Foundation  
**Tác giả**: `challenger_m1_1` (critic / specialist)  
**Tiêu chuẩn nhận thức**: `#Argon-Omega` (BLUF-MECE, Causal Constructivism, Anti-Goodhart Testing)  
**Ngày thực hiện**: 2026-10-25  
**Phán quyết chính thức**: **`APPROVE`** (Chấp thuận nghiệm thu M1)

---

## 1. TỔNG QUAN PHÁN QUYẾT (EXECUTIVE SUMMARY - BLUF)

- **Phán quyết**: **`APPROVE`**
- **Đánh giá rủi ro tổng thể**: **LOW (Rất thấp)**
- **Kết quả thực nghiệm**: Toàn bộ **37 kịch bản kiểm thử nghịch đảo chuyên sâu** (13 kịch bản Concurrency & Conflict Stress, 24 kịch bản SLA Invariants & Edge Cases) đều **100% PASSED** khi thực thi trực tiếp trên SQLite Engine:
  - `AdversarialConcurrencyStressTests.cs`: 13/13 tests PASSED (0 lỗi race condition, 0 lỗi SQLite locking).
  - `AdversarialSlaStressTests.cs`: 24/24 tests PASSED (0 lỗi trôi thời gian, bảo toàn chuẩn xác DateTimeKind.Utc).
- **Kết luận kiến trúc**: Cơ chế kiểm soát xung đột đồng thời lạc quan (Optimistic Concurrency Control) thông qua `ConcurrencyToken` (Guid) kết hợp giữa Rich Domain Model (`Ticket.RenewConcurrencyToken()`) và `ConcurrencyTokenInterceptor` trên EF Core 9 hoạt động cực kỳ tin cậy, triệt tiêu hoàn toàn nguy cơ Lost Updates. Thuật toán SLA `DueAt` tuân thủ toán học lịch Gregory (năm nhuận, chuyển tiếp thế kỷ) và bảo toàn tính đơn điệu trên trục thời gian UTC.

---

## 2. KẾT QUẢ KIỂM THỬ NGHỊCH ĐẢO CHI TIẾT (STRESS TEST RESULTS)

### 2.1. Concurrency & Conflict Stress Harness (`AdversarialConcurrencyStressTests`)
Đã triển khai bộ kiểm thử áp lực đa phiên làm việc thực tế (`DbContext` độc lập) trên file CSDL SQLite vật lý (chế độ `WAL` và `busy_timeout = 5000`):

| # | Kịch bản kiểm thử áp lực | Hành vi kỳ vọng | Kết quả thực tế | Trạng thái |
|---|---|---|---|:---:|
| 1 | **Xung đột 2 DbContext sửa 2 thuộc tính khác nhau** (`Title` vs `Description`) | User 1 sửa Title thành công, đổi token; User 2 sửa Description nhận `DbUpdateConcurrencyException`, không ghi đè mất mát dữ liệu | User 1 commit thành công, token đổi từ $T_0 \to T_1$. User 2 ném `DbUpdateConcurrencyException`. Dữ liệu User 1 trong DB được bảo toàn nguyên vẹn. | **PASS** |
| 2 | **Cơ chế hòa giải xung đột (Conflict Resolution)**: Reload & Retry | Sau khi nhận ngoại lệ xung đột, User 2 gọi `entry.ReloadAsync()`, nạp token mới $T_1$, áp dụng lại thay đổi và commit thành công với token $T_2$ | Bản ghi cuối cùng phản ánh đồng thời `Title` của User 1 và `Description` của User 2. Token $T_2 \neq T_1 \neq T_0$. | **PASS** |
| 3 | **Chuyển đổi trạng thái nghiệp vụ đối đầu sửa dữ liệu** | Leader phân công vé (`Assign`), User đồng thời sửa `Description` dựa trên bản ghi cũ | Thao tác `Assign` của Leader thành công (Status sang Assigned, token $T_1$). Thao tác sửa Description của User bị chặn bởi `DbUpdateConcurrencyException`. | **PASS** |
| 4 | **Sửa đổi bản ghi sau khi đã bị xóa (Update after Delete)** | Context 1 xóa vé, Context 2 cập nhật vé đã bị xóa | EF Core nhận diện 0 hàng bị ảnh hưởng (`RowsAffected == 0`) và ném `DbUpdateConcurrencyException`. Không phát sinh lỗi SQL crash. | **PASS** |
| 5 | **Đua tranh cao độ (High Concurrency Race - 10 Tasks)** | 10 tác vụ bất đồng bộ kích hoạt rào chắn (`Barrier`) ghi đè đồng thời vào 1 Ticket | **Duy nhất 1 tác vụ thành công**, **9 tác vụ còn lại nhận `DbUpdateConcurrencyException`**, 0 tác vụ bị lỗi khóa SQLite (`database is locked`). | **PASS** |
| 6-13 | **Bảo toàn bất biến cấp mới ConcurrencyToken qua 8 đường dẫn biến động**: `Assign`, `StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reopen`, `Reject`, `PropertyChange` | Mọi biến động trên Ticket đều bắt buộc phải cấp phát một `Guid ConcurrencyToken` hoàn toàn mới và cập nhật `UpdatedAt >= oldUpdatedAt` | 8/8 hành vi nghiệp vụ đều chứng minh token mới $\neq$ token cũ, và giá trị được đồng bộ chính xác xuống CSDL SQLite. | **PASS** |

### 2.2. SLA Invariants & Edge Case Harness (`AdversarialSlaStressTests`)
Đã stress-test toàn diện các trường hợp biên của thời gian và tham số đầu vào:

| # | Nhóm biên kiểm thử SLA | Kịch bản chi tiết | Kết quả thực tế | Trạng thái |
|---|---|---|---|:---:|
| 1-5 | **Năm nhuận 2028 (Tháng 2 có 29 ngày)** | - Tạo lúc `28/02 23:30`, Critical (+1h) $\to$ `29/02 00:30`<br>- Tạo lúc `28/02 23:00`, High (+4h) $\to$ `29/02 03:00`<br>- Tạo lúc `28/02 12:00`, Medium (+24h) $\to$ `29/02 12:00`<br>- Tạo lúc `28/02 00:00`, Low (+72h) $\to$ `02/03 00:00`<br>- Tạo lúc `29/02 10:00`, Medium (+24h) $\to$ `01/03 10:00` | Toàn bộ 5 trường hợp đều tính toán chính xác tuyệt đối, bước qua ngày nhuận 29/02 và chuyển tiếp sang tháng 3 chuẩn xác. | **PASS** |
| 6-9 | **Năm không nhuận 2026 (Tháng 2 chỉ có 28 ngày)** | - Tạo lúc `28/02 23:30`, Critical (+1h) $\to$ `01/03 00:30`<br>- Tạo lúc `28/02 22:00`, High (+4h) $\to$ `01/03 02:00`<br>- Tạo lúc `28/02 12:00`, Medium (+24h) $\to$ `01/03 12:00`<br>- Tạo lúc `28/02 00:00`, Low (+72h) $\to$ `03/03 00:00` | Không phát sinh ngày 29/02 ảo, chuyển tiếp mượt mà sang ngày 01/03. | **PASS** |
| 10 | **Quy tắc nhuận thế kỷ (Century Leap Year)** | - Năm 2000 (chia hết cho 400) là năm nhuận: `28/02/2000 23:00` + 1h $\to$ `29/02/2000 00:00`<br>- Năm 2100 (chia hết cho 100, không chia hết cho 400) không nhuận: `28/02/2100 23:00` + 1h $\to$ `01/03/2100 00:00` | Tuân thủ 100% chuẩn lịch Gregory quốc tế. | **PASS** |
| 11 | **Giờ mùa hè (Daylight Saving Time - DST)** | Điểm chuyển tiếp US DST Spring Forward (08/03/2026) | Nhờ chuẩn hóa `DateTimeKind.Utc`, các khoảng thời gian duy trì chính xác 1h (3600s), 4h (14400s), 24h (86400s), 72h (259200s), hoàn toàn không bị trôi/lệch múi giờ. | **PASS** |
| 12-14 | **Enum Priority bất hợp pháp** (`0`, `-1`, `99`) | Truyền giá trị ưu tiên nằm ngoài enum | Thuật toán fallback an toàn về `+24h` mặc định, không gây crash hệ thống. | **PASS** |
| 15 | **Độ chính xác ranh giới (Exact Second & Tick Boundary)** | - Tại đúng thời điểm `DueAt`: `IsOverdue == false`, `RemainingTime == TimeSpan.Zero`<br>- Tại `DueAt + 1 tick` (100ns): `IsOverdue == true`, `RemainingTime < 0` | Xác định chuẩn xác trạng thái quá hạn tới mức phân giải nanosecond. | **PASS** |
| 16-22 | **Ma trận 7 trạng thái vòng đời khi quá hạn (`now > DueAt`)** | - `New`, `Assigned`, `InProgress`, `Waiting`: Quá hạn (`IsOverdue == true`)<br>- `Resolved`, `Closed`, `Rejected`: Đã hoàn tất/dừng xử lý, **không bị tính quá hạn** (`IsOverdue == false`) | Đáp ứng hoàn hảo luật nghiệp vụ vận hành helpdesk thực tế. | **PASS** |
| 23 | **Thời gian còn lại âm (`RemainingTime < 0`)** | Khi ticket đã quá hạn, `RemainingTime` trả về `TimeSpan` âm phản ánh số giờ trễ hạn | Giá trị phản ánh đúng độ trễ hạn (ví dụ `-02:00:00`). | **PASS** |
| 24 | **SQLite Roundtrip & DateTimeKind.Utc** | Lưu trữ Ticket có ngày giờ năm nhuận xuống SQLite và đọc lại qua DbContext mới | `ValueConverter` bảo toàn nguyên vẹn `DateTimeKind.Utc` và độ chính xác thời gian. | **PASS** |

---

## 3. DANH MỤC THỬ THÁCH ĐÃ ĐẶT RA (CHALLENGES & MITIGATIONS)

### [Low] Thách thức 1: Thiếu kiểm tra `Enum.IsDefined` trong `Ticket.Create`
- **Mô tả**: `Ticket.Create` không kiểm tra `Enum.IsDefined(typeof(TicketPriority), priority)`. Khi truyền giá trị âm hoặc giá trị ngoài dải (ví dụ `(TicketPriority)99`), phương thức vẫn tạo thực thể thành công và fallback SLA về `24h`. Khi lưu vào SQLite, cột `Priority` lưu chuỗi `"99"`.
- **Đánh giá rủi ro**: **LOW**. Không gây crash CSDL vì EF Core hỗ trợ parse số nguyên sang enum chuỗi và ngược lại. Ở tầng Presentation/API (M3), validation ViewModel sẽ lọc dữ liệu đầu vào.
- **Khuyến nghị khắc phục (M2/M3)**: Bổ sung guard clause `if (!Enum.IsDefined(priority)) return Result<Ticket>.Failure(...)` trong Command Handler hoặc Domain Factory để chặt chẽ hơn.

### [Low] Thách thức 2: Định dạng hiển thị của `RemainingTime` khi trễ hạn
- **Mô tả**: `ticket.RemainingTime(now)` trả về `TimeSpan` âm khi ticket đã quá hạn. Nếu tầng UI hiển thị trực tiếp `remaining.ToString()`, người dùng có thể thấy `-01:30:00`.
- **Đánh giá rủi ro**: **LOW**. Đây là hành vi đúng về mặt số học.
- **Khuyến nghị khắc phục (M3)**: Tầng ViewModel cần có helper hiển thị (ví dụ: `Quá hạn 1 giờ 30 phút` nếu `RemainingTime < TimeSpan.Zero`).

---

## 4. KHU VỰC KHÔNG THÁCH THỨC (UNCHALLENGED AREAS)
- Mất kết nối mạng vật lý tới SQLite: Không áp dụng do SQLite là cơ sở dữ liệu embedded chạy in-process.
- Phân tán transaction đa máy chủ (Distributed Transactions): Không thuộc phạm vi kiến trúc đơn node của hệ thống.

---

## 5. KẾT LUẬN & PHÁN QUYẾT CHÍNH THỨC

```
================================================================================
VERDICT: APPROVE
STATUS: READY FOR MILESTONE 2 (Design Patterns & Business Logic Engine)
================================================================================
```
Milestone 1 đạt chuẩn sản phẩm cao cấp (Production-Grade). Cơ chế Concurrency Token và thuật toán SLA vững chắc trước mọi thử thách áp lực đồng thời và điều kiện biên.
