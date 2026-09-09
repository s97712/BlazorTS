using Xunit;
using BlazorTS;

namespace BlazorTS.SourceGenerator.Tests;

public class ResolveGeneratorTests : TestBase
{
    [Fact]
    public void ResolveGenerator_Create_DoesNotThrow()
    {
        var generator = new ResolveGenerator();
        Assert.NotNull(generator);
    }

    [Fact]
    public void ResolveGenerator_WithRazorTsFile_GeneratesPartialClass_And_TopLevelInterop()
    {
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(name: string): string { return `Hello, ${name}!`; }";
        var tsFile = CreateAdditionalText("Components/Pages/MyComponent.razor.ts", tsContent);

        var generatorResult = RunGenerator(
            generator,
            new[] { tsFile },
            "/test/project/",
            "TestApp",
            "namespace TestApp.Components.Pages; public partial class MyComponent { }");

        Assert.Equal(2, generatorResult.GeneratedSources.Length);

        var generatedClass = generatorResult.GeneratedSources.Single(s => s.HintName == "TestApp.Components.Pages.MyComponent.razor.g.cs");
        var code = generatedClass.SourceText.ToString();
        Assert.Contains("namespace TestApp.Components.Pages;", code);
        Assert.Contains("public partial class MyComponent", code);
        Assert.Contains("[Inject] public MyComponentTSInterop Scripts { get; set; } = null!;", code);
        Assert.Contains("public class MyComponentTSInterop(ScriptBridge invoker)", code);
        Assert.Contains("public async Task<string> greet(string name)", code);
        Assert.Contains("invoker.ResolveNS(\"TestApp.Components.Pages.MyComponent\", \".razor\")", code);

        var extensionSource = generatorResult.GeneratedSources.Single(s => s.HintName.EndsWith("Scripts.g.cs"));
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static void AddScripts(IServiceCollection services)", extensionCode);
        Assert.Contains("services.AddScoped<TestApp.Components.Pages.MyComponentTSInterop>();", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithGenericRazorTsFile_GeneratesGenericPartialClass_And_NonGenericInterop()
    {
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(): string { return 'Hello'; }";
        var tsFile = CreateAdditionalText("Components/Grid.razor.ts", tsContent);

        var generatorResult = RunGenerator(
            generator,
            new[] { tsFile },
            "/test/project/",
            "TestApp",
            "namespace TestApp.Components; public partial class Grid<TItem> where TItem : class { }");

        var generatedClass = generatorResult.GeneratedSources.Single(s => s.HintName == "TestApp.Components.Grid.razor.g.cs");
        var code = generatedClass.SourceText.ToString();
        Assert.Contains("public partial class Grid<TItem>", code);
        Assert.Contains("where TItem : class", code);
        Assert.Contains("[Inject] public GridTSInterop Scripts { get; set; } = null!;", code);
        Assert.Contains("public class GridTSInterop(ScriptBridge invoker)", code);
        Assert.Contains("invoker.ResolveNS(\"TestApp.Components.Grid\", \".razor\")", code);


        var extensionSource = generatorResult.GeneratedSources.Single(s => s.HintName.EndsWith("Scripts.g.cs"));
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("services.AddScoped<TestApp.Components.GridTSInterop>();", extensionCode);
        Assert.DoesNotContain("Grid<TItem>.TSInterop", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithInterfaces_GeneratesStronglyTypedModels()
    {
        var generatorResult = RunGenerator(
            new ResolveGenerator(),
            new[] { CreateAdditionalText("Services/User.entry.ts", @"
export interface Address { city: string; }
export interface User { name: string; address: Address; tags?: string[]; }
export function getUser(): Promise<User> { throw new Error(); }
export function saveUser(user: User): boolean { return true; }") },
            "/test/project/", "TestApp");

        var code = generatorResult.GeneratedSources.Single(source => source.HintName.EndsWith(".entry.g.cs")).SourceText.ToString();
        Assert.Contains("public sealed class Address", code);
        Assert.Contains("public required string city", code);
        Assert.Contains("public sealed class User", code);
        Assert.Contains("public required Address address", code);
        Assert.Contains("public string[]? tags", code);
        Assert.Contains("public async Task<User> getUser()", code);
        Assert.Contains("public async Task<bool> saveUser(User user)", code);
    }

    [Fact]
    public void ResolveGenerator_WithEntryTsFile_GeneratesStandardClassAndExtension()
    {
        var generator = new ResolveGenerator();
        var tsContent = "export function doWork(): void {}";
        var tsFile = CreateAdditionalText("Services/MyService.entry.ts", tsContent);

        var generatorResult = RunGenerator(generator, new[] { tsFile }, "/test/project/", "TestApp");

        Assert.Equal(2, generatorResult.GeneratedSources.Length);

        var classSource = generatorResult.GeneratedSources.FirstOrDefault(s => s.HintName.EndsWith(".entry.g.cs"));
        Assert.NotNull(classSource.SourceText);
        Assert.Equal("TestApp.Services.MyService.entry.g.cs", classSource.HintName);
        var classCode = classSource.SourceText.ToString();
        Assert.Contains("namespace TestApp.Services;", classCode);
        Assert.Contains("public class MyService(ScriptBridge invoker)", classCode);
        Assert.DoesNotContain("public partial class", classCode);
        Assert.DoesNotContain("[Inject]", classCode);
        Assert.Contains("public async Task doWork()", classCode);

        var extensionSource = generatorResult.GeneratedSources.FirstOrDefault(s => s.HintName.EndsWith("Scripts.g.cs"));
        Assert.NotNull(extensionSource.SourceText);
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static void AddScripts(IServiceCollection services)", extensionCode);
        Assert.Contains("services.AddScoped<TestApp.Services.MyService>();", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithUnsupportedTsFile_GeneratesOnlyExtension()
    {
        var generator = new ResolveGenerator();
        var tsContent = "export function greet(name: string): string { return `Hello, ${name}!`; }";
        var tsFile = CreateAdditionalText("Demo.ts", tsContent);

        var generatorResult = RunGenerator(generator, new[] { tsFile });

        Assert.Single(generatorResult.GeneratedSources);
        var extensionSource = generatorResult.GeneratedSources.Single();
        Assert.EndsWith("Scripts.g.cs", extensionSource.HintName);
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static void AddScripts(IServiceCollection services)", extensionCode);
    }

    [Fact]
    public void ResolveGenerator_WithMixedFiles_GeneratesCorrectly()
    {
        var generator = new ResolveGenerator();
        var razorContent = "export function razorFunc(): void {}";
        var entryContent = "export function entryFunc(): void {}";
        var razorFile = CreateAdditionalText("Components/MyPage.razor.ts", razorContent);
        var entryFile = CreateAdditionalText("Api/Client.entry.ts", entryContent);

        var generatorResult = RunGenerator(
            generator,
            new[] { razorFile, entryFile },
            "/test/project/",
            "MyApp",
            "namespace MyApp.Components; public partial class MyPage { }");

        Assert.Equal(3, generatorResult.GeneratedSources.Length);

        var razorSource = generatorResult.GeneratedSources.Single(s => s.HintName == "MyApp.Components.MyPage.razor.g.cs");
        var razorCode = razorSource.SourceText.ToString();
        Assert.Contains("public partial class MyPage", razorCode);
        Assert.Contains("public async Task razorFunc()", razorCode);
        Assert.Contains("public class MyPageTSInterop(ScriptBridge invoker)", razorCode);

        var entrySource = generatorResult.GeneratedSources.Single(s => s.HintName == "MyApp.Api.Client.entry.g.cs");
        var entryCode = entrySource.SourceText.ToString();
        Assert.Contains("public class Client(ScriptBridge invoker)", entryCode);
        Assert.Contains("public async Task entryFunc()", entryCode);

        var extensionSource = generatorResult.GeneratedSources.Single(s => s.HintName.EndsWith("Scripts.g.cs"));
        var extensionCode = extensionSource.SourceText.ToString();
        Assert.Contains("public static void AddScripts(IServiceCollection services)", extensionCode);
        Assert.Contains("services.AddScoped<MyApp.Api.Client>();", extensionCode);
        Assert.Contains("services.AddScoped<MyApp.Components.MyPageTSInterop>();", extensionCode);
    }
}
