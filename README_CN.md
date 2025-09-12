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
dotnet add package Microsoft.TypeScript.MSBuild # (可选)
```

**使用 Package Manager Console 安装**
```powershell
Install-Package BlazorTS
Install-Package BlazorTS.SourceGenerator
Install-Package Microsoft.TypeScript.MSBuild # (可选)
```

> **替代方案：** 如果不使用 `Microsoft.TypeScript.MSBuild`，可手动在 `.csproj` 中添加 `Target` 来调用 `npx tsc` 编译。
>
> ```xml
> <Target Name="CompileTypeScript" BeforeTargets="Build">
>   <Exec Command="npx tsc" />
> </Target>
> ```

**2. 配置项目文件**

在 `.csproj` 文件中添加以下配置，以确保 TypeScript 文件被正确处理：

```xml
<!-- 添加 .razor.ts 和 .entry.ts 文件为附加文件 -->
<ItemGroup>
  <AdditionalFiles Include="**/*.razor.ts" Exclude="**/node_modules/**" />
  <AdditionalFiles Include="**/*.entry.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

## 🚀 文件命名约定

BlazorTS 支持两种 TypeScript 文件类型，以提供灵活的模块化方案：

### 1. Razor 组件脚本 (`.razor.ts`)

这种文件与特定的 Razor 组件绑定，用于组件级别的脚本逻辑。

- **命名约定**: `MyComponent.razor.ts` 必须与 `MyComponent.razor` 配对。
- **生成结果**: 自动为 `MyComponent` 生成一个 `partial class`，并注入一个名为 `Scripts` 的 `TSInterop` 实例。
- **使用方式**: 在组件内通过 `@inject` 的 `Scripts` 属性直接调用 TypeScript 函数。

**示例：**

**`Components/Pages/Counter.razor.ts`**
```typescript
// Counter.razor 组件的专属模块
export function increment(count: number): number {
    console.log("Incrementing count from TypeScript module!");
    return count + 1;
}
```

**`Components/Pages/Counter.razor`**
```csharp
@page "/counter"
@rendermode InteractiveServer

@code {
    public partial class Counter // 必须是 partial class
    {
        private int currentCount = 0;

        private async Task HandleClick()
        {
            // 直接调用由 BlazorTS 注入的 Scripts 属性
            currentCount = await Scripts.increment(currentCount);
        }
    }
}
```

### 2. 独立功能模块 (`.entry.ts`)

这种文件用于定义可被多个组件或服务共享的通用 TypeScript 模块。

- **命名约定**: `my-utils.entry.ts` 或 `api.entry.ts`。
- **生成结果**: 生成一个标准的 C# 类（例如 `MyUtils` 或 `Api`），需要手动注册和注入。
- **使用方式**: 在 `Program.cs` 中注册服务，然后在需要的地方通过依赖注入使用。

**示例：**

**`Services/Formatter.entry.ts`**
```typescript
export function formatCurrency(amount: number): string {
    return `$${amount.toFixed(2)}`;
}
```

**`Program.cs`**
```csharp
// 自动查找并注册所有 .entry.ts 生成的服务
builder.Services.AddBlazorTSScripts();
```

**`MyComponent.razor`**
```csharp
@inject TestApp.Services.Formatter Formatter

<p>@Formatter.formatCurrency(123.45)</p>
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
    "rootDir": ".",
    "outDir": "wwwroot/js"
  },
  "include": [
    "**/*.razor.ts",
    "**/*.entry.ts"
  ]
}
```
> 这样配置后，`Components/Pages/Counter.razor.ts` 将被编译到 `wwwroot/js/Components/Pages/Counter.js`。

### 4. 注册服务

在 `Program.cs` 中注册 BlazorTS 服务。

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;
using Microsoft.Extensions.DependencyInjection;

// 注册 BlazorTS 核心服务（包含默认路径解析器）
builder.Services.AddBlazorTS();
// 自动查找并注册所有 .entry.ts 生成的服务
builder.Services.AddBlazorTSScripts();
```

### 5. 运行并查看结果

现在，运行你的 Blazor 应用。当你点击按钮时：
1.  `Counter.razor` 中的 `HandleClick` 方法被调用。
2.  它直接访问 `Scripts` 属性，这是 BlazorTS 自动生成的。
3.  `Scripts.increment` 调用会执行 `Counter.razor.ts` 中的相应函数。

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
        public async Task<double> increment(double count)
        {
            // ... 调用 JS ...
        }
    }
}
```

通过这种方式，BlazorTS 将 TypeScript 的开发体验与 Blazor 组件模型完美融合，实现了真正的模块化。

## 🛠️ 自定义路径解析

BlazorTS 默认将 `MyApp.Components.Counter` 映射为 `/js/Components/Counter.js`。

如需自定义路径，可在注册服务时指定：

```csharp
// 使用自定义函数
builder.Services.AddBlazorTS(type =>
{
    var path = type.FullName!.Replace('.', '/');
    return $"/scripts/{path}.js";
});

// 使用自定义解析器类
public class CustomResolver : INSResolver
{
    public string ResolveNS(Type tsType)
    {
        var path = tsType.FullName!.Replace('.', '/');
        return $"/lib/{path}.js";
    }
}
builder.Services.AddBlazorTS<CustomResolver>();
```

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