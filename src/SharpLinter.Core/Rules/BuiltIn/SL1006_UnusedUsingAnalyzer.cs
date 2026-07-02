using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1006: Detect potentially unused using directives.
/// Uses a lightweight syntax-only heuristic: checks if any identifier in the file
/// matches a plausible type from the imported namespace.
/// Inspired by IDE0005.
/// </summary>
public sealed class SL1006_UnusedUsingAnalyzer : IRuleAnalyzer
{
    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1006",
        Title: "Potentially unused using directive",
        Description: "Using directives that are not referenced anywhere in the file add unnecessary clutter. Remove unused usings to keep the file clean.",
        Category: RuleCategory.Style,
        DefaultSeverity: LintSeverity.Suggestion,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0005"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var diagnostics = new List<LintDiagnostic>();

        // Get all using directives
        var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(u => u.Alias == null && u.StaticKeyword.IsKind(SyntaxKind.None))
            .ToList();

        if (usings.Count == 0) return [];

        // Collect all identifiers used in the file (excluding the usings themselves)
        var allIdentifiers = root.DescendantTokens()
            .Where(t => t.IsKind(SyntaxKind.IdentifierToken)
                     && t.Parent is not UsingDirectiveSyntax
                     && t.Parent?.Parent is not UsingDirectiveSyntax)
            .Select(t => t.Text)
            .ToHashSet();

        // Collect all text in the file to check for namespace segment usage
        var fullText = tree.GetText().ToString();

        foreach (var usingDirective in usings)
        {
            var namespaceName = usingDirective.Name?.ToString();
            if (string.IsNullOrEmpty(namespaceName)) continue;

            // Get the last segment of the namespace (most likely to appear as a qualifier)
            var lastSegment = namespaceName.Split('.').Last();

            // Check if any identifier in the file could reference something from this namespace
            // This is a heuristic — syntax-only analysis cannot be 100% accurate
            var isLikelyUsed = allIdentifiers.Contains(lastSegment)
                            || fullText.Contains(lastSegment + ".");

            if (!isLikelyUsed)
            {
                var span = usingDirective.GetLocation().GetLineSpan();
                diagnostics.Add(new LintDiagnostic(
                    RuleId: "SL1006",
                    Message: $"Using directive '{namespaceName}' appears to be unused",
                    Severity: severity,
                    FilePath: filePath,
                    Line: span.StartLinePosition.Line + 1,
                    Column: span.StartLinePosition.Character + 1,
                    EndLine: span.EndLinePosition.Line + 1,
                    EndColumn: span.EndLinePosition.Character + 1
                ));
            }
        }

        return diagnostics;
    }
}
