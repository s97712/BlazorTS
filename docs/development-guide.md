# BlazorTS

基于 TypeScriptParser 和 Tree-sitter 的 Blazor TypeScript 互操作框架。

## 工作原理

1. **编译时分析**：源代码生成器扫描 `.razor.ts` 和 `.entry.ts` 文件，使用 TypeScriptParser 解析导出函数。
2. **代码生成**：
   - 对 `.razor.ts` 生成组件对应的 `partial class`，并为其配套生成顶层 `*TSInterop` 互操作类。
   - 对 `.entry.ts` 生成可直接注入的服务类。
3. **服务注册**：每个项目生成 `Generated.Scripts.AddScripts(IServiceCollection)` 静态方法，宿主通过 `AddScripts(Project.Generated.Scripts.AddScripts)` 注册生成的互操作服务。

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

```csharp
// Program.cs
builder.Services.AddScripts(MyApp.Generated.Scripts.AddScripts);
```

### 4. 在组件中使用

#### Razor 组件脚本（`.razor.ts`）

对 `Counter.razor.ts`，BlazorTS 会：

- 为 `Counter` 生成 `partial class`
- 为该组件生成顶层 `CounterTSInterop` 互操作类
- 在组件中自动注入 `Scripts`

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

概念化的生成结果如下：

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
  - 自动注入一个名为 `Scripts` 的顶层 `*TSInterop` 服务实例
- **使用方式**：在组件内直接通过 `Scripts` 调用 TypeScript 函数。

### 2. 独立功能模块（`.entry.ts`）

- **命名约定**：`my-utils.entry.ts` 或 `api.entry.ts`
- **生成结果**：生成一个标准的 C# 服务类
- **使用方式**：通过 `AddScripts(Project.Generated.Scripts.AddScripts)` 注册后，在任意位置通过依赖注入使用

## 生成代码结构

### Razor 组件

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
- `AddScripts(Project.Generated.Scripts.AddScripts)` 会把对应互操作类型注册进 DI

## 类型映射

| TypeScript | C# 参数类型 | C# 返回类型 |
|------------|-------------|-------------|
| `string` | `string` | `Task<string>` |
| `number` | `double` | `Task<double>` |
| `boolean` | `bool` | `Task<bool>` |
| `any` | `object?` | `Task<object?>` |
| `void` | - | `Task` |
| `Promise<T>` | - | `Task<T>` |

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

## 构建和测试

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
