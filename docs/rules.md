# SharpLinter Built-in Rules Catalog

Here is the complete catalog of the 12 built-in rules that ship with SharpLinter. All rules run fully offline and are implemented as fast syntax-tree analyzers.

---

## Style Rules

### SL1001: Add braces to control flow statements
- **Default Severity:** Warning
- **Auto-Fixable:** Yes
- **Inspired By:** IDE0011, SA1503
- **Description:** Control flow statements (`if`, `else`, `for`, `foreach`, `while`, `do`) should always use braces, even for single-line bodies. This prevents errors during code modification.

**Non-compliant code:**
```csharp
if (condition)
    Console.WriteLine("Violating");
```

**Compliant code:**
```csharp
if (condition)
{
    Console.WriteLine("Compliant");
}
```

---

### SL1006: Potentially unused using directives
- **Default Severity:** Suggestion
- **Auto-Fixable:** Yes (via format command)
- **Inspired By:** IDE0005
- **Description:** Unused `using` directives clutter the file header. Clean them up automatically.

---

### SL1008: Consistent brace placement
- **Default Severity:** Suggestion
- **Options:** `style: "newLine" | "sameLine"`
- **Description:** Enforces consistent brace placement. Default style is Allman (opening brace on a new line).

**Allman Style (Default):**
```csharp
void Run()
{
}
```

**K&R Style:**
```csharp
void Run() {
}
```

---

### SL1009: Trailing whitespace
- **Default Severity:** Suggestion
- **Auto-Fixable:** Yes
- **Description:** Trailing whitespace at the end of lines creates messy diffs.

---

## Naming Rules

### SL1005: Naming conventions
- **Default Severity:** Warning
- **Description:** Enforces C# coding conventions:
  - Classes, Structs, Enums, Records, Interfaces, Methods, Properties: `PascalCase`
  - Local variables, Parameters: `camelCase`
  - Interfaces: Must start with prefix `I`
  - Constants: `PascalCase` or `UPPER_SNAKE_CASE`

---

## Design Rules

### SL1002: Avoid empty catch blocks
- **Default Severity:** Warning
- **Inspired By:** CA1031
- **Description:** Empty catch blocks silently swallow exceptions. A comment explaining why the exception is intentionally ignored will satisfy this rule.

**Non-compliant:**
```csharp
try {
    DoWork();
} catch (Exception ex) {
}
```

**Compliant:**
```csharp
try {
    DoWork();
} catch (Exception ex) {
    // Intentionally ignored because...
}
```

---

### SL1003: Avoid public fields
- **Default Severity:** Warning
- **Inspired By:** CA1051
- **Description:** Public fields violate encapsulation. Use properties instead. `const` and `static readonly` fields are exempt.

---

## Maintainability Rules

### SL1004: Method is too long
- **Default Severity:** Suggestion
- **Options:** `maxLines: 50`
- **Description:** Long methods are hard to read. Extract logic into smaller helpers.

---

### SL1007: Cyclomatic complexity
- **Default Severity:** Suggestion
- **Options:** `maxComplexity: 10`
- **Description:** Limits the number of decision paths (loops, branches) within a single method.

---

### SL1010: File is too long
- **Default Severity:** Suggestion
- **Options:** `maxLines: 500`
- **Description:** Files should be modular. Split large classes into smaller files.

---

## Performance Rules

### SL1011: Pattern matching suggestions
- **Default Severity:** Suggestion
- **Inspired By:** IDE0019, IDE0020
- **Description:** Prefer modern pattern matching checks over type checking and type casting.

**Non-compliant:**
```csharp
if (obj is Person)
{
    var p = (Person)obj;
}
```

**Compliant:**
```csharp
if (obj is Person p)
{
}
```

---

### SL1012: Avoid string concatenation in loops
- **Default Severity:** Warning
- **Inspired By:** CA1845
- **Description:** String concatenation in a loop generates new objects on each iteration. Use `StringBuilder` for O(n) performance.

**Non-compliant:**
```csharp
string result = "";
foreach (var item in list)
{
    result += item;
}
```

**Compliant:**
```csharp
var sb = new StringBuilder();
foreach (var item in list)
{
    sb.Append(item);
}
string result = sb.ToString();
```
