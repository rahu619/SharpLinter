using SharpLinter.Core.Configuration;

namespace SharpLinter.Core.Analysis;

/// <summary>
/// Discovers C# files for analysis or formatting, respecting configuration exclude patterns.
/// Shared by both the LintEngine and CLI commands to avoid duplicated filtering logic.
/// </summary>
public static class FileDiscovery
{
    /// <summary>
    /// Discovers C# files to process from the given path.
    /// If the path is a single .cs file, returns it directly.
    /// If the path is a directory, recursively finds all .cs files and filters using the config's exclude patterns.
    /// </summary>
    public static IReadOnlyList<string> DiscoverFiles(string path, LintConfiguration config)
    {
        if (File.Exists(path))
        {
            return path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? [path] : [];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !ShouldExclude(f, path, config.Exclude))
            .ToList();
    }

    private static bool ShouldExclude(string filePath, string basePath, List<string> excludePatterns)
    {
        var relativePath = Path.GetRelativePath(basePath, filePath).Replace('\\', '/');

        foreach (var pattern in excludePatterns)
        {
            var normalizedPattern = pattern.Replace("**/", "");
            if (relativePath.Contains(normalizedPattern.Trim('*', '/')))
            {
                return true;
            }
        }

        return false;
    }
}
