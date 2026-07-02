using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Configuration;

/// <summary>
/// Represents a per-rule configuration override.
/// </summary>
public sealed class RuleOverride
{
    /// <summary>The severity to apply for this rule.</summary>
    public LintSeverity Severity { get; set; } = LintSeverity.Warning;

    /// <summary>Rule-specific options (e.g., maxLines, maxComplexity).</summary>
    public Dictionary<string, object> Options { get; set; } = new();
}
