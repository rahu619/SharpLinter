using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1003: Public fields should be properties instead for proper encapsulation.
/// Inspired by CA1051.
/// </summary>
public sealed class SL1003_PublicFieldAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1003",
        Title: "Avoid public fields — use properties instead",
        Description: "Public instance fields break encapsulation. Use properties instead to allow validation, change notification, and binary compatibility.",
        Category: RuleCategory.Design,
        DefaultSeverity: LintSeverity.Warning,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1051"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var walker = new PublicFieldWalker(filePath, severity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class PublicFieldWalker(string filePath, LintSeverity severity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // Skip const and static readonly fields — those are fine as public
            var isConst = node.Modifiers.Any(SyntaxKind.ConstKeyword);
            var isStaticReadonly = node.Modifiers.Any(SyntaxKind.StaticKeyword)
                               && node.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);

            if (isConst || isStaticReadonly)
            {
                base.VisitFieldDeclaration(node);
                return;
            }

            var isPublic = node.Modifiers.Any(SyntaxKind.PublicKeyword);
            var isProtected = node.Modifiers.Any(SyntaxKind.ProtectedKeyword);

            if (isPublic || isProtected)
            {
                foreach (var variable in node.Declaration.Variables)
                {
                    var span = variable.Identifier.GetLocation().GetLineSpan();
                    Diagnostics.Add(new LintDiagnostic(
                        RuleId: "SL1003",
                        Message: $"Public field '{variable.Identifier.Text}' should be a property",
                        Severity: severity,
                        FilePath: filePath,
                        Line: span.StartLinePosition.Line + 1,
                        Column: span.StartLinePosition.Character + 1,
                        EndLine: span.EndLinePosition.Line + 1,
                        EndColumn: span.EndLinePosition.Character + 1
                    ));
                }
            }

            base.VisitFieldDeclaration(node);
        }
    }
}
