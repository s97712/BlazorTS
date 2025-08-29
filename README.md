# BlazorTS

🚀 A tool for seamless Blazor-TypeScript integration

<div align="center">

[English](README.md) | [中文](README_CN.md) | [🏠 GitHub](https://github.com/s97712/BlazorTS)

</div>

---

BlazorTS is a source generator library that uses **Tree-sitter syntax tree parsing** to analyze TypeScript code, automatically generating C# wrapper code that enables you to call TypeScript functions directly in Blazor applications without manually writing JavaScript interop code.

## ✨ Features

- 🔄 **Auto Generation**: Uses Tree-sitter syntax tree parsing to analyze TypeScript code and automatically generates C# wrapper code
- 🎯 **Type Safety**: Complete type mapping and compile-time checking
- 🚀 **Zero Configuration**: Minimal configuration, works out of the box
- 🔧 **Smart Dependencies**: Automatic service resolution and registration
- 🌳 **Precise Parsing**: Uses Tree-sitter for accurate TypeScript syntax structure parsing

## 📦 Installation

**Core Libraries**

| NuGet Package | NuGet Version | Description |
|--|--|--|
| [`BlazorTS`](https://www.nuget.org/packages/BlazorTS) | [![NuGet](https://img.shields.io/nuget/v/BlazorTS.svg?style=flat)](https://www.nuget.org/packages/BlazorTS) | Core runtime library |
| [`BlazorTS.SourceGenerator`](https://www.nuget.org/packages/BlazorTS.SourceGenerator) | [![NuGet](https://img.shields.io/nuget/v/BlazorTS.SourceGenerator.svg?style=flat)](https://www.nuget.org/packages/BlazorTS.SourceGenerator) | Source generator library |
| [`Microsoft.TypeScript.MSBuild`](https://www.nuget.org/packages/Microsoft.TypeScript.MSBuild) | [![NuGet](https://img.shields.io/nuget/v/Microsoft.TypeScript.MSBuild.svg?style=flat)](https://www.nuget.org/packages/Microsoft.TypeScript.MSBuild) | TypeScript compilation support |

## 🚀 Quick Start

### 1. Configure Project File

Add the following configuration to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="BlazorTS" Version="1.0.5.7" />
  <PackageReference Include="BlazorTS.SourceGenerator" Version="1.0.5.7" OutputItemType="Analyzer" ReferenceOutputAssembly="true">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Microsoft.TypeScript.MSBuild" Version="5.8.3">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>

<!-- Add TypeScript files as additional files -->
<ItemGroup>
  <AdditionalFiles Include="**/*.ts" />
</ItemGroup>
```

### 2. Create tsconfig.json

Create a `tsconfig.json` configuration file in your project root:

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

### 3. Create TypeScript File

```typescript
// Components/Pages/Counter.ts
export function IncrementCount(count: number): number {
    return count + 1;
}
```

### 4. Register Services

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;

builder.Services.AddScoped<BlazorTS.ScriptBridge>();
builder.Services.AddBlazorTSScripts();  // Auto-register all TSInterop services


```

### 5. Use in Components

```csharp
@page "/counter"
@rendermode InteractiveServer

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>

<p role="status">Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="HandleClick">Click to Increment</button>

@code {
    private int currentCount = 0;

    private async Task HandleClick()
    {
        // Call TypeScript function to increment count
        currentCount = await Scripts.IncrementCount(currentCount);
    }
}
```

That's it! BlazorTS will automatically generate corresponding C# wrapper classes for your TypeScript files.

## 🔧 Supported Types

| TypeScript | C# Parameter | Return Type |
|------------|-------------|------------|
| `string` | `string` | `Task<string>` |
| `number` | `double` | `Task<double>` |
| `boolean` | `bool` | `Task<bool>` |
| `any` | `object?` | `Task<object?>` |
| `void` | - | `Task` |
| `Promise<T>` | - | `Task<T>` |

## 📖 Documentation

- [Development Guide (Chinese)](docs/开发指南.md) - Detailed development and build instructions
- [Supported TypeScript Syntax (Chinese)](docs/支持的TypeScript语法.md) - Complete syntax support list
- [DLL Path Resolution Mechanism (Chinese)](docs/dll路径解析机制文档.md) - Advanced configuration options
- [中文说明](README_CN.md) - Chinese version of this README

> **Note**: Documentation is currently available in Chinese. English translations coming soon.

## 🤝 Contributing

Issues and Pull Requests are welcome!

## 📄 License

MIT License