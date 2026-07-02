using SharpLinter.Core.Rules;

namespace SharpLinter.Core.Providers;

/// <summary>
/// Interface for rule sources that can provide rule metadata.
/// </summary>
public interface IRuleProvider
{
    /// <summary>
    /// Gets all rules from this provider.
    /// </summary>
    Task<IReadOnlyList<RuleMetadata>> GetRulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Whether this provider can serve rules without network access.
    /// </summary>
    bool IsAvailableOffline { get; }
}
