using FluentAssertions;
using Xunit;
using SharpLinter.Cli;
using System.IO;

namespace SharpLinter.Core.Tests;

public class CliTests
{
    [Fact]
    public async Task Cli_RulesCommand_ShouldReturnZeroExitCode()
    {
        // Arrange
        var args = new[] { "rules", "--source", "builtin" };

        // Redirect console output
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            exitCode.Should().Be(0);
            var output = sw.ToString();
            output.Should().Contain("SL1001");
            output.Should().Contain("SL1012");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task Cli_AnalyzeCommand_ShouldScanFileAndReportIssues()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".cs";
        var code = """
            class my_class {
                void Run() {
                    if (true) Console.WriteLine();
                }
            }
            """;
        await File.WriteAllTextAsync(tempFile, code);

        var args = new[] { "analyze", tempFile, "--no-cache" };

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            // Since we have a warning/error (SL1005 naming convention and SL1001 braces), exit code will depend on rules
            var output = sw.ToString();
            output.Should().Contain("SL1001"); // braces
            output.Should().Contain("SL1005"); // naming convention (my_class)
        }
        finally
        {
            Console.SetOut(originalOut);
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Cli_SyncCommand_ShouldFetchAndCacheRules()
    {
        // Arrange
        var args = new[] { "sync", "--force" };

        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            exitCode.Should().Be(0);
            var output = sw.ToString();
            output.Should().MatchRegex("(?i)(Synced|cache is up to date)");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
