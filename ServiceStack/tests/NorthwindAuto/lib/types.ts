import { ApiResult } from './client';

/* Options:
Date: 2025-03-14 11:35:18
Version: 8.61
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
    sessionId?: string;
}

export interface IHasBearerToken
{
    bearerToken?: string;
}

export interface IGet
{
}

export interface IPost
{
}

export interface IDelete
{
}

export interface IPut
{
}

export interface IPatch
{
}

// @DataContract
export class Property
{
    // @DataMember(Order=1)
    public name: string;

    // @DataMember(Order=2)
    public value: string;

    public constructor(init?: Partial<Property>) { (Object as any).assign(this, init); }
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
    public meta: { [index:string]: string; };

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
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<ResponseStatus>) { (Object as any).assign(this, init); }
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
    public phoneNumber: string;

    // @DataMember(Order=9)
    public userAuthProperties: { [index:string]: string; };

    // @DataMember(Order=10)
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AdminUserBase>) { (Object as any).assign(this, init); }
}

// @DataContract
export class QueryBase
{
    // @DataMember(Order=1)
    public skip?: number;

    // @DataMember(Order=2)
    public take?: number;

    // @DataMember(Order=3)
    public orderBy: string;

    // @DataMember(Order=4)
    public orderByDesc: string;

    // @DataMember(Order=5)
    public include: string;

    // @DataMember(Order=6)
    public fields: string;

    // @DataMember(Order=7)
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<QueryBase>) { (Object as any).assign(this, init); }
}

export class QueryDb<T> extends QueryBase
{

    public constructor(init?: Partial<QueryDb<T>>) { super(init); (Object as any).assign(this, init); }
}

export class RequestLog
{
    public id: number;
    public traceId: string;
    public operationName: string;
    public dateTime: string;
    public statusCode: number;
    public statusDescription?: string;
    public httpMethod?: string;
    public absoluteUri?: string;
    public pathInfo?: string;
    public request?: string;
    // @StringLength(2147483647)
    public requestBody?: string;

    public userAuthId?: string;
    public sessionId?: string;
    public ipAddress?: string;
    public forwardedFor?: string;
    public referer?: string;
    public headers: { [index:string]: string; } = {};
    public formData?: { [index:string]: string; };
    public items: { [index:string]: string; } = {};
    public responseHeaders?: { [index:string]: string; };
    public response?: string;
    public responseBody?: string;
    public sessionBody?: string;
    public error?: ResponseStatus;
    public exceptionSource?: string;
    public exceptionDataBody?: string;
    public requestDuration: string;
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<RequestLog>) { (Object as any).assign(this, init); }
}

export class RedisEndpointInfo
{
    public host: string;
    public port: number;
    public ssl?: boolean;
    public db: number;
    public username: string;
    public password: string;

    public constructor(init?: Partial<RedisEndpointInfo>) { (Object as any).assign(this, init); }
}

export enum BackgroundJobState
{
    Queued = 'Queued',
    Started = 'Started',
    Executed = 'Executed',
    Completed = 'Completed',
    Failed = 'Failed',
    Cancelled = 'Cancelled',
}

export class BackgroundJobBase
{
    public id: number;
    public parentId?: number;
    public refId?: string;
    public worker?: string;
    public tag?: string;
    public batchId?: string;
    public callback?: string;
    public dependsOn?: number;
    public runAfter?: string;
    public createdDate: string;
    public createdBy?: string;
    public requestId?: string;
    public requestType: string;
    public command?: string;
    public request: string;
    public requestBody: string;
    public userId?: string;
    public response?: string;
    public responseBody?: string;
    public state: BackgroundJobState;
    public startedDate?: string;
    public completedDate?: string;
    public notifiedDate?: string;
    public retryLimit?: number;
    public attempts: number;
    public durationMs: number;
    public timeoutSecs?: number;
    public progress?: number;
    public status?: string;
    public logs?: string;
    public lastActivityDate?: string;
    public replyTo?: string;
    public errorCode?: string;
    public error?: ResponseStatus;
    public args?: { [index:string]: string; };
    public meta?: { [index:string]: string; };

    public constructor(init?: Partial<BackgroundJobBase>) { (Object as any).assign(this, init); }
}

export class BackgroundJob extends BackgroundJobBase
{
    public id: number;

    public constructor(init?: Partial<BackgroundJob>) { super(init); (Object as any).assign(this, init); }
}

export class JobSummary
{
    public id: number;
    public parentId?: number;
    public refId?: string;
    public worker?: string;
    public tag?: string;
    public batchId?: string;
    public createdDate: string;
    public createdBy?: string;
    public requestType: string;
    public command?: string;
    public request: string;
    public response?: string;
    public userId?: string;
    public callback?: string;
    public startedDate?: string;
    public completedDate?: string;
    public state: BackgroundJobState;
    public durationMs: number;
    public attempts: number;
    public errorCode?: string;
    public errorMessage?: string;

    public constructor(init?: Partial<JobSummary>) { (Object as any).assign(this, init); }
}

export class BackgroundJobOptions
{
    public refId?: string;
    public parentId?: number;
    public worker?: string;
    public runAfter?: string;
    public callback?: string;
    public dependsOn?: number;
    public userId?: string;
    public retryLimit?: number;
    public replyTo?: string;
    public tag?: string;
    public batchId?: string;
    public createdBy?: string;
    public timeoutSecs?: number;
    public timeout?: string;
    public args?: { [index:string]: string; };
    public runCommand?: boolean;

    public constructor(init?: Partial<BackgroundJobOptions>) { (Object as any).assign(this, init); }
}

export class ScheduledTask
{
    public id: number;
    public name: string;
    public interval?: string;
    public cronExpression?: string;
    public requestType: string;
    public command?: string;
    public request: string;
    public requestBody: string;
    public options?: BackgroundJobOptions;
    public lastRun?: string;
    public lastJobId?: number;

    public constructor(init?: Partial<ScheduledTask>) { (Object as any).assign(this, init); }
}

export class CompletedJob extends BackgroundJobBase
{

    public constructor(init?: Partial<CompletedJob>) { super(init); (Object as any).assign(this, init); }
}

export class FailedJob extends BackgroundJobBase
{

    public constructor(init?: Partial<FailedJob>) { super(init); (Object as any).assign(this, init); }
}

export class ValidateRule
{
    public validator: string;
    public condition: string;
    public errorCode: string;
    public message: string;

    public constructor(init?: Partial<ValidateRule>) { (Object as any).assign(this, init); }
}

export class ValidationRule extends ValidateRule
{
    public id: number;
    // @Required()
    public type: string;

    public field: string;
    public createdBy: string;
    public createdDate?: string;
    public modifiedBy: string;
    public modifiedDate?: string;
    public suspendedBy: string;
    public suspendedDate?: string;
    public notes: string;

    public constructor(init?: Partial<ValidationRule>) { super(init); (Object as any).assign(this, init); }
}

export class AppInfo
{
    public baseUrl: string;
    public serviceStackVersion: string;
    public serviceName: string;
    public apiVersion: string;
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
    public useSystemJson: string;
    public endpointRouting?: string[];
    public meta: { [index:string]: string; };

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
    public show: string;
    public hide: string;

    public constructor(init?: Partial<LinkInfo>) { (Object as any).assign(this, init); }
}

export class ThemeInfo
{
    public form: string;
    public modelIcon: ImageInfo;

    public constructor(init?: Partial<ThemeInfo>) { (Object as any).assign(this, init); }
}

export class ApiCss
{
    public form: string;
    public fieldset: string;
    public field: string;

    public constructor(init?: Partial<ApiCss>) { (Object as any).assign(this, init); }
}

export class AppTags
{
    public default: string;
    public other: string;

    public constructor(init?: Partial<AppTags>) { (Object as any).assign(this, init); }
}

export class LocodeUi
{
    public css: ApiCss;
    public tags: AppTags;
    public maxFieldLength: number;
    public maxNestedFields: number;
    public maxNestedFieldLength: number;

    public constructor(init?: Partial<LocodeUi>) { (Object as any).assign(this, init); }
}

export class ExplorerUi
{
    public css: ApiCss;
    public tags: AppTags;

    public constructor(init?: Partial<ExplorerUi>) { (Object as any).assign(this, init); }
}

export class AdminUi
{
    public css: ApiCss;

    public constructor(init?: Partial<AdminUi>) { (Object as any).assign(this, init); }
}

export class FormatInfo
{
    public method: string;
    public options: string;
    public locale: string;

    public constructor(init?: Partial<FormatInfo>) { (Object as any).assign(this, init); }
}

export class ApiFormat
{
    public locale: string;
    public assumeUtc: boolean;
    public number: FormatInfo;
    public date: FormatInfo;

    public constructor(init?: Partial<ApiFormat>) { (Object as any).assign(this, init); }
}

export class UiInfo
{
    public brandIcon: ImageInfo;
    public hideTags: string[];
    public modules: string[];
    public alwaysHideTags: string[];
    public adminLinks: LinkInfo[];
    public theme: ThemeInfo;
    public locode: LocodeUi;
    public explorer: ExplorerUi;
    public admin: AdminUi;
    public defaultFormats: ApiFormat;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<UiInfo>) { (Object as any).assign(this, init); }
}

export class ConfigInfo
{
    public debugMode?: boolean;
    public meta: { [index:string]: string; };

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
    public iconSrc: string;
    public show: string;
    public hide: string;
    public children: NavItem[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<NavItem>) { (Object as any).assign(this, init); }
}

export class FieldCss
{
    public field: string;
    public input: string;
    public label: string;

    public constructor(init?: Partial<FieldCss>) { (Object as any).assign(this, init); }
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
    public step: string;
    public minLength?: number;
    public maxLength?: number;
    public accept: string;
    public capture: string;
    public multiple?: boolean;
    public allowableValues: string[];
    public allowableEntries: KeyValuePair<string, string>[];
    public options: string;
    public ignore?: boolean;
    public css: FieldCss;
    public meta: { [index:string]: string; };

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
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<MetaAuthProvider>) { (Object as any).assign(this, init); }
}

export class IdentityAuthInfo
{
    public hasRefreshToken?: boolean;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<IdentityAuthInfo>) { (Object as any).assign(this, init); }
}

export class AuthInfo
{
    public hasAuthSecret?: boolean;
    public hasAuthRepository?: boolean;
    public includesRoles?: boolean;
    public includesOAuthTokens?: boolean;
    public htmlRedirect: string;
    public authProviders: MetaAuthProvider[];
    public identityAuth: IdentityAuthInfo;
    public roleLinks: { [index:string]: LinkInfo[]; };
    public serviceRoutes: { [index:string]: string[]; };
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AuthInfo>) { (Object as any).assign(this, init); }
}

export class ApiKeyInfo
{
    public label: string;
    public httpHeader: string;
    public scopes: string[];
    public features: string[];
    public requestTypes: string[];
    public expiresIn: KeyValuePair<string,string>[];
    public hide: string[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<ApiKeyInfo>) { (Object as any).assign(this, init); }
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

export class RefInfo
{
    public model: string;
    public selfId: string;
    public refId: string;
    public refLabel: string;
    public queryApi: string;

    public constructor(init?: Partial<RefInfo>) { (Object as any).assign(this, init); }
}

export class MetadataPropertyType
{
    public name: string;
    public type: string;
    public namespace: string;
    public isValueType?: boolean;
    public isEnum?: boolean;
    public isPrimaryKey?: boolean;
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
    public uploadTo: string;
    public input: InputInfo;
    public format: FormatInfo;
    public ref: RefInfo;

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
    public icon: ImageInfo;
    public isNested?: boolean;
    public isEnum?: boolean;
    public isEnumInt?: boolean;
    public isInterface?: boolean;
    public isAbstract?: boolean;
    public isGenericTypeDef?: boolean;
    public dataContract: MetadataDataContract;
    public properties: MetadataPropertyType[];
    public attributes: MetadataAttribute[];
    public innerTypes: MetadataTypeName[];
    public enumNames: string[];
    public enumValues: string[];
    public enumMemberValues: string[];
    public enumDescriptions: string[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<MetadataType>) { (Object as any).assign(this, init); }
}

export class CommandInfo
{
    public name: string;
    public tag: string;
    public request: MetadataType;
    public response: MetadataType;

    public constructor(init?: Partial<CommandInfo>) { (Object as any).assign(this, init); }
}

export class CommandsInfo
{
    public commands: CommandInfo[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<CommandsInfo>) { (Object as any).assign(this, init); }
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
    public meta: { [index:string]: string; };

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
    public serviceRoutes: { [index:string]: string[]; };
    public typeValidators: ScriptMethodType[];
    public propertyValidators: ScriptMethodType[];
    public accessRole: string;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<ValidationInfo>) { (Object as any).assign(this, init); }
}

export class SharpPagesInfo
{
    public apiPath: string;
    public scriptAdminRole: string;
    public metadataDebugAdminRole: string;
    public metadataDebug?: boolean;
    public spaFallback?: boolean;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<SharpPagesInfo>) { (Object as any).assign(this, init); }
}

export class RequestLogsInfo
{
    public accessRole: string;
    public requestLogger: string;
    public defaultLimit: number;
    public serviceRoutes: { [index:string]: string[]; };
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<RequestLogsInfo>) { (Object as any).assign(this, init); }
}

export class ProfilingInfo
{
    public accessRole: string;
    public defaultLimit: number;
    public summaryFields: string[];
    public tagLabel?: string;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<ProfilingInfo>) { (Object as any).assign(this, init); }
}

export class FilesUploadLocation
{
    public name: string;
    public readAccessRole: string;
    public writeAccessRole: string;
    public allowExtensions: string[];
    public allowOperations: string;
    public maxFileCount?: number;
    public minFileBytes?: number;
    public maxFileBytes?: number;

    public constructor(init?: Partial<FilesUploadLocation>) { (Object as any).assign(this, init); }
}

export class FilesUploadInfo
{
    public basePath: string;
    public locations: FilesUploadLocation[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<FilesUploadInfo>) { (Object as any).assign(this, init); }
}

export class MediaRule
{
    public size: string;
    public rule: string;
    public applyTo: string[];
    public meta: { [index:string]: string; };

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
    public formLayout: InputInfo[];
    public css: ApiCss;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AdminUsersInfo>) { (Object as any).assign(this, init); }
}

export class AdminIdentityUsersInfo
{
    public accessRole: string;
    public enabled: string[];
    public identityUser: MetadataType;
    public allRoles: string[];
    public allPermissions: string[];
    public queryIdentityUserProperties: string[];
    public queryMediaRules: MediaRule[];
    public formLayout: InputInfo[];
    public css: ApiCss;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AdminIdentityUsersInfo>) { (Object as any).assign(this, init); }
}

export class AdminRedisInfo
{
    public queryLimit: number;
    public databases: number[];
    public modifiableConnection?: boolean;
    public endpoint: RedisEndpointInfo;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AdminRedisInfo>) { (Object as any).assign(this, init); }
}

export class SchemaInfo
{
    public alias: string;
    public name: string;
    public tables: string[];

    public constructor(init?: Partial<SchemaInfo>) { (Object as any).assign(this, init); }
}

export class DatabaseInfo
{
    public alias: string;
    public name: string;
    public schemas: SchemaInfo[];

    public constructor(init?: Partial<DatabaseInfo>) { (Object as any).assign(this, init); }
}

export class AdminDatabaseInfo
{
    public queryLimit: number;
    public databases: DatabaseInfo[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AdminDatabaseInfo>) { (Object as any).assign(this, init); }
}

export class PluginInfo
{
    public loaded: string[];
    public auth: AuthInfo;
    public apiKey: ApiKeyInfo;
    public commands: CommandsInfo;
    public autoQuery: AutoQueryInfo;
    public validation: ValidationInfo;
    public sharpPages: SharpPagesInfo;
    public requestLogs: RequestLogsInfo;
    public profiling: ProfilingInfo;
    public filesUpload: FilesUploadInfo;
    public adminUsers: AdminUsersInfo;
    public adminIdentityUsers: AdminIdentityUsersInfo;
    public adminRedis: AdminRedisInfo;
    public adminDatabase: AdminDatabaseInfo;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<PluginInfo>) { (Object as any).assign(this, init); }
}

export class CustomPluginInfo
{
    public accessRole: string;
    public serviceRoutes: { [index:string]: string[]; };
    public enabled: string[];
    public meta: { [index:string]: string; };

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
    public addDocAnnotations: boolean;
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
    public addNullableAnnotations: boolean;
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
    public exportTags: string[];
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

export class ApiUiInfo
{
    public locodeCss: ApiCss;
    public explorerCss: ApiCss;
    public formLayout: InputInfo[];
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<ApiUiInfo>) { (Object as any).assign(this, init); }
}

export class MetadataOperationType
{
    public request: MetadataType;
    public response: MetadataType;
    public actions: string[];
    public returnsVoid?: boolean;
    public method: string;
    public returnType: MetadataTypeName;
    public routes: MetadataRoute[];
    public dataModel: MetadataTypeName;
    public viewModel: MetadataTypeName;
    public requiresAuth?: boolean;
    public requiresApiKey?: boolean;
    public requiredRoles: string[];
    public requiresAnyRole: string[];
    public requiredPermissions: string[];
    public requiresAnyPermission: string[];
    public tags: string[];
    public ui: ApiUiInfo;

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
export class AdminRole
{

    public constructor(init?: Partial<AdminRole>) { (Object as any).assign(this, init); }
}

export class ServerStats
{
    public redis: { [index:string]: number; };
    public serverEvents: { [index:string]: string; };
    public mqDescription: string;
    public mqWorkers: { [index:string]: number; };

    public constructor(init?: Partial<ServerStats>) { (Object as any).assign(this, init); }
}

// @DataContract
export class QueryResponse<T>
{
    // @DataMember(Order=1)
    public offset: number;

    // @DataMember(Order=2)
    public total: number;

    // @DataMember(Order=3)
    public results: T[];

    // @DataMember(Order=4)
    public meta: { [index:string]: string; };

    // @DataMember(Order=5)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<QueryResponse<T>>) { (Object as any).assign(this, init); }
}

export class DiagnosticEntry
{
    public id: number;
    public traceId?: string;
    public source: string;
    public eventType: string;
    public message: string;
    public operation: string;
    public threadId: number;
    public error?: ResponseStatus;
    public commandType: string;
    public command: string;
    public userAuthId?: string;
    public sessionId?: string;
    public arg?: string;
    public args?: string[];
    public argLengths?: number[];
    public namedArgs?: { [index:string]: Object; };
    public duration?: string;
    public timestamp: number;
    public date: string;
    public tag?: string;
    public stackTrace?: string;
    public meta: { [index:string]: string; } = {};

    public constructor(init?: Partial<DiagnosticEntry>) { (Object as any).assign(this, init); }
}

export class RedisSearchResult
{
    public id: string;
    public type: string;
    public ttl: number;
    public size: number;

    public constructor(init?: Partial<RedisSearchResult>) { (Object as any).assign(this, init); }
}

export class RedisText
{
    public text: string;
    public children: RedisText[];

    public constructor(init?: Partial<RedisText>) { (Object as any).assign(this, init); }
}

export class CommandSummary
{
    public type: string;
    public name: string;
    public count: number;
    public failed: number;
    public retries: number;
    public totalMs: number;
    public minMs: number;
    public maxMs: number;
    public averageMs: number;
    public medianMs: number;
    public lastError?: ResponseStatus;
    public timings: ConcurrentQueue<number>;

    public constructor(init?: Partial<CommandSummary>) { (Object as any).assign(this, init); }
}

export class CommandResult
{
    public type: string;
    public name: string;
    public ms?: number;
    public at: string;
    public request: string;
    public retries?: number;
    public attempt: number;
    public error?: ResponseStatus;

    public constructor(init?: Partial<CommandResult>) { (Object as any).assign(this, init); }
}

// @DataContract
export class PartialApiKey
{
    // @DataMember(Order=1)
    public id: number;

    // @DataMember(Order=2)
    public name: string;

    // @DataMember(Order=3)
    public userId: string;

    // @DataMember(Order=4)
    public userName: string;

    // @DataMember(Order=5)
    public visibleKey: string;

    // @DataMember(Order=6)
    public environment: string;

    // @DataMember(Order=7)
    public createdDate: string;

    // @DataMember(Order=8)
    public expiryDate?: string;

    // @DataMember(Order=9)
    public cancelledDate?: string;

    // @DataMember(Order=10)
    public lastUsedDate?: string;

    // @DataMember(Order=11)
    public scopes: string[];

    // @DataMember(Order=12)
    public features: string[];

    // @DataMember(Order=13)
    public restrictTo: string[];

    // @DataMember(Order=14)
    public notes: string;

    // @DataMember(Order=15)
    public refId?: number;

    // @DataMember(Order=16)
    public refIdStr: string;

    // @DataMember(Order=17)
    public meta: { [index:string]: string; };

    // @DataMember(Order=18)
    public active: boolean;

    public constructor(init?: Partial<PartialApiKey>) { (Object as any).assign(this, init); }
}

export class JobStatSummary
{
    public name: string;
    public total: number;
    public completed: number;
    public retries: number;
    public failed: number;
    public cancelled: number;

    public constructor(init?: Partial<JobStatSummary>) { (Object as any).assign(this, init); }
}

export class HourSummary
{
    public hour: string;
    public total: number;
    public completed: number;
    public failed: number;
    public cancelled: number;

    public constructor(init?: Partial<HourSummary>) { (Object as any).assign(this, init); }
}

export class WorkerStats
{
    public name: string;
    public queued: number;
    public received: number;
    public completed: number;
    public retries: number;
    public failed: number;
    public runningJob?: number;
    public runningTime?: string;

    public constructor(init?: Partial<WorkerStats>) { (Object as any).assign(this, init); }
}

export class RequestLogEntry
{
    public id: number;
    public traceId: string;
    public operationName: string;
    public dateTime: string;
    public statusCode: number;
    public statusDescription: string;
    public httpMethod: string;
    public absoluteUri: string;
    public pathInfo: string;
    // @StringLength(2147483647)
    public requestBody: string;

    public requestDto: Object;
    public userAuthId: string;
    public sessionId: string;
    public ipAddress: string;
    public forwardedFor: string;
    public referer: string;
    public headers: { [index:string]: string; };
    public formData: { [index:string]: string; };
    public items: { [index:string]: string; };
    public responseHeaders: { [index:string]: string; };
    public session: Object;
    public responseDto: Object;
    public errorResponse: Object;
    public exceptionSource: string;
    public exceptionData: any;
    public requestDuration: string;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<RequestLogEntry>) { (Object as any).assign(this, init); }
}

// @DataContract
export class RequestSummary
{
    // @DataMember(Order=1)
    public name: string;

    // @DataMember(Order=2)
    public requests: number;

    // @DataMember(Order=3)
    public requestLength: number;

    // @DataMember(Order=4)
    public duration: number;

    public constructor(init?: Partial<RequestSummary>) { (Object as any).assign(this, init); }
}

export class KeyValuePair<TKey, TValue>
{
    public key: TKey;
    public value: TValue;

    public constructor(init?: Partial<KeyValuePair<TKey, TValue>>) { (Object as any).assign(this, init); }
}

export class AppMetadata
{
    public date: string;
    public app: AppInfo;
    public ui: UiInfo;
    public config: ConfigInfo;
    public contentTypeFormats: { [index:string]: string; };
    public httpHandlers: { [index:string]: string; };
    public plugins: PluginInfo;
    public customPlugins: { [index:string]: CustomPluginInfo; };
    public api: MetadataTypes;
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AppMetadata>) { (Object as any).assign(this, init); }
}

// @DataContract
export class IdResponse
{
    // @DataMember(Order=1)
    public id: string;

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<IdResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminGetRolesResponse
{
    // @DataMember(Order=1)
    public results: AdminRole[];

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminGetRolesResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminGetRoleResponse
{
    // @DataMember(Order=1)
    public result: AdminRole;

    // @DataMember(Order=2)
    public claims: Property[];

    // @DataMember(Order=3)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminGetRoleResponse>) { (Object as any).assign(this, init); }
}

export class AdminDashboardResponse
{
    public serverStats: ServerStats;
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminDashboardResponse>) { (Object as any).assign(this, init); }
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
    public refreshTokenExpiry?: string;

    // @DataMember(Order=9)
    public profileUrl: string;

    // @DataMember(Order=10)
    public roles: string[];

    // @DataMember(Order=11)
    public permissions: string[];

    // @DataMember(Order=12)
    public authProvider: string;

    // @DataMember(Order=13)
    public responseStatus: ResponseStatus;

    // @DataMember(Order=14)
    public meta: { [index:string]: string; };

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
    public meta: { [index:string]: string; };

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
    public meta: { [index:string]: string; };

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
    public result: { [index:string]: Object; };

    // @DataMember(Order=3)
    public details: { [index:string]: Object; }[];

    // @DataMember(Order=4)
    public claims: Property[];

    // @DataMember(Order=5)
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

export class AdminProfilingResponse
{
    public results: DiagnosticEntry[] = [];
    public total: number;
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminProfilingResponse>) { (Object as any).assign(this, init); }
}

export class AdminRedisResponse
{
    public db: number;
    public searchResults?: RedisSearchResult[];
    public info?: { [index:string]: string; };
    public endpoint?: RedisEndpointInfo;
    public result?: RedisText;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminRedisResponse>) { (Object as any).assign(this, init); }
}

export class AdminDatabaseResponse
{
    public results: { [index:string]: Object; }[] = [];
    public total?: number;
    public columns?: MetadataPropertyType[];
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminDatabaseResponse>) { (Object as any).assign(this, init); }
}

export class ViewCommandsResponse
{
    public commandTotals: CommandSummary[] = [];
    public latestCommands: CommandResult[] = [];
    public latestFailed: CommandResult[] = [];
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<ViewCommandsResponse>) { (Object as any).assign(this, init); }
}

export class ExecuteCommandResponse
{
    public commandResult?: CommandResult;
    public result?: string;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<ExecuteCommandResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminApiKeysResponse
{
    // @DataMember(Order=1)
    public results: PartialApiKey[];

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminApiKeysResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AdminApiKeyResponse
{
    // @DataMember(Order=1)
    public result: string;

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminApiKeyResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class EmptyResponse
{
    // @DataMember(Order=1)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<EmptyResponse>) { (Object as any).assign(this, init); }
}

export class AdminJobDashboardResponse
{
    public commands: JobStatSummary[] = [];
    public apis: JobStatSummary[] = [];
    public workers: JobStatSummary[] = [];
    public today: HourSummary[] = [];
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminJobDashboardResponse>) { (Object as any).assign(this, init); }
}

export class AdminJobInfoResponse
{
    public monthDbs: string[] = [];
    public tableCounts: { [index:string]: number; } = {};
    public workerStats: WorkerStats[] = [];
    public queueCounts: { [index:string]: number; } = {};
    public workerCounts: { [index:string]: number; } = {};
    public stateCounts: { [index:string]: number; } = {};
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminJobInfoResponse>) { (Object as any).assign(this, init); }
}

export class AdminGetJobResponse
{
    public result: JobSummary;
    public queued?: BackgroundJob;
    public completed?: CompletedJob;
    public failed?: FailedJob;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminGetJobResponse>) { (Object as any).assign(this, init); }
}

export class AdminGetJobProgressResponse
{
    public state: BackgroundJobState;
    public progress?: number;
    public status?: string;
    public logs?: string;
    public durationMs?: number;
    public error?: ResponseStatus;
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminGetJobProgressResponse>) { (Object as any).assign(this, init); }
}

export class AdminRequeueFailedJobsJobsResponse
{
    public results: number[] = [];
    public errors: { [index:number]: string; } = {};
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminRequeueFailedJobsJobsResponse>) { (Object as any).assign(this, init); }
}

export class AdminCancelJobsResponse
{
    public results: number[] = [];
    public errors: { [index:number]: string; } = {};
    public responseStatus?: ResponseStatus;

    public constructor(init?: Partial<AdminCancelJobsResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class RequestLogsResponse
{
    // @DataMember(Order=1)
    public results: RequestLogEntry[];

    // @DataMember(Order=2)
    public usage: { [index:string]: string; };

    // @DataMember(Order=3)
    public total: number;

    // @DataMember(Order=4)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<RequestLogsResponse>) { (Object as any).assign(this, init); }
}

// @DataContract
export class AnalyticsReports
{
    // @DataMember(Order=1)
    public apis: { [index:string]: RequestSummary; };

    // @DataMember(Order=2)
    public users: { [index:string]: RequestSummary; };

    // @DataMember(Order=3)
    public tags: { [index:string]: RequestSummary; };

    // @DataMember(Order=4)
    public status: { [index:string]: RequestSummary; };

    // @DataMember(Order=5)
    public days: { [index:string]: RequestSummary; };

    // @DataMember(Order=6)
    public apiKeys: { [index:string]: RequestSummary; };

    // @DataMember(Order=7)
    public ipAddresses: { [index:string]: RequestSummary; };

    // @DataMember(Order=8)
    public durationRange: { [index:string]: number; };

    public constructor(init?: Partial<AnalyticsReports>) { (Object as any).assign(this, init); }
}

// @DataContract
export class GetValidationRulesResponse
{
    // @DataMember(Order=1)
    public results: ValidationRule[];

    // @DataMember(Order=2)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<GetValidationRulesResponse>) { (Object as any).assign(this, init); }
}

// @Route("/metadata/app")
// @DataContract
export class MetadataApp implements IReturn<AppMetadata>, IGet
{
    // @DataMember(Order=1)
    public view: string;

    // @DataMember(Order=2)
    public includeTypes: string[];

    public constructor(init?: Partial<MetadataApp>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'MetadataApp'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AppMetadata(); }
}

// @DataContract
export class AdminCreateRole implements IReturn<IdResponse>, IPost
{
    // @DataMember(Order=1)
    public name: string;

    public constructor(init?: Partial<AdminCreateRole>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminCreateRole'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @DataContract
export class AdminGetRoles implements IReturn<AdminGetRolesResponse>, IGet
{

    public constructor(init?: Partial<AdminGetRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminGetRoles'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminGetRolesResponse(); }
}

// @DataContract
export class AdminGetRole implements IReturn<AdminGetRoleResponse>, IGet
{
    // @DataMember(Order=1)
    public id: string;

    public constructor(init?: Partial<AdminGetRole>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminGetRole'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminGetRoleResponse(); }
}

// @DataContract
export class AdminUpdateRole implements IReturn<IdResponse>, IPost
{
    // @DataMember(Order=1)
    public id: string;

    // @DataMember(Order=2)
    public name: string;

    // @DataMember(Order=3)
    public addClaims: Property[];

    // @DataMember(Order=4)
    public removeClaims: Property[];

    // @DataMember(Order=5)
    public responseStatus: ResponseStatus;

    public constructor(init?: Partial<AdminUpdateRole>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminUpdateRole'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new IdResponse(); }
}

// @DataContract
export class AdminDeleteRole implements IReturnVoid, IDelete
{
    // @DataMember(Order=1)
    public id: string;

    public constructor(init?: Partial<AdminDeleteRole>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminDeleteRole'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() {}
}

export class AdminDashboard implements IReturn<AdminDashboardResponse>, IGet
{

    public constructor(init?: Partial<AdminDashboard>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminDashboard'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminDashboardResponse(); }
}

/** @description Sign In */
// @Route("/auth", "GET,POST")
// @Route("/auth/{provider}", "GET,POST")
// @Api(Description="Sign In")
// @DataContract
export class Authenticate implements IReturn<AuthenticateResponse>, IPost
{
    /** @description AuthProvider, e.g. credentials */
    // @DataMember(Order=1)
    public provider: string;

    // @DataMember(Order=2)
    public userName: string;

    // @DataMember(Order=3)
    public password: string;

    // @DataMember(Order=4)
    public rememberMe?: boolean;

    // @DataMember(Order=5)
    public accessToken: string;

    // @DataMember(Order=6)
    public accessTokenSecret: string;

    // @DataMember(Order=7)
    public returnUrl: string;

    // @DataMember(Order=8)
    public errorView: string;

    // @DataMember(Order=9)
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<Authenticate>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'Authenticate'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AuthenticateResponse(); }
}

// @Route("/assignroles", "POST")
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
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AssignRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AssignRoles'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AssignRolesResponse(); }
}

// @Route("/unassignroles", "POST")
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
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<UnAssignRoles>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'UnAssignRoles'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new UnAssignRolesResponse(); }
}

// @DataContract
export class AdminGetUser implements IReturn<AdminUserResponse>, IGet
{
    // @DataMember(Order=10)
    public id: string;

    public constructor(init?: Partial<AdminGetUser>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminGetUser'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminUserResponse(); }
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
    public getTypeName() { return 'AdminQueryUsers'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminUsersResponse(); }
}

// @DataContract
export class AdminCreateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPost
{
    // @DataMember(Order=10)
    public roles: string[];

    // @DataMember(Order=11)
    public permissions: string[];

    public constructor(init?: Partial<AdminCreateUser>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminCreateUser'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AdminUserResponse(); }
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
    public lockUserUntil?: string;

    // @DataMember(Order=14)
    public addRoles: string[];

    // @DataMember(Order=15)
    public removeRoles: string[];

    // @DataMember(Order=16)
    public addPermissions: string[];

    // @DataMember(Order=17)
    public removePermissions: string[];

    // @DataMember(Order=18)
    public addClaims: Property[];

    // @DataMember(Order=19)
    public removeClaims: Property[];

    public constructor(init?: Partial<AdminUpdateUser>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminUpdateUser'; }
    public getMethod() { return 'PUT'; }
    public createResponse() { return new AdminUserResponse(); }
}

// @DataContract
export class AdminDeleteUser implements IReturn<AdminDeleteUserResponse>, IDelete
{
    // @DataMember(Order=10)
    public id: string;

    public constructor(init?: Partial<AdminDeleteUser>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminDeleteUser'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new AdminDeleteUserResponse(); }
}

export class AdminQueryRequestLogs extends QueryDb<RequestLog> implements IReturn<QueryResponse<RequestLog>>
{
    public month?: string;

    public constructor(init?: Partial<AdminQueryRequestLogs>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryRequestLogs'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<RequestLog>(); }
}

export class AdminProfiling implements IReturn<AdminProfilingResponse>
{
    public source?: string;
    public eventType?: string;
    public threadId?: number;
    public traceId?: string;
    public userAuthId?: string;
    public sessionId?: string;
    public tag?: string;
    public skip: number;
    public take?: number;
    public orderBy?: string;
    public withErrors?: boolean;
    public pending?: boolean;

    public constructor(init?: Partial<AdminProfiling>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminProfiling'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AdminProfilingResponse(); }
}

export class AdminRedis implements IReturn<AdminRedisResponse>, IPost
{
    public db?: number;
    public query?: string;
    public reconnect?: RedisEndpointInfo;
    public take?: number;
    public position?: number;
    public args?: string[];

    public constructor(init?: Partial<AdminRedis>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminRedis'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AdminRedisResponse(); }
}

export class AdminDatabase implements IReturn<AdminDatabaseResponse>, IGet
{
    public db?: string;
    public schema?: string;
    public table?: string;
    public fields?: string[];
    public take?: number;
    public skip?: number;
    public orderBy?: string;
    public include?: string;

    public constructor(init?: Partial<AdminDatabase>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminDatabase'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminDatabaseResponse(); }
}

export class ViewCommands implements IReturn<ViewCommandsResponse>, IGet
{
    public include?: string[];
    public skip?: number;
    public take?: number;

    public constructor(init?: Partial<ViewCommands>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ViewCommands'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new ViewCommandsResponse(); }
}

export class ExecuteCommand implements IReturn<ExecuteCommandResponse>, IPost
{
    public command: string;
    public requestJson?: string;

    public constructor(init?: Partial<ExecuteCommand>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ExecuteCommand'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new ExecuteCommandResponse(); }
}

// @DataContract
export class AdminQueryApiKeys implements IReturn<AdminApiKeysResponse>, IGet
{
    // @DataMember(Order=1)
    public id?: number;

    // @DataMember(Order=2)
    public search: string;

    // @DataMember(Order=3)
    public userId: string;

    // @DataMember(Order=4)
    public userName: string;

    // @DataMember(Order=5)
    public orderBy: string;

    // @DataMember(Order=6)
    public skip?: number;

    // @DataMember(Order=7)
    public take?: number;

    public constructor(init?: Partial<AdminQueryApiKeys>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryApiKeys'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminApiKeysResponse(); }
}

// @DataContract
export class AdminCreateApiKey implements IReturn<AdminApiKeyResponse>, IPost
{
    // @DataMember(Order=1)
    public name: string;

    // @DataMember(Order=2)
    public userId: string;

    // @DataMember(Order=3)
    public userName: string;

    // @DataMember(Order=4)
    public scopes: string[];

    // @DataMember(Order=5)
    public features: string[];

    // @DataMember(Order=6)
    public restrictTo: string[];

    // @DataMember(Order=7)
    public expiryDate?: string;

    // @DataMember(Order=8)
    public notes: string;

    // @DataMember(Order=9)
    public refId?: number;

    // @DataMember(Order=10)
    public refIdStr: string;

    // @DataMember(Order=11)
    public meta: { [index:string]: string; };

    public constructor(init?: Partial<AdminCreateApiKey>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminCreateApiKey'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AdminApiKeyResponse(); }
}

// @DataContract
export class AdminUpdateApiKey implements IReturn<EmptyResponse>, IPatch
{
    // @DataMember(Order=1)
    // @Validate(Validator="GreaterThan(0)")
    public id: number;

    // @DataMember(Order=2)
    public name: string;

    // @DataMember(Order=3)
    public userId: string;

    // @DataMember(Order=4)
    public userName: string;

    // @DataMember(Order=5)
    public scopes: string[];

    // @DataMember(Order=6)
    public features: string[];

    // @DataMember(Order=7)
    public restrictTo: string[];

    // @DataMember(Order=8)
    public expiryDate?: string;

    // @DataMember(Order=9)
    public cancelledDate?: string;

    // @DataMember(Order=10)
    public notes: string;

    // @DataMember(Order=11)
    public refId?: number;

    // @DataMember(Order=12)
    public refIdStr: string;

    // @DataMember(Order=13)
    public meta: { [index:string]: string; };

    // @DataMember(Order=14)
    public reset: string[];

    public constructor(init?: Partial<AdminUpdateApiKey>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminUpdateApiKey'; }
    public getMethod() { return 'PATCH'; }
    public createResponse() { return new EmptyResponse(); }
}

// @DataContract
export class AdminDeleteApiKey implements IReturn<EmptyResponse>, IDelete
{
    // @DataMember(Order=1)
    // @Validate(Validator="GreaterThan(0)")
    public id?: number;

    public constructor(init?: Partial<AdminDeleteApiKey>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminDeleteApiKey'; }
    public getMethod() { return 'DELETE'; }
    public createResponse() { return new EmptyResponse(); }
}

export class AdminJobDashboard implements IReturn<AdminJobDashboardResponse>, IGet
{
    public from?: string;
    public to?: string;

    public constructor(init?: Partial<AdminJobDashboard>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminJobDashboard'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminJobDashboardResponse(); }
}

export class AdminJobInfo implements IReturn<AdminJobInfoResponse>, IGet
{
    public month?: string;

    public constructor(init?: Partial<AdminJobInfo>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminJobInfo'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminJobInfoResponse(); }
}

export class AdminGetJob implements IReturn<AdminGetJobResponse>, IGet
{
    public id?: number;
    public refId?: string;

    public constructor(init?: Partial<AdminGetJob>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminGetJob'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminGetJobResponse(); }
}

export class AdminGetJobProgress implements IReturn<AdminGetJobProgressResponse>, IGet
{
    // @Validate(Validator="GreaterThan(0)")
    public id: number;

    public logStart?: number;

    public constructor(init?: Partial<AdminGetJobProgress>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminGetJobProgress'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminGetJobProgressResponse(); }
}

export class AdminQueryBackgroundJobs extends QueryDb<BackgroundJob> implements IReturn<QueryResponse<BackgroundJob>>
{
    public id?: number;
    public refId?: string;

    public constructor(init?: Partial<AdminQueryBackgroundJobs>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryBackgroundJobs'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<BackgroundJob>(); }
}

export class AdminQueryJobSummary extends QueryDb<JobSummary> implements IReturn<QueryResponse<JobSummary>>
{
    public id?: number;
    public refId?: string;

    public constructor(init?: Partial<AdminQueryJobSummary>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryJobSummary'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<JobSummary>(); }
}

export class AdminQueryScheduledTasks extends QueryDb<ScheduledTask> implements IReturn<QueryResponse<ScheduledTask>>
{

    public constructor(init?: Partial<AdminQueryScheduledTasks>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryScheduledTasks'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<ScheduledTask>(); }
}

export class AdminQueryCompletedJobs extends QueryDb<CompletedJob> implements IReturn<QueryResponse<CompletedJob>>
{
    public month?: string;

    public constructor(init?: Partial<AdminQueryCompletedJobs>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryCompletedJobs'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<CompletedJob>(); }
}

export class AdminQueryFailedJobs extends QueryDb<FailedJob> implements IReturn<QueryResponse<FailedJob>>
{
    public month?: string;

    public constructor(init?: Partial<AdminQueryFailedJobs>) { super(init); (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminQueryFailedJobs'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new QueryResponse<FailedJob>(); }
}

export class AdminRequeueFailedJobs implements IReturn<AdminRequeueFailedJobsJobsResponse>
{
    public ids?: number[];

    public constructor(init?: Partial<AdminRequeueFailedJobs>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminRequeueFailedJobs'; }
    public getMethod() { return 'POST'; }
    public createResponse() { return new AdminRequeueFailedJobsJobsResponse(); }
}

export class AdminCancelJobs implements IReturn<AdminCancelJobsResponse>, IGet
{
    public ids?: number[];
    public worker?: string;
    public state?: BackgroundJobState;
    public cancelWorker?: string;

    public constructor(init?: Partial<AdminCancelJobs>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'AdminCancelJobs'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AdminCancelJobsResponse(); }
}

// @Route("/requestlogs")
// @DataContract
export class RequestLogs implements IReturn<RequestLogsResponse>, IGet
{
    // @DataMember(Order=1)
    public beforeSecs?: number;

    // @DataMember(Order=2)
    public afterSecs?: number;

    // @DataMember(Order=3)
    public operationName: string;

    // @DataMember(Order=4)
    public ipAddress: string;

    // @DataMember(Order=5)
    public forwardedFor: string;

    // @DataMember(Order=6)
    public userAuthId: string;

    // @DataMember(Order=7)
    public sessionId: string;

    // @DataMember(Order=8)
    public referer: string;

    // @DataMember(Order=9)
    public pathInfo: string;

    // @DataMember(Order=10)
    public ids: number[];

    // @DataMember(Order=11)
    public beforeId?: number;

    // @DataMember(Order=12)
    public afterId?: number;

    // @DataMember(Order=13)
    public hasResponse?: boolean;

    // @DataMember(Order=14)
    public withErrors?: boolean;

    // @DataMember(Order=15)
    public enableSessionTracking?: boolean;

    // @DataMember(Order=16)
    public enableResponseTracking?: boolean;

    // @DataMember(Order=17)
    public enableErrorTracking?: boolean;

    // @DataMember(Order=18)
    public durationLongerThan?: string;

    // @DataMember(Order=19)
    public durationLessThan?: string;

    // @DataMember(Order=20)
    public skip: number;

    // @DataMember(Order=21)
    public take?: number;

    // @DataMember(Order=22)
    public orderBy: string;

    public constructor(init?: Partial<RequestLogs>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'RequestLogs'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new RequestLogsResponse(); }
}

// @DataContract
export class GetAnalyticsReports implements IReturn<AnalyticsReports>, IGet
{
    // @DataMember(Order=1)
    public month?: string;

    public constructor(init?: Partial<GetAnalyticsReports>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetAnalyticsReports'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new AnalyticsReports(); }
}

// @Route("/validation/rules/{Type}")
// @DataContract
export class GetValidationRules implements IReturn<GetValidationRulesResponse>, IGet
{
    // @DataMember(Order=1)
    public authSecret: string;

    // @DataMember(Order=2)
    public type: string;

    public constructor(init?: Partial<GetValidationRules>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'GetValidationRules'; }
    public getMethod() { return 'GET'; }
    public createResponse() { return new GetValidationRulesResponse(); }
}

// @Route("/validation/rules")
// @DataContract
export class ModifyValidationRules implements IReturnVoid
{
    // @DataMember(Order=1)
    public authSecret: string;

    // @DataMember(Order=2)
    public saveRules: ValidationRule[];

    // @DataMember(Order=3)
    public deleteRuleIds: number[];

    // @DataMember(Order=4)
    public suspendRuleIds: number[];

    // @DataMember(Order=5)
    public unsuspendRuleIds: number[];

    // @DataMember(Order=6)
    public clearCache?: boolean;

    public constructor(init?: Partial<ModifyValidationRules>) { (Object as any).assign(this, init); }
    public getTypeName() { return 'ModifyValidationRules'; }
    public getMethod() { return 'POST'; }
    public createResponse() {}
}



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
    /** Get Locode URL */
    locodeUrl: (op:string) => string;
    /** Get URL with initial queryString state */
    urlWithState: (url:string) => string;
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
