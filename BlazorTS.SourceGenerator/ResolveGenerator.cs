using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using MoreLinq;
using System.Reflection;
using System.Runtime.InteropServices;
using BlazorTS.SourceGenerator;

namespace BlazorTS
{

    [Generator]
    public class ResolveGenerator : IIncrementalGenerator
    {

        private static bool _resolverInitialized;
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

            // 首先显示native path信息 - 作为第一个RegisterSourceOutput确保能看到
            context.RegisterSourceOutput(context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider), (spc, data) =>
            {
                var (compilation, optionsProvider) = data;
                var nativePath = DllResolver.GetTypeScriptParserNativePath(compilation, optionsProvider);
                Helper.Log(spc, $"TypeScript Parser Native Path: {nativePath}");

                // 检查nativePath是否有效，如果无效则跳过处理
                if (string.IsNullOrEmpty(nativePath))
                {
                    Helper.Log(spc, $"Skipping TypeScript processing - invalid nativePath: {nativePath}");
                    return;
                }

                if (!_resolverInitialized)
                {
                    try
                    {
                        var dllResolver = new DllResolver(nativePath);
                        NativeLibrary.SetDllImportResolver(typeof(TypeScriptParser.Parser).Assembly,
                            (libraryName, assembly, searchPath) => dllResolver.Resolve(libraryName, assembly, searchPath));
                        _resolverInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Helper.Log(spc, $"SetupNativeLibraryResolver failed : {ex.Message}");
                    }
                }
                
                // 显示程序集位置信息
                var assembly = Assembly.GetExecutingAssembly();
                Helper.Log(spc, $"Source Generator Assembly Location: {assembly.Location}");
                
                // 检查native库文件是否存在
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
            });

            // TypeScript文件处理
            var tsFilesProvider = context
                .AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".razor.ts", StringComparison.OrdinalIgnoreCase) || file.Path.EndsWith(".entry.ts", StringComparison.OrdinalIgnoreCase))
                .Select(static (file, _) => file.Path);
            
            // 注册诊断输出
            context.RegisterSourceOutput(tsFilesProvider.Collect().Combine(context.CompilationProvider).Combine(context.AnalyzerConfigOptionsProvider), (spc, data) =>
            {
                var ((paths, compilation), optionsProvider) = data;
                var nativePath = DllResolver.GetTypeScriptParserNativePath(compilation, optionsProvider);
                
                if (string.IsNullOrEmpty(nativePath))
                {
                    Helper.Log(spc, $"Skipping TypeScript files processing - invalid nativePath: {nativePath}");
                    return;
                }

                Helper.Log(spc, $"Found {paths.Length} BlazorTS files");
                foreach (var path in paths)
                {
                    Helper.Log(spc, $"BlazorTS file: {path}");
                }
            });

            // 真正业务逻辑
            var metaProvider = context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider)
                .Select((data, _) =>
                {
                    var (options, compilation) = data;
                    options.GlobalOptions.TryGetValue("build_property.ProjectDir", out var dir);
                    options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns);
                    var nativePath = DllResolver.GetTypeScriptParserNativePath(compilation, options);
                    return (dir, ns, nativePath);
                });

            var razorJsFiles = context
                .AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".razor.ts", StringComparison.OrdinalIgnoreCase) || file.Path.EndsWith(".entry.ts", StringComparison.OrdinalIgnoreCase))
                .Combine(metaProvider);

            var razorContents = razorJsFiles
                .Select((info, cancellationToken) =>
                {
                    var (additionalText, meta) = info;
                    var (className, namespaceName, isRazorComponent) = ExtractFileInfo(additionalText.Path, meta.ns ?? string.Empty, meta.dir ?? string.Empty);
                    var content = additionalText.GetText(cancellationToken)?.ToString() ?? string.Empty;
                    return (ClassName: className, NamespaceName: namespaceName, IsRazorComponent: isRazorComponent, Content: content);
                })
                .Collect();


            context.RegisterSourceOutput(razorContents.Combine(metaProvider), (spc, data) =>
            {
                var (files, meta) = data;
                var nativePath = meta.nativePath;

                if (string.IsNullOrEmpty(nativePath))
                {
                    Helper.Log(spc, $"Skipping BlazorTS processing - invalid nativePath: {nativePath}");
                    return;
                }

                Helper.Log(spc, $"Processing {files.Length} BlazorTS files with nativePath: {nativePath}");

                var entryModuleNames = new List<string>();

                foreach (var file in files)
                {
                    var fullName = Generate(spc, file.ClassName, file.NamespaceName, file.IsRazorComponent, file.Content);
                    if (!file.IsRazorComponent)
                    {
                        entryModuleNames.Add(fullName);
                    }
                }

                if (entryModuleNames.Any())
                {
                    var extensionCode = GenerateExtension(spc, entryModuleNames);
                    var fileName = "BlazorTS.SourceGenerator.Extensions.ServiceCollectionExtensions.g.cs";
                    spc.AddSource(fileName, SourceText.From(extensionCode, Encoding.UTF8));
                    Helper.Log(spc, $"Generated service collection extension for {entryModuleNames.Count} entry modules.");
                }

            });

        }

        private static (string className, string namespaceName, bool isRazorComponent) ExtractFileInfo(string filePath, string rootNamespace, string projectDir)
        {
            var relativePath = Path.GetRelativePath(projectDir, filePath);
            var fileName = Path.GetFileName(relativePath);
            var directoryPath = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '.');
            
            var namespaceParts = new List<string> { rootNamespace };
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath != ".")
            {
                namespaceParts.Add(directoryPath);
            }
            var finalNamespace = string.Join(".", namespaceParts);

            if (fileName.EndsWith(".razor.ts"))
            {
                var className = fileName[0..^".razor.ts".Length];
                return (className, finalNamespace, isRazorComponent: true);
            }

            if (fileName.EndsWith(".entry.ts"))
            {
                var className = fileName[0..^".entry.ts".Length];
                return (className, finalNamespace, isRazorComponent: false);
            }

            throw new InvalidOperationException($"Unsupported file type: {filePath}");
        }

        private static string Generate(SourceProductionContext spc, string className, string ns, bool isRazorComponent, string content)
        {
            var methods = MethodExtractor.Extract(content);
            Helper.Log(spc, $"Generating wrapper for '{ns}.{className}' with {methods.Count()} methods. IsRazorComponent: {isRazorComponent}");

            var code = GenerateWrapper(ns, className, methods, isRazorComponent);
            var fileHint = isRazorComponent ? $"{ns}.{className}.razor.g.cs" : $"{ns}.{className}.entry.g.cs";
            spc.AddSource(fileHint, SourceText.From(code, Encoding.UTF8));
            
            return $"{ns}.{className}";
        }

        private static string GenerateWrapper(string ns, string className, IEnumerable<TSFunction> methods, bool isRazorComponent)
        {
            var fullName = $"{ns}.{className}";

            if (isRazorComponent)
            {
                return $@"#nullable enable
using BlazorTS;
using Microsoft.AspNetCore.Components;

namespace {ns};

/// <summary>
/// A partial class to provide access to TypeScript functions from {className}.ts.
/// </summary>
public partial class {className}
{{
    /// <summary>
    /// Gets or sets the TypeScript interop instance.
    /// </summary>
    [Inject] public TSInterop Scripts {{ get; set; }} = null!;

    /// <summary>
    /// Provides strongly-typed access to the TypeScript functions in {className}.ts.
    /// </summary>
    public class TSInterop(ScriptBridge invoker)
    {{
        private readonly string url = invoker.ResolveNS(typeof({fullName}));

        {methods.Select(GenerateMethod).ToDelimitedString("\n")}
    }}
}}
#nullable restore
";
            }
            else
            {
                return $@"#nullable enable
using BlazorTS;
using Microsoft.AspNetCore.Components;

namespace {ns};

/// <summary>
/// Provides strongly-typed access to the TypeScript functions in {className}.entry.ts.
/// This class is designed for dependency injection.
/// </summary>
public class {className}(ScriptBridge invoker)
{{
    private readonly string url = invoker.ResolveNS(typeof({fullName}));

    {methods.Select(GenerateMethod).ToDelimitedString("\n")}
}}
#nullable restore
";
            }
        }

        private static string GenerateMethod(TSFunction function)
        {
            var parameters = string.Join(", ", function.Parameters.Select(p =>
                $"{ConvertType(p.Type)} {p.Name}" + (p.IsOptional ? " = default" : "")));
            var args = string.Join(", ", function.Parameters.Select(p => p.Name));
            var returnType = ConvertType(function.ReturnType);
            var paramDocs = string.Join("\n", function.Parameters.Select(p => $"        /// <param name=\"{p.Name}\">A {p.Type} value.</param>"));

            if (returnType == "void")
            {
                return $@"
        /// <summary>
        /// Invokes the '{function.Name}' TypeScript function.
        /// </summary>
{paramDocs}
        public async Task {function.Name}({parameters})
        {{
            await invoker.InvokeAsync<object?>(url, ""{function.Name}"",
                new object?[] {{ {args} }});
        }}";
            }
            else
            {
                return $@"
        /// <summary>
        /// Invokes the '{function.Name}' TypeScript function and returns a {returnType}.
        /// </summary>
{paramDocs}
        /// <returns>A Task that represents the asynchronous operation, containing the {returnType} result from the TypeScript function.</returns>
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

        private static string GenerateExtension(SourceProductionContext spc, List<string> entryModuleNames)
        {
            var servicesRegistration = entryModuleNames
                .Select(name => $"            services.AddScoped<{name}>();")
                .ToDelimitedString("\n");

            return $@"#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace BlazorTS.SourceGenerator.Extensions
{{
    /// <summary>
    /// Provides extension methods for setting up BlazorTS-generated script interop services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {{
        /// <summary>
        /// Adds all generated TypeScript interop services to the specified <see cref=""IServiceCollection""/>.
        /// </summary>
        /// <param name=""services"">The <see cref=""IServiceCollection""/> to add the services to.</param>
        /// <returns>The <see cref=""IServiceCollection""/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddBlazorTSScripts(this IServiceCollection services)
        {{
{servicesRegistration}
            return services;
        }}
    }}
}}
#nullable restore
";
        }



    }
}