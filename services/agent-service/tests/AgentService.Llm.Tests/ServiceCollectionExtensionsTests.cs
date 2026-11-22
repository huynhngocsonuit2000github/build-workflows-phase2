using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentService.Llm.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLlmClient_WithConfiguration_BindsOptionsAndRegistersServices()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Llm:PrimaryModel"] = "my-custom-model",
            ["Llm:FallbackModel"] = "my-fallback-model",
            ["Llm:MaxAttemptsPerModel"] = "5",
            ["Gemini:ApiKey"] = "gemini-api-key-test",
            ["Gemini:BaseUrl"] = "https://custom.gemini.api"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLlmClient(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
        var llmClient = serviceProvider.GetRequiredService<ILlmClient>();
        var geminiClient = serviceProvider.GetRequiredService<GeminiLlmClient>();

        // Assert
        Assert.Equal("my-custom-model", options.PrimaryModel);
        Assert.Equal("my-fallback-model", options.FallbackModel);
        Assert.Equal(5, options.MaxAttemptsPerModel);
        Assert.Equal("gemini-api-key-test", options.ApiKey);
        Assert.Equal("https://custom.gemini.api", options.BaseUrl);

        Assert.NotNull(llmClient);
        Assert.IsType<ResilientLlmClient>(llmClient);
        Assert.NotNull(geminiClient);
    }

    [Fact]
    public async Task FakeLlmClient_ReturnsEnqueuedResponsesAndRecordsRequests()
    {
        // Arrange
        var fakeClient = new FakeLlmClient();
        fakeClient.EnqueueResponse("First response", model: "fake-1", inputTokens: 5, outputTokens: 10);
        fakeClient.EnqueueResponse("Second response", model: "fake-2", inputTokens: 8, outputTokens: 16);

        var req1 = new LlmRequest("Sys1", "Prompt 1");
        var req2 = new LlmRequest("Sys2", "Prompt 2");

        // Act
        var resp1 = await fakeClient.CompleteAsync(req1);
        var resp2 = await fakeClient.CompleteAsync(req2);

        // Assert
        Assert.Equal("First response", resp1.Text);
        Assert.Equal("fake-1", resp1.ModelUsed);
        Assert.Equal(5, resp1.InputTokens);
        Assert.Equal(10, resp1.OutputTokens);

        Assert.Equal("Second response", resp2.Text);
        Assert.Equal("fake-2", resp2.ModelUsed);
        Assert.Equal(8, resp2.InputTokens);
        Assert.Equal(16, resp2.OutputTokens);

        Assert.Equal(2, fakeClient.ReceivedRequests.Count);
        Assert.Equal("Prompt 1", fakeClient.ReceivedRequests[0].UserPrompt);
        Assert.Equal("Prompt 2", fakeClient.ReceivedRequests[1].UserPrompt);
    }
}
