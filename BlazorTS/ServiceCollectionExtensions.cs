using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// BlazorTS 服务注册扩展方法
/// </summary>
public static class BlazorTSServiceCollectionExtensions
{
    /// <summary>
    /// 添加脚本服务注册委托。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">脚本服务注册委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddScripts(
        this IServiceCollection services,
        Action<IServiceCollection> configure)
    {
        services.AddBlazorTS();
        configure(services);
        return services;
    }

    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBlazorTS(this IServiceCollection services)
    {
        services.TryAddScoped<BlazorTS.INSResolver, BlazorTS.DefaultNSResolver>();
        services.TryAddScoped<BlazorTS.ScriptBridge>();
        return services;
    }

    /// <summary>
    /// 添加 BlazorTS 服务，使用自定义的路径解析函数
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="customResolver">自定义路径解析函数，接收逻辑类型全名和后缀</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBlazorTS(
        this IServiceCollection services,
        Func<string, string, string> customResolver)
    {
        services.Replace(ServiceDescriptor.Scoped<BlazorTS.INSResolver>(_ => new BlazorTS.DefaultNSResolver(customResolver)));
        services.TryAddScoped<BlazorTS.ScriptBridge>();
        return services;
    }

    /// <summary>
    /// 添加 BlazorTS 服务，使用自定义的解析器实现
    /// </summary>
    /// <typeparam name="TResolver">自定义解析器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBlazorTS<TResolver>(this IServiceCollection services)
        where TResolver : class, BlazorTS.INSResolver
    {
        services.Replace(ServiceDescriptor.Scoped<BlazorTS.INSResolver, TResolver>());
        services.TryAddScoped<BlazorTS.ScriptBridge>();
        return services;
    }
}