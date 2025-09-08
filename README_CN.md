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
<!-- 添加 TypeScript 文件为附加文件 -->
<ItemGroup>
  <AdditionalFiles Include="**/*.ts" />
</ItemGroup>
```

## 🚀 快速开始

### 1. 创建 tsconfig.json

在项目根目录创建 `tsconfig.json` 配置文件：

```json
{
  "compilerOptions": {
    "noImplicitAny": false,
    "noEmitOnError": true,
    "removeComments": false,
    "target": "es2015",
    "baseUrl": "./",
    "outDir": "wwwroot/js"
  },
  "include": [
    "**/*"
  ]
}
```

### 3. 创建 TypeScript 文件

```typescript
// Components/Pages/Counter.ts
export function IncrementCount(count: number): number {
    return count + 1;
}
```

### 4. 注册服务

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;

builder.Services.AddScoped<BlazorTS.ScriptBridge>();
builder.Services.AddBlazorTSScripts();  // 自动注册所有TSInterop服务


```

### 5. 在组件中使用

```csharp
@page "/counter"
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="HandleClick">点击增加</button>

@code {
    private int currentCount = 0;

    private async Task HandleClick()
    {
        // 调用 TypeScript 函数进行计数
        currentCount = await Scripts.IncrementCount(currentCount);
    }
}
```

就这么简单！BlazorTS 会自动为你的 TypeScript 文件生成对应的 C# 包装类。

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