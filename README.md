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
<!-- Add .razor.ts and .entry.ts files as additional files -->
<ItemGroup>
  <AdditionalFiles Include="**/*.razor.ts" Exclude="**/node_modules/**" />
  <AdditionalFiles Include="**/*.entry.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

## 🚀 File Naming Conventions

BlazorTS supports two types of TypeScript files to provide a flexible modularization scheme:

### 1. Razor Component Scripts (`.razor.ts`)

These files are bound to a specific Razor component for component-level script logic.

- **Naming Convention**: `MyComponent.razor.ts` must be paired with `MyComponent.razor`.
- **Generated Output**: Automatically generates a `partial class` for `MyComponent` and injects a `TSInterop` instance named `Scripts`.
- **Usage**: Directly call TypeScript functions via the `@inject`ed `Scripts` property within the component.

**Example:**

**`Components/Pages/Counter.razor.ts`**
```typescript
// Dedicated module for the Counter.razor component
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
    private int currentCount = 0;

    private async Task HandleClick()
    {
        // Directly call the `Scripts` property injected by BlazorTS
        currentCount = await Scripts.increment(currentCount);
    }
}
```

### 2. Standalone Feature Modules (`.entry.ts`)

These files are used to define common TypeScript modules that can be shared across multiple components or services.

- **Naming Convention**: `my-utils.entry.ts` or `api.entry.ts`.
- **Generated Output**: Generates a standard C# class (e.g., `MyUtils` or `Api`) that needs to be manually registered and injected.
- **Usage**: Register the service in `Program.cs` and use it where needed via dependency injection.

**Example:**

**`Services/Formatter.entry.ts`**
```typescript
export function formatCurrency(amount: number): string {
    return `$${amount.toFixed(2)}`;
}
```

**`Program.cs`**
```csharp
// Automatically finds and registers all services generated from .entry.ts files
builder.Services.AddBlazorTSScripts();
```

**`MyComponent.razor`**
```csharp
@inject TestApp.Services.Formatter Formatter

<p>@Formatter.formatCurrency(123.45)</p>
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
    "rootDir": ".",
    "outDir": "wwwroot/js"
  },
  "include": [
    "**/*.razor.ts",
    "**/*.entry.ts"
  ]
}
```
> With this configuration, `Components/Pages/Counter.razor.ts` will be compiled to `wwwroot/js/Components/Pages/Counter.js`.

### 3. Register Services

Register the BlazorTS services in `Program.cs`.

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;
using Microsoft.Extensions.DependencyInjection;

// Register BlazorTS core services (includes default path resolver)
builder.Services.AddBlazorTS();
// Automatically finds and registers all services generated from .entry.ts files
builder.Services.AddBlazorTSScripts();
```

### 4. Run and See the Result

Now, run your Blazor application. When you click the button:
1.  The `HandleClick` method in `Counter.razor` is called.
2.  It directly accesses the `Scripts` property, which is automatically generated by BlazorTS.
3.  The `Scripts.increment` call executes the corresponding function in `Counter.razor.ts`.

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
        public async Task<double> increment(double count)
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

- [Development Guide](docs/development-guide.md) - Detailed development and build instructions
- [Supported TypeScript Syntax](docs/supported-typescript-syntax.md) - Complete syntax support list
- [DLL Path Resolution Mechanism](docs/dll-resolver-mechanism.md) - Advanced configuration options
- [中文说明](README_CN.md) - Chinese version of this README

## 🤝 Contributing

Issues and Pull Requests are welcome!

## 📄 License

MIT License