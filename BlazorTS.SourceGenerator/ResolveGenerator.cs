using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MoreLinq;
using System.Runtime.InteropServices;
using System.Text;
using BlazorTS.SourceGenerator;

namespace BlazorTS
{
    internal sealed record ComponentModel(
        string ClassName,
        string NamespaceName,
        bool IsRazorComponent,
        string Content,
        string TypeParameterListText,
        string InteropClassName,
        string? WhereClauseText,
        string FullName);

    [Generator]
    public class ResolveGenerator : IIncrementalGenerator
    {
        private static bool _resolverInitialized;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var metaProvider = context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider)
                .Select((data, _) =>
                {
                    var (options, compilation) = data;
                    options.GlobalOptions.TryGetValue("build_property.ProjectDir", out var dir);
                    options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns);
                    var nativePath = DllResolver.GetTypeScriptParserNativePath(compilation, options);
                    return (dir, ns, nativePath, compilation);
                });

            var tsFiles = context
                .AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".razor.ts", StringComparison.OrdinalIgnoreCase) || file.Path.EndsWith(".entry.ts", StringComparison.OrdinalIgnoreCase))
                .Combine(metaProvider);

            var componentModels = tsFiles
                .Select((info, cancellationToken) =>
                {
                    var (additionalText, meta) = info;
                    EnsureNativeResolver(meta.nativePath);

                    var content = additionalText.GetText(cancellationToken)?.ToString() ?? string.Empty;
                    return CreateComponentModel(additionalText.Path, content, meta.ns ?? string.Empty, meta.dir ?? string.Empty, meta.compilation);
                })
                .Collect();

            context.RegisterSourceOutput(componentModels.Combine(metaProvider), (spc, data) =>
            {
                var (files, meta) = data;
                if (string.IsNullOrEmpty(meta.nativePath))
                {
                    return;
                }

                var injectableTypeNames = new List<string>(files.Length);
                foreach (var file in files)
                {
                    injectableTypeNames.Add(Generate(spc, file));
                }

                // BLAZORTS-DESIGN-GUARD: 泛型仅作用于 Razor partial class，
                // 互操作类型统一提升为顶层可注入服务，避免让 DI 服务类型被组件泛型污染。
                var extensionCode = GenerateExtension(injectableTypeNames, meta.ns ?? string.Empty);
                var extensionNamespace = string.IsNullOrEmpty(meta.ns)
                    ? "BlazorTS.Generated"
                    : $"{meta.ns}.Generated";
                spc.AddSource(
                    $"{extensionNamespace}.Scripts.g.cs",
                    SourceText.From(extensionCode, Encoding.UTF8));
            });
        }

        private static void EnsureNativeResolver(string? nativePath)
        {
            if (_resolverInitialized || string.IsNullOrEmpty(nativePath))
            {
                return;
            }

            try
            {
                var dllResolver = new DllResolver(nativePath);
                NativeLibrary.SetDllImportResolver(
                    typeof(TypeScriptParser.Parser).Assembly,
                    (libraryName, assembly, searchPath) => dllResolver.Resolve(libraryName, assembly, searchPath));
                _resolverInitialized = true;
            }
            catch
            {
                // Native resolver setup is best-effort here; downstream parsing failures will surface naturally.
            }
        }

        private static ComponentModel CreateComponentModel(string filePath, string content, string rootNamespace, string projectDir, Compilation compilation)
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

            if (fileName.EndsWith(".razor.ts", StringComparison.OrdinalIgnoreCase))
            {
                var className = fileName[0..^".razor.ts".Length];
                var componentType = ResolveComponentType(compilation, finalNamespace, className);
                var typeParameterListText = BuildTypeParameterList(componentType);
                var whereClauseText = BuildWhereClause(componentType);
                var interopClassName = $"{className}TSInterop";
                var fullName = $"{finalNamespace}.{className}";

                return new ComponentModel(
                    className,
                    finalNamespace,
                    true,
                    content,
                    typeParameterListText,
                    interopClassName,
                    whereClauseText,
                    fullName);
            }

            if (fileName.EndsWith(".entry.ts", StringComparison.OrdinalIgnoreCase))
            {
                var className = fileName[0..^".entry.ts".Length];
                var fullName = $"{finalNamespace}.{className}";
                return new ComponentModel(
                    className,
                    finalNamespace,
                    false,
                    content,
                    string.Empty,
                    className,
                    null,
                    fullName);
            }

            throw new InvalidOperationException($"Unsupported file type: {filePath}");
        }

        private static INamedTypeSymbol? ResolveComponentType(Compilation compilation, string namespaceName, string className)
        {
            return compilation.GetSymbolsWithName(className, SymbolFilter.Type)
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault(symbol => symbol.Name == className && symbol.ContainingNamespace?.ToDisplayString() == namespaceName);
        }

        private static string BuildTypeParameterList(INamedTypeSymbol? componentType)
        {
            if (componentType is null || componentType.TypeParameters.Length == 0)
            {
                return string.Empty;
            }

            return $"<{string.Join(", ", componentType.TypeParameters.Select(parameter => parameter.Name))}>";
        }

        private static string? BuildWhereClause(INamedTypeSymbol? componentType)
        {
            if (componentType is null || componentType.TypeParameters.Length == 0)
            {
                return null;
            }

            var clauses = componentType.TypeParameters
                .Select(BuildWhereClause)
                .Where(static clause => !string.IsNullOrWhiteSpace(clause))
                .ToArray();

            return clauses.Length == 0 ? null : string.Join(" ", clauses);
        }

        private static string? BuildWhereClause(ITypeParameterSymbol parameter)
        {
            var constraints = new List<string>();

            if (parameter.HasReferenceTypeConstraint)
            {
                constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
            }

            if (parameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }

            if (parameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            if (parameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }

            constraints.AddRange(parameter.ConstraintTypes.Select(type => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));

            if (parameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            return constraints.Count == 0 ? null : $"where {parameter.Name} : {string.Join(", ", constraints)}";
        }

        private static string Generate(SourceProductionContext spc, ComponentModel component)
        {
            var methods = MethodExtractor.Extract(component.Content).ToArray();
            var code = GenerateWrapper(component, methods);
            var hintName = component.IsRazorComponent
                ? $"{component.NamespaceName}.{component.ClassName}.razor.g.cs"
                : $"{component.NamespaceName}.{component.ClassName}.entry.g.cs";
            spc.AddSource(hintName, SourceText.From(code, Encoding.UTF8));

            return component.IsRazorComponent ? $"{component.NamespaceName}.{component.InteropClassName}" : component.FullName;
        }

        private static string GenerateWrapper(ComponentModel component, IEnumerable<TSFunction> methods)
        {
            var methodsCode = methods.Select(GenerateMethod).ToDelimitedString("\n");

            if (component.IsRazorComponent)
            {
                var whereClauseSuffix = string.IsNullOrWhiteSpace(component.WhereClauseText)
                    ? string.Empty
                    : $"\n    {component.WhereClauseText}";

                return $@"#nullable enable
using BlazorTS;
using Microsoft.AspNetCore.Components;

namespace {component.NamespaceName};

/// <summary>
/// A partial class to provide access to TypeScript functions from {component.ClassName}.razor.ts.
/// </summary>
public partial class {component.ClassName}{component.TypeParameterListText}{whereClauseSuffix}
{{
    /// <summary>
    /// BLAZORTS-DESIGN-GUARD: 保留组件自动注入，但将互操作类型提升为顶层服务，
    /// 这样组件泛型参数不会进入 DI 服务模型。
    /// </summary>
    [Inject] public {component.InteropClassName} Scripts {{ get; set; }} = null!;
}}

/// <summary>
/// Provides strongly-typed access to the TypeScript functions in {component.ClassName}.razor.ts.
/// </summary>
public class {component.InteropClassName}(ScriptBridge invoker)
{{
    private readonly string url = invoker.ResolveNS(""{component.FullName}"", "".razor"");

    {methodsCode}
}}
#nullable restore
";
            }

            return $@"#nullable enable
using BlazorTS;

namespace {component.NamespaceName};

/// <summary>
/// Provides strongly-typed access to the TypeScript functions in {component.ClassName}.entry.ts.
/// This class is designed for dependency injection.
/// </summary>
public class {component.ClassName}(ScriptBridge invoker)
{{
    private readonly string url = invoker.ResolveNS(typeof({component.FullName}), "".entry"");

    {methodsCode}
}}
#nullable restore
";
        }

        private static string GenerateMethod(TSFunction function)
        {
            var parameters = string.Join(", ", function.Parameters.Select(p =>
                $"{ConvertType(p.Type)} {p.Name}" + (p.IsOptional ? " = default" : string.Empty)));
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

        private static string GenerateExtension(List<string> injectableTypeNames, string rootNamespace)
        {
            var servicesRegistration = injectableTypeNames
                .Select(name => $"        services.AddScoped<{name}>();")
                .ToDelimitedString("\n");
            var normalizedNamespace = string.IsNullOrEmpty(rootNamespace)
                ? "BlazorTS.Generated"
                : $"{rootNamespace}.Generated";
            var methodName = "AddScripts";

            return $@"#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace {normalizedNamespace};

/// <summary>
/// Registers TypeScript interop services generated for this project.
/// </summary>
public static class Scripts
{{
    /// <summary>
    /// Registers all TypeScript interop services generated for this project.
    /// </summary>
    public static void {methodName}(IServiceCollection services)
    {{
{servicesRegistration}
    }}
}}
#nullable restore
";
        }
    }
}
