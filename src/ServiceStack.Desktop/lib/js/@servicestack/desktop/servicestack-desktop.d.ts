export declare function invokeHostJsonMethod(target: string, args: {
    [id: string]: any;
}): Promise<any>;
export declare function invokeHostTextMethod(target: string, args: {
    [id: string]: any;
}): Promise<string>;
export declare function evaluateScript(scriptSrc: string): Promise<any>;
export declare function evaluateCode(scriptSrc: string): Promise<any>;
export declare function evaluateLisp(scriptSrc: string): Promise<any>;
export declare function renderScript(scriptSrc: string): Promise<string>;
export declare function renderCode(scriptSrc: string): Promise<string>;
export declare function renderLisp(scriptSrc: string): Promise<string>;
export declare function evaluateScriptAsync(scriptSrc: string): Promise<any>;
export declare function evaluateCodeAsync(scriptSrc: string): Promise<any>;
export declare function evaluateLispAsync(scriptSrc: string): Promise<any>;
export declare function renderScriptAsync(scriptSrc: string): Promise<string>;
export declare function renderCodeAsync(scriptSrc: string): Promise<string>;
export declare function renderLispAsync(scriptSrc: string): Promise<string>;
