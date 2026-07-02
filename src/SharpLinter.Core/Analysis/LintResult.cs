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
        ErrorCount = diagnostics.Count(d => d.Severity == LintSeverity.Error);
        WarningCount = diagnostics.Count(d => d.Severity == LintSeverity.Warning);
        SuggestionCount = diagnostics.Count(d => d.Severity == LintSeverity.Suggestion);
    }

    /// <summary>True if any diagnostics with Error severity were found.</summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>True if any diagnostics were found at any severity level.</summary>
    public bool HasIssues => Diagnostics.Count > 0;
}
