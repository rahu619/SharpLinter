using System.Text.Json;
using System.Text.Json.Serialization;
using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Output;

/// <summary>
/// Formats lint results as machine-readable JSON.
/// </summary>
public sealed class JsonOutputFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Format(LintResult result) => Format([result]);

    public string Format(IReadOnlyList<LintResult> results)
    {
        var output = new JsonOutput
        {
            TotalIssues = results.Sum(r => r.Diagnostics.Count),
            ErrorCount = results.Sum(r => r.ErrorCount),
            WarningCount = results.Sum(r => r.WarningCount),
            SuggestionCount = results.Sum(r => r.SuggestionCount),
            Duration = results.Aggregate(TimeSpan.Zero, (sum, r) => sum + r.Duration).ToString(),
            Diagnostics = results.SelectMany(r => r.Diagnostics).ToList()
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private sealed class JsonOutput
    {
        public int TotalIssues { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int SuggestionCount { get; set; }
        public string Duration { get; set; } = "";
        public List<LintDiagnostic> Diagnostics { get; set; } = [];
    }
}
