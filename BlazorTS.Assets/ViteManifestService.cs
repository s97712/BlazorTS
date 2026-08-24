using System.Text.Json;

namespace BlazorTS.Assets;

/// <summary>
/// Vite Manifest 清单读取与解析服务。
/// </summary>
public class ViteManifestService
{
    private readonly Lazy<Dictionary<string, ViteManifestEntry>> _manifestCache;

    /// <summary>
    /// 初始化 <see cref="ViteManifestService"/> 的新实例。
    /// </summary>
    /// <param name="manifestFilePath">Vite manifest.json 文件的完整物理路径。</param>
    public ViteManifestService(string manifestFilePath)
    {
        _manifestCache = new Lazy<Dictionary<string, ViteManifestEntry>>(() => LoadManifest(manifestFilePath));
    }

    /// <summary>
    /// 尝试根据键获取清单条目。
    /// </summary>
    /// <param name="key">清单条目键。</param>
    /// <param name="entry">匹配的清单条目。</param>
    /// <returns>找到条目时返回 true，否则返回 false。</returns>
    public bool TryGetEntry(string key, out ViteManifestEntry entry)
    {
        return _manifestCache.Value.TryGetValue(key, out entry!);
    }

    private static Dictionary<string, ViteManifestEntry> LoadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Vite manifest file not found: {manifestPath}");
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var dictionary = JsonSerializer.Deserialize<Dictionary<string, ViteManifestEntry>>(manifestJson, deserializeOptions);
        return dictionary != null
            ? new Dictionary<string, ViteManifestEntry>(dictionary, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, ViteManifestEntry>(StringComparer.OrdinalIgnoreCase);
    }
}
