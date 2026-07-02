using System.Reflection;
using System.Text.Json;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Rules;

namespace SharpLinter.Core.Providers;

/// <summary>
/// Provides a bundled snapshot of the most common Microsoft CA/IDE rules.
/// This is embedded in the NuGet package and ensures SharpLinter works
/// fully offline even on first run without any cache.
/// </summary>
public sealed class BundledRuleCatalog : IRuleProvider
{
    public bool IsAvailableOffline => true;

    public Task<IReadOnlyList<RuleMetadata>> GetRulesAsync(CancellationToken ct = default)
    {
        var rules = LoadEmbeddedRules();
        return Task.FromResult(rules);
    }

    private static IReadOnlyList<RuleMetadata> LoadEmbeddedRules()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "SharpLinter.Core.Providers.bundled-rules.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback: return a hardcoded minimal set
            return GetFallbackRules();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var rules = JsonSerializer.Deserialize<List<BundledRule>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return rules?.Select(r => new RuleMetadata(
            RuleId: r.RuleId ?? "",
            Title: r.Title ?? "",
            Description: r.Description ?? r.Title ?? "",
            Category: Enum.TryParse<RuleCategory>(r.Category, true, out var cat) ? cat : RuleCategory.Design,
            DefaultSeverity: LintSeverity.Warning,
            Source: "microsoft-learn",
            HasAnalyzer: false,
            DocumentationUrl: r.DocumentationUrl
        )).ToList() ?? GetFallbackRules();
    }

    private static List<RuleMetadata> GetFallbackRules() =>
    [
        new("CA1001", "Types that own disposable fields should be disposable", "Types that own disposable fields should be disposable", RuleCategory.Design, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1001"),
        new("CA1031", "Do not catch general exception types", "Do not catch general exception types", RuleCategory.Design, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1031"),
        new("CA1051", "Do not declare visible instance fields", "Do not declare visible instance fields", RuleCategory.Design, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1051"),
        new("CA1062", "Validate arguments of public methods", "Validate arguments of public methods", RuleCategory.Design, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1062"),
        new("CA1715", "Identifiers should have correct prefix", "Identifiers should have correct prefix", RuleCategory.Naming, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1715"),
        new("CA1822", "Mark members as static", "Mark members as static", RuleCategory.Performance, LintSeverity.Suggestion, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1822"),
        new("CA1845", "Use span-based string.Concat", "Use span-based string.Concat", RuleCategory.Performance, LintSeverity.Suggestion, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1845"),
        new("CA2000", "Dispose objects before losing scope", "Dispose objects before losing scope", RuleCategory.Design, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2000"),
        new("CA2007", "Do not directly await a Task", "Do not directly await a Task without ConfigureAwait", RuleCategory.Design, LintSeverity.Suggestion, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007"),
        new("CA2227", "Collection properties should be read only", "Collection properties should be read only", RuleCategory.Design, LintSeverity.Warning, "microsoft-learn", false, "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2227"),
    ];

    private sealed class BundledRule
    {
        public string? RuleId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? DocumentationUrl { get; set; }
    }
}
