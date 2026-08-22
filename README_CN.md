# BlazorTS

🚀 让 Blazor 与 TypeScript 无缝协作的工具

<div align="center">

[English](README.md) | [中文](README_CN.md) | [🏠 GitHub](https://github.com/s97712/BlazorTS)

</div>

---

BlazorTS 是一个源代码生成器库，基于 **Tree-sitter 语法树解析** 分析 TypeScript 代码，并自动生成强类型 C# 包装代码，让你可以在 Blazor 应用中直接调用 TypeScript 函数，而无需手写 JavaScript 互操作胶水代码。

## ✨ 特性

- 🔄 **编译时生成**：自动分析 TypeScript 文件并生成 C# 包装代码
- 🎯 **类型安全**：为导出的 TypeScript 函数生成强类型 C# API
- 🚀 **零配置**：最小配置即可使用
- 🔧 **自动注册**：通过 `AddBlazorTSScripts()` 注册所有生成的互操作服务
- 🌳 **精确解析**：使用 Tree-sitter 精确处理 TypeScript 语法

## 📦 安装

### 核心库

| NuGet 包 | 描述 |
|--|--|
| [`BlazorTS`](https://www.nuget.org/packages/BlazorTS) | 核心运行时库 |
| [`BlazorTS.SourceGenerator`](https://www.nuget.org/packages/BlazorTS.SourceGenerator) | 源代码生成器库 |
| [`BlazorTS.Assets`](https://www.nuget.org/packages/BlazorTS.Assets) | 资源管理与动态 URL 支持 |
| [`Microsoft.TypeScript.MSBuild`](https://www.nuget.org/packages/Microsoft.TypeScript.MSBuild) | TypeScript 编译支持 |

### 安装与配置

```bash
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
dotnet add package BlazorTS.Assets # 可选
dotnet add package Microsoft.TypeScript.MSBuild # 可选
```

```xml
<ItemGroup>
  <AdditionalFiles Include="**/*.razor.ts" Exclude="**/node_modules/**" />
  <AdditionalFiles Include="**/*.entry.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

## 🚀 文件命名约定

### 1. Razor 组件脚本（`.razor.ts`）

这类文件与某个 Razor 组件绑定。

- **命名**：`MyComponent.razor.ts` 必须与 `MyComponent.razor` 配对
- **生成结果**：
  - 为组件生成 `partial class`
  - 为组件生成顶层 `*TSInterop` 互操作类
  - 在组件上自动注入 `Scripts` 属性
- **使用方式**：通过 `Scripts` 直接调用导出的 TypeScript 函数

**示例**

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

BlazorTS 在概念上会生成如下代码：

```csharp
public partial class Counter
{
    [Inject] public CounterTSInterop Scripts { get; set; } = null!;
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

### 2. 独立功能模块（`.entry.ts`）

这类文件用于定义可复用的 TypeScript 模块，并生成普通服务类。

- **命名**：`my-utils.entry.ts` 或 `api.entry.ts`
- **生成结果**：普通 C# 服务类
- **使用方式**：通过 `AddBlazorTSScripts()` 注册后，在任意位置注入使用

```typescript
// Services/Formatter.entry.ts
export function formatCurrency(amount: number): string {
    return `$${amount.toFixed(2)}`;
}
```

```csharp
builder.Services.AddBlazorTSScripts();
```

```csharp
@inject TestApp.Services.Formatter Formatter

<p>@await Formatter.formatCurrency(123.45)</p>
```

## 配置 `tsconfig.json`

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

## 注册服务

```csharp
using MyApp.Generated;

builder.Services.AddScripts(MyApp.Generated.Scripts.AddScripts);
```

源生成器会在每个类库的根命名空间下生成 `Scripts.AddScripts(IServiceCollection)`。宿主项目使用完整方法名传入 `AddScripts`，例如 `MyApp.Generated.Scripts.AddScripts`。

## 🛠️ 自定义路径解析

默认情况下，BlazorTS 会把逻辑组件名或模块名映射到 `/js/...`。

```csharp
builder.Services.AddBlazorTS((type, suffix) =>
{
    var path = type.FullName!.Replace('.', '/');
    return $"/scripts/{path}{suffix}.js";
});
```

```csharp
public class CustomResolver : INSResolver
{
    public string ResolveNS(Type tsType, string suffix)
    {
        var path = tsType.FullName!.Replace('.', '/');
        return $"/lib/{path}{suffix}.js";
    }

    public string ResolveNS(string fullName, string suffix)
    {
        var path = fullName.Replace('.', '/');
        return $"/lib/{path}{suffix}.js";
    }
}

builder.Services.AddBlazorTS<CustomResolver>();
```

### Suffix 参数

- Razor 组件（`.razor.ts`）使用后缀 `".razor"`
- Entry 模块（`.entry.ts`）使用后缀 `".entry"`
- 其他模块样式可使用任意自定义后缀

## 🔧 支持的类型

| TypeScript | C# 参数 | 返回类型 |
|------------|---------|----------|
| `string` | `string` | `Task<string>` |
| `number` | `double` | `Task<double>` |
| `boolean` | `bool` | `Task<bool>` |
| `any` | `object?` | `Task<object?>` |
| `void` | - | `Task` |
| `Promise<T>` | - | `Task<T>` |

## 真实 Blazor 回归验证

仓库中新增了一个最小真实 Blazor 工程 `BlazorTS.RealBlazorApp/`，用于补足 Roslyn 生成器测试之外的真实 Razor 编译验证。

- `BlazorTS.RealBlazorApp/`：最小真实 Blazor Web App，仅保留泛型组件验证所需内容
- `BlazorTS.RealBlazorApp/Components/Pages/GenericProbe.razor`：真实泛型 Razor 组件
- `BlazorTS.RealBlazorApp/Components/Pages/GenericProbe.razor.ts`：组件配套脚本，生成顶层 `GenericProbeTSInterop`
- `BlazorTS.RealBlazorApp.Tests/`：通过反射、DI 和真实编译产物断言当前已定稿模型

这个场景重点覆盖：

- 泛型仅作用于组件 partial class
- `Scripts` 自动注入在真实 Razor 编译语义下成立
- 互操作服务保持顶层非泛型 `*TSInterop`
- `AddBlazorTSScripts()` 会把对应互操作类型注册进 DI

## 📖 更多文档

- [开发指南](docs/development-guide.md)
- [支持的 TypeScript 语法](docs/supported-typescript-syntax.md)
- [DLL 路径解析机制](docs/dll-resolver-mechanism.md)

## 🤝 贡献

欢迎提交 Issue 和 Pull Request。

## 📄 许可证

MIT License
