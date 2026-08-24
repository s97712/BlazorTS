using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BlazorTS.Assets;

/// <summary>
/// 按程序集缓存并管理 Vite Manifest 服务实例。
/// </summary>
public sealed class ViteManifestServiceManager
{
    private readonly IViteManifestResolver _resolver;
    private readonly ConcurrentDictionary<string, ViteManifestService> _services = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 初始化 <see cref="ViteManifestServiceManager"/> 的新实例。
    /// </summary>
    /// <param name="resolver">Manifest 路径解析器。</param>
    public ViteManifestServiceManager(IViteManifestResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// 尝试根据程序集获取清单条目。
    /// </summary>
    /// <param name="key">清单条目键。</param>
    /// <param name="entry">匹配的清单条目。</param>
    /// <param name="assembly">目标程序集，为 null 时使用调用方程序集。</param>
    /// <returns>找到条目时返回 true，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryGetEntry(string key, out ViteManifestEntry entry, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        return GetService(assembly).TryGetEntry(key, out entry!);
    }

    /// <summary>
    /// 根据程序集获取清单条目，找不到时抛出异常。
    /// </summary>
    /// <param name="key">清单条目键。</param>
    /// <param name="assembly">目标程序集，为 null 时使用调用方程序集。</param>
    /// <returns>匹配的清单条目。</returns>
    /// <exception cref="KeyNotFoundException">清单中不存在指定键时抛出。</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ViteManifestEntry GetOrThrow(string key, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        if (GetService(assembly).TryGetEntry(key, out var entry))
        {
            return entry;
        }

        throw new KeyNotFoundException($"Vite manifest entry not found: {key}");
    }

    /// <summary>
    /// 根据程序集获取清单条目。
    /// </summary>
    /// <param name="key">清单条目键。</param>
    /// <param name="assembly">目标程序集，为 null 时使用调用方程序集。</param>
    /// <returns>匹配的清单条目，若不存在则为 null。</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ViteManifestEntry? GetEntry(string key, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        return GetService(assembly).TryGetEntry(key, out var entry) ? entry : null;
    }

    /// <summary>
    /// 根据程序集获取清单服务实例。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <returns>对应程序集的清单服务实例。</returns>
    public ViteManifestService GetService(Assembly assembly)
    {
        var manifestPath = _resolver.Resolve(assembly);
        return _services.GetOrAdd(manifestPath, static path => new ViteManifestService(path));
    }
}
