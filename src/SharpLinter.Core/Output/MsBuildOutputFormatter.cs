using System.Text;
using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Output;

/// <summary>
/// Formats lint results in the standard MSBuild error/warning format:
/// {FilePath}({Line},{Column}): {Severity} {Code}: {Message}
/// This allows MSBuild to natively parse the output and display them as actual IDE compiler warnings.
/// </summary>
public sealed class MsBuildOutputFormatter : IOutputFormatter
{
    public string Format(LintResult result) => Format([result]);

    public string Format(IReadOnlyList<LintResult> results)
    {
        var sb = new StringBuilder();
        var allDiagnostics = results.SelectMany(r => r.Diagnostics).ToList();

        foreach (var diagnostic in allDiagnostics.OrderBy(d => d.FilePath).ThenBy(d => d.Line).ThenBy(d => d.Column))
        {
            var severity = MapSeverity(diagnostic.Severity);
            
            // Format: FilePath(Line,Column): Severity Code: [SharpLinter] Message (colored cyan)
            sb.AppendLine($"{diagnostic.FilePath}({diagnostic.Line},{diagnostic.Column}): {severity} {diagnostic.RuleId}: \u001b[36m[SharpLinter]\u001b[0m {diagnostic.Message}");
        }

        return sb.ToString();
    }

    private static string MapSeverity(LintSeverity severity) => severity switch
    {
        LintSeverity.Error => "error",
        LintSeverity.Warning => "warning",
        LintSeverity.Suggestion => "warning", // MSBuild doesn't support 'suggestion', map to warning or info
        _ => "info"
    };
}
