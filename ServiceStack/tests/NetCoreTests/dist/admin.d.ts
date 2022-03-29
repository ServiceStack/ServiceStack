import { ApiResult, JsonServiceClient } from './client'
import { App, Forms, MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from './shared'

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
/**: SignIn:provider */
/** @typedef {{tab?:string,provider?:string,q?:string,page?:string,sort?:string,new?:string,edit?:string}} AdminRoutes */
/** @type {AdminRoutes & {page: string, set: (function(any): void), state: any, to: (function(any): void), href: (function(any): string)}} */
export let routes: AdminRoutes & {
    page: string;
    set: ((arg0: any) => void);
    state: any;
    to: ((arg0: any) => void);
    href: ((arg0: any) => string);
};
/**
 * @type {{
    adminLink(string): LinkInfo,
    init(): void,
    cachedFetch(string): Promise<unknown>,
    debug: boolean,
    copied: boolean,
    auth: AuthenticateResponse|null,
    readonly authProfileUrl: string|null,
    readonly displayName: null,
    readonly link: LinkInfo,
    readonly isAdmin: boolean,
    login(any): void,
    readonly adminUsers: AdminUsersInfo,
    readonly authRoles: string[],
    filter: string,
    baseUrl: string,
    logout(): void,
    readonly authLinks: LinkInfo[],
    SignIn(): Function,
    readonly adminLinks: LinkInfo[],
    api: ApiResult<AuthenticateResponse>|null,
    readonly authPermissions: *
    }}
 */
export let store: {
    adminLink(string: any): LinkInfo;
    init(): void;
    cachedFetch(string: any): Promise<unknown>;
    debug: boolean;
    copied: boolean;
    auth: AuthenticateResponse | null;
    readonly authProfileUrl: string | null;
    readonly displayName: null;
    readonly link: LinkInfo;
    readonly isAdmin: boolean;
    login(any: any): void;
    readonly adminUsers: AdminUsersInfo;
    readonly authRoles: string[];
    filter: string;
    baseUrl: string;
    logout(): void;
    readonly authLinks: LinkInfo[];
    SignIn(): Function;
    readonly adminLinks: LinkInfo[];
    api: ApiResult<AuthenticateResponse> | null;
    readonly authPermissions: any;
};
export type AdminRoutes = {
    tab?: string;
    provider?: string;
    q?: string;
    page?: string;
    sort?: string;
    new?: string;
    edit?: string;
};

export declare var App:App
export declare var Forms:Forms
