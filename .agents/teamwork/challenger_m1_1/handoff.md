# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — CHALLENGER M1_1

> **Agent**: `challenger_m1_1`  
> **Vai trò**: critic, specialist  
> **Người nhận**: Orchestrator (`9e3cad2c-3e7f-49c0-9814-015216856d2e`) / Milestone 2 Lead  
> **Thư mục làm việc**: `c:\IT_System\.agents\teamwork\challenger_m1_1`  
> **Phán quyết chính thức**: **`APPROVE`**  
> **Loại bàn giao**: Hard Handoff (Hoàn tất nhiệm vụ)  
> **Ngày hoàn tất**: 2026-10-25  

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM)

1. **Cơ chế Khóa Lạc quan & Concurrency Token**:
   - `src/ItTms.Domain/Entities/Ticket.cs` (dòng 32, 86, 260-263): Thực thể `Ticket` sở hữu thuộc tính `public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();` và phương thức `RenewConcurrencyToken()`.
   - `src/ItTms.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` (dòng 43-45): Cấu hình Fluent API `builder.Property(t => t.ConcurrencyToken).IsConcurrencyToken().IsRequired();`.
   - `src/ItTms.Infrastructure/Persistence/Interceptors/ConcurrencyTokenInterceptor.cs` (dòng 32-40): Tự động gán `entry.Entity.ConcurrencyToken = Guid.NewGuid();` và `entry.Entity.UpdatedAt = utcNow;` cho mọi thực thể `Ticket` có `entry.State == EntityState.Modified`.
2. **Thuật toán Tính toán SLA DueAt & Trạng thái Quá hạn**:
   - `src/ItTms.Domain/Entities/Ticket.cs` (dòng 98-108): `CalculateDueAt(TicketPriority priority, DateTime createdAt)` cộng giờ chính xác theo mức độ ưu tiên: Critical (+1h), High (+4h), Medium (+24h), Low (+72h), và default fallback (+24h).
   - `src/ItTms.Domain/Extensions/TicketSlaExtensions.cs` (dòng 7-22): `IsOverdue` kiểm tra `ticket.DueAt < now` và loại trừ 3 trạng thái đã hoàn tất/dừng xử lý (`Resolved`, `Closed`, `Rejected`). `RemainingTime` tính `ticket.DueAt - now`.
3. **Thực thi Bộ Kiểm thử Nghịch đảo Chuyên sâu**:
   - Đã biên soạn hai tệp kiểm thử áp lực tại:
     - `tests/ItTms.Tests/IntegrationTests/Persistence/AdversarialConcurrencyStressTests.cs` (13 test cases)
     - `tests/ItTms.Tests/UnitTests/Domain/AdversarialSlaStressTests.cs` (24 test cases)
   - Lệnh thực thi:
     ```powershell
     dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --filter "FullyQualifiedName~AdversarialConcurrencyStressTests|FullyQualifiedName~AdversarialSlaStressTests" --verbosity normal
     ```
   - Kết quả đầu ra:
     ```
     Test run for c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll (.NETCoreApp,Version=v9.0)
     Starting test execution, please wait...
     A total of 1 test files matched the specified pattern.
     ...
     Passed ItTms.Tests.IntegrationTests.Persistence.AdversarialConcurrencyStressTests.ConcurrentDisjointPropertyEdits_ThrowsDbUpdateConcurrencyException_AndPreservesRowVersionIntegrity [121 ms]
     Passed ItTms.Tests.IntegrationTests.Persistence.AdversarialConcurrencyStressTests.HighConcurrencyRace_10ConcurrentTasks_ExactlyOneSucceedsPerRound [235 ms]
     Passed ItTms.Tests.IntegrationTests.Persistence.AdversarialConcurrencyStressTests.ConflictResolution_ReloadAndRetry_SucceedsWithNewConcurrencyToken [124 ms]
     Passed ItTms.Tests.IntegrationTests.Persistence.AdversarialConcurrencyStressTests.ConcurrentStateTransitionVsDirectEdit_ThrowsDbUpdateConcurrencyException [75 ms]
     Passed ItTms.Tests.IntegrationTests.Persistence.AdversarialConcurrencyStressTests.ConcurrentUpdateAfterDelete_ThrowsDbUpdateConcurrencyException [82 ms]
     ...
     Test Run Successful.
     Total tests: 37
          Passed: 37
      Total time: 5.0438 Seconds
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     ```
4. **Hành vi Dưới Tải Đua Tranh Cao (10 Concurrent Tasks)**:
   - Trong `HighConcurrencyRace_10ConcurrentTasks_ExactlyOneSucceedsPerRound`, 10 tác vụ bất đồng bộ dùng `Barrier` để đồng bộ điểm ghi đồng thời vào 1 Ticket trong SQLite (WAL mode).
   - Kết quả: `successCount == 1`, `conflictCount == 9`, `otherErrors` rỗng (0 lỗi database lock, 0 exception ngoài ý muốn).

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN LOGIC)

1. **Từ Observation 1 & 3**:
   - Khi hai phiên làm việc (`context1`, `context2`) nạp cùng 1 bản ghi `Ticket` với token ban đầu $T_0$, EF Core thay thế câu lệnh SQL UPDATE thành:
     `UPDATE Tickets SET ..., ConcurrencyToken = @p0, UpdatedAt = @p1 WHERE Id = @p2 AND ConcurrencyToken = @p3`
   - Khi `context1` lưu trước, token trong SQLite chuyển thành $T_1$. Khi `context2` thực thi, mệnh đề `WHERE ConcurrencyToken = @p3` ($T_0$) trả về 0 hàng cập nhật.
   - EF Core lập tức ném `DbUpdateConcurrencyException`. Thử nghiệm `ConcurrentDisjointPropertyEdits` và `ConflictResolution_ReloadAndRetry` chứng minh CSDL bảo toàn 100% dữ liệu của người ghi trước, ngăn chặn triệt để lỗi Lost Updates.
2. **Từ Observation 1 & 4**:
   - Dưới tải cạnh tranh 10 luồng song song, cấu hình SQLite PRAGMAs (`WAL`, `busy_timeout = 5000`) bảo đảm SQLite engine xếp hàng các transaction ghi an toàn, loại bỏ lỗi `database is locked`, cho phép `ConcurrencyTokenInterceptor` và Change Tracker của EF Core xử lý xung đột theo đúng chuẩn mực phân phối.
3. **Từ Observation 2 & 3**:
   - Phép toán cộng giờ của SLA `DueAt` dựa trên `DateTime.AddHours()` với `DateTimeKind.Utc` độc lập hoàn toàn với quy tắc giờ mùa hè (DST) của hệ điều hành máy khách, bảo toàn tính đơn điệu trên trục thời gian.
   - Thử nghiệm trên năm nhuận 2028 (nhảy qua ngày 29/02), năm không nhuận 2026 (chuyển tiếp thẳng sang 01/03), và quy tắc năm 2000 (nhuận thế kỷ) vs 2100 (không nhuận thế kỷ) chứng minh SLA hoàn toàn chính xác theo lịch Gregory quốc tế.
   - Thuộc tính `IsOverdue` phản ánh chính xác ranh giới tới mức độ tick (100 nanoseconds), và tự động loại bỏ cờ quá hạn khi ticket đã chuyển sang `Resolved`, `Closed` hoặc `Rejected`.

---

## 3. CAVEATS (CÁC ĐIỂM LƯU Ý & GIẢ ĐỊNH)

1. **Validation Enum Giá trị Ngoài dải**: Trong `Ticket.Create`, các giá trị enum không xác định (ví dụ `(TicketPriority)99`) không bị ném ngoại lệ hay trả về `Result.Failure`, mà fallback an toàn về SLA 24h. Tầng Application / ViewModel (Milestone 2 & 3) nên bổ sung kiểm tra `Enum.IsDefined` để từ chối sớm ở mức validation.
2. **Định dạng `RemainingTime` khi trễ hạn**: `RemainingTime` trả về `TimeSpan` mang giá trị âm khi ticket đã quá hạn. Tầng View (Milestone 3) cần định dạng chuỗi hiển thị thân thiện (ví dụ: "Quá hạn X giờ Y phút") thay vì hiển thị dấu trừ thô.
3. **Môi trường Cơ sở dữ liệu**: Toàn bộ kiểm thử được thực thi trên SQLite vật lý cục bộ phù hợp với đặc tả R2 của dự án. Không có giả định nào về CSDL phân tán đa cụm.

---

## 4. CONCLUSION (KẾT LUẬN & PHÁN QUYẾT CHÍNH THỨC)

**PHÁN QUYẾT CHÍNH THỨC**: **`APPROVE`**

- Milestone 1 đã xây dựng một nền tảng thực thể miền và cơ chế kiểm soát đồng thời cực kỳ vững chắc, đáp ứng trọn vẹn yêu cầu sản xuất (Production-Grade).
- Các bất biến nghiệp vụ, Concurrency Token, và thuật toán SLA đã được kiểm chứng bằng 37 test cases áp lực cao và vượt qua 100%.
- Không phát hiện bất kỳ lỗi nghẽn khóa (deadlock/leak), mất mát dữ liệu đồng thời (lost updates), hoặc sai số thời gian SLA nào.
- Đủ điều kiện và sẵn sàng tuyệt đối để chuyển giao sang **Milestone 2 (Design Patterns & Business Logic Engine)**.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

Để tái lập và kiểm chứng độc lập toàn bộ kết quả trên, thực thi các bước sau tại `c:\IT_System`:

1. **Chạy Bộ Kiểm thử Nghịch đảo Concurrency & SLA (37/37 Tests Passed)**:
   ```powershell
   dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --filter "FullyQualifiedName~AdversarialConcurrencyStressTests|FullyQualifiedName~AdversarialSlaStressTests" --verbosity normal
   ```
   *Điều kiện đạt*: Toàn bộ 37 tests hiển thị `Passed`, 0 `Failed`, Exit code = `0`.

2. **Kiểm tra File Báo cáo Chi tiết**:
   - `c:\IT_System\.agents\teamwork\challenger_m1_1\challenge_report.md`
   - `c:\IT_System\tests\ItTms.Tests\IntegrationTests\Persistence\AdversarialConcurrencyStressTests.cs`
   - `tests\ItTms.Tests\UnitTests\Domain\AdversarialSlaStressTests.cs`
