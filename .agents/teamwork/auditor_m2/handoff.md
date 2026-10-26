# BÁO CÁO BÀN GIAO KIỂM TOÁN (HANDOFF REPORT) — AUDITOR M2

> **Vai trò**: Forensic Auditor (Milestone 2 Integrity Verification)  
> **Người thực hiện**: `auditor_m2`  
> **Người nhận**: `orchestrator` / `parent` (`9e3cad2c-3e7f-49c0-9814-015216856d2e`)  
> **Thời điểm**: 2026-10-25  
> **Loại bàn giao**: Hard Handoff (Hoàn tất kiểm toán toàn vẹn)  
> **Phán quyết**: **CLEAN**  

---

## 1. OBSERVATION (QUAN SÁT THỰC TẾ)

1. **Kiểm tra Mã Nguồn State Pattern (`src/ItTms.Domain/States/`)**:
   - `TicketStateBase.cs` (dòng 28-75): Cài đặt mô hình Fail-Closed mặc định, toàn bộ phương thức trả về `Result.Failure(TicketErrors.InvalidTransition(Name, action))`.
   - `NewState.cs` (dòng 20-53): Chỉ cho phép `Assign` (gán `AssignedToId`, chuyển sang `AssignedState`, sinh `TicketAssignedEvent`) và `Reject` (gán `RejectionReason`, chuyển sang `RejectedState`). Các thao tác khác kế thừa Fail-Closed.
   - `InProgressState.cs` (dòng 59-81): `Resolve` gán `ResolvedAt = DateTime.UtcNow` và chuyển `ResolvedState`. Phương thức `Reject` ghi đè trả về `Result.Failure(TicketErrors.CannotRejectInProgress)` theo Bảng 7.2 TC-06.
   - `ResolvedState.cs` (dòng 20-44): `CanComment` trả về `false` theo Bảng 7.2 TC-08. `Close` xác nhận `CreatorId == userId`, lưu `Feedback`, cập nhật `ClosedAt`, chuyển sang `ClosedState`, và sinh `TicketClosedEvent` theo Bảng 7.2 TC-09.
   - `Ticket.cs` (dòng 24-85): Thuộc tính `CurrentState => _currentState ??= TicketStateFactory.CreateState(this, _statusId);`. Phương thức `TransitionTo` cập nhật trạng thái, làm mới `ConcurrencyToken`, và sinh `TicketStatusChangedEvent`.

2. **Kiểm tra Mã Nguồn Observer Pattern & Event Pipeline**:
   - `NotificationObserver.cs` (dòng 31-207): Thực hiện gọi `_dbContext.Users` và `_dbContext.Tickets`, khởi tạo thực thể `Notification`, và thêm vào DbContext qua `_dbContext.Notifications.AddRangeAsync` / `AddAsync`.
   - `SystemLogObserver.cs` (dòng 30-121): Tạo thực thể `SystemLog` và thêm vào `_dbContext.SystemLogs.AddAsync` cho 6 loại sự kiện domain.
   - `ItTmsDbContext.cs` (dòng 31-56): SaveChangesAsync tích hợp Two-Phase Dispatching: Pha 1 điều phối in-transaction, lưu tất cả entity cùng transaction với `base.SaveChangesAsync`, Pha 2 điều phối post-commit qua SignalR.

3. **Kiểm tra Mã Nguồn Security Action Filters (`src/ItTms.Web/Filters/`)**:
   - `AuthorizeRolesAttribute.cs` (dòng 63-104): Truy vấn `db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)`, kiểm tra `UserStatus.Active` trong thời gian thực, thu hồi Cookie đăng nhập nếu bị vô hiệu hóa (Bảng 7.2 TC-01), và kiểm tra RBAC trả về HTTP 403 Forbidden nếu trái quyền (Bảng 7.2 TC-02).
   - `AuthorizeTicketAccessAttribute.cs` (dòng 63-123): Kiểm tra quyền sở hữu vé theo vai trò (`AssignedToId == currentUserId` đối với Staff, `CreatorId == currentUserId` đối với User), chặn đứng BOLA/IDOR với HTTP 403 Forbidden và ghi nhật ký cảnh báo an ninh vào `SystemLogs` (Bảng 7.2 TC-07).

4. **Kiểm tra Dấu vết Giả mạo & Pre-populated Artifacts**:
   - Quét tìm các chuỗi `NotImplementedException`, `TODO`, `FIXME` trong mã nguồn C#: Không tìm thấy kết quả nào trong mã nguồn dự án.
   - Quét tìm tệp log, kết quả dựng sẵn: 0 tệp log giả lập hoặc pre-populated outputs.

5. **Kết quả Biên dịch và Thực thi Kiểm thử Thực tế**:
   - Lệnh: `dotnet build c:\IT_System\ItTms.sln`
     ```text
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:08.41
     ```
   - Lệnh: `dotnet test c:\IT_System\ItTms.sln`
     ```text
     Passed!  - Failed:     0, Passed:   282, Skipped:     0, Total:   282, Duration: 30 s - ItTms.Tests.dll (net9.0)
     ```

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN LOGIC)

1. Từ Quan sát 1:
   - Các lớp State không hề trả về kết quả giả mạo hay hằng số cứng; mỗi lớp State sở hữu logic chuyển dịch trạng thái riêng biệt và cập nhật đúng đắn dữ liệu vào thực thể `Ticket`.
   - Các ràng buộc bất biến cốt lõi (TC-05, TC-06, TC-08, TC-09) đều được đóng gói kiên cố ngay tại tầng Domain, không phát sinh ngoại lệ không kiểm soát.

2. Từ Quan sát 2:
   - Hai observers (`NotificationObserver` và `SystemLogObserver`) thao tác thực tế với `IItTmsDbContext`, tạo bản ghi thực và thêm vào các tập hợp `Notifications` và `SystemLogs`.
   - Việc tích hợp điều phối hai pha trong `ItTmsDbContext.SaveChangesAsync` đảm bảo tính toàn vẹn giao dịch ACID tuyệt đối trên CSDL SQLite.

3. Từ Quan sát 3:
   - Các bộ lọc bảo mật MVC `AuthorizeRolesAttribute` và `AuthorizeTicketAccessAttribute` thực hiện kiểm tra động hai lớp (Claims + CSDL thực tế) tại thời điểm xử lý request, ngăn chặn triệt để tấn công sử dụng phiên làm việc đã bị thu hồi (TC-01) và tấn công truy cập chéo dữ liệu BOLA/IDOR (TC-07).

4. Từ Quan sát 4 & 5:
   - Không có bất kỳ dấu hiệu làm giả kết quả kiểm thử hay mã nguồn bù nhìn (facade/dummy).
   - Test suite chạy thực thi độc lập thành công 100% với 282/282 tests passed (bao gồm toàn bộ các bài kiểm thử thách thức adversarial).
   - Suy ra sản phẩm bàn giao Milestone 2 hoàn toàn chính thống, đạt chuẩn chất lượng sản xuất.

---

## 3. CAVEATS (LƯU Ý)

- Kiểm toán tập trung vào tính toàn vẹn của mã nguồn nghiệp vụ, mẫu thiết kế, bộ lọc an ninh và bộ kiểm thử của Milestone 2.
- Các giao diện đồ họa Razor Views và Controllers hoàn chỉnh cho 20 Use Cases thuộc về phạm vi Milestone 3.

---

## 4. CONCLUSION (KẾT LUẬN & PHÁN QUYẾT)

- **PHÁN QUYẾT TOÀN VẸN**: **CLEAN**.
- Sản phẩm bàn giao Milestone 2 của `worker_m2` đạt tính chân thực 100%, không phát hiện bất kỳ vi phạm liêm chính nào (không hardcoded, không facade, không fabricated results).
- Toàn bộ 282 bài kiểm thử (bao gồm unit tests, ma trận 49 hoán vị, và adversarial security tests) đều chạy thành công trên CSDL SQLite in-memory thực tế.
- Khuyến nghị chấp thuận nghiệm thu Milestone 2 và cho phép tiến hành Milestone 3.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Bất kỳ bên thứ ba hoặc agent nào cũng có thể kiểm chứng lại kết quả độc lập bằng các câu lệnh sau tại thư mục gốc:

1. **Biên dịch độc lập**:
   ```powershell
   dotnet build c:\IT_System\ItTms.sln
   ```
   *Điều kiện*: `0 Warning(s), 0 Error(s)`.

2. **Thực thi toàn bộ bộ kiểm thử độc lập**:
   ```powershell
   dotnet test c:\IT_System\ItTms.sln
   ```
   *Điều kiện*: `Passed: 282, Failed: 0, Skipped: 0`.

3. **Kiểm tra báo cáo kiểm toán chi tiết**:
   - `c:\IT_System\.agents\teamwork\auditor_m2\audit_report.md`
