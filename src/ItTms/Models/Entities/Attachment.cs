

namespace ItTms.Models.Entities;

public class Attachment : EntityBase<int>
{
    public const long MaxFileSizeBytes = 16 * 1024 * 1024; // 16 MB

    public Attachment() { }

    public int TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;         // Tên lưu vật lý trên ổ cứng (GUID)
    public string OriginalFileName { get; set; } = string.Empty; // Tên gốc người dùng tải lên
    public string ContentType { get; set; } = string.Empty;      // MIME type (image/png, application/pdf...)
    public long FileSize { get; set; }                           // Dung lượng (bytes)
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual Ticket Ticket { get; set; } = null!;

    public static Result<Attachment> Create(
        int ticketId,
        string fileName,
        string originalFileName,
        string contentType,
        long fileSize)
    {
        if (ticketId <= 0)
            return Result<Attachment>.Failure(new Error("ATTACH_TICKET_INVALID", "Mã ticket không hợp lệ.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(fileName))
            return Result<Attachment>.Failure(new Error("ATTACH_FILENAME_REQUIRED", "Tên tệp lưu trữ không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result<Attachment>.Failure(new Error("ATTACH_ORIGINAL_REQUIRED", "Tên tệp gốc không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(contentType))
            return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_REQUIRED", "Kiểu nội dung tệp tin không được để trống.", ErrorType.Validation));

        if (contentType.Length > 100)
            return Result<Attachment>.Failure(new Error("ATTACH_CONTENT_TYPE_TOO_LONG", "Kiểu nội dung tệp tin không được vượt quá 100 ký tự.", ErrorType.Validation));

        if (fileSize <= 0)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_EMPTY", "Tệp tin không có dung lượng.", ErrorType.Validation));

        if (fileSize > MaxFileSizeBytes)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_TOO_LARGE", "Dung lượng tệp tin vượt quá giới hạn cho phép (16MB).", ErrorType.Validation));

        var attachment = new Attachment
        {
            TicketId = ticketId,
            FileName = fileName.Trim(),
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            FileSize = fileSize,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Attachment>.Success(attachment);
    }
}
