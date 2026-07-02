using System.Text.Json;
using SharpLinter.Core.Rules;

namespace SharpLinter.Core.Providers;

/// <summary>
/// Manages a local JSON cache of fetched rules for offline use.
/// Cache is stored at ~/.sharplinter/rules-cache/rules.json.
/// </summary>
public sealed class RuleCacheManager
{
    private readonly string _cachePath;
    private readonly int _expiryDays;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public RuleCacheManager(string cachePath, int expiryDays = 30)
    {
        _cachePath = cachePath;
        _expiryDays = expiryDays;
    }

    /// <summary>
    /// Gets the path to the cache file.
    /// </summary>
    public string CacheFilePath => Path.Combine(_cachePath, "rules.json");

    /// <summary>
    /// Returns true if the cache exists and has not expired.
    /// </summary>
    public bool IsCacheValid()
    {
        if (!File.Exists(CacheFilePath)) return false;

        var cacheInfo = new FileInfo(CacheFilePath);
        return (DateTime.UtcNow - cacheInfo.LastWriteTimeUtc).TotalDays < _expiryDays;
    }

    /// <summary>
    /// Saves rules to the local cache.
    /// </summary>
    public async Task SaveToCacheAsync(IReadOnlyList<RuleMetadata> rules, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_cachePath);

        var cacheData = new RuleCacheData
        {
            CachedAt = DateTime.UtcNow,
            Rules = rules.ToList()
        };

        var json = JsonSerializer.Serialize(cacheData, JsonOptions);
        await File.WriteAllTextAsync(CacheFilePath, json, ct);
    }

    /// <summary>
    /// Loads rules from the local cache.
    /// Returns null if cache doesn't exist or is invalid JSON.
    /// </summary>
    public async Task<IReadOnlyList<RuleMetadata>?> LoadFromCacheAsync(CancellationToken ct = default)
    {
        if (!File.Exists(CacheFilePath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(CacheFilePath, ct);
            var cacheData = JsonSerializer.Deserialize<RuleCacheData>(json, JsonOptions);
            return cacheData?.Rules;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes the cache file.
    /// </summary>
    public void ClearCache()
    {
        if (File.Exists(CacheFilePath))
        {
            File.Delete(CacheFilePath);
        }
    }

    private sealed class RuleCacheData
    {
        public DateTime CachedAt { get; set; }
        public List<RuleMetadata> Rules { get; set; } = [];
    }
}
