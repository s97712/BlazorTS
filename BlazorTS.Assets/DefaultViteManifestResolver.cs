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
    public string? Resolve(Assembly assembly)
    {
        var relativePath = GetRelativePath(assembly);
        var fileInfo = _environment.WebRootFileProvider.GetFileInfo(relativePath);
        if (fileInfo.Exists && !string.IsNullOrEmpty(fileInfo.PhysicalPath))
        {
            return fileInfo.PhysicalPath;
        }

        return null;
    }

    /// <inheritdoc />
    public string ResolveOrThrow(Assembly assembly)
    {
        var manifestPath = Resolve(assembly);
        if (manifestPath is not null)
        {
            return manifestPath;
        }

        var relativePath = GetRelativePath(assembly);
        throw new FileNotFoundException(
            $"Vite manifest file for assembly '{assembly.GetName().Name}' at '{relativePath}' not found via WebRootFileProvider.");
    }

    private static string GetRelativePath(Assembly assembly)
    {
        var applicationAssembly = Assembly.GetEntryAssembly()!;
        return assembly == applicationAssembly
            ? ".vite/manifest.json"
            : $"_content/{assembly.GetName().Name}/.vite/manifest.json";
    }
}
