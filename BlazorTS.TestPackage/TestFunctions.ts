// 导出的标准函数
export async function hello(name: string): Promise<string> {
    return `Hello, ${name}!`;
}

export function add(a: number, b: number): number {
    return a + b;
}

export function greet(name: string, age?: number): void {
    console.log(`Hi ${name}, you are ${age || 'unknown'} years old`);
}

// 非导出的标准函数 - 这些应该被忽略
function privateFunction(x: number): boolean {
    return x > 0;
}

async function privateAsyncFunction(): Promise<void> {
    console.log('private async');
}

// 导出的箭头函数
export const arrowFunction = (value: string): string => {
    return value.toLowerCase();
};

export const asyncArrowFunction = async (id: number): Promise<string> => {
    return `ID: ${id}`;
};

// 非导出的箭头函数 - 这些应该被忽略
const privateArrowFunction = (x: number) => x * 2;

// 导出的函数表达式
export const functionExpression = function(name: string): string {
    return `Hello ${name}`;
};

// 默认导出函数
export default function defaultFunction(message: string): void {
    console.log(message);
}

// 非导出的函数表达式 - 应该被忽略
const privateFunctionExpression = function() {
    return 'private';
};