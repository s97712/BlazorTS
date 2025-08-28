# BlazorTS 支持的 TypeScript 语法

本文档列出了 BlazorTS 源代码生成器当前支持的 TypeScript 函数语法形式。

## ✅ 支持的导出函数语法

### 1. 标准函数声明

```typescript
// 基本函数
export function add(a: number, b: number): number {
    return a + b;
}

// 无返回类型（默认为 void）
export function log(message: string) {
    console.log(message);
}

// 显式 void 返回类型
export function initialize(): void {
    console.log('initialized');
}
```

### 2. 异步函数

```typescript
// 异步函数
export async function fetchData(url: string): Promise<any> {
    return fetch(url);
}

// 异步 void 函数
export async function delay(ms: number): Promise<void> {
    await new Promise(resolve => setTimeout(resolve, ms));
}
```

### 3. 箭头函数导出

```typescript
// 基本箭头函数
export const multiply = (a: number, b: number): number => a * b;

// 带类型注解的箭头函数
export const greet = (name: string): string => {
    return `Hello, ${name}!`;
};

// 异步箭头函数
export const fetchUser = async (id: number): Promise<User> => {
    return await api.getUser(id);
};

// 无参数箭头函数
export const getCurrentTime = (): Date => new Date();

// 单参数箭头函数（无括号）
export const double = (x: number) => x * 2;
```

### 4. 函数表达式导出

```typescript
// 命名函数表达式
export const processData = function(data: any[]): any[] {
    return data.filter(item => item.isValid);
};

// 匿名函数表达式
export const validator = function(input: string): boolean {
    return input.length > 0;
};
```

### 5. 参数类型支持

```typescript
// 基本类型
export function basicTypes(
    str: string,
    num: number,
    bool: boolean,
    obj: any
): void { }

// 可选参数
export function withOptional(
    required: string,
    optional?: number
): boolean {
    return optional !== undefined;
}

// 联合类型
export function unionTypes(value: string | number): string {
    return String(value);
}

// 泛型（基本支持）
export function generic<T>(value: T): T {
    return value;
}
```

## ❌ 不支持的语法

以下语法形式**不会**被提取：

### 1. 非导出函数

```typescript
// 这些函数会被忽略
function privateFunction() { }
async function privateAsync() { }
const privateArrow = () => { };
```

### 2. 类方法

```typescript
class MyClass {
    // 类方法不会被提取
    method() { }
    async asyncMethod() { }
}
```

### 3. 命名空间函数

```typescript
namespace MyNamespace {
    // 命名空间内的函数不会被提取
    export function namespacedFunction() { }
}
```

### 4. 重新导出

```typescript
// 重新导出语法不支持
export { someFunction } from './other-module';
```

### 5. 默认导出（部分限制）

```typescript
// 默认导出函数有限支持
export default function defaultFunction() { }
```

## 📝 参数和返回类型映射

| TypeScript 类型 | C# 类型 | 说明 |
|-----------------|---------|------|
| `string` | `string` | 字符串 |
| `number` | `double` | 数字 |
| `boolean` | `bool` | 布尔值 |
| `any` | `object` | 任意类型 |
| `void` | `Task` | 无返回值 |
| `Promise<T>` | `Task<T>` | 异步返回 |
| `Promise<void>` | `Task` | 异步无返回值 |

## 🔧 最佳实践

1. **明确导出**：只有使用 `export` 关键字的函数会被生成
2. **类型注解**：尽量提供完整的类型注解以获得更好的 C# 类型映射
3. **参数命名**：使用有意义的参数名，它们会保留在生成的 C# 代码中
4. **返回类型**：明确指定返回类型，避免依赖类型推断

## 📖 示例

完整的 TypeScript 文件示例：

```typescript
// ✅ 支持的函数都会被生成为 C# 方法

export function calculateTotal(items: number[]): number {
    return items.reduce((sum, item) => sum + item, 0);
}

export async function saveData(data: any): Promise<boolean> {
    try {
        await api.save(data);
        return true;
    } catch {
        return false;
    }
}

export const formatCurrency = (amount: number): string => {
    return `$${amount.toFixed(2)}`;
};

// ❌ 这个函数不会被生成
function helperFunction() {
    return 'internal use only';
}
```

生成的 C# 代码将包含对应的方法签名和调用逻辑。