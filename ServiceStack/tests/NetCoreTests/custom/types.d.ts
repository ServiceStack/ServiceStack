import { AppMetadata, MetadataOperationType, MetadataPropertyType, MetadataType } from "../dist/shared"
import { AuthenticateResponse, AdminUsersInfo, InputInfo, LinkInfo, ThemeInfo } from "../dist/shared"
import { ApiResult, ResponseStatus } from "../dist/client"

/**
 * Server Metadata containing App capabilities and APIs used to dynamically render the UI  
 */
export let Server:AppMetadata;

/** Tailwind Responsive Breakpoints
 * { 2xl:1536, xl:1280, lg:1024, md:768, sm:640 } */
export type Breakpoints = Record<'2xl' | 'xl' | 'lg' | 'md' | 'sm', boolean>;

/** Return self or reactive proxy of self */
export type Identity = <T>(args: T) => T;

/** Invoke a Tailwind Definition Rule */
export type Transition = (prop:string,enter:boolean) => boolean;

/** Publish/Subscribe to App Events */
export type Bus = {
    subscribe: (type: string, callback: Function) => {
        unsubscribe: () => void;
    };
    publish: (eventType: string, arg: any) => void;
};

/** High-level API encapsulating client PetiteVue App */
export type App = {
    /** Publish/Subscript to App events */
    events: Bus;
    /** PetiteVue App instance */
    readonly petite: any;
    /** Register map of PetiteVue components using key as Components Name */
    components: (components: Record<string,Function|Object>) => void;
    /** Register single component 
     * @param {string} name
     * @param {string|Function} component Auto Component template HTML or Component Function */
    component: (name: string, component: string|Function) => void;
    /** Register Auto Component with $template contents
     * @param {string} name
     * @param {string} $template */
    template: (name: string, $template: string) => void;
    /** Register map of Auto Components using key as Components Name */
    templates: (templates: Record<string,string>) => void;
    /** Register PetiteVue directive */
    directive: (name: string, fn: Function) => void;
    /** Register (non-reactive) global App state property */
    prop: (name: string, val: any) => void;
    /** Register multiple (non-reactive) global App state props */
    props: (props:Record<string,any>) => void;
    /** Build PetiteVue App instance */
    build: (args: Record<string,any>) => any;
    /** Dynamically load external script src */
    import: (arg0: string) => Promise<any>;
    /** Register callback to invoke after App has started */
    onStart: (f: Function) => void;
    /** Start App instance */
    start: () => void;
    /** App function for unsubscribing 'sub' subscription in Component instance */
    unsubscribe: () => void;
    /** PetiteVue.createApp - create PetiteVue instance */
    createApp: (args: any) => any;
    /** PetiteVue.nextTick - register callback to be fired afterA next async loop */
    nextTick: (f: Function) => void;
    /** PetiteVue.reactive - create a reactive store */
    reactive: Identity;
};

/** Utility class for managing Forms UI and behavior */
export type Forms = {
    /** Server Metadata */
    Server: AppMetadata;
    /** Client Metadata APIs */
    Meta: Meta,
    getId: (type: MetadataType, row: any) => any;
    getType: (typeRef: string | {
        namespace: string;
        name: string;
    }) => MetadataType;
    inputId: (input: {id:string}) => string;
    colClass: (fields: number) => string;
    inputProp: (prop: MetadataPropertyType) => {
        id: string;
        type: string;
        'data-type': string;
    };
    getPrimaryKey: (type: MetadataType) => MetadataPropertyType|null;
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
    fetchLookupValues: (results: any[], props: MetadataPropertyType[], refreshFn: () => void) => void;
    theme: ThemeInfo;
    formClass: string;
    gridClass: string;
    opTitle(op: MetadataOperationType): string;
    forAutoForm(type: MetadataType): (field: any) => void;
    forCreate(type: MetadataType): (field: any) => void;
    forEdit(type: MetadataType): (field: any) => void;
    getFormProp(id: any, type: any): MetadataPropertyType;
    getGridInputs(formLayout: InputInfo[], f?: (args: {
        id: string;
        input: InputInfo;
        rowClass: string;
    }) => void): {
        id: string;
        input: InputInfo;
        rowClass: string;
    }[];
    getGridInput(input: InputInfo, f?: (args: {
        id: string;
        input: InputInfo;
        rowClass: string;
    }) => void): {
        id: string;
        input: InputInfo;
        rowClass: string;
    };
    getFieldError(error: any, id: any): string|null;
    kvpValues(input: any): {key,value}[];
    useLabel(input: any): string|null;
    usePlaceholder(input: any): string|null;
    isRequired(input: any): boolean;
    resolveFormLayout(op: MetadataOperationType): InputInfo[];
    formValues(form: any): Record<string,any>;
    formData(form: any, op: MetadataOperationType): FormData;
    groupTypes(allTypes: any): any[];
    complexProp(prop: any): boolean;
    supportsProp(prop: any): boolean;
    populateModel(model: any, formLayout: any): Record<string,any>;
    apiValue(o: any): any;
    format(o: any, prop: MetadataPropertyType): any;
}

/** Generic functionality around AppMetadata */
export type Meta = {
    /** Global Cache */
    CACHE: {};
    /** HTTP Errors specially handled by Locode */
    HttpErrors: Record<number, string>;
    /** Server Metadata */
    Server: AppMetadata;
    /** Map of Request DTO names to `MetadataOperationType` */
    OpsMap: Record<string, MetadataOperationType>;
    /** Map of DTO names to `MetadataType` */
    TypesMap: Record<string, MetadataType>;
    /** Map of DTO namespace + names to `MetadataType` */
    FullTypesMap: Record<string, MetadataType>;
    /** Get list of Request DTOs */
    operations: MetadataOperationType[];
    /** Get list of unique API tags */
    tags: string[];
    /** Find `MetadataOperationType` by API name */
    getOp: (opName: string) => MetadataOperationType;
    /** Find `MetadataType` by DTO name */
    getType: (typeRef: ({ namespace?: string; name: string; } | string)) => null | MetadataType;
    /** Check whether a Type is an Enum */
    isEnum: (type: string) => boolean;
    /** Get Enum Values of an Enum Type */
    enumValues: (type: string) => { key: string; value: string; }[];
    /** Get API Icon */
    getIcon: (args: ({ op?: MetadataOperationType; type?: MetadataType; })) => { svg: string; };
};

/** Reactive store to manage page navigation state and sync with history.pushState */
export type Routes = {
    /** The arg name that's used to identify the page name */
    page: string;
    /** Populate Route state */
    set: (args: Record<string, any>) => void;
    /** Snapshot of the current route state */
    state: Record<string, any>;
    /** Navigate to new route state */
    to: (args: Record<string, any>) => void;
    /** Return URL of current route state */
    href: (args: Record<string, any>) => string;
}

/** APIs for resolving SVG icons and data URIs for different File Types */
interface Files {
    /** Get Icon SVG for .ext */
    extSvg(ext: string): string | null;
    /** Get Icon src for .ext */
    extSrc(ext: string): string | null;
    /** Return file extension (without '.; prefix) of path or URI */
    getExt(path: string): string | null;
    /** Encode SVG for embedding in Data URI */
    encodeSvg(s: string): string;
    /** Return whether path is a URI to a previewable image */
    canPreview(path: string): boolean;
    /** Return file name part of URI or file path */
    getFileName(path: string): string | null;
    /** Format bytes into human-readable file size */
    formatBytes(bytes: number, d?: number): string;
    /** Get the Icon src for a file path or URI, previewable resources will return self, otherwise returns SVG URI of .ext */
    filePathUri(path: string): string | null;
    /** Convert SVG to Data URI */
    svgToDataUri(svg: string): string;
    /** Return Image URI of INPUT file attachments */
    fileImageUri(file: File | MediaSource): string;
    /** Clear all remaining Image URIs of INPUT file attachments */
    flush(): void;
}
export let Files:Files

/** APIs to inspect .NET Types */
interface Types {
    /** Return well-known C# alias for its .NET Type name */
    alias(type: string): any;
    /** Return underlying Type if nullable */
    unwrap(type: string): string;
    /** Resolve well-known C# Type Name from Type Ref */
    typeName(metaType:{name:string,genericArgs:string[]}): string;
    /** Resolve well-known C# Type Name from Name and Generic Args */
    typeName2(name: string, genericArgs: string[]): string;
    /** Return true if .NET Type is numeric */
    isNumber(type: string): boolean;
    /** Return true if .NET Type is a string */
    isString(type: string): boolean;
    /** Return true if .NET Type is a collection */
    isArray(type: string): boolean;
    /** Return true if typeof is a scalar value (string|number|symbol|boolean) */
    isPrimitive(type: string): boolean;
    /** Return value suitable for human display */
    formatValue(type: string, value: any): any;
    /** Create a unique key string from a Type Ref */
    key(typeRef:{namespace:string,name:string}): string | null;
    /** Return true if both Type Refs are equivalent */
    equals(a:{namespace:string,name:string},b:{namespace:string,name:string}): boolean;
    /** Return true if Property has named Attribute */
    propHasAttr(p: MetadataPropertyType, attr: string): boolean;
    /** Return named property on Type (case-insensitive) */
    getProp(type: MetadataType, name: string): MetadataPropertyType;
    /** Return all properties of a Type, inc. base class properties  */
    typeProperties(TypesMap, type): MetadataPropertyType[];
}
export let Types:Types


///=== EXPLORER ===///

/** Custom route params used in API Explorer */
export type ExplorerRoutes = {
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
/** Route methods used in API Explorer */
export type ExplorerRoutesExtend = {
    queryHref(): string;
};

/** App's primary reactive store maintaining global functionality for Admin UI */
export type ExplorerStore = {
    cachedFetch: (url: string) => Promise<string>;
    copied: boolean;
    readonly opTabs: { [p: string]: string; };
    sideNav: { expanded: boolean; operations: MetadataOperationType[]; tag: string; }[];
    auth: AuthenticateResponse;
    readonly displayName: string | null;
    loadLang: () => void;
    langCache: () => { op: string; lang: string; url: string; };
    login: (args: any, $on?: Function) => void;
    detailSrcResult: {};
    logout: () => void;
    readonly isServiceStackType: boolean;
    api: ApiResult<AuthenticateResponse>;
    init: () => void;
    readonly op: MetadataOperationType | null;
    debug: boolean;
    readonly filteredSideNav: { tag: string; operations: MetadataOperationType[]; expanded: boolean; }[];
    readonly authProfileUrl: string | null;
    previewResult: string | null;
    readonly activeLangSrc: string | null;
    readonly previewCache: { preview: string; url: string; lang: string; } | null;
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

/** Method arguments of custom Doc Components */
export interface DocComponentArgs {
    store: ExplorerStore;
    routes: ExplorerRoutes & Routes;
    breakpoints: Breakpoints;
    op: () => MetadataOperationType;
}

/** Method Signature of custom Doc Components */
export declare type DocComponent = (args:DocComponentArgs) => Record<string,any>;

///=== LOCODE ===///

/** Custom route params used in Locode */
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
/** Route methods used in Locode */
export type LocodeRoutesExtend = {
    onEditChange(any: any): void;
    update(): void;
    uiHref(any: any): string;
};

/* App's primary reactive store maintaining global functionality for Locode Apps */
export type LocodeStore = {
    cachedFetch: (url: string) => Promise<string>;
    copied: boolean;
    sideNav: { expanded: boolean; operations: MetadataOperationType[]; tag: string; }[];
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
    readonly filteredSideNav: { tag: string; operations: MetadataOperationType[]; expanded: boolean; }[];
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

/** Manage users query & filter preferences in the Users browsers localStorage */
export type LocodeSettings = {
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

/** Create a new state for an API that encapsulates its invocation and execution */
export type ApiState = {
    op: MetadataOperationType;
    client: any;
    apiState: ApiState;
    formLayout: any;
    createModel: (args: any) => any;
    apiLoading: boolean;
    apiResult: any;
    readonly api: any;
    createRequest: (args: any) => any;
    model: any;
    title: string | null;
    readonly error: ResponseStatus;
    readonly errorSummary: string|null;
    fieldError(id: string): string|null;
    field(propName: string, f?: (args: { id: string; input: InputInfo; rowClass: string; }) => void): any;
    apiSend(dtoArgs: Record<string, any>, queryArgs?: Record<string, any>): any;
    apiForm(formData: FormData, queryArgs?: Record<string, any>): any;
};

/** All CRUD API States available for this operation */
export type CrudApisState = {
    opQuery:  MetadataOperationType | null;
    opCreate: MetadataOperationType | null;
    opPatch:  MetadataOperationType | null;
    opUpdate: MetadataOperationType | null;
    opDelete: MetadataOperationType | null;
    apiQuery:  ApiState | null;
    apiCreate: ApiState | null;
    apiPatch:  ApiState | null;
    apiUpdate: ApiState | null;
    apiDelete: ApiState | null;
};

export type CrudApisStateProp = CrudApisState & {
    prop: MetadataPropertyType;
    opName: string;
    dataModel: MetadataType;
    viewModel: MetadataType; 
    viewModelColumns: MetadataPropertyType[];
    callback:Function;
    createPrefs: () => any;
    selectedColumns: (prefs:any) => MetadataPropertyType[];
}

/** Method arguments of custom Create Form Components */
export interface CreateComponentArgs {
    store: LocodeStore;
    routes: LocodeRoutes & LocodeRoutesExtend & Routes;
    settings: LocodeSettings;
    state: () => CrudApisState;
    save: () => void;
    done: () => void;
}
/** Method Signature of custom Create Form Components */
export declare type CreateComponent = (args:CreateComponentArgs) => Record<string,any>;

/** Method arguments of custom Edit Form Components */
export interface EditComponentArgs {
    store: LocodeStore;
    routes: LocodeRoutes & LocodeRoutesExtend & Routes;
    settings: LocodeSettings;
    state: () => CrudApisState;
    save: () => void;
    done: () => void;
}
/** Method signature of custom Edit Form Components */
export declare type EditComponent = (args:EditComponentArgs) => Record<string,any>;

///=== ADMIN ===///

/** Route methods used in Admin UI */
export type AdminRoutes = {
    tab?: string;
    provider?: string;
    q?: string;
    page?: string;
    sort?: string;
    new?: string;
    edit?: string;
};

/** App's primary reactive store maintaining global functionality for Admin UI */
export type AdminStore = {
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
