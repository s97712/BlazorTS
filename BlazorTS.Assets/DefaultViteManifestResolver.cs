using System.Reflection;
using Microsoft.AspNetCore.Hosting;

namespace BlazorTS.Assets;

/// <summary>
/// 使用 IWebHostEnvironment.WebRootFileProvider 解析 Vite Manifest 文件路径。
/// </summary>
public sealed class DefaultViteManifestResolver : IViteManifestResolver
{
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// 初始化 <see cref="DefaultViteManifestResolver"/> 的新实例。
    /// </summary>
    /// <param name="environment">Web 宿主环境。</param>
    public DefaultViteManifestResolver(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public string Resolve(Assembly assembly)
    {
        var applicationAssembly = Assembly.GetEntryAssembly()!;
        var relativePath = assembly == applicationAssembly
            ? ".vite/manifest.json"
            : $"_content/{assembly.GetName().Name}/.vite/manifest.json";

        var fileInfo = _environment.WebRootFileProvider.GetFileInfo(relativePath);
        if (fileInfo.Exists && !string.IsNullOrEmpty(fileInfo.PhysicalPath))
        {
            return fileInfo.PhysicalPath;
        }

        throw new FileNotFoundException($"Vite manifest file '{relativePath}' not found via WebRootFileProvider.");
    }
}
