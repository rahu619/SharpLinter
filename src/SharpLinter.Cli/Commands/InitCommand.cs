using System.CommandLine;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Cli.Commands;

/// <summary>
/// `sharplinter init` — Generate configuration files.
/// </summary>
public static class InitCommand
{
    public static Command Create()
    {
        var presetOpt = new Option<string>("--preset", () => "recommended", "Preset to use (recommended, strict, minimal)");
        var customRulesOpt = new Option<bool>("--custom-rules", "Also generate a .sharplinter.rules.yaml template");

        var command = new Command("init", "Generate SharpLinter configuration files")
        {
            presetOpt,
            customRulesOpt
        };

        command.SetHandler((preset, includeCustomRules) =>
        {
            // Generate .sharplinter.json
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), ".sharplinter.json");
            if (File.Exists(configPath))
            {
                Console.WriteLine($"⚠️  {configPath} already exists. Skipping.");
            }
            else
            {
                var config = Presets.GetPreset(preset);
                if (includeCustomRules)
                {
                    config.CustomRulesFile = ".sharplinter.rules.yaml";
                }
                File.WriteAllText(configPath, config.ToJson());
                Console.WriteLine($"✅ Created .sharplinter.json (preset: {preset})");
            }

            // Generate custom rules template
            if (includeCustomRules)
            {
                var rulesPath = Path.Combine(Directory.GetCurrentDirectory(), ".sharplinter.rules.yaml");
                if (File.Exists(rulesPath))
                {
                    Console.WriteLine($"⚠️  {rulesPath} already exists. Skipping.");
                }
                else
                {
                    File.WriteAllText(rulesPath, GetCustomRulesTemplate());
                    Console.WriteLine("✅ Created .sharplinter.rules.yaml (custom rules template)");
                }
            }

        }, presetOpt, customRulesOpt);

        return command;
    }

    private static string GetCustomRulesTemplate() => """
        # SharpLinter Custom Rules
        # Define your own lint rules using simple YAML syntax.
        # See: https://github.com/rahu619/SharpLinter/docs/custom-rules.md
        
        rules:
          # Example: Flag TODO/HACK/FIXME comments
          - id: "CUSTOM001"
            title: "Avoid TODO comments in production code"
            description: "TODO comments indicate incomplete work"
            category: "Maintainability"
            severity: "warning"
            type: "pattern"
            pattern:
              kind: "comment"
              match: "TODO|HACK|FIXME"
        
          # Example: Limit method length
          - id: "CUSTOM002"
            title: "Method exceeds maximum length"
            description: "Methods should not exceed the configured line count"
            category: "Maintainability"
            severity: "warning"
            type: "metric"
            metric:
              target: "method"
              measure: "lines"
              max: 40
        
          # Example: Ban specific API usage
          # - id: "CUSTOM003"
          #   title: "Avoid Thread.Sleep"
          #   description: "Use Task.Delay instead of Thread.Sleep"
          #   category: "Performance"
          #   severity: "error"
          #   type: "pattern"
          #   pattern:
          #     kind: "invocation"
          #     match: "Thread\\.Sleep"
        """;
}
