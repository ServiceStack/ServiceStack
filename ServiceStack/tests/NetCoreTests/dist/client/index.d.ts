export interface ApiRequest {
    getTypeName(): string;
    getMethod(): string;
    createResponse(): any;
}
export interface IReturnVoid {
    createResponse(): any;
}
export interface IReturn<T> {
    createResponse(): T;
}
export declare class ResponseStatus {
    constructor(init?: Partial<ResponseStatus>);
    errorCode: string;
    message: string;
    stackTrace: string;
    errors: ResponseError[];
    meta: {
        [index: string]: string;
    };
}
export declare class ResponseError {
    constructor(init?: Partial<ResponseError>);
    errorCode: string;
    fieldName: string;
    message: string;
    meta: {
        [index: string]: string;
    };
}
export declare class ErrorResponse {
    constructor(init?: Partial<ErrorResponse>);
    type: ErrorResponseType;
    responseStatus: ResponseStatus;
}
export declare class EmptyResponse {
    constructor(init?: Partial<ErrorResponse>);
    responseStatus: ResponseStatus;
}
export declare class NavItem {
    label: string;
    href: string;
    exact: boolean;
    id: string;
    className: string;
    iconClass: string;
    show: string;
    hide: string;
    children: NavItem[];
    meta: {
        [index: string]: string;
    };
    constructor(init?: Partial<NavItem>);
}
export declare class GetNavItems {
    constructor(init?: Partial<GetNavItems>);
    createResponse(): GetNavItemsResponse;
    getTypeName(): string;
    getMethod(): string;
}
export declare class GetNavItemsResponse {
    baseUrl: string;
    results: NavItem[];
    navItemsMap: {
        [index: string]: NavItem[];
    };
    meta: {
        [index: string]: string;
    };
    responseStatus: ResponseStatus;
    constructor(init?: Partial<GetNavItemsResponse>);
}
export declare class MetadataTypesConfig {
    baseUrl?: string;
    defaultNamespaces?: string[];
    defaultImports?: string[];
    includeTypes?: string[];
    excludeTypes?: string[];
    treatTypesAsStrings?: string[];
    globalNamespace?: string;
    ignoreTypes?: string[];
    exportTypes?: string[];
    exportAttributes?: string[];
    ignoreTypesInNamespaces?: string[];
    constructor(init?: Partial<MetadataTypesConfig>);
}
export declare class MetadataRoute {
    path?: string;
    verbs?: string;
    notes?: string;
    summary?: string;
    constructor(init?: Partial<MetadataRoute>);
}
export declare class MetadataOperationType {
    request?: MetadataType;
    response?: MetadataType;
    actions?: string[];
    returnsVoid?: boolean;
    returnType?: MetadataTypeName;
    routes?: MetadataRoute[];
    dataModel?: MetadataTypeName;
    viewModel?: MetadataTypeName;
    requiresAuth?: boolean;
    requiredRoles?: string[];
    requiresAnyRole?: string[];
    requiredPermissions?: string[];
    requiresAnyPermission?: string[];
    tags?: string[];
    constructor(init?: Partial<MetadataOperationType>);
}
export declare class MetadataTypes {
    config?: MetadataTypesConfig;
    namespaces?: string[];
    types?: MetadataType[];
    operations?: MetadataOperationType[];
    constructor(init?: Partial<MetadataTypes>);
}
export declare class MetadataTypeName {
    name?: string;
    namespace?: string;
    genericArgs?: string[];
    constructor(init?: Partial<MetadataTypeName>);
}
export declare class MetadataDataContract {
    name?: string;
    namespace?: string;
    constructor(init?: Partial<MetadataDataContract>);
}
export declare class MetadataDataMember {
    name?: string;
    order?: number;
    isRequired?: boolean;
    emitDefaultValue?: boolean;
    constructor(init?: Partial<MetadataDataMember>);
}
export declare class MetadataAttribute {
    name?: string;
    constructorArgs?: MetadataPropertyType[];
    args?: MetadataPropertyType[];
    constructor(init?: Partial<MetadataAttribute>);
}
export declare class MetadataPropertyType {
    name?: string;
    type?: string;
    isValueType?: boolean;
    isSystemType?: boolean;
    isEnum?: boolean;
    isPrimaryKey?: boolean;
    typeNamespace?: string;
    genericArgs?: string[];
    value?: string;
    description?: string;
    dataMember?: MetadataDataMember;
    readOnly?: boolean;
    paramType?: string;
    displayType?: string;
    isRequired?: boolean;
    allowableValues?: string[];
    allowableMin?: number;
    allowableMax?: number;
    attributes?: MetadataAttribute[];
    constructor(init?: Partial<MetadataPropertyType>);
}
export declare class MetadataType {
    name?: string;
    namespace?: string;
    genericArgs?: string[];
    inherits?: MetadataTypeName;
    implements?: MetadataTypeName[];
    displayType?: string;
    description?: string;
    isNested?: boolean;
    isEnum?: boolean;
    isEnumInt?: boolean;
    isInterface?: boolean;
    isAbstract?: boolean;
    dataContract?: MetadataDataContract;
    properties?: MetadataPropertyType[];
    attributes?: MetadataAttribute[];
    innerTypes?: MetadataTypeName[];
    enumNames?: string[];
    enumValues?: string[];
    enumMemberValues?: string[];
    enumDescriptions?: string[];
    meta?: {
        [index: string]: string;
    };
    constructor(init?: Partial<MetadataType>);
}
export declare type ErrorResponseType = null | "RefreshTokenException";
export interface IAuthSession {
    userName: string;
    displayName: string;
    userId?: string;
    roles?: string[];
    permissions?: string[];
    profileUrl?: string;
}
export interface IResolver {
    tryResolve(Function: any): any;
}
export declare class NewInstanceResolver implements IResolver {
    tryResolve(ctor: ObjectConstructor): any;
}
export declare class SingletonInstanceResolver implements IResolver {
    tryResolve(ctor: ObjectConstructor): any;
}
export interface ServerEventMessage {
    type: "ServerEventConnect" | "ServerEventHeartbeat" | "ServerEventJoin" | "ServerEventLeave" | "ServerEventUpdate" | "ServerEventMessage";
    eventId: number;
    channel: string;
    data: string;
    selector: string;
    json: string;
    op: string;
    target: string;
    cssSelector: string;
    body: any;
    meta: {
        [index: string]: string;
    };
}
export interface ServerEventCommand extends ServerEventMessage {
    userId: string;
    displayName: string;
    channels: string;
    profileUrl: string;
}
export interface ServerEventConnect extends ServerEventCommand {
    id: string;
    unRegisterUrl: string;
    heartbeatUrl: string;
    updateSubscriberUrl: string;
    heartbeatIntervalMs: number;
    idleTimeoutMs: number;
}
export interface ServerEventHeartbeat extends ServerEventCommand {
}
export interface ServerEventJoin extends ServerEventCommand {
}
export interface ServerEventLeave extends ServerEventCommand {
}
export interface ServerEventUpdate extends ServerEventCommand {
}
export interface IReconnectServerEventsOptions {
    url?: string;
    onerror?: (...args: any[]) => void;
    onmessage?: (...args: any[]) => void;
    error?: Error;
}
/**
 * EventSource
 */
export declare enum ReadyState {
    CONNECTING = 0,
    OPEN = 1,
    CLOSED = 2
}
export interface IEventSourceStatic extends EventTarget {
    new (url: string, eventSourceInitDict?: IEventSourceInit): IEventSourceStatic;
    url: string;
    withCredentials: boolean;
    CONNECTING: ReadyState;
    OPEN: ReadyState;
    CLOSED: ReadyState;
    readyState: ReadyState;
    onopen: Function;
    onmessage: (event: IOnMessageEvent) => void;
    onerror: Function;
    close: () => void;
}
export interface IEventSourceInit {
    withCredentials?: boolean;
}
export interface IOnMessageEvent {
    data: string;
}
export interface IEventSourceOptions {
    channels?: string;
    handlers?: any;
    receivers?: any;
    onException?: Function;
    onReconnect?: Function;
    onTick?: Function;
    resolver?: IResolver;
    validate?: (request: ServerEventMessage) => boolean;
    heartbeatUrl?: string;
    unRegisterUrl?: string;
    updateSubscriberUrl?: string;
    heartbeatIntervalMs?: number;
    heartbeat?: number;
    resolveStreamUrl?: (url: string) => string;
}
export declare class ServerEventsClient {
    channels: string[];
    options: IEventSourceOptions;
    eventSource: IEventSourceStatic;
    static UnknownChannel: string;
    eventStreamUri: string;
    updateSubscriberUrl: string;
    connectionInfo: ServerEventConnect;
    serviceClient: JsonServiceClient;
    stopped: boolean;
    resolver: IResolver;
    listeners: {
        [index: string]: ((e: ServerEventMessage) => void)[];
    };
    EventSource: IEventSourceStatic;
    withCredentials: boolean;
    constructor(baseUrl: string, channels: string[], options?: IEventSourceOptions, eventSource?: IEventSourceStatic);
    onMessage: (e: IOnMessageEvent) => void;
    _onMessage: (e: IOnMessageEvent) => void;
    onError: (error?: any) => void;
    getEventSourceOptions(): {
        withCredentials: boolean;
    };
    reconnectServerEvents(opt?: IReconnectServerEventsOptions): IEventSourceStatic;
    start(): this;
    stop(): Promise<void>;
    invokeReceiver(r: any, cmd: string, el: Element, request: ServerEventMessage, name: string): void;
    hasConnected(): boolean;
    registerHandler(name: string, fn: Function): this;
    setResolver(resolver: IResolver): this;
    registerReceiver(receiver: any): this;
    registerNamedReceiver(name: string, receiver: any): this;
    unregisterReceiver(name?: string): this;
    updateChannels(channels: string[]): void;
    update(subscribe: string | string[], unsubscribe: string | string[]): void;
    addListener(eventName: string, handler: ((e: ServerEventMessage) => void)): this;
    removeListener(eventName: string, handler: ((e: ServerEventMessage) => void)): this;
    raiseEvent(eventName: string, msg: ServerEventMessage): void;
    getConnectionInfo(): ServerEventConnect;
    getSubscriptionId(): string;
    updateSubscriber(request: UpdateEventSubscriber): Promise<void>;
    subscribeToChannels(...channels: string[]): Promise<void>;
    unsubscribeFromChannels(...channels: string[]): Promise<void>;
    getChannelSubscribers(): Promise<ServerEventUser[]>;
    toServerEventUser(map: {
        [id: string]: string;
    }): ServerEventUser;
}
export interface IReceiver {
    noSuchMethod(selector: string, message: any): any;
}
export declare class ServerEventReceiver implements IReceiver {
    client: ServerEventsClient;
    request: ServerEventMessage;
    noSuchMethod(selector: string, message: any): void;
}
export declare class UpdateEventSubscriber implements IReturn<UpdateEventSubscriberResponse> {
    id: string;
    subscribeChannels: string[];
    unsubscribeChannels: string[];
    createResponse(): UpdateEventSubscriberResponse;
    getTypeName(): string;
}
export declare class UpdateEventSubscriberResponse {
    responseStatus: ResponseStatus;
}
export declare class GetEventSubscribers implements IReturn<any[]> {
    channels: string[];
    createResponse(): any[];
    getTypeName(): string;
}
export declare class ServerEventUser {
    userId: string;
    displayName: string;
    profileUrl: string;
    channels: string[];
    meta: {
        [index: string]: string;
    };
}
export declare class HttpMethods {
    static Get: string;
    static Post: string;
    static Put: string;
    static Delete: string;
    static Patch: string;
    static Head: string;
    static Options: string;
    static hasRequestBody: (method: string) => boolean;
}
export interface IRequestFilterOptions {
    url: string;
}
export interface IRequestInit extends RequestInit {
    url?: string;
    compress?: boolean;
}
export interface Cookie {
    name: string;
    value: string;
    path: string;
    domain?: string;
    expires?: Date;
    httpOnly?: boolean;
    secure?: boolean;
    sameSite?: string;
}
export declare class GetAccessTokenResponse {
    accessToken: string;
    responseStatus: ResponseStatus;
}
export interface ISendRequest {
    method: string;
    request: any | null;
    body?: any | null;
    args?: any;
    url?: string;
    returns?: {
        createResponse: () => any;
    };
}
export declare class JsonServiceClient {
    baseUrl: string;
    replyBaseUrl: string;
    oneWayBaseUrl: string;
    mode: RequestMode;
    credentials: RequestCredentials;
    headers: Headers;
    userName: string;
    password: string;
    bearerToken: string;
    refreshToken: string;
    refreshTokenUri: string;
    useTokenCookie: boolean;
    enableAutoRefreshToken: boolean;
    requestFilter: (req: IRequestInit) => void;
    static globalRequestFilter: (req: IRequestInit) => void;
    responseFilter: (res: Response) => void;
    static globalResponseFilter: (res: Response) => void;
    exceptionFilter: (res: Response, error: any) => void;
    urlFilter: (url: string) => void;
    onAuthenticationRequired: () => Promise<any>;
    manageCookies: boolean;
    cookies: {
        [index: string]: Cookie;
    };
    parseJson: (res: Response) => Promise<any>;
    static toBase64: (rawString: string) => string;
    constructor(baseUrl?: string);
    setCredentials(userName: string, password: string): void;
    useBasePath(path?: string): this;
    set basePath(path: string | null);
    apply(f: (client: JsonServiceClient) => void): this;
    get<T>(request: IReturn<T> | string, args?: any): Promise<T>;
    delete<T>(request: IReturn<T> | string, args?: any): Promise<T>;
    post<T>(request: IReturn<T>, args?: any): Promise<T>;
    postToUrl<T>(url: string, request: IReturn<T>, args?: any): Promise<T>;
    postBody<T>(request: IReturn<T>, body: string | any, args?: any): Promise<T>;
    put<T>(request: IReturn<T>, args?: any): Promise<T>;
    putToUrl<T>(url: string, request: IReturn<T>, args?: any): Promise<T>;
    putBody<T>(request: IReturn<T>, body: string | any, args?: any): Promise<T>;
    patch<T>(request: IReturn<T>, args?: any): Promise<T>;
    patchToUrl<T>(url: string, request: IReturn<T>, args?: any): Promise<T>;
    patchBody<T>(request: IReturn<T>, body: string | any, args?: any): Promise<T>;
    publish(request: IReturnVoid, args?: any): Promise<any>;
    sendOneWay<T>(request: IReturn<T> | IReturnVoid, args?: any): Promise<T>;
    sendAll<T>(requests: IReturn<T>[]): Promise<T[]>;
    sendAllOneWay<T>(requests: IReturn<T>[]): Promise<void>;
    createUrlFromDto<T>(method: string, request: IReturn<T>): string;
    toAbsoluteUrl(relativeOrAbsoluteUrl: string): string;
    deleteCookie(name: string): void;
    private createRequest;
    private json;
    private applyResponseFilters;
    private createResponse;
    private handleError;
    fetch<T>(method: string, request: any | null, args?: any, url?: string): Promise<T>;
    fetchBody<T>(method: string, request: IReturn<T>, body: string | any, args?: any): Promise<T>;
    sendRequest<T>(info: ISendRequest): Promise<T>;
    raiseError(res: Response, error: any): any;
    send<T>(request: IReturn<T>, args?: any, url?: string): Promise<T>;
    sendVoid(request: IReturnVoid, args?: any, url?: string): Promise<EmptyResponse>;
    api<TResponse>(request: IReturn<TResponse> | ApiRequest, args?: any, method?: string): Promise<ApiResult<TResponse>>;
    apiVoid(request: IReturnVoid | ApiRequest, args?: any, method?: string): Promise<ApiResult<EmptyResponse>>;
    apiForm<TResponse>(request: IReturn<TResponse> | ApiRequest, body: FormData, args?: any, method?: string): Promise<ApiResult<TResponse>>;
    apiFormVoid(request: IReturnVoid | ApiRequest, body: FormData, args?: any, method?: string): Promise<ApiResult<EmptyResponse>>;
}
export declare function getMethod(request: any, method?: string): any;
export declare function getResponseStatus(e: any): any;
export interface ApiResponse {
    response?: any;
    error?: ResponseStatus;
    get completed(): boolean;
    get failed(): boolean;
    get succeeded(): boolean;
    get errorMessage(): string;
    get errorCode(): string;
    get errors(): ResponseError[];
    get errorSummary(): string;
}
export declare class ApiResult<TResponse> implements ApiResponse {
    response?: TResponse;
    error?: ResponseStatus;
    constructor(init?: Partial<ApiResult<TResponse>>);
    get completed(): boolean;
    get failed(): boolean;
    get succeeded(): boolean;
    get errorMessage(): string;
    get errorCode(): string;
    get errors(): ResponseError[];
    get errorSummary(): string;
    fieldError(fieldName: string): ResponseError;
    fieldErrorMessage(fieldName: string): string;
    hasFieldError(fieldName: string): boolean;
    showSummary(exceptFields?: string[]): boolean;
    summaryMessage(exceptFields?: string[]): string;
    addFieldError(fieldName: string, message: string, errorCode?: string): void;
}
export declare function createErrorStatus(message: string, errorCode?: string): ResponseStatus;
export declare function createFieldError(fieldName: string, message: string, errorCode?: string): ResponseStatus;
export declare function isFormData(body: any): boolean;
export declare function createError(errorCode: string, message: string, fieldName?: string): ErrorResponse;
export declare function toCamelCase(s: string): string;
export declare function toPascalCase(s: string): string;
export declare function sanitize(status: any): any;
export declare function nameOf(o: any): any;
export declare function css(selector: string | NodeListOf<Element>, name: string, value: string): void;
export declare function splitOnFirst(s: string, c: string): string[];
export declare function splitOnLast(s: string, c: string): string[];
export declare function leftPart(s: string, needle: string): string;
export declare function rightPart(s: string, needle: string): string;
export declare function lastLeftPart(s: string, needle: string): string;
export declare function lastRightPart(s: string, needle: string): string;
export declare function chop(str: string, len?: number): string;
export declare function onlyProps(obj: {
    [index: string]: any;
}, keys: string[]): {
    [index: string]: any;
};
export declare function humanize(s: any): any;
export declare const ucFirst: (s: string) => string;
export declare const isUpper: (c: string) => boolean;
export declare const isLower: (c: string) => boolean;
export declare const isDigit: (c: string) => boolean;
export declare function splitTitleCase(s: string): any[];
export declare const humanify: (s: any) => any;
export declare function queryString(url: string): any;
export declare function combinePaths(...paths: string[]): string;
export declare function createPath(route: string, args: any): string;
export declare function createUrl(route: string, args: any): string;
export declare function appendQueryString(url: string, args: any): string;
export declare function bytesToBase64(aBytes: Uint8Array): string;
export declare function stripQuotes(s: string): string;
export declare function tryDecode(s: string): string;
export declare function parseCookie(setCookie: string): Cookie;
export declare function normalizeKey(key: string): string;
export declare function normalize(dto: any, deep?: boolean): any;
export declare function getField(o: any, name: string): any;
export declare function parseResponseStatus(json: string, defaultMsg?: any): any;
export declare function toFormData(o: any): FormData;
export declare function toObject(keys: any): {};
export declare function errorResponseSummary(): any;
export declare function errorResponseExcept(fieldNames: string[] | string): any;
export declare function errorResponse(fieldName: string): any;
export declare function isDate(d: any): boolean;
export declare function toDate(s: string | any): Date;
export declare function toDateFmt(s: string): string;
export declare function padInt(n: number): string | number;
export declare function dateFmt(d?: Date): string;
export declare function dateFmtHM(d?: Date): string;
export declare function timeFmt12(d?: Date): string;
export declare function toLocalISOString(d?: Date): string;
export interface ICreateElementOptions {
    insertAfter?: Element | null;
}
export declare function createElement(tagName: string, options?: ICreateElementOptions, attrs?: any): HTMLElement;
export declare function $1(sel: string | any, el?: HTMLElement): any;
export declare function $$(sel: string | any, el?: HTMLElement): any;
export declare function on(sel: any, handlers: any): void;
export declare function delaySet(f: (loading: boolean) => any, opt?: {
    duration?: number;
}): () => void;
export declare function bootstrap(el?: Element): void;
export interface IBindHandlersOptions {
    events: string[];
}
export declare function bindHandlers(handlers: any, el?: Document | Element, opt?: IBindHandlersOptions): void;
export interface IAjaxFormOptions {
    type?: string;
    url?: string;
    model?: any;
    credentials?: RequestCredentials;
    validate?: (this: HTMLFormElement) => boolean;
    onSubmitDisable?: string;
    submit?: (this: HTMLFormElement, options: IAjaxFormOptions) => Promise<any>;
    success?: (this: HTMLFormElement, result: any) => void;
    error?: (this: HTMLFormElement, e: any) => void;
    complete?: (this: HTMLFormElement) => void;
    requestFilter?: (req: IRequestInit) => void;
    responseFilter?: (res: Response) => void;
    errorFilter?: (this: IValidation, message: string, errorCode: string, type: string) => void;
    messages?: {
        [index: string]: string;
    };
}
export declare function bootstrapForm(form: HTMLFormElement | null, options: IAjaxFormOptions): void;
export interface IValidation {
    overrideMessages: boolean;
    messages: {
        [index: string]: string;
    };
    errorFilter?: (this: IValidation, message: string, errorCode: string, type: string) => void;
}
export declare function toVarNames(names: string[] | string | null): string[];
export declare function formSubmit(this: HTMLFormElement, options?: IAjaxFormOptions): Promise<any>;
export declare function ajaxSubmit(f: HTMLFormElement, options?: IAjaxFormOptions): any;
export declare function serializeForm(form: HTMLFormElement, contentType?: string | null): string | FormData;
export declare function serializeToObject(form: HTMLFormElement): any;
export declare function serializeToUrlEncoded(form: HTMLFormElement): string;
export declare function serializeToFormData(form: HTMLFormElement): FormData;
export declare function triggerEvent(el: Element, name: string, data?: any): void;
export declare function populateForm(form: HTMLFormElement, model: any): void;
export declare function trimEnd(s: string, c: string): string;
export declare function safeVarName(s: string): string;
export declare function pick(o: any, keys: string[]): {};
export declare function omit(o: any, keys: string[]): {};
export declare function apply<T>(x: T, fn: (x: T) => void): T;
export declare function each(xs: any[], f: (acc: any, x: any) => void, o?: any): any;
export declare function resolve<T>(o: T, f?: (x: T) => any): any;
export declare function mapGet(o: any, name: string): any;
export declare function apiValue(o: any): any;
export declare function apiValueFmt(o: any): any;
export declare function activeClassNav(x: NavItem, activePath: string): string;
export declare function activeClass(href: string | null, activePath: string, exact?: boolean): string;
export declare const BootstrapColors: string[];
export declare function btnColorClass(props: any): string;
export declare const BootstrapSizes: string[];
export declare function btnSizeClass(props: any): string;
export declare function btnClasses(props: any): any[];
export declare class NavDefaults {
    static navClass: string;
    static navItemClass: string;
    static navLinkClass: string;
    static childNavItemClass: string;
    static childNavLinkClass: string;
    static childNavMenuClass: string;
    static childNavMenuItemClass: string;
    static create(): NavOptions;
    static forNav(options?: NavOptions | null): NavOptions;
    static overrideDefaults(targets: NavOptions | null | undefined, source: NavOptions): NavOptions;
    static showNav(navItem: NavItem, attributes: string[]): boolean;
}
export declare class NavLinkDefaults {
    static forNavLink(options?: NavOptions | null): NavOptions;
}
export declare class NavbarDefaults {
    static navClass: string;
    static create(): NavOptions;
    static forNavbar(options?: NavOptions | null): NavOptions;
}
export declare class NavButtonGroupDefaults {
    static navClass: string;
    static navItemClass: string;
    static create(): NavOptions;
    static forNavButtonGroup(options?: NavOptions | null): NavOptions;
}
export declare class LinkButtonDefaults {
    static navItemClass: string;
    static create(): NavOptions;
    static forLinkButton(options?: NavOptions | null): NavOptions;
}
export declare class UserAttributes {
    static fromSession(session: IAuthSession | null): string[];
}
export declare class NavOptions {
    static fromSession(session: IAuthSession | null, to?: NavOptions): NavOptions;
    attributes: string[];
    activePath?: string;
    baseHref?: string;
    navClass?: string;
    navItemClass?: string;
    navLinkClass?: string;
    childNavItemClass?: string;
    childNavLinkClass?: string;
    childNavMenuClass?: string;
    childNavMenuItemClass?: string;
    constructor(init?: Partial<NavOptions>);
}
export declare function classNames(...args: any[]): string;
export declare function fromXsdDuration(xsd: string): number;
export declare function toXsdDuration(time: number): string;
export declare function toTimeSpanFmt(time: number): string;
export declare function flatMap(f: Function, xs: any[]): any;
export declare function uniq(xs: string[]): string[];
export declare function enc(o: any): string;
export declare function htmlAttrs(o: any): string;
export declare function indexOfAny(str: string, needles: string[]): number;
export declare function isNullOrEmpty(o: any): boolean;
export declare function fromDateTime(dateTime: string): Date;
export declare function toDateTime(date: Date): string;
export declare function fromTimeSpan(xsdDuration: string): string;
export declare function toTimeSpan(xsdDuration: string): string;
export declare function fromGuid(xsdDuration: string): string;
export declare function toGuid(xsdDuration: string): string;
export declare function fromByteArray(base64: string): Uint8Array;
export declare function toByteArray(bytes: Uint8Array): string;
export declare function toBase64String(source: string): string;
export declare class StringBuffer {
    buffer_: string;
    constructor(opt_a1?: any, ...var_args: any[]);
    set(s: string): void;
    append(a1: any, opt_a2?: any, ...var_args: any[]): this;
    clear(): void;
    getLength(): number;
    toString(): string;
}
export declare class JSV {
    static ESCAPE_CHARS: string[];
    static encodeString(str: string): string;
    static encodeArray(array: any[]): string;
    static encodeObject(obj: any): string;
    static stringify(obj: any): any;
}
export declare function uniqueKeys(rows: any[]): string[];
export declare function alignLeft(str: string, len: number, pad?: string): string;
export declare function alignCenter(str: string, len: number, pad?: string): string;
export declare function alignRight(str: string, len: number, pad?: string): string;
export declare function alignAuto(obj: any, len: number, pad?: string): string;
export declare function EventBus(): void;
export declare class Inspect {
    static vars(obj: any): void;
    static dump(obj: any): string;
    static printDump(obj: any): void;
    static dumpTable(rows: any[]): string;
    static printDumpTable(rows: any[]): void;
}
