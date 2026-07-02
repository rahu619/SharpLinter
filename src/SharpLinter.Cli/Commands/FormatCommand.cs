using System.CommandLine;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Cli.Commands;

/// <summary>
/// `sharplinter format` — Format C# files according to configuration.
/// </summary>
public static class FormatCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", () => ".", "Path to a file or directory to format");
        var dryRunOpt = new Option<bool>("--dry-run", "Show what would be changed without modifying files");
        var checkOpt = new Option<bool>("--check", "Exit with code 1 if any files need formatting (CI mode)");
        var configOpt = new Option<string?>("--config", "Path to .sharplinter.json config file");

        var command = new Command("format", "Format C# files according to configuration")
        {
            pathArg,
            dryRunOpt,
            checkOpt,
            configOpt
        };

        command.SetHandler(async (path, dryRun, check, configPath) =>
        {
            var config = configPath != null
                ? LintConfiguration.LoadFromFile(configPath)
                : LintConfiguration.Discover(Path.GetFullPath(path));

            var engine = new LintEngine(config);
            var fullPath = Path.GetFullPath(path);
            var files = new List<string>();

            if (File.Exists(fullPath))
            {
                files.Add(fullPath);
            }
            else if (Directory.Exists(fullPath))
            {
                files.AddRange(Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/") && !f.Contains("\\obj\\") && !f.Contains("\\bin\\")));
            }
            else
            {
                Console.Error.WriteLine($"Error: Path '{path}' does not exist.");
                Environment.ExitCode = 1;
                return;
            }

            int changedCount = 0;

            foreach (var file in files)
            {
                var original = await File.ReadAllTextAsync(file);
                var formatted = engine.FormatCode(original);

                if (original != formatted)
                {
                    changedCount++;
                    var relative = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);

                    if (dryRun || check)
                    {
                        Console.WriteLine($"  ⚡ {relative} (needs formatting)");
                    }
                    else
                    {
                        await File.WriteAllTextAsync(file, formatted);
                        Console.WriteLine($"  ✅ {relative} (formatted)");
                    }
                }
            }

            if (changedCount == 0)
            {
                Console.WriteLine("✅ All files are properly formatted.");
            }
            else if (dryRun)
            {
                Console.WriteLine($"\n  {changedCount} file(s) would be changed.");
            }
            else if (check)
            {
                Console.WriteLine($"\n  {changedCount} file(s) need formatting.");
                Environment.ExitCode = 1;
            }
            else
            {
                Console.WriteLine($"\n  {changedCount} file(s) formatted.");
            }

        }, pathArg, dryRunOpt, checkOpt, configOpt);

        return command;
    }
}
