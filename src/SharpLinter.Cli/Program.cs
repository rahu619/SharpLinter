using System.CommandLine;
using SharpLinter.Cli.Commands;

namespace SharpLinter.Cli;

/// <summary>
/// SharpLinter CLI entry point.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("SharpLinter — A free, customizable C# linter")
        {
            AnalyzeCommand.Create(),
            FormatCommand.Create(),
            InitCommand.Create(),
            RulesCommand.Create(),
            SyncCommand.Create()
        };

        return await rootCommand.InvokeAsync(args);
    }
}
