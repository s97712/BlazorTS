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
    /// <returns>Manifest 文件的完整物理路径。</returns>
    string Resolve(Assembly assembly);
}
