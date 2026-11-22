using System.Text.RegularExpressions;

namespace AgentService.Ba;

/// <summary>
/// Represents a versioned prompt template containing System instructions and a User template.
/// Handles front-matter parsing, version validation, single-pass placeholder replacement,
/// and tag breakout neutralization.
/// </summary>
public sealed class PromptTemplate
{
    private static readonly Regex VersionFileNameRegex = new(@"\.v(\d+)\.md$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TicketDataTagRegex = new(@"</?ticket_data\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PlaceholderRegex = new(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex TicketKeyRegex = new(@"^[A-Za-z][A-Za-z0-9_]{0,19}-\d{1,9}$", RegexOptions.Compiled);

    public string Name { get; }
    public int Version { get; }
    public string SystemPrompt { get; }
    public string UserPromptTemplate { get; }

    public PromptTemplate(string name, int version, string systemPrompt, string userPromptTemplate)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version;
        SystemPrompt = systemPrompt ?? throw new ArgumentNullException(nameof(systemPrompt));
        UserPromptTemplate = userPromptTemplate ?? throw new ArgumentNullException(nameof(userPromptTemplate));
    }

    public static PromptTemplate FromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Prompt file not found: '{filePath}'", filePath);
        }

        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileName(filePath);
        return Parse(content, fileName);
    }

    public static PromptTemplate Parse(string content, string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // 1. Extract version from file name (e.g., requirement-analysis.v1.md -> 1)
        var versionMatch = VersionFileNameRegex.Match(fileName);
        if (!versionMatch.Success)
        {
            throw new FormatException($"Filename '{fileName}' does not contain a valid version suffix like '.v1.md'.");
        }
        var fileVersion = int.Parse(versionMatch.Groups[1].Value);

        // 2. Parse YAML-style front matter (--- ... ---)
        var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        if (lines.Length < 3 || lines[0].Trim() != "---")
        {
            throw new FormatException("Prompt file is missing opening '---' for front matter.");
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

        if (closingIndex == -1)
        {
            throw new FormatException("Prompt file is missing closing '---' for front matter.");
        }

        string? frontName = null;
        int? frontVersion = null;

        for (int i = 1; i < closingIndex; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                var key = line[..colonIdx].Trim();
                var val = line[(colonIdx + 1)..].Trim();
                if (string.Equals(key, "name", StringComparison.OrdinalIgnoreCase))
                {
                    frontName = val;
                }
                else if (string.Equals(key, "version", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(val, out var v))
                    {
                        frontVersion = v;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(frontName))
        {
            throw new FormatException("Front matter is missing required 'name' property.");
        }

        if (!frontVersion.HasValue)
        {
            throw new FormatException("Front matter is missing required 'version' property.");
        }

        if (fileVersion != frontVersion.Value)
        {
            throw new InvalidOperationException(
                $"Version mismatch: filename indicates version {fileVersion}, but front matter specifies version {frontVersion.Value}.");
        }

        // 3. Extract sections (## System and ## User)
        var body = string.Join("\n", lines.Skip(closingIndex + 1));
        var systemMatch = Regex.Match(body, @"##\s+System\s*\n([\s\S]*?)(?=(##\s+User)|$)", RegexOptions.IgnoreCase);
        var userMatch = Regex.Match(body, @"##\s+User\s*\n([\s\S]*?)$", RegexOptions.IgnoreCase);

        if (!systemMatch.Success || string.IsNullOrWhiteSpace(systemMatch.Groups[1].Value))
        {
            throw new InvalidOperationException("Prompt file is missing required '## System' section or the section is empty.");
        }

        if (!userMatch.Success || string.IsNullOrWhiteSpace(userMatch.Groups[1].Value))
        {
            throw new InvalidOperationException("Prompt file is missing required '## User' section or the section is empty.");
        }

        var systemPrompt = systemMatch.Groups[1].Value.Trim();
        var userTemplate = userMatch.Groups[1].Value.Trim();

        // 4. Validate required placeholders
        string[] requiredPlaceholders = ["{{ticket_key}}", "{{ticket_title}}", "{{ticket_description}}"];
        foreach (var placeholder in requiredPlaceholders)
        {
            if (!userTemplate.Contains(placeholder, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Prompt template is missing required placeholder '{placeholder}'.");
            }
        }

        return new PromptTemplate(frontName, fileVersion, systemPrompt, userTemplate);
    }

    /// <summary>
    /// Renders the user prompt with ticket values.
    /// Neutralizes <ticket_data> tags inside untrusted fields and performs single-pass placeholder replacement.
    /// </summary>
    public string RenderUserPrompt(TicketInput ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        // Mã ticket nằm ngoài khối <ticket_data>, nên không được chứa chữ tự do: chỉ nhận đúng dạng Jira (ví dụ TAS-12).
        if (ticket.Key is null || !TicketKeyRegex.IsMatch(ticket.Key))
        {
            throw new ArgumentException(
                "Ticket key must look like 'ABC-123' (letters, optional digits/underscore, a dash, then digits).",
                nameof(ticket));
        }

        var sanitizedTitle = NeutralizeTicketDataTags(ticket.Title ?? string.Empty);
        var sanitizedDescription = NeutralizeTicketDataTags(ticket.Description ?? string.Empty);

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ticket_key"] = ticket.Key,
            ["ticket_title"] = sanitizedTitle,
            ["ticket_description"] = sanitizedDescription
        };

        // Single-pass replacement: matched {{key}} tokens are replaced once.
        // Any {{placeholder}} tokens present inside values are NOT expanded.
        return PlaceholderRegex.Replace(UserPromptTemplate, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var val) ? val : match.Value;
        });
    }

    /// <summary>
    /// Neutralizes <ticket_data> and </ticket_data> tags to prevent ticket content from closing or breaking
    /// the template's data enclosure.
    /// </summary>
    public static string NeutralizeTicketDataTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return TicketDataTagRegex.Replace(input, m => m.Value.Replace("<", "&lt;").Replace(">", "&gt;"));
    }
}
