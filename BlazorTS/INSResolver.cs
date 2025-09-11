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
    /// <returns>JavaScript 模块的 URL 路径</returns>
    string ResolveNS(Type tsType);
}

/// <summary>
/// 默认的命名空间解析器实现
/// </summary>
public sealed class DefaultNSResolver : INSResolver
{
    private readonly Func<Type, string> _resolveNS;

    /// <summary>
    /// 使用默认规则创建解析器：移除程序集名前缀，映射到 /js/Namespace/Foo.js
    /// </summary>
    public DefaultNSResolver() : this(static t =>
    {
        var pkgName = Assembly.GetEntryAssembly()?.GetName().Name!;
        var name = Regex.Replace(t.FullName!, $"^{pkgName}.", "").Replace(".", "/");
        return $"/js/{name}.js";
    }) {}

    /// <summary>
    /// 使用自定义解析函数创建解析器
    /// </summary>
    /// <param name="resolveNS">自定义路径解析函数</param>
    /// <exception cref="ArgumentNullException">resolveNS 为 null</exception>
    public DefaultNSResolver(Func<Type, string> resolveNS)
    {
        _resolveNS = resolveNS ?? throw new ArgumentNullException(nameof(resolveNS));
    }

    /// <inheritdoc />
    public string ResolveNS(Type tsType) => _resolveNS(tsType);
}