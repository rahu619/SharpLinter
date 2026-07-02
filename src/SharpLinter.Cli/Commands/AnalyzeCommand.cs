using System.CommandLine;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;
using SharpLinter.Core.Output;

namespace SharpLinter.Cli.Commands;

/// <summary>
/// `sharplinter analyze` — Analyze C# files for lint violations.
/// </summary>
public static class AnalyzeCommand
{
    public static Command Create()
    {
        var pathArg = new Argument<string>("path", () => ".", "Path to a file or directory to analyze");
        var severityOpt = new Option<string>("--severity", () => "suggestion", "Minimum severity to report (suggestion, warning, error)");
        var formatOpt = new Option<string>("--format", () => "console", "Output format (console, json, sarif)");
        var configOpt = new Option<string?>("--config", "Path to .sharplinter.json config file");
        var noCacheOpt = new Option<bool>("--no-cache", "Skip rule cache, use only built-in rules");

        var command = new Command("analyze", "Analyze C# files for lint violations")
        {
            pathArg,
            severityOpt,
            formatOpt,
            configOpt,
            noCacheOpt
        };

        command.SetHandler(async (path, severity, format, configPath, noCache) =>
        {
            var config = configPath != null
                ? LintConfiguration.LoadFromFile(configPath)
                : LintConfiguration.Discover(Path.GetFullPath(path));

            if (noCache)
            {
                config.RuleSync = new RuleSyncConfig { Enabled = false };
            }

            var engine = new LintEngine(config);
            var results = new List<LintResult>();

            if (File.Exists(path))
            {
                var result = await engine.AnalyzeFileAsync(Path.GetFullPath(path));
                results.Add(result);
            }
            else if (Directory.Exists(path))
            {
                var dirResults = await engine.AnalyzeDirectoryAsync(Path.GetFullPath(path));
                results.AddRange(dirResults);
            }
            else
            {
                Console.Error.WriteLine($"Error: Path '{path}' does not exist.");
                Environment.ExitCode = 1;
                return;
            }

            // Filter by minimum severity
            var minSeverity = ParseSeverity(severity);
            var filteredResults = results.Select(r => new LintResult(
                r.Diagnostics.Where(d => d.Severity >= minSeverity).ToList(),
                r.Duration
            )).ToList();

            // Format output
            IOutputFormatter formatter = format.ToLowerInvariant() switch
            {
                "json" => new JsonOutputFormatter(),
                "sarif" => new SarifOutputFormatter(),
                "msbuild" => new MsBuildOutputFormatter(),
                _ => new ConsoleOutputFormatter()
            };

            var output = formatter.Format(filteredResults);
            Console.Write(output);

            // Set exit code
            var hasErrors = filteredResults.Any(r => r.HasErrors);
            Environment.ExitCode = hasErrors ? 1 : 0;

        }, pathArg, severityOpt, formatOpt, configOpt, noCacheOpt);

        return command;
    }

    private static LintSeverity ParseSeverity(string severity) => severity.ToLowerInvariant() switch
    {
        "error" => LintSeverity.Error,
        "warning" => LintSeverity.Warning,
        "suggestion" or "info" => LintSeverity.Suggestion,
        _ => LintSeverity.Suggestion
    };
}
