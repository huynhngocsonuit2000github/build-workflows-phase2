using AgentService.Ba;
using AgentService.Llm;
using Microsoft.Extensions.Options;

namespace AgentService.Ba.Tests;

public class RequirementAnalyzerTests
{
    private const string ValidJson = """
    {
      "summary": "Tóm tắt yêu cầu phân trang",
      "userStory": "Là một người dùng, tôi muốn xem danh sách theo trang để giao diện tải nhanh hơn.",
      "acceptanceCriteria": [
        "API nhận page và pageSize.",
        "Trả về items, totalCount, page, pageSize, totalPages."
      ],
      "openQuestions": [
        "Có cần giới hạn pageSize tối đa 100 không?"
      ],
      "assumptions": [
        "Mặc định page=1, pageSize=20."
      ]
    }
    """;

    private readonly TicketInput _defaultTicket = new(
        Key: "TAS-1",
        Title: "Phân trang API",
        Description: "Cần thêm phân trang cho endpoint tasks.");

    [Fact]
    public async Task AnalyzeAsync_ValidOnFirstAttempt_ReturnsValidWithOneAttemptAndFullAnalysis()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse(ValidJson, model: "gemini-3.5-flash-lite", inputTokens: 50, outputTokens: 80);

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.NotNull(result.Analysis);
        Assert.Equal("Tóm tắt yêu cầu phân trang", result.Analysis.Summary);
        Assert.Equal(2, result.Analysis.AcceptanceCriteria.Count);
        Assert.Single(result.Analysis.OpenQuestions);
        Assert.Single(result.Analysis.Assumptions);
        Assert.Equal(1, result.GenerationAttempts);
        Assert.Single(result.ModelsUsed);
        Assert.Equal("gemini-3.5-flash-lite", result.ModelsUsed[0]);
        Assert.Equal(50, result.TotalInputTokens);
        Assert.Equal(80, result.TotalOutputTokens);
        Assert.Equal(1, result.PromptVersion);
        Assert.Equal("requirement-analysis", result.PromptName);
        Assert.Equal("requirement-analysis.v1", result.SchemaVersion);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task AnalyzeAsync_FirstAttemptNotJson_SecondAttemptValid_ReturnsValidWithTwoAttemptsAndFeedbackInPrompt()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse("Sorry, I cannot help with that as plain text.", inputTokens: 40, outputTokens: 20);
        fakeClient.EnqueueResponse(ValidJson, inputTokens: 60, outputTokens: 80);

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.NotNull(result.Analysis);
        Assert.Equal(2, result.GenerationAttempts);
        Assert.Equal(2, fakeClient.ReceivedRequests.Count);

        // Second request user prompt must contain feedback on the failure
        var secondRequestUserPrompt = fakeClient.ReceivedRequests[1].UserPrompt;
        Assert.Contains("LƯU Ý: Lần sinh trước không hợp lệ", secondRequestUserPrompt);
        Assert.Contains("JSON parse error", secondRequestUserPrompt);

        Assert.Equal(100, result.TotalInputTokens);
        Assert.Equal(100, result.TotalOutputTokens);
        Assert.Single(result.Failures); // Contains failure reason from attempt 1
    }

    [Theory]
    [InlineData("""{"summary":"S","userStory":"U","acceptanceCriteria":["AC"],"assumptions":[]}""", "openQuestions")] // Missing required
    [InlineData("""{"summary":"S","userStory":"U","acceptanceCriteria":["AC"],"openQuestions":[],"assumptions":[],"extra":1}""", "extra")] // Extra field
    [InlineData("""{"summary":"S","userStory":"U","acceptanceCriteria":"not-array","openQuestions":[],"assumptions":[]}""", "acceptanceCriteria")] // Wrong type
    [InlineData("""{"summary":"S","userStory":"U","acceptanceCriteria":[],"openQuestions":[],"assumptions":[]}""", "acceptanceCriteria")] // Empty AC
    [InlineData("""{"summary":"   ","userStory":"U","acceptanceCriteria":["AC"],"openQuestions":[],"assumptions":[]}""", "summary")] // Whitespace summary
    [InlineData("""{"summary":"S","userStory":"U","acceptanceCriteria":["A","A"],"openQuestions":[],"assumptions":[]}""", "duplicate")] // Duplicate AC
    public async Task AnalyzeAsync_SchemaAndDomainViolations_RejectedAndRetried(string invalidJson, string expectedKeyword)
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse(invalidJson);
        fakeClient.EnqueueResponse(ValidJson);

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.Equal(2, result.GenerationAttempts);
        Assert.Contains(result.Failures, f => f.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeAsync_StringTooLong_RejectedAndRetried()
    {
        // Arrange
        var longSummary = new string('S', 601);
        var invalidJson = $$"""{"summary":"{{longSummary}}","userStory":"U","acceptanceCriteria":["AC"],"openQuestions":[],"assumptions":[]}""";

        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse(invalidJson);
        fakeClient.EnqueueResponse(ValidJson);

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.Equal(2, result.GenerationAttempts);
        Assert.NotEmpty(result.Failures);
    }

    [Fact]
    public async Task AnalyzeAsync_WrappedInMarkdownFence_StrippedAndSucceeds()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse($"```json\n{ValidJson}\n```");

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.Equal(1, result.GenerationAttempts);
        Assert.NotNull(result.Analysis);
    }

    [Fact]
    public async Task AnalyzeAsync_FinishReasonMaxTokens_RejectedAsTruncatedAndRetried()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        // Even if text looks valid, FinishReason MAX_TOKENS must be rejected as truncated
        fakeClient.EnqueueResponse(ValidJson, finishReason: "MAX_TOKENS");
        fakeClient.EnqueueResponse(ValidJson, finishReason: "STOP");

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.Equal(2, result.GenerationAttempts);
        Assert.Single(result.Failures);
        Assert.Contains("MAX_TOKENS", result.Failures[0]);
    }

    [Fact]
    public async Task AnalyzeAsync_AlwaysInvalid_StopsAtExactMaxGenerationAttempts_ReturnsNeedsHuman()
    {
        // Arrange: default max attempts is 3
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse("{ not valid json 1 }");
        fakeClient.EnqueueResponse("{ not valid json 2 }");
        fakeClient.EnqueueResponse("{ not valid json 3 }");
        fakeClient.EnqueueResponse(ValidJson); // 4th attempt should NEVER be called!

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.NeedsHuman, result.Status);
        Assert.Null(result.Analysis);
        Assert.Equal(3, result.GenerationAttempts);
        Assert.Equal(3, fakeClient.ReceivedRequests.Count); // Verified fake called exactly 3 times
        Assert.Equal(3, result.Failures.Count); // Has exactly 3 failure reasons
    }

    [Fact]
    public async Task AnalyzeAsync_NonRetryableLlmException_ThrowsWithoutRetrying()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueException(new LlmException(
            "API key unauthorized",
            LlmErrorKind.Unauthorized,
            httpStatus: 401,
            retryable: false,
            model: "gemini-3.5-flash-lite",
            attempts: 1));

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LlmException>(() => analyzer.AnalyzeAsync(_defaultTicket));
        Assert.Equal(LlmErrorKind.Unauthorized, ex.Kind);
        Assert.Single(fakeClient.ReceivedRequests); // Fake called ONLY once
    }

    [Fact]
    public async Task AnalyzeAsync_ProviderBlockedOrEmptyOutput_ReturnsNeedsHumanWithoutThrowingOrRegenerating()
    {
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueException(new LlmException(
            "Gemini candidate was blocked by safety filters.",
            LlmErrorKind.EmptyOrBlocked,
            httpStatus: 200,
            retryable: false,
            model: "gemini-3.5-flash-lite"));

        var analyzer = new RequirementAnalyzer(fakeClient);

        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        Assert.Equal(AnalysisStatus.NeedsHuman, result.Status);
        Assert.Null(result.Analysis);
        Assert.Single(fakeClient.ReceivedRequests); // không sinh lại
        Assert.Contains(result.Failures, f => f.Contains("EmptyOrBlocked"));
        Assert.Equal("gemini-3.5-flash-lite", Assert.Single(result.ModelsUsed));
        Assert.Equal(1, result.PromptVersion);
    }

    [Theory]
    [InlineData(LlmErrorKind.Validation)]
    [InlineData(LlmErrorKind.Unauthorized)]
    [InlineData(LlmErrorKind.Permanent)]
    public async Task AnalyzeAsync_InfrastructureErrors_StillThrow(LlmErrorKind kind)
    {
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueException(new LlmException("boom", kind, retryable: false, model: "m"));
        var analyzer = new RequirementAnalyzer(fakeClient);

        var ex = await Assert.ThrowsAsync<LlmException>(() => analyzer.AnalyzeAsync(_defaultTicket));

        Assert.Equal(kind, ex.Kind);
        Assert.Single(fakeClient.ReceivedRequests);
    }

    [Fact]
    public async Task AnalyzeAsync_CallerCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var fakeClient = new FakeLlmClient();
        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => analyzer.AnalyzeAsync(_defaultTicket, cts.Token));
        Assert.Empty(fakeClient.ReceivedRequests);
    }

    [Fact]
    public async Task AnalyzeAsync_RecordsFallbackModelsAndTokensCorrectly()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        // First attempt fails validation with primary model
        fakeClient.EnqueueResponse("{ broken }", model: "gemini-3.8-flash", inputTokens: 40, outputTokens: 10);
        // Second attempt succeeds with fallback model
        fakeClient.EnqueueResponse(ValidJson, model: "gemini-3.5-flash-lite", inputTokens: 50, outputTokens: 90);

        var analyzer = new RequirementAnalyzer(fakeClient);

        // Act
        var result = await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Equal(AnalysisStatus.Valid, result.Status);
        Assert.Equal(2, result.ModelsUsed.Count);
        Assert.Equal("gemini-3.8-flash", result.ModelsUsed[0]);
        Assert.Equal("gemini-3.5-flash-lite", result.ModelsUsed[1]);
        Assert.Equal(90, result.TotalInputTokens);
        Assert.Equal(100, result.TotalOutputTokens);
        Assert.Equal(1, result.PromptVersion);
    }

    [Fact]
    public async Task AnalyzeAsync_TemperatureAndJsonOutputPassedDown()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse(ValidJson);

        var options = new RequirementAnalyzerOptions
        {
            Temperature = 0.2,
            MaxOutputTokens = 2048
        };
        var analyzer = new RequirementAnalyzer(fakeClient, Options.Create(options));

        // Act
        await analyzer.AnalyzeAsync(_defaultTicket);

        // Assert
        Assert.Single(fakeClient.ReceivedRequests);
        var request = fakeClient.ReceivedRequests[0];
        Assert.Equal(0.2, request.Temperature);
        Assert.Equal(2048, request.MaxOutputTokens);
        Assert.True(request.JsonOutput);
    }
}
