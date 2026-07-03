using FluentAssertions;
using Xunit;
using Microsoft.CodeAnalysis.CSharp;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;
using SharpLinter.Core.Rules;
using SharpLinter.Core.Rules.Custom;

namespace SharpLinter.Core.Tests.Custom;

public class CustomRuleTests
{
    [Fact]
    public void CustomRuleLoader_ShouldLoadRulesFromYaml()
    {
        // Arrange
        var yaml = """
            rules:
              - id: "CUSTOM001"
                title: "Avoid TODOs"
                description: "Clean up TODO comments"
                category: "Maintainability"
                severity: "warning"
                type: "pattern"
                pattern:
                  kind: "comment"
                  match: "TODO"
            """;

        // Act
        var rules = CustomRuleLoader.LoadFromYaml(yaml);

        // Assert
        rules.Should().ContainSingle();
        rules[0].Metadata.RuleId.Should().Be("CUSTOM001");
        rules[0].Metadata.Category.Should().Be(RuleCategory.Maintainability);
        rules[0].Metadata.DefaultSeverity.Should().Be(LintSeverity.Warning);
    }

    [Fact]
    public void MetricRuleAnalyzer_ShouldFlagTooManyParameters()
    {
        // Arrange
        var metadata = new RuleMetadata("METRIC001", "Too many parameters", "Desc", RuleCategory.Design, LintSeverity.Warning, "custom", true);
        var config = new MetricConfig { Target = "method", Measure = "parameters", Max = 2 };
        var analyzer = new MetricRuleAnalyzer(metadata, config);

        var code = """
            class TestClass
            {
                void Run(int a, int b, int c) {}
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var lintConfig = new LintConfiguration();

        // Act
        var diagnostics = analyzer.Analyze(tree, "test.cs", lintConfig);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("METRIC001");
        diagnostics[0].Message.Should().Contain("parameters");
    }

    [Fact]
    public void NamingRuleAnalyzer_ShouldFlagFieldNaming()
    {
        // Arrange
        var metadata = new RuleMetadata("NAMING001", "Private field prefix", "Desc", RuleCategory.Naming, LintSeverity.Warning, "custom", true);
        var config = new NamingConfig { Target = "field", Pattern = "^_[a-z]" };
        var analyzer = new NamingRuleAnalyzer(metadata, config);

        var code = """
            class TestClass
            {
                private int myField;
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var lintConfig = new LintConfiguration();

        // Act
        var diagnostics = analyzer.Analyze(tree, "test.cs", lintConfig);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("NAMING001");
    }

    [Fact]
    public void PatternRuleAnalyzer_ShouldFlagInvocations()
    {
        // Arrange
        var metadata = new RuleMetadata("PATTERN001", "Avoid Console.Write", "Desc", RuleCategory.Maintainability, LintSeverity.Warning, "custom", true);
        var config = new PatternConfig { Kind = "invocation", Match = "Console\\.Write" };
        var analyzer = new PatternRuleAnalyzer(metadata, config);

        var code = """
            class TestClass
            {
                void Run()
                {
                    System.Console.Write("hello");
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var lintConfig = new LintConfiguration();

        // Act
        var diagnostics = analyzer.Analyze(tree, "test.cs", lintConfig);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("PATTERN001");
    }
}
