using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1002: Catch blocks should not be empty — they silently swallow exceptions.
/// Inspired by CA1031.
/// </summary>
public sealed class SL1002_EmptyCatchBlockAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1002",
        Title: "Avoid empty catch blocks",
        Description: "Empty catch blocks silently swallow exceptions, hiding potential bugs. At minimum, log the exception or add a comment explaining why it is intentionally ignored.",
        Category: RuleCategory.Design,
        DefaultSeverity: LintSeverity.Warning,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1031"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var walker = new EmptyCatchWalker(filePath, severity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class EmptyCatchWalker(string filePath, LintSeverity severity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            if (node.Block.Statements.Count == 0)
            {
                // Check if there's at least a comment explaining the empty catch
                var hasComment = node.Block.CloseBraceToken.LeadingTrivia
                    .Concat(node.Block.OpenBraceToken.TrailingTrivia)
                    .Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia)
                           || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

                if (!hasComment)
                {
                    var span = node.CatchKeyword.GetLocation().GetLineSpan();
                    Diagnostics.Add(new LintDiagnostic(
                        RuleId: "SL1002",
                        Message: "Empty catch block — exceptions should not be silently swallowed",
                        Severity: severity,
                        FilePath: filePath,
                        Line: span.StartLinePosition.Line + 1,
                        Column: span.StartLinePosition.Character + 1,
                        EndLine: span.EndLinePosition.Line + 1,
                        EndColumn: span.EndLinePosition.Character + 1
                    ));
                }
            }

            base.VisitCatchClause(node);
        }
    }
}
