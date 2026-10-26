# 🏗️ KẾ HOẠCH THIẾT KẾ SCAFFOLDING .NET 9 SOLUTION (ItTms.sln)
**Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
**Kiến trúc**: Pragmatic Clean Architecture + 5 MVC Areas + Real-Time SignalR WebSockets  
**Target Framework**: .NET 9 (`net9.0`) | C# 13 | EF Core 9 (SQLite)  
**Đơn vị thực hiện**: `explorer_m1_1`  
**Ngày lập**: 2026-10-25  
**Phương pháp luận**: Chuẩn nhận thức `#Argon-Omega` (BLUF-MECE, First-Principles, Causal Constructivism)

---

## 1. TÓM TẮT ĐIỀU HÀNH & MỤC TIÊU KIẾN TRÚC (BLUF)

1. **Hiện trạng Môi trường**:
   - Hệ điều hành máy chủ: Windows Enterprise x64 (Build 10.0.26200).
   - .NET SDK đã cài đặt: **`9.0.317`** (C:\Program Files\dotnet\sdk\9.0.317\).
   - .NET Runtimes: `Microsoft.AspNetCore.App 9.0.19`, `Microsoft.NETCore.App 9.0.19`.
   - Kết luận: Môi trường hoàn toàn sẵn sàng 100% để khởi tạo giải pháp .NET 9 mà không cần cài thêm bất kỳ SDK hay runtime ngoại vi nào.

2. **Mục tiêu Scaffolding (To-Be Invariants)**:
   - Khởi tạo Solution trung tâm `ItTms.sln` tại thư mục gốc `c:\IT_System`.
   - Phân rã hệ thống thành 5 dự án độc lập tuân thủ nghiêm ngặt **Pragmatic Clean Architecture**:
     - `src/ItTms.Domain`: Class Library, net9.0, **0 external dependencies** (Zero-ORM, Zero-Framework).
     - `src/ItTms.Application`: Class Library, net9.0, chỉ tham chiếu `Domain`, cung cấp Interfaces, DTOs, Use Case Slices, Observers.
     - `src/ItTms.Infrastructure`: Class Library, net9.0, tham chiếu `Application` và `Domain`, tích hợp EF Core 9 SQLite, Migrations, DataSeeder, PasswordHasher, SignalR Dispatcher.
     - `src/ItTms.Web`: ASP.NET Core MVC (Web SDK), net9.0, tham chiếu `Application`, `Infrastructure`, `Domain`, chứa 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`), `TicketHub` SignalR endpoint, Bootstrap 5 UI.
     - `tests/ItTms.Tests`: xUnit Test Project, net9.0, tham chiếu cả 4 projects trên, chứa State Machine tests, Concurrency tests, SignalR tests, và bộ 10 kịch bản kiểm thử Bảng 7.2.
   - Thiết lập cấu hình **Warning-Free Build** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<Nullable>enable</Nullable>`, `<AnalysisLevel>latest-recommended</AnalysisLevel>`) thông qua `Directory.Build.props` dùng chung ở thư mục gốc.
   - Toàn bộ gói NuGet được xác thực tương thích tuyệt đối với `net9.0`, chốt phiên bản mới nhất và ổn định nhất thuộc nhánh 9.0 (`9.0.21` cho hệ sinh thái Microsoft/EF Core, `7.2.2` cho FluentAssertions tránh vấn đề bản quyền thương mại v8).

---

## 2. CẤU TRÚC SOLUTION VÀ MA TRẬN PHỤ THUỘC (DEPENDENCY MATRIX)

### 2.1. Sơ đồ Luồng Phụ thuộc (Clean Architecture Invariant)

```text
               ┌────────────────────────┐
               │    src/ItTms.Domain    │ ◄── [0 external dependencies / Pure C#]
               └───────────▲────────────┘
                           │
             ┌─────────────┴─────────────┐
             │                           │
  ┌──────────┴──────────┐     ┌──────────┴──────────┐
  │ src/ItTms.Application│     │src/ItTms.Infrastructure│
  └──────────▲──────────┘     └──────────▲──────────┘
             │                           │
             ├───────────────────────────┘
             │
   ┌─────────┴─────────┐
   │    src/ItTms.Web  │ ◄── [Host Project: MVC 5 Areas + SignalR Hub]
   └─────────▲─────────┘
             │
 ════════════╪═══════════════════════════════════════════════════
             │
   ┌─────────┴─────────┐
   │  tests/ItTms.Tests│ ◄── [Test Harness: Unit, Integration, SignalR, E2E]
   └───────────────────┘
```

### 2.2. Ma trận Tham chiếu Dự án (Project Reference Matrix)

| Dự án mục tiêu | Tham chiếu đến Domain | Tham chiếu đến Application | Tham chiếu đến Infrastructure | Tham chiếu đến Web | Lý do kiến trúc & Ràng buộc bảo vệ |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **`ItTms.Domain`** | ❌ (Chính nó) | ❌ | ❌ | ❌ | **Bất biến cốt lõi**: Không phụ thuộc bất kỳ thư viện hay tầng nào khác. Không nạp EF Core vào Domain. |
| **`ItTms.Application`** | ✅ | ❌ (Chính nó) | ❌ | ❌ | Chứa các giao diện (`IItTmsDbContext`, `IPasswordHasher`, `INotificationDispatcher`) và điều phối Domain Entities. |
| **`ItTms.Infrastructure`**| ✅ | ✅ | ❌ (Chính nó) | ❌ | Hiện thực hóa các interface của Application, cài đặt EF Core SQLite, cấu hình Fluent API, Migrations, SignalR Notification Dispatcher. |
| **`ItTms.Web`** | ✅ | ✅ | ✅ | ❌ (Chính nó) | Đóng vai trò Composition Root; nạp DI, cấu hình Middleware pipeline, định tuyến 5 MVC Areas và SignalR WebSocket endpoint. |
| **`ItTms.Tests`** | ✅ | ✅ | ✅ | ✅ | Kiểm thử đa tầng: Kiểm thử đơn vị State Pattern (Domain), kiểm thử Concurrency/Persistence (Infrastructure), kiểm thử Endpoints & SignalR WebSocket (Web). |

---

## 3. CHUỖI LỆNH CLI SCAFFOLDING TOÀN DIỆN (EXECUTABLE CLI PIPELINE)

Các lệnh dưới đây được thiết kế để chạy trực tiếp trên Windows PowerShell tại thư mục gốc `c:\IT_System`. Implementer chỉ cần copy-paste theo thứ tự tuần tự:

### Giai đoạn 1: Khởi tạo Solution và Cấu trúc Thư mục

```powershell
# 1. Đảm bảo đứng tại thư mục gốc
cd c:\IT_System

# 2. Tạo file Solution trung tâm
dotnet new sln -n ItTms

# 3. Tạo 4 dự án nguồn (src) và 1 dự án kiểm thử (tests) với target net9.0
dotnet new classlib -n ItTms.Domain -o src/ItTms.Domain -f net9.0
dotnet new classlib -n ItTms.Application -o src/ItTms.Application -f net9.0
dotnet new classlib -n ItTms.Infrastructure -o src/ItTms.Infrastructure -f net9.0
dotnet new mvc -n ItTms.Web -o src/ItTms.Web -f net9.0
dotnet new xunit -n ItTms.Tests -o tests/ItTms.Tests -f net9.0

# 4. Dọn dẹp các tệp mẫu mặc định để tránh warning lớp rỗng và xung đột namespace
Remove-Item -Path "src/ItTms.Domain/Class1.cs" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "src/ItTms.Application/Class1.cs" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "src/ItTms.Infrastructure/Class1.cs" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "tests/ItTms.Tests/UnitTest1.cs" -Force -ErrorAction SilentlyContinue
```

### Giai đoạn 2: Đăng ký Dự án vào Solution

```powershell
# Thêm toàn bộ 5 dự án vào file Solution ItTms.sln
dotnet sln ItTms.sln add src/ItTms.Domain/ItTms.Domain.csproj
dotnet sln ItTms.sln add src/ItTms.Application/ItTms.Application.csproj
dotnet sln ItTms.sln add src/ItTms.Infrastructure/ItTms.Infrastructure.csproj
dotnet sln ItTms.sln add src/ItTms.Web/ItTms.Web.csproj
dotnet sln ItTms.sln add tests/ItTms.Tests/ItTms.Tests.csproj
```

### Giai đoạn 3: Thiết lập Project References

```powershell
# 1. ItTms.Application tham chiếu Domain
dotnet add src/ItTms.Application/ItTms.Application.csproj reference src/ItTms.Domain/ItTms.Domain.csproj

# 2. ItTms.Infrastructure tham chiếu Application và Domain
dotnet add src/ItTms.Infrastructure/ItTms.Infrastructure.csproj reference src/ItTms.Application/ItTms.Application.csproj
dotnet add src/ItTms.Infrastructure/ItTms.Infrastructure.csproj reference src/ItTms.Domain/ItTms.Domain.csproj

# 3. ItTms.Web tham chiếu Application, Infrastructure, Domain
dotnet add src/ItTms.Web/ItTms.Web.csproj reference src/ItTms.Application/ItTms.Application.csproj
dotnet add src/ItTms.Web/ItTms.Web.csproj reference src/ItTms.Infrastructure/ItTms.Infrastructure.csproj
dotnet add src/ItTms.Web/ItTms.Web.csproj reference src/ItTms.Domain/ItTms.Domain.csproj

# 4. tests/ItTms.Tests tham chiếu cả 4 dự án
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj reference src/ItTms.Domain/ItTms.Domain.csproj
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj reference src/ItTms.Application/ItTms.Application.csproj
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj reference src/ItTms.Infrastructure/ItTms.Infrastructure.csproj
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj reference src/ItTms.Web/ItTms.Web.csproj
```

### Giai đoạn 4: Cài đặt NuGet Packages cho net9.0

```powershell
# 1. ItTms.Application: EF Core Abstractions cho IItTmsDbContext (DbSet<T>, async linq)
dotnet add src/ItTms.Application/ItTms.Application.csproj package Microsoft.EntityFrameworkCore --version 9.0.21

# 2. ItTms.Infrastructure: EF Core SQLite provider, Migrations & Tools
dotnet add src/ItTms.Infrastructure/ItTms.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.21
dotnet add src/ItTms.Infrastructure/ItTms.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.21
dotnet add src/ItTms.Infrastructure/ItTms.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools --version 9.0.21

# 3. ItTms.Web: EF Core Design để hỗ trợ dotnet ef CLI
dotnet add src/ItTms.Web/ItTms.Web.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.21

# 4. tests/ItTms.Tests: Frameworks kiểm thử và Mocking
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj package FluentAssertions --version 7.2.2
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj package Moq --version 4.20.72
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 9.0.21
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj package Microsoft.AspNetCore.SignalR.Client --version 9.0.21
dotnet add tests/ItTms.Tests/ItTms.Tests.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.21
```

### Giai đoạn 5: Tạo khung thư mục phân tầng nội bộ (Directory Layout)

```powershell
# Tạo các thư mục con trong ItTms.Domain
New-Item -ItemType Directory -Force -Path "src/ItTms.Domain/Common"
New-Item -ItemType Directory -Force -Path "src/ItTms.Domain/Entities"
New-Item -ItemType Directory -Force -Path "src/ItTms.Domain/Enums"
New-Item -ItemType Directory -Force -Path "src/ItTms.Domain/Events"
New-Item -ItemType Directory -Force -Path "src/ItTms.Domain/States"

# Tạo các thư mục con trong ItTms.Application
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/Common/Interfaces"
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/DTOs"
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/Features/Tickets"
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/Features/Users"
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/Features/Comments"
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/Features/Notifications"
New-Item -ItemType Directory -Force -Path "src/ItTms.Application/Observers"

# Tạo các thư mục con trong ItTms.Infrastructure
New-Item -ItemType Directory -Force -Path "src/ItTms.Infrastructure/Persistence/Configurations"
New-Item -ItemType Directory -Force -Path "src/ItTms.Infrastructure/Persistence/Migrations"
New-Item -ItemType Directory -Force -Path "src/ItTms.Infrastructure/Seed"
New-Item -ItemType Directory -Force -Path "src/ItTms.Infrastructure/Security"
New-Item -ItemType Directory -Force -Path "src/ItTms.Infrastructure/Realtime"

# Tạo các thư mục con trong ItTms.Web
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Auth/Controllers"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Auth/Views"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/User/Controllers"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/User/Views"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Staff/Controllers"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Staff/Views"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Leader/Controllers"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Leader/Views"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Admin/Controllers"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Areas/Admin/Views"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Filters"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/Hubs"
New-Item -ItemType Directory -Force -Path "src/ItTms.Web/ViewModels"

# Tạo các thư mục con trong tests/ItTms.Tests
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/UnitTests/States"
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/UnitTests/Domain"
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/UnitTests/Observers"
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/IntegrationTests/EfCoreConcurrency"
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/IntegrationTests/Security"
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/SignalRTests"
New-Item -ItemType Directory -Force -Path "tests/ItTms.Tests/E2EVerificationTests"
```

---

## 4. DANH MỤC NUGET PACKAGES VÀ PHIÊN BẢN CHÍNH XÁC (NET9.0 CATALOG)

### 4.1. Bảng Chi tiết Gói NuGet Được Chỉ định

| Gói NuGet (Package ID) | Phiên bản Mục tiêu | Dự án Sử dụng | Vai trò & Lý do Lựa chọn Phiên bản |
| :--- | :---: | :--- | :--- |
| **`Microsoft.EntityFrameworkCore`** | `9.0.21` | `ItTms.Application`, `ItTms.Infrastructure` | Chứa `DbSet<T>`, LINQ async operations, ChangeTracker abstractions. Phiên bản `9.0.21` là bản phát hành ổn định cao nhất của dòng .NET 9 LTS. |
| **`Microsoft.EntityFrameworkCore.Sqlite`** | `9.0.21` | `ItTms.Infrastructure`, `ItTms.Tests` | Provider SQLite chuyên dụng. Cho phép vận hành cơ sở dữ liệu `it_tms.db` tự hành, zero-setup, hỗ trợ in-memory connection trong bộ test. |
| **`Microsoft.EntityFrameworkCore.Design`** | `9.0.21` | `ItTms.Infrastructure`, `ItTms.Web` | Công cụ thiết kế phục vụ các lệnh CLI `dotnet ef migrations add`, `dotnet ef database update`. Đặt cờ `PrivateAssets="all"`. |
| **`Microsoft.EntityFrameworkCore.Tools`** | `9.0.21` | `ItTms.Infrastructure` | Cung cấp PowerShell cmdlets cho EF Core. Đặt cờ `PrivateAssets="all"`. |
| **`Microsoft.AspNetCore.Mvc.Testing`** | `9.0.21` | `ItTms.Tests` | `WebApplicationFactory<Program>` giúp khởi chạy Web server ảo in-memory để kiểm thử toàn diện HTTP requests, Controllers, Cookies, và Filters mà không cần mở cổng mạng thật. |
| **`Microsoft.AspNetCore.SignalR.Client`**| `9.0.21` | `ItTms.Tests` | Client C# kết nối WebSocket tới `TicketHub` tại `/hubs/ticket`, chứng minh luồng phát tán tin nhắn real-time vượt qua kiểm thử tự động. |
| **`FluentAssertions`** | `7.2.2` | `ItTms.Tests` | Thư viện assertion dạng fluent (`result.Should().BeSuccess()`). **Lưu ý chiến lược**: Chốt tại `7.2.2` (phiên bản cuối cùng mang giấy phép nguồn mở Apache 2.0 hoàn toàn miễn phí, tránh rủi ro bản quyền thương mại của bản 8.x). |
| **`Moq`** | `4.20.72` | `ItTms.Tests` | Thư viện mock đối tượng ngoại vi chuẩn mực của .NET. |
| **`xunit`** | `2.9.3` | `ItTms.Tests` | Framework kiểm thử đơn vị hiện đại cho .NET 9. |
| **`xunit.runner.visualstudio`** | `3.0.2` | `ItTms.Tests` | Test runner tích hợp `dotnet test` và Visual Studio Test Explorer. |
| **`coverlet.collector`** | `6.0.4` | `ItTms.Tests` | Thu thập chỉ số bao phủ mã nguồn (code coverage). |

### 4.2. Giải pháp FrameworkReference trong `ItTms.Infrastructure`
Trong .NET 9, thư viện `ItTms.Infrastructure` cần triển khai `SignalRNotificationDispatcher` (cần `IHubContext<TicketHub>`) và `PasswordHasher` (cần `IPasswordHasher<User>`).
Thay vì cài đặt các gói NuGet riêng lẻ có nguy cơ xung đột phiên bản, ta áp dụng cơ chế native của .NET SDK:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```
Cơ chế này cấp quyền truy cập toàn bộ ASP.NET Core abstractions (SignalR, Security, Identity) đồng bộ hoàn hảo với runtime máy chủ mà không tăng thêm kích thước bundle.

---

## 5. CẤU HÌNH BIÊN DỊCH KHÔNG CẢNH BÁO (WARNING-FREE BUILD CONFIGURATION)

Để đạt mục tiêu tiêu chí nghiệm thu **"Solution compiles with zero warnings/errors via dotnet build"**, giải pháp sử dụng tập trung hóa thuộc tính MSBuild qua file `Directory.Build.props`.

### 5.1. File `Directory.Build.props` (Đặt tại thư mục gốc `c:\IT_System\Directory.Build.props`)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Ép toàn bộ cảnh báo thành lỗi biên dịch -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- Phân tích mã nguồn chuyên sâu -->
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <Deterministic>true</Deterministic>

    <!-- Tắt tạo file XML documentation để tránh cảnh báo CS1591 -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>

  <!-- Cấu hình riêng cho dự án kiểm thử để linh hoạt hơn trong việc gán giá trị test -->
  <PropertyGroup Condition="'$(MSBuildProjectExtension)' == '.csproj' And $(MSBuildProjectName.EndsWith('Tests'))">
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <!-- Cho phép một số cảnh báo đặc thù trong test nếu cần -->
    <NoWarn>$(NoWarn)</NoWarn>
  </PropertyGroup>
</Project>
```

### 5.2. Phân tích 6 Họ Cảnh báo Phổ biến trong .NET 9 và Chiến lược Triệt tiêu

| Mã Cảnh báo | Mô tả Cảnh báo | Nguy cơ Tiềm ẩn | Giải pháp Ngăn chặn Triệt để |
| :--- | :--- | :--- | :--- |
| **CS8618** | *Non-nullable property must contain a non-null value when exiting constructor.* | Gây lỗi build ngay lập tức khi `<Nullable>enable</Nullable>` và `<TreatWarningsAsErrors>true`. | 1. Khởi tạo thuộc tính kiểu chuỗi: `public string Title { get; set; } = string.Empty;`<br>2. Với Navigation Property của EF Core: `public virtual User Creator { get; set; } = null!;` kết hợp constructor rỗng riêng tư `private Ticket() { }`. |
| **CS8602 / CS8604** | *Dereference of a possibly null reference / Possible null reference argument.* | Rủi ro ném `NullReferenceException` tại runtime. | Áp dụng Guard Clauses, Pattern Matching `if (entity is null)` và sử dụng `Result<T>` đóng gói trạng thái. Không dùng `!` bừa bãi. |
| **CS1591** | *Missing XML comment for publicly visible type or member.* | Xuất hiện khi vô tình bật `<GenerateDocumentationFile>true`. | Thiết lập `<GenerateDocumentationFile>false</GenerateDocumentationFile>` trong `Directory.Build.props`. |
| **CS1998** | *This async method lacks 'await' operators and will run synchronously.* | Xảy ra khi viết các phương thức stub trong giai đoạn scaffolding ban đầu. | Trả về `Task.FromResult(result)` hoặc dùng `await Task.CompletedTask;` trong các handler mẫu. |
| **CA1822** | *Member can be marked as static.* | Code analysis analyzer phát hiện hàm không truy cập state instance. | Đánh dấu hàm là `static` hoặc cấu hình phù hợp trong code implementation. |
| **ASP0000 / ASP0014** | *Calling 'UseEndpoints' is obsolete / Area routing anti-patterns.* | Cảnh báo cấu hình routing lỗi thời trong ASP.NET Core MVC. | Sử dụng cú pháp chuẩn .NET 9: `app.MapControllerRoute(...)` kèm `MapAreaControllerRoute` và `app.MapHub<TicketHub>("/hubs/ticket")`. |

### 5.3. File `.gitignore` Chuẩn cho .NET 9 (Đặt tại `c:\IT_System\.gitignore`)

```gitignore
## Build and Output Binaries
[Bb]in/
[Oo]bj/
out/
[Tt]est[Rr]esults/

## Visual Studio & JetBrains IDE
.vs/
.idea/
.vscode/
*.suo
*.user
*.userosscache
*.sln.docstates

## SQLite Database Files (Ngăn chặn commit database runtime vào git)
*.db
*.db-shm
*.db-wal
it_tms.db
it_tms.db-*

## OS Generated
Thumbs.db
ehthumbs.db
Desktop.ini
.DS_Store
```

---

## 6. KHUNG THƯ MỤC CHI TIẾT TOÀN BỘ SOLUTION (DETAILED DIRECTORY LAYOUT)

```text
c:\IT_System\
├── ItTms.sln
├── Directory.Build.props
├── .gitignore
├── PROJECT.md
├── PROJECT_MEMORY.md
├── BaoCao_ITTMS_TKPMHDT (4).docx
│
├── src\
│   ├── ItTms.Domain\
│   │   ├── Common\
│   │   │   ├── BaseEntity.cs
│   │   │   ├── Result.cs
│   │   │   ├── Error.cs
│   │   │   └── IDomainEvent.cs
│   │   ├── Entities\
│   │   │   ├── User.cs
│   │   │   ├── Ticket.cs
│   │   │   ├── TicketStatus.cs
│   │   │   ├── Comment.cs
│   │   │   ├── Feedback.cs
│   │   │   ├── Attachment.cs
│   │   │   ├── Notification.cs
│   │   │   └── SystemLog.cs
│   │   ├── Enums\
│   │   │   ├── PriorityLevel.cs
│   │   │   ├── TicketCategory.cs
│   │   │   └── RoleNames.cs
│   │   ├── Events\
│   │   │   ├── TicketCreatedEvent.cs
│   │   │   ├── TicketAssignedEvent.cs
│   │   │   ├── TicketStatusChangedEvent.cs
│   │   │   ├── TicketClosedEvent.cs
│   │   │   └── CommentAddedEvent.cs
│   │   ├── States\
│   │   │   ├── ITicketState.cs
│   │   │   ├── TicketStateBase.cs
│   │   │   ├── NewState.cs
│   │   │   ├── AssignedState.cs
│   │   │   ├── InProgressState.cs
│   │   │   ├── WaitingState.cs
│   │   │   ├── ResolvedState.cs
│   │   │   ├── ClosedState.cs
│   │   │   └── RejectedState.cs
│   │   └── ItTms.Domain.csproj
│   │
│   ├── ItTms.Application\
│   │   ├── Common\
│   │   │   └── Interfaces\
│   │   │       ├── IItTmsDbContext.cs
│   │   │       ├── IPasswordHasher.cs
│   │   │       ├── ICurrentUserService.cs
│   │   │       ├── INotificationDispatcher.cs
│   │   │       └── IDateTimeProvider.cs
│   │   ├── DTOs\
│   │   │   ├── TicketDto.cs
│   │   │   ├── UserDto.cs
│   │   │   ├── CommentDto.cs
│   │   │   ├── NotificationDto.cs
│   │   │   ├── FeedbackDto.cs
│   │   │   └── SystemLogDto.cs
│   │   ├── Features\
│   │   │   ├── Tickets\
│   │   │   ├── Users\
│   │   │   ├── Comments\
│   │   │   └── Notifications\
│   │   ├── Observers\
│   │   │   ├── ITicketObserver.cs
│   │   │   ├── NotificationObserver.cs
│   │   │   └── SystemLogObserver.cs
│   │   └── ItTms.Application.csproj
│   │
│   ├── ItTms.Infrastructure\
│   │   ├── Persistence\
│   │   │   ├── ItTmsDbContext.cs
│   │   │   ├── Configurations\
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── TicketConfiguration.cs
│   │   │   │   ├── TicketStatusConfiguration.cs
│   │   │   │   ├── CommentConfiguration.cs
│   │   │   │   ├── FeedbackConfiguration.cs
│   │   │   │   ├── AttachmentConfiguration.cs
│   │   │   │   ├── NotificationConfiguration.cs
│   │   │   │   └── SystemLogConfiguration.cs
│   │   │   └── Migrations\
│   │   ├── Seed\
│   │   │   └── DatabaseSeeder.cs
│   │   ├── Security\
│   │   │   ├── PasswordHasher.cs
│   │   │   └── CurrentUserService.cs
│   │   ├── Realtime\
│   │   │   └── SignalRNotificationDispatcher.cs
│   │   ├── DependencyInjection.cs
│   │   └── ItTms.Infrastructure.csproj
│   │
│   └── ItTms.Web\
│       ├── Areas\
│       │   ├── Auth\
│       │   │   ├── Controllers\ (AccountController.cs)
│       │   │   └── Views\
│       │   ├── User\
│       │   │   ├── Controllers\ (DashboardController.cs, TicketController.cs)
│       │   │   └── Views\
│       │   ├── Staff\
│       │   │   ├── Controllers\ (DashboardController.cs, WorkQueueController.cs)
│       │   │   └── Views\
│       │   ├── Leader\
│       │   │   ├── Controllers\ (DashboardController.cs, TicketManagementController.cs)
│       │   │   └── Views\
│       │   └── Admin\
│       │       ├── Controllers\ (DashboardController.cs, UserController.cs, SystemLogController.cs)
│       │       └── Views\
│       ├── Controllers\
│       │   └── HomeController.cs
│       ├── Filters\
│       │   ├── AuthorizeRolesAttribute.cs
│       │   └── AntiIdorFilter.cs
│       ├── Hubs\
│       │   └── TicketHub.cs
│       ├── ViewModels\
│       ├── Views\
│       │   ├── Shared\
│       │   │   ├── _Layout.cshtml
│       │   │   ├── _Navbar.cshtml
│       │   │   ├── _Sidebar.cshtml
│       │   │   └── _ValidationScriptsPartial.cshtml
│       │   ├── _ViewImports.cshtml
│       │   └── _ViewStart.cshtml
│       ├── wwwroot\
│       │   ├── css\ (app.css)
│       │   ├── js\ (ticketHub.js)
│       │   └── lib\ (bootstrap, signalr)
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs
│       └── ItTms.Web.csproj
│
└── tests\
    └── ItTms.Tests\
        ├── UnitTests\
        │   ├── States\ (StateTransitionTests.cs, ResultMonadTests.cs)
        │   ├── Domain\ (TicketInvariantTests.cs, DueAtSlaTests.cs)
        │   └── Observers\ (ObserverDispatchTests.cs)
        ├── IntegrationTests\
        │   ├── EfCoreConcurrency\ (OptimisticConcurrencyTests.cs)
        │   └── Security\ (AuthorizeRolesFilterTests.cs, InactiveAccountTests.cs)
        ├── SignalRTests\
        │   └── TicketHubRealtimeTests.cs
        ├── E2EVerificationTests\
        │   └── Table72VerificationTests.cs (TC01 đến TC10 đầy đủ)
        └── ItTms.Tests.csproj
```

---

## 7. MẪU ĐẶC TẢ CHI TIẾT 5 FILE `.csproj`

Dưới đây là nội dung hoàn chỉnh của từng file dự án khi đã kết nối tham chiếu và NuGet packages:

### 7.1. `src/ItTms.Domain/ItTms.Domain.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ItTms.Domain</RootNamespace>
  </PropertyGroup>

</Project>
```

### 7.2. `src/ItTms.Application/ItTms.Application.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ItTms.Application</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ItTms.Domain\ItTms.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.21" />
  </ItemGroup>

</Project>
```

### 7.3. `src/ItTms.Infrastructure/ItTms.Infrastructure.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ItTms.Infrastructure</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ItTms.Application\ItTms.Application.csproj" />
    <ProjectReference Include="..\ItTms.Domain\ItTms.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.21" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.21">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.21">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

### 7.4. `src/ItTms.Web/ItTms.Web.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>ItTms.Web</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ItTms.Application\ItTms.Application.csproj" />
    <ProjectReference Include="..\ItTms.Infrastructure\ItTms.Infrastructure.csproj" />
    <ProjectReference Include="..\ItTms.Domain\ItTms.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.21">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

### 7.5. `tests/ItTms.Tests/ItTms.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RootNamespace>ItTms.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ItTms.Domain\ItTms.Domain.csproj" />
    <ProjectReference Include="..\..\src\ItTms.Application\ItTms.Application.csproj" />
    <ProjectReference Include="..\..\src\ItTms.Infrastructure\ItTms.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\ItTms.Web\ItTms.Web.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="7.2.2" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.21" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="9.0.21" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.21" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

---

## 8. QUY TRÌNH KIỂM CHỨNG BUILD CƠ SỞ SAU KHI SCAFFOLD (VERIFICATION PROTOCOL)

Sau khi implementer thực hiện chuỗi lệnh CLI và nạp các tệp cấu hình trên, quy trình kiểm chứng độc lập bắt buộc phải thực thi 3 bước sau:

### Bước 1: Khôi phục và Biên dịch Toàn bộ Solution
```powershell
dotnet restore ItTms.sln
dotnet build ItTms.sln --no-incremental -v normal
```
*Điều kiện đạt*:
- Mã thoát (Exit Code): `0`.
- Output: `0 Warning(s)`, `0 Error(s)`.
- Không xuất hiện bất kỳ cảnh báo CS8618, CS1591, hoặc package downgrade nào.

### Bước 2: Chạy Thử nghiệm Test Suite
```powershell
dotnet test ItTms.sln --no-build --verbosity normal
```
*Điều kiện đạt*:
- Test runner nhận diện dự án `ItTms.Tests`.
- Toàn bộ test scenarios (hoặc baseline smoke test) thực thi thành công, 0 failed.

### Bước 3: Kiểm tra Khả năng Hỗ trợ EF Core CLI
```powershell
dotnet ef --version
```
*Điều kiện đạt*:
- Công cụ `dotnet ef` sẵn sàng hoạt động với target project `ItTms.Infrastructure` và startup project `ItTms.Web`.

---

## 9. KẾT LUẬN & CHỈ THỊ CHO WORKER TIẾP THEO

Bản thiết kế scaffolding này tuân thủ 100% các nguyên tắc bất biến trong `PROJECT_MEMORY.md` và `PROJECT.md`:
1. Giữ nguyên ranh giới Clean Architecture, Domain thuần khiết không vướng ORM.
2. SignalR WebSocket và EF Core 9 SQLite được tích hợp chuẩn xác ở tầng Infrastructure và Web.
3. Toàn bộ phiên bản NuGet được ghim chặt chẽ (`9.0.21`, `7.2.2`, `4.20.72`), triệt tiêu xung đột phụ thuộc chéo.
4. Worker thực hiện M1 Scaffold có thể áp dụng ngay lập tức chuỗi lệnh CLI tại Mục 3 và cấu hình tại Mục 5, 7 để hoàn tất nền móng kỹ thuật trong thời gian ngắn nhất.
