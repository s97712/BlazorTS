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

### Installation and Configuration

**1. Install NuGet Packages**

**Install via .NET CLI**
```bash
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
dotnet add package Microsoft.TypeScript.MSBuild # (optional)
```

**Install via Package Manager Console**
```powershell
Install-Package BlazorTS
Install-Package BlazorTS.SourceGenerator
Install-Package Microsoft.TypeScript.MSBuild # (optional)
```

> **Alternative:** If you prefer not to use `Microsoft.TypeScript.MSBuild`, you can manually add a `Target` to your `.csproj` file to invoke the TypeScript compiler (`npx tsc`).
>
> ```xml
> <Target Name="CompileTypeScript" BeforeTargets="Build">
>   <Exec Command="npx tsc" />
> </Target>
> ```

**2. Configure Project File**

Add the following to your `.csproj` file to ensure TypeScript files are processed correctly:

```xml
<!-- Add TypeScript files as additional files, excluding node_modules -->
<ItemGroup>
  <AdditionalFiles Include="**/*.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

## 🚀 Quick Start: Binding a TypeScript Module to a Razor Component

The core strength of BlazorTS is its ability to seamlessly "bind" a TypeScript file to a Razor component as its dedicated script module. This is achieved through **file naming conventions** and **partial classes**.

### 1. Create the Component and its TypeScript Module

Let's assume we have a `Counter` component.

**`Components/Pages/Counter.razor`**
```csharp
@page "/counter"
@rendermode InteractiveServer

@* Declare this component as a partial class to merge with the generated code *@
@code {
    public partial class Counter
    {
        private int currentCount = 0;

        private async Task HandleClick()
        {
            // Directly call the `Scripts` property injected by BlazorTS
            currentCount = await Scripts.IncrementCount(currentCount);
        }
    }
}
```

**`Components/Pages/Counter.ts`**
Create a TypeScript file with the same name as the Razor component.
```typescript
// This file is the dedicated module for the Counter.razor component
export function IncrementCount(count: number): number {
    console.log("Incrementing count from TypeScript module!");
    return count + 1;
}
```

### 2. Configure `tsconfig.json`

To enable Blazor to find the compiled JS file, we need to configure `tsconfig.json` to preserve the directory structure.

```json
{
  "compilerOptions": {
    "noImplicitAny": false,
    "noEmitOnError": true,
    "removeComments": false,
    "target": "es2015",
    // Use "rootDir" and "outDir" together to preserve the source directory structure in the output directory
    "rootDir": ".",
    "outDir": "wwwroot/js"
  },
  "include": [
    // Only include .ts files in the project
    "**/*.ts"
  ]
}
```
> With this configuration, `Components/Pages/Counter.ts` will be compiled to `wwwroot/js/Components/Pages/Counter.js`.

### 3. Register Services

Register the BlazorTS services in `Program.cs`.

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;
using Microsoft.Extensions.DependencyInjection;

// Register BlazorTS core services (includes default path resolver)
builder.Services.AddBlazorTS();
// Automatically finds and registers all generated TSInterop services
builder.Services.AddBlazorTSScripts();
```

### 4. Run and See the Result

Now, run your Blazor application. When you click the button:
1.  The `HandleClick` method in `Counter.razor` is called.
2.  It directly accesses the `Scripts` property, which is automatically generated by BlazorTS.
3.  The `Scripts.IncrementCount` call executes the corresponding function in `Counter.ts`.

Behind the scenes, BlazorTS generates the following `partial class` code for you and merges it with your `Counter.razor.cs`:

```csharp
// Code automatically generated by BlazorTS (conceptual)
public partial class Counter
{
    // Automatically injects the TSInterop instance
    [Inject]
    public TSInterop Scripts { get; set; } = null!;

    // Wrapper class responsible for JS interop
    public class TSInterop(ScriptBridge invoker)
    {
        // ... implementation details ...
        public async Task<double> IncrementCount(double count)
        {
            // ... calls JS ...
        }
    }
}
```

In this way, BlazorTS perfectly integrates the TypeScript development experience with the Blazor component model, achieving true modularity.

## 🛠️ Custom Path Resolution

BlazorTS maps `MyApp.Components.Counter` to `/js/Components/Counter.js` by default.

To customize paths, specify when registering services:

```csharp
// Using custom function
builder.Services.AddBlazorTS(type =>
{
    var path = type.FullName!.Replace('.', '/');
    return $"/scripts/{path}.js";
});

// Using custom resolver class
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