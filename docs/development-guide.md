# BlazorTS

基于 TypeScriptParser 和 Tree-sitter 的 Blazor TypeScript 互操作框架。

## 工作原理

1. **编译时分析**：源代码生成器扫描 `.razor.ts` 和 `.entry.ts` 文件，使用 Tree-sitter 分析导出函数与 `interface` 声明。
2. **代码生成**：
   - 对 `.razor.ts` 生成组件对应的 `partial class`，并为其配套生成顶层 `*TSInterop` 互操作类。
   - 对 `.entry.ts` 生成可直接注入的服务类。
   - 对 `interface` 声明生成强类型的 C# `public sealed class` 数据模型，支持 `required` 属性、可空属性、接口嵌套与数组。
3. **服务注册**：`AddBlazorTS()` 注册基础服务和 `ScriptsAccess<>`；生成的 `Scripts.AddScripts(IServiceCollection)` 方法注册项目的 `.entry.ts` 服务。

## 使用步骤

### 1. 创建 TypeScript 文件

```typescript
// Components/Pages/Counter.razor.ts
export function increment(count: number): number {
    console.log("Incrementing count from TypeScript module!");
    return count + 1;
}
```

```typescript
// Services/Formatter.entry.ts
export function formatCurrency(amount: number): string {
    return `$${amount.toFixed(2)}`;
}
```

### 2. 安装包

```bash
# 安装 BlazorTS 核心包与源代码生成器
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
```

```xml
<!-- 在项目文件中添加 TypeScript 文件为附加文件，并排除 node_modules -->
<ItemGroup>
  <AdditionalFiles Include="**/*.razor.ts" Exclude="**/node_modules/**" />
  <AdditionalFiles Include="**/*.entry.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

### 3. 注册服务

`AddBlazorTS()` 以 Scoped 生命周期注册 `NSResolver`、`ScriptBridge` 和开放泛型 `ScriptsAccess<>`。`ScriptsAccess<T>` 在构造时创建互操作实例，组件的 `Scripts` 属性返回该实例。

`AddScripts(...)` 调用 `AddBlazorTS()`，再执行传入的注册方法。生成的 `MyApp.Generated.Scripts.AddScripts` 方法注册该项目的 `.entry.ts` 服务。

```csharp
// Program.cs
builder.Services.AddScripts(MyApp.Generated.Scripts.AddScripts);
```

### 4. 在组件中使用

#### Razor 组件脚本（`.razor.ts`）

对 `Counter.razor.ts`，BlazorTS 会：

- 为 `Counter` 生成 `partial class`
- 为该组件生成顶层 `CounterTSInterop` 互操作类
- 生成 `ScriptsAccess<CounterTSInterop>` 注入属性和只读的 `Scripts` 属性。

```csharp
@page "/counter"
@rendermode InteractiveServer

@code {
    private int currentCount = 0;

    private async Task HandleClick()
    {
        currentCount = await Scripts.increment(currentCount);
    }
}
```

对应的 C# 代码：

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
        return await invoker.InvokeAsync<double>(url, "increment",
            new object?[] { count });
    }
}
```

#### 独立功能模块（`.entry.ts`）

对 `Formatter.entry.ts`，BlazorTS 会生成普通服务类，可直接通过 DI 使用。

```csharp
// Program.cs
builder.Services.AddScripts(MyApp.Generated.Scripts.AddScripts);
```

```csharp
@inject TestApp.Services.Formatter Formatter

<p>@await Formatter.formatCurrency(123.45)</p>
```

## 文件命名约定

### 1. Razor 组件脚本（`.razor.ts`）

- **命名约定**：`MyComponent.razor.ts` 必须与 `MyComponent.razor` 配对。
- **生成结果**：
  - 自动为组件生成 `partial class`
  - 生成 `ScriptsAccess<T>` 注入属性，`Scripts` 属性返回该访问器持有的互操作实例。
- **使用方式**：在组件内直接通过 `Scripts` 调用 TypeScript 函数。

### 2. 独立功能模块（`.entry.ts`）

- **命名约定**：`my-utils.entry.ts` 或 `api.entry.ts`
- **生成结果**：生成一个标准的 C# 服务类
- **使用方式**：通过 `AddScripts(Project.Generated.Scripts.AddScripts)` 注册生成的服务，再通过依赖注入使用。

## 生成代码结构

### Razor 组件

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

### Entry 模块

```csharp
public class Formatter(ScriptBridge invoker)
{
    private readonly string url = invoker.ResolveNS(typeof(Formatter), ".entry");

    public async Task<string> formatCurrency(double amount)
    {
        return await invoker.InvokeAsync<string>(url, "formatCurrency", new object?[] { amount });
    }
}
```

## Blazor 回归测试

`BlazorTS.RealBlazorApp` 提供泛型组件 `GenericProbe<TItem>` 及其配套脚本。`BlazorTS.RealBlazorApp.Tests` 检查生成类型、方法签名和 `ScriptsAccess<>` 的 Scoped 注册，并验证 `ScriptsAccess<T>` 能够创建互操作实例。

## 类型映射

| TypeScript | C# 参数类型 | C# 返回类型 |
|------------|-------------|-------------|
| `string` | `string` | `Task<string>` |
| `number` | `double` | `Task<double>` |
| `boolean` | `bool` | `Task<bool>` |
| `any` | `object?` | `Task<object?>` |
| `void` | - | `Task` |
| `Promise<T>` | - | `Task<T>` |
| `T[]` / `Array<T>` | `T[]` | `Task<T[]>` |
| `interface Name` | `Name` | `Task<Name>` |

TypeScript `interface` 映射为 C# `public sealed class`。非可选字段生成 `required` 属性，可选字段生成可空类型，支持在方法入参与返回值中直接作为强类型传递。

## Suffix 参数说明

`ResolveNS` 将 `suffix` 传递给已注册的解析函数。解析函数根据完整类型名和后缀生成模块路径，返回 `null` 时继续调用下一条解析函数。

```csharp
builder.Services.AddBlazorTSResolver((fullName, suffix) =>
{
    var path = fullName.Replace('.', '/');
    return $"/js/{path}{suffix}.js";
});
```

Razor 组件通常使用 `.razor`，Entry 模块通常使用 `.entry`。

## 本地测试

**严格遵守以下工作流，如果你试图自作聪明，自以为是，强行绕开以下流程，我会杀了你**

```bash
### 清理缓存
dotnet clean
dotnet nuget locals all --clear
/bin/rm -rf **/bin **/obj
/bin/rm -rf ./artifacts/*.nupkg

### 打包测试
dotnet test BlazorTS.SourceGenerator.Tests/
dotnet test
dotnet test BlazorTS.RealBlazorApp.Tests/
dotnet pack BlazorTS/BlazorTS.csproj --configuration Release --output ./artifacts/
dotnet pack BlazorTS.Assets/BlazorTS.Assets.csproj --configuration Release --output ./artifacts/
dotnet pack BlazorTS.SourceGenerator/BlazorTS.SourceGenerator.csproj --configuration Release --output ./artifacts/
dotnet build-server shutdown
dotnet add package BlazorTS.SourceGenerator --version 0.1.0-dev --project BlazorTS.TestPackage/
BLAZORTS_LOG_ENABLED=true dotnet test BlazorTS.TestPackage/
dotnet publish BlazorTS.TestPackage/ -c Release
```

## CI/CD 流程

### 开发流程

1. 创建功能分支
2. 提交 Pull Request，自动执行构建与测试
3. 合并到 main 分支

### 发布流程

1. 创建版本标签：

```bash
(VERSION=v1.0.5 && git tag $VERSION && git push origin $VERSION)
```

2. 自动构建、测试并发布到 NuGet.org
3. 版本号格式：`1.0.0.{构建号}`
