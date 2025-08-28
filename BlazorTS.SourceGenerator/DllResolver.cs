using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorTS.SourceGenerator
{
    /// <summary>
    /// DLL解析器类 - 管理Native库路径和解析
    /// </summary>
    public class DllResolver
    {
        private readonly string? _nativePath;

        /// <summary>
        /// 构造函数，接收native路径
        /// </summary>
        public DllResolver(string? nativePath = null)
        {
            _nativePath = nativePath;
        }

        /// <summary>
        /// DLL导入解析器
        /// </summary>
        public IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!string.IsNullOrEmpty(_nativePath))
            {
                // 构建可能的库文件路径
                var possiblePaths = new[]
                {
                    Path.Combine(_nativePath, $"lib{libraryName}.so"),
                    Path.Combine(_nativePath, $"{libraryName}.so"),
                    Path.Combine(_nativePath, $"lib{libraryName}.dylib"),
                    Path.Combine(_nativePath, $"{libraryName}.dylib"),
                    Path.Combine(_nativePath, $"{libraryName}.dll")
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
            // 回退到默认行为
            return IntPtr.Zero;
        }

        /// <summary>
        /// 获取TypeScript解析器Native路径 - 从build property或Compilation的AssemblyMetadata获取
        /// </summary>
        public static string? GetTypeScriptParserNativePath(Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider = null)
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