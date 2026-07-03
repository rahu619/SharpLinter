using FluentAssertions;
using Xunit;
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Tests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void Presets_ShouldReturnSensibleDefaults()
    {
        // Act
        var recommended = Presets.GetPreset("recommended");
        var strict = Presets.GetPreset("strict");
        var minimal = Presets.GetPreset("minimal");

        // Assert
        recommended.Preset.Should().Be("recommended");
        recommended.Rules["SL1001"].Severity.Should().Be(LintSeverity.Warning);

        strict.Preset.Should().Be("strict");
        strict.Rules["SL1001"].Severity.Should().Be(LintSeverity.Error);

        minimal.Preset.Should().Be("minimal");
    }

    [Fact]
    public void LintConfiguration_ShouldGetEffectiveSeverity()
    {
        // Arrange
        var config = new LintConfiguration();
        config.Rules["SL1001"] = new RuleOverride { Severity = LintSeverity.Error };

        // Act & Assert
        config.GetEffectiveSeverity("SL1001", LintSeverity.Warning).Should().Be(LintSeverity.Error);
        config.GetEffectiveSeverity("SL1002", LintSeverity.Warning).Should().Be(LintSeverity.Warning);
    }

    [Fact]
    public void LintConfiguration_ShouldGetRuleOption()
    {
        // Arrange
        var config = new LintConfiguration();
        config.Rules["SL1004"] = new RuleOverride
        {
            Options = new Dictionary<string, object> { { "maxLines", 42 } }
        };

        // Act
        var maxLines = config.GetRuleOption("SL1004", "maxLines", 50);
        var fallback = config.GetRuleOption("SL1004", "nonExistent", 100);

        // Assert
        maxLines.Should().Be(42);
        fallback.Should().Be(100);
    }
}
