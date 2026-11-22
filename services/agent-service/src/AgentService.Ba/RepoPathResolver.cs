namespace AgentService.Ba;

/// <summary>
/// Utility to locate repo-relative directories such as contracts/schemas and agents/ba/prompts.
/// </summary>
public static class RepoPathResolver
{
    public static string FindRepoRoot()
    {
        // 1. Walk up from AppContext.BaseDirectory
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "contracts", "schemas")) &&
                Directory.Exists(Path.Combine(current.FullName, "agents", "ba", "prompts")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        // 2. Walk up from current working directory
        current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "contracts", "schemas")) &&
                Directory.Exists(Path.Combine(current.FullName, "agents", "ba", "prompts")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate repository root containing both 'contracts/schemas' and 'agents/ba/prompts'.");
    }

    public static string ResolveSchemaPath(string? customPath = null, string schemaFileName = "requirement-analysis.v1.schema.json")
    {
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            return customPath;
        }

        if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
        {
            var candidate = Path.Combine(customPath, schemaFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var root = FindRepoRoot();
        var defaultPath = Path.Combine(root, "contracts", "schemas", schemaFileName);
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        throw new FileNotFoundException($"Schema file not found at '{defaultPath}'.", defaultPath);
    }

    public static string ResolvePromptPath(string? customPath = null, string promptFileName = "requirement-analysis.v1.md")
    {
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            return customPath;
        }

        if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
        {
            var candidate = Path.Combine(customPath, promptFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var root = FindRepoRoot();
        var defaultPath = Path.Combine(root, "agents", "ba", "prompts", promptFileName);
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        throw new FileNotFoundException($"Prompt file not found at '{defaultPath}'.", defaultPath);
    }
}
