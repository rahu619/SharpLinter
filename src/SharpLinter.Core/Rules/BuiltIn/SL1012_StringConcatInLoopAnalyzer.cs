using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1012: String concatenation inside loops should use StringBuilder.
/// Detects += with string operands inside for, foreach, while, and do loops.
/// Inspired by CA1845.
/// </summary>
public sealed class SL1012_StringConcatInLoopAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1012",
        Title: "Avoid string concatenation in loops — use StringBuilder",
        Description: "String concatenation (+= or + in loops) creates a new string object on each iteration, leading to O(n²) performance. Use StringBuilder instead.",
        Category: RuleCategory.Performance,
        DefaultSeverity: LintSeverity.Warning,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1845"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var walker = new StringConcatWalker(filePath, severity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class StringConcatWalker(string filePath, LintSeverity severity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitForStatement(ForStatementSyntax node)
        {
            CheckBlockForStringConcat(node.Statement);
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            CheckBlockForStringConcat(node.Statement);
            base.VisitForEachStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            CheckBlockForStringConcat(node.Statement);
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            CheckBlockForStringConcat(node.Statement);
            base.VisitDoStatement(node);
        }

        private void CheckBlockForStringConcat(StatementSyntax statement)
        {
            var assignments = statement.DescendantNodes().OfType<AssignmentExpressionSyntax>();

            foreach (var assignment in assignments)
            {
                // Check for: str += "something" or str += variable
                if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
                {
                    // Heuristic: if the right side is a string literal, interpolated string,
                    // or the left side is commonly named like a string builder pattern
                    if (IsLikelyStringExpression(assignment.Right) || IsLikelyStringExpression(assignment.Left))
                    {
                        ReportDiagnostic(assignment);
                    }
                }

                // Check for: str = str + "something"
                if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                    && assignment.Right is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } addExpr)
                {
                    if ((IsLikelyStringExpression(addExpr.Left) || IsLikelyStringExpression(addExpr.Right))
                        && addExpr.Left.ToString() == assignment.Left.ToString())
                    {
                        ReportDiagnostic(assignment);
                    }
                }
            }
        }

        private static bool IsLikelyStringExpression(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression }
                || expression is InterpolatedStringExpressionSyntax;
        }

        private void ReportDiagnostic(AssignmentExpressionSyntax assignment)
        {
            var span = assignment.GetLocation().GetLineSpan();
            Diagnostics.Add(new LintDiagnostic(
                RuleId: "SL1012",
                Message: "String concatenation in a loop — consider using StringBuilder",
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
