export async function invokeHostJsonMethod(target:string, args:{[id:string]:any}) {
    const formData = new FormData();
    for (var k in args) {
        if (!args.hasOwnProperty(k)) continue;
        formData.append(k, args[k]);
    }
    try {
        const r = await fetch(`https://host/script`, {
            method: "POST",
            body: formData
        });
        const body = await r.text();
        if (r.ok)
            return body.length > 0 ? JSON.parse(body) : null;
        throw body;
    } catch (e) {
        throw e;
    }
}

export async function invokeHostTextMethod(target:string, args:{[id:string]:any}) {
    const formData = new FormData();
    for (var k in args) {
        if (!args.hasOwnProperty(k)) continue;
        formData.append(k, args[k]);
    }
    try {
        const r = await fetch(`https://host/script`, {
            method: "POST",
            body: formData
        });
        return await r.text();
    } catch (e) {
        throw e;
    }
}

export async function evaluateScript(scriptSrc:string) {
    return await invokeHostJsonMethod('script', { 'EvaluateScript':scriptSrc });
}

export async function evaluateCode(scriptSrc:string) {
    return await invokeHostJsonMethod('script', { 'EvaluateCode':scriptSrc });
}

export async function evaluateLisp(scriptSrc:string) {
    return await invokeHostJsonMethod('script', { 'EvaluateLisp':scriptSrc });
}

export async function renderScript(scriptSrc:string) {
    return await invokeHostTextMethod('script', { 'RenderScript':scriptSrc });
}

export async function renderCode(scriptSrc:string) {
    return await invokeHostTextMethod('script', { 'RenderCode':scriptSrc });
}

export async function renderLisp(scriptSrc:string) {
    return await invokeHostTextMethod('script', { 'RenderLisp':scriptSrc });
}


export async function evaluateScriptAsync(scriptSrc:string) {
    return await invokeHostJsonMethod('script', { 'EvaluateScriptAsync':scriptSrc });
}

export async function evaluateCodeAsync(scriptSrc:string) {
    return await invokeHostJsonMethod('script', { 'EvaluateCodeAsync':scriptSrc });
}

export async function evaluateLispAsync(scriptSrc:string) {
    return await invokeHostJsonMethod('script', { 'EvaluateLispAsync':scriptSrc });
}

export async function renderScriptAsync(scriptSrc:string) {
    return await invokeHostTextMethod('script', { 'RenderScriptAsync':scriptSrc });
}

export async function renderCodeAsync(scriptSrc:string) {
    return await invokeHostTextMethod('script', { 'RenderCodeAsync':scriptSrc });
}

export async function renderLispAsync(scriptSrc:string) {
    return await invokeHostTextMethod('script', { 'RenderLispAsync':scriptSrc });
}
