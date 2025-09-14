# BlazorTS

基于 TypeScriptParser 和 Tree-sitter 的 Blazor TypeScript 互操作框架。

## 工作原理

1. **编译时分析**: 源代码生成器扫描`.ts`文件，使用TypeScriptParser解析函数声明
2. **代码生成**: 为每个.ts文件生成对应的partial class + TSInterop嵌套类
3. **服务注册**: 自动生成`AddBlazorTSScripts()`扩展方法注册所有TSInterop服务

## 使用步骤

### 1. 创建TypeScript文件
```typescript
// TestFunctions.ts
export async function hello(name: string): Promise<string> {
    return `Hello, ${name}!`;
}

export function add(a: number, b: number): number {
    return a + b;
}

export function greet(name: string, age?: number): void {
    console.log(`Hi ${name}, you are ${age || 'unknown'} years old`);
}
```

### 2. 安装包
```bash
# 安装BlazorTS源代码生成器
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
```

```xml
<!-- 在项目文件中添加TypeScript文件为附加文件, 并排除 node_modules -->
<ItemGroup>
  <AdditionalFiles Include="**/*.ts" Exclude="**/node_modules/**" />
</ItemGroup>
```

### 3. 注册服务
```csharp
// Program.cs 
using BlazorTS.SourceGenerator.Extensions;

builder.Services.AddBlazorTSScripts();  // 自动注册所有TSInterop服务
```

### 4. 在组件中使用
```csharp
@inject TestFunctions.TSInterop TestJS

<button @onclick="CallFunctions">测试</button>

@code {
    private async Task CallFunctions()
    {
        // 所有调用都返回Task<T>或Task
        var message = await TestJS.hello("World");        // Task<string>
        var sum = await TestJS.add(10, 20);              // Task<double>
        await TestJS.greet("张三", 25);                   // Task（void函数）
    }
}
```

## 生成的代码结构

```csharp
// 自动生成: TestFunctions.ts.module.g.cs
public partial class TestFunctions
{
    [Inject] public TSInterop Scripts { get; set; } = null!;

    public class TSInterop(ScriptBridge invoker)
    {
        private readonly string url = invoker.ResolveNS(typeof(TestFunctions), "");

        public async Task<string> hello(string name)
        {
            return await invoker.InvokeAsync<string>(url, "hello",
                new object?[] { name });
        }

        public async Task<double> add(double a, double b)
        {
            return await invoker.InvokeAsync<double>(url, "add", 
                new object?[] { a, b });
        }

        public async Task greet(string name, double age = default)
        {
            await invoker.InvokeAsync<object?>(url, "greet",
                new object?[] { name, age });
        }
    }
}
```

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

`ResolveNS` 方法现在支持 `suffix` 参数来区分不同模块类型：

```csharp
// Razor 组件：Component.razor.ts → /js/Component.razor.js
private readonly string url = invoker.ResolveNS(typeof(Component), ".razor");

// Entry 模块：Module.entry.ts → /js/Module.entry.js
private readonly string url = invoker.ResolveNS(typeof(Module), ".entry");

// 普通模块：Utils.ts → /js/Utils.js
private readonly string url = invoker.ResolveNS(typeof(Utils), "");
```

## 构建和测试

```bash

### 清理缓存
dotnet clean
dotnet nuget locals all --clear
rm -rf **/bin **/obj
rm -rf ./artifacts/*.nupkg

# or trash
dotnet clean
dotnet nuget locals all --clear
trash -f **/bin **/obj
trash -f ./artifacts/*.nupkg


### 打包测试
dotnet test BlazorTS.SourceGenerator.Tests/
dotnet test
dotnet pack --configuration Release --output ./artifacts/
dotnet build-server shutdown
dotnet add package  BlazorTS.SourceGenerator --version 0.1.0-dev --project BlazorTS.TestPackage/
BLAZORTS_LOG_ENABLED=true dotnet test BlazorTS.TestPackage/
dotnet publish BlazorTS.TestPackage/ -c Release
```


## CI/CD流程

### 开发流程
1. 创建功能分支
2. 提交Pull Request → 自动构建测试
3. 合并到main分支

### 发布流程
1. 创建版本标签：
```bash
(VERSION=v1.0.5 && git tag $VERSION && git push origin $VERSION)
```
2. 自动构建测试发布到NuGet.org
3. 版本号格式：`1.0.0.{构建号}`