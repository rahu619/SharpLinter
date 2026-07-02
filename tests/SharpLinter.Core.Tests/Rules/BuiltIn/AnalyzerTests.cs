using FluentAssertions;
using Xunit;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;
using SharpLinter.Core.Rules.BuiltIn;
using SharpLinter.Core.Rules.Custom;

namespace SharpLinter.Core.Tests.Rules.BuiltIn;

public class AnalyzerTests
{
    private static LintConfiguration CreateSingleRuleConfig(string ruleId, LintSeverity severity = LintSeverity.Warning)
    {
        var config = new LintConfiguration
        {
            Preset = "none",
            Rules = new Dictionary<string, RuleOverride>()
        };

        // Disable all standard rules by default
        string[] allRuleIds = ["SL1001", "SL1002", "SL1003", "SL1004", "SL1005", "SL1006", "SL1007", "SL1008", "SL1009", "SL1010", "SL1011", "SL1012"];
        foreach (var id in allRuleIds)
        {
            config.Rules[id] = new RuleOverride { Severity = LintSeverity.None };
        }

        // Enable only the target rule
        config.Rules[ruleId] = new RuleOverride { Severity = severity };
        config.Formatting.Enabled = false; // Disable formatter to keep test output pure

        return config;
    }

    [Fact]
    public async Task AddBracesAnalyzer_ShouldFlagMissingBraces()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run(bool condition)
                {
                    if (condition)
                        System.Console.WriteLine("No braces");
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1001");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1001");
        result.Diagnostics[0].Message.Should().Contain("'if' statement should use braces");
    }

    [Fact]
    public async Task EmptyCatchBlockAnalyzer_ShouldFlagEmptyCatchWithoutComment()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run()
                {
                    try {
                        int.Parse("not a number");
                    } catch (System.Exception) {
                    }
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1002");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1002");
    }

    [Fact]
    public async Task EmptyCatchBlockAnalyzer_ShouldNotFlagEmptyCatchWithComment()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run()
                {
                    try {
                        int.Parse("not a number");
                    } catch (System.Exception) {
                        // Intentionally ignored because of test scenario
                    }
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1002");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task PublicFieldAnalyzer_ShouldFlagPublicInstanceFields()
    {
        // Arrange
        var code = """
            class TestClass
            {
                public int MyField;
                public const int MyConst = 42;
                public static readonly int MyStaticReadonly = 100;
            }
            """;

        var config = CreateSingleRuleConfig("SL1003");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1003" && d.Message.Contains("MyField"));
    }

    [Fact]
    public async Task CustomRules_ShouldParseAndExecuteCorrectly()
    {
        // Arrange
        var yaml = """
            rules:
              - id: "CUSTOM999"
                title: "Avoid HACK comments"
                description: "Clean up HACK comments before committing"
                category: "Maintainability"
                severity: "warning"
                type: "pattern"
                pattern:
                  kind: "comment"
                  match: "HACK"
            """;

        var code = """
            // HACK: this is a temporary fix
            class TestClass {}
            """;

        var customRules = CustomRuleLoader.LoadFromYaml(yaml);
        customRules.Should().ContainSingle();
        customRules[0].Metadata.RuleId.Should().Be("CUSTOM999");

        var config = Presets.GetPreset("recommended");
        config.CustomRulesFile = null;

        // Manual evaluation
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var diagnostics = customRules[0].Analyze(tree, "test.cs", config);

        // Assert
        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("CUSTOM999");
    }
}
