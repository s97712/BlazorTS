
/**
 * @param {any} url
 * @param {Function} method
 * @param {any[]} args
 */

export async function InvokeWrapper(url, method, args) {
    const mod = await import(url);
    function isDotNetCallback(arg) {
        return arg && typeof arg === 'object' && 'obj' in arg && 'method' in arg;
    }

    args = args.map(arg => {
        if (isDotNetCallback(arg)) {
            return CreateCallback(arg);
        } else {
            return arg;
        }
    });
    return await mod[method](...args);
}

/**
 * @typedef DotNetCallback
 * @property {DotNetObject} obj
 * @property {string} method
 * @property {any} data
 */

/**
 * @param {DotNetCallback} callee
 */
export function CreateCallback(callee) {
    return (...args) => callee.obj.invokeMethodAsync(callee.method, callee.data, args)
}

