using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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
            if (!string.IsNullOrEmpty(_nativePath) && _nativePath != "未找到")
            {
                // 构建可能的库文件路径
                var possiblePaths = new[]
                {
                    Path.Combine(_nativePath, $"lib{libraryName}.so"),
                    Path.Combine(_nativePath, $"{libraryName}.so"),
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
    }
}