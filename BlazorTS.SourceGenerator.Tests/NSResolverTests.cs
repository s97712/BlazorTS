using BlazorTS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorTS.SourceGenerator.Tests;

public class NSResolverTests
{
    [Fact]
    public void NSResolver_ThrowsWhenNoResolversConfigured()
    {
        NSResolver resolver = new NSResolver([]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.ResolveNS("BlazorTS.SourceGenerator.Tests.TestClass", ""));

        Assert.Contains("No resolver matched module 'BlazorTS.SourceGenerator.Tests.TestClass' with suffix ''.", exception.Message);
    }

    [Fact]
    public void NSResolver_ThrowsWhenAllResolversReturnNull()
    {
        NSResolver resolver = new NSResolver(
        [
            (_, suffix) => suffix == ".razor" ? "/components/Counter.razor.js" : null
        ]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.ResolveNS("MyApp.Services.Client", ".entry"));

        Assert.Contains("No resolver matched module 'MyApp.Services.Client' with suffix '.entry'.", exception.Message);
    }

    [Fact]
    public void NSResolver_TypeOverloadForwardsToUnderlyingResolver()
    {
        NSResolver resolver = new NSResolver(
        [
            (fullName, suffix) => $"/modules/{fullName}{suffix}.js"
        ]);

        var result = resolver.ResolveNS(typeof(TestClass), ".entry");

        Assert.Equal($"/modules/{typeof(TestClass).FullName}.entry.js", result);
    }

    [Fact]
    public void NSResolver_UsesFirstResolverReturningPath()
    {
        var resolver = new NSResolver(
        [
            (fullName, suffix) => suffix == ".razor"
                ? $"/components/{fullName}{suffix}.js"
                : null,
            (fullName, suffix) => $"/modules/{fullName}{suffix}.js"
        ]);

        var result = resolver.ResolveNS("MyApp.Components.Counter", ".razor");

        Assert.Equal("/components/MyApp.Components.Counter.razor.js", result);
    }

    [Fact]
    public void NSResolver_UsesNextResolverWhenPreviousReturnsNull()
    {
        var resolver = new NSResolver(
        [
            (fullName, suffix) => fullName.StartsWith("MyApp.Components.")
                ? $"/components/{fullName}{suffix}.js"
                : null,
            (fullName, suffix) => $"/modules/{fullName}{suffix}.js"
        ]);

        var result = resolver.ResolveNS("MyApp.Services.Client", ".entry");

        Assert.Equal("/modules/MyApp.Services.Client.entry.js", result);
    }

    [Fact]
    public void AddBlazorTSResolver_RegistersResolversInOrder()
    {
        var services = new ServiceCollection();
        services.AddBlazorTSResolver((fullName, suffix) =>
            suffix == ".razor" ? $"/components/{fullName}{suffix}.js" : null);
        services.AddBlazorTSResolver((fullName, suffix) =>
            $"/modules/{fullName}{suffix}.js");

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<NSResolver>();

        Assert.Equal(
            "/components/MyApp.Components.Counter.razor.js",
            resolver.ResolveNS("MyApp.Components.Counter", ".razor"));
        Assert.Equal(
            "/modules/MyApp.Services.Client.entry.js",
            resolver.ResolveNS("MyApp.Services.Client", ".entry"));
    }

    private sealed class TestClass
    {
    }
}
