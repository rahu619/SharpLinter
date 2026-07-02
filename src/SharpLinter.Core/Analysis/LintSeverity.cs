namespace SharpLinter.Core.Analysis;

/// <summary>
/// Represents the severity level of a lint diagnostic.
/// </summary>
public enum LintSeverity
{
    /// <summary>Rule is disabled.</summary>
    None = 0,

    /// <summary>Informational suggestion — will not cause CI failure.</summary>
    Suggestion = 1,

    /// <summary>Warning — may cause CI failure depending on configuration.</summary>
    Warning = 2,

    /// <summary>Error — will cause CI failure (non-zero exit code).</summary>
    Error = 3
}
