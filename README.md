# BlazorTS

基于 TypeScriptParser 和 Tree-sitter 的 Blazor TypeScript 互操作框架。

## 工作原理

1. **编译时分析**: 源代码生成器扫描`.ts`文件，使用TypeScriptParser解析函数声明
2. **代码生成**: 为每个.ts文件生成对应的partial class + TSInterop嵌套类
3. **服务注册**: 自动生成`AddJsInvokeServices()`扩展方法注册所有TSInterop服务

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

### 2. 项目配置
```xml
<!-- 项目文件中添加 -->
<ItemGroup>
  <PackageReference Include="BlazorTS.SourceGenerator" Version="0.1.0-dev" />
  <AdditionalFiles Include="**/*.ts" />
</ItemGroup>
```

### 3. 注册服务
```csharp
// Program.cs 
using BlazorTS.SourceGenerator.Extensions;

builder.Services.AddJsInvokeServices();  // 自动注册所有TSInterop服务
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
    [Inject] public TSInterop TypeScriptJS { get; set; } = null!;

    public class TSInterop(InvokeWrapper invoker)
    {
        private string url = InvokeWrapper.ResolveNS(typeof(TestFunctions));

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

## 构建和测试

### 清理缓存
```bash
dotnet clean
dotnet nuget locals all --clear

rm -rf **/bin **/obj
rm -rf ./artifacts

# or trash
trash -f **/bin **/obj
trash -f ./artifacts/*.nupkg
```

### 基本命令
```bash
dotnet restore
dotnet build
dotnet test
```

### 打包测试
```bash
dotnet pack --configuration Release --output ./artifacts/
dotnet test BlazorTS.TestPackage/
```

## CI/CD流程

### 开发流程
1. 创建功能分支
2. 提交Pull Request → 自动构建测试
3. 合并到main分支

### 发布流程
1. 创建版本标签：
```bash
(VERSION=v0.1.0 && git tag $VERSION && git push origin $VERSION)
```
2. 自动构建测试发布到NuGet.org
3. 版本号格式：`1.0.0.{构建号}`