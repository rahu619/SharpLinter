using System.Text;
using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Output;

/// <summary>
/// Formats lint results for terminal display with colored, ESLint-style output.
/// Groups diagnostics by file with severity-based coloring.
/// </summary>
public sealed class ConsoleOutputFormatter : IOutputFormatter
{
    public string Format(LintResult result)
    {
        return Format([result]);
    }

    public string Format(IReadOnlyList<LintResult> results)
    {
        var sb = new StringBuilder();
        var allDiagnostics = results.SelectMany(r => r.Diagnostics).ToList();

        if (allDiagnostics.Count == 0)
        {
            sb.AppendLine("✅ No issues found.");
            return sb.ToString();
        }

        // Group by file
        var grouped = allDiagnostics.GroupBy(d => d.FilePath);

        foreach (var fileGroup in grouped)
        {
            sb.AppendLine();
            sb.AppendLine($"📄 {fileGroup.Key}");

            foreach (var diagnostic in fileGroup.OrderBy(d => d.Line).ThenBy(d => d.Column))
            {
                var icon = GetSeverityIcon(diagnostic.Severity);
                var color = GetSeverityColor(diagnostic.Severity);

                sb.AppendLine($"  {color}{icon} {diagnostic.Line}:{diagnostic.Column}  {diagnostic.Severity.ToString().ToLowerInvariant()}  {diagnostic.Message}  [{diagnostic.RuleId}]{ResetColor()}");
            }
        }

        // Summary
        sb.AppendLine();
        var totalErrors = allDiagnostics.Count(d => d.Severity == LintSeverity.Error);
        var totalWarnings = allDiagnostics.Count(d => d.Severity == LintSeverity.Warning);
        var totalSuggestions = allDiagnostics.Count(d => d.Severity == LintSeverity.Suggestion);

        var parts = new List<string>();
        if (totalErrors > 0) parts.Add($"❌ {totalErrors} error(s)");
        if (totalWarnings > 0) parts.Add($"⚠️  {totalWarnings} warning(s)");
        if (totalSuggestions > 0) parts.Add($"💡 {totalSuggestions} suggestion(s)");

        sb.AppendLine($"  {string.Join("  ", parts)}");
        sb.AppendLine($"  Found {allDiagnostics.Count} issue(s) in {grouped.Count()} file(s)");

        return sb.ToString();
    }

    private static string GetSeverityIcon(LintSeverity severity) => severity switch
    {
        LintSeverity.Error => "✖",
        LintSeverity.Warning => "⚠",
        LintSeverity.Suggestion => "ℹ",
        _ => " "
    };

    private static string GetSeverityColor(LintSeverity severity) => severity switch
    {
        LintSeverity.Error => "\u001b[31m",      // Red
        LintSeverity.Warning => "\u001b[33m",    // Yellow
        LintSeverity.Suggestion => "\u001b[36m", // Cyan
        _ => ""
    };

    private static string ResetColor() => "\u001b[0m";
}
