using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using OnlineLinter.Interfaces;
using OnlineLinter.SyntaxRewriters;

namespace OnlineLinter.Services
{
  public class CodeAnalysisService : ICodeAnalysisService
  {
    private readonly ILogger<ICodeAnalysisService> _logger;

    public CodeAnalysisService(ILogger<ICodeAnalysisService> logger)
    {
      this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GetFormattedCode(string codeSnippet, CancellationToken cancellationToken = default)
    {
      //TODO: Upgrade to .NET 7
      //https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception.throwifnullorempty?view=net-7.0

      if (string.IsNullOrWhiteSpace(codeSnippet))
      {
        throw new ArgumentNullException(nameof(codeSnippet));
      }

      if (!ValidateCode(codeSnippet))
      {
        throw new ArgumentException("Error parsing code snippet");
      }

      var syntaxTree = CSharpSyntaxTree.ParseText(codeSnippet);
      var root = await syntaxTree.GetRootAsync(cancellationToken);
      var rewriter = new AddBracesRewriter();
      var modifiedRoot = rewriter.Visit(root); //Applying syntax rewriters

      var workspace = new AdhocWorkspace();

      var options = workspace.Options
                             .WithChangedOption(CSharpFormattingOptions.IndentBlock, true)
                             .WithChangedOption(CSharpFormattingOptions.IndentBraces, false)
                             .WithChangedOption(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, true)
                             .WithChangedOption(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, false)
                             .WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLineForClausesInQuery, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, true)
                             .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, true)
                             ;


      var formattedSyntaxNode = Formatter.Format(modifiedRoot, workspace, options, cancellationToken);

      return formattedSyntaxNode.ToFullString();
    }

    private bool ValidateCode(string codeSnippet)
    {
      var syntaxTree = CSharpSyntaxTree.ParseText(codeSnippet);
      if (syntaxTree is null)
      {
        _logger.LogError("Error parsing syntax : {codeSnippet}", codeSnippet);
        return false;
      }

      foreach (var diagnostic in syntaxTree.GetDiagnostics())
      {
        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
          _logger.LogError("Error parsing syntax : {codeSnippet}", codeSnippet);
          return false;
        }
      }

      return true; 
    }
  }
}
