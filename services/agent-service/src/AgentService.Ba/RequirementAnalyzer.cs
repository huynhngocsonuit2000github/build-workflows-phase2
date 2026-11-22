using System.Diagnostics;
using System.Text;
using AgentService.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentService.Ba;

/// <summary>
/// Pipeline implementing AI Business Analyst requirement analysis.
/// Handles generation, schema validation, bounded retries, and escalation to human review.
/// </summary>
public sealed class RequirementAnalyzer : IRequirementAnalyzer
{
    private readonly ILlmClient _llmClient;
    private readonly RequirementAnalyzerOptions _options;
    private readonly PromptTemplate _promptTemplate;
    private readonly SchemaValidator _schemaValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RequirementAnalyzer>? _logger;

    public RequirementAnalyzer(
        ILlmClient llmClient,
        IOptions<RequirementAnalyzerOptions>? options = null,
        PromptTemplate? promptTemplate = null,
        SchemaValidator? schemaValidator = null,
        TimeProvider? timeProvider = null,
        ILogger<RequirementAnalyzer>? logger = null)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _options = options?.Value ?? new RequirementAnalyzerOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;

        if (promptTemplate != null)
        {
            _promptTemplate = promptTemplate;
        }
        else
        {
            var promptPath = RepoPathResolver.ResolvePromptPath(_options.PromptPath);
            _promptTemplate = PromptTemplate.FromFile(promptPath);
        }

        if (schemaValidator != null)
        {
            _schemaValidator = schemaValidator;
        }
        else
        {
            var schemaPath = RepoPathResolver.ResolveSchemaPath(_options.SchemaPath);
            _schemaValidator = SchemaValidator.FromFile(schemaPath);
        }
    }

    public async Task<AnalysisResult> AnalyzeAsync(TicketInput ticket, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        ct.ThrowIfCancellationRequested();

        var maxAttempts = Math.Max(1, _options.MaxGenerationAttempts);
        var baseUserPrompt = _promptTemplate.RenderUserPrompt(ticket);

        var modelsUsed = new List<string>();
        int totalInputTokens = 0;
        int totalOutputTokens = 0;
        var allFailures = new List<string>();
        List<string> previousAttemptErrors = [];

        var startTimestamp = _timeProvider.GetTimestamp();

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            // Construct user prompt (with retry feedback if attempt > 1)
            string currentUserPrompt;
            if (attempt == 1 || previousAttemptErrors.Count == 0)
            {
                currentUserPrompt = baseUserPrompt;
            }
            else
            {
                var sb = new StringBuilder(baseUserPrompt);
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendLine("LƯU Ý: Lần sinh trước không hợp lệ do các lỗi sau:");
                foreach (var err in previousAttemptErrors)
                {
                    sb.AppendLine($"- {err}");
                }
                sb.AppendLine("Hãy sửa tất cả các lỗi trên và chỉ trả về duy nhất một đối tượng JSON hợp lệ tuân thủ đúng schema.");
                currentUserPrompt = sb.ToString();
            }

            var request = new LlmRequest(
                SystemPrompt: _promptTemplate.SystemPrompt,
                UserPrompt: currentUserPrompt,
                ModelOverride: _options.ModelOverride,
                Temperature: _options.Temperature,
                MaxOutputTokens: _options.MaxOutputTokens,
                JsonOutput: true);

            LlmResponse response;
            try
            {
                response = await _llmClient.CompleteAsync(request, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Caller cancellation: do not wrap or swallow, propagate immediately
                throw;
            }
            catch (LlmException ex) when (ex.Kind == LlmErrorKind.EmptyOrBlocked)
            {
                // Provider returned nothing or blocked the content (e.g. safety filter). This is an outcome about the
                // CONTENT, not a broken infrastructure: regenerating will not help, so hand it to a person.
                var blockedFailure = $"Provider returned empty or blocked output ({ex.Kind}).";
                allFailures.Add(blockedFailure);
                if (!string.IsNullOrWhiteSpace(ex.Model))
                {
                    modelsUsed.Add(ex.Model);
                }

                _logger?.LogWarning(
                    "Requirement analysis needs human review for ticket {TicketKey}: provider returned empty or blocked output (attempt {Attempt}).",
                    ticket.Key,
                    attempt);

                return new AnalysisResult(
                    Status: AnalysisStatus.NeedsHuman,
                    Analysis: null,
                    PromptName: _promptTemplate.Name,
                    PromptVersion: _promptTemplate.Version,
                    SchemaVersion: _schemaValidator.SchemaVersion,
                    ModelsUsed: modelsUsed,
                    GenerationAttempts: attempt,
                    TotalInputTokens: totalInputTokens,
                    TotalOutputTokens: totalOutputTokens,
                    TotalLatency: _timeProvider.GetElapsedTime(startTimestamp),
                    Failures: allFailures);
            }
            catch (LlmException ex)
            {
                // Infrastructure/configuration errors from LLM layer are not retried by the analyzer: rethrow
                _logger?.LogError(
                    "LLM invocation failed for ticket {TicketKey} with kind {ErrorKind} (attempts={Attempts}): {Message}",
                    ticket.Key,
                    ex.Kind,
                    ex.Attempts,
                    ex.Message);
                throw;
            }

            modelsUsed.Add(response.ModelUsed);
            totalInputTokens += response.InputTokens ?? 0;
            totalOutputTokens += response.OutputTokens ?? 0;

            // 1. Check FinishReason for truncation
            if (string.Equals(response.FinishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
            {
                var truncationFailure = "Generation was truncated because max tokens limit was reached (FinishReason: MAX_TOKENS).";
                allFailures.Add(truncationFailure);
                previousAttemptErrors = [truncationFailure];

                _logger?.LogWarning(
                    "Attempt {Attempt}/{MaxAttempts} for ticket {TicketKey} failed: truncated by MAX_TOKENS.",
                    attempt,
                    maxAttempts,
                    ticket.Key);
                continue;
            }

            // 2. Validate response with schema validator (checks JSON parse, schema, and domain rules)
            if (_schemaValidator.Validate(response.Text, out var analysis, out var validationErrors))
            {
                var totalLatency = _timeProvider.GetElapsedTime(startTimestamp);

                _logger?.LogInformation(
                    "Requirement analysis succeeded for ticket {TicketKey}: promptVersion={PromptVersion}, model={Model}, attempts={Attempts}, valid=true",
                    ticket.Key,
                    _promptTemplate.Version,
                    response.ModelUsed,
                    attempt);

                return new AnalysisResult(
                    Status: AnalysisStatus.Valid,
                    Analysis: analysis,
                    PromptName: _promptTemplate.Name,
                    PromptVersion: _promptTemplate.Version,
                    SchemaVersion: _schemaValidator.SchemaVersion,
                    ModelsUsed: modelsUsed,
                    GenerationAttempts: attempt,
                    TotalInputTokens: totalInputTokens,
                    TotalOutputTokens: totalOutputTokens,
                    TotalLatency: totalLatency,
                    Failures: allFailures);
            }

            // Validation failed for this attempt
            allFailures.AddRange(validationErrors);
            previousAttemptErrors = validationErrors;

            _logger?.LogWarning(
                "Attempt {Attempt}/{MaxAttempts} for ticket {TicketKey} failed validation with {ErrorCount} errors.",
                attempt,
                maxAttempts,
                ticket.Key,
                validationErrors.Count);
        }

        // Exhausted all generation attempts without a valid output
        var finalLatency = _timeProvider.GetElapsedTime(startTimestamp);

        _logger?.LogWarning(
            "Requirement analysis needs human review for ticket {TicketKey}: promptVersion={PromptVersion}, model={Model}, attempts={Attempts}, valid=false",
            ticket.Key,
            _promptTemplate.Version,
            modelsUsed.LastOrDefault() ?? "unknown",
            maxAttempts);

        return new AnalysisResult(
            Status: AnalysisStatus.NeedsHuman,
            Analysis: null,
            PromptName: _promptTemplate.Name,
            PromptVersion: _promptTemplate.Version,
            SchemaVersion: _schemaValidator.SchemaVersion,
            ModelsUsed: modelsUsed,
            GenerationAttempts: maxAttempts,
            TotalInputTokens: totalInputTokens,
            TotalOutputTokens: totalOutputTokens,
            TotalLatency: finalLatency,
            Failures: allFailures);
    }
}
