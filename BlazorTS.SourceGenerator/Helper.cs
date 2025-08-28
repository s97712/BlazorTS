using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorTS.SourceGenerator
{
    public static class Helper
    {
        private static readonly DiagnosticDescriptor LogDescriptor = new DiagnosticDescriptor(
            id: "BLAZORTS_LOG",
            title: "BlazorTS日志",
            messageFormat: "{0}",
            category: "BlazorTS",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: IsLogEnabled()
        );

        private static bool IsLogEnabled()
        {
            // 通过环境变量控制
            var envValue = Environment.GetEnvironmentVariable("BLAZORTS_LOG_ENABLED");
            if (bool.TryParse(envValue, out var enabled))
                return enabled;
            
            // 调试模式下默认启用
            return Debugger.IsAttached;
        }


        /// <summary>
        /// 使用诊断报告记录日志（推荐方式）- SourceProductionContext版本
        /// </summary>
        public static void Log(SourceProductionContext context, string message)
        {
            context.ReportDiagnostic(Diagnostic.Create(LogDescriptor, Location.None, message));
        }

        /// <summary>
        /// 等待调试器附加
        /// </summary>
        public static bool WaitForDebugger(TimeSpan? limit = null)
        {
            limit ??= TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();

            var processId = Process.GetCurrentProcess().Id;
            var message = $"◉ Waiting {limit.Value.TotalSeconds} secs for debugger (PID: {processId})...";

            Console.WriteLine(message);

            // 将进程ID写入文件
            try
            {
                var pidFile = Path.Combine(Path.GetTempPath(), "blazor_sourcegen_debug.pid");
                File.WriteAllText(pidFile, processId.ToString());
            }
            catch
            {
                // 忽略文件写入错误
            }

            try
            {
                while (!Debugger.IsAttached && stopwatch.Elapsed < limit.Value)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception)
            {
                // it's ok
            }

            Console.WriteLine(Debugger.IsAttached
                ? "✔ Debugger attached"
                : "✕ Continuing without debugger");

            return Debugger.IsAttached;
        }


        /// <summary>
        /// 获取TypeScript解析器Native路径 - 从build property或Compilation的AssemblyMetadata获取
        /// </summary>
        public static string GetTypeScriptParserNativePath(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider = null)
        {
            try
            {
                // 先尝试从build property获取
                if (optionsProvider?.GlobalOptions?.TryGetValue("build_property.TypeScriptParserNativePath", out var buildPath) == true &&
                    !string.IsNullOrEmpty(buildPath))
                {
                    return buildPath;
                }
                
                // 如果没有，再尝试从Assembly级别的MetadataAttribute获取
                var assemblyAttributes = compilation.Assembly.GetAttributes();
                
                foreach (var attr in assemblyAttributes)
                {
                    if (attr.AttributeClass?.Name == "AssemblyMetadataAttribute" &&
                        attr.ConstructorArguments.Length == 2)
                    {
                        var key = attr.ConstructorArguments[0].Value?.ToString();
                        var value = attr.ConstructorArguments[1].Value?.ToString();
                        
                        if (key == "TypeScriptParserNativePath")
                        {
                            return value;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略异常，返回null
            }

            return null;
        }



    }
}