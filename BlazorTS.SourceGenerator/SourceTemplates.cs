namespace BlazorTS;

internal static class SourceTemplates
{
    internal static string RenderRazorWrapper(
        ComponentModel component,
        string methodsCode,
        string interfacesSection,
        string whereClauseSuffix)
    {
        return $$"""
            #nullable enable
            using BlazorTS;
            using Microsoft.AspNetCore.Components;

            namespace {{component.NamespaceName}};

            {{interfacesSection}}/// <summary>
            /// A partial class to provide access to TypeScript functions from {{component.ClassName}}.razor.ts.
            /// </summary>
            public partial class {{component.ClassName}}{{component.TypeParameterListText}}{{whereClauseSuffix}}
            {
                /// <summary>
                /// BLAZORTS-DESIGN-GUARD: 保留组件自动注入，但将互操作类型提升为顶层服务，
                /// 这样组件泛型参数不会进入 DI 服务模型。
                /// </summary>
                [Inject] public {{component.InteropClassName}} Scripts { get; set; } = null!;
            }

            /// <summary>
            /// Provides strongly-typed access to the TypeScript functions in {{component.ClassName}}.razor.ts.
            /// </summary>
            public class {{component.InteropClassName}}(ScriptBridge invoker)
            {
                private readonly string url = invoker.ResolveNS("{{component.FullName}}", ".razor");

            {{methodsCode}}
            }
            #nullable restore
            """;
    }

    internal static string RenderEntryWrapper(
        ComponentModel component,
        string methodsCode,
        string interfacesSection)
    {
        return $$"""
            #nullable enable
            using BlazorTS;

            namespace {{component.NamespaceName}};

            {{interfacesSection}}/// <summary>
            /// Provides strongly-typed access to the TypeScript functions in {{component.ClassName}}.entry.ts.
            /// This class is designed for dependency injection.
            /// </summary>
            public class {{component.ClassName}}(ScriptBridge invoker)
            {
                private readonly string url = invoker.ResolveNS(typeof({{component.FullName}}), ".entry");

            {{methodsCode}}
            }
            #nullable restore
            """;
    }

    internal static string RenderProperty(string name, string type, string required)
    {
        return $$"""
                /// <summary>
                /// Gets or sets the '{{name}}' property.
                /// </summary>
                public {{required}}{{type}} {{name}} { get; set; }
            """;
    }

    internal static string RenderInterface(string interfaceName, string properties)
    {
        return $$"""
            /// <summary>
            /// Strongly-typed model generated from TypeScript interface '{{interfaceName}}'.
            /// </summary>
            public sealed class {{interfaceName}}
            {
            {{properties}}
            }
            """;
    }

    internal static string RenderParameterDocumentation(string name, string type)
    {
        return $"        /// <param name=\"{name}\">A {type} value.</param>";
    }

    internal static string RenderVoidMethod(
        string functionName,
        string parameters,
        string arguments,
        string parameterDocumentation)
    {
        return $$"""

                /// <summary>
                /// Invokes the '{{functionName}}' TypeScript function.
                /// </summary>
            {{parameterDocumentation}}
                public async Task {{functionName}}({{parameters}})
                {
                    await invoker.InvokeAsync<object?>(url, "{{functionName}}",
                        new object?[] { {{arguments}} });
                }
            """;
    }

    internal static string RenderResultMethod(
        string functionName,
        string parameters,
        string arguments,
        string parameterDocumentation,
        string returnType)
    {
        return $$"""

                /// <summary>
                /// Invokes the '{{functionName}}' TypeScript function and returns a {{returnType}}.
                /// </summary>
            {{parameterDocumentation}}
                /// <returns>A Task that represents the asynchronous operation, containing the {{returnType}} result from the TypeScript function.</returns>
                public async Task<{{returnType}}> {{functionName}}({{parameters}})
                {
                    return await invoker.InvokeAsync<{{returnType}}>(url, "{{functionName}}",
                        new object?[] { {{arguments}} });
                }
            """;
    }

    internal static string RenderExtension(string namespaceName, string servicesRegistration)
    {
        return $$"""
            #nullable enable
            using Microsoft.Extensions.DependencyInjection;

            namespace {{namespaceName}};

            /// <summary>
            /// Registers TypeScript interop services generated for this project.
            /// </summary>
            public static class Scripts
            {
                /// <summary>
                /// Registers all TypeScript interop services generated for this project.
                /// </summary>
                public static void AddScripts(IServiceCollection services)
                {
            {{servicesRegistration}}
                }
            }
            #nullable restore
            """;
    }
}
