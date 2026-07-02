using System.Text.Json;
using System.Text.Json.Serialization;
using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Configuration;

/// <summary>
/// Represents the complete SharpLinter configuration loaded from .sharplinter.json.
/// </summary>
public sealed class LintConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>Preset name to use as a base: "recommended", "strict", or "minimal".</summary>
    public string Preset { get; set; } = "recommended";

    /// <summary>Per-rule severity overrides. Key is rule ID (e.g., "SL1001").</summary>
    public Dictionary<string, RuleOverride> Rules { get; set; } = new();

    /// <summary>Glob patterns for files/directories to exclude from analysis.</summary>
    public List<string> Exclude { get; set; } = ["**/obj/**", "**/bin/**"];

    /// <summary>Glob patterns for files to include in analysis.</summary>
    public List<string> Include { get; set; } = ["**/*.cs"];

    /// <summary>Path to custom rules YAML file (relative to config location).</summary>
    public string? CustomRulesFile { get; set; }

    /// <summary>Formatting options.</summary>
    public FormattingConfig Formatting { get; set; } = new();

    /// <summary>Rule sync/cache options.</summary>
    public RuleSyncConfig RuleSync { get; set; } = new();

    /// <summary>
    /// Gets the effective severity for a given rule, applying overrides on top of defaults.
    /// </summary>
    public LintSeverity GetEffectiveSeverity(string ruleId, LintSeverity defaultSeverity)
    {
        if (Rules.TryGetValue(ruleId, out var ruleOverride))
        {
            return ruleOverride.Severity;
        }

        return defaultSeverity;
    }

    /// <summary>
    /// Gets a rule-specific option value, falling back to a default.
    /// </summary>
    public T GetRuleOption<T>(string ruleId, string optionName, T defaultValue)
    {
        if (Rules.TryGetValue(ruleId, out var ruleOverride)
            && ruleOverride.Options.TryGetValue(optionName, out var value))
        {
            if (value is JsonElement element)
            {
                return element.Deserialize<T>(JsonOptions) ?? defaultValue;
            }

            if (value is T typedValue)
            {
                return typedValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Loads configuration from a .sharplinter.json file.
    /// Returns default configuration if file does not exist.
    /// </summary>
    public static LintConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Presets.GetPreset("recommended");
        }

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<LintConfiguration>(json, JsonOptions);
        return config ?? Presets.GetPreset("recommended");
    }

    /// <summary>
    /// Loads configuration from the given directory by searching for .sharplinter.json.
    /// Walks up parent directories if not found in the given directory.
    /// </summary>
    public static LintConfiguration Discover(string startDirectory)
    {
        var dir = new DirectoryInfo(startDirectory);
        while (dir != null)
        {
            var configPath = Path.Combine(dir.FullName, ".sharplinter.json");
            if (File.Exists(configPath))
            {
                return LoadFromFile(configPath);
            }
            dir = dir.Parent;
        }

        return Presets.GetPreset("recommended");
    }

    /// <summary>
    /// Serializes this configuration to JSON.
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions(JsonOptions)
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(this, options);
    }
}

/// <summary>
/// Formatting-specific configuration options.
/// </summary>
public sealed class FormattingConfig
{
    public bool Enabled { get; set; } = true;
    public int IndentSize { get; set; } = 4;
    public bool UseTabs { get; set; } = false;
    public bool NewLineForBraces { get; set; } = true;
}

/// <summary>
/// Configuration for rule sync (online fetching and caching).
/// </summary>
public sealed class RuleSyncConfig
{
    public bool Enabled { get; set; } = true;
    public int CacheExpiryDays { get; set; } = 30;
    public string CachePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".sharplinter", "rules-cache");
}
