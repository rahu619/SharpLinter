# SharpLinter

[![Build Status](https://img.shields.io/github/actions/workflow/status/rahu619/SharpLinter/ci.yml?branch=main)](https://github.com/rahu619/SharpLinter/actions)
[![NuGet Version](https://img.shields.io/nuget/v/SharpLinter.svg)](https://www.nuget.org/packages/SharpLinter)
[![License](https://img.shields.io/github/license/rahu619/SharpLinter.svg)](LICENSE)

SharpLinter is a free, lightweight, syntax-tree-first C# linter and formatter. It runs fully offline with zero configuration required, but is highly customizable.

Unlike heavy build-time Roslyn analyzers or expensive enterprise tools like SonarQube, SharpLinter is built for speed and flexibility. It can run in pre-commit hooks, CI/CD pipelines, or as a standalone CLI tool.

---

## Key Features

- **Lightning Fast:** Analyzes code structures using syntax-tree-only matching without full compilation overhead.
- **Pluggable & Distributable:** Available as both a reusable NuGet library (`SharpLinter`) and a dotnet CLI tool (`SharpLinter.Cli`).
- **Online/Offline Rule Sync:** Syncs the latest Microsoft Code Analysis (CA/IDE) rules from Microsoft Learn, caching them locally.
- **YAML-Based Custom Rules:** Create custom rules (regex patterns, metrics like method length/complexity, naming styles) with zero C# coding.
- **Multi-Format Output:** Get results in human-readable console colors, JSON, standard **SARIF v2.1.0**, or **MSBuild native warning format**.

---

## Installation & Setup

### For CLI usage:
```bash
dotnet tool install --global SharpLinter.Cli
```

### For programmatic C# Library usage:
Add the NuGet package dependency:
```xml
<PackageReference Include="SharpLinter" Version="1.0.0" />
```

---

## CLI Usage & Commands

### 1. Initialize Configuration
Set up the default configuration file `.sharplinter.json` and a custom rules YAML template in your project root:
```bash
sharplinter init --custom-rules
```

### 2. Analyze Code files and folders
Scan your workspace for style and code violations:
```bash
# Print colored, grouped console reports (default)
sharplinter analyze ./src

# Generate a machine-readable JSON report
sharplinter analyze ./src --format json

# Export a SARIF report for GitHub Code Scanning/Security alerts
sharplinter analyze ./src --format sarif > results.sarif
```

### 3. Automatically Format & Fix Issues
Auto-fix fixable issues (like adding missing control flow braces or stripping trailing whitespaces):
```bash
# Fix files in place
sharplinter format ./src

# Verify formatting without altering files (non-zero exit code if issues exist — perfect for pull requests)
sharplinter format ./src --check
```

### 4. Sync Rule Databases Offline
Synchronize local caches with the latest Microsoft Learn rules database:
```bash
sharplinter sync
```

---

## MSBuild & IDE Integration

You can trigger analysis automatically whenever anyone runs `dotnet build` (or builds within Visual Studio, Rider, or VS Code). Adding the following targets block to your `.csproj` automatically flags `SharpLinter` findings as native IDE compiler warnings:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- ... existing properties ... -->

  <Target Name="RunSharpLinterAnalysis" BeforeTargets="Build">
    <Message Importance="High" Text="🔍 Running SharpLinter Build-Time Analysis..." />
    <Exec Command="dotnet run --project ../../src/SharpLinter.Cli -- analyze . --no-cache --format msbuild" IgnoreExitCode="true" />
  </Target>
</Project>
```

When building, warnings will display highlighted with `[SharpLinter]` in cyan:
```bash
$ dotnet build
/Users/user/Program.cs(6,11): warning SL1005: [SharpLinter] Class 'my_bad_class' should use PascalCase [/Users/user/Project.csproj]
```

---

## Programmatic API Usage

Consume the `SharpLinter.Core` library directly in your C# code to build custom linting pipelines:

```csharp
using SharpLinter.Core.Analysis;
using SharpLinter.Core.Configuration;

// 1. Load configuration (discover .sharplinter.json automatically or load recommended defaults)
var config = Presets.GetPreset("recommended");
var engine = new LintEngine(config);

// 2. Perform static analysis on code string
string code = "class bad_name {}";
LintResult result = await engine.AnalyzeCodeAsync(code, "Temp.cs");

// 3. Inspect diagnostics
Console.WriteLine($"Found {result.Diagnostics.Count} issue(s):");
foreach (var diag in result.Diagnostics)
{
    Console.WriteLine($"[{diag.RuleId}] Line {diag.Line}: {diag.Message}");
}

// 4. Auto-formatted code output (if formatting is enabled)
string formattedCode = result.FormattedCode;
```

---

## Defining Custom Rules (`.sharplinter.rules.yaml`)

Add custom validation rules declaratively using YAML without writing C# compilation code:

```yaml
rules:
  # Example 1: Regexp pattern checks on comments
  - id: "CUSTOM001"
    title: "Avoid temporary comment markers"
    description: "HACK, TODO, or FIXME comments should be cleaned up before merging."
    category: "Maintainability"
    severity: "warning"
    type: "pattern"
    pattern:
      kind: "comment"
      match: "HACK|TODO|FIXME"

  # Example 2: Metric boundaries on method lines
  - id: "CUSTOM002"
    title: "Method is too long"
    description: "Keep methods under 40 lines to maintain clean architecture."
    category: "Maintainability"
    severity: "suggestion"
    type: "metric"
    metric:
      target: "method"
      measure: "lines"
      max: 40

  # Example 3: Private field prefix validation
  - id: "CUSTOM003"
    title: "Private fields prefix rule"
    category: "Naming"
    severity: "warning"
    type: "naming"
    naming:
      target: "field"
      pattern: "^_[a-z][a-zA-Z0-9]*$"
```

---

## Configuration Reference (`.sharplinter.json`)

Configure rule behaviors, formatting constraints, and file filters:

```json
{
  "preset": "recommended",
  "rules": {
    "SL1001": "error",
    "SL1004": { "severity": "warning", "maxLines": 60 },
    "SL1007": { "severity": "suggestion", "maxComplexity": 15 },
    "SL1010": "off"
  },
  "exclude": [
    "**/bin/**",
    "**/obj/**",
    "**/Generated/**"
  ],
  "formatting": {
    "enabled": true,
    "indentSize": 4,
    "useTabs": false,
    "newLineForBraces": true
  }
}
```

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
