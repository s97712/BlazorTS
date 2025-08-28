using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;

namespace BlazorTS;

public class InvokeWrapper(IJSRuntime jsRuntime)
{
    private readonly Lazy<Task<IJSObjectReference>> _module =
        new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorTS/InvokeWrapper.js").AsTask());

    private readonly Dictionary<string, Delegate> _delegates = new();
    private DotNetObjectReference<InvokeWrapper>? _objRef;
    private DotNetObjectReference<InvokeWrapper> ObjRef => _objRef ??= DotNetObjectReference.Create(this);

    [JSInvokable]
    public object? InvokeCallback(string key, object[] args)
    {
        return _delegates[key].DynamicInvoke(args);
    }

    private object?[]? ConvertArgs(object?[]? args)
    {
        return args
            ?.Select(arg =>
            {
                if (arg is Delegate action)
                {
                    var key = Guid.NewGuid().ToString();
                    _delegates.Add(key, action);
                    return new
                    {
                        obj = ObjRef,
                        method = nameof(InvokeCallback),
                        data = key
                    };
                }
                else
                {
                    return arg;
                }
            })
            ?.ToArray();
    }

    private static readonly string PkgName = Assembly.GetEntryAssembly()?
        .GetName().Name!;
    
    public static string ResolveModule(Type module)
    {
        var name = Regex.Replace(module.FullName!, $"^{PkgName}.", "")
            !.Replace(".", "/");
        
        var path = $"/{name}.razor.js";
        return path;
    }

    // TODO rootDir move to config
    public static string ResolveNS(Type module)
    {
        var rootDir = "js";
        var name = Regex.Replace(module.FullName!, $"^{PkgName}.", "")
            !.Replace(".", "/");
        var path = $"/{rootDir}/{name}.js";
        return path;
    }

    private async ValueTask<TValue> Invoke<TValue>(string moduleName, string methodName, params object?[]? args)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<TValue>("InvokeWrapper", moduleName, methodName, ConvertArgs(args));
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string moduleName, string identifier, object?[]? args)
    {
        return Invoke<TValue>(moduleName, identifier, ConvertArgs(args));
    }
    
}