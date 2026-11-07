# BlazorTS

🚀 A tool for seamless Blazor-TypeScript integration

<div align="center">

[English](README.md) | [中文](README_CN.md) | [🏠 GitHub](https://github.com/s97712/BlazorTS)

</div>

---

BlazorTS is a source generator library that uses **Tree-sitter syntax tree parsing** to analyze TypeScript code and automatically generate strongly typed C# wrappers, allowing Blazor applications to call TypeScript functions directly without hand-written JavaScript interop glue.

## ✨ Features

- 🔄 **Compile-time generation**: Analyze TypeScript files and generate C# wrappers automatically
- 🎯 **Type safety**: Strongly typed C# APIs and models for exported TypeScript functions and interfaces
- 🚀 **Zero configuration**: Minimal setup, works out of the box
- 🔧 **Service registration**: `AddBlazorTS()` registers the core services and `ScriptsAccess<>`.
- 🌳 **Precise parsing**: Uses Tree-sitter for accurate TypeScript syntax handling

## 📦 Installation

### Core Libraries

| NuGet Package | Description |
|--|--|
| [`BlazorTS`](https://www.nuget.org/packages/BlazorTS) | Core runtime library |
| [`BlazorTS.SourceGenerator`](https://www.nuget.org/packages/BlazorTS.SourceGenerator) | Source generator library |
| [`BlazorTS.Assets`](https://www.nuget.org/packages/BlazorTS.Assets) | Asset management with dynamic URLs |
| [`Microsoft.TypeScript.MSBuild`](https://www.nuget.org/packages/Microsoft.TypeScript.MSBuild) | TypeScript compilation support |

### Installation and Configuration

```bash
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
dotnet add package BlazorTS.Assets # optional
dotnet add package Microsoft.TypeScript.MSBuild # optional
```

```xml
<ItemGroup>
  <AdditionalFiles Include="**/*.razor.ts" Exclude="**/node_modules/**" />
  <AdditionalFiles Include="**/*.entry.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

## 🚀 File Naming Conventions

### 1. Razor Component Scripts (`.razor.ts`)

These files are bound to a specific Razor component.

- **Naming**: `MyComponent.razor.ts` must pair with `MyComponent.razor`
- **Generated output**:
  - a `partial class` for the component
  - a top-level `*TSInterop` class for that component
  - an injected `ScriptsAccess<T>` and a read-only `Scripts` property that returns the interop instance
- **Usage**: call exported TypeScript functions through the generated `Scripts` property

**Example**

```typescript
// Components/Pages/Counter.razor.ts
export function increment(count: number): number {
    return count + 1;
}
```

```csharp
@page "/counter"

@code {
    private int currentCount = 0;

    private async Task HandleClick()
    {
        currentCount = await Scripts.increment(currentCount);
    }
}
```

Generated C# code:

```csharp
public partial class Counter
{
    [Inject] public ScriptsAccess<CounterTSInterop> ScriptsAccess { get; set; } = null!;

    public CounterTSInterop Scripts => ScriptsAccess.Value;
}

public class CounterTSInterop(ScriptBridge invoker)
{
    private readonly string url = invoker.ResolveNS("MyApp.Components.Pages.Counter", ".razor");

    public async Task<double> increment(double count)
    {
        return await invoker.InvokeAsync<double>(url, "increment", new object?[] { count });
    }
}
```

### 2. Standalone Feature Modules (`.entry.ts`)

These files define reusable TypeScript modules that can be injected as services.

- **Naming**: `my-utils.entry.ts` or `api.entry.ts`
- **Generated output**: a normal C# service class
- **Usage**: register the generated services with `AddScripts(Project.Generated.Scripts.AddScripts)`, then inject them where needed.

```typescript
// Services/Formatter.entry.ts
export function formatCurrency(amount: number): string {
    return `$${amount.toFixed(2)}`;
}
```

```csharp
builder.Services.AddScripts(TestApp.Generated.Scripts.AddScripts);
```

```csharp
@inject TestApp.Services.Formatter Formatter

<p>@await Formatter.formatCurrency(123.45)</p>
```

## Configure `tsconfig.json`

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

## Register Services

`AddBlazorTS()` registers `NSResolver`, `ScriptBridge`, and the open generic `ScriptsAccess<>` with scoped lifetimes. `ScriptsAccess<T>` creates the interop instance in its constructor. The component's `Scripts` property returns that instance.

The generator creates `Scripts.AddScripts(IServiceCollection)` in the `{RootNamespace}.Generated` namespace to register the project's `.entry.ts` services. The following call registers those services and the BlazorTS core services.

```csharp
builder.Services.AddScripts(MyApp.Generated.Scripts.AddScripts);
```

## 🛠️ Path Resolution

BlazorTS resolves JavaScript module paths using registered resolver delegates. If no registered resolver returns a path, an `InvalidOperationException` is thrown.

```csharp
builder.Services.AddBlazorTSResolver((fullName, suffix) =>
{
    var path = fullName.Replace('.', '/');
    return $"/scripts/{path}{suffix}.js";
});

builder.Services.AddBlazorTSResolver((fullName, suffix) =>
{
    if (!fullName.StartsWith("MyApp.Services."))
    {
        return null;
    }

    return $"/services/{fullName.Replace('.', '/')}{suffix}.js";
});
```

Resolvers are called in registration order. Return a module path for a match and `null` to continue with the next resolver. If all resolvers return `null`, resolving throws an exception.

### Suffix Parameter

- Razor components (`.razor.ts`) use suffix `".razor"`
- Entry modules (`.entry.ts`) use suffix `".entry"`
- Other module styles can use any custom suffix

## 🔧 Supported Types

| TypeScript | C# Parameter | Return Type |
|------------|-------------|------------|
| `string` | `string` | `Task<string>` |
| `number` | `double` | `Task<double>` |
| `boolean` | `bool` | `Task<bool>` |
| `any` | `object?` | `Task<object?>` |
| `void` | - | `Task` |
| `Promise<T>` | - | `Task<T>` |
| `T[]` / `Array<T>` | `T[]` | `Task<T[]>` |
| `interface Name` | `Name` | `Task<Name>` |

Exported TypeScript `interface` declarations generate `public sealed class` models in the corresponding C# namespace, with `required` properties for mandatory fields, nullable annotations for optional fields, and recursive type resolution for interface references and arrays.

## Blazor Regression Tests

`BlazorTS.RealBlazorApp` contains the generic component `GenericProbe<TItem>` and its paired script. `BlazorTS.RealBlazorApp.Tests` checks the generated types, method signatures, and scoped registration of `ScriptsAccess<>`, and verifies that `ScriptsAccess<T>` creates an interop instance.

## 📖 Documentation

- [Development Guide](docs/development-guide.md)
- [Supported TypeScript Syntax](docs/supported-typescript-syntax.md)
- [DLL Path Resolution Mechanism](docs/dll-resolver-mechanism.md)
- [中文说明](README_CN.md)

## 🤝 Contributing

Issues and Pull Requests are welcome.

## 📄 License

MIT License
