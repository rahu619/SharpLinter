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

    [Fact]
    public async Task MethodLengthAnalyzer_ShouldFlagLongMethod()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run()
                {
                    // 1
                    // 2
                    // 3
                    // 4
                    // 5
                    // 6
                    // 7
                    // 8
                    // 9
                    // 10
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1004");
        // Override maxLines to 5 for testing purposes
        config.Rules["SL1004"].Options = new Dictionary<string, object> { { "maxLines", 5 } };
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1004");
    }

    [Fact]
    public async Task NamingConventionAnalyzer_ShouldFlagNonPascalCaseClass()
    {
        // Arrange
        var code = """
            class testClass {}
            """;

        var config = CreateSingleRuleConfig("SL1005");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1005" && d.Message.Contains("PascalCase"));
    }

    [Fact]
    public async Task NamingConventionAnalyzer_ShouldFlagNonCamelCaseLocal()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run()
                {
                    int MyVar = 10;
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1005");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1005" && d.Message.Contains("camelCase"));
    }

    [Fact]
    public async Task UnusedUsingAnalyzer_ShouldFlagUnusedUsing()
    {
        // Arrange
        var code = """
            using System.Text.RegularExpressions;
            class TestClass
            {
                void Run()
                {
                    System.Console.WriteLine("No regex here");
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1006");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1006" && d.Message.Contains("unused"));
    }

    [Fact]
    public async Task CyclomaticComplexityAnalyzer_ShouldFlagComplexMethod()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run(int x)
                {
                    if (x > 1) {
                        if (x > 2) {
                            if (x > 3) {
                            }
                        }
                    }
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1007");
        config.Rules["SL1007"].Options = new Dictionary<string, object> { { "maxComplexity", 2 } };
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1007");
    }

    [Fact]
    public async Task ConsistentBracePlacementAnalyzer_ShouldFlagNonAllmanBracesByDefault()
    {
        // Arrange
        var code = """
            class TestClass {
                void Run() 
                {
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1008");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1008");
    }

    [Fact]
    public async Task TrailingWhitespaceAnalyzer_ShouldFlagTrailingWhitespace()
    {
        // Arrange
        var code = "class TestClass \n{\n}\n"; // notice the space after TestClass

        var config = CreateSingleRuleConfig("SL1009");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1009");
    }

    [Fact]
    public async Task FileLengthAnalyzer_ShouldFlagLongFile()
    {
        // Arrange
        var code = "class TestClass {}\n\n\n\n\n\n\n"; // 8 lines

        var config = CreateSingleRuleConfig("SL1010");
        config.Rules["SL1010"].Options = new Dictionary<string, object> { { "maxLines", 5 } };
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1010");
    }

    [Fact]
    public async Task PatternMatchingAnalyzer_ShouldFlagOldStyleCast()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run(object x)
                {
                    if (x is string)
                    {
                        var s = (string)x;
                    }
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1011");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1011");
    }

    [Fact]
    public async Task StringConcatInLoopAnalyzer_ShouldFlagConcatenationInLoop()
    {
        // Arrange
        var code = """
            class TestClass
            {
                void Run()
                {
                    string s = "";
                    for (int i = 0; i < 10; i++)
                    {
                        s += "hello";
                    }
                }
            }
            """;

        var config = CreateSingleRuleConfig("SL1012");
        var engine = new LintEngine(config);

        // Act
        var result = await engine.AnalyzeCodeAsync(code);

        // Assert
        result.Diagnostics.Should().ContainSingle(d => d.RuleId == "SL1012");
    }
}

