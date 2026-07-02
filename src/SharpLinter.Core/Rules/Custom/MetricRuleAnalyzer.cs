using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.Custom;

/// <summary>
/// Executes metric-type custom rules: quantitative thresholds on code elements.
/// Supports measuring: lines, parameters, complexity for methods, classes, and files.
/// </summary>
public sealed class MetricRuleAnalyzer : IRuleAnalyzer
{
    private readonly MetricConfig? _config;

    public RuleMetadata Metadata { get; }

    public MetricRuleAnalyzer(RuleMetadata metadata, MetricConfig? config)
    {
        Metadata = metadata;
        _config = config;
    }

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None || _config == null) return [];

        var root = tree.GetRoot();
        var diagnostics = new List<LintDiagnostic>();

        var target = _config.Target?.ToLowerInvariant();
        var measure = _config.Measure?.ToLowerInvariant();

        switch (target)
        {
            case "method":
                AnalyzeMethods(root, filePath, severity, measure, diagnostics);
                break;
            case "class":
                AnalyzeClasses(root, filePath, severity, measure, diagnostics);
                break;
            case "file":
                AnalyzeFile(tree, filePath, severity, measure, diagnostics);
                break;
        }

        return diagnostics;
    }

    private void AnalyzeMethods(SyntaxNode root, string filePath, LintSeverity severity, string? measure, List<LintDiagnostic> diagnostics)
    {
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            int value = measure switch
            {
                "lines" => GetLineCount(method.Body),
                "parameters" => method.ParameterList.Parameters.Count,
                _ => 0
            };

            if (value > _config!.Max)
            {
                var span = method.Identifier.GetLocation().GetLineSpan();
                diagnostics.Add(new LintDiagnostic(
                    RuleId: Metadata.RuleId,
                    Message: $"Method '{method.Identifier.Text}' has {measure} count of {value} (max: {_config.Max})",
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

    private void AnalyzeClasses(SyntaxNode root, string filePath, LintSeverity severity, string? measure, List<LintDiagnostic> diagnostics)
    {
        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            int value = measure switch
            {
                "lines" => GetLineCount(classDecl),
                "methods" => classDecl.Members.OfType<MethodDeclarationSyntax>().Count(),
                "fields" => classDecl.Members.OfType<FieldDeclarationSyntax>().Count(),
                _ => 0
            };

            if (value > _config!.Max)
            {
                var span = classDecl.Identifier.GetLocation().GetLineSpan();
                diagnostics.Add(new LintDiagnostic(
                    RuleId: Metadata.RuleId,
                    Message: $"Class '{classDecl.Identifier.Text}' has {measure} count of {value} (max: {_config.Max})",
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

    private void AnalyzeFile(SyntaxTree tree, string filePath, LintSeverity severity, string? measure, List<LintDiagnostic> diagnostics)
    {
        var text = tree.GetText();
        int value = measure switch
        {
            "lines" => text.Lines.Count,
            _ => 0
        };

        if (value > _config!.Max)
        {
            diagnostics.Add(new LintDiagnostic(
                RuleId: Metadata.RuleId,
                Message: $"File has {measure} count of {value} (max: {_config.Max})",
                Severity: severity,
                FilePath: filePath,
                Line: 1,
                Column: 1,
                EndLine: 1,
                EndColumn: 1
            ));
        }
    }

    private static int GetLineCount(SyntaxNode? node)
    {
        if (node == null) return 0;
        var span = node.GetLocation().GetLineSpan();
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    }
}
