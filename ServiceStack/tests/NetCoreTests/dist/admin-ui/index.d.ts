import { ApiResult, JsonServiceClient } from '../client'
import { MetadataOperationType, MetadataType, MetadataPropertyType, InputInfo, ThemeInfo, LinkInfo, Breakpoints, AuthenticateResponse, AdminUsersInfo } from '../shared'

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

