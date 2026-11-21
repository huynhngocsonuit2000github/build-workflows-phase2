namespace WorkflowApi.Api.Data;

/// <summary>
/// Kết quả đã lưu của một lần gọi POST /executions, khóa theo Idempotency-Key.
/// Key là khóa chính (unique): đây là tuyến phòng thủ cuối khi hai request cùng key đến cùng lúc.
/// </summary>
public class IdempotencyRecord
{
    public string Key { get; set; } = "";

    /// <summary>SHA-256 của nội dung request đã chuẩn hóa, để phát hiện cùng key nhưng khác nội dung.</summary>
    public string RequestHash { get; set; } = "";

    public Guid ExecutionId { get; set; }
    public int StatusCode { get; set; }

    /// <summary>Đúng chuỗi JSON đã trả lần đầu; lần gọi lặp trả lại nguyên văn.</summary>
    public string ResponseBody { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
