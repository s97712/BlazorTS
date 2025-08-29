using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;

namespace BlazorTS;

/// <summary>
/// Provides a bridge for interoperability between Blazor and TypeScript/JavaScript.
/// </summary>
/// <param name="jsRuntime">The JavaScript runtime environment</param>
public class ScriptBridge(IJSRuntime jsRuntime)
{
    private readonly Lazy<Task<IJSObjectReference>> _module =
        new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorTS/ScriptBridge.js").AsTask());

    private readonly Dictionary<string, Delegate> _delegates = new();
    private DotNetObjectReference<ScriptBridge>? _objRef;
    private DotNetObjectReference<ScriptBridge> ObjRef => _objRef ??= DotNetObjectReference.Create(this);

    /// <summary>
    /// JavaScript-invokable callback method that executes C# delegates.
    /// </summary>
    /// <param name="key">Unique identifier for the delegate</param>
    /// <param name="args">Arguments array passed to the delegate</param>
    /// <returns>The return value of the delegate execution</returns>
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
    
    /// <summary>
    /// Resolves the JavaScript path for a Blazor Razor component.
    /// </summary>
    /// <param name="componentType">The Blazor component type</param>
    /// <returns>The Razor component's JavaScript file path</returns>
    public static string ResolveRazorJS(Type componentType)
    {
        var name = Regex.Replace(componentType.FullName!, $"^{PkgName}.", "")
            !.Replace(".", "/");
        
        var path = $"/{name}.razor.js";
        return path;
    }

    /// <summary>
    /// Resolves the compiled JavaScript path for TypeScript modules.
    /// </summary>
    /// <param name="tsType">The TypeScript module type</param>
    /// <returns>The compiled JavaScript file path</returns>
    public static string ResolveNS(Type tsType)
    {
        return ResolveNS("js", tsType);
    }

    /// <summary>
    /// Resolves the compiled JavaScript path for TypeScript modules with custom root directory.
    /// </summary>
    /// <param name="rootDir">The root directory for compiled JavaScript files</param>
    /// <param name="tsType">The TypeScript module type</param>
    /// <returns>The compiled JavaScript file path</returns>
    public static string ResolveNS(string rootDir, Type tsType)
    {
        var name = Regex.Replace(tsType.FullName!, $"^{PkgName}.", "")
            !.Replace(".", "/");
        var path = $"/{rootDir}/{name}.js";
        return path;
    }

    private async ValueTask<TValue> Invoke<TValue>(string moduleName, string methodName, params object?[]? args)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<TValue>("ScriptBridge", moduleName, methodName, ConvertArgs(args));
    }

    /// <summary>
    /// Asynchronously invokes a method in the specified JavaScript module.
    /// </summary>
    /// <typeparam name="TValue">The return value type</typeparam>
    /// <param name="moduleName">The JavaScript module path</param>
    /// <param name="identifier">The name of the JavaScript function to invoke</param>
    /// <param name="args">The arguments array passed to the function</param>
    /// <returns>The execution result of the JavaScript function</returns>
    public ValueTask<TValue> InvokeAsync<TValue>(string moduleName, string identifier, object?[]? args)
    {
        return Invoke<TValue>(moduleName, identifier, ConvertArgs(args));
    }
    
}