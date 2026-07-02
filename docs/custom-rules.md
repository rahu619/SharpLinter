# SharpLinter Custom Rules Reference

Custom rules are defined in a simple YAML configuration file (by default `.sharplinter.rules.yaml` in your project root). They allow teams to enforce codebase-specific constraints, deprecate APIs, or prevent pattern anti-patterns without writing compiled Roslyn extensions.

---

## Configuration Schema

A custom rule requires:
- `id`: A unique string starting with `CUSTOM` (e.g., `CUSTOM001`).
- `title`: Short summary message shown to developers.
- `description`: Detailed explanation of the rule's rationale.
- `category`: `Style`, `Naming`, `Design`, `Performance`, `Security`, or `Maintainability`.
- `severity`: `error`, `warning`, `suggestion`, or `none`.
- `type`: The engine to run: `pattern`, `metric`, or `naming`.

---

## 1. Pattern Rules (`type: "pattern"`)

Pattern rules use regex matching against specified syntax elements.

### Supported Fields
- `kind`: The syntax element to target:
  - `comment`: Both single-line (`//`) and multi-line (`/* */`) comments.
  - `invocation`: Method invocations (e.g., `Console.WriteLine`, `Thread.Sleep`).
  - `numeric-literal`: Numeric constants.
  - `string-literal`: Hardcoded string variables.
  - `identifier`: Variable, class, or method names.
- `match`: Regex pattern to match against target text.
- `exclude`: List of values to ignore (for `numeric-literal`).
- `scope`: Set to `method-body` to restrict scanning (optional).

### Examples

**Flag temporary comment tags:**
```yaml
- id: "CUSTOM001"
  title: "Avoid HACK comments"
  category: "Maintainability"
  severity: "warning"
  type: "pattern"
  pattern:
    kind: "comment"
    match: "HACK|TODO|FIXME"
```

**Ban synchronous threading APIs:**
```yaml
- id: "CUSTOM002"
  title: "Avoid Thread.Sleep"
  category: "Performance"
  severity: "error"
  type: "pattern"
  pattern:
    kind: "invocation"
    match: "Thread\\.Sleep"
```

---

## 2. Metric Rules (`type: "metric"`)

Metric rules enforce quantitative thresholds.

### Supported Fields
- `target`: What element to count: `method`, `class`, or `file`.
- `measure`: What unit to measure:
  - `lines`: Line count of the target block.
  - `parameters`: Parameter count (for `method`).
  - `methods`: Method count (for `class`).
  - `fields`: Field count (for `class`).
- `max`: The maximum allowed value before triggering a warning.

### Examples

**Enforce maximum parameters:**
```yaml
- id: "CUSTOM003"
  title: "Too many parameters"
  category: "Design"
  severity: "warning"
  type: "metric"
  metric:
    target: "method"
    measure: "parameters"
    max: 5
```

---

## 3. Naming Rules (`type: "naming"`)

Naming rules enforce regex-based naming constraints on structural tokens.

### Supported Fields
- `target`: Target syntax element: `class`, `interface`, `method`, `property`, `field`, `parameter`, or `variable`.
- `pattern`: Regex pattern that names must match.

### Examples

**Enforce private field prefix conventions:**
```yaml
- id: "CUSTOM004"
  title: "Private fields must start with underscore"
  category: "Naming"
  severity: "warning"
  type: "naming"
  naming:
    target: "field"
    pattern: "^_[a-z][a-zA-Z0-9]*$"
```
