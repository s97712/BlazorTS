using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using MoreLinq;
using System.Reflection;
using BlazorTS.SourceGenerator;

namespace BlazorTS
{

    [Generator]
    public class ResolveGenerator : IIncrementalGenerator
    {

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {


            // 首先显示native path信息 - 作为第一个RegisterSourceOutput确保能看到
            context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider, (spc, options) =>
            {
                // 显示native path路径
                var nativePath = Helper.GetTypeScriptParserNativePath();
                Helper.Log(spc, $"TypeScript Parser Native Path: {nativePath}");

                try
                {
                    Helper.SetupNativeLibraryResolver();
                }
                catch (Exception ex)
                {
                    Helper.Log(spc, $"SetupNativeLibraryResolver failed : {ex.Message}");
                }
                
                // 显示程序集位置信息
                var assembly = Assembly.GetExecutingAssembly();
                Helper.Log(spc, $"Source Generator Assembly Location: {assembly.Location}");
                
                // 检查native库文件是否存在
                if (!string.IsNullOrEmpty(nativePath) && nativePath != "未找到")
                {
                    var exists = Directory.Exists(nativePath);
                    if (exists)
                    {
                        try
                        {
                            var files = Directory.GetFiles(nativePath, "*.*");
                            Helper.Log(spc, $"Files in native path: {string.Join(", ", files.Select(Path.GetFileName))}");
                        }
                        catch (Exception ex)
                        {
                            Helper.Log(spc, $"Error listing native path files: {ex.Message}");
                        }
                    }
                }
            });

            // 添加调试日志
            var tsFilesProvider = context
                .AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
                .Select((file, _) => file.Path);
            
            // 注册诊断输出
            context.RegisterSourceOutput(tsFilesProvider.Collect(), (spc, paths) =>
            {
                Helper.Log(spc, $"Found {paths.Length} TypeScript files");
                foreach (var path in paths)
                {
                    Helper.Log(spc, $"TypeScript file: {path}");
                }
            });

            // 真正业务逻辑
            var metaProvider = context.AnalyzerConfigOptionsProvider
                .Select((options, _) =>
                {
                    options.GlobalOptions.TryGetValue("build_property.ProjectDir", out var dir);
                    options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns);
                    return (dir, ns);
                });

            var razorJsFiles = context
                .AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
                .Combine(metaProvider);

            var razorContents = razorJsFiles
                .Select((info, cancellationToken) =>
                {
                    var (additionalText, meta) = info;

                    var content = additionalText.GetText(cancellationToken)?.ToString() ?? string.Empty;
                    var path = Path.GetRelativePath(meta.dir, additionalText.Path);

                    return (Path: Path.Join(meta.ns, path), Content: content);
                })
                .Collect();


            context.RegisterSourceOutput(razorContents, (spc, files) =>
            {
                Helper.Log(spc, $"Processing {files.Length} TypeScript files");
                
                var names = files
                    .Select(item => Generate(spc, item.Path, item.Content))
                    .ToList();

                {
                    var extensionCode = GenerateExtension(spc, names);
                    var fileName = $"BlazorTS.SourceGenerator.Extensions.ServiceCollectionExtensions.g.cs";
                    spc.AddSource(fileName, SourceText.From(extensionCode, Encoding.UTF8));
                    Helper.Log(spc, $"Generated service collection extension with {names.Count} services");
                }

            });

        }

        private static string Generate(SourceProductionContext spc, string path, string content)
        {
            var ns = string.Join(".", Path.GetDirectoryName(path)!.Split(Path.DirectorySeparatorChar));
            var className = Path.GetFileName(path)[0..^".ts".Length];
            var methods = MethodExtractor.Extract(content);

            Helper.Log(spc, $"Generating wrapper for {className} with {methods.Count()} methods");

            {
                var fileName = $"{ns}.{className}.ts.module.g.cs";
                var code = GenerateWrapper(ns, className, methods);
                spc.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
            }
            return $"{ns}.{className}";
        }

        private static string GenerateJsModuleCode(string ns, string className, IEnumerable<string> methods)
        {
            var fullName = $"{ns}.{className}";
            var code = $@"

using BlazorApp2.services.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace {ns} {{
public partial class {className}
{{

[Inject] JSInterop JS {{ get; set; }}
public class JSInterop(IJSRuntime runtime) {{
    private JsResolve JsModule = new JsResolve(runtime, JsResolve.Resolve(typeof({fullName})));

    {methods.Select(method => $@"
    public async Task<object?> {method}(object?[]? args) {{
        return await JsModule.InvokeAsync<object?>(""{method}"", args);
    }}
    ").ToDelimitedString("\n")}
    
}}
}}
}}
";

            return code;
        }

        private static string GenerateWrapper(string ns, string className, IEnumerable<TSFunction> methods)
        {
            var fullName = $"{ns}.{className}";

            var code = $@"#nullable enable
using BlazorTS;
using Microsoft.AspNetCore.Components;

namespace {ns};

public partial class {className}
{{
    [Inject] public TSInterop TypeScriptJS {{ get; set; }} = null!;

    public class TSInterop(InvokeWrapper invoker)
    {{
        private string url = InvokeWrapper.ResolveNS(typeof({fullName}));

        {methods.Select(GenerateMethod).ToDelimitedString("\n")}

    }}
}}
#nullable restore

";
            return code;
        }

        private static string GenerateMethod(TSFunction function)
        {
            var parameters = string.Join(", ", function.Parameters.Select(p =>
                $"{ConvertType(p.Type)} {p.Name}" + (p.IsOptional ? " = default" : "")));
            var args = string.Join(", ", function.Parameters.Select(p => p.Name));
            var returnType = ConvertType(function.ReturnType);

            if (returnType == "void")
            {
                return $@"
        public async Task {function.Name}({parameters})
        {{
            await invoker.InvokeAsync<object?>(url, ""{function.Name}"",
                new object?[] {{ {args} }});
        }}";
            }
            else
            {
                return $@"
        public async Task<{returnType}> {function.Name}({parameters})
        {{
            return await invoker.InvokeAsync<{returnType}>(url, ""{function.Name}"",
                new object?[] {{ {args} }});
        }}";
            }
        }

        private static string ConvertType(string tsType)
        {
            return tsType switch
            {
                "string" => "string",
                "number" => "double",
                "boolean" => "bool",
                "void" => "void",
                "any" => "object?",
                _ => "object?"
            };
        }

        private static string GenerateExtension(SourceProductionContext spc, List<string> names)
        {
            var code = @$"#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTS.SourceGenerator.Extensions
{{
    public static class ServiceCollectionExtensions
    {{
        public static IServiceCollection AddJsInvokeServices(this IServiceCollection services)
        {{
            {names
                .Select(name => $@"services.AddScoped<{name}.TSInterop>();").ToDelimitedString("\n")}
            return services;
        }}
    }}
}}
#nullable restore

";
            return code;
        }



    }
}