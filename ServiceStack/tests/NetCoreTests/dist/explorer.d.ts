import { ApiResult, JsonServiceClient } from './client'
import { App, Forms, MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from './shared'

export function createClient(fn: any): any;
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

/** @type {function(string, boolean?): boolean} */
export let transition: (arg0: string, arg1: boolean | null) => boolean;
/** @type {Breakpoints & {previous: Breakpoints, current: Breakpoints, snap: (function(): void)}} */
export let breakpoints: Breakpoints & {
    previous: Breakpoints;
    current: Breakpoints;
    snap: (() => void);
};
/** @typedef {{op?:string,tab?:string,lang?:string,provider?:string,preview?:string,body?:string,doc?:string,detailSrc?:string,form?:string,response?:string}} UiRoutes */
/** @typedef {{queryHref(): string}} UiRoutesExtend */
/** @type {UiRoutes & UiRoutesExtend & {page: string, set: (function(any): void), state: any, to: (function(any): void), href: (function(any): string)}} */
export let routes: UiRoutes & UiRoutesExtend & {
    page: string;
    set: ((arg0: any) => void);
    state: any;
    to: ((arg0: any) => void);
    href: ((arg0: any) => string);
};
/** @type {{
    cachedFetch: (url:string) => Promise<string>,
    copied: boolean,
    readonly opTabs: {[p: string]: string},
    sideNav: {expanded: boolean, operations: MetadataOperationType[], tag: string}[],
    auth: AuthenticateResponse,
    readonly displayName: string|null,
    loadLang: () => void,
    langCache: () => {op: string, lang: string, url: string},
    login: (args:any, $on?:Function) => void,
    detailSrcResult: {},
    logout: () => void,
    readonly isServiceStackType: boolean,
    api: ApiResult<AuthenticateResponse>,
    init: () => void,
    readonly op: MetadataOperationType|null,
    debug: boolean,
    readonly filteredSideNav: {tag: string, operations: MetadataOperationType[], expanded: boolean}[],
    readonly authProfileUrl: string|null,
    previewResult: string|null,
    readonly activeLangSrc: string|null,
    readonly previewCache: {preview: string, url: string, lang: string}|null,
    toggle: (tag:string) => void,
    getTypeUrl: (types: string) => string,
    readonly authRoles: string[],
    filter: string,
    loadDetailSrc: () => void,
    baseUrl: string,
    readonly activeDetailSrc: string,
    readonly authLinks: LinkInfo[],
    readonly opName: string,
    readonly previewSrc: string,
    SignIn: (opt:any) => Function,
    hasRole: (role:string) => boolean,
    loadPreview: () => void,
    readonly authPermissions: string[],
    readonly useLang: string,
    invalidAccess: () => string|null
}}
 */
export let store: {
    cachedFetch: (url: string) => Promise<string>;
    copied: boolean;
    readonly opTabs: {
        [p: string]: string;
    };
    sideNav: {
        expanded: boolean;
        operations: MetadataOperationType[];
        tag: string;
    }[];
    auth: AuthenticateResponse;
    readonly displayName: string | null;
    loadLang: () => void;
    langCache: () => {
        op: string;
        lang: string;
        url: string;
    };
    login: (args: any, $on?: Function) => void;
    detailSrcResult: {};
    logout: () => void;
    readonly isServiceStackType: boolean;
    api: ApiResult<AuthenticateResponse>;
    init: () => void;
    readonly op: MetadataOperationType | null;
    debug: boolean;
    readonly filteredSideNav: {
        tag: string;
        operations: MetadataOperationType[];
        expanded: boolean;
    }[];
    readonly authProfileUrl: string | null;
    previewResult: string | null;
    readonly activeLangSrc: string | null;
    readonly previewCache: {
        preview: string;
        url: string;
        lang: string;
    } | null;
    toggle: (tag: string) => void;
    getTypeUrl: (types: string) => string;
    readonly authRoles: string[];
    filter: string;
    loadDetailSrc: () => void;
    baseUrl: string;
    readonly activeDetailSrc: string;
    readonly authLinks: LinkInfo[];
    readonly opName: string;
    readonly previewSrc: string;
    SignIn: (opt: any) => Function;
    hasRole: (role: string) => boolean;
    loadPreview: () => void;
    readonly authPermissions: string[];
    readonly useLang: string;
    invalidAccess: () => string | null;
};
export type UiRoutes = {
    op?: string;
    tab?: string;
    lang?: string;
    provider?: string;
    preview?: string;
    body?: string;
    doc?: string;
    detailSrc?: string;
    form?: string;
    response?: string;
};
export type UiRoutesExtend = {
    queryHref(): string;
};

export declare var App:App
export declare var Forms:Forms
export interface DocComponentArgs {
    store: typeof store;
    routes: typeof routes;
    breakpoints: typeof breakpoints;
    op: () => MetadataOperationType;
}
export declare type DocComponent = (args:DocComponentArgs) => Record<string,any>;
