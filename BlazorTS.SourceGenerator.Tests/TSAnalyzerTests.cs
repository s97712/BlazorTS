using BlazorTS;
using Xunit;

namespace BlazorTS.SourceGenerator.Tests;

public class TSAnalyzerTests : TestBase, IDisposable
{
    private readonly TSAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_Interface_ReturnsProperties()
    {
        var module = _analyzer.Analyze(@"
export interface Address { city: string; zip?: number; }
export interface User { name: string; address: Address; tags: string[]; }
export function getUser(): Promise<User> { throw new Error(); }");

        Assert.Equal(2, module.Interfaces.Count);
        var address = module.Interfaces.Single(item => item.Name == "Address");
        AssertParameter(new TSParameter { Name = address.Properties[0].Name, Type = address.Properties[0].Type }, "city", "string");
        Assert.True(address.Properties.Single(item => item.Name == "zip").IsOptional);
        Assert.Equal("Address", module.Interfaces.Single(item => item.Name == "User").Properties.Single(item => item.Name == "address").Type);
        Assert.Single(module.Functions);
        Assert.Equal("Promise<User>", module.Functions[0].ReturnType);
    }

    [Fact]
    public void Analyze_PromiseReturnTypes_ExtractsCorrectly()
    {
        var code = @"
export async function fetchVoid(): Promise<void> {}
export async function fetchText(): Promise<string> { return 'ok'; }
export async function fetchNumber(): Promise<number> { return 42; }
export async function fetchItems(): Promise<string[]> { return []; }
export async function fetchList(): Promise<Array<number>> { return []; }
export const fetchArrow = async (): Promise<boolean> => true;
";
        var functions = _analyzer.Analyze(code).Functions;

        Assert.Equal(6, functions.Count);
        AssertFunction(functions[0], "fetchVoid", "Promise<void>", true, 0);
        AssertFunction(functions[1], "fetchText", "Promise<string>", true, 0);
        AssertFunction(functions[2], "fetchNumber", "Promise<number>", true, 0);
        AssertFunction(functions[3], "fetchItems", "Promise<string[]>", true, 0);
        AssertFunction(functions[4], "fetchList", "Promise<Array<number>>", true, 0);
        AssertFunction(functions[5], "fetchArrow", "Promise<boolean>", true, 0);
    }

    [Fact]
    public void ExtractFunctions_SimpleFunction_ReturnsCorrectFunction()
    {
        var code = "export function greet(name: string): string { return `Hello, ${name}!`; }";

        var functions = _analyzer.Analyze(code).Functions;

        var func = Assert.Single(functions);
        AssertFunction(func, "greet", "string", false, 1);
        AssertParameter(func.Parameters[0], "name", "string", false);
    }

    [Fact]
    public void ExtractFunctions_AsyncFunction_ReturnsAsyncFunction()
    {
        var code = "export async function fetchData(url: string): Promise<any> { return fetch(url); }";

        var functions = _analyzer.Analyze(code).Functions;

        var func = Assert.Single(functions);
        AssertFunction(func, "fetchData", "Promise<any>", true, 1);
        AssertParameter(func.Parameters[0], "url", "string", false);
    }

    [Fact]
    public void ExtractFunctions_MultipleParameters_ParsesAllParameters()
    {
        var code = "export function add(a: number, b: number, c: number): number { return a + b + c; }";

        var functions = _analyzer.Analyze(code).Functions;

        var func = Assert.Single(functions);
        AssertFunction(func, "add", "number", false, 3);
        AssertParameter(func.Parameters[0], "a", "number", false);
        AssertParameter(func.Parameters[1], "b", "number", false);
        AssertParameter(func.Parameters[2], "c", "number", false);
    }

    [Fact]
    public void ExtractFunctions_VoidFunction_ReturnsVoidType()
    {
        var code = "export function initialize(): void { console.log('init'); }";

        var functions = _analyzer.Analyze(code).Functions;

        var func = Assert.Single(functions);
        AssertFunction(func, "initialize", "void", false, 0);
    }

    [Fact]
    public void ExtractFunctions_FunctionWithoutReturnType_DefaultsToVoid()
    {
        var code = "export function test() { console.log('test'); }";

        var functions = _analyzer.Analyze(code).Functions;

        var func = Assert.Single(functions);
        AssertFunction(func, "test", "void", false, 0);
    }

    [Fact]
    public void ExtractFunctions_OptionalParameters_ParsesOptionalFlags()
    {
        var code = "export function process(data: string, timeout?: number): boolean { return true; }";

        var functions = _analyzer.Analyze(code).Functions;

        var func = Assert.Single(functions);
        AssertFunction(func, "process", "boolean", false, 2);
        AssertParameter(func.Parameters[0], "data", "string", false);
        AssertParameter(func.Parameters[1], "timeout", "number", true);
    }

    [Fact]
    public void ExtractFunctions_MultipleFunctions_ExtractsAll()
    {
        var code = @"
export function first(): string { return 'first'; }
export function second(x: number): number { return x * 2; }
export function third(): void { }
";

        var functions = _analyzer.Analyze(code).Functions;

        Assert.Equal(3, functions.Count);
        AssertFunction(functions[0], "first", "string", false, 0);
        AssertFunction(functions[1], "second", "number", false, 1);
        AssertParameter(functions[1].Parameters[0], "x", "number", false);
        AssertFunction(functions[2], "third", "void", false, 0);
    }

    [Fact]
    public void ExtractFunctions_EmptyCode_ReturnsEmptyList()
    {
        var code = "";

        var functions = _analyzer.Analyze(code).Functions;

        Assert.Empty(functions);
    }

    [Fact]
    public void ExtractFunctions_CodeWithoutFunctions_ReturnsEmptyList()
    {
        var code = "const x = 42; let y = 'hello';";

        var functions = _analyzer.Analyze(code).Functions;

        Assert.Empty(functions);
    }

    [Fact]
    public void ExtractFunctions_BasicTypes_ParsesCorrectly()
    {
        var code = @"
export function testString(s: string): string { return s; }
export function testNumber(n: number): number { return n; }
export function testBoolean(b: boolean): boolean { return b; }
export function testAny(a: any): any { return a; }
";

        var functions = _analyzer.Analyze(code).Functions;

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
