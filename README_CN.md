# BlazorTS

🚀 让 Blazor 与 TypeScript 无缝协作的工具

<div align="center">

[English](README.md) | [中文](README_CN.md) | [🏠 GitHub](https://github.com/s97712/BlazorTS)

</div>

---

BlazorTS 是一个源代码生成器库，基于 **Tree-sitter 语法树解析** 分析 TypeScript 代码，自动生成 C# 包装代码，让你可以在 Blazor 应用中直接调用 TypeScript 函数，无需手动编写 JavaScript 互操作代码。

## ✨ 特性

- 🔄 **编译时生成**: 基于 Tree-sitter 语法树解析，从 TypeScript 文件自动生成 C# 包装代码
- 🎯 **类型安全**: 完整的类型映射和编译时检查
- 🚀 **零配置**: 最小化配置，开箱即用
- 🔧 **智能依赖**: 自动解析和注册服务
- 🌳 **精确解析**: 使用 Tree-sitter 精确解析 TypeScript 语法结构

## � 安装

**核心库**

| NuGet 包 | NuGet 版本 | 描述 |
|--|--|--|
| [`BlazorTS`](https://www.nuget.org/packages/BlazorTS) | [![NuGet](https://img.shields.io/nuget/v/BlazorTS.svg?style=flat)](https://www.nuget.org/packages/BlazorTS) | 核心运行时库 |
| [`BlazorTS.SourceGenerator`](https://www.nuget.org/packages/BlazorTS.SourceGenerator) | [![NuGet](https://img.shields.io/nuget/v/BlazorTS.SourceGenerator.svg?style=flat)](https://www.nuget.org/packages/BlazorTS.SourceGenerator) | 源代码生成器库 |
| [`Microsoft.TypeScript.MSBuild`](https://www.nuget.org/packages/Microsoft.TypeScript.MSBuild) | [![NuGet](https://img.shields.io/nuget/v/Microsoft.TypeScript.MSBuild.svg?style=flat)](https://www.nuget.org/packages/Microsoft.TypeScript.MSBuild) | TypeScript 编译支持 |

### 安装与配置

**1. 安装 NuGet 包**

**使用 .NET CLI 安装**
```bash
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
dotnet add package Microsoft.TypeScript.MSBuild
```

**使用 Package Manager Console 安装**
```powershell
Install-Package BlazorTS
Install-Package BlazorTS.SourceGenerator
Install-Package Microsoft.TypeScript.MSBuild
```

**2. 配置项目文件**

在 `.csproj` 文件中添加以下配置，以确保 TypeScript 文件被正确处理：

```xml
<!-- 添加 TypeScript 文件为附加文件, 并排除 node_modules -->
<ItemGroup>
  <AdditionalFiles Include="**/*.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

## 🚀 快速开始：将 TypeScript 模块绑定到 Razor 组件

BlazorTS 的核心优势在于能够将一个 TypeScript 文件无缝地“绑定”到一个 Razor 组件上，作为其专属的脚本模块。这是通过**文件命名约定**和 **partial class** 实现的。

### 1. 创建组件及其 TypeScript 模块

假设我们有一个 `Counter` 组件。

**`Components/Pages/Counter.razor`**
```csharp
@page "/counter"
@rendermode InteractiveServer

@* 将这个组件声明为 partial class，以便与生成的代码合并 *@
@code {
    public partial class Counter
    {
        private int currentCount = 0;

        private async Task HandleClick()
        {
            // 直接调用由 BlazorTS 注入的 Scripts 属性
            currentCount = await Scripts.IncrementCount(currentCount);
        }
    }
}
```

**`Components/Pages/Counter.ts`**
创建一个与 Razor 组件同名的 TypeScript 文件。
```typescript
// 这个文件是 Counter.razor 组件的专属模块
export function IncrementCount(count: number): number {
    console.log("Incrementing count from TypeScript module!");
    return count + 1;
}
```

### 2. 配置 `tsconfig.json`

为了让 Blazor 能够找到编译后的 JS 文件，我们需要配置 `tsconfig.json` 以保留目录结构。

```json
{
  "compilerOptions": {
    "noImplicitAny": false,
    "noEmitOnError": true,
    "removeComments": false,
    "target": "es2015",
    // "rootDir" 和 "outDir" 配合使用，以在输出目录中保留源目录结构
    "rootDir": ".",
    "outDir": "wwwroot/js"
  },
  "include": [
    // 仅包含项目中的 .ts 文件
    "**/*.ts"
  ]
}
```
> 这样配置后，`Components/Pages/Counter.ts` 将被编译到 `wwwroot/js/Components/Pages/Counter.js`。

### 3. 注册服务

在 `Program.cs` 中注册 BlazorTS 服务。

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;

builder.Services.AddScoped<BlazorTS.ScriptBridge>();
// 自动查找并注册所有生成的 TSInterop 服务
builder.Services.AddBlazorTSScripts();
```

### 4. 运行并查看结果

现在，运行你的 Blazor 应用。当你点击按钮时：
1.  `Counter.razor` 中的 `HandleClick` 方法被调用。
2.  它直接访问 `Scripts` 属性，这是 BlazorTS 自动生成的。
3.  `Scripts.IncrementCount` 调用会执行 `Counter.ts` 中的相应函数。

BlazorTS 在后台为你生成了如下的 `partial class` 代码，并将其与你的 `Counter.razor.cs` 合并：

```csharp
// BlazorTS 自动生成的代码 (conceptual)
public partial class Counter
{
    // 自动注入 TSInterop 实例
    [Inject]
    public TSInterop Scripts { get; set; } = null!;

    // 包装类，负责与 JS 互操作
    public class TSInterop(ScriptBridge invoker)
    {
        // ... 实现细节 ...
        public async Task<double> IncrementCount(double count)
        {
            // ... 调用 JS ...
        }
    }
}
```

通过这种方式，BlazorTS 将 TypeScript 的开发体验与 Blazor 组件模型完美融合，实现了真正的模块化。

## 🔧 支持的类型

| TypeScript | C# 参数 | 返回类型 |
|------------|---------|----------|
| `string` | `string` | `Task<string>` |
| `number` | `double` | `Task<double>` |
| `boolean` | `bool` | `Task<bool>` |
| `any` | `object?` | `Task<object?>` |
| `void` | - | `Task` |
| `Promise<T>` | - | `Task<T>` |

## 📖 更多文档

- [开发指南](docs/开发指南.md) - 详细的开发和构建说明
- [支持的 TypeScript 语法](docs/支持的TypeScript语法.md) - 完整的语法支持列表
- [DLL 路径解析机制](docs/dll路径解析机制文档.md) - 高级配置选项

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License