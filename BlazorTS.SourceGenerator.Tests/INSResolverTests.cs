using Xunit;
using BlazorTS;

namespace BlazorTS.SourceGenerator.Tests;

public class INSResolverTests
{
    [Fact]
    public void DefaultNSResolver_WithDefaultConstructor_UsesDefaultRule()
    {
        INSResolver resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);

        var result = resolver.ResolveNS(testType, "");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
        Assert.Contains("INSResolverTests", result);
    }

    [Fact]
    public void DefaultNSResolver_WithCustomFunction_UsesCustomRule()
    {
        INSResolver customResolver = new DefaultNSResolver((fullName, suffix) => $"/custom/{fullName}{suffix}.module.js");
        var testType = typeof(TestClass);

        var result = customResolver.ResolveNS(testType, ".test");

        Assert.Equal("/custom/BlazorTS.SourceGenerator.Tests.INSResolverTests+TestClass.test.module.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithCustomFunction_UsesSameRuleForLogicalFullName()
    {
        INSResolver resolver = new DefaultNSResolver((fullName, suffix) => $"/custom/{fullName}{suffix}.module.js");

        var result = resolver.ResolveNS("MyApp.Components.Counter", ".razor");

        Assert.Equal("/custom/MyApp.Components.Counter.razor.module.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNullFunction_UsesDefaultRule()
    {
        INSResolver resolver = new DefaultNSResolver(null);
        var testType = typeof(TestClass);

        var result = resolver.ResolveNS(testType, "");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNestedClass_HandlesCorrectly()
    {
        INSResolver resolver = new DefaultNSResolver();
        var nestedType = typeof(TestClass.NestedClass);

        var result = resolver.ResolveNS(nestedType, "");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithRazorSuffix_GeneratesRazorPath()
    {
        INSResolver resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);

        var result = resolver.ResolveNS(testType, ".razor");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".razor.js", result);
        Assert.Contains("TestClass", result);
    }

    [Fact]
    public void DefaultNSResolver_WithEntrySuffix_GeneratesEntryPath()
    {
        INSResolver resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);

        var result = resolver.ResolveNS(testType, ".entry");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".entry.js", result);
        Assert.Contains("TestClass", result);
    }

    [Fact]
    public void DefaultNSResolver_WithEmptySuffix_GeneratesBasicPath()
    {
        INSResolver resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);

        var result = resolver.ResolveNS(testType, "");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
        Assert.Contains("TestClass", result);
        Assert.DoesNotContain(".razor", result);
        Assert.DoesNotContain(".entry", result);
    }

    [Fact]
    public void DefaultNSResolver_WithGenericType_RemovesGenericArityFromPath()
    {
        INSResolver resolver = new DefaultNSResolver();
        var result = resolver.ResolveNS(typeof(GenericTestClass<>), ".razor");

        Assert.DoesNotContain("`1", result);
        Assert.EndsWith("GenericTestClass.razor.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithLogicalFullName_RemovesGenericArityFromPath()
    {
        INSResolver resolver = new DefaultNSResolver();
        var result = resolver.ResolveNS("BlazorTS.SourceGenerator.Tests.GenericTestClass`1", ".razor");

        Assert.DoesNotContain("`1", result);
        Assert.EndsWith("GenericTestClass.razor.js", result);
    }

    [Fact]
    public void CustomNSResolver_ImplementsInterface_WorksCorrectly()
    {
        INSResolver customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);

        var result = customResolver.ResolveNS(testType, "");

        Assert.Equal("/test/custom/BlazorTS.SourceGenerator.Tests.INSResolverTests.TestClass.js", result);
    }

    [Fact]
    public void CustomNSResolver_WithSuffix_IncludesSuffixInPath()
    {
        INSResolver customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);

        var result = customResolver.ResolveNS(testType, ".razor");

        Assert.Equal("/test/custom/BlazorTS.SourceGenerator.Tests.INSResolverTests.TestClass.razor.js", result);
    }

    private class TestClass
    {
        public class NestedClass { }
    }

    private class GenericTestClass<T>
    {
    }

    private class CustomTestResolver : INSResolver
    {
        public string ResolveNS(string fullName, string suffix)
        {
            return $"/test/custom/{fullName.Replace("+", ".")}{suffix}.js";
        }
    }
}
