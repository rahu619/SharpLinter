using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Rules;

/// <summary>
/// Describes a lint rule's metadata — its identity, classification, and origin.
/// </summary>
/// <param name="RuleId">Unique rule identifier (e.g., "SL1001", "CA1051", "CUSTOM001").</param>
/// <param name="Title">Human-readable short title (e.g., "Add braces to control flow").</param>
/// <param name="Description">Detailed explanation of the rule and why it matters.</param>
/// <param name="Category">The rule's classification category.</param>
/// <param name="DefaultSeverity">Default severity when no configuration override is applied.</param>
/// <param name="Source">Origin of the rule: "builtin", "microsoft-learn", or "custom".</param>
/// <param name="HasAnalyzer">Whether SharpLinter has a built-in analyzer implementation for this rule.</param>
/// <param name="DocumentationUrl">Optional link to external documentation (e.g., MS Learn page).</param>
public record RuleMetadata(
    string RuleId,
    string Title,
    string Description,
    RuleCategory Category,
    LintSeverity DefaultSeverity,
    string Source,
    bool HasAnalyzer,
    string? DocumentationUrl = null
);
