using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Reflection;

namespace BlazorTS.SourceGenerator.Tests;

/// <summary>
/// 测试基类，提供公共的测试辅助方法和类
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// 创建带有默认配置的ResolveGenerator测试驱动程序
    /// </summary>
    protected static CSharpGeneratorDriver CreateGeneratorDriver(
        ResolveGenerator generator,
        IEnumerable<AdditionalText> additionalTexts,
        string projectDir = "/test/project/",
        string rootNamespace = "TestApp")
    {
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(new Dictionary<string, string>
        {
            ["build_property.ProjectDir"] = projectDir,
            ["build_property.RootNamespace"] = rootNamespace,
            ["build_property.TypeScriptParserNativePath"] = GetTypeScriptParserNativePathFromAssembly()
        });

        return (CSharpGeneratorDriver)CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts))
            .WithUpdatedAnalyzerConfigOptions(optionsProvider);
    }

    /// <summary>
    /// 运行生成器并返回生成结果
    /// </summary>
    protected static GeneratorRunResult RunGenerator(
        ResolveGenerator generator,
        IEnumerable<AdditionalText> additionalTexts,
        string projectDir = "/test/project/",
        string rootNamespace = "TestApp")
    {
        var compilation = CSharpCompilation.Create("test");
        var driver = CreateGeneratorDriver(generator, additionalTexts, projectDir, rootNamespace);
        
        var result = driver.RunGenerators(compilation);
        var runResult = result.GetRunResult();
        
        return runResult.Results[0];
    }

    /// <summary>
    /// 创建AdditionalText测试对象
    /// </summary>
    protected static AdditionalText CreateAdditionalText(string path, string content)
    {
        return new TestAdditionalText(path, content);
    }

    /// <summary>
    /// 验证函数参数的公共断言方法
    /// </summary>
    protected static void AssertParameter(
        TSParameter parameter,
        string expectedName,
        string expectedType,
        bool expectedIsOptional = false)
    {
        Xunit.Assert.Equal(expectedName, parameter.Name);
        Xunit.Assert.Equal(expectedType, parameter.Type);
        Xunit.Assert.Equal(expectedIsOptional, parameter.IsOptional);
    }

    /// <summary>
    /// 验证函数基本信息的公共断言方法
    /// </summary>
    protected static void AssertFunction(
        TSFunction function,
        string expectedName,
        string expectedReturnType,
        bool expectedIsAsync = false,
        int expectedParameterCount = 0)
    {
        Xunit.Assert.Equal(expectedName, function.Name);
        Xunit.Assert.Equal(expectedReturnType, function.ReturnType);
        Xunit.Assert.Equal(expectedIsAsync, function.IsAsync);
        Xunit.Assert.Equal(expectedParameterCount, function.Parameters.Count);
    }

    /// <summary>
    /// 从执行程序集的元数据中获取TypeScriptParserNativePath
    /// </summary>
    private static string GetTypeScriptParserNativePathFromAssembly()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
            
            foreach (var attr in attributes)
            {
                if (attr.Key == "TypeScriptParserNativePath")
                {
                    return attr.Value ?? "/test/native/";
                }
            }
        }
        catch (Exception)
        {
            // 忽略异常，返回默认测试路径
        }
        
        // 使用一个非空的默认测试路径，确保生成器不会因空路径而跳过处理
        return "/test/native/";
    }
}

/// <summary>
/// 测试用的AdditionalText实现
/// </summary>
public class TestAdditionalText : AdditionalText
{
    private readonly string _path;
    private readonly string _content;

    public TestAdditionalText(string path, string content)
    {
        _path = path;
        _content = content;
    }

    public override string Path => _path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(_content);
    }
}

/// <summary>
/// 测试用的AnalyzerConfigOptionsProvider实现
/// </summary>
public class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly Dictionary<string, string> _globalOptions;

    public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public override AnalyzerConfigOptions GlobalOptions => new TestAnalyzerConfigOptions(_globalOptions);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => 
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => 
        new TestAnalyzerConfigOptions(new Dictionary<string, string>());
}

/// <summary>
/// 测试用的AnalyzerConfigOptions实现
/// </summary>
public class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptions(Dictionary<string, string> options)
    {
        _options = options;
    }

    public override bool TryGetValue(string key, out string value)
    {
        return _options.TryGetValue(key, out value!);
    }
}
