using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Rules.Custom;

/// <summary>
/// Loads custom rule definitions from a .sharplinter.rules.yaml file.
/// </summary>
public static class CustomRuleLoader
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Loads custom rules from the specified YAML file.
    /// Returns an empty list if the file does not exist.
    /// </summary>
    public static IReadOnlyList<IRuleAnalyzer> LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        var yaml = File.ReadAllText(filePath);
        return LoadFromYaml(yaml);
    }

    /// <summary>
    /// Loads custom rules from a YAML string.
    /// </summary>
    public static IReadOnlyList<IRuleAnalyzer> LoadFromYaml(string yaml)
    {
        var doc = YamlDeserializer.Deserialize<CustomRulesDocument>(yaml);
        if (doc?.Rules == null || doc.Rules.Count == 0)
        {
            return [];
        }

        var analyzers = new List<IRuleAnalyzer>();

        foreach (var ruleDef in doc.Rules)
        {
            var metadata = new RuleMetadata(
                RuleId: ruleDef.Id ?? throw new InvalidOperationException("Custom rule must have an 'id'"),
                Title: ruleDef.Title ?? ruleDef.Id,
                Description: ruleDef.Description ?? "",
                Category: ParseCategory(ruleDef.Category),
                DefaultSeverity: ParseSeverity(ruleDef.Severity),
                Source: "custom",
                HasAnalyzer: true
            );

            IRuleAnalyzer? analyzer = ruleDef.Type?.ToLowerInvariant() switch
            {
                "pattern" => new PatternRuleAnalyzer(metadata, ruleDef.Pattern),
                "metric" => new MetricRuleAnalyzer(metadata, ruleDef.Metric),
                "naming" => new NamingRuleAnalyzer(metadata, ruleDef.Naming),
                _ => null
            };

            if (analyzer != null)
            {
                analyzers.Add(analyzer);
            }
        }

        return analyzers;
    }

    private static RuleCategory ParseCategory(string? category) => category?.ToLowerInvariant() switch
    {
        "style" => RuleCategory.Style,
        "naming" => RuleCategory.Naming,
        "design" => RuleCategory.Design,
        "performance" => RuleCategory.Performance,
        "security" => RuleCategory.Security,
        "maintainability" => RuleCategory.Maintainability,
        _ => RuleCategory.Maintainability
    };

    private static LintSeverity ParseSeverity(string? severity) => severity?.ToLowerInvariant() switch
    {
        "error" => LintSeverity.Error,
        "warning" => LintSeverity.Warning,
        "suggestion" or "info" => LintSeverity.Suggestion,
        "none" or "off" => LintSeverity.None,
        _ => LintSeverity.Warning
    };
}

/// <summary>YAML document root.</summary>
public sealed class CustomRulesDocument
{
    public List<CustomRuleDefinition> Rules { get; set; } = [];
}

/// <summary>A single custom rule definition from YAML.</summary>
public sealed class CustomRuleDefinition
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public string? Type { get; set; }
    public PatternConfig? Pattern { get; set; }
    public MetricConfig? Metric { get; set; }
    public NamingConfig? Naming { get; set; }
}

/// <summary>Configuration for pattern-type custom rules.</summary>
public sealed class PatternConfig
{
    public string? Kind { get; set; }
    public string? Match { get; set; }
    public List<object>? Exclude { get; set; }
    public string? Scope { get; set; }
}

/// <summary>Configuration for metric-type custom rules.</summary>
public sealed class MetricConfig
{
    public string? Target { get; set; }
    public string? Measure { get; set; }
    public int Max { get; set; } = int.MaxValue;
}

/// <summary>Configuration for naming-type custom rules.</summary>
public sealed class NamingConfig
{
    public string? Target { get; set; }
    public string? Pattern { get; set; }
}
