export async function hello(name: string): Promise<string> {
    return `Hello, ${name}!`;
}

export function add(a: number, b: number): number {
    return a + b;
}

export function greet(name: string, age?: number): void {
    console.log(`Hi ${name}, you are ${age || 'unknown'} years old`);
}