using Microsoft.CodeAnalysis;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules;

/// <summary>
/// Interface for all lint rule analyzers — built-in, custom, or provider-backed.
/// Each implementation inspects a Roslyn SyntaxTree and returns diagnostics.
/// </summary>
public interface IRuleAnalyzer
{
    /// <summary>
    /// Metadata describing this rule (ID, title, category, severity, etc.).
    /// </summary>
    RuleMetadata Metadata { get; }

    /// <summary>
    /// Analyzes the given syntax tree and returns any diagnostics found.
    /// </summary>
    /// <param name="tree">The Roslyn syntax tree to analyze.</param>
    /// <param name="filePath">The file path (for diagnostic reporting).</param>
    /// <param name="config">The current lint configuration (for rule-specific options).</param>
    /// <returns>A list of diagnostics found by this rule.</returns>
    IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config);
}
