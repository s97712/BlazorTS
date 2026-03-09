# BlazorTS 支持的 TypeScript 语法

本文档列出了 BlazorTS 源代码生成器当前支持的 TypeScript 导出函数语法，以及这些函数在当前生成模型中的映射方式。

## 当前生成模型

BlazorTS 目前有两类生成目标：

1. **`.razor.ts`**：
   - 生成组件对应的 `partial class`
   - 生成顶层 `*TSInterop` 互操作类
   - 在组件中自动注入 `Scripts`

2. **`.entry.ts`**：
   - 生成普通可注入服务类

不论使用哪种文件类型，函数提取规则是一致的，区别只在于最终 C# 包装类的落点不同。

## ✅ 支持的导出函数语法

### 1. 标准函数声明

```typescript
export function add(a: number, b: number): number {
    return a + b;
}

export function log(message: string) {
    console.log(message);
}

export function initialize(): void {
    console.log('initialized');
}
```

### 2. 异步函数

```typescript
export async function fetchData(url: string): Promise<any> {
    return fetch(url);
}

export async function delay(ms: number): Promise<void> {
    await new Promise(resolve => setTimeout(resolve, ms));
}
```

### 3. 箭头函数导出

```typescript
export const multiply = (a: number, b: number): number => a * b;

export const greet = (name: string): string => {
    return `Hello, ${name}!`;
};

export const fetchUser = async (id: number): Promise<User> => {
    return await api.getUser(id);
};

export const getCurrentTime = (): Date => new Date();
export const double = (x: number) => x * 2;
```

### 4. 函数表达式导出

```typescript
export const processData = function(data: any[]): any[] {
    return data.filter(item => item.isValid);
};

export const validator = function(input: string): boolean {
    return input.length > 0;
};
```

### 5. 参数类型支持

```typescript
export function basicTypes(
    str: string,
    num: number,
    bool: boolean,
    obj: any
): void { }

export function withOptional(
    required: string,
    optional?: number
): boolean {
    return optional !== undefined;
}

export function unionTypes(value: string | number): string {
    return String(value);
}

export function generic<T>(value: T): T {
    return value;
}
```

## ❌ 不支持的语法

以下语法形式不会被提取：

### 1. 非导出函数

```typescript
function privateFunction() { }
async function privateAsync() { }
const privateArrow = () => { };
```

### 2. 类方法

```typescript
class MyClass {
    method() { }
    async asyncMethod() { }
}
```

### 3. 命名空间函数

```typescript
namespace MyNamespace {
    export function namespacedFunction() { }
}
```

### 4. 重新导出

```typescript
export { someFunction } from './other-module';
```

### 5. 默认导出（有限支持）

```typescript
export default function defaultFunction() { }
```

## 📝 参数和返回类型映射

| TypeScript 类型 | C# 类型 | 说明 |
|-----------------|---------|------|
| `string` | `string` | 字符串 |
| `number` | `double` | 数字 |
| `boolean` | `bool` | 布尔值 |
| `any` | `object?` | 任意类型 |
| `void` | `Task` | 无返回值 |
| `Promise<T>` | `Task<T>` | 异步返回 |
| `Promise<void>` | `Task` | 异步无返回值 |

## 🔧 最佳实践

1. 仅导出需要暴露给 C# 的函数。
2. 尽量提供明确的类型注解，以获得稳定的 C# 映射结果。
3. 使用清晰的参数名，它们会直接出现在生成的 C# 方法签名中。
4. 对返回类型进行显式声明，避免不必要的推断差异。

## 📖 示例

### Razor 组件脚本

```typescript
// MyComponent.razor.ts
export function showMessage(message: string): void {
    alert(message);
}
```

概念化生成结果：

```csharp
public partial class MyComponent
{
    [Inject] public MyComponentTSInterop Scripts { get; set; } = null!;
}

public class MyComponentTSInterop(ScriptBridge invoker)
{
    private readonly string url = invoker.ResolveNS("MyApp.Components.MyComponent", ".razor");

    public async Task showMessage(string message)
    {
        await invoker.InvokeAsync<object?>(url, "showMessage", new object?[] { message });
    }
}
```

### Entry 模块

```typescript
// utils.entry.ts
export function calculateTotal(items: number[]): number {
    return items.reduce((sum, item) => sum + item, 0);
}

export async function saveData(data: any): Promise<boolean> {
    try {
        return true;
    } catch {
        return false;
    }
}
```

概念化生成结果：

```csharp
public class Utils(ScriptBridge invoker)
{
    private readonly string url = invoker.ResolveNS(typeof(Utils), ".entry");
}
```
