using AgentService.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Gọi Gemini thật đúng một lần để kiểm tra lớp LLM. Chỉ gửi một câu vô hại, không dữ liệu thật.
// Key đọc từ user-secrets (Gemini:ApiKey) hoặc biến môi trường Gemini__ApiKey. Không bao giờ in key.
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

if (string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]))
{
    Console.Error.WriteLine("Chưa có key. Đặt bằng: dotnet user-secrets set \"Gemini:ApiKey\" \"<key>\"  (hoặc biến môi trường Gemini__ApiKey).");
    return 2;
}

var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
services.AddLlmClient(configuration);
await using var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<ILlmClient>();
var model = args.Length > 0 ? args[0] : null;

try
{
    var response = await client.CompleteAsync(new LlmRequest(
        SystemPrompt: "Trả lời bằng tiếng Việt, đúng một câu ngắn.",
        UserPrompt: "Hãy chào tôi và nói bạn là model nào.",
        ModelOverride: model,
        MaxOutputTokens: 200));

    Console.WriteLine($"Model dùng      : {response.ModelUsed} (dự phòng: {response.UsedFallback})");
    Console.WriteLine($"Số lần thử      : {response.Attempts}");
    Console.WriteLine($"Token vào / ra  : {response.InputTokens} / {response.OutputTokens}");
    Console.WriteLine($"Độ trễ          : {response.Latency.TotalMilliseconds:F0} ms");
    Console.WriteLine($"Trả lời         : {response.Text}");
    return 0;
}
catch (LlmException ex)
{
    Console.Error.WriteLine($"LỖI {ex.Kind} (HTTP {ex.HttpStatus}, thử {ex.Attempts} lần, model {ex.Model}): {ex.Message}");
    return 1;
}
