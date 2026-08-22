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
    string ResolveNS(Type tsType, string suffix)
    {
        return ResolveNS(tsType.FullName ?? tsType.Name, suffix);
    }

    /// <summary>
    /// 解析逻辑类型全名对应的 JavaScript 模块路径。
    /// </summary>
    /// <param name="fullName">逻辑类型全名</param>
    /// <param name="suffix">文件后缀，如 ".razor" 或 ""</param>
    /// <returns>JavaScript 模块的 URL 路径</returns>
    string ResolveNS(string fullName, string suffix);
}

/// <summary>
/// 默认的命名空间解析器实现
/// </summary>
public sealed class DefaultNSResolver : INSResolver
{
    private static readonly Regex GenericArityRegex = new(@"`\d+", RegexOptions.Compiled);
    private readonly Func<string, string, string>? _customResolver;

    /// <summary>
    /// 使用默认规则或自定义函数创建解析器
    /// </summary>
    /// <param name="customResolver">自定义路径解析函数，接收逻辑类型全名和后缀</param>
    public DefaultNSResolver(Func<string, string, string>? customResolver = null)
    {
        _customResolver = customResolver;
    }

    /// <inheritdoc />
    public string ResolveNS(string fullName, string suffix)
    {
        if (_customResolver != null)
        {
            return _customResolver(fullName, suffix);
        }

        var pkgName = Assembly.GetEntryAssembly()?.GetName().Name!;
        var name = NormalizeTypeName(fullName, pkgName);
        return $"/js/{name}{suffix}.js";
    }

    private static string NormalizeTypeName(string fullName, string pkgName)
    {
        // BLAZORTS-DESIGN-GUARD: 模块路径必须稳定映射到源文件，
        // 不能把 CLR 泛型 arity 标记（例如 `1）泄漏到最终 URL 中。
        var withoutAssemblyPrefix = Regex.Replace(fullName, $"^{Regex.Escape(pkgName)}\\.", string.Empty);
        var withoutGenericArity = GenericArityRegex.Replace(withoutAssemblyPrefix, string.Empty);
        return withoutGenericArity.Replace("+", ".").Replace(".", "/");
    }
}
