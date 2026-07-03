using FluentAssertions;
using Xunit;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Output;

namespace SharpLinter.Core.Tests.Output;

public class OutputFormatterTests
{
    private static LintResult CreateSampleResult()
    {
        var diagnostics = new List<LintDiagnostic>
        {
            new LintDiagnostic(
                RuleId: "SL1001",
                Message: "if statement should use braces",
                Severity: LintSeverity.Warning,
                FilePath: "TestFile.cs",
                Line: 10,
                Column: 5,
                EndLine: 10,
                EndColumn: 20
            )
        };

        return new LintResult(diagnostics, TimeSpan.FromMilliseconds(123), "formatted code");
    }

    [Fact]
    public void JsonOutputFormatter_ShouldFormatCorrectly()
    {
        // Arrange
        var result = CreateSampleResult();
        var formatter = new JsonOutputFormatter();

        // Act
        var output = formatter.Format(result);

        // Assert
        output.Should().Contain("\"totalIssues\": 1");
        output.Should().Contain("\"warningCount\": 1");
        output.Should().Contain("\"ruleId\": \"SL1001\"");
    }

    [Fact]
    public void MsBuildOutputFormatter_ShouldFormatCorrectly()
    {
        // Arrange
        var result = CreateSampleResult();
        var formatter = new MsBuildOutputFormatter();

        // Act
        var output = formatter.Format(result);

        // Assert
        output.Should().Contain("TestFile.cs(10,5): warning SL1001:");
        output.Should().Contain("if statement should use braces");
    }

    [Fact]
    public void SarifOutputFormatter_ShouldFormatCorrectly()
    {
        // Arrange
        var result = CreateSampleResult();
        var formatter = new SarifOutputFormatter();

        // Act
        var output = formatter.Format(result);

        // Assert
        output.Should().Contain("\"version\": \"2.1.0\"");
        output.Should().Contain("\"driver\": {");
        output.Should().Contain("\"name\": \"SharpLinter\"");
        output.Should().Contain("\"rules\": [");
        output.Should().Contain("\"id\": \"SL1001\"");
    }
}
