# Agent Service

Lớp trừu tượng và resilient client cho LLM (Large Language Model) phục vụ toàn bộ các agent (AI BA, Developer, Reviewer) trong nền tảng.

## 1. Cấu trúc thư mục

```
services/agent-service/
├── AgentService.slnx                  # Solution file (.slnx)
├── README.md                          # Tài liệu hướng dẫn
├── .gitignore                         # Bỏ qua bin/, obj/, TestResults/
├── src/
│   └── AgentService.Llm/              # Class library lõi cho LLM
│       ├── ILlmClient.cs              # Hợp đồng CompleteAsync
│       ├── LlmRequest.cs              # DTO yêu cầu gọi LLM
│       ├── LlmResponse.cs             # DTO kết quả trả về từ LLM
│       ├── LlmErrorKind.cs            # Phân loại lỗi chuẩn hóa
│       ├── LlmException.cs            # Exception mang thông tin phân loại lỗi
│       ├── LlmOptions.cs              # Cấu hình model, timeout, backoff, credentials
│       ├── GeminiLlmClient.cs         # Bản cài đặt HTTP gọi trực tiếp Google Gemini API
│       ├── ResilientLlmClient.cs      # Lớp bọc timeout từng lần, exponential backoff và failover dự phòng
│       ├── FakeLlmClient.cs           # Test double phục vụ unit test cho consumer
│       └── ServiceCollectionExtensions.cs # Extension đăng ký DI cho IServiceCollection
└── tests/
    └── AgentService.Llm.Tests/        # Bộ kiểm thử xUnit
        ├── GeminiLlmClientTests.cs    # Test dịch mã lỗi, header, URL, mapping Gemini
        ├── ResilientLlmClientTests.cs # Test retry backoff, timeout, jitter, fallback failover
        ├── ServiceCollectionExtensionsTests.cs # Test DI configuration binding và FakeLlmClient
        └── TestHelpers/
            ├── MockHttpMessageHandler.cs # Mock HTTP handler không gọi mạng thật
            └── TestLogger.cs          # Mock logger ghi nhận log entries
```

## 2. Cách cấu hình

Cấu hình được quản lý qua `LlmOptions` và đăng ký thông qua `services.AddLlmClient(configuration)`:

### Cấu hình qua `appsettings.json` / `IConfiguration`:

```json
{
  "Llm": {
    "PrimaryModel": "gemini-3.8-flash",
    "FallbackModel": "gemini-3.5-flash-lite",
    "PerAttemptTimeout": "00:01:00",
    "MaxAttemptsPerModel": 3,
    "BaseDelay": "00:00:01",
    "MaxDelay": "00:00:20"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "BaseUrl": "https://generativelanguage.googleapis.com"
  }
}
```

### Bảo mật API Key:
API key **không** bao giờ được commit vào repo. Đặt key thông qua một trong hai cách:
1. `dotnet user-secrets`:
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "<your-key>"
   ```
2. Biến môi trường:
   ```bash
   export Gemini__ApiKey="<your-key>"          # Linux/macOS
   $env:Gemini__ApiKey="<your-key>"            # PowerShell
   ```

Key được gửi an toàn bằng header HTTP `x-goog-api-key`, **không** nằm trên URL query string, không bị ghi lại trong exception hay log.

## 3. Cách chạy kiểm thử (Test)

Toàn bộ test sử dụng mock in-memory, kiểm thử backoff tức thì qua delegate tiêm được, không gọi mạng thật và không yêu cầu API key:

```bash
cd services/agent-service
dotnet build AgentService.slnx -c Release
dotnet test AgentService.slnx -c Release
```

## Việc còn nợ sau review `s2-2` (2026-11-22)

Sửa khi tới việc cần chúng; điểm 1 và 2 bắt buộc sửa ở `s2-3` và `s2-4`.

| # | Việc | Sửa ở |
|---|---|---|
| 1 | Thêm `FinishReason` vào `LlmResponse`; `MAX_TOKENS` (đầu ra bị cắt) hiện vẫn trả như thành công, dễ làm JSON hỏng | `s2-3` |
| 2 | Đọc thêm số token suy nghĩ trong `usageMetadata` (token suy nghĩ tính vào đầu ra theo bảng giá Google; tên trường cần xác nhận trên tài liệu), nếu không `s2-4` ước chi phí thấp hơn thực tế | `s2-4` |
| 3 | Bọc lỗi khi đọc body giữa chừng (`IOException`) thành `Transient` trong `GeminiLlmClient` | khi gặp |
| 4 | Thiếu `Gemini:ApiKey` thì báo ngay lỗi cấu hình (`Unauthorized`, không retry) thay vì gửi header rỗng | khi gặp |
| 5 | Nhận biết 429 do hết hạn mức theo ngày (gói Free: 20 request/ngày cho model mạnh) và nhảy thẳng sang model dự phòng, không retry | khi gặp 429 thật |

Chưa làm: chạy thử một lần gọi Gemini thật (key trong `dotnet user-secrets`) để xác nhận tên trường phản hồi; ADR "build hay mua".
