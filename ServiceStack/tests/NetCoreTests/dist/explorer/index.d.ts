import { ApiResult, JsonServiceClient } from '../client'
import { MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from '../shared'

export function createClient(fn: any): any;
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

