using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Formatting;

/// <summary>
/// Wraps Roslyn's code formatter with configurable options from SharpLinter configuration.
/// </summary>
public sealed class CodeFormatter
{
    /// <summary>
    /// Formats C# code according to the given configuration.
    /// </summary>
    public string Format(string code, LintConfiguration config)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var options = workspace.Options
            .WithChangedOption(CSharpFormattingOptions.IndentBlock, true)
            .WithChangedOption(CSharpFormattingOptions.IndentBraces, false)
            .WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true)
            .WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true)
            .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true)
            .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, true)
            .WithChangedOption(CSharpFormattingOptions.NewLineForClausesInQuery, true)
            .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, config.Formatting.NewLineForBraces)
            .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, config.Formatting.NewLineForBraces)
            .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, config.Formatting.NewLineForBraces)
            .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, config.Formatting.NewLineForBraces)
            .WithChangedOption(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, true)
            .WithChangedOption(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, true);

        var formatted = Formatter.Format(root, workspace, options);
        return formatted.ToFullString();
    }
}
