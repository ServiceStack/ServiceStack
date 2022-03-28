import { ApiResult, JsonServiceClient } from './client'
import { App, MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from './shared'

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
export let HttpErrors: {
    401: string;
    403: string;
};
export let OpsMap: {};
export let TypesMap: {};
export let FullTypesMap: {};
export let getOp: (opName: string) => any;
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
}) => any;
export let Forms: {
    getId: (type: MetadataType, row: any) => any;
    getType: (typeRef: string | {
        namespace: string;
        name: string;
    }) => MetadataType;
    inputId: (input: any) => any;
    colClass: (fields: any) => string;
    inputProp: (prop: any) => {
        id: any;
        type: any;
        'data-type': any;
    };
    getPrimaryKey: (type: MetadataType) => any;
    typeProperties: (type: MetadataType) => MetadataPropertyType[];
    relativeTime: (val: string | number | Date, rtf?: Intl.RelativeTimeFormat) => string;
    relativeTimeFromMs: (elapsedMs: number, rtf?: Intl.RelativeTimeFormat) => string;
    relativeTimeFromDate: (d: Date, from?: Date) => string;
    Lookup: {};
    lookupLabel: (model: any, id: any, label: string) => any;
    refInfo: (row: any, prop: MetadataPropertyType, props: MetadataPropertyType[]) => {
        href: {
            op: string;
            skip: any;
            edit: any;
            new: any;
            $qs: {
                [x: string]: any;
            };
        };
        icon: any;
        html: any;
    };
    fetchLookupValues: (results: any[], props: MetadataPropertyType[], refreshFn: Function) => void;
    theme: ThemeInfo;
    formClass: string;
    gridClass: string;
    opTitle(op: MetadataOperationType): any;
    forAutoForm(type: MetadataType): (field: any) => void;
    forCreate(type: MetadataType): (field: any) => void;
    forEdit(type: MetadataType): (field: any) => void;
    getFormProp(id: any, type: any): MetadataPropertyType;
    getGridInputs(formLayout: InputInfo[], f?: ({ id, input, rowClass }: {
        id: any;
        input: any;
        rowClass: any;
    }) => void): {
        id: any;
        input: InputInfo;
        rowClass: string;
    }[];
    getGridInput(input: InputInfo, f?: ({ id, input, rowClass }: {
        id: any;
        input: any;
        rowClass: any;
    }) => void): {
        id: any;
        input: InputInfo;
        rowClass: string;
    };
    getFieldError(error: any, id: any): any;
    kvpValues(input: any): any;
    useLabel(input: any): any;
    usePlaceholder(input: any): any;
    isRequired(input: any): any;
    resolveFormLayout(op: MetadataOperationType): InputInfo[];
    formValues(form: any): {};
    formData(form: Element, op: MetadataOperationType): any;
    groupTypes(allTypes: any): any[];
    complexProp(prop: any): boolean;
    supportsProp(prop: any): boolean;
    populateModel(model: any, formLayout: any): any;
    apiValue(o: any): any;
    format(o: any, prop: MetadataPropertyType): any;
};

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
