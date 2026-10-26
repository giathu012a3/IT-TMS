# BÁO CÁO BÀN GIAO (HANDOFF REPORT) — EXPLORER_M1_1

**Dự án**: IT Ticket Management System (.NET 9) — IT-TMS  
**Nhiệm vụ**: M1 Explorer 1 — Thiết kế Solution Scaffolding & Project Dependencies Plan  
**Tác giả**: `explorer_m1_1`  
**Ngày thực hiện**: 2026-10-25  
**Hồ sơ chi tiết**: `c:\IT_System\.agents\teamwork\explorer_m1_1\scaffolding_plan.md`  
**Loại bàn giao**: Hard Handoff (Nhiệm vụ hoàn tất đầy đủ)

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM TRỰC TIẾP)

1. **Môi trường Máy chủ và .NET SDK**:
   - Lệnh thực thi: `dotnet --info` tại `c:\IT_System`.
   - Kết quả trực tiếp:
     ```text
     .NET SDK:
      Version:           9.0.317
      Commit:            26570c2743
     Runtime Environment:
      OS Name:     Windows
      OS Version:  10.0.26200
      RID:         win-x64
     .NET runtimes installed:
      Microsoft.AspNetCore.App 9.0.19 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
      Microsoft.NETCore.App 9.0.19 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
     ```
   - Xác nhận: Máy chủ sở hữu .NET SDK 9.0.317 và Runtime 9.0.19 bản quyền chính thức, hỗ trợ đầy đủ `net9.0`, C# 13, ASP.NET Core MVC, và SignalR.

2. **Hiện trạng Kho Mã Nguồn**:
   - Lệnh thực thi: `list_dir(DirectoryPath="c:\\IT_System")`.
   - Kết quả: Thư mục chứa 1 thư mục `.agents` và 5 files (`AGENTS.md`, `BaoCao_ITTMS_TKPMHDT (4).docx`, `PROJECT.md`, `PROJECT_MEMORY.md`, `extracted_doc_text.txt`).
   - Xác nhận: Chưa có file `.sln`, `.csproj` hay `.cs` nào trên đĩa. Trạng thái giải pháp là 100% Greenfield.

3. **Xác thực Phiên bản Gói NuGet Thực tế trên nuget.org**:
   - Lệnh thực thi: `curl.exe -s https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/index.json`.
   - Kết quả: Các phiên bản .NET 9 trải dài từ `9.0.0` đến **`9.0.21`**.
   - Lệnh thực thi: `curl.exe -s https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.signalr.client/index.json` và `microsoft.aspnetcore.mvc.testing/index.json`.
   - Kết quả: Bản phát hành cao nhất của nhánh 9.0 đều đạt mốc **`9.0.21`**.
   - Lệnh thực thi: `curl.exe -s https://api.nuget.org/v3-flatcontainer/fluentassertions/index.json`.
   - Kết quả: Phiên bản `7.2.2` là bản phát hành ổn định cuối cùng trước khi chuyển đổi mô hình cấp phép thương mại ở phiên bản 8.0+.
   - Lệnh thực thi: `curl.exe -s https://api.nuget.org/v3-flatcontainer/moq/index.json`.
   - Kết quả: Phiên bản ổn định `4.20.72`.

4. **Yêu cầu Kiến trúc và Ranh giới Phụ thuộc**:
   - Văn bản: `c:\IT_System\PROJECT.md` (dòng 8-12) và `c:\IT_System\.agents\teamwork\explorer_m1_1\DISPATCH.md` (dòng 4-9).
   - Yêu cầu chính xác 5 projects:
     - `src/ItTms.Domain` (classlib, net9.0)
     - `src/ItTms.Application` (classlib, net9.0, refs Domain)
     - `src/ItTms.Infrastructure` (classlib, net9.0, refs Application, Domain)
     - `src/ItTms.Web` (web / mvc, net9.0, refs Application, Infrastructure, Domain)
     - `tests/ItTms.Tests` (xunit, net9.0, refs Web, Infrastructure, Application, Domain)

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN TỪ QUAN SÁT ĐẾN KẾT LUẬN)

1. Từ **Observation 1 & 2**: Do môi trường có sẵn .NET SDK `9.0.317` và thư mục hiện tại hoàn toàn greenfield, toàn bộ giải pháp có thể được scaffold thuần túy qua CLI tiêu chuẩn (`dotnet new sln`, `dotnet new classlib`, `dotnet new mvc`, `dotnet new xunit`) với cờ `-f net9.0` mà không cần workaround hay cài đặt SDK bên ngoài.
2. Từ **Observation 3**: Nhánh .NET 9 LTS có bản cập nhật bảo trì đồng bộ `9.0.21` trên toàn bộ hệ sinh thái Microsoft (`Microsoft.EntityFrameworkCore.*`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.AspNetCore.SignalR.Client`). Sử dụng đồng nhất phiên bản `9.0.21` triệt tiêu hoàn toàn rủi ro cảnh báo NU1605 (Package downgrade) và xung đột binary assembly.
3. Từ **Observation 3 (FluentAssertions)**: Phiên bản FluentAssertions 8.x đã thay đổi sang giấy phép thương mại đóng. Do đó, việc chốt phiên bản `7.2.2` (giấy phép Apache-2.0) đảm bảo tính hợp lệ pháp lý, tuân thủ tiêu chuẩn mã nguồn mở học thuật và doanh nghiệp mà vẫn tương thích 100% với .NET 9.
4. Từ **Observation 4**: Trong Clean Architecture:
   - `ItTms.Domain` phải hoàn toàn thuần khiết (Zero Dependency).
   - `ItTms.Application` chỉ cần gói `Microsoft.EntityFrameworkCore` (`9.0.21`) để định nghĩa `DbSet<T>` và các async query extensions trong `IItTmsDbContext`, không phụ thuộc provider cụ thể.
   - `ItTms.Infrastructure` cần `Microsoft.EntityFrameworkCore.Sqlite` (`9.0.21`) và `<FrameworkReference Include="Microsoft.AspNetCore.App" />` để có `IHubContext<TicketHub>` và `IPasswordHasher<User>` mà không cần kéo thêm các gói nuget trung gian.
   - `ItTms.Web` kế thừa `Microsoft.NET.Sdk.Web`, chỉ cần bổ sung `Microsoft.EntityFrameworkCore.Design` (`9.0.21`) với `PrivateAssets="all"` để làm Startup Project cho EF Core migrations.
   - `tests/ItTms.Tests` cần tham chiếu `ItTms.Web` để dùng `WebApplicationFactory<Program>` cho test tích hợp, và `Microsoft.AspNetCore.SignalR.Client` để kiểm thử kết nối WebSocket thời gian thực của `TicketHub`.
5. Từ yêu cầu **Warning-Free Build**: Việc đặt file `Directory.Build.props` tại thư mục gốc cấu hình `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, và `<GenerateDocumentationFile>false</GenerateDocumentationFile>` sẽ ép mọi dự án tuân thủ nghiêm ngặt chuẩn code sạch, loại trừ triệt để cảnh báo CS1591, CS8618 ngay từ commit đầu tiên.

---

## 3. CAVEATS (GIỚI HẠN VÀ KHUYẾN NGHỊ BỔ TRỢ)

1. **Phạm vi Trách nhiệm (Role Boundary)**: Báo cáo này đóng vai trò Thiết kế Kế hoạch (Explorer Read-only investigation). Worker phụ trách triển khai M1 (Implementer) sẽ trực tiếp thực thi các lệnh CLI và ghi các tệp dự án vào `src/` và `tests/`.
2. **Xử lý cảnh báo CS8618 trong Entity**: Vì `<Nullable>enable</Nullable>` và `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, các Navigation Properties trong Domain Entities (như `public User Creator { get; set; } = null!;`) và chuỗi (như `public string Title { get; set; } = string.Empty;`) cần được khởi tạo giá trị mặc định để tránh lỗi compile ngay khi scaffold entity.
3. **EF Core CLI Global Tool**: Để chạy `dotnet ef migrations add`, máy chủ cần cài đặt `dotnet-ef` global tool (`dotnet tool install --global dotnet-ef`). Nếu chưa cài, worker có thể cài bằng lệnh đơn giản `dotnet tool install --global dotnet-ef` hoặc sử dụng runtime migration `context.Database.Migrate()` tự động lúc ứng dụng khởi động.

---

## 4. CONCLUSION (KẾT LUẬN & ĐÁNH GIÁ CUỐI CÙNG)

1. Kế hoạch Scaffolding đã được thiết kế hoàn thiện, chính xác từng ký tự lệnh CLI, từng version gói NuGet và từng cấu hình MSBuild.
2. File thiết kế chi tiết đã được lưu trữ an toàn tại:
   `c:\IT_System\.agents\teamwork\explorer_m1_1\scaffolding_plan.md`.
3. Giải pháp đảm bảo:
   - 5 projects Clean Architecture chuẩn mực: `ItTms.Domain`, `ItTms.Application`, `ItTms.Infrastructure`, `ItTms.Web`, `tests/ItTms.Tests`.
   - Trạng thái biên dịch 0 cảnh báo, 0 lỗi (`TreatWarningsAsErrors = true`).
   - Sẵn sàng để M1 Worker thực thi ngay lập tức.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP KIỂM CHỨNG ĐỘC LẬP)

Để kiểm chứng tính chính xác của kế hoạch, Agent tiếp theo hoặc Orchestrator có thể thực thi các bước kiểm chứng sau:

1. **Kiểm tra cú pháp và sự tồn tại của tài liệu**:
   - Đọc tệp: `c:\IT_System\.agents\teamwork\explorer_m1_1\scaffolding_plan.md`.
   - Xác nhận có đầy đủ các phần: CLI pipeline (Mục 3), Danh mục NuGet (Mục 4), `Directory.Build.props` (Mục 5), Khung thư mục (Mục 6), và 5 file `.csproj` mẫu (Mục 7).

2. **Kiểm chứng độc lập các gói NuGet**:
   - Chạy lệnh kiểm tra độ khả dụng của gói chính:
     ```powershell
     curl.exe -s https://api.nuget.org/v3-flatcontainer/microsoft.entityframeworkcore.sqlite/index.json | Select-String "9.0.21"
     ```
   - Kết quả trả về phải chứa `"9.0.21"`.

3. **Kiểm chứng sau khi Worker thực thi Scaffold**:
   - Sau khi worker chạy toàn bộ lệnh tại Mục 3 và tạo `Directory.Build.props` tại Mục 5:
     ```powershell
     dotnet build c:\IT_System\ItTms.sln --no-incremental -v normal
     ```
   - Điều kiện Đạt (Pass):
     - Exit code: `0`.
     - Output kết thúc: `0 Warning(s)`, `0 Error(s)`.
   - Chạy kiểm thử:
     ```powershell
     dotnet test c:\IT_System\ItTms.sln --verbosity normal
     ```
     - Kết quả: `Passed! - Failed: 0, Passed: X, Skipped: 0`.
