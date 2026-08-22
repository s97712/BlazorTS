namespace BlazorTS;

/// <summary>
/// 解析 JavaScript 模块路径。
/// </summary>
/// <param name="fullName">逻辑类型全名。</param>
/// <param name="suffix">文件后缀。</param>
/// <returns>模块路径；返回 null 表示继续调用下一条解析函数。</returns>
public delegate string? NSResolverDelegate(string fullName, string suffix);

/// <summary>
/// 按注册顺序调用解析函数并生成 JavaScript 模块路径。
/// </summary>
public sealed class NSResolver
{
    private readonly NSResolverDelegate[] _resolvers;

    /// <summary>
    /// 创建 resolver。
    /// </summary>
    /// <param name="resolvers">按注册顺序排列的解析函数。</param>
    public NSResolver(IEnumerable<NSResolverDelegate> resolvers)
    {
        _resolvers = resolvers.ToArray();
    }

    /// <summary>
    /// 解析 TypeScript 类型对应的 JavaScript 模块路径。
    /// </summary>
    /// <param name="tsType">TypeScript 类型。</param>
    /// <param name="suffix">文件后缀，例如 ".razor" 或空字符串。</param>
    /// <returns>JavaScript 模块 URL 路径。</returns>
    public string ResolveNS(Type tsType, string suffix)
    {
        return ResolveNS(tsType.FullName ?? tsType.Name, suffix);
    }

    /// <summary>
    /// 解析逻辑类型全名对应的 JavaScript 模块路径。
    /// </summary>
    /// <param name="fullName">逻辑类型全名。</param>
    /// <param name="suffix">文件后缀，例如 ".razor" 或空字符串。</param>
    /// <returns>JavaScript 模块 URL 路径。</returns>
    public string ResolveNS(string fullName, string suffix)
    {
        foreach (var resolver in _resolvers)
        {
            var path = resolver(fullName, suffix);
            if (path is not null)
            {
                return path;
            }
        }

        throw new InvalidOperationException(
            $"No resolver matched module '{fullName}' with suffix '{suffix}'.");
    }
}
