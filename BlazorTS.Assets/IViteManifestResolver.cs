using System.Reflection;

namespace BlazorTS.Assets;

/// <summary>
/// 根据程序集解析 Vite Manifest 文件路径。
/// </summary>
public interface IViteManifestResolver
{
    /// <summary>
    /// 解析指定程序集对应的 Manifest 文件路径。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <returns>Manifest 文件的完整物理路径；文件不存在时返回 null。</returns>
    string? Resolve(Assembly assembly);

    /// <summary>
    /// 解析指定程序集对应的 Manifest 文件路径，找不到时抛出异常。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <returns>Manifest 文件的完整物理路径。</returns>
    /// <exception cref="FileNotFoundException">Manifest 文件不存在时抛出。</exception>
    string ResolveOrThrow(Assembly assembly);
}
