using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1008: Brace placement should be consistent — either Allman style (new line) or K&amp;R (same line).
/// Default: Allman style (new line for braces), matching the .NET standard.
/// </summary>
public sealed class SL1008_ConsistentBracePlacementAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1008",
        Title: "Inconsistent brace placement",
        Description: "Brace placement should be consistent. Default style is Allman (opening brace on a new line), the .NET community standard.",
        Category: RuleCategory.Style,
        DefaultSeverity: LintSeverity.Suggestion,
        Source: "builtin",
        HasAnalyzer: true
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var style = config.GetRuleOption(Metadata.RuleId, "style", "newLine");
        var expectNewLine = style.Equals("newLine", StringComparison.OrdinalIgnoreCase);

        var root = tree.GetRoot();
        var diagnostics = new List<LintDiagnostic>();

        foreach (var openBrace in root.DescendantTokens().Where(t => t.IsKind(SyntaxKind.OpenBraceToken)))
        {
            // Skip initializers and collection expressions
            if (openBrace.Parent is InitializerExpressionSyntax
                || openBrace.Parent is AnonymousObjectCreationExpressionSyntax)
            {
                continue;
            }

            var braceLineSpan = openBrace.GetLocation().GetLineSpan();
            var braceLine = braceLineSpan.StartLinePosition.Line;

            // Get the previous significant token
            var prevToken = openBrace.GetPreviousToken();
            if (prevToken == default) continue;

            var prevLineSpan = prevToken.GetLocation().GetLineSpan();
            var prevLine = prevLineSpan.EndLinePosition.Line;

            var isOnNewLine = braceLine > prevLine;

            if (expectNewLine && !isOnNewLine)
            {
                diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1008",
                    Message: "Opening brace should be on a new line (Allman style)",
                    Severity: severity,
                    FilePath: filePath,
                    Line: braceLineSpan.StartLinePosition.Line + 1,
                    Column: braceLineSpan.StartLinePosition.Character + 1,
                    EndLine: braceLineSpan.EndLinePosition.Line + 1,
                    EndColumn: braceLineSpan.EndLinePosition.Character + 1
                ));
            }
            else if (!expectNewLine && isOnNewLine)
            {
                diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1008",
                    Message: "Opening brace should be on the same line (K&R style)",
                    Severity: severity,
                    FilePath: filePath,
                    Line: braceLineSpan.StartLinePosition.Line + 1,
                    Column: braceLineSpan.StartLinePosition.Character + 1,
                    EndLine: braceLineSpan.EndLinePosition.Line + 1,
                    EndColumn: braceLineSpan.EndLinePosition.Character + 1
                ));
            }
        }

        return diagnostics;
    }
}
