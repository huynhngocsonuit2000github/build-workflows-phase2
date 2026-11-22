using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentService.Llm;

/// <summary>
/// Dependency injection registration extensions for LLM clients.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the LLM client infrastructure with configuration from IConfiguration.
    /// Reads settings from "Llm" section and Gemini credentials from "Gemini:ApiKey" and "Gemini:BaseUrl".
    /// </summary>
    public static IServiceCollection AddLlmClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<LlmOptions>(options =>
        {
            configuration.GetSection(LlmOptions.SectionName).Bind(options);

            var geminiApiKey = configuration["Gemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(geminiApiKey))
            {
                options.ApiKey = geminiApiKey;
            }

            var geminiBaseUrl = configuration["Gemini:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(geminiBaseUrl))
            {
                options.BaseUrl = geminiBaseUrl;
            }
        });

        return services.AddLlmClientCore();
    }

    /// <summary>
    /// Registers the LLM client infrastructure with custom options configuration.
    /// </summary>
    public static IServiceCollection AddLlmClient(
        this IServiceCollection services,
        Action<LlmOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddLlmClientCore();
    }

    private static IServiceCollection AddLlmClientCore(this IServiceCollection services)
    {
        // Add typed HttpClient for Gemini without default factory retries to prevent nested retry policies
        services.AddHttpClient<GeminiLlmClient>();

        services.AddTransient<ILlmClient>(sp =>
        {
            var geminiClient = sp.GetRequiredService<GeminiLlmClient>();
            var options = sp.GetRequiredService<IOptions<LlmOptions>>();
            var logger = sp.GetService<ILogger<ResilientLlmClient>>();
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;

            return new ResilientLlmClient(geminiClient, options, logger, timeProvider);
        });

        return services;
    }
}
