namespace SharpLinter.Core.Rules;

/// <summary>
/// Categories for lint rules, used for grouping and filtering.
/// </summary>
public enum RuleCategory
{
    /// <summary>Code style rules (braces, whitespace, formatting).</summary>
    Style,

    /// <summary>Naming convention rules (PascalCase, camelCase, prefixes).</summary>
    Naming,

    /// <summary>Design rules (encapsulation, error handling patterns).</summary>
    Design,

    /// <summary>Performance rules (string concatenation, allocation patterns).</summary>
    Performance,

    /// <summary>Security rules (input validation, sensitive data handling).</summary>
    Security,

    /// <summary>Maintainability rules (complexity, method length, TODOs).</summary>
    Maintainability
}
