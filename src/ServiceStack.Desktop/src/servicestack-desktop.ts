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

export function combinePaths(...paths: string[]): string {
    let parts = [], i, l;
    for (i = 0, l = paths.length; i < l; i++) {
        const arg = paths[i];
        parts = arg.indexOf("://") === -1
            ? parts.concat(arg.split("/"))
            : parts.concat(arg.lastIndexOf("/") === arg.length - 1 ? arg.substring(0, arg.length - 1) : arg);
    }
    const combinedPaths = [];
    for (i = 0, l = parts.length; i < l; i++) {
        const part = parts[i];
        if (!part || part === ".") continue;
        if (part === "..") combinedPaths.pop();
        else combinedPaths.push(part);
    }
    if (parts[0] === "") combinedPaths.unshift("");
    return combinedPaths.join("/") || (combinedPaths.length ? "/" : ".");
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

export function quote(text:string) {
    return '"' + text.replace('"','\\"') + '"';
}

export interface DesktopInfo {
    tool: string;
    toolVersion: string;
    chromeVersion: string;
}

export async function evalToBool(scriptSrc:string) {
    return await evaluateCode(scriptSrc) as boolean;
}

export async function evalToBoolAsync(scriptSrc:string) {
    return await evaluateCodeAsync(scriptSrc) as boolean;
}

export async function desktopInfo() { return (await evaluateCode('desktopInfo')) as DesktopInfo; }

export async function openUrl(url:string) { return (await evalToBool(`openUrl(${quote(url)})`)); }

export async function start(url:string) { return (await evalToBool(`start(${quote(url)})`)); }

export async function expandEnvVars(name:string) {
    return await evaluateCode(`expandEnvVars(${quote(name)})`);
}

export async function findWindowByName(name:string) {
    return parseInt(await evaluateCode(`findWindowByName(${quote(name)})`));
}

/**
 * Get Clipboard Contents as a UTF-8 string
 */
export async function clipboard() { return await evaluateCode('clipboard'); }

/**
 * Set the Clipboard Contents with a UTF-8 string
 */
export async function setClipboard(contents:string) { 
    return await evalToBool(`setClipboard(${quote(contents)})`); 
}

export interface Size {
    width:number;
    height:number;
}
export interface Rectangle {
    top:number;
    left:number;
    bottom:number;
    right:number;
}
export interface MonitorInfo {
    monitor:Rectangle;
    work:Rectangle;
    flags:number;
}
export async function deviceScreenResolution() {
    return (await evaluateCode('deviceScreenResolution')) as Size;
}
export async function primaryMonitorInfo() {
    return (await evaluateCode('primaryMonitorInfo')) as MonitorInfo;
}
export async function windowSendToForeground() { return await evalToBool('windowSendToForeground'); }
export async function windowCenterToScreen(useWorkArea?:boolean) { 
    return await evalToBool(useWorkArea ? `windowCenterToScreen(${useWorkArea})` : `windowCenterToScreen`); 
}
export async function windowSetFullScreen() { return await evalToBoolAsync('windowSetFullScreen'); }
export async function windowSetFocus() { return await evalToBool('windowSetFocus'); }
export async function windowShowScrollBar(show:boolean) { return await evalToBoolAsync(`windowShowScrollBar(${show})`); }
export async function windowSetPosition(x:number,y:number,width?:number,height?:number) { 
    return await evalToBoolAsync(width 
        ? `windowSetPosition(${x},${y},${width},${height})`
        : `windowSetPosition(${x},${y})`); 
}
export async function windowSetSize(width:number,height:number) { 
    return await evalToBoolAsync(`windowSetSize(${width},${height})`);
}
export async function windowRedrawFrame() { return await evalToBool('windowRedrawFrame'); }
export async function windowIsVisible() { return await evalToBool('windowIsVisible'); }
export async function windowIsEnabled() { return await evalToBool('windowIsEnabled'); }
export async function windowShow() { return await evalToBool('windowShow'); }
export async function windowHide() { return await evalToBool('windowHide'); }
export async function windowText() { return await evaluateCode('windowText'); }
export async function windowSetText(text:string) { return await evalToBool(`windowSetText(${quote(text)})`); }

export async function windowSize() {
    return await evaluateCode('windowSize') as Size;
}
export async function windowClientSize() {
    return await evaluateCode('windowClientSize') as Size;
}
export async function windowClientRect() {
    return await evaluateCode('windowClientRect') as Rectangle;
}
export async function windowSetState(state:ShowWindowCommands) {
    return await evalToBool(`windowSetState(${state})`);
}

export async function knownFolder(folder:KnownFolders) {
    return await evaluateCode(`knownFolder(${quote(folder)})`) as string;
}

async function desktopFolderTextFile(folder:string,fileName:string) {
    const r = await fetch(`/desktop/${folder}/${fileName}`);
    if (!r.ok)
        throw `${r.status} ${r.statusText}`;
    return await r.text();
}
async function desktopSaveFolderTextFile(folder:string,fileName:string,body:string) {
    try {
        const r = await fetch(`/desktop/${folder}/${fileName}`, {
            method: "POST",
            body
        });
        if (!r.ok)
            throw `${r.status} ${r.statusText}`;
        const contents = await r.text();
        return contents;
    } catch (e) {
        throw e;
    }
}

export async function desktopTextFile(fileName:string) {
    return await desktopFolderTextFile('files',fileName);
}
export async function desktopSaveTextFile(fileName:string,body:string) {
    return await desktopSaveFolderTextFile('files',fileName,body);
}
export async function desktopDownloadsTextFile(fileName:string) {
    return await desktopFolderTextFile('downloads',fileName);
}
export async function desktopSaveDownloadsTextFile(fileName:string,body:string) {
    return await desktopSaveFolderTextFile('downloads',fileName,body);
}
export function desktopSaveDownloadUrl(fileName:string, url:string) {
    return combinePaths('/desktop/downloads', encodeURIComponent(fileName), 'url', encodeURIComponent(url));
}


/**
 * refer to http://pinvoke.net/default.aspx/Enums/ShowWindowCommand.html
 */
export enum ShowWindowCommands {
    /**
     * Hides the window and activates another window.
     */
    Hide = 0,
    /**
     * Activates and displays a window. If the window is minimized or
     * maximized, the system restores it to its original size and position.
     * An application should specify this flag when displaying the window
     * for the first time.
     */
    Normal = 1,
    /**
     * Activates the window and displays it as a minimized window.
     */
    ShowMinimized = 2,
    /**
     * Maximizes the specified window
     */
    Maximize = 3,
    /**
     * Activates the window and displays it as a maximized window.
     */
    ShowMaximized = 3,
    /**
     * Displays a window in its most recent size and position. This value
     * is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
     * the window is not activated.
     */
    ShowNoActivate = 4,
    /**
     * Activates the window and displays it in its current size and position.
     */
    Show = 5,
    /**
     * Minimizes the specified window and activates the next top-level
     * window in the Z order.
     */
    Minimize = 6,
    /**
     * Displays the window as a minimized window. This value is similar to
     * <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
     * window is not activated.
     */
    ShowMinNoActive = ShowMinimized | Show,
    /**
     * Displays the window in its current size and position. This value is
     * similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
     * window is not activated.
     */
    ShowNA = 8,
    /**
     * Activates and displays the window. If the window is minimized or
     * maximized, the system restores it to its original size and position.
     * An application should specify this flag when restoring a minimized window.
     */
    Restore = 9,
    /**
     * Sets the show state based on the SW_* value specified in the
     * STARTUPINFO structure passed to the CreateProcess function by the
     * program that started the application.
     */
    ShowDefault = 10,
    /**
     * Windows 2000/XP: Minimizes a window, even if the thread
     * that owns the window is not responding. This flag should only be
     * used when minimizing windows from a different thread.
     */
    ForceMinimize = 11,
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
export interface OpenFileOptions {
    flags?: OpenFolderFlags;
    title?: string;
    filter?: string;
    filterIndex?: number;
    initialDir?: string;
    defaultExt?: string;
    templateName?: string;
    isFolderPicker?: boolean;
}

export interface DialogResult {
    file: string|null;
    fileTitle: string|null;
    ok: boolean|null;
}
export async function openFile(options:OpenFileOptions) {
    return (await evaluateCode(`openFile(${JSON.stringify(options)})`)) as DialogResult;
}

export interface OpenFolderOptions {
    title?: string;
    initialDir?: string;
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

export enum KnownFolders {
    Contacts = 'Contacts',
    Desktop = 'Desktop',
    Documents = 'Documents',
    Downloads = 'Downloads',
    Favorites = 'Favorites',
    Links = 'Links',
    Music = 'Music',
    Pictures = 'Pictures',
    SavedGames = 'SavedGames',
    SavedSearches = 'SavedSearches',
    Videos = 'Videos',
}

/**
 * Refer to Win32 API 
 * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox
 * @param title
 * @param caption
 * @param type
 */
export async function messageBox(title:string, caption:string="", type:MessageBoxType=MessageBoxType.Ok|MessageBoxType.TopMost) {
    return (await evaluateCode(`messageBox(${quote(title)},${quote(caption)},${type})`)) as MessageBoxReturn;
}
