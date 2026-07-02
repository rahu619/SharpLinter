using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1004: Methods should not exceed a configurable line count.
/// Default: 50 lines.
/// </summary>
public sealed class SL1004_MethodLengthAnalyzer : IRuleAnalyzer
{
    private const int DefaultMaxLines = 50;

    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1004",
        Title: "Method is too long",
        Description: "Long methods are hard to understand and maintain. Consider extracting logic into smaller, well-named helper methods. Default threshold: 50 lines.",
        Category: RuleCategory.Maintainability,
        DefaultSeverity: LintSeverity.Suggestion,
        Source: "builtin",
        HasAnalyzer: true
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var maxLines = config.GetRuleOption(Metadata.RuleId, "maxLines", DefaultMaxLines);
        var root = tree.GetRoot();
        var walker = new MethodLengthWalker(filePath, severity, maxLines);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class MethodLengthWalker(string filePath, LintSeverity severity, int maxLines) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            CheckBody(node.Body, node.Identifier.Text, node.Identifier);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            CheckBody(node.Body, node.Identifier.Text, node.Identifier);
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            CheckBody(node.Body, node.Identifier.Text, node.Identifier);
            base.VisitLocalFunctionStatement(node);
        }

        private void CheckBody(BlockSyntax? body, string name, SyntaxToken identifier)
        {
            if (body == null) return;

            var bodySpan = body.GetLocation().GetLineSpan();
            var lineCount = bodySpan.EndLinePosition.Line - bodySpan.StartLinePosition.Line + 1;

            if (lineCount > maxLines)
            {
                var identSpan = identifier.GetLocation().GetLineSpan();
                Diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1004",
                    Message: $"Method '{name}' is {lineCount} lines long (max: {maxLines})",
                    Severity: severity,
                    FilePath: filePath,
                    Line: identSpan.StartLinePosition.Line + 1,
                    Column: identSpan.StartLinePosition.Character + 1,
                    EndLine: identSpan.EndLinePosition.Line + 1,
                    EndColumn: identSpan.EndLinePosition.Character + 1
                ));
            }
        }
    }
}
