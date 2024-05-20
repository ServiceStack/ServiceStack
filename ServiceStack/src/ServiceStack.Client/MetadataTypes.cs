using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack;

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataTypesConfig
{
    public MetadataTypesConfig(
        string baseUrl = null,
        bool makePartial = true,
        bool makeVirtual = true,
        bool addReturnMarker = true,
        bool convertDescriptionToComments = true,
        bool addDocAnnotations = true,
        bool addDataContractAttributes = false,
        bool addIndexesToDataMembers = false,
        bool addGeneratedCodeAttributes = false,
        string addDefaultXmlNamespace = null,
        string baseClass = null,
        string package = null,
        bool addResponseStatus = false,
        bool addServiceStackTypes = true,
        bool addModelExtensions = true,
        bool addPropertyAccessors = true,
        bool excludeGenericBaseTypes = false,
        bool settersReturnThis = true,
        bool addNullableAnnotations = false,
        bool makePropertiesOptional = false,
        bool makeDataContractsExtensible = false,
        bool initializeCollections = true,
        int? addImplicitVersion = null)
    {
        BaseUrl = baseUrl;
        MakePartial = makePartial;
        MakeVirtual = makeVirtual;
        AddReturnMarker = addReturnMarker;
        AddDescriptionAsComments = convertDescriptionToComments;
        AddDocAnnotations = addDocAnnotations;
        AddDataContractAttributes = addDataContractAttributes;
        AddDefaultXmlNamespace = addDefaultXmlNamespace;
        BaseClass = baseClass;
        Package = package;
        MakeDataContractsExtensible = makeDataContractsExtensible;
        AddIndexesToDataMembers = addIndexesToDataMembers;
        AddGeneratedCodeAttributes = addGeneratedCodeAttributes;
        InitializeCollections = initializeCollections;
        AddResponseStatus = addResponseStatus;
        AddServiceStackTypes = addServiceStackTypes;
        AddModelExtensions = addModelExtensions;
        AddPropertyAccessors = addPropertyAccessors;
        ExcludeGenericBaseTypes = excludeGenericBaseTypes;
        SettersReturnThis = settersReturnThis;
        AddNullableAnnotations = addNullableAnnotations;
        MakePropertiesOptional = makePropertiesOptional;
        AddImplicitVersion = addImplicitVersion;
    }

    public string BaseUrl { get; set; }
    public string UsePath { get; set; }
    public bool MakePartial { get; set; }
    public bool MakeVirtual { get; set; }
    public bool MakeInternal { get; set; }
    public string BaseClass { get; set; }
    public string Package { get; set; }
    public bool AddReturnMarker { get; set; }
    public bool AddDescriptionAsComments { get; set; }
    public bool AddDocAnnotations { get; set; }
    public bool AddDataContractAttributes { get; set; }
    public bool AddIndexesToDataMembers { get; set; }
    public bool AddGeneratedCodeAttributes { get; set; }
    public int? AddImplicitVersion { get; set; }
    public bool AddResponseStatus { get; set; }
    public bool AddServiceStackTypes { get; set; }
    public bool AddModelExtensions { get; set; }
    public bool AddPropertyAccessors { get; set; }
    public bool ExcludeGenericBaseTypes { get; set; }
    public bool SettersReturnThis { get; set; }
    public bool AddNullableAnnotations { get; set; }
    public bool MakePropertiesOptional { get; set; }
    public bool ExportAsTypes { get; set; }
    public bool ExcludeImplementedInterfaces { get; set; }
    public string AddDefaultXmlNamespace { get; set; }
    public bool MakeDataContractsExtensible { get; set; }
    public bool InitializeCollections { get; set; }
    public List<string> AddNamespaces { get; set; }
    public List<string> DefaultNamespaces { get; set; }
    public List<string> DefaultImports { get; set; }
    public List<string> IncludeTypes { get; set; }
    public List<string> ExcludeTypes { get; set; }
    public List<string> ExportTags { get; set; }
    public List<string> TreatTypesAsStrings { get; set; }
    public bool ExportValueTypes { get; set; }

    public string GlobalNamespace { get; set; }
    public bool ExcludeNamespace { get; set; }
    public string DataClass { get; set; }
    public string DataClassJson { get; set; }

    public HashSet<Type> IgnoreTypes { get; set; }
    public HashSet<Type> ExportTypes { get; set; }
    public HashSet<Type> ExportAttributes { get; set; }
    public List<string> IgnoreTypesInNamespaces { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataTypes
{
    public MetadataTypes()
    {
        Types = new List<MetadataType>();
        Operations = new List<MetadataOperationType>();
        Namespaces = new List<string>();
    }

    public MetadataTypesConfig Config { get; set; }
    public List<string> Namespaces { get; set; }
    public List<MetadataType> Types { get; set; }
    public List<MetadataOperationType> Operations { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AppMetadata : IMeta
{
    public DateTime Date { get; set; }
    public AppInfo App { get; set; }
    public UiInfo Ui { get; set; }
    public ConfigInfo Config { get; set; }
    public Dictionary<string, string> ContentTypeFormats { get; set; }
    public Dictionary<string, string> HttpHandlers { get; set; }
    public PluginInfo Plugins { get; set; }
    public Dictionary<string,CustomPluginInfo> CustomPlugins { get; set; }
    public MetadataTypes Api { get; set; }
    public Dictionary<string, string> Meta { get; set; }
    
    [IgnoreDataMember]
    public AppMetadataCache Cache { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ConfigInfo : IMeta
{
    public bool? DebugMode { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class PluginInfo : IMeta
{
    public List<string> Loaded { get; set; }
    public AuthInfo Auth { get; set; }
    public ApiKeyInfo ApiKey { get; set; }
    public AutoQueryInfo AutoQuery { get; set; }
    public ValidationInfo Validation { get; set; }
    public SharpPagesInfo SharpPages { get; set; }
    public RequestLogsInfo RequestLogs { get; set; }
    public ProfilingInfo Profiling { get; set; }
    public FilesUploadInfo FilesUpload { get; set; }
    public AdminUsersInfo AdminUsers { get; set; }
    public AdminIdentityUsersInfo AdminIdentityUsers { get; set; }
    public AdminRedisInfo AdminRedis { get; set; }
    public AdminDatabaseInfo AdminDatabase { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class LinkInfo
{
    public string Id { get; set; }
    public string Href { get; set; }
    public string Label { get; set; }
    public ImageInfo Icon { get; set; }
    /// <summary>
    /// Only show if authAttributes.contains(Show) E.g. limit to role:TheRole 
    /// </summary>
    public string Show { get; set; }
    /// <summary>
    /// Do not show if authAttributes.contains(Hide) E.g. hide from role:TheRole 
    /// </summary>
    public string Hide { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ImageInfo
{
    public string Svg { get; set; }
    public string Uri { get; set; }
    public string Alt { get; set; }
    public string Cls { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ApiFormat
{
    /// <summary>
    /// If not specified uses browser's navigator.languages
    /// </summary>
    public string Locale { get; set; }
    /// <summary>
    /// Assume Dates are returned as UTC 
    /// </summary>
    public bool AssumeUtc { get; set; }
    public FormatInfo Number { get; set; }
    public FormatInfo Date { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class FormatInfo
{
    public string Method { get; set; }
    public string Options { get; set; }
    public string Locale { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class RefInfo
{
    [IgnoreDataMember]
    public Type ModelType { get; set; }
    [IgnoreDataMember]
    public Type QueryType { get; set; }

    public string Model { get; set; }
    public string SelfId { get; set; }
    public string RefId { get; set; }
    public string RefLabel { get; set; }
    public string QueryApi { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AuthInfo : IMeta
{
    public bool? HasAuthSecret { get; set; }
    public bool? HasAuthRepository { get; set; }
    public bool? IncludesRoles { get; set; }
    public bool? IncludesOAuthTokens { get; set; }
    public string HtmlRedirect { get; set; }
    public List<MetaAuthProvider> AuthProviders { get; set; }
    public IdentityAuthInfo IdentityAuth { get; set; }
    
    public Dictionary<string, List<LinkInfo>> RoleLinks { get; set; }
    public Dictionary<string,string[]> ServiceRoutes { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ApiKeyInfo : IMeta
{
    public string Label { get; set; }
    public List<InputInfo> FormLayout { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public class IdentityAuthInfo : IMeta
{
    public bool? HasRefreshToken { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AutoQueryInfo : IMeta
{
    public int? MaxLimit { get; set; }
    public bool? UntypedQueries { get; set; }
    public bool? RawSqlFilters { get; set; }
    public bool? AutoQueryViewer { get; set; }
    public bool? Async { get; set; }
    public bool? OrderByPrimaryKey { get; set; }
    public bool? CrudEvents { get; set; }
    public bool? CrudEventsServices { get; set; }
    public string AccessRole { get; set; }
    public string NamedConnection { get; set; }
    public List<AutoQueryConvention> ViewerConventions { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ValidationInfo : IMeta
{
    public bool? HasValidationSource { get; set; }
    public bool? HasValidationSourceAdmin { get; set; }
    public Dictionary<string,string[]> ServiceRoutes { get; set; }
    public List<ScriptMethodType> TypeValidators { get; set; }
    public List<ScriptMethodType> PropertyValidators { get; set; }
    public string AccessRole { get; set; }
    
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class SharpPagesInfo : IMeta
{
    public string ApiPath { get; set; }
    public string ScriptAdminRole { get; set; }
    public string MetadataDebugAdminRole { get; set; }
    public bool? MetadataDebug { get; set; }
    public bool? SpaFallback { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class RequestLogsInfo : IMeta
{
    public string AccessRole { get; set; }
    [Obsolete("Use AccessRole")]
    public string[] RequiredRoles { get; set; }
    public string RequestLogger { get; set; }
    public int DefaultLimit { get; set; }
    public Dictionary<string,string[]> ServiceRoutes { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ProfilingInfo : IMeta
{
    public string AccessRole { get; set; }
    public int DefaultLimit { get; set; }
    public List<string> SummaryFields { get; set; }
    public string? TagLabel { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class FilesUploadInfo : IMeta
{
    public string BasePath { get; set; }
    public List<FilesUploadLocation> Locations { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

public class FilesUploadLocation
{
    public string Name { get; set; }
    public string ReadAccessRole { get; set; }
    public string WriteAccessRole { get; set; }
    public HashSet<string> AllowExtensions { get; set; }
    public string AllowOperations { get; set; }
    public int? MaxFileCount { get; set; }
    public long? MinFileBytes { get; set; }
    public long? MaxFileBytes { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AdminUsersInfo : IMeta
{
    public string AccessRole { get; set; }
    public List<string> Enabled { get; set; }
    public MetadataType UserAuth { get; set; }
    public List<string> AllRoles { get; set; }
    public List<string> AllPermissions { get; set; }
    public List<string> QueryUserAuthProperties { get; set; }
    
    public List<MediaRule> QueryMediaRules { get; set; }
    
    public List<InputInfo> FormLayout { get; set; }
    public ApiCss Css { get; set; } 
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AdminIdentityUsersInfo : IMeta
{
    public string AccessRole { get; set; }
    public List<string> Enabled { get; set; }
    public MetadataType IdentityUser { get; set; }
    public List<string> AllRoles { get; set; }
    public List<string> AllPermissions { get; set; }
    public List<string> QueryIdentityUserProperties { get; set; }
    
    public List<MediaRule> QueryMediaRules { get; set; }
    
    public List<InputInfo> FormLayout { get; set; }
    public ApiCss Css { get; set; } 
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AdminRedisInfo : IMeta
{
    public int QueryLimit { get; set; }
    public List<int> Databases { get; set; }
    public bool? ModifiableConnection { get; set; }
    public RedisEndpointInfo Endpoint { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class RedisEndpointInfo
{
    public string Host { get; set; }
    public int Port { get; set; }
    public bool? Ssl { get; set; }
    public long Db { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AdminDatabaseInfo : IMeta
{
    public int QueryLimit { get; set; }
    public List<DatabaseInfo> Databases { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class DatabaseInfo
{
    public string Alias { get; set; }
    public string Name { get; set; }
    public List<SchemaInfo> Schemas { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class SchemaInfo
{
    public string Alias { get; set; }
    public string Name { get; set; }
    public List<string> Tables { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class InputInfo : IMeta
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string Placeholder { get; set; }
    public string Help { get; set; }
    public string Label { get; set; }
    public string Title { get; set; }
    public string Size { get; set; }
    public string Pattern { get; set; }
    public bool? ReadOnly { get; set; }
    public bool? Required { get; set; }
    public bool? Disabled { get; set; }
    public string Autocomplete { get; set; }
    public string Autofocus  { get; set; }
    public string Min { get; set; }
    public string Max { get; set; }
    public string Step { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string Accept  { get; set; }
    public string Capture  { get; set; }
    public bool? Multiple { get; set; }
    public string[] AllowableValues { get; set; }
    public KeyValuePair<string,string>[] AllowableEntries { get; set; }
    public string Options  { get; set; }
    public bool? Ignore { get; set; }
    public FieldCss Css { get; set; }
    
    public Dictionary<string, string> Meta { get; set; }

    public InputInfo() { }
    public InputInfo(string id) => Id = id;
    public InputInfo(string id, string type)
    {
        Id = id;
        Type = type;
    }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MediaRule : IMeta
{
    public string Size { get; set; }
    public string Rule { get; set; }
    public string[] ApplyTo { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

/// <summary>
/// Generic template for adding metadata info about custom plugins  
/// </summary>
[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class CustomPluginInfo : IMeta
{
    /// <summary>
    /// Which User Roles have access to this Plugins Services. See RoleNames for built-in Roles.
    /// </summary>
    public string AccessRole { get; set; }
    
    /// <summary>
    /// What Services Types (and their user-defined routes) are enabled in this plugin
    /// </summary>
    public Dictionary<string,string[]> ServiceRoutes { get; set; }
    
    /// <summary>
    /// List of enabled features in this plugin
    /// </summary>
    public List<string> Enabled { get; set; }
    
    /// <summary>
    /// Additional custom metadata about this plugin
    /// </summary>
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ScriptMethodType
{
    public string Name { get; set; }
    public string[] ParamNames { get; set; }
    public string[] ParamTypes { get; set; }
    public string ReturnType { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AutoQueryConvention
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Types { get; set; }
    public string ValueType { get; set; }
}

/// <summary>
/// App Info and 
/// </summary>
[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AppInfo : IMeta
{
    /// <summary>
    /// The App's BaseUrl
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// The ServiceStack Version
    /// </summary>
    public string ServiceStackVersion => Text.Env.VersionString;
    /// <summary>
    /// Name of the ServiceStack Instance
    /// </summary>
    public string ServiceName { get; set; }
    /// <summary>
    /// The Config.ApiVersion
    /// </summary>
    public string ApiVersion { get; set; }
    /// <summary>
    /// Textual description of the ServiceStack App (shown in Home Services list)
    /// </summary>
    public string ServiceDescription { get; set; }
    /// <summary>
    /// Icon for this ServiceStack App (shown in Home Services list)
    /// </summary>
    public string ServiceIconUrl { get; set; }
    /// <summary>
    /// Link to your website users can click to find out more about you
    /// </summary>
    public string BrandUrl { get; set; }
    /// <summary>
    /// A custom logo or image that users can click on to visit your site
    /// </summary>
    public string BrandImageUrl { get; set; }
    /// <summary>
    /// The default color of text
    /// </summary>
    public string TextColor { get; set; }
    /// <summary>
    /// The default color of links
    /// </summary>
    public string LinkColor { get; set; }
    /// <summary>
    /// The default background color of each screen
    /// </summary>
    public string BackgroundColor { get; set; }
    /// <summary>
    /// The default background image of each screen anchored to the bottom left
    /// </summary>
    public string BackgroundImageUrl { get; set; }
    /// <summary>
    /// The default icon for each of your App's Services
    /// </summary>
    public string IconUrl { get; set; }

    /// <summary>
    /// The configured JsConfig.TextCase
    /// </summary>
    public string JsTextCase { get; set; }

    /// <summary>
    /// Use System.Text.Json for APIs by default 
    /// </summary>
    public string UseSystemJson { get; set; }
    
    /// <summary>
    /// Info on Endpoint Routing
    /// </summary>
    public List<string>? EndpointRouting { get; set; }
    
    /// <summary>
    /// Custom User-Defined Attributes
    /// </summary>
    public Dictionary<string, string> Meta { get; set; }
}

/// <summary>
/// App Info and 
/// </summary>
[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class UiInfo : IMeta
{
    /// <summary>
    /// The brand icon to use with App brand name
    /// </summary>
    public ImageInfo BrandIcon { get; set; }

    /// <summary>
    /// Hide APIs with tags 
    /// </summary>
    public List<string> HideTags { get; set; }
    
    /// <summary>
    /// The module paths that are loaded
    /// </summary>
    public List<string> Modules { get; set; }

    /// <summary>
    /// Always hide APIs with tags (inc DebugMode) 
    /// </summary>
    public List<string> AlwaysHideTags { get; set; }

    /// <summary>
    /// Admin UI Links
    /// </summary>
    public List<LinkInfo> AdminLinks { get; set; }
    
    /// <summary>
    /// Default Themes for all UIs
    /// </summary>
    public ThemeInfo Theme { get; set; }
    
    /// <summary>
    /// The default styles to use for rendering AutoQuery UI Forms 
    /// </summary>
    public LocodeUi Locode { get; set; } 
    
    /// <summary>
    /// The default styles to use for rendering API Explorer Forms 
    /// </summary>
    public ExplorerUi Explorer { get; set; }
    
    /// <summary>
    /// The default styles to use for rendering Admin UI 
    /// </summary>
    public AdminUi Admin { get; set; }
    
    /// <summary>
    /// The default formats for displaying info
    /// </summary>
    public ApiFormat DefaultFormats { get; set; }
    
    /// <summary>
    /// Custom User-Defined Attributes
    /// </summary>
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ThemeInfo
{
    public string Form { get; set; }
    public ImageInfo ModelIcon { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class LocodeUi
{
    public ApiCss Css { get; set; }
    public AppTags Tags { get; set; }
    public int MaxFieldLength { get; set; }
    public int MaxNestedFields { get; set; }
    public int MaxNestedFieldLength { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ExplorerUi
{
    public ApiCss Css { get; set; }
    public AppTags Tags { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AdminUi
{
    /// <summary>
    /// Customize Admin Users FormLayout
    /// </summary>
    public ApiCss Css { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class ApiCss
{
    public string Form { get; set; }
    public string Fieldset { get; set; }
    public string Field { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class FieldCss
{
    public string Field { get; set; }
    public string Input { get; set; }
    public string Label { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AppTags
{
    public string Default { get; set; }
    public string Other { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetaAuthProvider : IMeta
{
    public string Name { get; set; }
    public string Label { get; set; }
    public string Type { get; set; }
    public NavItem NavItem { get; set; }
    public ImageInfo Icon { get; set; }
    public List<InputInfo> FormLayout { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataOperationType
{
    public MetadataType Request { get; set; }
    public MetadataType Response { get; set; }
    public List<string> Actions { get; set; }
    public bool? ReturnsVoid { get; set; }
    public string Method { get; set; }
    public MetadataTypeName ReturnType { get; set; }
    public List<MetadataRoute> Routes { get; set; }
    public MetadataTypeName DataModel { get; set; }
    public MetadataTypeName ViewModel { get; set; }
    public bool? RequiresAuth { get; set; }
    public bool? RequiresApiKey { get; set; }
    public List<string> RequiredRoles { get; set; }
    public List<string> RequiresAnyRole { get; set; }
    public List<string> RequiredPermissions { get; set; }
    public List<string> RequiresAnyPermission { get; set; }
    public List<string> Tags { get; set; }
    public ApiUiInfo Ui { get; set; }
}

public class ApiUiInfo : IMeta
{
    public ApiCss LocodeCss { get; set; } 
    public ApiCss ExplorerCss { get; set; } 
    public List<InputInfo> FormLayout { get; set; }
    public Dictionary<string, string> Meta { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataType : IMeta
{
    [IgnoreDataMember]
    public Type Type { get; set; }
    [IgnoreDataMember]
    public Dictionary<string, object> Items { get; set; }
    [IgnoreDataMember]
    public MetadataOperationType RequestType { get; set; }

    [IgnoreDataMember]
    public bool IsClass => Type?.IsClass ?? !(IsEnum == true || IsInterface == true);
    
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string[] GenericArgs { get; set; }
    public MetadataTypeName Inherits { get; set; }
    public MetadataTypeName[] Implements { get; set; }
    public string DisplayType { get; set; }
    public string Description { get; set; }
    public string Notes { get; set; }
    public ImageInfo Icon { get; set; } 
    public bool? IsNested { get; set; }
    public bool? IsEnum { get; set; }
    public bool? IsEnumInt { get; set; }
    public bool? IsInterface { get; set; }
    public bool? IsAbstract { get; set; }
    public bool? IsGenericTypeDef { get; set; }
    public MetadataDataContract DataContract { get; set; }

    public List<MetadataPropertyType> Properties { get; set; }

    public List<MetadataAttribute> Attributes { get; set; }

    public List<MetadataTypeName> InnerTypes { get; set; }

    public List<string> EnumNames { get; set; }
    public List<string> EnumValues { get; set; }
    public List<string> EnumMemberValues { get; set; }
    public List<string> EnumDescriptions { get; set; }

    public Dictionary<string, string> Meta { get; set; }

    public string GetFullName() => Namespace + "." + Name;

    protected bool Equals(MetadataType other)
    {
        return Name == other.Name && Namespace == other.Namespace;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MetadataType) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
        }
    }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataTypeName
{
    [IgnoreDataMember]
    public Type Type { get; set; }
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string[] GenericArgs { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataRoute
{
    [IgnoreDataMember]
    public RouteAttribute RouteAttribute { get; set; }
    public string Path { get; set; }
    public string Verbs { get; set; }
    public string Notes { get; set; }
    public string Summary { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataDataContract
{
    public string Name { get; set; }
    public string Namespace { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataDataMember
{
    public string Name { get; set; }
    public int? Order { get; set; }
    public bool? IsRequired { get; set; }
    public bool? EmitDefaultValue { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataPropertyType
{
    [IgnoreDataMember]
    public PropertyInfo PropertyInfo { get; set; }
    [IgnoreDataMember]
    public Type PropertyType { get; set; }
    [IgnoreDataMember]
    public Dictionary<string, object> Items { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Namespace { get; set; }
    public bool? IsValueType { get; set; }
    public bool? IsEnum { get; set; }
    public bool? IsPrimaryKey { get; set; }
    public string[] GenericArgs { get; set; }
    public string Value { get; set; }
    public string Description { get; set; }
    public MetadataDataMember DataMember { get; set; }
    public bool? ReadOnly { get; set; }

    public string ParamType { get; set; }
    public string DisplayType { get; set; }
    public bool? IsRequired { get; set; }
    public string[] AllowableValues { get; set; }
    public int? AllowableMin { get; set; }
    public int? AllowableMax { get; set; }

    public List<MetadataAttribute> Attributes { get; set; }
    
    public string UploadTo { get; set; }
    public InputInfo Input { get; set; }
    public FormatInfo Format { get; set; }
    public RefInfo Ref { get; set; }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class MetadataAttribute
{
    [IgnoreDataMember]
    public Attribute Attribute { get; set; }
    public string Name { get; set; }
    public List<MetadataPropertyType> ConstructorArgs { get; set; }
    public List<MetadataPropertyType> Args { get; set; }
}

[DataContract]
public class ApiDescription
{
    [DataMember(Order = 1)]
    public string Name { get; set; }
    [DataMember(Order = 2)]
    public string Returns { get; set; }
    [DataMember(Order = 3)]
    public string Description { get; set; }
    [DataMember(Order = 4)]
    public string Notes { get; set; }
    [DataMember(Order = 5)]
    public Dictionary<string, string> Links { get; set; }
}


public static class MetadataTypeExtensions
{
    public static bool InheritsAny(this MetadataType type, params string[] typeNames) =>
        type.Inherits != null && typeNames.Contains(type.Inherits.Name);

    public static bool InheritsAny(this MetadataType type, HashSet<string> typeNames) =>
        type.Inherits != null && typeNames.Contains(type.Inherits.Name);

    public static bool ImplementsAny(this MetadataType type, params string[] typeNames) =>
        type.Implements != null && type.Implements.Any(i => typeNames.Contains(i.Name));

    public static bool ImplementsAny(this MetadataType type, HashSet<string> typeNames) =>
        type.Implements != null && type.Implements.Any(i => typeNames.Contains(i.Name));

    public static bool ReferencesAny(this MetadataOperationType op, params string[] typeNames) =>
        (op.Request.Inherits != null && (typeNames.Contains(op.Request.Inherits.Name) ||
                                         op.Request.Inherits.GenericArgs?.Length > 0 &&
                                         op.Request.Inherits.GenericArgs.Any(typeNames.Contains))) 
        ||
        (op.Response != null && (typeNames.Contains(op.Response.Name) ||
                                 op.Response.GenericArgs?.Length > 0 &&
                                 op.Response.GenericArgs.Any(typeNames.Contains))) 
        ||
        (op.Request.Implements != null && op.Request.Implements.Any(i =>
             i.GenericArgs?.Length > 0 && i.GenericArgs.Any(typeNames.Contains))) 
        ||
        (op.Response?.Inherits != null && (typeNames.Contains(op.Response.Inherits.Name) ||
                                           op.Response.Inherits.GenericArgs?.Length > 0 &&
                                           op.Response.Inherits.GenericArgs.Any(typeNames.Contains)));

    public static List<MetadataRoute> GetRoutes(this List<MetadataOperationType> operations, MetadataType type) =>
        operations.FirstOrDefault(x => ReferenceEquals(x.Request, type))?.Routes ?? operations.GetRoutes(type.Name);
    public static List<MetadataRoute> GetRoutes(this List<MetadataOperationType> operations, string typeName)
    {
        return operations.FirstOrDefault(x => x.Request.Name == typeName)?.Routes;
    }

    public static string ToScriptSignature(this ScriptMethodType method)
    {
        var paramCount = method.ParamNames?.Length ?? 0;
        var firstParam = method.ParamNames?.Length > 0 ? method.ParamNames[0] : null;
        var ret = method.ReturnType != null && method.ReturnType != "StopExecution" ? " -> " + method.ReturnType : "";
        var sig = paramCount == 0
            ? $"{method.Name}{ret}"
            : paramCount == 1
                ? $"{firstParam} |> {method.Name}{ret}"
                : $"{firstParam} |> {method.Name}(" + string.Join(", ", method.ParamNames?.Skip(1) ?? new string[0]) + $"){ret}";
        return sig;
    }
    
    public static List<MetadataOperationType> GetOperationsByTags(this MetadataTypes types, string[] tags) => 
        types.Operations.Where(x => x.Tags != null && x.Tags.Any(t => Array.IndexOf(tags, t) >= 0)).ToList();


    private static readonly char[] SystemTypeChars = { '<', '>', '+' };
    public static bool IsSystemOrServiceStackType(this MetadataTypeName metaRef)
    {
        if (metaRef.Namespace == null)
            return false;
        return metaRef.Namespace.StartsWith("System") || 
               metaRef.Namespace.StartsWith("ServiceStack") ||
               metaRef.Name.IndexOfAny(SystemTypeChars) >= 0;
    }

    public static MetadataOperationType FindAutoQueryReturning(this MetadataTypes types, string dataModel)
    {
        return types.Operations.FirstOrDefault(x => x.DataModel?.Name == dataModel && Crud.IsQuery(x.Request))
            ?? types.Operations.FirstOrDefault(x => x.ViewModel?.Name == dataModel && Crud.IsQuery(x.Request));
    }
}

[Exclude(Feature.Soap | Feature.ApiExplorer)]
public class AppMetadataCache
{
    public Dictionary<string, MetadataOperationType> OperationsMap { get; set; }
    public Dictionary<string, MetadataType> TypesMap { get; set; }

    public AppMetadataCache(Dictionary<string, MetadataOperationType> operationsMap,
        Dictionary<string, MetadataType> typesMap)
    {
        TypesMap = typesMap;
        OperationsMap = operationsMap;
    }
}

public static class AppMetadataUtils
{
    public static AppMetadataCache GetCache(this AppMetadata app)
    {
        if (app.Cache == null)
        {
            var allOps = new Dictionary<string, MetadataOperationType>();
            foreach (var op in app.Api.Operations)
            {
                allOps[op.Request.Name] = op;
            }
            
            var allTypes = new Dictionary<string, MetadataType>();
            foreach (var type in app.Api.Types)
            {
                allTypes[type.Name] = type;
                if (type.Namespace != null)
                    allTypes[type.Namespace + "." + type.Name] = type;
            }
            // Some times Types only appear in Response Types
            foreach (var op in app.Api.Operations)
            {
                allTypes[op.Request.Name] = op.Request;
                if (op.Request.Namespace != null)
                    allTypes[op.Request.Namespace + "." + op.Request.Name] = op.Request;

                var type = op.Response;
                if (type == null || allTypes.ContainsKey(type.Name))
                    continue;
                
                allTypes[type.Name] = type;
                if (type.Namespace != null)
                    allTypes[type.Namespace + "." + type.Name] = type;
            }
            app.Cache = new AppMetadataCache(allOps, allTypes);
        }
        return app.Cache;
    }
    public static MetadataOperationType GetOperation(this AppMetadata app, string name) => 
        app.GetCache().OperationsMap.TryGetValue(name, out var op) ? op : null;

    public static MetadataType GetType(this AppMetadata app, Type type) => 
        app.GetType(type.Namespace, type.Name);

    public static MetadataType GetType(this AppMetadata app, string name) => 
        app.GetCache().TypesMap.TryGetValue(name, out var type) ? type : null;

    public static MetadataType GetType(this AppMetadata app, MetadataTypeName typeRef) =>
        typeRef == null ? null : app.GetType(typeRef.Namespace, typeRef.Name);

    public static MetadataType GetType(this AppMetadata app, string @namespace, string name)
    {
        var map = app.GetCache().TypesMap;
        if (map.TryGetValue(@namespace + "." + name, out var type))
            return type;
        if (map.TryGetValue(name, out type))
            return type;
        return null;
    }

    public static void EachOperation(this AppMetadata app, Action<MetadataOperationType> configure) 
    {
        foreach (var entry in app.GetCache().OperationsMap)
        {
            configure(entry.Value);
        }
    }

    public static void EachOperation(this AppMetadata app, Action<MetadataOperationType> configure, Predicate<MetadataOperationType> where) 
    {
        foreach (var entry in app.GetCache().OperationsMap)
        {
            if (!where(entry.Value))
                continue;
            configure(entry.Value);
        }
    }

    public static void EachType(this AppMetadata app, Action<MetadataType> configure) 
    {
        foreach (var entry in app.GetCache().TypesMap)
        {
            configure(entry.Value);
        }
    }

    public static void EachType(this AppMetadata app, Action<MetadataType> configure, Predicate<MetadataType> where) 
    {
        foreach (var entry in app.GetCache().TypesMap)
        {
            if (!where(entry.Value))
                continue;
            configure(entry.Value);
        }
    }

    public static async Task<AppMetadata> GetAppMetadataAsync(this string baseUrl)
    {
        string appResponseJson = null;
        try
        {
            appResponseJson = await baseUrl.CombineWith("/metadata/app.json")
                .GetJsonFromUrlAsync();
        
            if (!appResponseJson.Trim().StartsWith("{"))
                throw new Exception("Not a remote ServiceStack Instance");
        }
        catch (Exception appEx)
        {
            string ssMetadata;
            try
            {
                ssMetadata = await baseUrl.CombineWith("/metadata")
                    .GetStringFromUrlAsync(requestFilter:req => req.With(c => c.UserAgent = "ServiceStack"));
            }
            catch (Exception ssEx)
            {
                throw new Exception("Not a remote ServiceStack Instance", ssEx);
            }

            if (ssMetadata.IndexOf("https://servicestack.net", StringComparison.Ordinal) == -1)
                throw new Exception("Not a remote ServiceStack Instance");

            throw new Exception("ServiceStack Instance v5.10 or higher required", appEx);
        }

        AppMetadata appMetadata;
        try
        {
            appMetadata = appResponseJson.FromJson<AppMetadata>();
        }
        catch (Exception e)
        {
            throw new Exception("Could not read AppMetadata, try upgrading this App or remote ServiceStack Instance", e);
        }

        return appMetadata;
    }

    public static FieldCss ToCss(this FieldCssAttribute attr) => attr == null
        ? null
        : new FieldCss { Field = attr.Field, Input = attr.Input, Label = attr.Label };

    static bool? NullIfFalse(this bool value) => value ? true : (bool?)null;
    static int? NullIfMinValue(this int value) => value != int.MinValue ? value : (int?)null;

    public static InputInfo ToInput(this InputAttributeBase input, Action<InputInfo> configure = null)
    {
        var ret = new InputInfo
        {
            Type = input.Type,
            Value = input.Value,
            Placeholder = input.Placeholder,
            Help = input.Help,
            Label = input.Label,
            Size = input.Size,
            Pattern = input.Pattern,
            ReadOnly = input.ReadOnly.NullIfFalse(),
            Disabled = input.Disabled.NullIfFalse(),
            Required = input.Required.NullIfFalse(),
            Min = input.Min,
            Max = input.Max,
            Step = input.Step,
            MinLength = input.MinLength.NullIfMinValue(),
            MaxLength = input.MaxLength.NullIfMinValue(),
            Accept = input.Accept,
            Capture = input.Capture,
            Multiple = input.Multiple.NullIfFalse(),
            AllowableValues = input.AllowableValues ?? Input.GetEnumValues(input.AllowableValuesEnum),
            Options = input.Options,
            Ignore = input.Ignore.NullIfFalse(),
        };

        if (ClientConfig.EvalExpression != null)
        {
            if (input.EvalAllowableValues != null)
                ret.AllowableValues = ClientConfig.EvalExpression(input.EvalAllowableValues).ConvertTo<string[]>();
            if (input.EvalAllowableEntries != null)
                ret.AllowableEntries = ClientConfig.EvalExpression(input.EvalAllowableEntries).ConvertTo<KeyValuePair<string, string>[]>();
        }
        
        configure?.Invoke(ret);
        return ret;
    }

    public static FormatInfo ToFormat(this FormatAttribute attr) => attr == null
        ? null
        : new FormatInfo { Method = attr.Method, Options = attr.Options, Locale = attr.Locale };

    static string LowerFirst(this string s) => char.ToLower(s[0]) + s.Substring(1);
    
    public static FormatInfo ToFormat(this Intl attr)
    {
        if (attr == null) 
            return null; 
        
        var to = new FormatInfo
        {
            Method = attr.Type switch {
                IntlFormat.Number => "Intl.NumberFormat",
                IntlFormat.DateTime => "Intl.DateTimeFormat",
                IntlFormat.RelativeTime => "Intl.RelativeTimeFormat",
                _ => throw new NotSupportedException($"{attr.Type}")
            }, 
            Options = attr.Options, 
            Locale = attr.Locale,
        };

        if (to.Options == null)
        {
            var args = new Dictionary<string, object>();
            
            if (attr.Type == IntlFormat.Number)
            {
                var style = attr.Number;
                if (!string.IsNullOrEmpty(attr.Currency))
                {
                    args["style"] = "currency";
                    args["currency"] = attr.Currency;
                }
                if (!string.IsNullOrEmpty(attr.Unit))
                {
                    args["style"] = "unit";
                    args["unit"] = attr.Unit;
                }
                if (style != NumberStyle.Undefined)
                    args["style"] = style.ToString().LowerFirst();
                
                if (attr.Notation != Notation.Undefined)
                    args[nameof(attr.Notation).LowerFirst()] = attr.Notation.ToString().LowerFirst();
                if (attr.RoundingMode != RoundingMode.Undefined)
                    args[nameof(attr.RoundingMode).LowerFirst()] = attr.RoundingMode.ToString().LowerFirst();
                if (attr.UnitDisplay != UnitDisplay.Undefined)
                    args[nameof(attr.UnitDisplay).LowerFirst()] = attr.UnitDisplay.ToString().LowerFirst();
                if (attr.SignDisplay != SignDisplay.Undefined)
                    args[nameof(attr.SignDisplay).LowerFirst()] = attr.SignDisplay.ToString().LowerFirst();
                if (attr.CurrencyDisplay != CurrencyDisplay.Undefined)
                    args[nameof(attr.CurrencyDisplay).LowerFirst()] = attr.CurrencyDisplay.ToString().LowerFirst();
                if (attr.CurrencySign != CurrencySign.Undefined)
                    args[nameof(attr.CurrencySign).LowerFirst()] = attr.CurrencySign.ToString().LowerFirst();

                if (attr.MinimumIntegerDigits >= 0)
                    args[nameof(attr.MinimumIntegerDigits).LowerFirst()] = attr.MinimumIntegerDigits;
                if (attr.MinimumFractionDigits >= 0)
                    args[nameof(attr.MinimumFractionDigits).LowerFirst()] = attr.MinimumFractionDigits;
                if (attr.MaximumFractionDigits >= 0)
                    args[nameof(attr.MaximumFractionDigits).LowerFirst()] = attr.MaximumFractionDigits;
                if (attr.MinimumSignificantDigits >= 0)
                    args[nameof(attr.MinimumSignificantDigits).LowerFirst()] = attr.MinimumSignificantDigits;
                if (attr.MaximumSignificantDigits >= 0)
                    args[nameof(attr.MaximumSignificantDigits).LowerFirst()] = attr.MaximumSignificantDigits;
            }
            else if (attr.Type == IntlFormat.DateTime)
            {
                if (attr.Date != DateStyle.Undefined)
                    args["dateStyle"] = attr.Date.ToString().LowerFirst();
                if (attr.Time != TimeStyle.Undefined)
                    args["timeStyle"] = attr.Time.ToString().LowerFirst();

                void AddDateText(Dictionary<string, object> args, string name, DateText value) {
                    if (value != DateText.Undefined)
                        args[name.LowerFirst()] = value.ToString().ToLower();
                }
                void AddDatePart(Dictionary<string, object> args, string name, DatePart value) {
                    if (value != DatePart.Undefined)
                        args[name.LowerFirst()] = value == DatePart.Digits2 ? "2-digit" : value.ToString().ToLower();
                }
                void AddDateMonth(Dictionary<string, object> args, string name, DateMonth value) {
                    if (value != DateMonth.Undefined)
                        args[name.LowerFirst()] = value == DateMonth.Digits2 ? "2-digit" : value.ToString().ToLower();
                }
                
                AddDateText(args, nameof(attr.Weekday), attr.Weekday);
                AddDateText(args, nameof(attr.Era), attr.Era);
                AddDatePart(args, nameof(attr.Year), attr.Year);
                AddDateMonth(args, nameof(attr.Month), attr.Month);
                AddDatePart(args, nameof(attr.Day), attr.Day);
                AddDatePart(args, nameof(attr.Hour), attr.Hour);
                AddDatePart(args, nameof(attr.Minute), attr.Minute);
                AddDatePart(args, nameof(attr.Second), attr.Second);
                AddDateText(args, nameof(attr.TimeZoneName), attr.TimeZoneName);

                if (attr.Hour12)
                    args[nameof(attr.Hour12).LowerFirst()] = attr.Hour12;
            }
            else if (attr.Type == IntlFormat.RelativeTime)
            {
                if (attr.RelativeTime != RelativeTimeStyle.Undefined)
                    args["style"] = attr.RelativeTime.ToString().LowerFirst();
                if (attr.Numeric != Numeric.Undefined)
                    args[nameof(attr.Numeric).LowerFirst()] = attr.Numeric.ToString().LowerFirst();
            }
            else throw new NotSupportedException(attr.Type.ToString());

            if (args.Count > 0)
            {
                var sb = StringBuilderCache.Allocate();
                foreach (var entry in args)
                {
                    if (sb.Length > 0)
                        sb.Append(",");
                    sb.Append(entry.Key);
                    sb.Append(':');
                    sb.Append(entry.Value switch {
                        string s => $"'{s}'",
                        int i => i.ToString(),
                        bool b => b.ToString().ToLower(),
                        _ => throw new NotSupportedException($"{entry.Key}:{entry.Value}")
                    });
                }
                sb.Insert(0, "{");
                sb.Append("}");
                to.Options = StringBuilderCache.ReturnAndFree(sb);
            }
        }
        
        return to;
    }

    public static MetadataPropertyType Property(this MetadataType type, string name) =>
        type.Properties?.FirstOrDefault(x => x.Name == name);
   
    public static MetadataPropertyType RequiredProperty(this MetadataType type, string name) =>
        type.Properties?.FirstOrDefault(x => x.Name == name) ?? throw new Exception($"{type.Name} does not contain property ${name}");

    public static void Property(this MetadataType type, string name, Action<MetadataPropertyType> configure)
    {
        var prop = type.Properties?.FirstOrDefault(x => x.Name == name);
        if (prop != null) configure(prop);
    }
    
    /// <summary>
    /// Reorder where the DB Column appears in Type (changes API &amp; UI ordering)
    /// </summary>
    public static MetadataPropertyType ReorderProperty(this MetadataType type, string name, string before = null, string after = null)
    {
        var prop = type.Property(name);
        if (prop == null) 
            return null;
        var beforeProp = before != null
            ? type.Properties.FirstOrDefault(x => x.Name.Equals(before, StringComparison.OrdinalIgnoreCase))
            : null;
        if (beforeProp != null)
            return type.ReorderProperty(name, Math.Max(0, type.Properties.IndexOf(beforeProp)));
        var afterProp = after != null
            ? type.Properties.FirstOrDefault(x => x.Name.Equals(after, StringComparison.OrdinalIgnoreCase))
            : null;
        if (afterProp != null)
            return type.ReorderProperty(name, Math.Max(0, type.Properties.IndexOf(afterProp) + 1));
        return prop;
    }

    /// <summary>
    /// Reorder where the DB Column appears in Type (changes API &amp; UI ordering)
    /// </summary>
    public static MetadataPropertyType ReorderProperty(this MetadataType type, string name, int index)
    {
        var prop = type.Property(name);
        if (prop == null)
            return null;
        type.Properties.Remove(prop);
        type.Properties.Insert(index, prop);
        var order = 1;
        foreach (var p in type.Properties)
        {
            if (p.DataMember?.Order != null)
                p.DataMember.Order = order++;
        }
        return prop;
    }

    /// <summary>
    /// Apply custom lambda to each matching property
    /// </summary>
    public static void EachProperty(this MetadataType type, Func<MetadataPropertyType,bool> where, Action<MetadataPropertyType> configure)
    {
        if (type.Properties == null) 
            return;
        foreach (var prop in type.Properties.Where(where))
        {
            configure(prop);
        }
    }

    /// <summary>
    /// Omit properties that match filter from inclusion in code-gen type
    /// </summary>
    public static void RemoveProperty(this MetadataType type, Predicate<MetadataPropertyType> where) => 
        type.Properties?.RemoveAll(where);

    /// <summary>
    /// Omit property from inclusion in code-gen type
    /// </summary>
    public static void RemoveProperty(this MetadataType type, string name)
    {
        if (name != null) 
            type.Properties?.RemoveAll(x => x.Name == name);
    }
    public static bool IsSystemType(this MetadataPropertyType prop) =>
        prop.PropertyType?.Namespace?.StartsWith("System") == true ||
        prop.Namespace?.StartsWith("System") == true;

    public static string GetSerializedAlias(this MetadataPropertyType prop) =>
        prop.DataMember?.Name?.SafeVarName();

    public static MetadataPropertyType GetPrimaryKey(this List<MetadataPropertyType> props) =>
        props.FirstOrDefault(c => c.IsPrimaryKey == true);

    public static object GetId<T>(this MetadataType type, T model) => type.Properties.GetId<T>(model);
    public static object GetId<T>(this List<MetadataPropertyType> props, T model)
    {
        var pk = props.GetPrimaryKey();
        if (pk == null)
            return null;
        return pk.GetValue<T>(model);
    }

    public static object GetValue<T>(this MetadataPropertyType prop, T model)
    {
        var pi = TypeConfig<T>.Properties.FirstOrDefault(x => x.Name.EqualsIgnoreCase(prop.Name));
        var value = pi!.GetValue(model);
        return value;
    }

    public static Type GetResponseType(this Type requestType)
    {
        var returnMarker = requestType.GetTypeWithGenericInterfaceOf(typeof(IReturn<>));
        return returnMarker?.FirstGenericArg();
    }

    public static MetadataType ToMetadataType(this Type type)
    {
        var ret = new MetadataType
        {
            Type = type,
            Name = type.Name,
            Namespace = type.Namespace,
            GenericArgs = type.IsGenericType ? type.GetGenericArguments().Select(x => x.GetOperationName()).ToArray() : null,
            Properties = ToProperties(type, x => ToMetadataPropertyType(x)),
            IsNested = type.IsNested,
            IsEnum = type.IsEnum,
            IsEnumInt = (JsConfig.TreatEnumAsInteger || type.IsEnumFlags()),
            IsInterface = type.IsInterface,
            IsAbstract = type.IsAbstract,
            IsGenericTypeDef = type.IsGenericTypeDefinition.NullIfFalse(),
        };
        return ret;
    }

    public static List<MetadataPropertyType> ToProperties(Type type, Func<PropertyInfo, MetadataPropertyType> toProperty, HashSet<Type> exportTypes = null)
    {
        var props = (!type.IsUserType() &&
                     !type.IsInterface &&
                     !type.IsTuple() &&
                     !(exportTypes?.ContainsMatch(type) == true && JsConfig.TreatValueAsRefTypes.ContainsMatch(type)))
            || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
            ? null
            : type.GetInstancePublicProperties().Select(x => toProperty(x))
                .ToList().PopulatePrimaryKey();

        return props == null || props.Count == 0 ? null : props;
    }

    public static List<MetadataPropertyType> PopulatePrimaryKey(this List<MetadataPropertyType> props)
    {
        var hasPkAttr =
            props.FirstOrDefault(p => p.PropertyInfo?.HasAttributeCached<PrimaryKeyAttribute>() == true) ??
            props.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) ??
            props.FirstOrDefault(p => p.PropertyInfo?.HasAttributeCached<AutoIncrementAttribute>() == true) ??
            props.FirstOrDefault(p => p.PropertyInfo?.HasAttributeCached<AutoIdAttribute>() == true);

        if (hasPkAttr != null)
        {
            hasPkAttr.IsPrimaryKey = true;
        }

        return props;
    }

    public static List<MetadataPropertyType> GetAllMetadataProperties(this Type type) =>
        type.GetAllProperties().Select(x => x.ToMetadataPropertyType()).ToList();

    public static List<MetadataPropertyType> GetAllProperties(this AppMetadata api, string typeName) =>
        api.GetAllProperties(api.GetType(typeName));
    public static List<MetadataPropertyType> GetAllProperties(this AppMetadata api, string @namespace, string typeName) =>
        api.GetAllProperties(api.GetType(@namespace, typeName));

    public static List<MetadataPropertyType> GetAllProperties(this AppMetadata api, MetadataType metaType)
    {
        var to = new List<MetadataPropertyType>();

        while (metaType != null)
        {
            foreach (var prop in metaType.Properties.OrEmpty())
            {
                if (to.All(x => x.Name != prop.Name))
                    to.Add(prop);
            }
            metaType = metaType.Inherits != null ? api.GetType(metaType.Inherits) : null;
        }

        return to;
    }

    public static PropertyInfo[] GetInstancePublicProperties(this Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OnlySerializableProperties(type)
            .Where(t =>
                t.GetIndexParameters().Length == 0 && // ignore indexed properties
                !t.HasAttribute<ExcludeMetadataAttribute>())
            .ToArray();
    }

    public static RefInfo CreateRefModel(this AppMetadata meta, string model)
    {
        var refType = meta.GetType(model);
        var pk = refType?.Properties?.FirstOrDefault(x => x.IsPrimaryKey == true);
        if (pk == null)
            return null;

        var firstStringProp = pk.Type != nameof(String) 
            ? refType.Properties.FirstOrDefault(x => x.IsPrimaryKey != true && x.Type == nameof(String))
            : null;
        var refInfo = new RefInfo
        {
            Model = refType.Name,
            RefId = pk.Name,
            RefLabel = firstStringProp?.Name,
        };
        return refInfo;
    }

    public static RefInfo CreateRef(this AppMetadata meta, MetadataType type, MetadataPropertyType p)
    {
        if (p?.Ref != null)
            return p.Ref;
        
        if (p?.PropertyInfo == null)
        {
            // Attempt to use naming convention to create ref
            if (p?.Name.Length > 2 && p.Name.EndsWith("Id"))
            {
                var model = p.Name.Substring(0, p.Name.Length - 2);
                return meta.CreateRefModel(model);
            }
            return null;
        }
        
        var allAttrs = p.PropertyInfo.AllAttributes();
        var refAttr = allAttrs.OfType<RefAttribute>().FirstOrDefault();
        if (refAttr != null)
        {
            if (!refAttr.None)
            {
                var model = refAttr.Model ?? refAttr.ModelType?.Name;
                if (model == null)
                    return null;
                return new RefInfo {
                    ModelType = refAttr.ModelType,
                    QueryType = refAttr.QueryType,
                    QueryApi = refAttr.QueryType?.Name,
                    Model = model, 
                    SelfId = refAttr.SelfId, 
                    RefId = refAttr.RefId, 
                    RefLabel = refAttr.RefLabel,
                };
            }
            return null;
        }

        var refsAttr = allAttrs.OfType<ReferencesAttribute>().FirstOrDefault();
        if (refsAttr != null)
            return meta.CreateRefModel(refsAttr.Type.Name);

        var fkAttr = allAttrs.OfType<ForeignKeyAttribute>().FirstOrDefault();
        if (fkAttr != null)
            return meta.CreateRefModel(fkAttr.Type.Name);

        if (allAttrs.OfType<ReferenceAttribute>().FirstOrDefault() != null)
        {
            var pt = p.PropertyInfo.PropertyType;
            var typePk = type.Properties?.FirstOrDefault(prop => prop.IsPrimaryKey == true);
            if (pt.HasInterface(typeof(IEnumerable)))
            {
                if (typePk == null)
                    return null;
                    
                var refType = pt.GetCollectionType();
                var refMetaType = meta.GetType(refType.Name);
                if (refMetaType == null)
                    return null;
                    
                var fkId = type.Name + "Id";
                var fkProp = refMetaType.Properties?.FirstOrDefault(prop => prop.Name == fkId);
                    
                return fkProp == null ? null : new RefInfo
                {
                    Model = refType.Name,
                    SelfId = typePk.Name,
                    RefId = fkProp.Name,
                };
            }
            else
            {
                var selfRefId = pt.Name + "Id";
                var selfRef = type.Properties?.FirstOrDefault(prop => prop.Name == selfRefId);
                if (selfRef == null)
                    return meta.CreateRefModel(pt.Name);
                var refMetaType = meta.GetType(pt.Name);
                var fkProp = refMetaType?.Properties?.FirstOrDefault(prop => prop.IsPrimaryKey == true);
                    
                return fkProp == null ? null : new RefInfo
                {
                    Model = pt.Name,
                    SelfId = selfRefId,
                    RefId = fkProp.Name,
                };
            }
        }
        return null;
    }

    internal static bool ContainsMatch(this HashSet<Type> types, Type target)
    {
        if (types == null)
            return false;

        if (types.Contains(target))
            return true;

        return types.Any(x => x.IsGenericTypeDefinition && target.IsOrHasGenericInterfaceTypeOf(x));
    }

    public static PropertyInfo GetPrimaryKey(this PropertyInfo[] props)
    {
        var hasPkAttr =
            props.FirstOrDefault(p => p?.HasAttributeCached<PrimaryKeyAttribute>() == true) ??
            props.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) ??
            props.FirstOrDefault(p => p?.HasAttributeCached<AutoIncrementAttribute>() == true) ??
            props.FirstOrDefault(p => p?.HasAttributeCached<AutoIdAttribute>() == true);

        if (hasPkAttr != null)
        {
            return hasPkAttr;
        }
        return null;
    }

    public static RefInfo CreateRefModel(this Type refType)
    {
        var props = refType.GetAllProperties();
        var pk = props.GetPrimaryKey();
        if (pk == null)
            return null;

        var firstStringProp = pk.PropertyType != typeof(string)
            ? props.FirstOrDefault(x => x != pk && x.PropertyType == typeof(string))
            : null;
        var refInfo = new RefInfo
        {
            ModelType = refType,
            Model = refType.Name,
            RefId = pk.Name,
            RefLabel = firstStringProp?.Name,
        };
        return refInfo;
    }

    /// <summary>
    /// Best effort to create RefInfo from Reflection alone
    /// </summary>
    public static RefInfo CreateRef(this MetadataPropertyType p)
    {
        if (p?.Ref != null)
            return p.Ref;
        
        if (p?.PropertyInfo == null)
            return null;
        
        var allAttrs = p.PropertyInfo.AllAttributes();
        var refAttr = allAttrs.OfType<RefAttribute>().FirstOrDefault();
        if (refAttr != null)
        {
            if (!refAttr.None)
            {
                var model = refAttr.Model ?? refAttr.ModelType?.Name;
                if (model == null)
                    return null;
                return new RefInfo {
                    ModelType = refAttr.ModelType,
                    QueryType = refAttr.QueryType,
                    QueryApi = refAttr.QueryType?.Name,
                    Model = model, 
                    SelfId = refAttr.SelfId, 
                    RefId = refAttr.RefId, 
                    RefLabel = refAttr.RefLabel,
                };
            }
            return null;
        }
        if (!ClientConfig.ImplicitRefInfo)
            return null;
        
        var refsAttr = allAttrs.OfType<ReferencesAttribute>().FirstOrDefault();
        if (refsAttr != null)
            return refsAttr.Type.CreateRefModel();
 
        var fkAttr = allAttrs.OfType<ForeignKeyAttribute>().FirstOrDefault();
        if (fkAttr != null)
            return fkAttr.Type.CreateRefModel();

        if (allAttrs.OfType<ReferenceAttribute>().FirstOrDefault() != null)
        {
            var pt = p.PropertyInfo.PropertyType;
            var props = p.PropertyInfo.DeclaringType?.GetAllProperties() ?? Array.Empty<PropertyInfo>();
            var typePk = props.GetPrimaryKey();
            if (pt.HasInterface(typeof(IEnumerable)))
            {
                if (typePk == null)
                    return null;

                var refType = pt.GetCollectionType();
                var refTypeProps = refType.GetAllProperties();
                    
                var fkId = refType.Name + "Id";
                var fkProp = refTypeProps.FirstOrDefault(prop => prop.Name == fkId);
                    
                return fkProp == null ? null : new RefInfo
                {
                    Model = refType.Name,
                    SelfId = typePk.Name,
                    RefId = fkProp.Name,
                };
            }
            else
            {
                var selfRefId = pt.Name + "Id";
                var selfRef = props.FirstOrDefault(prop => prop.Name == selfRefId);
                if (selfRef == null)
                    return pt.CreateRefModel();
                    
                var refMetaTypeProps = pt.GetAllProperties();
                var fkProp = refMetaTypeProps.GetPrimaryKey();
                return fkProp == null ? null : new RefInfo
                {
                    Model = pt.Name,
                    SelfId = selfRefId,
                    RefId = fkProp.Name,
                };
            }            
        }
        return null;
    }

    // Shared by NativeTypesMetadata.ToProperty
    public static MetadataPropertyType ToMetadataPropertyType(this PropertyInfo pi, object instance = null, Dictionary<string, object> ignoreValues = null,
        bool treatNonNullableRefTypesAsRequired = true)
    {
        var propType = pi.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
        var property = new MetadataPropertyType
        {
            PropertyInfo = pi,
            PropertyType = propType,
            Name = pi.Name,
            //Attributes = ToAttributes(pi.GetCustomAttributes(false)),
            Type = propType.GetMetadataPropertyType(),
            IsValueType = underlyingType.IsValueType.NullIfFalse(),
            IsEnum = underlyingType.IsEnum.NullIfFalse(),
            Namespace = propType.Namespace,
            DataMember = pi.GetDataMember().ToDataMember(),
            GenericArgs = propType.ToGenericArgs(),
            Description = pi.GetDescription(),
        };
        property.Ref = property.CreateRef();

        property.Format ??= pi.FirstAttribute<Intl>().ToFormat();
        property.Format ??= pi.FirstAttribute<FormatAttribute>().ToFormat();

        if (treatNonNullableRefTypesAsRequired)
        {
            var notNullRefType = pi.IsNotNullable();
            property.IsRequired = notNullRefType;
        }

        var apiMember = pi.FirstAttribute<ApiMemberAttribute>();
        if (apiMember != null)
        {
            if (apiMember.IsRequired)
                property.IsRequired = true;
            else if (apiMember.IsOptional)
                property.IsRequired = false;

            property.ParamType = apiMember.ParameterType;
            property.DisplayType = apiMember.DataType;
            property.Format ??= new FormatInfo { Method = apiMember.Format };
            property.Description = apiMember.Description;
        }

        var requiredProp = pi.FirstAttribute<RequiredAttribute>();
        if (requiredProp != null)
            property.IsRequired = true;

        var apiAllowableValues = pi.FirstAttribute<ApiAllowableValuesAttribute>();
        if (apiAllowableValues != null)
        {
            property.AllowableValues = apiAllowableValues.Values;
            property.AllowableMin = apiAllowableValues.Min;
            property.AllowableMax = apiAllowableValues.Max;
        }

        var inputAttr = pi.FirstAttribute<InputAttribute>();
        if (inputAttr != null)
            property.PopulateInput(inputAttr.ToInput(c => c.Id ??= pi.Name));

        var css = pi.FirstAttribute<FieldCssAttribute>().ToCss();
        if (css != null)
        {
            property.Input ??= new InputInfo();
            property.Input.Css = css;
        }
        property.UploadTo = pi.FirstAttribute<UploadToAttribute>()?.Location;

        if (property.Input?.Type == Input.Types.File)
        {
            property.Input.Multiple = (propType != typeof(string) && propType.HasInterface(typeof(IEnumerable))).NullIfFalse();
        }

        if (instance != null)
        {
            var ignoreValue = ignoreValues != null && ignoreValues.TryGetValue(pi.Name, out var oValue)
                ? oValue
                : propType.GetDefaultValue();
            property.Value = pi.PropertyStringValue(instance, ignoreValue);

            if (pi.GetSetMethod() == null) //ReadOnly is bool? to minimize serialization
                property.ReadOnly = true;
        }
        return property;
    }

    public static void PopulateInput(this MetadataPropertyType property, InputInfo input)
    {
        var pi = property.PropertyInfo;

        property.Input = input;
        input.Required ??= property.IsRequired;
        input.MinLength ??= property.AllowableMin;
        input.MaxLength ??= property.AllowableMax;

        if (pi.PropertyType.IsEnum && input.AllowableValues == null)
        {
            input.Type ??= Html.Input.Types.Select;
            if (Html.Input.GetEnumEntries(pi.PropertyType, out var entries))
                input.AllowableEntries = entries;
            else
                input.AllowableValues = entries.Select(x => x.Value).ToArray();
        }
    }

    public static string PropertyStringValue(this PropertyInfo pi, object instance, object ignoreIfValue = null)
    {
        try
        {
            var value = pi.GetValue(instance, null);
            if (value != null && !value.Equals(ignoreIfValue))
            {
                return PropertyValueAsString(pi, value);
            }
        }
        catch (Exception ex)
        {
            var log = LogManager.GetLogger(typeof(MetadataType));
            log.Warn($"Could not get value for property '{pi.PropertyType}.{pi.Name}'", ex);
        }

        return null;
    }

    public static string PropertyValueAsString(this PropertyInfo pi, object value)
    {
        if (pi.PropertyType.IsEnum)
            return pi.PropertyType.Name + "." + value;

        if (pi.PropertyType == typeof(Type))
        {
            var type = (Type)value;
            return $"typeof({type.FullName})";
        }

        var strValue = value as string;
        return strValue ?? value.ToJson();
    }

    public static MetadataDataMember ToDataMember(this DataMemberAttribute attr)
    {
        if (attr == null) return null;

        var metaAttr = new MetadataDataMember
        {
            Name = attr.Name,
            EmitDefaultValue = attr.EmitDefaultValue == false ? false : (bool?)null, //true by default
            Order = attr.Order >= 0 ? attr.Order : (int?)null,
            IsRequired = attr.IsRequired.NullIfFalse(),
        };

        return metaAttr;
    }

    public static string[] ToGenericArgs(this Type propType)
    {
        var genericArgs = propType.IsGenericType
            ? propType.GetGenericArguments().Select(x => x.ExpandTypeName()).ToArray()
            : null;
        
        if (genericArgs == null && typeof(IEnumerable).IsAssignableFrom(propType))
        {
            var elType = propType.GetCollectionType();
            return elType?.IsGenericType == true
                ? elType.GetGenericArguments().Select(x => x.ExpandTypeName()).ToArray()
                : null;
        }

        return genericArgs;
    }

    public static ImageInfo GetIcon(this Type type) => 
        X.Map(type.FirstAttribute<IconAttribute>() ?? type.GetCollectionType()?.FirstAttribute<IconAttribute>(),
            x => new ImageInfo { Svg = x.Svg, Uri = x.Uri, Cls = x.Cls, Alt = x.Alt });

    public static ImageInfo GetIcon(this MetadataType type) => type.Icon;
}
