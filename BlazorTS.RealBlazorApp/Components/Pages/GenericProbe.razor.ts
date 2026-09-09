export interface Address {
    city: string;
}

export interface User {
    name: string;
    address: Address;
    tags?: string[];
}

export function describeCount(count: number): string {
    return `items:${count}`;
}

export function describeUser(user: User): string {
    return `${user.name}@${user.address.city}`;
}
