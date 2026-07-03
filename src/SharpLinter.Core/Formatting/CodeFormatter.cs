using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Formatting;

/// <summary>
/// Wraps Roslyn's code formatter with configurable options from SharpLinter configuration.
/// Implements IDisposable to properly clean up the underlying Roslyn workspace.
/// </summary>
public sealed class CodeFormatter : IDisposable
{
    private readonly AdhocWorkspace _workspace = new();
    private bool _disposed;

    /// <summary>
    /// Formats C# code according to the given configuration.
    /// </summary>
    public string Format(string code, LintConfiguration config)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return Format(tree, config);
    }

    /// <summary>
    /// Formats a pre-parsed syntax tree according to the given configuration.
    /// Use this overload to avoid re-parsing code that has already been parsed.
    /// </summary>
    public string Format(SyntaxTree tree, LintConfiguration config)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var root = tree.GetRoot();
        var options = _workspace.Options
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

        var formatted = Formatter.Format(root, _workspace, options);
        return formatted.ToFullString();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _workspace.Dispose();
            _disposed = true;
        }
    }
}
