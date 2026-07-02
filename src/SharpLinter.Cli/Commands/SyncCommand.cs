using System.CommandLine;
using SharpLinter.Core.Configuration;
using SharpLinter.Core.Providers;

namespace SharpLinter.Cli.Commands;

/// <summary>
/// `sharplinter sync` — Fetch rules from online sources and cache locally.
/// </summary>
public static class SyncCommand
{
    public static Command Create()
    {
        var forceOpt = new Option<bool>("--force", "Force refresh even if cache is fresh");

        var command = new Command("sync", "Fetch latest rules from Microsoft Learn and cache locally")
        {
            forceOpt
        };

        command.SetHandler(async (force) =>
        {
            var config = LintConfiguration.Discover(Directory.GetCurrentDirectory());
            var cacheManager = new RuleCacheManager(config.RuleSync.CachePath, config.RuleSync.CacheExpiryDays);

            if (!force && cacheManager.IsCacheValid())
            {
                Console.WriteLine("✅ Rule cache is up to date.");
                Console.WriteLine($"   Cache location: {cacheManager.CacheFilePath}");
                Console.WriteLine("   Use --force to refresh anyway.");
                return;
            }

            Console.WriteLine("🔄 Fetching rules from Microsoft Learn...");

            try
            {
                var provider = new MicrosoftLearnRuleProvider();
                var rules = await provider.GetRulesAsync();

                if (rules.Count > 0)
                {
                    await cacheManager.SaveToCacheAsync(rules);
                    Console.WriteLine($"✅ Synced {rules.Count} rules to local cache.");
                    Console.WriteLine($"   Cache location: {cacheManager.CacheFilePath}");
                    Console.WriteLine($"   Expires in {config.RuleSync.CacheExpiryDays} days.");
                }
                else
                {
                    Console.WriteLine("⚠️  No rules fetched. The page structure may have changed.");
                    Console.WriteLine("   Built-in rules remain available.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Failed to fetch rules: {ex.Message}");
                Console.Error.WriteLine("   Built-in rules remain available for offline use.");
                Environment.ExitCode = 1;
            }

        }, forceOpt);

        return command;
    }
}
