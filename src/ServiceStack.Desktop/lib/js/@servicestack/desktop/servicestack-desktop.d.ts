export declare function invokeHostJsonMethod(target: string, args: {
    [id: string]: any;
}): Promise<any>;
export declare function invokeHostTextMethod(target: string, args: {
    [id: string]: any;
}): Promise<string>;
export declare function combinePaths(...paths: string[]): string;
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
export interface DesktopInfo {
    tool: string;
    toolVersion: string;
    chromeVersion: string;
}
export declare function evalToBool(scriptSrc: string): Promise<boolean>;
export declare function evalToBoolAsync(scriptSrc: string): Promise<boolean>;
export declare function desktopInfo(): Promise<DesktopInfo>;
export declare function openUrl(url: string): Promise<boolean>;
export declare function start(url: string): Promise<boolean>;
export declare function expandEnvVars(name: string): Promise<any>;
export declare function findWindowByName(name: string): Promise<number>;
/**
 * Get Clipboard Contents as a UTF-8 string
 */
export declare function clipboard(): Promise<any>;
/**
 * Set the Clipboard Contents with a UTF-8 string
 */
export declare function setClipboard(contents: string): Promise<boolean>;
export interface Size {
    width: number;
    height: number;
}
export interface Rectangle {
    top: number;
    left: number;
    bottom: number;
    right: number;
}
export interface MonitorInfo {
    monitor: Rectangle;
    work: Rectangle;
    flags: number;
}
export declare function deviceScreenResolution(): Promise<Size>;
export declare function primaryMonitorInfo(): Promise<MonitorInfo>;
export declare function windowSendToForeground(): Promise<boolean>;
export declare function windowCenterToScreen(useWorkArea?: boolean): Promise<boolean>;
export declare function windowSetFullScreen(): Promise<boolean>;
export declare function windowSetFocus(): Promise<boolean>;
export declare function windowShowScrollBar(show: boolean): Promise<boolean>;
export declare function windowSetPosition(x: number, y: number, width?: number, height?: number): Promise<boolean>;
export declare function windowSetSize(width: number, height: number): Promise<boolean>;
export declare function windowRedrawFrame(): Promise<boolean>;
export declare function windowIsVisible(): Promise<boolean>;
export declare function windowIsEnabled(): Promise<boolean>;
export declare function windowShow(): Promise<boolean>;
export declare function windowHide(): Promise<boolean>;
export declare function windowText(): Promise<any>;
export declare function windowSetText(text: string): Promise<boolean>;
export declare function windowSize(): Promise<Size>;
export declare function windowClientSize(): Promise<Size>;
export declare function windowClientRect(): Promise<Rectangle>;
export declare function windowSetState(state: ShowWindowCommands): Promise<boolean>;
export declare function knownFolder(folder: KnownFolders): Promise<string>;
export declare function desktopTextFile(fileName: string): Promise<string>;
export declare function desktopSaveTextFile(fileName: string, body: string): Promise<string>;
export declare function desktopDownloadsTextFile(fileName: string): Promise<string>;
export declare function desktopSaveDownloadsTextFile(fileName: string, body: string): Promise<string>;
export declare function desktopSaveDownloadUrl(fileName: string, url: string): string;
/**
 * refer to http://pinvoke.net/default.aspx/Enums/ShowWindowCommand.html
 */
export declare enum ShowWindowCommands {
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
    ShowMinNoActive = 7,
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
    ForceMinimize = 11
}
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
    file: string | null;
    fileTitle: string | null;
    ok: boolean | null;
}
export declare function openFile(options: OpenFileOptions): Promise<DialogResult>;
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
export declare enum KnownFolders {
    Contacts = "Contacts",
    Desktop = "Desktop",
    Documents = "Documents",
    Downloads = "Downloads",
    Favorites = "Favorites",
    Links = "Links",
    Music = "Music",
    Pictures = "Pictures",
    SavedGames = "SavedGames",
    SavedSearches = "SavedSearches",
    Videos = "Videos"
}
/**
 * Refer to Win32 API
 * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox
 * @param title
 * @param caption
 * @param type
 */
export declare function messageBox(title: string, caption?: string, type?: MessageBoxType): Promise<MessageBoxReturn>;
