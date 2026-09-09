using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MoreLinq;
using System.Runtime.InteropServices;
using System.Text;
using BlazorTS.SourceGenerator;

namespace BlazorTS;

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

        var dllResolver = new DllResolver(nativePath);
        NativeLibrary.SetDllImportResolver(
            typeof(TypeScriptParser.Parser).Assembly,
            (libraryName, assembly, searchPath) => dllResolver.Resolve(libraryName, assembly, searchPath));
        _resolverInitialized = true;
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
            var classConstraint = parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                ? "class?"
                : "class";
            constraints.Add(classConstraint);
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

        foreach (var type in parameter.ConstraintTypes)
        {
            constraints.Add(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }

        if (parameter.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        if (constraints.Count == 0)
        {
            return null;
        }

        return $"where {parameter.Name} : {string.Join(", ", constraints)}";
    }

    private static string Generate(SourceProductionContext spc, ComponentModel component)
    {
        using var analyzer = new TSAnalyzer();
        var module = analyzer.Analyze(component.Content);
        var code = GenerateWrapper(component, module.Functions, module.Interfaces);
        var hintName = component.IsRazorComponent
            ? $"{component.NamespaceName}.{component.ClassName}.razor.g.cs"
            : $"{component.NamespaceName}.{component.ClassName}.entry.g.cs";
        spc.AddSource(hintName, SourceText.From(code, Encoding.UTF8));

        return component.IsRazorComponent ? $"{component.NamespaceName}.{component.InteropClassName}" : component.FullName;
    }

    private static string GenerateWrapper(ComponentModel component, IEnumerable<TSFunction> methods, IReadOnlyList<TSInterface> interfaces)
    {
        var methodsCode = methods.Select(method => GenerateMethod(method, interfaces)).ToDelimitedString("\n");
        var interfacesCode = GenerateInterfaces(interfaces);
        var interfacesSection = string.IsNullOrWhiteSpace(interfacesCode)
            ? string.Empty
            : $"{interfacesCode}\n\n";

        if (component.IsRazorComponent)
        {
            var whereClauseSuffix = string.IsNullOrWhiteSpace(component.WhereClauseText)
                ? string.Empty
                : $"\n    {component.WhereClauseText}";

            return SourceTemplates.RenderRazorWrapper(
                component,
                methodsCode,
                interfacesSection,
                whereClauseSuffix);
        }

        return SourceTemplates.RenderEntryWrapper(component, methodsCode, interfacesSection);
    }

    private static string GenerateInterfaces(IReadOnlyList<TSInterface> interfaces)
    {
        return interfaces
            .Select(tsInterface =>
            {
                var properties = tsInterface.Properties
                    .Select(property => GenerateProperty(property, interfaces))
                    .ToDelimitedString("\n");

                return SourceTemplates.RenderInterface(tsInterface.Name, properties);
            })
            .ToDelimitedString("\n\n");
    }

    private static string GenerateProperty(TSProperty property, IReadOnlyList<TSInterface> interfaces)
    {
        var type = ConvertType(property.Type, interfaces);
        if (property.IsOptional && !type.EndsWith('?') && type != "object?")
        {
            type += "?";
        }

        var required = property.IsOptional ? string.Empty : "required ";
        return SourceTemplates.RenderProperty(property.Name, type, required);
    }

    private static string ConvertType(string tsType, IReadOnlyList<TSInterface> interfaces)
    {
        var type = tsType.Trim();
        if (type.StartsWith("Promise<", StringComparison.Ordinal) && type.EndsWith('>'))
        {
            type = type[8..^1].Trim();
        }

        if (type.EndsWith("[]", StringComparison.Ordinal))
        {
            return $"{ConvertType(type[..^2], interfaces)}[]";
        }

        if (type.StartsWith("Array<", StringComparison.Ordinal) && type.EndsWith('>'))
        {
            return $"{ConvertType(type[6..^1], interfaces)}[]";
        }

        return type switch
        {
            "string" => "string",
            "number" => "double",
            "boolean" => "bool",
            "void" => "void",
            "any" => "object?",
            _ when interfaces.Any(item => item.Name == type) => type,
            _ => "object?"
        };
    }

    private static string GenerateMethod(TSFunction function, IReadOnlyList<TSInterface> interfaces)
    {
        var parameters = string.Join(", ", function.Parameters.Select(parameter =>
            $"{ConvertType(parameter.Type, interfaces)} {parameter.Name}" + (parameter.IsOptional ? " = default" : string.Empty)));
        var arguments = string.Join(", ", function.Parameters.Select(parameter => parameter.Name));
        var returnType = ConvertType(function.ReturnType, interfaces);
        var parameterDocumentation = function.Parameters
            .Select(parameter => SourceTemplates.RenderParameterDocumentation(parameter.Name, parameter.Type))
            .ToDelimitedString("\n");

        if (returnType == "void")
        {
            return SourceTemplates.RenderVoidMethod(
                function.Name,
                parameters,
                arguments,
                parameterDocumentation);
        }

        return SourceTemplates.RenderResultMethod(
            function.Name,
            parameters,
            arguments,
            parameterDocumentation,
            returnType);
    }

    private static string GenerateExtension(List<string> injectableTypeNames, string rootNamespace)
    {
        var servicesRegistration = injectableTypeNames
            .Select(name => $"        services.AddScoped<{name}>();")
            .ToDelimitedString("\n");
        var normalizedNamespace = string.IsNullOrEmpty(rootNamespace)
            ? "BlazorTS.Generated"
            : $"{rootNamespace}.Generated";

        return SourceTemplates.RenderExtension(normalizedNamespace, servicesRegistration);
    }
}
