using System;
using System.IO;
using BlazorTS;

namespace BlazorTS.SourceGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            // 测试TSAnalyzer
            using var analyzer = new TSAnalyzer();
            
            // 读取Demo.ts文件进行测试
            var demoTsPath = "../web/Components/Pages/Demo.ts";
            if (File.Exists(demoTsPath))
            {
                var code = File.ReadAllText(demoTsPath);
                Console.WriteLine("=== 分析 Demo.ts ===");
                TestFunctionExtraction(analyzer, code);
            }
            
            // 读取Counter.ts文件进行测试
            var counterTsPath = "../web/Components/Pages/Counter.ts";
            if (File.Exists(counterTsPath))
            {
                var code = File.ReadAllText(counterTsPath);
                Console.WriteLine("\n=== 分析 Counter.ts ===");
                TestFunctionExtraction(analyzer, code);
            }
            
            // 测试内联代码
            Console.WriteLine("\n=== 分析内联代码 ===");
            var inlineCode = @"
export async function testAsync(name: string, count?: number = 10): Promise<string> {
    return `Hello ${name}, count: ${count}`;
}

function regularFunction(x: number, y: boolean): void {
    console.log(x, y);
}
";
            TestFunctionExtraction(analyzer, inlineCode);
        }
        
        static void TestFunctionExtraction(TSAnalyzer analyzer, string code)
        {
            try
            {
                var functions = analyzer.ExtractFunctions(code);
                Console.WriteLine($"找到 {functions.Count} 个函数:");
                
                foreach (var func in functions)
                {
                    Console.WriteLine($"\n函数名: {func.Name}");
                    Console.WriteLine($"返回类型: {func.ReturnType}");
                    Console.WriteLine($"异步: {func.IsAsync}");
                    Console.WriteLine($"参数数量: {func.Parameters.Count}");
                    
                    foreach (var param in func.Parameters)
                    {
                        Console.WriteLine($"  - {param.Name}: {param.Type}" + 
                            (param.IsOptional ? " (可选)" : "") +
                            (!string.IsNullOrEmpty(param.DefaultValue) ? $" = {param.DefaultValue}" : ""));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析错误: {ex.Message}");
            }
        }
    }
}