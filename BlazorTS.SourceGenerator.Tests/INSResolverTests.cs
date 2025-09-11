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
        var result = resolver.ResolveNS(testType);
        
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
        var customResolver = new DefaultNSResolver(type => $"/custom/{type.Name}.module.js");
        var testType = typeof(TestClass);
        
        // Act
        var result = customResolver.ResolveNS(testType);
        
        // Assert
        Assert.Equal("/custom/TestClass.module.js", result);
    }

    [Fact]
    public void DefaultNSResolver_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultNSResolver(null!));
    }

    [Fact]
    public void DefaultNSResolver_WithNestedClass_HandlesCorrectly()
    {
        // Arrange
        var resolver = new DefaultNSResolver();
        var nestedType = typeof(TestClass.NestedClass);
        
        // Act
        var result = resolver.ResolveNS(nestedType);
        
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("/js/", result);
        Assert.EndsWith(".js", result);
    }

    [Fact]
    public void CustomNSResolver_ImplementsInterface_WorksCorrectly()
    {
        // Arrange
        var customResolver = new CustomTestResolver();
        var testType = typeof(TestClass);
        
        // Act
        var result = customResolver.ResolveNS(testType);
        
        // Assert
        Assert.Equal("/test/custom/TestClass.js", result);
    }

    private class TestClass
    {
        public class NestedClass { }
    }

    private class CustomTestResolver : INSResolver
    {
        public string ResolveNS(Type tsType)
        {
            return $"/test/custom/{tsType.Name}.js";
        }
    }
}