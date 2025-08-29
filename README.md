# BlazorTS

🚀 让 Blazor 与 TypeScript 无缝协作的框架

BlazorTS 是一个基于源代码生成的框架，使用 **Tree-sitter 语法树解析技术** 分析 TypeScript 代码，让你可以在 Blazor 应用中直接调用 TypeScript 函数，无需手动编写 JavaScript 互操作代码。

## ✨ 特性

- 🔄 **自动生成**: 基于 Tree-sitter 语法树解析，从 TypeScript 文件自动生成 C# 包装代码
- 🎯 **类型安全**: 完整的类型映射和编译时检查
- 🚀 **零配置**: 最小化配置，开箱即用
- 🔧 **智能依赖**: 自动解析和注册服务
- 🌳 **精确解析**: 使用 Tree-sitter 精确解析 TypeScript 语法结构

## 📦 快速开始

### 1. 安装包

```bash
dotnet add package BlazorTS
dotnet add package BlazorTS.SourceGenerator
dotnet add package Microsoft.TypeScript.MSBuild
```

### 2. 配置项目文件

在 `.csproj` 文件中添加以下配置：

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

<!-- 添加 TypeScript 文件为附加文件 -->
<ItemGroup>
  <AdditionalFiles Include="**/*.ts" />
</ItemGroup>
```

### 3. 创建 tsconfig.json

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

### 4. 创建 TypeScript 文件

```typescript
// Components/Pages/Counter.ts
export function IncrementCount(count: number): number {
    return count + 1;
}
```

### 5. 注册服务

```csharp
// Program.cs
using BlazorTS.SourceGenerator.Extensions;

builder.Services.AddScoped<BlazorTS.InvokeWrapper>();
builder.Services.AddJsInvokeServices();  // 自动注册所有TSInterop服务
```

### 6. 在组件中使用

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