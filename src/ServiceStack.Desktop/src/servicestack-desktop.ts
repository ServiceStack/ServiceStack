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

function quote(text:string) {
    return '"' + text.replace('"','\\"') + '"';
}

export interface IDesktopInfo {
    tool: string;
    toolVersion: string;
    chromeVersion: string;
}

export async function desktopInfo() { return (await evaluateCode('desktopInfo')) as IDesktopInfo; }

/**
 * Send Window to Foreground
 * @param windowName - The name of the window to send to foreground, supported: browser 
 */
export async function sendToForeground(windowName:string) { 
    return (await evaluateCode('desktopInfo')) as IDesktopInfo; 
}

export async function expandEnvVars(name:string) { 
    return await evaluateCode(`expandEnvVars(${quote(name)})`); 
}

/**
 * Get Clipboard Contents as a UTF-8 string
 */
export async function clipboard() { return await evaluateCode('clipboard'); }

/**
 * Set the Clipboard Contents with a UTF-8 string
 */
export async function setClipboard(contents:string) { 
    return await evaluateCode(`setClipboard(${quote(contents)})`); 
}

export enum OpenFolderFlags {
    AllowMultiSelect = 0x00000200,
    CreatePrompt = 0x00002000,
    DontAddToRecent = 0x02000000,
    EnableHook = 0x00000020,
    EnableIncludeNotify = 0x00400000,
    EnableSizing = 0x00800000,
    EnableTemplate = 0x00000040,
    EnableTemplateHandle = 0x00000080,
    Explorer = 0x00080000,
    ExtensionIsDifferent = 0x00000400,
    FileMustExist = 0x00001000,
    ForceShowHidden = 0x10000000,
    HideReadOnly = 0x00000004,
    LongNames = 0x00200000,
    NoChangeDir = 0x00000008,
    NoDereferenceLinks = 0x00100000,
    NoLongNames = 0x00040000,
    NoNetworkButton = 0x00020000,
    NoReadOnlyReturn = 0x00008000,
    NoTestFileCreate = 0x00010000,
    NoValidate = 0x00000100,
    OverwritePrompt = 0x00000002,
    PathMustExist = 0x00000800,
    ReadOnly = 0x00000001,
    ShareAware = 0x00004000,
    ShowHelp = 0x00000010,
}

/**
 * Refer to the Win32 GetOpenFileName options at:
 * https://docs.microsoft.com/en-us/windows/win32/api/commdlg/ns-commdlg-openfilenamea
 */
export interface OpenFolderOptions {
    flags?: OpenFolderFlags;
    title?: string;
    filter?: string;
    initialDir?: string;
    defaultExt?: string;
}

export interface DialogResult {
    folderPath:string;
    fileTitle:string;
    ok:boolean;
}

export async function openFolder(options:OpenFolderOptions) {
    return (await evaluateCode(`openFolder(${JSON.stringify(options)})`)) as DialogResult;
}

export enum MessageBoxType {
    AbortRetryIgnore = 0x00000002,
    CancelTryContinue = 0x00000006,
    Help = 0x00004000,
    Ok = 0x00000000,
    OkCancel = 0x00000001,
    RetryCancel = 0x00000005,
    YesNo = 0x00000004,
    YesNoCancel = 0x00000003,
    IconExclamation = 0x00000030,
    IconWarning = 0x00000030,
    IconInformation = 0x00000040,
    IconQuestion = 0x00000020,
    IconStop = 0x00000010,
    DefaultButton1 = 0x00000000,
    DefaultButton2 = 0x00000100,
    DefaultButton3 = 0x00000200,
    DefaultButton4 = 0x00000300,
    AppModal = 0x00000000,
    SystemModal = 0x00001000,
    TaskModal = 0x00002000,
    DefaultDesktopOnly = 0x00020000,
    RightJustified = 0x00080000,
    RightToLeftReading = 0x00100000,
    SetForeground = 0x00010000,
    TopMost = 0x00040000,
    ServiceNotification = 0x00200000,
}

export enum MessageBoxReturn {
    Abort = 3,
    Cancel = 2,
    Continue = 11,
    Ignore = 5,
    No = 7,
    Ok = 1,
    Retry = 4,
    TryAgain = 10,
    Yes = 6,
}

/**
 * Refer to Win32 API 
 * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox
 * @param title
 * @param caption
 * @param type
 */
export async function messageBox(title:string, caption:string="", type:MessageBoxType=MessageBoxType.Ok) {
    return (await evaluateCode(`messageBox(${quote(title)},${quote(caption)},${type})`)) as MessageBoxReturn;
}
