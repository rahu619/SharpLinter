using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.BuiltIn;

/// <summary>
/// SL1005: Naming convention enforcement.
/// - Types (classes, structs, enums, records): PascalCase
/// - Interfaces: Must start with 'I' followed by PascalCase
/// - Local variables and parameters: camelCase
/// - Constants: PascalCase or UPPER_SNAKE_CASE
/// Inspired by CA1715, SA1300, SA1302, SA1312.
/// </summary>
public sealed class SL1005_NamingConventionAnalyzer : IRuleAnalyzer
{
    private static readonly Regex PascalCaseRegex = new(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex CamelCaseRegex = new(@"^[a-z_][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex InterfacePrefixRegex = new(@"^I[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex ConstantRegex = new(@"^([A-Z][a-zA-Z0-9]*|[A-Z][A-Z0-9_]*)$", RegexOptions.Compiled);

    public RuleMetadata Metadata { get; } = new(
        RuleId: "SL1005",
        Title: "Naming convention violation",
        Description: "Enforces .NET naming conventions: PascalCase for types, camelCase for locals/parameters, I-prefix for interfaces, PascalCase or UPPER_SNAKE_CASE for constants.",
        Category: RuleCategory.Naming,
        DefaultSeverity: LintSeverity.Warning,
        Source: "builtin",
        HasAnalyzer: true,
        DocumentationUrl: "https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions"
    );

    public IReadOnlyList<LintDiagnostic> Analyze(SyntaxTree tree, string filePath, LintConfiguration config)
    {
        var severity = config.GetEffectiveSeverity(Metadata.RuleId, Metadata.DefaultSeverity);
        if (severity == LintSeverity.None) return [];

        var root = tree.GetRoot();
        var walker = new NamingWalker(filePath, severity);
        walker.Visit(root);
        return walker.Diagnostics;
    }

    private sealed class NamingWalker(string filePath, LintSeverity severity) : CSharpSyntaxWalker
    {
        public List<LintDiagnostic> Diagnostics { get; } = [];

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            CheckPascalCase(node.Identifier, "Class");
            base.VisitClassDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            CheckPascalCase(node.Identifier, "Struct");
            base.VisitStructDeclaration(node);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            CheckPascalCase(node.Identifier, "Record");
            base.VisitRecordDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            CheckPascalCase(node.Identifier, "Enum");
            base.VisitEnumDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var name = node.Identifier.Text;
            if (!InterfacePrefixRegex.IsMatch(name))
            {
                AddDiagnostic(node.Identifier, $"Interface '{name}' should start with 'I' followed by PascalCase");
            }
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            CheckPascalCase(node.Identifier, "Method");
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            CheckPascalCase(node.Identifier, "Property");
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            var name = node.Identifier.Text;
            if (!string.IsNullOrEmpty(name) && !CamelCaseRegex.IsMatch(name))
            {
                AddDiagnostic(node.Identifier, $"Parameter '{name}' should use camelCase");
            }
            base.VisitParameter(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            // Only check local variables, not fields (fields have different rules)
            if (node.Parent?.Parent is LocalDeclarationStatementSyntax)
            {
                var name = node.Identifier.Text;
                if (!CamelCaseRegex.IsMatch(name))
                {
                    AddDiagnostic(node.Identifier, $"Local variable '{name}' should use camelCase");
                }
            }

            // Check constants
            if (node.Parent?.Parent is FieldDeclarationSyntax field
                && field.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                var name = node.Identifier.Text;
                if (!ConstantRegex.IsMatch(name))
                {
                    AddDiagnostic(node.Identifier, $"Constant '{name}' should use PascalCase or UPPER_SNAKE_CASE");
                }
            }

            base.VisitVariableDeclarator(node);
        }

        private void CheckPascalCase(SyntaxToken identifier, string kind)
        {
            var name = identifier.Text;
            if (!string.IsNullOrEmpty(name) && !PascalCaseRegex.IsMatch(name))
            {
                AddDiagnostic(identifier, $"{kind} '{name}' should use PascalCase");
            }
        }

        private void AddDiagnostic(SyntaxToken identifier, string message)
        {
            var span = identifier.GetLocation().GetLineSpan();
            Diagnostics.Add(new LintDiagnostic(
                RuleId: "SL1005",
                Message: message,
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
