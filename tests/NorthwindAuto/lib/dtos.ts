/* Options:
Date: 2022-01-08 14:02:56
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

export class UiInfo
{
    public brandIconUri: string;
    public hideTags: string[];
    public alwaysHideTags: string[];
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
    public logoUri: string;
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

// @Route("/metadata/app")
// @DataContract
export class MetadataApp implements IReturn<AppMetadata>
{

    public constructor(init?: Partial<MetadataApp>) { (Object as any).assign(this, init); }
    public createResponse() { return new AppMetadata(); }
    public getTypeName() { return 'MetadataApp'; }
    public getMethod() { return 'POST'; }
}

