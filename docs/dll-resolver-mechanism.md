# BlazorTS - DLL 路径解析机制

## 工作机制

`BlazorTS.SourceGenerator` 依赖 `TypeScriptParser` 的原生库来解析 TypeScript 语法树。

整体机制如下：

1. `TypeScriptParser` 包通过 `buildTransitive` 将原生库路径传递给消费项目。
2. 路径被写入消费项目的构建元数据中。
3. `BlazorTS.SourceGenerator` 在编译时读取该路径，并为 `TypeScriptParser` 注册原生库解析器。

## 路径来源

`DllResolver.GetTypeScriptParserNativePath()` 按以下优先级获取原生库目录：

1. **构建属性**：例如测试时显式传入的 `TypeScriptParserNativePath`
2. **程序集元数据属性**：通过 `buildTransitive` 自动注入到目标项目

## 解析逻辑

`DllResolver.Resolve()` 按平台约定尝试加载原生库：

- Linux：`lib{libraryName}.so` 或 `{libraryName}.so`
- macOS：`lib{libraryName}.dylib` 或 `{libraryName}.dylib`
- Windows：`{libraryName}.dll`

## 与当前生成模型的关系

当前生成模型中：

- `.razor.ts` 对应组件 `partial class` 与顶层 `*TSInterop` 互操作类
- `.entry.ts` 对应普通可注入服务类

无论是 Razor 组件脚本还是 entry 模块，TypeScript 函数提取都依赖同一套原生解析器链路，因此 DLL 路径解析机制仍然是所有生成结果的统一前置条件。

## 路径解析与模块 URL 的边界

需要区分两类“路径解析”：

1. **原生 DLL 路径解析**：供 `TypeScriptParser` 原生库加载使用
2. **模块 URL 路径解析**：供运行时 `ResolveNS(...)` 计算 `/js/...` 模块地址使用

二者职责完全不同：

- DLL 路径解析发生在编译期，用于启用 TypeScript AST 分析
- 模块 URL 路径解析发生在运行时，用于定位编译后的 JavaScript 模块
