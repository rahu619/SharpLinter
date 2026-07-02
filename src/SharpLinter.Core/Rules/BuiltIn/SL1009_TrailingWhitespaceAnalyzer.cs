using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1009: Lines should not have trailing whitespace.
/// Inspired by SA1028.
/// </summary>
public sealed class SL1009_TrailingWhitespaceAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1009",
        Title: "Trailing whitespace detected",
        Description: "Lines should not have trailing whitespace characters. Trailing whitespace creates noisy diffs and serves no purpose.",
        Category: RuleCategory.Style,
        DefaultSeverity: LintSeverity.Suggestion,
        Source: "builtin",
        HasAnalyzer: true
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var diagnostics = new List<LintDiagnostic>();
        var text = tree.GetText();

        for (int i = 0; i < text.Lines.Count; i++)
        {
            var line = text.Lines[i];
            var lineText = line.ToString();

            if (lineText.Length > 0 && char.IsWhiteSpace(lineText[^1]))
            {
                // Find where the trailing whitespace starts
                int trailingStart = lineText.Length - 1;
                while (trailingStart > 0 && char.IsWhiteSpace(lineText[trailingStart - 1]))
                {
                    trailingStart--;
                }

                diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1009",
                    Message: "Line has trailing whitespace",
                    Severity: severity,
                    FilePath: filePath,
                    Line: i + 1,
                    Column: trailingStart + 1,
                    EndLine: i + 1,
                    EndColumn: lineText.Length + 1
                ));
            }
        }

        return diagnostics;
    }
}
