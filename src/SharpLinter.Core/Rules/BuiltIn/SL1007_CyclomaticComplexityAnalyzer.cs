using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1007: Cyclomatic complexity should not exceed a configurable threshold.
/// Counts decision points (if, else if, case, while, for, foreach, &&, ||, ??, ?:, catch).
/// Default threshold: 10.
/// </summary>
public sealed class SL1007_CyclomaticComplexityAnalyzer : IRuleAnalyzer
{
    private const int DefaultMaxComplexity = 10;

    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1007",
        Title: "Method has high cyclomatic complexity",
        Description: "High cyclomatic complexity makes code hard to test and maintain. Consider decomposing into smaller methods. Default threshold: 10.",
        Category: RuleCategory.Maintainability,
        DefaultSeverity: LintSeverity.Suggestion,
        Source: "builtin",
        HasAnalyzer: true
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var maxComplexity = config.GetRuleOption(Metadata.RuleId, "maxComplexity", DefaultMaxComplexity);
        var root = tree.GetRoot();
        var walker = new ComplexityWalker(filePath, severity, maxComplexity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class ComplexityWalker(string filePath, LintSeverity severity, int maxComplexity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Body != null || node.ExpressionBody != null)
            {
                var complexity = CalculateComplexity(node);
                CheckComplexity(complexity, node.Identifier.Text, node.Identifier);
            }
            // Don't call base — we don't want to count nested methods double
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Body != null)
            {
                var complexity = CalculateComplexity(node);
                CheckComplexity(complexity, node.Identifier.Text, node.Identifier);
            }
        }

        private static int CalculateComplexity(SyntaxNode methodNode)
        {
            int complexity = 1; // Base complexity

            foreach (var node in methodNode.DescendantNodes())
            {
                complexity += node switch
                {
                    IfStatementSyntax => 1,
                    WhileStatementSyntax => 1,
                    ForStatementSyntax => 1,
                    ForEachStatementSyntax => 1,
                    DoStatementSyntax => 1,
                    CaseSwitchLabelSyntax => 1,
                    CasePatternSwitchLabelSyntax => 1,
                    ConditionalExpressionSyntax => 1,
                    CatchClauseSyntax => 1,
                    SwitchExpressionArmSyntax => 1,
                    _ => 0
                };
            }

            // Count logical operators (&&, ||, ??)
            foreach (var token in methodNode.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.AmpersandAmpersandToken)
                    || token.IsKind(SyntaxKind.BarBarToken)
                    || token.IsKind(SyntaxKind.QuestionQuestionToken))
                {
                    complexity++;
                }
            }

            return complexity;
        }

        private void CheckComplexity(int complexity, string name, SyntaxToken identifier)
        {
            if (complexity > maxComplexity)
            {
                var span = identifier.GetLocation().GetLineSpan();
                Diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1007",
                    Message: $"Method '{name}' has cyclomatic complexity of {complexity} (max: {maxComplexity})",
                    Severity: severity,
                    FilePath: filePath,
                    Line: span.StartLinePosition.Line + 1,
                    Column: span.StartLinePosition.Character + 1,
                    EndLine: span.EndLinePosition.Line + 1,
                    EndColumn: span.EndLinePosition.Character + 1
                ));
            }
        }
    }
}
