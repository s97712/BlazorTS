using Microsoft.Extensions.DependencyInjection;

namespace BlazorTS;

/// <summary>
/// 创建并保存 TypeScript 互操作实例。
/// </summary>
/// <typeparam name="T">互操作类型。</typeparam>
/// <param name="services">当前作用域的服务提供者。</param>
public sealed class ScriptsAccess<T>(IServiceProvider services) where T : class
{
    /// <summary>
    /// 获取互操作实例。
    /// </summary>
    public T Value { get; } = ActivatorUtilities.CreateInstance<T>(services);
}
