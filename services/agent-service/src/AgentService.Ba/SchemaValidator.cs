using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace AgentService.Ba;

/// <summary>
/// Validates JSON output against the requirement-analysis JSON schema and extra domain rules.
/// </summary>
public sealed class SchemaValidator
{
    private readonly JsonSchema _schema;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string SchemaVersion { get; }

    public SchemaValidator(JsonSchema schema, string schemaVersion = "requirement-analysis.v1")
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        SchemaVersion = schemaVersion;
    }

    public static SchemaValidator FromFile(string filePath, string schemaVersion = "requirement-analysis.v1")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Schema file not found: '{filePath}'", filePath);
        }

        var buildOptions = new BuildOptions { SchemaRegistry = new SchemaRegistry() };
        var schema = JsonSchema.FromFile(filePath, buildOptions);
        return new SchemaValidator(schema, schemaVersion);
    }

    public static SchemaValidator FromText(string schemaJson, string schemaVersion = "requirement-analysis.v1")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaJson);
        var buildOptions = new BuildOptions { SchemaRegistry = new SchemaRegistry() };
        var schema = JsonSchema.FromText(schemaJson, buildOptions);
        return new SchemaValidator(schema, schemaVersion);
    }

    /// <summary>
    /// Unwraps one outer layer of markdown code fences (```json ... ``` or ``` ... ```) if present.
    /// </summary>
    public static string UnwrapMarkdownFence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewline = trimmed.IndexOfAny(['\r', '\n']);
            if (firstNewline > 0)
            {
                var firstLine = trimmed[..firstNewline].Trim();
                if (firstLine.Equals("```", StringComparison.OrdinalIgnoreCase) ||
                    firstLine.Equals("```json", StringComparison.OrdinalIgnoreCase))
                {
                    int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                    if (lastFence > firstNewline)
                    {
                        return trimmed[(firstNewline + 1)..lastFence].Trim();
                    }
                }
            }
        }

        return text;
    }

    /// <summary>
    /// Validates raw text output from the LLM.
    /// </summary>
    public bool Validate(string rawText, out RequirementAnalysis? analysis, out List<string> errors)
    {
        errors = [];
        analysis = null;

        var text = UnwrapMarkdownFence(rawText);

        // 1. JSON parsing
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parse error: {ex.Message}");
            return false;
        }

        using (doc)
        {
            // 2. Schema evaluation
            var evalOptions = new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            };

            var evalResult = _schema.Evaluate(doc.RootElement, evalOptions);
            if (!evalResult.IsValid)
            {
                var schemaErrors = ExtractErrors(evalResult);
                errors.AddRange(schemaErrors.Take(5));
                return false;
            }
        }

        // 3. Deserialize
        try
        {
            analysis = JsonSerializer.Deserialize<RequirementAnalysis>(text, JsonOptions);
            if (analysis == null)
            {
                errors.Add("Deserialization produced null RequirementAnalysis.");
                return false;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Deserialization error: {ex.Message}");
            return false;
        }

        // 4. Extra checks outside schema
        // - summary not all whitespace
        if (string.IsNullOrWhiteSpace(analysis.Summary))
        {
            errors.Add("Field 'summary' must not be empty or whitespace only.");
        }

        // - userStory not all whitespace
        if (string.IsNullOrWhiteSpace(analysis.UserStory))
        {
            errors.Add("Field 'userStory' must not be empty or whitespace only.");
        }

        // - no two identical acceptanceCriteria items
        if (analysis.AcceptanceCriteria != null)
        {
            var duplicates = analysis.AcceptanceCriteria
                .GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                errors.Add($"Field 'acceptanceCriteria' contains duplicate criteria: '{duplicates[0]}'.");
            }
        }

        if (errors.Count > 0)
        {
            analysis = null;
            return false;
        }

        return true;
    }

    private static List<string> ExtractErrors(EvaluationResults results)
    {
        var list = new List<string>();

        void Collect(EvaluationResults node)
        {
            if (node.Errors != null && node.Errors.Count > 0)
            {
                foreach (var (keyword, message) in node.Errors)
                {
                    var location = node.InstanceLocation.ToString();
                    var locationText = string.IsNullOrEmpty(location) ? "root" : location;
                    list.Add($"{locationText}: {keyword} - {message}");
                }
            }

            if (node.Details != null)
            {
                foreach (var detail in node.Details)
                {
                    Collect(detail);
                }
            }
        }

        Collect(results);
        return list;
    }
}
