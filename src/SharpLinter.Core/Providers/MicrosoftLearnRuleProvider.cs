using AngleSharp;
using AngleSharp.Dom;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Rules;

namespace SharpLinter.Core.Providers;

/// <summary>
/// Fetches C# code analysis rule metadata from Microsoft Learn documentation pages.
/// Parses the public HTML tables to extract rule IDs, titles, and categories.
/// </summary>
public sealed class MicrosoftLearnRuleProvider : IRuleProvider
{
    private const string QualityRulesUrl = "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/";
    private const string StyleRulesUrl = "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/";

    public bool IsAvailableOffline => false;

    /// <summary>
    /// Fetches rule metadata from Microsoft Learn CA and IDE rule pages.
    /// </summary>
    public async Task<IReadOnlyList<RuleMetadata>> GetRulesAsync(CancellationToken ct = default)
    {
        var rules = new List<RuleMetadata>();

        try
        {
            var browsingConfig = AngleSharp.Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(browsingConfig);

            // Fetch quality rules (CA####)
            var qualityRules = await FetchRulesFromPage(context, QualityRulesUrl, "microsoft-learn", ct);
            rules.AddRange(qualityRules);

            // Fetch style rules (IDE####)
            var styleRules = await FetchRulesFromPage(context, StyleRulesUrl, "microsoft-learn", ct);
            rules.AddRange(styleRules);
        }
        catch (Exception)
        {
            // Network failure — return empty, caller should fall back to cache/bundled
        }

        return rules;
    }

    private static async Task<List<RuleMetadata>> FetchRulesFromPage(
        IBrowsingContext context, string url, string source, CancellationToken ct)
    {
        var rules = new List<RuleMetadata>();

        var document = await context.OpenAsync(url, ct);
        if (document == null) return rules;

        // Find all table rows containing rule data
        var rows = document.QuerySelectorAll("table tbody tr");

        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("td").ToList();
            if (cells.Count < 2) continue;

            var linkElement = cells[0].QuerySelector("a");
            var ruleText = linkElement?.TextContent?.Trim() ?? cells[0].TextContent?.Trim();
            if (string.IsNullOrEmpty(ruleText)) continue;

            // Parse "CA1001: Types that own disposable fields should be disposable"
            var colonIndex = ruleText.IndexOf(':');
            if (colonIndex < 0) continue;

            var ruleId = ruleText[..colonIndex].Trim();
            var title = ruleText[(colonIndex + 1)..].Trim();

            // Category is typically in the second column
            var category = cells.Count > 1 ? cells[1].TextContent?.Trim() : null;

            var href = linkElement?.GetAttribute("href");
            var docUrl = href != null && !href.StartsWith("http")
                ? new Uri(new Uri(url), href).ToString()
                : href;

            rules.Add(new RuleMetadata(
                RuleId: ruleId,
                Title: title,
                Description: title, // Full description would need individual page fetch
                Category: ParseCategory(category),
                DefaultSeverity: LintSeverity.Warning,
                Source: source,
                HasAnalyzer: false, // Fetched rules don't have built-in analyzers
                DocumentationUrl: docUrl
            ));
        }

        return rules;
    }

    private static RuleCategory ParseCategory(string? category) => category?.ToLowerInvariant() switch
    {
        "design" => RuleCategory.Design,
        "naming" => RuleCategory.Naming,
        "style" => RuleCategory.Style,
        "performance" => RuleCategory.Performance,
        "security" => RuleCategory.Security,
        "maintainability" => RuleCategory.Maintainability,
        "reliability" => RuleCategory.Design,
        "usage" => RuleCategory.Design,
        "globalization" => RuleCategory.Design,
        "interoperability" => RuleCategory.Design,
        _ => RuleCategory.Design
    };
}
