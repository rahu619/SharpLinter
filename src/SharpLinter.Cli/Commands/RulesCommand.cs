using System.CommandLine;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;
using SharpLinter.Core.Providers;

namespace SharpLinter.Cli.Commands;

/// <summary>
/// `sharplinter rules` — List all available rules.
/// </summary>
public static class RulesCommand
{
    public static Command Create()
    {
        var categoryOpt = new Option<string?>("--category", "Filter by category (style, naming, design, performance, security, maintainability)");
        var sourceOpt = new Option<string?>("--source", "Filter by source (builtin, microsoft-learn, custom)");

        var command = new Command("rules", "List all available lint rules")
        {
            categoryOpt,
            sourceOpt
        };

        command.SetHandler(async (category, source) =>
        {
            var config = LintConfiguration.Discover(Directory.GetCurrentDirectory());
            var engine = new LintEngine(config);
            await engine.InitializeAsync();

            var analyzers = engine.GetLoadedAnalyzers();

            // Also load cached/bundled provider rules for display
            var cacheManager = new RuleCacheManager(config.RuleSync.CachePath, config.RuleSync.CacheExpiryDays);
            var cachedRules = await cacheManager.LoadFromCacheAsync();
            cachedRules ??= await new BundledRuleCatalog().GetRulesAsync();

            // Combine built-in analyzer metadata with provider rules
            var allRules = analyzers.Select(a => a.Metadata)
                .Concat(cachedRules.Where(cr => !analyzers.Any(a => a.Metadata.RuleId == cr.RuleId)))
                .ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(category))
            {
                allRules = allRules.Where(r =>
                    r.Category.ToString().Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(source))
            {
                allRules = allRules.Where(r =>
                    r.Source.Equals(source, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Display
            Console.WriteLine($"\n  {"Rule ID",-12} {"Category",-16} {"Severity",-12} {"Source",-16} Title");
            Console.WriteLine($"  {new string('─', 90)}");

            foreach (var rule in allRules.OrderBy(r => r.RuleId))
            {
                var severity = config.GetEffectiveSeverity(rule.RuleId, rule.DefaultSeverity);
                var severityIcon = severity switch
                {
                    LintSeverity.Error => "✖",
                    LintSeverity.Warning => "⚠",
                    LintSeverity.Suggestion => "ℹ",
                    _ => "○"
                };

                Console.WriteLine($"  {rule.RuleId,-12} {rule.Category,-16} {severityIcon} {severity.ToString().ToLower(),-10} {rule.Source,-16} {rule.Title}");
            }

            Console.WriteLine($"\n  Total: {allRules.Count} rule(s)");
            Console.WriteLine($"  Built-in: {allRules.Count(r => r.Source == "builtin")} | Fetched: {allRules.Count(r => r.Source == "microsoft-learn")} | Custom: {allRules.Count(r => r.Source == "custom")}");

        }, categoryOpt, sourceOpt);

        return command;
    }
}
