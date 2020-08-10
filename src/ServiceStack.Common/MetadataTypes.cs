using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Script;

namespace ServiceStack
{
    public class MetadataTypesConfig
    {
        public MetadataTypesConfig(
            string baseUrl = null,
            bool makePartial = true,
            bool makeVirtual = true,
            bool addReturnMarker = true,
            bool convertDescriptionToComments = true,
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
            bool makePropertiesOptional = true,
            bool makeDataContractsExtensible = false,
            bool initializeCollections = true,
            int? addImplicitVersion = null)
        {
            BaseUrl = baseUrl;
            MakePartial = makePartial;
            MakeVirtual = makeVirtual;
            AddReturnMarker = addReturnMarker;
            AddDescriptionAsComments = convertDescriptionToComments;
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
        public List<string> TreatTypesAsStrings { get; set; }
        public bool ExportValueTypes { get; set; }

        public string GlobalNamespace { get; set; }
        public bool ExcludeNamespace { get; set; }

        public HashSet<Type> IgnoreTypes { get; set; }
        public HashSet<Type> ExportTypes { get; set; }
        public HashSet<Type> ExportAttributes { get; set; }
        public List<string> IgnoreTypesInNamespaces { get; set; }
    }

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

    public class AppMetadata : IMeta
    {
        public AppInfo App { get; set; }
        public Dictionary<string, string> ContentTypeFormats { get; set; }
        public PluginInfo Plugins { get; set; }
        public Dictionary<string,CustomPlugin> CustomPlugins { get; set; }
        public MetadataTypes Api { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public class PluginInfo : IMeta
    {
        public List<string> Loaded { get; set; }
        public AuthInfo Auth { get; set; }
        public AutoQueryInfo AutoQuery { get; set; }
        public ValidationInfo Validation { get; set; }
        public SharpPagesInfo SharpPages { get; set; }
        public RequestLogsInfo RequestLogs { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public class AuthInfo : IMeta
    {
        public bool? HasAuthSecret { get; set; }
        public bool? HasAuthRepository { get; set; }
        public bool? IncludesRoles { get; set; }
        public bool? IncludesOAuthTokens { get; set; }
        public string HtmlRedirect { get; set; }
        public List<MetaAuthProvider> AuthProviders { get; set; }
        public Dictionary<string,string[]> ServiceRoutes { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

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

    public class SharpPagesInfo : IMeta
    {
        public string ApiPath { get; set; }
        public string ScriptAdminRole { get; set; }
        public string MetadataDebugAdminRole { get; set; }
        public bool? MetadataDebug { get; set; }
        public bool? SpaFallback { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public class RequestLogsInfo : IMeta
    {
        public string[] RequiredRoles { get; set; }
        public string RequestLogger { get; set; }
        public Dictionary<string,string[]> ServiceRoutes { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    /// <summary>
    /// Generic template for adding metadata info about custom plugins  
    /// </summary>
    public class CustomPlugin : IMeta
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

    public class ScriptMethodType
    {
        public string Name { get; set; }
        public string[] ParamNames { get; set; }
        public string[] ParamTypes { get; set; }
        public string ReturnType { get; set; }
    }

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
        /// Custom User-Defined Attributes
        /// </summary>
        public Dictionary<string, string> Meta { get; set; }
    }

    public class MetaAuthProvider : IMeta
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public NavItem NavItem { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public class MetadataOperationType
    {
        public MetadataType Request { get; set; }
        public MetadataType Response { get; set; }
        public List<string> Actions { get; set; }
        public bool ReturnsVoid { get; set; }
        public MetadataTypeName ReturnType { get; set; }
        public List<MetadataRoute> Routes { get; set; }
        public MetadataTypeName DataModel { get; set; }
        public MetadataTypeName ViewModel { get; set; }
        public bool RequiresAuth { get; set; }
        public List<string> RequiredRoles { get; set; }
        public List<string> RequiresAnyRole { get; set; }
        public List<string> RequiredPermissions { get; set; }
        public List<string> RequiresAnyPermission { get; set; }
        public List<string> Tags { get; set; }
    }

    public class MetadataType : IMeta
    {
        [IgnoreDataMember]
        public Type Type { get; set; }
        [IgnoreDataMember]
        public Dictionary<string, object> Items { get; set; }
        [IgnoreDataMember]
        public MetadataOperationType RequestType { get; set; }
        // [IgnoreDataMember, Obsolete("Use MetadataOperationType.ReturnVoidMarker")]
        // public bool ReturnVoidMarker => RequestType?.ReturnVoidMarker == true;
        // [IgnoreDataMember, Obsolete("Use MetadataOperationType.ReturnMarkerTypeName")]
        // public MetadataTypeName ReturnMarkerTypeName => RequestType?.ReturnMarkerTypeName;
        
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string[] GenericArgs { get; set; }
        public MetadataTypeName Inherits { get; set; }
        public MetadataTypeName[] Implements { get; set; }
        public string DisplayType { get; set; }
        public string Description { get; set; }
        public bool? IsNested { get; set; }
        public bool? IsEnum { get; set; }
        public bool? IsEnumInt { get; set; }
        public bool? IsInterface { get; set; }
        public bool? IsAbstract { get; set; }
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
    }

    public class MetadataTypeName
    {
        [IgnoreDataMember]
        public Type Type { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string[] GenericArgs { get; set; }
    }

    public class MetadataRoute
    {
        [IgnoreDataMember]
        public RouteAttribute RouteAttribute { get; set; }
        public string Path { get; set; }
        public string Verbs { get; set; }
        public string Notes { get; set; }
        public string Summary { get; set; }
    }

    public class MetadataDataContract
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
    }

    public class MetadataDataMember
    {
        public string Name { get; set; }
        public int? Order { get; set; }
        public bool? IsRequired { get; set; }
        public bool? EmitDefaultValue { get; set; }
    }

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
        public bool? IsValueType { get; set; }
        public bool? IsSystemType { get; set; }
        public bool? IsEnum { get; set; }
        public bool? IsPrimaryKey { get; set; }
        public string TypeNamespace { get; set; }
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
    }

    public class MetadataAttribute
    {
        [IgnoreDataMember]
        public Attribute Attribute { get; set; }
        public string Name { get; set; }
        public List<MetadataPropertyType> ConstructorArgs { get; set; }
        public List<MetadataPropertyType> Args { get; set; }
    }

    public static class MetadataTypeExtensions
    {
        public static bool InheritsAny(this MetadataType type, params string[] typeNames) =>
            type.Inherits != null && typeNames.Contains(type.Inherits.Name);

        public static bool ImplementsAny(this MetadataType type, params string[] typeNames) =>
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
            var ret = method.ReturnType != null && method.ReturnType != nameof(StopExecution) ? " -> " + method.ReturnType : "";
            var sig = paramCount == 0
                ? $"{method.Name}{ret}"
                : paramCount == 1
                    ? $"{firstParam} |> {method.Name}{ret}"
                    : $"{firstParam} |> {method.Name}(" + string.Join(", ", method.ParamNames?.Skip(1) ?? new string[0]) + $"){ret}";
            return sig;
        }
        
        public static List<MetadataOperationType> GetOperationsByTags(this MetadataTypes types, string[] tags) => 
            types.Operations.Where(x => x.Tags != null && x.Tags.Any(t => Array.IndexOf(tags, t) >= 0)).ToList();

    }
}