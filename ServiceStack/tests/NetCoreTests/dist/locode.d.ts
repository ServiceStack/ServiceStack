import { ApiResult, JsonServiceClient } from './client'
import { App, Forms, MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from './shared'

/** @param {Function} [fn]
 *  @return {JsonServiceClient}
 */
export function createClient(fn?: Function): JsonServiceClient;
export let client: JsonServiceClient;
export function resolveApiUrl(op: string): any;
/** @type {{expanded: boolean, operations: MetadataOperationType[], tag: string}[]} */
export let sideNav: {
    expanded: boolean;
    operations: MetadataOperationType[];
    tag: string;
}[];
export let CACHE: {};
export let HttpErrors: Record<number, string>;
export let OpsMap: Record<string, MetadataOperationType>;
export let TypesMap: Record<string, MetadataType>;
export let FullTypesMap: Record<string, MetadataType>;
export let getOp: (opName: string) => MetadataOperationType;
export let getType: (typeRef: string | {
    namespace: string;
    name: string;
}) => MetadataType;
export let isEnum: (type: string) => boolean;
export let enumValues: (type: string) => {
    key: string;
    value: string;
}[];
export let getIcon: ({ op, type }: {
    op: MetadataOperationType;
    type: MetadataType;
}) => {
    svg: string;
};

/** @param {MetadataOperationType} op */
export function apiState(op: MetadataOperationType): {
    op: MetadataOperationType;
    client: any;
    apiState: typeof apiState;
    formLayout: any;
    createModel: (args: any) => any;
    apiLoading: boolean;
    apiResult: any;
    readonly api: any;
    createRequest: (args: any) => any;
    model: any;
    title: any;
    readonly error: any;
    readonly errorSummary: any;
    /** @param {string} id */
    fieldError(id: string): any;
    /** @param {string} propName
        @param {(args:{id:string,input:InputInfo,rowClass:string}) => void} [f] */
    field(propName: string, f?: (args: {
        id: string;
        input: InputInfo;
        rowClass: string;
    }) => void): any;
    /** @param {Record<string,any>} dtoArgs
        @param {Record<string,any>} [queryArgs]*/
    apiSend(dtoArgs: Record<string, any>, queryArgs?: Record<string, any>): any;
    /** @param {FormData} formData
        @param {Record<string,any>} [queryArgs]*/
    apiForm(formData: FormData, queryArgs?: Record<string, any>): any;
};
/** @typedef {ReturnType<apiState>} ApiState */
/** @typedef {{
    opPatch: MetadataOperationType,
    apiPatch: ApiState,
    apiUpdate: ApiState,
    opQuery: MetadataOperationType,
    apiQuery: ApiState,
    opCreate: MetadataOperationType,
    opUpdate: MetadataOperationType,
    opDelete: MetadataOperationType,
    apiCreate: ApiState,
    apiDelete: ApiState
}} State
 */
/**
 * @param {string} opName
 * @return {State}
 */
export function createState(opName: string): State;
/** @type {function(string, boolean?): boolean} */
export let transition: (arg0: string, arg1: boolean | null) => boolean;
/** @type {Breakpoints & {previous: Breakpoints, current: Breakpoints, snap: (function(): void)}} */
export let breakpoints: Breakpoints & {
    previous: Breakpoints;
    current: Breakpoints;
    snap: (() => void);
};
/** @typedef {{op?:string,tab?:string,provider?:string,preview?:string,body?:string,doc?:string,skip?:string,new?:string,edit?:string}} LocodeRoutes */
/** @typedef {{onEditChange(any): void, update(): void, uiHref(any): string}} LocodeRoutesExtend */
/** @type {LocodeRoutes & LocodeRoutesExtend & {page: string, set: (function(any): void), state: any, to: (function(any): void), href: (function(any): string)}} */
export let routes: LocodeRoutes & LocodeRoutesExtend & {
    page: string;
    set: ((arg0: any) => void);
    state: any;
    to: ((arg0: any) => void);
    href: ((arg0: any) => string);
};
/** @type {{
    op: (op:string) => any,
    lookup: (op:string) => any,
    saveOp: (op:string, fn:Function) => void,
    hasPrefs: (op:string) => boolean,
    saveOpProp: (op:string, name:string, fn:Function)=> void,
    saveLookup: (op:string, fn:Function) => void,
    events: {
        op: (op:string) => string,
        lookup: (op:string) => string,
        opProp: (op:string, name:string) => string
    },
    opProp: (op:string, name:string) => any,
    clearPrefs: (op:string) => void
 }} */
export let settings: {
    op: (op: string) => any;
    lookup: (op: string) => any;
    saveOp: (op: string, fn: Function) => void;
    hasPrefs: (op: string) => boolean;
    saveOpProp: (op: string, name: string, fn: Function) => void;
    saveLookup: (op: string, fn: Function) => void;
    events: {
        op: (op: string) => string;
        lookup: (op: string) => string;
        opProp: (op: string, name: string) => string;
    };
    opProp: (op: string, name: string) => any;
    clearPrefs: (op: string) => void;
};
/** @type {{
    cachedFetch: (url:string) => Promise<string>,
    copied: boolean,
    sideNav: {expanded: boolean, operations: MetadataOperationType[], tag: string}[],
    auth: AuthenticateResponse,
    readonly displayName: string|null,
    login: (args:any, $on?:Function) => void,
    detailSrcResult: any,
    logout: () => void,
    readonly isServiceStackType: boolean,
    readonly opViewModel: string,
    api: ApiResult<AuthenticateResponse>,
    modalLookup: any|null,
    init: () => void,
    readonly op: MetadataOperationType,
    debug: boolean,
    readonly filteredSideNav: {tag: string, operations: MetadataOperationType[], expanded: boolean}[],
    readonly authProfileUrl: string|null,
    previewResult: string|null,
    readonly opDesc: string,
    toggle: (tag:string) => void,
    readonly opDataModel: string,
    readonly authRoles: string[],
    filter: string,
    baseUrl: string,
    readonly authLinks: LinkInfo[],
    readonly opName: string,
    SignIn: (opt:any) => Function,
    hasRole: (role:string) => boolean,
    readonly authPermissions: string[],
    readonly useLang: string,
    invalidAccess: () => string|null
}} */
export let store: {
    cachedFetch: (url: string) => Promise<string>;
    copied: boolean;
    sideNav: {
        expanded: boolean;
        operations: MetadataOperationType[];
        tag: string;
    }[];
    auth: AuthenticateResponse;
    readonly displayName: string | null;
    login: (args: any, $on?: Function) => void;
    detailSrcResult: any;
    logout: () => void;
    readonly isServiceStackType: boolean;
    readonly opViewModel: string;
    api: ApiResult<AuthenticateResponse>;
    modalLookup: any | null;
    init: () => void;
    readonly op: MetadataOperationType;
    debug: boolean;
    readonly filteredSideNav: {
        tag: string;
        operations: MetadataOperationType[];
        expanded: boolean;
    }[];
    readonly authProfileUrl: string | null;
    previewResult: string | null;
    readonly opDesc: string;
    toggle: (tag: string) => void;
    readonly opDataModel: string;
    readonly authRoles: string[];
    filter: string;
    baseUrl: string;
    readonly authLinks: LinkInfo[];
    readonly opName: string;
    SignIn: (opt: any) => Function;
    hasRole: (role: string) => boolean;
    readonly authPermissions: string[];
    readonly useLang: string;
    invalidAccess: () => string | null;
};
export type ApiState = ReturnType<typeof apiState>;
export type State = {
    opPatch: MetadataOperationType;
    apiPatch: ApiState;
    apiUpdate: ApiState;
    opQuery: MetadataOperationType;
    apiQuery: ApiState;
    opCreate: MetadataOperationType;
    opUpdate: MetadataOperationType;
    opDelete: MetadataOperationType;
    apiCreate: ApiState;
    apiDelete: ApiState;
};
export type LocodeRoutes = {
    op?: string;
    tab?: string;
    provider?: string;
    preview?: string;
    body?: string;
    doc?: string;
    skip?: string;
    new?: string;
    edit?: string;
};
export type LocodeRoutesExtend = {
    onEditChange(any: any): void;
    update(): void;
    uiHref(any: any): string;
};

export declare var App:App
export declare var Forms:Forms
export interface CreateComponentArgs {
    store: typeof store;
    routes: typeof routes;
    settings: typeof settings;
    state: () => State;
    save: () => void;
    done: () => void;
}
export declare type CreateComponent = (args:CreateComponentArgs) => Record<string,any>;

export interface EditComponentArgs {
    store: typeof store;
    routes: typeof routes;
    settings: typeof settings;
    state: () => State;
    save: () => void;
    done: () => void;
}
export declare type EditComponent = (args:EditComponentArgs) => Record<string,any>;
