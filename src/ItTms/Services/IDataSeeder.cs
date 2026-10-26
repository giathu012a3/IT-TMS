namespace ItTms.Services;

public interface IDataSeeder
{
    /// <summary>
    /// Thực thi nạp dữ liệu khởi tạo mặc định cho hệ thống nếu chưa tồn tại.
    /// Phương thức phải đảm bảo tính Idempotent (chạy nhiều lần không gây lỗi).
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
