using SharpLinter.Core.Analysis;

namespace SharpLinter.Core.Output;

/// <summary>
/// Interface for formatting lint results into human/machine-readable output.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Formats a single lint result into a string.
    /// </summary>
    string Format(LintResult result);

    /// <summary>
    /// Formats multiple lint results into a string.
    /// </summary>
    string Format(IReadOnlyList<LintResult> results);
}
