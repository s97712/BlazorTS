using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// BlazorTS 服务注册扩展方法
/// </summary>
public static class BlazorTSServiceCollectionExtensions
{
    /// <summary>
    /// 添加 BlazorTS 服务，使用默认的命名空间解析器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBlazorTS(this IServiceCollection services)
    {
        services.AddScoped<BlazorTS.INSResolver, BlazorTS.DefaultNSResolver>();
        services.AddScoped<BlazorTS.ScriptBridge>();
        return services;
    }

    /// <summary>
    /// 添加 BlazorTS 服务，使用自定义的路径解析函数
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="customResolver">自定义路径解析函数</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBlazorTS(
        this IServiceCollection services,
        Func<Type, string, string> customResolver)
    {
        services.AddScoped<BlazorTS.INSResolver>(_ => new BlazorTS.DefaultNSResolver(customResolver));
        services.AddScoped<BlazorTS.ScriptBridge>();
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
        services.AddScoped<BlazorTS.INSResolver, TResolver>();
        services.AddScoped<BlazorTS.ScriptBridge>();
        return services;
    }
}