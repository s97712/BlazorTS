using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// BlazorTS 服务注册扩展方法。
/// </summary>
public static class BlazorTSServiceCollectionExtensions
{
    /// <summary>
    /// 添加脚本服务注册委托。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">脚本服务注册委托。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddScripts(
        this IServiceCollection services,
        Action<IServiceCollection> configure)
    {
        services.AddBlazorTS();
        configure(services);
        return services;
    }

    /// <summary>
    /// 添加 BlazorTS 默认服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddBlazorTS(this IServiceCollection services)
    {
        services.TryAddScoped<BlazorTS.NSResolver>();
        services.TryAddScoped<BlazorTS.ScriptBridge>();
        return services;
    }

    /// <summary>
    /// 注册一条 JavaScript 模块路径解析函数。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="resolve">返回模块路径；返回 null 时继续调用下一条解析函数。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection AddBlazorTSResolver(
        this IServiceCollection services,
        BlazorTS.NSResolverDelegate resolve)
    {
        services.AddBlazorTS();
        services.AddScoped<BlazorTS.NSResolverDelegate>(_ => resolve);
        return services;
    }
}
