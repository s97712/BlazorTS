# BlazorTS - DLL路径解析机制

## 工作机制

- TypeScriptParser包通过`buildTransitive`机制将Native库路径传递到目标项目
- 路径被写入目标项目编译后程序集的`AssemblyMetadataAttribute`中
- BlazorTS.SourceGenerator从目标项目的程序集元数据中读取路径

## 路径分配来源

[`DllResolver.GetTypeScriptParserNativePath()`](BlazorTS.SourceGenerator/DllResolver.cs)按以下优先级获取路径：

1. **构建属性** - 测试时手动设置的MSBuild属性
2. **程序集元数据属性** - 通过buildTransitive传递到目标项目

## 解析逻辑

[`DllResolver.Resolve()`](BlazorTS.SourceGenerator/DllResolver.cs)按平台约定尝试加载：
- Linux: `lib{libraryName}.so` 或 `{libraryName}.so`
- Windows: `{libraryName}.dll`