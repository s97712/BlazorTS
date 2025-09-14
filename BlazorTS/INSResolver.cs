using System.Reflection;
using System.Text.RegularExpressions;

namespace BlazorTS;

/// <summary>
/// 提供自定义命名空间模块路径解析功能
/// </summary>
public interface INSResolver
{
    /// <summary>
    /// 解析 TypeScript 类型对应的 JavaScript 模块路径
    /// </summary>
    /// <param name="tsType">TypeScript 类型</param>
    /// <param name="suffix">文件后缀，如 ".razor" 或 ""</param>
    /// <returns>JavaScript 模块的 URL 路径</returns>
    string ResolveNS(Type tsType, string suffix);
}

/// <summary>
/// 默认的命名空间解析器实现
/// </summary>
public sealed class DefaultNSResolver : INSResolver
{
    private readonly Func<Type, string, string>? _customResolver;

    /// <summary>
    /// 使用默认规则创建解析器
    /// </summary>
    public DefaultNSResolver() : this(null) {}

    /// <summary>
    /// 使用自定义解析函数创建解析器
    /// </summary>
    /// <param name="customResolver">自定义路径解析函数</param>
    public DefaultNSResolver(Func<Type, string, string>? customResolver)
    {
        _customResolver = customResolver;
    }

    /// <inheritdoc />
    public string ResolveNS(Type tsType, string suffix)
    {
        if (_customResolver != null)
        {
            return _customResolver(tsType, suffix);
        }

        // 默认解析逻辑
        var pkgName = Assembly.GetEntryAssembly()?.GetName().Name!;
        var name = Regex.Replace(tsType.FullName!, $"^{pkgName}.", "").Replace(".", "/");
        return $"/js/{name}{suffix}.js";
    }
}