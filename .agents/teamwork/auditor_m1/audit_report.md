# BÁO CÁO KIỂM TOÁN PHÁP CHỨNG TÍNH TOÀN VẸN (FORENSIC AUDIT REPORT)
**Milestone 1**: Solution Scaffolding & Core Domain Foundation  
**Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (.NET 9 - IT-TMS)  
**Kiểm toán viên**: `auditor_m1` (Forensic Auditor / Critic / Specialist)  
**Thời gian thực hiện**: 2026-10-25T08:36:00Z  
**Hồ sơ kiểm toán**: General Project (Integrity Mode: `development` theo `ORIGINAL_REQUEST.md`)  
**Phán quyết pháp chứng (Forensic Verdict)**: **CLEAN**

---

## 1. TỔNG QUAN PHÁN QUYẾT (EXECUTIVE VERDICT — BLUF)

Toàn bộ sản phẩm bàn giao Milestone 1 của `worker_m1` đạt chuẩn toàn vẹn mã nguồn ở mức độ cao nhất:
- **KHÔNG** phát hiện bất kỳ dấu hiệu hardcode kết quả kiểm thử, token giả tạo (fake token), hay cài cắm chuỗi cố định.
- **KHÔNG** phát hiện hiện thực giả dạng (facade implementation), phương thức rỗng, hoặc bypass ràng buộc nghiệp vụ.
- **XÁC MINH THỰC NGHIỆM ĐỘC LẬP**:
  1. Biên dịch toàn bộ Solution: **0 Warning(s), 0 Error(s)** dưới cờ `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
  2. Thực thi bộ kiểm thử độc lập: **30/30 tests PASSED (100%)** trong thời gian 4.40 giây.
  3. Lược đồ CSDL SQLite (`ittms.db`) gồm đủ 8 bảng chuẩn, 4 covering indexes, và 1 bản ghi `20261025082220_InitialCreate` trong `__EFMigrationsHistory`.
  4. Cơ chế băm mật khẩu: Chuẩn PBKDF2 HMAC-SHA512 với Salt ngẫu nhiên 128-bit riêng biệt cho từng người dùng, không trùng lặp salt/hash.
  5. Cơ chế kiểm soát đồng thời: `ConcurrencyToken` kiểu GUID động, tự động cấp mới qua EF Core `ConcurrencyTokenInterceptor` và xử lý triệt để xung đột Lost Updates ném `DbUpdateConcurrencyException`.

---

## 2. KẾT QUẢ KIỂM TRA PHÁP CHỨNG THEO TỪNG HẠNG MỤC (PHASE RESULTS)

| STT | Hạng mục kiểm tra pháp chứng | Phương pháp kiểm chứng | Kết quả | Chi tiết quan sát |
|:---:|---|---|:---:|---|
| 1 | **Phát hiện Hardcoded Test Results & Fake Values** | Static Analysis / Grep AST (`return <const>`, token literals) | **PASS** | Không tồn tại bất kỳ chuỗi token hoặc kết quả băm hardcode nào. Tất cả đều sinh động (`Guid.NewGuid()`, PBKDF2 salt). |
| 2 | **Phát hiện Facade Implementation** | Quét toàn bộ mã nguồn `src/` tìm dummy returns, `NotImplementedException`, stub methods | **PASS** | `src/ItTms.Domain` chứa đầy đủ logic Rich Domain Model (SLA calculation, CanComment, Reject InProgress block, State transitions). 0 phương thức facade. |
| 3 | **Phát hiện Pre-populated Artifacts** | Quét hệ thống tệp tìm log/output sinh sẵn trước kiểm toán | **PASS** | 0 tệp `*.log`, 0 tệp output kết quả làm giả tồn tại trong workspace trước khi auditor chạy kiểm thử. |
| 4 | **Xác minh Thuật toán Băm Mật khẩu (Cryptographic Hashing)** | Đọc mã nguồn `PasswordHasherService` & truy vấn trực tiếp bảng `Users` trong `ittms.db` | **PASS** | Mọi mật khẩu được băm qua `Microsoft.AspNetCore.Identity.PasswordHasher<User>` (chuẩn Identity V3: PBKDF2 HMAC-SHA512, 100,000 vòng, salt 128-bit). 8 tài khoản có 8 chuỗi hash khác biệt hoàn toàn (đều bắt đầu bằng tiền tố Base64 định dạng `AQAAAAIAAYagAAAA...`). |
| 5 | **Xác minh EF Core Migration & Lược đồ SQLite** | Kiểm tra migration C# CLI & truy vấn `sqlite_master` trong `ittms.db` | **PASS** | Đầy đủ 8 bảng nghiệp vụ + `__EFMigrationsHistory` + 4 covering indexes (`IX_Tickets_AssignedToId_StatusId`, `IX_Tickets_CreatorId_CreatedAt`, `IX_Tickets_StatusId_DueAt`, `IX_Tickets_StatusId_Priority_CreatedAt`). |
| 6 | **Xác minh Concurrency Token & Interceptor** | Kiểm tra `TicketConfiguration`, `ConcurrencyTokenInterceptor` & kiểm thử độc lập xung đột đồng thời | **PASS** | `ConcurrencyToken` được đánh dấu `.IsConcurrencyToken()`. Kiểm thử đồng thời xác nhận `DbUpdateConcurrencyException` được ném chính xác khi cập nhật bản ghi cũ. |
| 7 | **Xác minh Độc lập Biên dịch & Kiểm thử (Build & Run)** | Chạy trực tiếp `dotnet build` và `dotnet test` từ shell | **PASS** | Build thành công 0 warning, 0 error. 30/30 tests vượt qua trong 4.40s. |
| 8 | **Kiểm toán Phụ thuộc & Chế độ Toàn vẹn (Integrity Mode)** | Đối chiếu `ORIGINAL_REQUEST.md` (`development`) | **PASS** | Sử dụng đúng các thư viện chuẩn Microsoft (.NET 9, EF Core 9 SQLite, FluentAssertions, Moq, xUnit). Không vi phạm bất kỳ cấm đoán nào. |

---

## 3. BẰNG CHỨNG PHÁP CHỨNG THỰC NGHIỆM (RAW FORENSIC EVIDENCE)

### 3.1. Bằng chứng Biên dịch Toàn bộ Solution
```
Lệnh thực thi: dotnet build c:\IT_System\ItTms.sln
Mã thoát (Exit Code): 0
Output:
  Determining projects to restore...
  All projects are up-to-date for restore.
  ItTms.Domain -> c:\IT_System\src\ItTms.Domain\bin\Debug\net9.0\ItTms.Domain.dll
  ItTms.Application -> c:\IT_System\src\ItTms.Application\bin\Debug\net9.0\ItTms.Application.dll
  ItTms.Infrastructure -> c:\IT_System\src\ItTms.Infrastructure\bin\Debug\net9.0\ItTms.Infrastructure.dll
  ItTms.Web -> c:\IT_System\src\ItTms.Web\bin\Debug\net9.0\ItTms.Web.dll
  ItTms.Tests -> c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:05.81
```

### 3.2. Bằng chứng Thực thi Bộ Kiểm thử Độc lập
```
Lệnh thực thi: dotnet test c:\IT_System\ItTms.sln --verbosity normal
Mã thoát (Exit Code): 0
Output trích lục:
Test run for c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.0.2+dd36e86129 (64-bit .NET 9.0.19)
[xUnit.net 00:00:00.14]   Discovering: ItTms.Tests
[xUnit.net 00:00:00.22]   Discovered:  ItTms.Tests
[xUnit.net 00:00:00.27]   Starting:    ItTms.Tests
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 3, expectedCanComment: True) [19 ms]
  Passed ItTms.Tests.UnitTests.Domain.ResultMonadTests.Failure_CreatesFailureResultWithSpecifiedError [21 ms]
  Passed ItTms.Tests.UnitTests.Domain.UserInvariantTests.CreateUser_WithValidParameters_InitializesCorrectly [24 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 1, expectedCanComment: True) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 2, expectedCanComment: True) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 7, expectedCanComment: False) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 5, expectedCanComment: False) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 6, expectedCanComment: False) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CanComment_ReturnsExpectedResultBasedOnStatus(statusId: 4, expectedCanComment: True) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.UserInvariantTests.Deactivate_WhenAdminDeactivatesThemselves_Fails_Table72TC10 [1 ms]
  Passed ItTms.Tests.UnitTests.Domain.UserInvariantTests.Deactivate_WhenAdminDeactivatesOtherAccount_Succeeds [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.Reject_WhenTicketIsInProgress_ReturnsFailure_Table72TC06 [4 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.IsOverdue_DetectsSlaBreachCorrectly [1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CalculateDueAt_MatchesSlaSpecification(priority: Medium, expectedHours: 24) [1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CalculateDueAt_MatchesSlaSpecification(priority: Low, expectedHours: 72) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CalculateDueAt_MatchesSlaSpecification(priority: Critical, expectedHours: 1) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CalculateDueAt_MatchesSlaSpecification(priority: High, expectedHours: 4) [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.CreateTicket_WithValidData_InitializesInNewStateAndCalculatesSla [4 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.FullLifecycle_FromNewToClosed_TransitionsCorrectly [2 ms]
  Passed ItTms.Tests.UnitTests.Domain.TicketInvariantTests.Reject_WhenTicketIsNew_Succeeds [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.ResultMonadTests.TypedResult_AccessingValueOnFailure_ThrowsInvalidOperationException [23 ms]
  Passed ItTms.Tests.UnitTests.Domain.ResultMonadTests.TypedResult_ImplicitConversion_WorksCorrectly [< 1 ms]
  Passed ItTms.Tests.UnitTests.Domain.ResultMonadTests.Success_CreatesSuccessResultWithoutError [< 1 ms]
  Passed ItTms.Tests.UnitTests.Security.PasswordHasherTests.HashPassword_ProducesNonEmptySaltedHash [261 ms]
  Passed ItTms.Tests.UnitTests.Security.PasswordHasherTests.VerifyPassword_ValidatesCorrectly(candidatePassword: "", expectedResult: False) [220 ms]
  Passed ItTms.Tests.UnitTests.Security.PasswordHasherTests.VerifyPassword_ValidatesCorrectly(candidatePassword: "Admin@123", expectedResult: True) [200 ms]
  Passed ItTms.Tests.UnitTests.Security.PasswordHasherTests.VerifyPassword_ValidatesCorrectly(candidatePassword: "admin@123", expectedResult: False) [207 ms]
  Passed ItTms.Tests.UnitTests.Security.PasswordHasherTests.VerifyPassword_ValidatesCorrectly(candidatePassword: "Admin@124", expectedResult: False) [198 ms]
  Passed ItTms.Tests.IntegrationTests.Persistence.OptimisticConcurrencyTests.ConcurrentUpdates_WhenSecondUserModifiesStaleTicket_ThrowsDbUpdateConcurrencyException [2 s]
[xUnit.net 00:00:03.52]   Finished:    ItTms.Tests
  Passed ItTms.Tests.IntegrationTests.Persistence.DatabaseMigrationAndSeederTests.MigrateAndSeed_ExecutesSuccessfullyAndPopulatesAllRequiredData [3 s]

Test Run Successful.
Total tests: 30
     Passed: 30
 Total time: 4.4020 Seconds
```

### 3.3. Bằng chứng Trích xuất Pháp chứng CSDL SQLite (`ittms.db`)
Trích lục trực tiếp từ tệp CSDL vật lý `src/ItTms.Web/ittms.db`:
```
--- SQLITE TABLES ---
Table: Attachments
Table: Comments
Table: Feedbacks
Table: Notifications
Table: SystemLogs
Table: TicketStatuses
Table: Tickets
Table: Users
Table: __EFMigrationsHistory
Table: __EFMigrationsLock
Table: sqlite_sequence

--- ROW COUNTS ---
TicketStatuses: 7
Users: 8
Tickets: 3
Comments: 2
Feedbacks: 1
Attachments: 0
Notifications: 0
SystemLogs: 0
__EFMigrationsHistory: 1

--- TICKET STATUSES ---
Status 1: New | Mới tạo | badge bg-primary | Order:1
Status 2: Assigned | Đã phân công | badge bg-info text-dark | Order:2
Status 3: InProgress | Đang xử lý | badge bg-warning text-dark | Order:3
Status 4: Waiting | Chờ phản hồi | badge bg-secondary | Order:4
Status 5: Resolved | Đã giải quyết | badge bg-success | Order:5
Status 6: Closed | Đã đóng | badge bg-dark | Order:6
Status 7: Rejected | Đã từ chối | badge bg-danger | Order:7

--- SEEDED USERS ---
User 1: admin (admin@ittms.local) | Role: Admin | Status: Active | Hash: AQAAAAIAAYagAAAAEAc0R982qCtixEI2G86...
User 2: leader (leader@ittms.local) | Role: Leader | Status: Active | Hash: AQAAAAIAAYagAAAAEN2poB2kDELX6OS8XYy...
User 3: staff1 (staff1@ittms.local) | Role: Staff | Status: Active | Hash: AQAAAAIAAYagAAAAELT07BiB42Z7oqma2cX...
User 4: staff2 (staff2@ittms.local) | Role: Staff | Status: Active | Hash: AQAAAAIAAYagAAAAEBabgJ5i7TLBMBMQqpf...
User 5: user1 (user1@ittms.local) | Role: User | Status: Active | Hash: AQAAAAIAAYagAAAAEPp3aIG9Kp7nt2YaRd1...
User 6: user2 (user2@ittms.local) | Role: User | Status: Active | Hash: AQAAAAIAAYagAAAAEH2k1tMcgawCknDIGP5...
User 7: deactivated (deactivated@ittms.local) | Role: User | Status: Inactive | Hash: AQAAAAIAAYagAAAAEPCErND/Xt9X6ki5Mn+...
User 8: inactive_user (inactive_user@ittms.local) | Role: User | Status: Inactive | Hash: AQAAAAIAAYagAAAAEJx6/9+VeP8XQkSdx/H...

--- DEMO TICKETS ---
Ticket #1: 'Máy tính phòng kế toán không bật được nguồn' | Prio:High | Cat:Hardware | Status:1 | ConcurrencyToken:5A4919C0-16F8-4CA5-9165-E8794A61D035 | Due:2026-10-25 11:57:18.7672546 | Created:2026-10-25 07:57:18.7672546
Ticket #2: 'Mất kết nối Internet toàn bộ khu vực tầng 3' | Prio:Critical | Cat:Network | Status:3 | ConcurrencyToken:6461968A-A237-446C-A344-C602D03F755A | Due:2026-10-25 07:27:18.7672546 | Created:2026-10-25 06:27:18.7672546
Ticket #3: 'Cài đặt phần mềm VPN làm việc từ xa' | Prio:Medium | Cat:Software | Status:6 | ConcurrencyToken:30D101A1-36F3-4197-9477-6C4C52FA0963 | Due:2026-10-25 08:27:18.7672546 | Created:2026-10-24 08:27:18.7672546

--- COMMENTS ---
Comment #1 (Ticket 2, User 3): Chào chị B, em đang kiểm tra switch trung tâm tại phòng server tầng 3, nghi ngờ bị sập nguồn.
Comment #2 (Ticket 2, User 6): Cảm ơn em, nhờ team xử lý sớm giúp phòng nhân sự chuẩn bị tài liệu phỏng vấn.

--- FEEDBACKS ---
Feedback #1 (Ticket 3): Rating 5* | 'Kỹ thuật viên hỗ trợ rất nhiệt tình và hướng dẫn chi tiết cách kết nối VPN!'

--- TICKET COVERING INDEXES ---
Index: IX_Tickets_AssignedToId_StatusId => CREATE INDEX "IX_Tickets_AssignedToId_StatusId" ON "Tickets" ("AssignedToId", "StatusId")
Index: IX_Tickets_CreatorId_CreatedAt => CREATE INDEX "IX_Tickets_CreatorId_CreatedAt" ON "Tickets" ("CreatorId", "CreatedAt")
Index: IX_Tickets_StatusId_DueAt => CREATE INDEX "IX_Tickets_StatusId_DueAt" ON "Tickets" ("StatusId", "DueAt")
Index: IX_Tickets_StatusId_Priority_CreatedAt => CREATE INDEX "IX_Tickets_StatusId_Priority_CreatedAt" ON "Tickets" ("StatusId", "Priority", "CreatedAt")

--- EF MIGRATIONS HISTORY ---
Applied Migration: 20261025082220_InitialCreate (EF Core 9.0.2)
```

---

## 4. KẾT LUẬN & KIẾN NGHỊ

Sản phẩm Milestone 1 hoàn toàn đạt chuẩn trung thực kỹ thuật và sẵn sàng phục vụ làm nền tảng cho Milestone 2 (Design Patterns & Business Logic Engine) mà không cần chỉnh sửa hay tái cấu trúc nền móng.

**Phán quyết cuối cùng**: **CLEAN**
