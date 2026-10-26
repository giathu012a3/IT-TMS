# BÁO CÁO THẨM ĐỊNH VÀ PHẢN BIỆN ĐỐI KHÁNG (MILESTONE 1 REVIEW REPORT)

**Dự án**: IT Ticket Management System (.NET 9) — IT-TMS  
**Đối tượng thẩm định**: Mã nguồn và kết quả triển khai Milestone 1 của `worker_m1`  
**Thực hiện bởi**: `reviewer_m1_1` (reviewer & critic)  
**Thời điểm**: 2026-10-25  
**Chuẩn nhận thức**: `#Argon-Omega` (BLUF-MECE, First-Principles, Adversarial Falsification, Zero-Fluff)  
**Ngôn ngữ**: Tiếng Việt (bảo toàn thuật ngữ kỹ thuật và mã code)  

---

## 1. TỔNG KẾT THẨM ĐỊNH (EXECUTIVE REVIEW SUMMARY)

**KẾT LUẬN CUỐI CÙNG (VERDICT)**: **`APPROVE` (CHẤP THUẬN TOÀN DIỆN)**

- **Đánh giá rủi ro tổng thể (Overall Risk Assessment)**: **`LOW` (RỦI RO THẤP)**
- **Kiểm tra vi phạm liêm chính (Integrity Violation Check)**: **`CLEAN / PASS`**. Không phát hiện bất kỳ dấu hiệu gian lận nào: không hardcode kết quả kiểm thử, không facade/dummy implementation, không tạo log/bằng chứng giả mạo, không bypass logic nghiệp vụ.
- **Tuân thủ Clean Architecture**: **`EXCELLENT`**. Tầng `ItTms.Domain` hoàn toàn thuần khiết (0 dependency ngoài, 0 package NuGet). Tầng `ItTms.Application` chỉ định nghĩa interfaces và phụ thuộc duy nhất vào Domain. Tầng `ItTms.Infrastructure` chịu trách nhiệm persistence, cấu hình Fluent API, SQLite PRAGMAs, migrations và seeder. Tầng `ItTms.Web` đóng vai trò Presentation Host và SignalR endpoint.
- **Biên dịch & Kiểm thử Độc lập**:
  - `dotnet build c:\IT_System\ItTms.sln`: **0 Warning(s), 0 Error(s)** (thực thi trong 8.14s dưới cờ `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`).
  - `dotnet test c:\IT_System\ItTms.sln --verbosity normal`: **30/30 tests PASSED (100%)**, 0 failed (thực thi trong 6.15s).
  - `dotnet ef migrations list`: Xác nhận migration `20261025082220_InitialCreate` tồn tại và đồng bộ hoàn hảo với DbContext.

---

## 2. KẾT QUẢ XÁC MINH CÁC TUYÊN BỐ CỐT LÕI (VERIFIED CLAIMS)

| Tuyên bố từ `worker_m1` | Phương pháp kiểm chứng độc lập của Reviewer | Kết quả | Ghi chú kỹ thuật |
|---|---|:---:|---|
| Solution và 5 dự án Clean Architecture được thiết lập chuẩn | Kiểm tra trực tiếp các file `.csproj` và build solution | **PASS** | `ItTms.Domain.csproj` không chứa bất kỳ PackageReference nào; Application chỉ dùng EF Core và DI Abstractions. |
| Biên dịch 0 cảnh báo, 0 lỗi | Thực thi `dotnet build c:\IT_System\ItTms.sln` | **PASS** | Exit code 0, 0 Warnings, 0 Errors, áp dụng nghiêm ngặt `TreatWarningsAsErrors = true`. |
| 30 tests xUnit đạt 100% | Thực thi `dotnet test c:\IT_System\ItTms.sln --verbosity normal` | **PASS** | 30/30 passed: 4 ResultMonad, 3 UserInvariant, 18 TicketInvariant, 3 PasswordHasher, 1 Concurrency, 1 Migration/Seeder. |
| 8 Core Entities đầy đủ thuộc tính và quan hệ | Kiểm tra mã nguồn từng entity trong `src/ItTms.Domain/Entities/` | **PASS** | `User`, `TicketStatus`, `Ticket`, `Comment`, `Feedback`, `Attachment`, `Notification`, `SystemLog` đầy đủ thuộc tính, factory method, private setter và navigation properties. |
| Tính toán SLA `DueAt` chính xác theo Priority | Kiểm tra `Ticket.CalculateDueAt()` và chạy unit test xUnit | **PASS** | Critical: 1h, High: 4h, Medium: 24h, Low: 72h. Extension `IsOverdue()` và `RemainingTime()` tính toán chính xác. |
| ConcurrencyToken chống Lost Updates trên SQLite | Kiểm tra `ConcurrencyTokenInterceptor.cs`, `TicketConfiguration.cs`, và `OptimisticConcurrencyTests.cs` | **PASS** | Cột `ConcurrencyToken` được đánh dấu `.IsConcurrencyToken()`, tự động sinh mới Guid khi cập nhật, ném `DbUpdateConcurrencyException` khi có xung đột. |
| Covering Indexes tối ưu hóa truy vấn | Kiểm tra Fluent API và file migration `InitialCreate.cs` | **PASS** | Đủ 4 covering indexes trên Tickets (`IX_Tickets_CreatorId_CreatedAt`, `IX_Tickets_AssignedToId_StatusId`, `IX_Tickets_StatusId_DueAt`, `IX_Tickets_StatusId_Priority_CreatedAt`) cùng các index trên Comments, Notifications, Logs và Users. |
| DataSeeder nạp dữ liệu Idempotent | Kiểm tra `DataSeeder.cs` và `DatabaseMigrationAndSeederTests.cs` | **PASS** | Nạp 7 TicketStatuses, 8 Users (bao gồm `deactivated` có `IsActive = false`), 3 tickets demo. Chạy lặp lại không nhân bản dữ liệu. |
| SQLite PRAGMAs chống khóa DB | Kiểm tra `Program.cs` và file CSDL thực tế `src/ItTms.Web/ittms.db*` | **PASS** | Kích hoạt `WAL`, `foreign_keys = ON`, `busy_timeout = 5000`, `synchronous = NORMAL`. File `-wal` và `-shm` xuất hiện thực tế. |

---

## 3. PHÂN TÍCH CHUYÊN SÂU 4 TRỤ CỘT MILESTONE 1

### 3.1. Clean Architecture Compliance (Tuân thủ Kiến trúc Sạch)
- **Domain Layer (`ItTms.Domain`)**: Hoàn toàn cô lập khỏi framework và ORM. Sử dụng DDD Rich Domain Model, đóng gói nghiệp vụ qua Factory methods (`Create`) và Business methods (`Assign`, `StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reopen`, `Reject`, `Deactivate`). Xử lý lỗi qua Railway-Oriented Programming Monad `Result` và `Result<T>` không ném Exception cho luồng nghiệp vụ thông thường.
- **Application Layer (`ItTms.Application`)**: Khai tử Generic Repository anti-pattern (`#Argon-Omega`), trừu tượng hóa trực tiếp `IItTmsDbContext` với `DbSet<T>` và `DatabaseFacade`, cung cấp toàn quyền tối ưu LINQ, `AsNoTracking()` và Projection ở các milestone tiếp theo.
- **Infrastructure Layer (`ItTms.Infrastructure`)**: Đóng gói toàn bộ cấu hình SQLite, Fluent API, `ConcurrencyTokenInterceptor`, `PasswordHasherService` (dùng `Microsoft.AspNetCore.Identity.PasswordHasher<User>` chuẩn PBKDF2 HMAC-SHA512).
- **Web Layer (`ItTms.Web`)**: Khởi động tự động migrate và seed CSDL, đăng ký SignalR Hub `/hubs/ticket` sẵn sàng cho giao tiếp hai chiều thời gian thực ở Milestone 3.

### 3.2. Đầy đủ 8 Core Entities & Encapsulation
1. **User**: Quản lý thông tin tài khoản, mật khẩu băm, vai trò (`UserRole`), trạng thái (`UserStatus`), ràng buộc bất biến Bảng 7.2 TC-10 (Admin không thể tự vô hiệu hóa tài khoản của chính mình).
2. **TicketStatus**: Danh mục 7 trạng thái chuẩn (Id 1..7: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected), `ValueGeneratedNever()` bảo đảm tính nhất quán định danh.
3. **Ticket**: Aggregate Root quản lý toàn bộ vòng đời, tính toán SLA `DueAt`, kiểm soát đồng thời `ConcurrencyToken`, bất biến Bảng 7.2 TC-08 (`CanComment()` chặn khi Resolved/Closed/Rejected), bất biến Bảng 7.2 TC-06 (`Reject()` chặn khi InProgress).
4. **Comment**: Hỗ trợ trao đổi hai chiều và cờ `IsInternal` cho ghi chú nội bộ kỹ thuật.
5. **Feedback**: Đánh giá chất lượng 1-5 sao và nhận xét khi đóng ticket. Quan hệ 1 - 0..1 với Ticket qua Unique Index.
6. **Attachment**: Metadata tệp đính kèm với giới hạn kiểm tra kích thước tối đa 16MB.
7. **Notification**: Thông báo đẩy tới từng User với trạng thái `IsRead` và liên kết điều hướng `Link`.
8. **SystemLog**: Nhật ký kiểm toán hệ thống bất biến (Append-Only Audit Log) ghi nhận thao tác, IP, thời điểm và chi tiết.

### 3.3. SLA DueAt Calculation Invariants
Thuật toán ánh xạ chính xác yêu cầu đặc tả:
- `Critical` $\to +1$ giờ ($t_0 + 1\text{h}$)
- `High` $\to +4$ giờ ($t_0 + 4\text{h}$)
- `Medium` $\to +24$ giờ ($t_0 + 24\text{h}$)
- `Low` $\to +72$ giờ ($t_0 + 72\text{h}$)
Bổ sung `TicketSlaExtensions` với `IsOverdue(DateTime? currentTime)` xác định vi phạm SLA khi `DueAt < now` và trạng thái chưa phải `Resolved`, `Closed` hoặc `Rejected`.

### 3.4. Covering Indexes & Performance Tuning
Cấu hình Fluent API định nghĩa đầy đủ 4 covering indexes trọng yếu trên bảng `Tickets` và các chỉ mục phụ trợ:
1. `IX_Tickets_CreatorId_CreatedAt`: Tối ưu hóa truy vấn UC05 (danh sách ticket của tôi).
2. `IX_Tickets_AssignedToId_StatusId`: Tối ưu hóa truy vấn UC13 (hàng đợi xử lý của Staff).
3. `IX_Tickets_StatusId_DueAt`: Tối ưu hóa truy vấn UC17 (theo dõi vi phạm SLA theo thời gian thực).
4. `IX_Tickets_StatusId_Priority_CreatedAt`: Tối ưu hóa phân loại, lọc đa tiêu chí cho Leader và Admin.
5. `IX_Comments_TicketId_CreatedAt`: Tối ưu hóa nạp luồng bình luận theo thứ tự thời gian.
6. `IX_Notifications_UserId_IsRead_CreatedAt`: Tối ưu hóa tính toán số badge chưa đọc và danh sách thông báo.
7. `IX_SystemLogs_CreatedAt_Action`: Tối ưu hóa kiểm toán hệ thống.

---

## 4. PHẢN BIỆN ĐỐI KHÁNG (ADVERSARIAL STRESS-TESTING & CRITIC ANALYSIS)

Dưới vai trò Đối kháng (`critic`), nhóm thẩm định đã chủ động xây dựng các kịch bản biên, phân tích giả định ngầm và tìm kiếm các điểm lỗi tiềm tàng (Failure Modes) trước khi chuyển giao sang Milestone 2.

### Thử thách 1 [Medium]: Cơ chế kép sinh mới `ConcurrencyToken` (Dual Concurrency Token Renewal)
- **Giả định bị chất vấn**: Cả phương thức nghiệp vụ trong `Ticket.cs` (như `Assign`, `StartProgress`, `Resolve`...) và `ConcurrencyTokenInterceptor` trong tầng Infrastructure đều gọi sinh mới `ConcurrencyToken = Guid.NewGuid()`.
- **Kịch bản tấn công / Phân tích lỗi**:
  - Khi một ticket được tải từ CSDL vào ChangeTracker của DbContext, `OriginalValue` của `ConcurrencyToken` là `Guid_A`.
  - Trong logic nghiệp vụ, gọi `ticket.Assign(...)` $\to$ thuộc tính `ticket.ConcurrencyToken` chuyển thành `Guid_B`.
  - Khi gọi `SaveChangesAsync()`, `ConcurrencyTokenInterceptor` can thiệp vào `SavingChanges` và gán lại `ticket.ConcurrencyToken = Guid_C`.
- **Đánh giá vùng ảnh hưởng (Blast Radius)**:
  - EF Core SQLite sử dụng giá trị `OriginalValue` (`Guid_A`) để đưa vào mệnh đề `WHERE ConcurrencyToken = 'Guid_A'`.
  - Kết quả kiểm thử thực tế tại `OptimisticConcurrencyTests.cs` chứng minh cơ chế này hoạt động chính xác và phát hiện xung đột `DbUpdateConcurrencyException`.
- **Khuyến nghị cho Milestone 2**:
  - Không cần sửa đổi hiện tại vì đây là mô hình "Phòng thủ theo chiều sâu" (Defense-in-depth). Logic trong Domain đảm bảo Entity luôn có token mới kể cả khi chạy test in-memory không qua DbContext, trong khi Interceptor bảo vệ cho các trường hợp cập nhật trực tiếp thuộc tính mà không qua Business Method. Tuy nhiên, lập trình viên M2 cần lưu ý không can thiệp thủ công vào `OriginalValues` trong ChangeTracker.

### Thử thách 2 [Low]: Giới hạn đơn ghi (Single-Writer Limit) của SQLite dưới tải cao
- **Giả định bị chất vấn**: Hệ thống IT-TMS hướng tới doanh nghiệp, trong khi SQLite là cơ sở dữ liệu file nhúng với cơ chế khóa file khi ghi.
- **Kịch bản tấn công**: Nhiều Staff và User đồng thời gửi Comment qua SignalR và cập nhật trạng thái vé có thể gây lỗi `database is locked (5)`.
- **Đánh giá phòng thủ đã triển khai**:
  - `worker_m1` đã cấu hình 4 SQLite PRAGMAs tối ưu ngay khi khởi động: `PRAGMA journal_mode = WAL;`, `PRAGMA busy_timeout = 5000;`, `PRAGMA synchronous = NORMAL;`.
  - Chế độ WAL (Write-Ahead Logging) cho phép nhiều luồng đọc đồng thời với 1 luồng ghi mà không chặn nhau; `busy_timeout = 5000` giúp tự động retry trong 5 giây trước khi báo lỗi.
- **Khuyến nghị kiến trúc dài hạn**:
  - Nhờ Clean Architecture, nếu trong tương lai quy mô vượt quá hàng nghìn concurrent writes/sec, toàn bộ hệ thống có thể chuyển sang SQL Server hoặc PostgreSQL chỉ bằng việc thay đổi chuỗi kết nối và provider trong `AddInfrastructure` mà không phải thay đổi 1 dòng code Domain hay Application nào.

### Thử thách 3 [Low]: Tách State Pattern theo Hợp đồng Giao diện ở Milestone 2
- **Giả định bị chất vấn**: Trong Milestone 1, các trạng thái vé và quy tắc chuyển đổi được đóng gói tập trung trong lớp `Ticket.cs` (`Assign`, `StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reject`). Tuy nhiên, `PROJECT.md` và `ORIGINAL_REQUEST.md` yêu cầu State Pattern với 7 class trạng thái kế thừa `ITicketState` / `TicketStateBase` tại Milestone 2.
- **Đánh giá**:
  - Đây là sự phân chia hoàn toàn hợp lý theo lộ trình Milestone: Milestone 1 xây dựng Scaffolding, Core Entities, và các bất biến nghiệp vụ cơ bản; Milestone 2 sẽ mở rộng và tái cấu trúc thành Design Patterns (State Pattern 7 states, Observer Pattern với Notification/SystemLog Observer).
- **Khuyến nghị cho Milestone 2 Worker**:
  - Khi triển khai Milestone 2, chuyển các phương thức chuyển trạng thái từ `Ticket.cs` sang các State classes tương ứng (`NewState`, `AssignedState`, `InProgressState`, `WaitingState`, `ResolvedState`, `ClosedState`, `RejectedState`), kế thừa `TicketStateBase` và trả về `Result<T>`.

---

## 5. BẢNG PHÁT HIỆN THẨM ĐỊNH (FINDINGS MATRIX)

| ID | Mức độ | Vị trí | Hiện tượng / Vấn đề | Đánh giá & Hướng xử lý |
|:---:|:---:|---|---|---|
| F-01 | **Informational** | `src/ItTms.Domain/Entities/Ticket.cs` & `ConcurrencyTokenInterceptor.cs` | Token đổi mới ở cả 2 tầng (Domain & Interceptor). | Hợp lệ (Defense-in-depth). Không cần sửa. |
| F-02 | **Informational** | `src/ItTms.Domain/Entities/User.cs:113` | Kiểm tra Admin không tự vô hiệu hóa: `if (Id == operatingAdminId)`. | Hoạt động chính xác khi Id được cấp từ CSDL ($>0$). |
| F-03 | **Note for M2** | `src/ItTms.Domain/States/` | Cần triển khai 7 State classes theo State Pattern. | Thuộc đúng phạm vi Milestone 2 theo `PROJECT.md`. |

*Không có phát hiện ở mức Critical hay Major.*

---

## 6. KẾT LUẬN & ĐỀ NGHỊ

1. **Kết luận**: Milestone 1 do `worker_m1` thực hiện đạt chất lượng xuất sắc, tuân thủ 100% nguyên tắc Clean Architecture, 8 Core Entities hoàn chỉnh, SLA DueAt chính xác, Covering Indexes tối ưu, Concurrency Token chống Lost Updates hiệu quả, và 30/30 tests kiểm thử tự động pass tuyệt đối.
2. **Quyết định (Verdict)**: **`APPROVE`**.
3. **Đề nghị**: Chuyển giao toàn bộ nền tảng sang Milestone 2 (Design Patterns & Business Logic Engine) để triển khai State Pattern 7 states, Observer Pattern và `AuthorizeRolesAttribute`.
