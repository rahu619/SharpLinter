using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.Custom;

/// <summary>
/// Executes pattern-type custom rules: regex matching against syntax elements.
/// Supports matching against comments, identifiers, invocations, numeric literals, and string literals.
/// </summary>
public sealed class PatternRuleAnalyzer : IRuleAnalyzer
{
    private readonly PatternConfig? _config;
    private readonly Regex? _regex;

    public RuleMetadata Metadata { get; }

    public PatternRuleAnalyzer(RuleMetadata metadata, PatternConfig? config)
    {
        Metadata = metadata;
        _config = config;
        _regex = config?.Match != null
            ? new Regex(config.Match, RegexOptions.Compiled | RegexOptions.IgnoreCase)
            : null;
    }

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None || _config == null) return [];

        var root = tree.GetRoot();
        var diagnostics = new List<LintDiagnostic>();

        var kind = _config.Kind?.ToLowerInvariant();

        switch (kind)
        {
            case "comment":
                AnalyzeComments(root, filePath, severity, diagnostics);
                break;
            case "invocation":
                AnalyzeInvocations(root, filePath, severity, diagnostics);
                break;
            case "numeric-literal":
                AnalyzeNumericLiterals(root, filePath, severity, diagnostics);
                break;
            case "string-literal":
                AnalyzeStringLiterals(root, filePath, severity, diagnostics);
                break;
            case "identifier":
                AnalyzeIdentifiers(root, filePath, severity, diagnostics);
                break;
            default:
                // Generic: search all tokens for the pattern
                AnalyzeAllTokens(root, filePath, severity, diagnostics);
                break;
        }

        return diagnostics;
    }

    private void AnalyzeComments(SyntaxNode root, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        foreach (var trivia in root.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                var text = trivia.ToString();
                if (_regex?.IsMatch(text) == true)
                {
                    AddDiagnostic(trivia.GetLocation(), filePath, severity, diagnostics);
                }
            }
        }
    }

    private void AnalyzeInvocations(SyntaxNode root, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var text = invocation.Expression.ToString();
            if (_regex?.IsMatch(text) == true)
            {
                AddDiagnostic(invocation.GetLocation(), filePath, severity, diagnostics);
            }
        }
    }

    private void AnalyzeNumericLiterals(SyntaxNode root, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        var excludeSet = _config?.Exclude?.Select(e => e.ToString()).ToHashSet() ?? [];

        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                var text = literal.Token.ValueText;
                if (!excludeSet.Contains(text))
                {
                    // Only check within method bodies if scope is method-body
                    if (_config?.Scope == "method-body")
                    {
                        var inMethod = literal.Ancestors().Any(a =>
                            a is MethodDeclarationSyntax || a is ConstructorDeclarationSyntax);
                        if (!inMethod) continue;
                    }

                    AddDiagnostic(literal.GetLocation(), filePath, severity, diagnostics);
                }
            }
        }
    }

    private void AnalyzeStringLiterals(SyntaxNode root, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var text = literal.Token.ValueText;
                if (_regex?.IsMatch(text) == true)
                {
                    AddDiagnostic(literal.GetLocation(), filePath, severity, diagnostics);
                }
            }
        }
    }

    private void AnalyzeIdentifiers(SyntaxNode root, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        foreach (var token in root.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)))
        {
            if (_regex?.IsMatch(token.Text) == true)
            {
                AddDiagnostic(token.GetLocation(), filePath, severity, diagnostics);
            }
        }
    }

    private void AnalyzeAllTokens(SyntaxNode root, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        var fullText = root.ToFullString();
        if (_regex == null) return;

        foreach (Match match in _regex.Matches(fullText))
        {
            var position = root.SyntaxTree.GetText().Lines.GetLinePosition(match.Index);
            diagnostics.Add(new LintDiagnostic(
                RuleId: Metadata.RuleId,
                Message: Metadata.Title,
                Severity: severity,
                FilePath: filePath,
                Line: position.Line + 1,
                Column: position.Character + 1,
                EndLine: position.Line + 1,
                EndColumn: position.Character + match.Length + 1
            ));
        }
    }

    private void AddDiagnostic(Location location, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        var span = location.GetLineSpan();
        diagnostics.Add(new LintDiagnostic(
            RuleId: Metadata.RuleId,
            Message: Metadata.Title,
            Severity: severity,
            FilePath: filePath,
            Line: span.StartLinePosition.Line + 1,
            Column: span.StartLinePosition.Character + 1,
            EndLine: span.EndLinePosition.Line + 1,
            EndColumn: span.EndLinePosition.Character + 1
        ));
    }
}
