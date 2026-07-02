using Microsoft.CodeAnalysis;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1010: Files should not exceed a configurable line count.
/// Default: 500 lines.
/// </summary>
public sealed class SL1010_FileLengthAnalyzer : IRuleAnalyzer
{
    private const int DefaultMaxLines = 500;

    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1010",
        Title: "File is too long",
        Description: "Large files are harder to navigate and maintain. Consider splitting into multiple files with focused responsibilities. Default threshold: 500 lines.",
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
        var text = tree.GetText();
        var lineCount = text.Lines.Count;

        if (lineCount > maxLines)
        {
            return
            [
                new LintDiagnostic(
                    RuleId: "SL1010",
                    Message: $"File is {lineCount} lines long (max: {maxLines})",
                    Severity: severity,
                    FilePath: filePath,
                    Line: 1,
                    Column: 1,
                    EndLine: 1,
                    EndColumn: 1
                )
            ];
        }

        return [];
    }
}
