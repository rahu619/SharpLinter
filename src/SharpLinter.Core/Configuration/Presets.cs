using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Configuration;

/// <summary>
/// Built-in configuration presets that provide sensible defaults.
/// </summary>
public static class Presets
{
    /// <summary>
    /// Gets a preset configuration by name.
    /// </summary>
    /// <param name="name">Preset name: "recommended", "strict", or "minimal".</param>
    /// <returns>A pre-configured <see cref="LintConfiguration"/>.</returns>
    public static LintConfiguration GetPreset(string name) => name.ToLowerInvariant() switch
    {
        "strict" => CreateStrictPreset(),
        "minimal" => CreateMinimalPreset(),
        _ => CreateRecommendedPreset()
    };

    private static LintConfiguration CreateRecommendedPreset() => new()
    {
        Preset = "recommended",
        Rules = new Dictionary<string, RuleOverride>
        {
            ["SL1001"] = new() { Severity = LintSeverity.Warning },
            ["SL1002"] = new() { Severity = LintSeverity.Warning },
            ["SL1003"] = new() { Severity = LintSeverity.Warning },
            ["SL1004"] = new() { Severity = LintSeverity.Suggestion, Options = new() { ["maxLines"] = 50 } },
            ["SL1005"] = new() { Severity = LintSeverity.Warning },
            ["SL1006"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1007"] = new() { Severity = LintSeverity.Suggestion, Options = new() { ["maxComplexity"] = 10 } },
            ["SL1008"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1009"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1010"] = new() { Severity = LintSeverity.Suggestion, Options = new() { ["maxLines"] = 500 } },
            ["SL1011"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1012"] = new() { Severity = LintSeverity.Warning }
        }
    };

    private static LintConfiguration CreateStrictPreset() => new()
    {
        Preset = "strict",
        Rules = new Dictionary<string, RuleOverride>
        {
            ["SL1001"] = new() { Severity = LintSeverity.Error },
            ["SL1002"] = new() { Severity = LintSeverity.Error },
            ["SL1003"] = new() { Severity = LintSeverity.Error },
            ["SL1004"] = new() { Severity = LintSeverity.Warning, Options = new() { ["maxLines"] = 30 } },
            ["SL1005"] = new() { Severity = LintSeverity.Error },
            ["SL1006"] = new() { Severity = LintSeverity.Warning },
            ["SL1007"] = new() { Severity = LintSeverity.Warning, Options = new() { ["maxComplexity"] = 8 } },
            ["SL1008"] = new() { Severity = LintSeverity.Warning },
            ["SL1009"] = new() { Severity = LintSeverity.Warning },
            ["SL1010"] = new() { Severity = LintSeverity.Warning, Options = new() { ["maxLines"] = 300 } },
            ["SL1011"] = new() { Severity = LintSeverity.Warning },
            ["SL1012"] = new() { Severity = LintSeverity.Error }
        }
    };

    private static LintConfiguration CreateMinimalPreset() => new()
    {
        Preset = "minimal",
        Rules = new Dictionary<string, RuleOverride>
        {
            ["SL1001"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1002"] = new() { Severity = LintSeverity.Warning },
            ["SL1003"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1004"] = new() { Severity = LintSeverity.None },
            ["SL1005"] = new() { Severity = LintSeverity.Suggestion },
            ["SL1006"] = new() { Severity = LintSeverity.None },
            ["SL1007"] = new() { Severity = LintSeverity.None },
            ["SL1008"] = new() { Severity = LintSeverity.None },
            ["SL1009"] = new() { Severity = LintSeverity.None },
            ["SL1010"] = new() { Severity = LintSeverity.None },
            ["SL1011"] = new() { Severity = LintSeverity.None },
            ["SL1012"] = new() { Severity = LintSeverity.Suggestion }
        }
    };
}
