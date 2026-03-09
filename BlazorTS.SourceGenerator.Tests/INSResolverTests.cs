using Xunit;
using BlazorTS;

namespace BlazorTS.SourceGenerator.Tests;

public class INSResolverTests
{
    [Fact]
    public void DefaultNSResolver_WithDefaultConstructor_UsesDefaultRule()
    {
        var resolver = new DefaultNSResolver();
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
        var customResolver = new DefaultNSResolver((type, suffix) => $"/custom/{type.Name}{suffix}.module.js");
        var testType = typeof(TestClass);

        var result = customResolver.ResolveNS(testType, ".test");

        Assert.Equal("/custom/TestClass.test.module.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNullFunction_UsesDefaultRule()
    {
        var resolver = new DefaultNSResolver(null);
        var testType = typeof(TestClass);

        var result = resolver.ResolveNS(testType, "");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNestedClass_HandlesCorrectly()
    {
        var resolver = new DefaultNSResolver();
        var nestedType = typeof(TestClass.NestedClass);

        var result = resolver.ResolveNS(nestedType, "");

        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithRazorSuffix_GeneratesRazorPath()
    {
        var resolver = new DefaultNSResolver();
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
        var resolver = new DefaultNSResolver();
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
        var resolver = new DefaultNSResolver();
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
        var resolver = new DefaultNSResolver();
        var result = resolver.ResolveNS(typeof(GenericTestClass<>), ".razor");

        Assert.DoesNotContain("`1", result);
        Assert.EndsWith("GenericTestClass.razor.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithLogicalFullName_RemovesGenericArityFromPath()
    {
        var resolver = new DefaultNSResolver();
        var result = resolver.ResolveNS("BlazorTS.SourceGenerator.Tests.GenericTestClass`1", ".razor");

        Assert.DoesNotContain("`1", result);
        Assert.EndsWith("GenericTestClass.razor.js", result);
    }

    [Fact]
    public void CustomNSResolver_ImplementsInterface_WorksCorrectly()
    {
        var customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);

        var result = customResolver.ResolveNS(testType, "");

        Assert.Equal("/test/custom/TestClass.js", result);
    }

    [Fact]
    public void CustomNSResolver_WithSuffix_IncludesSuffixInPath()
    {
        var customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);

        var result = customResolver.ResolveNS(testType, ".razor");

        Assert.Equal("/test/custom/TestClass.razor.js", result);
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
        public string ResolveNS(Type tsType, string suffix)
        {
            return $"/test/custom/{tsType.Name}{suffix}.js";
        }

        public string ResolveNS(string fullName, string suffix)
        {
            return $"/test/custom/{fullName}{suffix}.js";
        }
    }
}
