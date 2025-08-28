using BlazorTS;
using Xunit;

namespace BlazorTS.SourceGenerator.Tests;

public class MethodExtractorTests : TestBase
{
    [Fact]
    public void Extract_ValidTypeScript_ReturnsFunctions()
    {
        // Arrange
        var code = @"
export function add(a: number, b: number): number {
    return a + b;
}

export async function fetchData(url: string): Promise<any> {
    return fetch(url);
}";

        // Act
        var functions = MethodExtractor.Extract(code);

        // Assert
        Assert.Equal(2, functions.Count());
        
        var addFunc = functions.First(f => f.Name == "add");
        AssertFunction(addFunc, "add", "number", false, 2);
        
        var fetchFunc = functions.First(f => f.Name == "fetchData");
        AssertFunction(fetchFunc, "fetchData", "Promise<any>", true, 1);
    }

    [Fact]
    public void Extract_EmptyCode_ReturnsEmpty()
    {
        // Act & Assert
        Assert.Empty(MethodExtractor.Extract(""));
        Assert.Empty(MethodExtractor.Extract("   "));
        Assert.Empty(MethodExtractor.Extract("\n\t  "));
    }

    [Fact]
    public void Extract_NullCode_ReturnsEmpty()
    {
        // Act & Assert
        Assert.Empty(MethodExtractor.Extract(null));
    }

    [Fact]
    public void Extract_CodeWithoutFunctions_ReturnsEmpty()
    {
        // Arrange
        var code = @"
const message = 'Hello, World!';
let count = 42;
var isEnabled = true;
";

        // Act
        var functions = MethodExtractor.Extract(code);

        // Assert
        Assert.Empty(functions);
    }


    [Fact]
    public void Extract_ComplexTypeScript_HandlesAllFeatures()
    {
        // Arrange
        var code = @"
function simple(): void {
    console.log('simple');
}

async function asyncFunc(url: string): Promise<Response> {
    return fetch(url);
}

function withOptional(required: string, optional?: number): boolean {
    return true;
}

function multipleParams(a: number, b: string, c: boolean, d: any): void {
}
";

        // Act
        var functions = MethodExtractor.Extract(code);

        // Assert
        Assert.Equal(4, functions.Count());
        
        // Check simple function
        var simple = functions.First(f => f.Name == "simple");
        AssertFunction(simple, "simple", "void", false, 0);
        
        // Check async function
        var asyncFunc = functions.First(f => f.Name == "asyncFunc");
        AssertFunction(asyncFunc, "asyncFunc", "Promise<Response>", true, 1);
        AssertParameter(asyncFunc.Parameters[0], "url", "string", false);
        
        // Check function with optional parameter
        var withOptional = functions.First(f => f.Name == "withOptional");
        AssertFunction(withOptional, "withOptional", "boolean", false, 2);
        AssertParameter(withOptional.Parameters[0], "required", "string", false);
        AssertParameter(withOptional.Parameters[1], "optional", "number", true);
        
        // Check function with multiple parameters
        var multipleParams = functions.First(f => f.Name == "multipleParams");
        AssertFunction(multipleParams, "multipleParams", "void", false, 4);
        AssertParameter(multipleParams.Parameters[0], "a", "number", false);
        AssertParameter(multipleParams.Parameters[1], "b", "string", false);
        AssertParameter(multipleParams.Parameters[2], "c", "boolean", false);
        AssertParameter(multipleParams.Parameters[3], "d", "any", false);
    }

    [Fact]
    public void Extract_ExportedFunctions_ExtractsCorrectly()
    {
        // Arrange
        var code = @"
export function exportedFunction(param: string): string {
    return param.toUpperCase();
}

function nonExportedFunction(): void {
    console.log('private');
}

export async function exportedAsync(): Promise<void> {
    await delay(1000);
}
";

        // Act
        var functions = MethodExtractor.Extract(code);

        // Assert
        Assert.Equal(3, functions.Count());
        Assert.Contains(functions, f => f.Name == "exportedFunction");
        Assert.Contains(functions, f => f.Name == "nonExportedFunction");
        Assert.Contains(functions, f => f.Name == "exportedAsync");
    }

}