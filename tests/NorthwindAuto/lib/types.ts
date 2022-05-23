/* Options:
Date: 2022-01-13 07:18:45
Version: 5.133
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//GlobalNamespace: 
//MakePropertiesOptional: False
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/


export interface IReturn<T>
{
    createResponse(): T;
}

export interface IReturnVoid
{
    createResponse(): void;
}

export interface IHasSessionId
{
    sessionId: string;
}

export interface IHasBearerToken
{
    bearerToken: string;
}

export interface IPost
{
}

export interface IGet
{
}

export interface IPut
{
}

export interface IDelete
{
}

// @DataContract
export class AdminUserBase
{
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public firstName: string;

    // @DataMember(Order=3)
    public lastName: string;

    // @DataMember(Order=4)
    public displayName: string;

    // @DataMember(Order=5)
    public email: string;

    // @DataMember(Order=6)
    public password: string;

    // @DataMember(Order=7)
    public profileUrl: string;

    // @DataMember(Order=8)
    public userAuthProperties: { [index: string]: string; };

    // @DataMember(Order=9)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AdminUserBase>) { (Object as any).assign(this, init); }
}

export class AppInfo
{
    public baseUrl: string;
    public serviceStackVersion: string;
    public serviceName: string;
    public serviceDescription: string;
    public serviceIconUrl: string;
    public brandUrl: string;
    public brandImageUrl: string;
    public textColor: string;
    public linkColor: string;
    public backgroundColor: string;
    public backgroundImageUrl: string;
    public iconUrl: string;
    public jsTextCase: string;
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AppInfo>) { (Object as any).assign(this, init); }
}

export class ImageInfo
{
    public svg: string;
    public uri: string;
    public alt: string;
    public cls: string;

    public constructor(init?: Partial<ImageInfo>) { (Object as any).assign(this, init); }
}

export class LinkInfo
{
    public id: string;
    public href: string;
    public label: string;
    public icon: ImageInfo;

    public constructor(init?: Partial<LinkInfo>) { (Object as any).assign(this, init); }
}

export class UiInfo
{
    public brandIcon: ImageInfo;
    public hideTags: string[];
    public alwaysHideTags: string[];
    public adminLinks: LinkInfo[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<UiInfo>) { (Object as any).assign(this, init); }
}

export class ConfigInfo
{
    public debugMode?: boolean;
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<ConfigInfo>) { (Object as any).assign(this, init); }
}

export class NavItem
{
    public label: string;
    public href: string;
    public exact?: boolean;
    public id: string;
    public className: string;
    public iconClass: string;
    public show: string;
    public hide: string;
    public children: NavItem[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<NavItem>) { (Object as any).assign(this, init); }
}

export class InputInfo
{
    public id: string;
    public name: string;
    public type: string;
    public value: string;
    public placeholder: string;
    public help: string;
    public label: string;
    public title: string;
    public size: string;
    public pattern: string;
    public readOnly?: boolean;
    public required?: boolean;
    public disabled?: boolean;
    public autocomplete: string;
    public autofocus: string;
    public min: string;
    public max: string;
    public step?: number;
    public minLength?: number;
    public maxLength?: number;
    public allowableValues: string[];
    public allowableEntries: KeyValuePair<String,String>[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<InputInfo>) { (Object as any).assign(this, init); }
}

export class MetaAuthProvider
{
    public name: string;
    public label: string;
    public type: string;
    public navItem: NavItem;
    public icon: ImageInfo;
    public formLayout: InputInfo[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<MetaAuthProvider>) { (Object as any).assign(this, init); }
}

export class AuthInfo
{
    public hasAuthSecret?: boolean;
    public hasAuthRepository?: boolean;
    public includesRoles?: boolean;
    public includesOAuthTokens?: boolean;
    public htmlRedirect: string;
    public authProviders: MetaAuthProvider[];
    public roleLinks: { [index: string]: LinkInfo[]; };
    public serviceRoutes: { [index: string]: string[]; };
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AuthInfo>) { (Object as any).assign(this, init); }
}

export class AutoQueryConvention
{
    public name: string;
    public value: string;
    public types: string;
    public valueType: string;

    public constructor(init?: Partial<AutoQueryConvention>) { (Object as any).assign(this, init); }
}

export class AutoQueryInfo
{
    public maxLimit?: number;
    public untypedQueries?: boolean;
    public rawSqlFilters?: boolean;
    public autoQueryViewer?: boolean;
    public async?: boolean;
    public orderByPrimaryKey?: boolean;
    public crudEvents?: boolean;
    public crudEventsServices?: boolean;
    public accessRole: string;
    public namedConnection: string;
    public viewerConventions: AutoQueryConvention[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AutoQueryInfo>) { (Object as any).assign(this, init); }
}

export class ScriptMethodType
{
    public name: string;
    public paramNames: string[];
    public paramTypes: string[];
    public returnType: string;

    public constructor(init?: Partial<ScriptMethodType>) { (Object as any).assign(this, init); }
}

export class ValidationInfo
{
    public hasValidationSource?: boolean;
    public hasValidationSourceAdmin?: boolean;
    public serviceRoutes: { [index: string]: string[]; };
    public typeValidators: ScriptMethodType[];
    public propertyValidators: ScriptMethodType[];
    public accessRole: string;
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<ValidationInfo>) { (Object as any).assign(this, init); }
}

export class SharpPagesInfo
{
    public apiPath: string;
    public scriptAdminRole: string;
    public metadataDebugAdminRole: string;
    public metadataDebug?: boolean;
    public spaFallback?: boolean;
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<SharpPagesInfo>) { (Object as any).assign(this, init); }
}

export class RequestLogsInfo
{
    public requiredRoles: string[];
    public requestLogger: string;
    public serviceRoutes: { [index: string]: string[]; };
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<RequestLogsInfo>) { (Object as any).assign(this, init); }
}

export class MetadataTypeName
{
    public name: string;
    public namespace: string;
    public genericArgs: string[];

    public constructor(init?: Partial<MetadataTypeName>) { (Object as any).assign(this, init); }
}

export class MetadataDataContract
{
    public name: string;
    public namespace: string;

    public constructor(init?: Partial<MetadataDataContract>) { (Object as any).assign(this, init); }
}

export class MetadataDataMember
{
    public name: string;
    public order?: number;
    public isRequired?: boolean;
    public emitDefaultValue?: boolean;

    public constructor(init?: Partial<MetadataDataMember>) { (Object as any).assign(this, init); }
}

export class MetadataAttribute
{
    public name: string;
    public constructorArgs: MetadataPropertyType[];
    public args: MetadataPropertyType[];

    public constructor(init?: Partial<MetadataAttribute>) { (Object as any).assign(this, init); }
}

export class MetadataPropertyType
{
    public name: string;
    public type: string;
    public isValueType?: boolean;
    public isSystemType?: boolean;
    public isEnum?: boolean;
    public isPrimaryKey?: boolean;
    public typeNamespace: string;
    public genericArgs: string[];
    public value: string;
    public description: string;
    public dataMember: MetadataDataMember;
    public readOnly?: boolean;
    public paramType: string;
    public displayType: string;
    public isRequired?: boolean;
    public allowableValues: string[];
    public allowableMin?: number;
    public allowableMax?: number;
    public attributes: MetadataAttribute[];
    public input: InputInfo;

    public constructor(init?: Partial<MetadataPropertyType>) { (Object as any).assign(this, init); }
}

export class MetadataType
{
    public name: string;
    public namespace: string;
    public genericArgs: string[];
    public inherits: MetadataTypeName;
    public implements: MetadataTypeName[];
    public displayType: string;
    public description: string;
    public notes: string;
    public isNested?: boolean;
    public isEnum?: boolean;
    public isEnumInt?: boolean;
    public isInterface?: boolean;
    public isAbstract?: boolean;
    public dataContract: MetadataDataContract;
    public properties: MetadataPropertyType[];
    public attributes: MetadataAttribute[];
    public innerTypes: MetadataTypeName[];
    public enumNames: string[];
    public enumValues: string[];
    public enumMemberValues: string[];
    public enumDescriptions: string[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<MetadataType>) { (Object as any).assign(this, init); }
}

export class MediaRule
{
    public size: string;
    public rule: string;
    public applyTo: string[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<MediaRule>) { (Object as any).assign(this, init); }
}

export class AdminUsersInfo
{
    public accessRole: string;
    public enabled: string[];
    public userAuth: MetadataType;
    public allRoles: string[];
    public allPermissions: string[];
    public queryUserAuthProperties: string[];
    public queryMediaRules: MediaRule[];
    public userFormLayout: InputInfo[][];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AdminUsersInfo>) { (Object as any).assign(this, init); }
}

export class PluginInfo
{
    public loaded: string[];
    public auth: AuthInfo;
    public autoQuery: AutoQueryInfo;
    public validation: ValidationInfo;
    public sharpPages: SharpPagesInfo;
    public requestLogs: RequestLogsInfo;
    public adminUsers: AdminUsersInfo;
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<PluginInfo>) { (Object as any).assign(this, init); }
}

export class CustomPluginInfo
{
    public accessRole: string;
    public serviceRoutes: { [index: string]: string[]; };
    public enabled: string[];
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<CustomPluginInfo>) { (Object as any).assign(this, init); }
}

export class MetadataTypesConfig
{
    public baseUrl: string;
    public usePath: string;
    public makePartial: boolean;
    public makeVirtual: boolean;
    public makeInternal: boolean;
    public baseClass: string;
    public package: string;
    public addReturnMarker: boolean;
    public addDescriptionAsComments: boolean;
    public addDataContractAttributes: boolean;
    public addIndexesToDataMembers: boolean;
    public addGeneratedCodeAttributes: boolean;
    public addImplicitVersion?: number;
    public addResponseStatus: boolean;
    public addServiceStackTypes: boolean;
    public addModelExtensions: boolean;
    public addPropertyAccessors: boolean;
    public excludeGenericBaseTypes: boolean;
    public settersReturnThis: boolean;
    public makePropertiesOptional: boolean;
    public exportAsTypes: boolean;
    public excludeImplementedInterfaces: boolean;
    public addDefaultXmlNamespace: string;
    public makeDataContractsExtensible: boolean;
    public initializeCollections: boolean;
    public addNamespaces: string[];
    public defaultNamespaces: string[];
    public defaultImports: string[];
    public includeTypes: string[];
    public excludeTypes: string[];
    public treatTypesAsStrings: string[];
    public exportValueTypes: boolean;
    public globalNamespace: string;
    public excludeNamespace: boolean;
    public dataClass: string;
    public dataClassJson: string;
    public ignoreTypes: string[];
    public exportTypes: string[];
    public exportAttributes: string[];
    public ignoreTypesInNamespaces: string[];

    public constructor(init?: Partial<MetadataTypesConfig>) { (Object as any).assign(this, init); }
}

export class MetadataRoute
{
    public path: string;
    public verbs: string;
    public notes: string;
    public summary: string;

    public constructor(init?: Partial<MetadataRoute>) { (Object as any).assign(this, init); }
}

export class MetadataOperationType
{
    public request: MetadataType;
    public response: MetadataType;
    public actions: string[];
    public returnsVoid: boolean;
    public method: string;
    public returnType: MetadataTypeName;
    public routes: MetadataRoute[];
    public dataModel: MetadataTypeName;
    public viewModel: MetadataTypeName;
    public requiresAuth: boolean;
    public requiredRoles: string[];
    public requiresAnyRole: string[];
    public requiredPermissions: string[];
    public requiresAnyPermission: string[];
    public tags: string[];
    public formLayout: InputInfo[][];

    public constructor(init?: Partial<MetadataOperationType>) { (Object as any).assign(this, init); }
}

export class MetadataTypes
{
    public config: MetadataTypesConfig;
    public namespaces: string[];
    public types: MetadataType[];
    public operations: MetadataOperationType[];

    public constructor(init?: Partial<MetadataTypes>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ResponseError
{
    // @DataMember(Order=1)
    public errorCode: string;

    // @DataMember(Order=2)
    public fieldName: string;

    // @DataMember(Order=3)
    public message: string;

    // @DataMember(Order=4)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<ResponseError>) { (Object as any).assign(this, init); }
}

// @DataContract
export class ResponseStatus
{
    // @DataMember(Order=1)
    public errorCode: string;

    // @DataMember(Order=2)
    public message: string;

    // @DataMember(Order=3)
    public stackTrace: string;

    // @DataMember(Order=4)
    public errors: ResponseError[];

    // @DataMember(Order=5)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<ResponseStatus>) { (Object as any).assign(this, init); }
}

export class KeyValuePair<TKey, TValue>
{
    public key: TKey;
    public value: TValue;

    public constructor(init?: Partial<KeyValuePair<TKey, TValue>>) { (Object as any).assign(this, init); }
}

export class AppMetadata
{
    public app: AppInfo;
    public ui: UiInfo;
    public config: ConfigInfo;
    public contentTypeFormats: { [index: string]: string; };
    public httpHandlers: { [index: string]: string; };
    public plugins: PluginInfo;
    public customPlugins: { [index: string]: CustomPluginInfo; };
    public api: MetadataTypes;
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AppMetadata>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AuthenticateResponse implements IHasSessionId, IHasBearerToken
{
    // @DataMember(Order=1)
    public userId: string;

    // @DataMember(Order=2)
    public sessionId: string;

    // @DataMember(Order=3)
    public userName: string;

    // @DataMember(Order=4)
    public displayName: string;

    // @DataMember(Order=5)
    public referrerUrl: string;

    // @DataMember(Order=6)
    public bearerToken: string;

    // @DataMember(Order=7)
    public refreshToken: string;

    // @DataMember(Order=8)
    public profileUrl: string;

    // @DataMember(Order=9)
    public roles: string[];

    // @DataMember(Order=10)
    public permissions: string[];

    // @DataMember(Order=11)
    public responseStatus: ResponseStatus;

    // @DataMember(Order=12)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AuthenticateResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AssignRolesResponse
{
    // @DataMember(Order=1)
    public allRoles: string[];

    // @DataMember(Order=2)
    public allPermissions: string[];

    // @DataMember(Order=3)
    public meta: { [index: string]: string; };

    // @DataMember(Order=4)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AssignRolesResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class UnAssignRolesResponse
{
    // @DataMember(Order=1)
    public allRoles: string[];

    // @DataMember(Order=2)
    public allPermissions: string[];

    // @DataMember(Order=3)
    public meta: { [index: string]: string; };

    // @DataMember(Order=4)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<UnAssignRolesResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminUserResponse
{
    // @DataMember(Order=1)
    public id: string;

    // @DataMember(Order=2)
    public result: { [index: string]: Object; };

    // @DataMember(Order=3)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminUserResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminUsersResponse
{
    // @DataMember(Order=1)
    public results: { [index:string]: Object; }[];

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminUsersResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminDeleteUserResponse
{
    // @DataMember(Order=1)
    public id: string;

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminDeleteUserResponse>) { (Object as any).assign(this, init); }
}

// @Route("/metadata/app")
// @DataContract
export class MetadataApp implements IReturn<AppMetadata>
{

    public constructor(init?: Partial<MetadataApp>) { (Object as any).assign(this, init); }
    public createResponse() { return new AppMetadata(); }
    public getTypeName() { return 'MetadataApp'; }
    public getMethod() { return 'POST'; }
}

/**
* Sign In
*/
// @Route("/auth")
// @Route("/auth/{provider}")
// @Api(Description="Sign In")
// @DataContract
export class Authenticate implements IReturn<AuthenticateResponse>, IPost
{
    /**
    * AuthProvider, e.g. credentials
    */
    // @DataMember(Order=1)
    // @ApiMember(Description="AuthProvider, e.g. credentials")
    public provider: string;

    // @DataMember(Order=2)
    public state: string;

    // @DataMember(Order=3)
    public oauth_token: string;

    // @DataMember(Order=4)
    public oauth_verifier: string;

    // @DataMember(Order=5)
    public userName: string;

    // @DataMember(Order=6)
    public password: string;

    // @DataMember(Order=7)
    public rememberMe?: boolean;

    // @DataMember(Order=9)
    public errorView: string;

    // @DataMember(Order=10)
    public nonce: string;

    // @DataMember(Order=11)
    public uri: string;

    // @DataMember(Order=12)
    public response: string;

    // @DataMember(Order=13)
    public qop: string;

    // @DataMember(Order=14)
    public nc: string;

    // @DataMember(Order=15)
    public cnonce: string;

    // @DataMember(Order=17)
    public accessToken: string;

    // @DataMember(Order=18)
    public accessTokenSecret: string;

    // @DataMember(Order=19)
    public scope: string;

    // @DataMember(Order=20)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<Authenticate>) { (Object as any).assign(this, init); }
    public createResponse() { return new AuthenticateResponse(); }
    public getTypeName() { return 'Authenticate'; }
    public getMethod() { return 'POST'; }
}

// @Route("/assignroles")
// @DataContract
export class AssignRoles implements IReturn<AssignRolesResponse>, IPost
{
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public permissions: string[];

    // @DataMember(Order=3)
    public roles: string[];

    // @DataMember(Order=4)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<AssignRoles>) { (Object as any).assign(this, init); }
    public createResponse() { return new AssignRolesResponse(); }
    public getTypeName() { return 'AssignRoles'; }
    public getMethod() { return 'POST'; }
}

// @Route("/unassignroles")
// @DataContract
export class UnAssignRoles implements IReturn<UnAssignRolesResponse>, IPost
{
    // @DataMember(Order=1)
    public userName: string;

    // @DataMember(Order=2)
    public permissions: string[];

    // @DataMember(Order=3)
    public roles: string[];

    // @DataMember(Order=4)
    public meta: { [index: string]: string; };

    public constructor(init?: Partial<UnAssignRoles>) { (Object as any).assign(this, init); }
    public createResponse() { return new UnAssignRolesResponse(); }
    public getTypeName() { return 'UnAssignRoles'; }
    public getMethod() { return 'POST'; }
}

// @DataContract
export class AdminGetUser implements IReturn<AdminUserResponse>, IGet
{
    // @DataMember(Order=10)
    public id: string;

    public constructor(init?: Partial<AdminGetUser>) { (Object as any).assign(this, init); }
    public createResponse() { return new AdminUserResponse(); }
    public getTypeName() { return 'AdminGetUser'; }
    public getMethod() { return 'GET'; }
}

// @DataContract
export class AdminQueryUsers implements IReturn<AdminUsersResponse>, IGet
{
    // @DataMember(Order=1)
    public query: string;

    // @DataMember(Order=2)
    public orderBy: string;

    // @DataMember(Order=3)
    public skip?: number;

    // @DataMember(Order=4)
    public take?: number;

    public constructor(init?: Partial<AdminQueryUsers>) { (Object as any).assign(this, init); }
    public createResponse() { return new AdminUsersResponse(); }
    public getTypeName() { return 'AdminQueryUsers'; }
    public getMethod() { return 'GET'; }
}

// @DataContract
export class AdminCreateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPost
{
    // @DataMember(Order=10)
    public roles: string[];

    // @DataMember(Order=11)
    public permissions: string[];

    public constructor(init?: Partial<AdminCreateUser>) { super(init); (Object as any).assign(this, init); }
    public createResponse() { return new AdminUserResponse(); }
    public getTypeName() { return 'AdminCreateUser'; }
    public getMethod() { return 'POST'; }
}

// @DataContract
export class AdminUpdateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPut
{
    // @DataMember(Order=10)
    public id: string;

    // @DataMember(Order=11)
    public lockUser?: boolean;

    // @DataMember(Order=12)
    public unlockUser?: boolean;

    // @DataMember(Order=13)
    public addRoles: string[];

    // @DataMember(Order=14)
    public removeRoles: string[];

    // @DataMember(Order=15)
    public addPermissions: string[];

    // @DataMember(Order=16)
    public removePermissions: string[];

    public constructor(init?: Partial<AdminUpdateUser>) { super(init); (Object as any).assign(this, init); }
    public createResponse() { return new AdminUserResponse(); }
    public getTypeName() { return 'AdminUpdateUser'; }
    public getMethod() { return 'PUT'; }
}

// @DataContract
export class AdminDeleteUser implements IReturn<AdminDeleteUserResponse>, IDelete
{
    // @DataMember(Order=10)
    public id: string;

    public constructor(init?: Partial<AdminDeleteUser>) { (Object as any).assign(this, init); }
    public createResponse() { return new AdminDeleteUserResponse(); }
    public getTypeName() { return 'AdminDeleteUser'; }
    public getMethod() { return 'DELETE'; }
}


// declare Types used in /ui 
export declare var APP:AppMetadata
