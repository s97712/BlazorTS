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
    public void ResolveGenerator_WithRazorTsFile_GeneratesPartialClass_And_Extension()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(name: string): string { return `Hello, ${name}!`; }";
        var tsFile = CreateAdditionalText("Components/Pages/MyComponent.razor.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile }, "/test/project/", "TestApp");

        // Assert: now two files (class + extension)
        Assert.Equal(2, generatorResult.GeneratedSources.Length);

        var generatedClass = generatorResult.GeneratedSources.Single(s => s.HintName == "TestApp.Components.Pages.MyComponent.razor.g.cs");
        var code = generatedClass.SourceText.ToString();
        Assert.Contains("namespace TestApp.Components.Pages;", code);
        Assert.Contains("public partial class MyComponent", code);
        Assert.Contains("[Inject] public TSInterop Scripts { get; set; } = null!;", code);
        Assert.Contains("public class TSInterop(ScriptBridge invoker)", code);
        Assert.Contains("public async Task<string> greet(string name)", code);

        var extensionSource = generatorResult.GeneratedSources.Single(s => s.HintName.EndsWith("Extensions.g.cs"));
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static IServiceCollection AddBlazorTSScripts(this IServiceCollection services)", extensionCode);
        Assert.Contains("services.AddBlazorTS();", extensionCode);
        Assert.Contains("services.AddScoped<TestApp.Components.Pages.MyComponent.TSInterop>();", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithEntryTsFile_GeneratesStandardClassAndExtension()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = "export function doWork(): void {}";
        var tsFile = CreateAdditionalText("Services/MyService.entry.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile }, "/test/project/", "TestApp");

        // Assert
        Assert.Equal(2, generatorResult.GeneratedSources.Length);

        // Check generated class
        var classSource = generatorResult.GeneratedSources.FirstOrDefault(s => s.HintName.EndsWith(".entry.g.cs"));
        Assert.NotNull(classSource.SourceText);
        Assert.Equal("TestApp.Services.MyService.entry.g.cs", classSource.HintName);
        var classCode = classSource.SourceText.ToString();
        Assert.Contains("namespace TestApp.Services;", classCode);
        Assert.Contains("public class MyService(ScriptBridge invoker)", classCode);
        Assert.DoesNotContain("public partial class", classCode);
        Assert.DoesNotContain("[Inject]", classCode);
        Assert.Contains("public async Task doWork()", classCode);

        // Check service extension
        var extensionSource = generatorResult.GeneratedSources.FirstOrDefault(s => s.HintName.EndsWith("Extensions.g.cs"));
        Assert.NotNull(extensionSource.SourceText);
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static IServiceCollection AddBlazorTSScripts(this IServiceCollection services)", extensionCode);
        Assert.Contains("services.AddBlazorTS();", extensionCode);
        Assert.Contains("services.AddScoped<TestApp.Services.MyService>();", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithUnsupportedTsFile_GeneratesOnlyExtension()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(name: string): string { return `Hello, ${name}!`; }";
        var tsFile = CreateAdditionalText("Demo.ts", tsContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { tsFile });

        // Assert: now extension is always generated
        Assert.Single(generatorResult.GeneratedSources);
        var extensionSource = generatorResult.GeneratedSources.Single();
        Assert.EndsWith("Extensions.g.cs", extensionSource.HintName);
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static IServiceCollection AddBlazorTSScripts(this IServiceCollection services)", extensionCode);
        Assert.Contains("services.AddBlazorTS();", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithMixedFiles_GeneratesCorrectly()
    {
        // Arrange
        var generator = new ResolveGenerator();
        var razorContent = "export function razorFunc(): void {}";
        var entryContent = "export function entryFunc(): void {}";
        var razorFile = CreateAdditionalText("Components/MyPage.razor.ts", razorContent);
        var entryFile = CreateAdditionalText("Api/Client.entry.ts", entryContent);

        // Act
        var generatorResult = RunGenerator(generator, new[] { razorFile, entryFile }, "/test/project/", "MyApp");

        // Assert
        Assert.Equal(3, generatorResult.GeneratedSources.Length);

        // Razor file check
        var razorSource = generatorResult.GeneratedSources.Single(s => s.HintName == "MyApp.Components.MyPage.razor.g.cs");
        var razorCode = razorSource.SourceText.ToString();
        Assert.Contains("public partial class MyPage", razorCode);
        Assert.Contains("public async Task razorFunc()", razorCode);

        // Entry file check
        var entrySource = generatorResult.GeneratedSources.Single(s => s.HintName == "MyApp.Api.Client.entry.g.cs");
        var entryCode = entrySource.SourceText.ToString();
        Assert.Contains("public class Client(ScriptBridge invoker)", entryCode);
        Assert.Contains("public async Task entryFunc()", entryCode);

        // Extension check
        var extensionSource = generatorResult.GeneratedSources.Single(s => s.HintName.EndsWith("Extensions.g.cs"));
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("services.AddBlazorTS();", extensionCode);
        Assert.Contains("services.AddScoped<MyApp.Api.Client>();", extensionCode);
        Assert.Contains("services.AddScoped<MyApp.Components.MyPage.TSInterop>();", extensionCode);
    }

}
