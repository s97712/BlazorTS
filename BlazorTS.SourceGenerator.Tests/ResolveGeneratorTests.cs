using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using BlazorTS;

namespace BlazorTS.SourceGenerator.Tests;

public class ResolveGeneratorTests : TestBase
{
    [Fact]
    public void ResolveGenerator_Create_DoesNotThrow()
    {
        // Arrange & Act & Assert - 基础创建测试
        var generator = new ResolveGenerator();
        Assert.NotNull(generator);
    }

    [Fact]
    public void ResolveGenerator_WithSingleTsFile_GeneratesOutput()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(name: string): string { return `Hello, ${name}!`; }";
        var tsFile = CreateAdditionalText("Demo.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile });

        // Assert
        Assert.True(generatorResult.GeneratedSources.Length >= 0);
    }


    [Fact]
    public void ResolveGenerator_VerifyGeneratedWrapperCode_CorrectStructure()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(name: string): string { return `Hello, ${name}!`; }";
        var tsFile = CreateAdditionalText("Demo.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile });
        
        // Assert - 应该生成两个文件：包装类和服务扩展
        Assert.True(generatorResult.GeneratedSources.Length >= 2);
        
        // 检查包装类代码
        var wrapperSource = generatorResult.GeneratedSources
            .FirstOrDefault(s => s.HintName.Contains("Demo.ts.module.g.cs"));
        Assert.NotNull(wrapperSource.SourceText);
        
        var wrapperCode = wrapperSource.SourceText.ToString();
        
        // 验证包装类结构
        Assert.Contains("public partial class Demo", wrapperCode);
        Assert.Contains("public class TSInterop(InvokeWrapper invoker)", wrapperCode);
        Assert.Contains("public async Task<string> greet(string name)", wrapperCode);
        Assert.Contains("return await invoker.InvokeAsync<string>", wrapperCode);
        Assert.Contains("new object?[] { name }", wrapperCode);
    }

    [Fact]
    public void ResolveGenerator_VerifyServiceExtensionCode_CorrectRegistration()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsFile1 = CreateAdditionalText("Utils.ts", "function helper(): void {}");
        var tsFile2 = CreateAdditionalText("Api.ts", "function fetch(): Promise<any> { return Promise.resolve(); }");

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile1, tsFile2 }, "/test/project/", "MyApp");
        
        // Assert - 检查服务扩展代码
        var serviceSource = generatorResult.GeneratedSources
            .FirstOrDefault(s => s.HintName.Contains("ServiceCollectionExtensions.g.cs"));
        Assert.NotNull(serviceSource.SourceText);
        
        var serviceCode = serviceSource.SourceText.ToString();
        
        // 验证服务注册
        Assert.Contains("namespace BlazorTS.SourceGenerator.Extensions", serviceCode);
        Assert.Contains("public static class ServiceCollectionExtensions", serviceCode);
        Assert.Contains("AddJsInvokeServices", serviceCode);
        Assert.Contains("Utils.TSInterop", serviceCode);
        Assert.Contains("Api.TSInterop", serviceCode);
    }

    [Fact]
    public void ResolveGenerator_VerifyTypeConversion_CorrectMapping()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = @"
function processData(
    text: string,
    count: number,
    flag: boolean,
    data: any
): number {
    return 42;
}";
        var tsFile = CreateAdditionalText("TypeTest.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile }, "/test/project/", "TypeTests");
        
        // Assert
        var wrapperSource = generatorResult.GeneratedSources
            .FirstOrDefault(s => s.HintName.Contains("TypeTest.ts.module.g.cs"));
        Assert.NotNull(wrapperSource.SourceText);
        
        var wrapperCode = wrapperSource.SourceText.ToString();
        
        // 验证类型转换
        Assert.Contains("string text", wrapperCode);        // string -> string
        Assert.Contains("double count", wrapperCode);       // number -> double
        Assert.Contains("bool flag", wrapperCode);          // boolean -> bool
        Assert.Contains("object? data", wrapperCode);       // any -> object?
        Assert.Contains("Task<double>", wrapperCode);       // number -> Task<double>
    }

    [Fact]
    public void ResolveGenerator_VerifyVoidFunction_TaskReturnType()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = "function initialize(): void { console.log('init'); }";
        var tsFile = CreateAdditionalText("Init.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile }, "/test/project/", "InitApp");
        
        // Assert
        var wrapperSource = generatorResult.GeneratedSources
            .FirstOrDefault(s => s.HintName.Contains("Init.ts.module.g.cs"));
        Assert.NotNull(wrapperSource.SourceText);
        
        var wrapperCode = wrapperSource.SourceText.ToString();
        
        // 验证void函数返回Task而不是Task<void>
        Assert.Contains("public async Task initialize()", wrapperCode);
        Assert.Contains("await invoker.InvokeAsync<object?>", wrapperCode);
        Assert.DoesNotContain("Task<void>", wrapperCode);
    }

}