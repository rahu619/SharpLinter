namespace SharpLinter.Core.Analysis;

/// <summary>
/// Represents a single lint diagnostic — a specific issue found at a specific location.
/// </summary>
/// <param name="RuleId">The rule that triggered this diagnostic (e.g., "SL1001").</param>
/// <param name="Message">Human-readable description of the specific issue found.</param>
/// <param name="Severity">The severity level of this diagnostic.</param>
/// <param name="FilePath">Absolute or relative path to the file containing the issue.</param>
/// <param name="Line">1-based line number where the issue starts.</param>
/// <param name="Column">1-based column number where the issue starts.</param>
/// <param name="EndLine">1-based line number where the issue ends.</param>
/// <param name="EndColumn">1-based column number where the issue ends.</param>
public record LintDiagnostic(
    string RuleId,
    string Message,
    LintSeverity Severity,
    string FilePath,
    int Line,
    int Column,
    int EndLine,
    int EndColumn
);
