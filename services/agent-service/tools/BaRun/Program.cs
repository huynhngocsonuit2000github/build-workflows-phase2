using System.Text;
using System.Text.Json;
using AgentService.Ba;
using AgentService.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentService.Tools.BaRun;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // 1. Parse command line arguments
        string model = "gemini-3.5-flash-lite";
        string? casesDirArg = null;
        string? outDirArg = null;
        bool fallbackEnabled = false; // Default: no fallback

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--model", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                model = args[++i];
            }
            else if (arg.Equals("--cases", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                casesDirArg = args[++i];
            }
            else if (arg.Equals("--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                outDirArg = args[++i];
            }
            else if (arg.Equals("--no-fallback", StringComparison.OrdinalIgnoreCase))
            {
                fallbackEnabled = false;
            }
            else if (arg.Equals("--fallback", StringComparison.OrdinalIgnoreCase))
            {
                fallbackEnabled = true;
            }
        }

        // 2. Locate repo root and resolve paths
        string repoRoot;
        try
        {
            repoRoot = RepoPathResolver.FindRepoRoot();
        }
        catch
        {
            repoRoot = Directory.GetCurrentDirectory();
        }

        var casesDir = !string.IsNullOrWhiteSpace(casesDirArg)
            ? (Path.IsPathRooted(casesDirArg) ? casesDirArg : Path.Combine(repoRoot, casesDirArg))
            : Path.Combine(repoRoot, "tests", "evaluation", "ba", "cases");

        var outDir = !string.IsNullOrWhiteSpace(outDirArg)
            ? (Path.IsPathRooted(outDirArg) ? outDirArg : Path.Combine(repoRoot, outDirArg))
            : Path.Combine(repoRoot, "tests", "evaluation", "ba", "results");

        // 3. Load configuration (UserSecretsId is 89223f71-f534-45e6-8335-ffb6f6e9cbdf)
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]))
        {
            Console.Error.WriteLine(
                "Chưa có key. Đặt bằng: dotnet user-secrets set \"Gemini:ApiKey\" \"<key>\"  (hoặc biến môi trường Gemini__ApiKey).");
            return 2;
        }

        if (!Directory.Exists(casesDir))
        {
            Console.Error.WriteLine($"Thư mục ca kiểm thử không tồn tại: {casesDir}");
            return 2;
        }

        // 4. Load evaluation cases
        var caseFiles = Directory.GetFiles(casesDir, "*.md").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        if (caseFiles.Count == 0)
        {
            Console.Error.WriteLine($"Không tìm thấy file ca kiểm thử nào trong: {casesDir}");
            return 2;
        }

        var evaluationCases = new List<EvaluationCase>();
        foreach (var file in caseFiles)
        {
            var testCase = ParseCaseFile(file);
            if (testCase != null)
            {
                evaluationCases.Add(testCase);
            }
        }

        Console.WriteLine($"Đã tìm thấy {evaluationCases.Count} ca kiểm thử.");
        Console.WriteLine($"Model: {model} | Dự phòng: {(fallbackEnabled ? "Bật" : "Tắt")}");

        // 5. Configure DI services
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));

        // Configure LLM client
        services.AddLlmClient(configuration);
        services.Configure<LlmOptions>(opts =>
        {
            opts.PrimaryModel = model;
            if (!fallbackEnabled)
            {
                opts.FallbackModel = null;
            }
        });

        // Configure RequirementAnalyzer
        services.AddRequirementAnalyzer(configuration, opts =>
        {
            opts.ModelOverride = model;
            opts.Temperature = 0.2;
            opts.MaxGenerationAttempts = 3;
            opts.MaxOutputTokens = 2048;
        });

        await using var provider = services.BuildServiceProvider();
        var analyzer = provider.GetRequiredService<IRequirementAnalyzer>();

        // Thông tin prompt dùng để ghi nhận cả những ca bị lỗi LLM (analyzer không trả kết quả khi ném lỗi).
        var promptInfo = PromptTemplate.FromFile(RepoPathResolver.ResolvePromptPath());

        // 6. Execute cases sequentially
        var caseResults = new List<(EvaluationCase Case, AnalysisResult Result)>();
        for (int i = 0; i < evaluationCases.Count; i++)
        {
            var ec = evaluationCases[i];
            Console.WriteLine($"\n[{i + 1}/{evaluationCases.Count}] Đang chạy ca {ec.Id} ({ec.JiraKey}): \"{ec.Title}\"...");

            var ticket = new TicketInput(ec.JiraKey, ec.Title, ec.Description);
            AnalysisResult result;
            try
            {
                result = await analyzer.AnalyzeAsync(ticket);
            }
            catch (LlmException ex)
            {
                // Lỗi hạ tầng/nhà cung cấp (ví dụ 429 sau khi hết lượt retry): ghi vào kết quả rồi chạy tiếp ca khác.
                result = new AnalysisResult(
                    Status: AnalysisStatus.NeedsHuman,
                    Analysis: null,
                    PromptName: promptInfo.Name,
                    PromptVersion: promptInfo.Version,
                    SchemaVersion: "requirement-analysis.v1",
                    ModelsUsed: ex.Model is null ? [] : [ex.Model],
                    GenerationAttempts: 0,
                    TotalInputTokens: 0,
                    TotalOutputTokens: 0,
                    TotalLatency: TimeSpan.Zero,
                    Failures: [$"Lỗi LLM {ex.Kind} (HTTP {ex.HttpStatus}, {ex.Attempts} lần thử, model {ex.Model}): {ex.Message}"]);
            }

            caseResults.Add((ec, result));

            Console.WriteLine($"  -> Trạng thái: {result.Status} | Lần sinh: {result.GenerationAttempts} | Tokens: {result.TotalInputTokens} in / {result.TotalOutputTokens} out | Độ trễ: {result.TotalLatency.TotalMilliseconds:F0} ms");
            if (result.Failures.Count > 0)
            {
                foreach (var fail in result.Failures)
                {
                    Console.WriteLine($"     Lỗi ghi nhận: {fail}");
                }
            }

            // Rate-limiting delay between cases to not exceed 15 RPM (except after the last case)
            if (i < evaluationCases.Count - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(4.5));
            }
        }

        // 7. Write result file
        Directory.CreateDirectory(outDir);

        var firstResult = caseResults[0].Result;
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var resultFileName = $"{today}-{firstResult.PromptName}.v{firstResult.PromptVersion}-{model}.md";
        var resultFilePath = Path.Combine(outDir, resultFileName);

        // Chạy lại trong cùng ngày không ghi đè kết quả cũ: thêm hậu tố -2, -3...
        for (var n = 2; File.Exists(resultFilePath); n++)
        {
            resultFilePath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(resultFileName)}-{n}.md");
        }

        var validCount = caseResults.Count(c => c.Result.Status == AnalysisStatus.Valid);

        var sb = new StringBuilder();
        sb.AppendLine($"# Kết quả chạy AI BA: {firstResult.PromptName} (v{firstResult.PromptVersion})");
        sb.AppendLine();
        sb.AppendLine($"- **Thời gian chạy**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"- **Prompt**: {firstResult.PromptName} (v{firstResult.PromptVersion})");
        sb.AppendLine($"- **Schema**: {firstResult.SchemaVersion}");
        sb.AppendLine($"- **Model chính**: `{model}` (Dự phòng: {(fallbackEnabled ? "Bật" : "Tắt")})");
        sb.AppendLine("- **Tùy chọn**: Temperature = 0.2, MaxGenerationAttempts = 3, MaxOutputTokens = 2048");
        sb.AppendLine($"- **Tổng số ca**: {caseResults.Count}");
        sb.AppendLine($"- **Số ca hợp lệ (Valid)**: {validCount} / {caseResults.Count}");
        sb.AppendLine();

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // khớp schema: summary, userStory, ...
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        foreach (var (ec, res) in caseResults)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"## Ca: {ec.Id} (Mã: {ec.JiraKey})");
            sb.AppendLine();
            sb.AppendLine($"- **Tiêu đề ticket**: {ec.Title}");
            sb.AppendLine($"- **Trạng thái**: `{res.Status}`");
            sb.AppendLine($"- **Số lần sinh**: {res.GenerationAttempts}");
            sb.AppendLine($"- **Model đã dùng**: {string.Join(", ", res.ModelsUsed)}");
            sb.AppendLine($"- **Token vào / ra**: {res.TotalInputTokens} / {res.TotalOutputTokens}");
            sb.AppendLine($"- **Độ trễ**: {res.TotalLatency.TotalMilliseconds:F0} ms");

            if (res.Failures.Count > 0)
            {
                sb.AppendLine("- **Các lỗi ghi nhận trong quá trình sinh**:");
                foreach (var f in res.Failures)
                {
                    sb.AppendLine($"  - {f}");
                }
            }
            else
            {
                sb.AppendLine("- **Lỗi các lần sinh**: Không có");
            }

            if (res.Status == AnalysisStatus.Valid && res.Analysis != null)
            {
                sb.AppendLine();
                sb.AppendLine("### Đầu ra JSON đã định dạng");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(JsonSerializer.Serialize(res.Analysis, jsonSerializerOptions));
                sb.AppendLine("```");
            }

            sb.AppendLine();
        }

        await File.WriteAllTextAsync(resultFilePath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"\nĐã ghi kết quả vào: {resultFilePath}");

        // 8. Determine exit code
        if (validCount == caseResults.Count)
        {
            Console.WriteLine("Tất cả các ca đều hợp lệ (Valid). Thoát mã 0.");
            return 0;
        }

        Console.WriteLine($"Có {caseResults.Count - validCount} ca NeedsHuman. Thoát mã 1.");
        return 1;
    }

    private static EvaluationCase? ParseCaseFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        if (lines.Length < 3 || lines[0].Trim() != "---")
        {
            return null;
        }

        int closingIndex = -1;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == "---")
            {
                closingIndex = i;
                break;
            }
        }

        if (closingIndex == -1) return null;

        string id = Path.GetFileNameWithoutExtension(filePath);
        string jiraKey = id;
        string title = string.Empty;

        for (int i = 1; i < closingIndex; i++)
        {
            var line = lines[i].Trim();
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                var key = line[..colonIdx].Trim();
                var val = line[(colonIdx + 1)..].Trim();
                if (key.Equals("id", StringComparison.OrdinalIgnoreCase)) id = val;
                else if (key.Equals("jiraKey", StringComparison.OrdinalIgnoreCase)) jiraKey = val;
                else if (key.Equals("title", StringComparison.OrdinalIgnoreCase)) title = val;
            }
        }

        var description = string.Join("\n", lines.Skip(closingIndex + 1)).Trim();
        return new EvaluationCase(id, jiraKey, title, description);
    }

    private sealed record EvaluationCase(string Id, string JiraKey, string Title, string Description);
}
