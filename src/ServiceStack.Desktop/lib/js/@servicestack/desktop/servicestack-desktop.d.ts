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
export interface IDesktopInfo {
    tool: string;
    toolVersion: string;
    chromeVersion: string;
}
export declare function desktopInfo(): Promise<IDesktopInfo>;
/**
 * Send Window to Foreground
 * @param windowName - The name of the window to send to foreground, supported: browser
 */
export declare function sendToForeground(windowName: string): Promise<IDesktopInfo>;
export declare function expandEnvVars(name: string): Promise<any>;
/**
 * Get Clipboard Contents as a UTF-8 string
 */
export declare function clipboard(): Promise<any>;
/**
 * Set the Clipboard Contents with a UTF-8 string
 */
export declare function setClipboard(contents: string): Promise<any>;
export declare enum OpenFolderFlags {
    AllowMultiSelect = 512,
    CreatePrompt = 8192,
    DontAddToRecent = 33554432,
    EnableHook = 32,
    EnableIncludeNotify = 4194304,
    EnableSizing = 8388608,
    EnableTemplate = 64,
    EnableTemplateHandle = 128,
    Explorer = 524288,
    ExtensionIsDifferent = 1024,
    FileMustExist = 4096,
    ForceShowHidden = 268435456,
    HideReadOnly = 4,
    LongNames = 2097152,
    NoChangeDir = 8,
    NoDereferenceLinks = 1048576,
    NoLongNames = 262144,
    NoNetworkButton = 131072,
    NoReadOnlyReturn = 32768,
    NoTestFileCreate = 65536,
    NoValidate = 256,
    OverwritePrompt = 2,
    PathMustExist = 2048,
    ReadOnly = 1,
    ShareAware = 16384,
    ShowHelp = 16
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
    folderPath: string;
    fileTitle: string;
    ok: boolean;
}
export declare function openFolder(options: OpenFolderOptions): Promise<DialogResult>;
export declare enum MessageBoxType {
    AbortRetryIgnore = 2,
    CancelTryContinue = 6,
    Help = 16384,
    Ok = 0,
    OkCancel = 1,
    RetryCancel = 5,
    YesNo = 4,
    YesNoCancel = 3,
    IconExclamation = 48,
    IconWarning = 48,
    IconInformation = 64,
    IconQuestion = 32,
    IconStop = 16,
    DefaultButton1 = 0,
    DefaultButton2 = 256,
    DefaultButton3 = 512,
    DefaultButton4 = 768,
    AppModal = 0,
    SystemModal = 4096,
    TaskModal = 8192,
    DefaultDesktopOnly = 131072,
    RightJustified = 524288,
    RightToLeftReading = 1048576,
    SetForeground = 65536,
    TopMost = 262144,
    ServiceNotification = 2097152
}
export declare enum MessageBoxReturn {
    Abort = 3,
    Cancel = 2,
    Continue = 11,
    Ignore = 5,
    No = 7,
    Ok = 1,
    Retry = 4,
    TryAgain = 10,
    Yes = 6
}
/**
 * Refer to Win32 API
 * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox
 * @param title
 * @param caption
 * @param type
 */
export declare function messageBox(title: string, caption?: string, type?: MessageBoxType): Promise<MessageBoxReturn>;
