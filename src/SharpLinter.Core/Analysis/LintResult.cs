namespace SharpLinter.Core.Analysis;

/// <summary>
/// The result of running lint analysis on one or more files.
/// </summary>
public sealed class LintResult
{
    /// <summary>All diagnostics found during analysis.</summary>
    public IReadOnlyList<LintDiagnostic> Diagnostics { get; }

    /// <summary>Number of diagnostics with Error severity.</summary>
    public int ErrorCount { get; }

    /// <summary>Number of diagnostics with Warning severity.</summary>
    public int WarningCount { get; }

    /// <summary>Number of diagnostics with Suggestion severity.</summary>
    public int SuggestionCount { get; }

    /// <summary>Time taken for the analysis.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Optionally formatted/fixed code (when auto-fix is enabled).</summary>
    public string? FormattedCode { get; }

    public LintResult(
        IReadOnlyList<LintDiagnostic> diagnostics,
        TimeSpan duration,
        string? formattedCode = null)
    {
        Diagnostics = diagnostics;
        Duration = duration;
        FormattedCode = formattedCode;

        int errors = 0, warnings = 0, suggestions = 0;
        foreach (var d in diagnostics)
        {
            switch (d.Severity)
            {
                case LintSeverity.Error: errors++; break;
                case LintSeverity.Warning: warnings++; break;
                case LintSeverity.Suggestion: suggestions++; break;
            }
        }
        ErrorCount = errors;
        WarningCount = warnings;
        SuggestionCount = suggestions;
    }

    /// <summary>True if any diagnostics with Error severity were found.</summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>True if any diagnostics were found at any severity level.</summary>
    public bool HasIssues => Diagnostics.Count > 0;
}
