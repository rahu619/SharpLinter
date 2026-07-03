using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using SharpLinter.Core.Configuration;
using SharpLinter.Core.Formatting;
using SharpLinter.Core.Providers;
using SharpLinter.Core.Rules;
using SharpLinter.Core.Rules.BuiltIn;
using SharpLinter.Core.Rules.Custom;

namespace SharpLinter.Core.Analysis;

/// <summary>
/// The main orchestrator for SharpLinter analysis.
/// Coordinates rule discovery, configuration loading, file parsing, and diagnostic collection.
/// </summary>
public sealed class LintEngine : IDisposable
{
    private readonly LintConfiguration _config;
    private readonly List<IRuleAnalyzer> _analyzers = [];
    private readonly CodeFormatter _formatter = new();
    private bool _initialized;

    public LintEngine(LintConfiguration? config = null)
    {
        _config = config ?? Presets.GetPreset("recommended");
    }

    /// <summary>
    /// Initializes the engine by loading all rule analyzers.
    /// Called automatically on first analysis, but can be called explicitly for eager loading.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        // Tier 1: Built-in rules (always loaded)
        LoadBuiltInRules();

        // Tier 3: Custom rules (if configured)
        LoadCustomRules();

        // Tier 2: Fetched/cached rules (for awareness catalog — these don't have analyzers)
        await LoadProviderRulesAsync(ct);

        _initialized = true;
    }

    /// <summary>
    /// Analyzes a single C# code string.
    /// </summary>
    public async Task<LintResult> AnalyzeCodeAsync(string code, string filePath = "<input>", CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var stopwatch = Stopwatch.StartNew();
        var tree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = new List<LintDiagnostic>();

        foreach (var analyzer in _analyzers)
        {
            try
            {
                var results = analyzer.Analyze(tree, filePath, _config);
                diagnostics.AddRange(results);
            }
            catch (Exception)
            {
                // Individual rule failures should not break the entire analysis
            }
        }

        stopwatch.Stop();

        // Apply formatting if enabled
        string? formattedCode = null;
        if (_config.Formatting.Enabled)
        {
            try
            {
                formattedCode = _formatter.Format(tree, _config);
            }
            catch (Exception)
            {
                // Formatting failure is non-fatal
            }
        }

        return new LintResult(
            diagnostics.OrderBy(d => d.FilePath).ThenBy(d => d.Line).ThenBy(d => d.Column).ToList(),
            stopwatch.Elapsed,
            formattedCode
        );
    }

    /// <summary>
    /// Analyzes a single C# file.
    /// </summary>
    public async Task<LintResult> AnalyzeFileAsync(string filePath, CancellationToken ct = default)
    {
        var code = await File.ReadAllTextAsync(filePath, ct);
        return await AnalyzeCodeAsync(code, filePath, ct);
    }

    /// <summary>
    /// Analyzes all C# files in a directory (recursively).
    /// Respects include/exclude patterns from configuration.
    /// </summary>
    public async Task<IReadOnlyList<LintResult>> AnalyzeDirectoryAsync(string directoryPath, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var files = FileDiscovery.DiscoverFiles(directoryPath, _config);
        var results = new ConcurrentBag<LintResult>();

        await Parallel.ForEachAsync(files, ct, async (file, token) =>
        {
            var result = await AnalyzeFileAsync(file, token);
            results.Add(result);
        });

        return results.ToList();
    }

    /// <summary>
    /// Gets all loaded rule analyzers.
    /// </summary>
    public IReadOnlyList<IRuleAnalyzer> GetLoadedAnalyzers()
    {
        return _analyzers.AsReadOnly();
    }

    /// <summary>
    /// Formats a C# code string according to configuration.
    /// </summary>
    public string FormatCode(string code)
    {
        return _formatter.Format(code, _config);
    }

    private void LoadBuiltInRules()
    {
        _analyzers.AddRange(
        [
            new SL1001_AddBracesAnalyzer(),
            new SL1002_EmptyCatchBlockAnalyzer(),
            new SL1003_PublicFieldAnalyzer(),
            new SL1004_MethodLengthAnalyzer(),
            new SL1005_NamingConventionAnalyzer(),
            new SL1006_UnusedUsingAnalyzer(),
            new SL1007_CyclomaticComplexityAnalyzer(),
            new SL1008_ConsistentBracePlacementAnalyzer(),
            new SL1009_TrailingWhitespaceAnalyzer(),
            new SL1010_FileLengthAnalyzer(),
            new SL1011_PatternMatchingAnalyzer(),
            new SL1012_StringConcatInLoopAnalyzer()
        ]);
    }

    private void LoadCustomRules()
    {
        if (!string.IsNullOrEmpty(_config.CustomRulesFile))
        {
            var customAnalyzers = CustomRuleLoader.LoadFromFile(_config.CustomRulesFile);
            _analyzers.AddRange(customAnalyzers);
        }
    }

    private async Task LoadProviderRulesAsync(CancellationToken ct)
    {
        if (!_config.RuleSync.Enabled) return;

        var cacheManager = new RuleCacheManager(_config.RuleSync.CachePath, _config.RuleSync.CacheExpiryDays);

        // Try cache first
        if (cacheManager.IsCacheValid())
        {
            // Cache exists and is fresh — no need to fetch
            return;
        }

        // Try to fetch and cache (non-blocking — failure is OK)
        try
        {
            var provider = new MicrosoftLearnRuleProvider();
            var rules = await provider.GetRulesAsync(ct);
            if (rules.Count > 0)
            {
                await cacheManager.SaveToCacheAsync(rules, ct);
            }
        }
        catch (Exception)
        {
            // Network failure — continue with built-in rules only
        }
    }

    public void Dispose()
    {
        _formatter.Dispose();
    }
}
