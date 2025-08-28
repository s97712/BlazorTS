using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;

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
        /// 设置Native库解析器，重定向到源生成器程序集目录
        /// </summary>
        public static void SetupNativeLibraryResolver()
        {
#if NET5_0_OR_GREATER
            NativeLibrary.SetDllImportResolver(typeof(TypeScriptParser.Parser).Assembly, DllImportResolver);
#endif
        }

        /// <summary>
        /// DLL导入解析器
        /// </summary>
        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
#if NET5_0_OR_GREATER
            var assemblyDir = GetTypeScriptParserNativePath();

            if (!string.IsNullOrEmpty(assemblyDir))
            {
                // 构建可能的库文件路径
                var possiblePaths = new[]
                {
                    Path.Combine(assemblyDir, $"lib{libraryName}.so"),
                    Path.Combine(assemblyDir, $"{libraryName}.so"),
                    Path.Combine(assemblyDir, $"{libraryName}.dll")
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        return NativeLibrary.Load(path);
                    }
                    catch
                    {
                        // 忽略加载失败，继续尝试下一个路径
                    }
                }
            }
#endif

            // 回退到默认行为
            return IntPtr.Zero;
        }

        /// <summary>
        /// 获取TypeScript解析器Native路径
        /// </summary>
        public static string GetTypeScriptParserNativePath()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var nativePath = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attr => attr.Key == "TypeScriptParserNativePath")?.Value ?? "未找到";

            return nativePath;
        }



    }
}