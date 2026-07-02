using System.Text.Json;
using System.Text.Json.Serialization;
using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Output;

/// <summary>
/// Formats lint results as SARIF v2.1.0 (Static Analysis Results Interchange Format).
/// Compatible with GitHub Code Scanning, Azure DevOps, and other CI tools.
/// </summary>
public sealed class SarifOutputFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Format(LintResult result) => Format([result]);

    public string Format(IReadOnlyList<LintResult> results)
    {
        var allDiagnostics = results.SelectMany(r => r.Diagnostics).ToList();

        var sarif = new SarifLog
        {
            Schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/main/sarif-2.1/schema/sarif-schema-2.1.0.json",
            Version = "2.1.0",
            Runs =
            [
                new SarifRun
                {
                    Tool = new SarifTool
                    {
                        Driver = new SarifDriver
                        {
                            Name = "SharpLinter",
                            Version = "1.0.0",
                            InformationUri = "https://github.com/rahu619/SharpLinter",
                            Rules = allDiagnostics
                                .Select(d => d.RuleId)
                                .Distinct()
                                .Select(id => new SarifRule
                                {
                                    Id = id,
                                    ShortDescription = new SarifMessage
                                    {
                                        Text = allDiagnostics.First(d => d.RuleId == id).Message
                                    }
                                })
                                .ToList()
                        }
                    },
                    Results = allDiagnostics.Select(d => new SarifResult
                    {
                        RuleId = d.RuleId,
                        Level = MapSeverity(d.Severity),
                        Message = new SarifMessage { Text = d.Message },
                        Locations =
                        [
                            new SarifLocation
                            {
                                PhysicalLocation = new SarifPhysicalLocation
                                {
                                    ArtifactLocation = new SarifArtifactLocation
                                    {
                                        Uri = d.FilePath.Replace('\\', '/')
                                    },
                                    Region = new SarifRegion
                                    {
                                        StartLine = d.Line,
                                        StartColumn = d.Column,
                                        EndLine = d.EndLine,
                                        EndColumn = d.EndColumn
                                    }
                                }
                            }
                        ]
                    }).ToList()
                }
            ]
        };

        return JsonSerializer.Serialize(sarif, JsonOptions);
    }

    private static string MapSeverity(LintSeverity severity) => severity switch
    {
        LintSeverity.Error => "error",
        LintSeverity.Warning => "warning",
        LintSeverity.Suggestion => "note",
        _ => "none"
    };

    // SARIF v2.1.0 data model (minimal)
    private sealed class SarifLog
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }
        public string? Version { get; set; }
        public List<SarifRun>? Runs { get; set; }
    }

    private sealed class SarifRun
    {
        public SarifTool? Tool { get; set; }
        public List<SarifResult>? Results { get; set; }
    }

    private sealed class SarifTool
    {
        public SarifDriver? Driver { get; set; }
    }

    private sealed class SarifDriver
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? InformationUri { get; set; }
        public List<SarifRule>? Rules { get; set; }
    }

    private sealed class SarifRule
    {
        public string? Id { get; set; }
        public SarifMessage? ShortDescription { get; set; }
    }

    private sealed class SarifResult
    {
        public string? RuleId { get; set; }
        public string? Level { get; set; }
        public SarifMessage? Message { get; set; }
        public List<SarifLocation>? Locations { get; set; }
    }

    private sealed class SarifMessage
    {
        public string? Text { get; set; }
    }

    private sealed class SarifLocation
    {
        public SarifPhysicalLocation? PhysicalLocation { get; set; }
    }

    private sealed class SarifPhysicalLocation
    {
        public SarifArtifactLocation? ArtifactLocation { get; set; }
        public SarifRegion? Region { get; set; }
    }

    private sealed class SarifArtifactLocation
    {
        public string? Uri { get; set; }
    }

    private sealed class SarifRegion
    {
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
    }
}
