using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1011: Prefer pattern matching over type check + cast.
/// Detects: if (x is Type) { var y = (Type)x; } → suggest: if (x is Type y)
/// Inspired by IDE0019, IDE0020.
/// </summary>
public sealed class SL1011_PatternMatchingAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1011",
        Title: "Use pattern matching instead of type check and cast",
        Description: "Use C# pattern matching (is Type variable) instead of separate type check and cast. This is more concise and avoids potential null reference issues.",
        Category: RuleCategory.Style,
        DefaultSeverity: LintSeverity.Suggestion,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0020"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var walker = new PatternMatchingWalker(filePath, severity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class PatternMatchingWalker(string filePath, LintSeverity severity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            // Pattern 1: if (x is Type) { ... (Type)x ... }
            if (node.Condition is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression } isExpr)
            {
                var checkedExpr = isExpr.Left.ToString();

                // Look for a cast to the same type in the if body
                if (node.Statement is BlockSyntax block)
                {
                    foreach (var castExpr in block.DescendantNodes().OfType<CastExpressionSyntax>())
                    {
                        if (castExpr.Type.ToString() == isExpr.Right.ToString()
                            && castExpr.Expression.ToString() == checkedExpr)
                        {
                            var span = isExpr.GetLocation().GetLineSpan();
                            Diagnostics.Add(new LintDiagnostic(
                                RuleId: "SL1011",
                                Message: $"Use pattern matching: 'if ({checkedExpr} is {isExpr.Right} variable)' instead of type check + cast",
                                Severity: severity,
                                FilePath: filePath,
                                Line: span.StartLinePosition.Line + 1,
                                Column: span.StartLinePosition.Character + 1,
                                EndLine: span.EndLinePosition.Line + 1,
                                EndColumn: span.EndLinePosition.Character + 1
                            ));
                            break;
                        }
                    }
                }
            }

            // Pattern 2: x as Type followed by null check
            // if (x as Type != null) → use: if (x is Type variable)
            if (node.Condition is BinaryExpressionSyntax nullCheckExpr
                && (nullCheckExpr.IsKind(SyntaxKind.NotEqualsExpression) || nullCheckExpr.IsKind(SyntaxKind.EqualsExpression)))
            {
                var asExpr = nullCheckExpr.Left as BinaryExpressionSyntax
                          ?? nullCheckExpr.Right as BinaryExpressionSyntax;

                if (asExpr?.IsKind(SyntaxKind.AsExpression) == true)
                {
                    var span = node.Condition.GetLocation().GetLineSpan();
                    Diagnostics.Add(new LintDiagnostic(
                        RuleId: "SL1011",
                        Message: "Use pattern matching instead of 'as' operator with null check",
                        Severity: severity,
                        FilePath: filePath,
                        Line: span.StartLinePosition.Line + 1,
                        Column: span.StartLinePosition.Character + 1,
                        EndLine: span.EndLinePosition.Line + 1,
                        EndColumn: span.EndLinePosition.Character + 1
                    ));
                }
            }

            base.VisitIfStatement(node);
        }
    }
}
