using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1001: Control flow statements (if, else, for, foreach, while, do) should use braces.
/// Inspired by IDE0011, SA1503.
/// </summary>
public sealed class SL1001_AddBracesAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1001",
        Title: "Add braces to control flow statements",
        Description: "Control flow statements (if, else, for, foreach, while, do) should always use braces, even for single-line bodies. This prevents bugs from accidental mis-indentation.",
        Category: RuleCategory.Style,
        DefaultSeverity: LintSeverity.Warning,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0011"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var walker = new BracesWalker(filePath, severity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class BracesWalker(string filePath, LintSeverity severity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            CheckStatement(node.Statement, "if");
            if (node.Else?.Statement is not IfStatementSyntax and not null)
            {
                CheckStatement(node.Else.Statement, "else");
            }
            base.VisitIfStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            CheckStatement(node.Statement, "for");
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            CheckStatement(node.Statement, "foreach");
            base.VisitForEachStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            CheckStatement(node.Statement, "while");
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            CheckStatement(node.Statement, "do");
            base.VisitDoStatement(node);
        }

        private void CheckStatement(StatementSyntax statement, string keyword)
        {
            if (statement is not BlockSyntax)
            {
                var span = statement.GetLocation().GetLineSpan();
                Diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1001",
                    Message: $"'{keyword}' statement should use braces",
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
