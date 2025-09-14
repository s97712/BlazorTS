using Xunit;
using BlazorTS;
using System.Reflection;

namespace BlazorTS.SourceGenerator.Tests;

public class INSResolverTests
{
    [Fact]
    public void DefaultNSResolver_WithDefaultConstructor_UsesDefaultRule()
    {
        // Arrange
        var resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = resolver.ResolveNS(testType, "");
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
        Assert.Contains("INSResolverTests", result);
    }

    [Fact]
    public void DefaultNSResolver_WithCustomFunction_UsesCustomRule()
    {
        // Arrange
        var customResolver = new DefaultNSResolver((type, suffix) => $"/custom/{type.Name}{suffix}.module.js");
        var testType = typeof(TestClass);
        
        // Act
        var result = customResolver.ResolveNS(testType, ".test");
        
        // Assert
        Assert.Equal("/custom/TestClass.test.module.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNullFunction_UsesDefaultRule()
    {
        // Arrange
        var resolver = new DefaultNSResolver(null);
        var testType = typeof(TestClass);
        
        // Act
        var result = resolver.ResolveNS(testType, "");
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNestedClass_HandlesCorrectly()
    {
        // Arrange
        var resolver = new DefaultNSResolver();
        var nestedType = typeof(TestClass.NestedClass);
        
        // Act
        var result = resolver.ResolveNS(nestedType, "");
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithRazorSuffix_GeneratesRazorPath()
    {
        // Arrange
        var resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = resolver.ResolveNS(testType, ".razor");
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".razor.js", result);
        Assert.Contains("TestClass", result);
    }

    [Fact]
    public void DefaultNSResolver_WithEntrySuffix_GeneratesEntryPath()
    {
        // Arrange
        var resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = resolver.ResolveNS(testType, ".entry");
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".entry.js", result);
        Assert.Contains("TestClass", result);
    }

    [Fact]
    public void DefaultNSResolver_WithEmptySuffix_GeneratesBasicPath()
    {
        // Arrange
        var resolver = new DefaultNSResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = resolver.ResolveNS(testType, "");
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
        Assert.Contains("TestClass", result);
        Assert.DoesNotContain(".razor", result);
        Assert.DoesNotContain(".entry", result);
    }

    [Fact]
    public void CustomNSResolver_ImplementsInterface_WorksCorrectly()
    {
        // Arrange
        var customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = customResolver.ResolveNS(testType, "");
        
        // Assert
        Assert.Equal("/test/custom/TestClass.js", result);
    }

    [Fact]
    public void CustomNSResolver_WithSuffix_IncludesSuffixInPath()
    {
        // Arrange
        var customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = customResolver.ResolveNS(testType, ".razor");
        
        // Assert
        Assert.Equal("/test/custom/TestClass.razor.js", result);
    }

    private class TestClass
    {
        public class NestedClass { }
    }

    private class CustomTestResolver : INSResolver
    {
        public string ResolveNS(Type tsType, string suffix)
        {
            return $"/test/custom/{tsType.Name}{suffix}.js";
        }
    }
}