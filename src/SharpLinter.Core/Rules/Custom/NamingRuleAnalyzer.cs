using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.Custom;

/// <summary>
/// Executes naming-type custom rules: regex-based naming convention enforcement
/// on types, methods, fields, properties, and parameters.
/// </summary>
public sealed class NamingRuleAnalyzer : IRuleAnalyzer
{
    private readonly NamingConfig? _config;
    private readonly Regex? _regex;

    public RuleMetadata Metadata { get; }

    public NamingRuleAnalyzer(RuleMetadata metadata, NamingConfig? config)
    {
        Metadata = metadata;
        _config = config;
        _regex = config?.Pattern != null
            ? new Regex(config.Pattern, RegexOptions.Compiled)
            : null;
    }

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None || _config == null || _regex == null) return [];

        var root = tree.GetRoot();
        var diagnostics = new List<LintDiagnostic>();

        var target = _config.Target?.ToLowerInvariant();

        switch (target)
        {
            case "class":
                CheckNodes<ClassDeclarationSyntax>(root, n => n.Identifier, filePath, severity, diagnostics);
                break;
            case "interface":
                CheckNodes<InterfaceDeclarationSyntax>(root, n => n.Identifier, filePath, severity, diagnostics);
                break;
            case "method":
                CheckNodes<MethodDeclarationSyntax>(root, n => n.Identifier, filePath, severity, diagnostics);
                break;
            case "property":
                CheckNodes<PropertyDeclarationSyntax>(root, n => n.Identifier, filePath, severity, diagnostics);
                break;
            case "field":
                foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        CheckIdentifier(variable.Identifier, filePath, severity, diagnostics);
                    }
                }
                break;
            case "parameter":
                CheckNodes<ParameterSyntax>(root, n => n.Identifier, filePath, severity, diagnostics);
                break;
            case "variable":
                foreach (var local in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
                {
                    foreach (var variable in local.Declaration.Variables)
                    {
                        CheckIdentifier(variable.Identifier, filePath, severity, diagnostics);
                    }
                }
                break;
            default:
                // Check all identifiers
                foreach (var token in root.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)))
                {
                    CheckIdentifier(token, filePath, severity, diagnostics);
                }
                break;
        }

        return diagnostics;
    }

    private void CheckNodes<T>(SyntaxNode root, Func<T, SyntaxToken> getIdentifier,
        string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics) where T : SyntaxNode
    {
        foreach (var node in root.DescendantNodes().OfType<T>())
        {
            CheckIdentifier(getIdentifier(node), filePath, severity, diagnostics);
        }
    }

    private void CheckIdentifier(SyntaxToken identifier, string filePath, LintSeverity severity, List<LintDiagnostic> diagnostics)
    {
        var name = identifier.Text;
        if (string.IsNullOrEmpty(name)) return;

        if (!_regex!.IsMatch(name))
        {
            var span = identifier.GetLocation().GetLineSpan();
            diagnostics.Add(new LintDiagnostic(
                RuleId: Metadata.RuleId,
                Message: $"'{name}' does not match the required naming pattern: {_config!.Pattern}",
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
