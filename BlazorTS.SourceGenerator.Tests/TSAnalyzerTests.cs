using BlazorTS;
using Xunit;

namespace BlazorTS.SourceGenerator.Tests;

public class TSAnalyzerTests : TestBase, IDisposable
{
    private readonly TSAnalyzer _analyzer = new();

    [Fact]
    public void ExtractFunctions_SimpleFunction_ReturnsCorrectFunction()
    {
        // Arrange
        var code = "function greet(name: string): string { return `Hello, ${name}!`; }";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        var func = Assert.Single(functions);
        AssertFunction(func, "greet", "string", false, 1);
        AssertParameter(func.Parameters[0], "name", "string", false);
    }

    [Fact]
    public void ExtractFunctions_AsyncFunction_ReturnsAsyncFunction()
    {
        // Arrange
        var code = "async function fetchData(url: string): Promise<any> { return fetch(url); }";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        var func = Assert.Single(functions);
        AssertFunction(func, "fetchData", "Promise<any>", true, 1);
        AssertParameter(func.Parameters[0], "url", "string", false);
    }

    [Fact]
    public void ExtractFunctions_MultipleParameters_ParsesAllParameters()
    {
        // Arrange
        var code = "function add(a: number, b: number, c: number): number { return a + b + c; }";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        var func = Assert.Single(functions);
        AssertFunction(func, "add", "number", false, 3);
        AssertParameter(func.Parameters[0], "a", "number", false);
        AssertParameter(func.Parameters[1], "b", "number", false);
        AssertParameter(func.Parameters[2], "c", "number", false);
    }

    [Fact]
    public void ExtractFunctions_VoidFunction_ReturnsVoidType()
    {
        // Arrange
        var code = "function initialize(): void { console.log('init'); }";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        var func = Assert.Single(functions);
        AssertFunction(func, "initialize", "void", false, 0);
    }

    [Fact]
    public void ExtractFunctions_FunctionWithoutReturnType_DefaultsToVoid()
    {
        // Arrange
        var code = "function test() { console.log('test'); }";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        var func = Assert.Single(functions);
        AssertFunction(func, "test", "void", false, 0);
    }

    [Fact]
    public void ExtractFunctions_OptionalParameters_ParsesOptionalFlags()
    {
        // Arrange
        var code = "function process(data: string, timeout?: number): boolean { return true; }";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        var func = Assert.Single(functions);
        AssertFunction(func, "process", "boolean", false, 2);
        AssertParameter(func.Parameters[0], "data", "string", false);
        AssertParameter(func.Parameters[1], "timeout", "number", true);
    }

    [Fact]
    public void ExtractFunctions_MultipleFunctions_ExtractsAll()
    {
        // Arrange
        var code = @"
function first(): string { return 'first'; }
function second(x: number): number { return x * 2; }
function third(): void { }
";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        Assert.Equal(3, functions.Count);
        
        AssertFunction(functions[0], "first", "string", false, 0);
        AssertFunction(functions[1], "second", "number", false, 1);
        AssertParameter(functions[1].Parameters[0], "x", "number", false);
        AssertFunction(functions[2], "third", "void", false, 0);
    }

    [Fact]
    public void ExtractFunctions_EmptyCode_ReturnsEmptyList()
    {
        // Arrange
        var code = "";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        Assert.Empty(functions);
    }

    [Fact]
    public void ExtractFunctions_CodeWithoutFunctions_ReturnsEmptyList()
    {
        // Arrange
        var code = "const x = 42; let y = 'hello';";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        Assert.Empty(functions);
    }

    [Fact]
    public void ExtractFunctions_BasicTypes_ParsesCorrectly()
    {
        // Arrange
        var code = @"
function testString(s: string): string { return s; }
function testNumber(n: number): number { return n; }
function testBoolean(b: boolean): boolean { return b; }
function testAny(a: any): any { return a; }
";

        // Act
        var functions = _analyzer.ExtractFunctions(code);

        // Assert
        Assert.Equal(4, functions.Count);
        
        AssertFunction(functions[0], "testString", "string", false, 1);
        AssertParameter(functions[0].Parameters[0], "s", "string", false);
        
        AssertFunction(functions[1], "testNumber", "number", false, 1);
        AssertParameter(functions[1].Parameters[0], "n", "number", false);
        
        AssertFunction(functions[2], "testBoolean", "boolean", false, 1);
        AssertParameter(functions[2].Parameters[0], "b", "boolean", false);
        
        AssertFunction(functions[3], "testAny", "any", false, 1);
        AssertParameter(functions[3].Parameters[0], "a", "any", false);
    }

    public void Dispose()
    {
        _analyzer?.Dispose();
    }
}