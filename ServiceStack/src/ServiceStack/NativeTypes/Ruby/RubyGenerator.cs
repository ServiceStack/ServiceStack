using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Ruby;

public class RubyGenerator : ILangGenerator
{
    public Lang Lang => Lang.Ruby;
    public MetadataTypesConfig Config { get; }

    readonly NativeTypesFeature feature;
    public List<string> ConflictTypeNames = new();
    public List<MetadataType> AllTypes { get; set; }

    public RubyGenerator(MetadataTypesConfig config)
    {
        Config = config;
        feature = HostContext.GetPlugin<NativeTypesFeature>();
    }

    public static Func<IRequest,string> AddHeader { get; set; }

    public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }

    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

    public static HashSet<string> IgnoreAttributes { get; private set; } = new() {
        nameof(DataContractAttribute),
        nameof(DataMemberAttribute),
    };
    public static bool IgnoreAllAttributes
    {
        get => IgnoreAttributes == null;
        set => IgnoreAttributes = null;
    }

    public static List<string> DefaultImports = new() {
        "json",
        "servicestack",
    };

    /// <summary>
    /// The Ruby module of the ServiceStack Client Library that generated DTOs reference
    /// </summary>
    public static string LibraryModule { get; set; } = "ServiceStack";

    /// <summary>
    /// Built-in ServiceStack Types implemented in the servicestack gem which are
    /// referenced instead of being emitted in generated DTOs
    /// </summary>
    public static HashSet<string> LibraryTypes { get; set; } =
    [
        nameof(ResponseStatus),
        nameof(ResponseError),
        nameof(EmptyResponse),
        nameof(IdResponse),
        nameof(StringResponse),
        nameof(StringsResponse),
        nameof(AuditBase),
        nameof(QueryBase),
        "QueryDb",
        "QueryData",
        "QueryResponse",
        nameof(Authenticate),
        nameof(AuthenticateResponse),
        nameof(Register),
        nameof(RegisterResponse),
        nameof(ConvertSessionToToken),
        nameof(ConvertSessionToTokenResponse),
        nameof(GetAccessToken),
        nameof(GetAccessTokenResponse),
    ];

    /// <summary>
    /// Library Types that convert their generic results, e.g. QueryResponse.of(Booking)
    /// </summary>
    public static HashSet<string> GenericLibraryTypes { get; set; } = ["QueryResponse"];

    /// <summary>
    /// Library Types referenced by the generated DTOs, resolved in Init()
    /// </summary>
    public HashSet<string> UseLibraryTypes { get; set; } = new();

    /// <summary>
    /// Whether the Type Name refers to a Type implemented in the servicestack gem
    /// </summary>
    public bool IsLibraryType(string typeName) => UseLibraryTypes.Contains(typeName);

    /// <summary>
    /// Reference a Type implemented in the servicestack gem, e.g. ServiceStack::ResponseStatus
    /// </summary>
    public string LibraryType(string typeName) => LibraryModule + "::" + typeName;

    public static Dictionary<string, string> TypeAliases = new() {
        {"String", "String"},
        {"Boolean", "TrueClass"},
        {"DateTime", "DateTime"},
        {"DateOnly", "DateTime"},
        {"DateTimeOffset", "DateTime"},
        {"TimeSpan", "Time"},
        {"TimeOnly", "Time"},
        {"Guid", "String"},
        {"Char", "String"},
        {"Byte", "Integer"},
        {"Int16", "Integer"},
        {"Int32", "Integer"},
        {"Int64", "Integer"},
        {"UInt16", "Integer"},
        {"UInt32", "Integer"},
        {"UInt64", "Integer"},
        {"Single", "Float"},
        {"Double", "Float"},
        {"Decimal", "BigDecimal"},
        {"IntPtr", "Integer"},
        {"List", "Array"},
        {"Byte[]", "String"},
        {"Stream", "String"},
        {"HttpWebResponse", "String"},
        {"IDictionary", "Hash"},
        {"OrderedDictionary", "Hash"},
        {"Uri", "String"},
        {"Type", "String"},
    };

    internal static HashSet<string> typeAliasValues;

    public static Dictionary<string, string> ReturnTypeAliases = new() {
    };

    public static HashSet<string> KeyWords =
    [
        "BEGIN",
        "END",
        "__ENCODING__",
        "__END__",
        "__FILE__",
        "__LINE__",
        "alias",
        "and",
        "begin",
        "break",
        "case",
        "class",
        "def",
        "defined?",
        "do",
        "else",
        "elsif",
        "end",
        "ensure",
        "false",
        "for",
        "if",
        "in",
        "module",
        "next",
        "nil",
        "not",
        "or",
        "redo",
        "rescue",
        "retry",
        "return",
        "self",
        "super",
        "then",
        "true",
        "undef",
        "unless",
        "until",
        "when",
        "while",
        "yield"
    ];

    public static readonly Dictionary<string, string> DefaultValues = new() {
        {"Boolean", "false"},
        {"DateTime", "DateTime.new(1, 1, 1)"},
        {"DateOnly", "DateTime.new(1, 1, 1)"},
        {"DateTimeOffset", "DateTime.new(1, 1, 1)"},
        {"TimeSpan", "Time.new(0)"},
        {"TimeOnly", "Time.new(0)"},
        {"Byte", "0"},
        {"Int16", "0"},
        {"Int32", "0"},
        {"Int64", "0"},
        {"UInt16", "0"},
        {"UInt32", "0"},
        {"UInt64", "0"},
        {"Single", "0.0"},
        {"Double", "0.0"},
        {"Decimal", "BigDecimal('0')"},
        {"IntPtr", "0"},
        {"List", "[]"},
        {"Dictionary", "{}"},
    };

    public static bool GenerateServiceStackTypes => IgnoreTypeInfosFor.Count == 0;

    //In _builtInTypes servicestack library
    public static HashSet<string> IgnoreTypeInfosFor = [];
    /* if added in external library
    [
        "String",
        "Integer",
        "TrueClass",
        "Float",
        "Hash",
        "Array",
        "DateTime",
        "Time",
        "ResponseStatus",
        "ResponseError",
        "QueryBase",
        "QueryData",
        "QueryDb",
        "QueryResponse",
        nameof(Authenticate),
        nameof(AuthenticateResponse),
        nameof(Register),
        nameof(RegisterResponse),
        nameof(AssignRoles),
        nameof(AssignRolesResponse),
        nameof(UnAssignRoles),
        nameof(UnAssignRolesResponse),
        nameof(CancelRequest),
        nameof(CancelRequestResponse),
        nameof(UpdateEventSubscriber),
        nameof(UpdateEventSubscriberResponse),
        nameof(GetEventSubscribers),
        nameof(GetApiKeys),
        nameof(GetApiKeysResponse),
        nameof(RegenerateApiKeys),
        nameof(RegenerateApiKeysResponse),
        nameof(UserApiKey),
        nameof(ConvertSessionToToken),
        nameof(ConvertSessionToTokenResponse),
        nameof(GetAccessToken),
        nameof(GetAccessTokenResponse),
        nameof(NavItem),
        nameof(GetNavItems),
        nameof(GetNavItemsResponse),
        nameof(EmptyResponse),
        nameof(IdResponse),
        nameof(StringResponse),
        nameof(StringsResponse),
        nameof(AuditBase)
    ];
    */

    public static HashSet<string> IgnoreReturnMarkersForSubTypesOf = new() {
    };

    public static TypeFilterDelegate TypeFilter { get; set; }
    public static Func<string, string> CookedTypeFilter { get; set; }
    public static TypeFilterDelegate DeclarationTypeFilter { get; set; }
    public static Func<string, string> CookedDeclarationTypeFilter { get; set; }
    public static Func<string, string> ReturnMarkerFilter { get; set; }

    public static Func<List<MetadataType>, List<MetadataType>> FilterTypes { get; set; } = DefaultFilterTypes;

    public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types.OrderTypesByDeps();

    public static TextCase TextCase { get; set; } = TextCase.SnakeCase;

    public static Func<string, string> EnumNameFormat { get; set; } = name =>
        // If already has part separators, just convert to upper case
        name.IndexOf('_') >= 0
            ? name.ToUpper()
            // If has any lower case, convert to UPPER_CASE
            : name.Any(char.IsLower)
                ? name.ToLowercaseUnderscore().ToUpper()
                // Leave as is
                : name;

    /// <summary>
    /// Add Code to top of generated code
    /// </summary>
    public static AddCodeDelegate InsertCodeFilter { get; set; }

    /// <summary>
    /// Additional Options in Header Options
    /// </summary>
    public List<string> AddQueryParamOptions { get; set; }

    /// <summary>
    /// Emit code without Header Options
    /// </summary>
    public bool WithoutOptions { get; set; }

    /// <summary>
    /// Add Code to bottom of generated code
    /// </summary>
    public static AddCodeDelegate AddCodeFilter { get; set; }

    public HashSet<string> AddedDeclarations { get; set; } = new HashSet<string>();

    public static Func<RubyGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    /// <summary>
    /// Whether property should be marked optional
    /// </summary>
    public static Func<RubyGenerator, MetadataType, MetadataPropertyType, bool> IsPropertyOptional { get; set; } = DefaultIsPropertyOptional;
    public static bool DefaultIsPropertyOptional(RubyGenerator generator, MetadataType type, MetadataPropertyType prop)
    {
        return !prop.IsRequired();
    }

    public void Init(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        AllTypes = metadata.GetAllTypesOrdered();
        AllTypes.RemoveAll(x => x.IgnoreType(Config, includeList));

        //Only use Library Types when they're not shadowed by a User-defined Type of the same name
        var userTypeNames = AllTypes
            .Where(x => x.Namespace != nameof(ServiceStack))
            .Map(x => x.Name.LeftPart('`'))
            .ToSet();
        UseLibraryTypes = LibraryTypes.Where(x => !userTypeNames.Contains(x)).ToSet();

        //Library Types are implemented in the servicestack gem
        AllTypes.RemoveAll(x => UseLibraryTypes.Contains(x.Name.LeftPart('`')));

        AllTypes = FilterTypes(AllTypes);

        //Ruby doesn't support reusing same type name with different generic airity
        var conflictPartialNames = AllTypes.Map(x => x.Name).Distinct()
            .GroupBy(g => g.LeftPart('`'))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        ConflictTypeNames = AllTypes
            .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
            .Map(x => x.Name);

    }

    public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
    {
        var formatter = request.TryResolve<INativeTypesFormatter>();
        Init(metadata);

        var typeNamespaces = new HashSet<string>();
        metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
        metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

        List<string> defaultImports = new(!Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports);

        Func<string, string> defaultValue = k =>
            request.QueryString[k].IsNullOrEmpty() ? "#" : "";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);
        sb.AppendLine("# frozen_string_literal: true");
        sb.AppendLine("# encoding: utf-8");
        sb.AppendLine();

        var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("# Options:");
            sb.AppendLine("=begin");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));        
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}MakePartial: {1}".Fmt(defaultValue("MakePartial"), Config.MakePartial));
            sb.AppendLine("{0}MakeVirtual: {1}".Fmt(defaultValue("MakeVirtual"), Config.MakeVirtual));
            sb.AppendLine("{0}MakeInternal: {1}".Fmt(defaultValue("MakeInternal"), Config.MakeInternal));
            sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
            sb.AppendLine("{0}AddReturnMarker: {1}".Fmt(defaultValue("AddReturnMarker"), Config.AddReturnMarker));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}AddDataContractAttributes: {1}".Fmt(defaultValue("AddDataContractAttributes"), Config.AddDataContractAttributes));
            sb.AppendLine("{0}AddIndexesToDataMembers: {1}".Fmt(defaultValue("AddIndexesToDataMembers"), Config.AddIndexesToDataMembers));
            sb.AppendLine("{0}AddGeneratedCodeAttributes: {1}".Fmt(defaultValue("AddGeneratedCodeAttributes"), Config.AddGeneratedCodeAttributes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}ExportValueTypes: {1}".Fmt(defaultValue("ExportValueTypes"), Config.ExportValueTypes));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}AddNamespaces: {1}".Fmt(defaultValue("AddNamespaces"), Config.AddNamespaces.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));

            if (AddQueryParamOptions != null)
            {
                foreach (var name in AddQueryParamOptions)
                {
                    sb.AppendLine("{0}{1}: {2}".Fmt(defaultValue(name), name, request.QueryString[name]));
                }
            }

            sb.AppendLine("=end");
        }

        formatter?.AddHeader(sb, this, request);

        var header = AddHeader?.Invoke(request);
        if (!string.IsNullOrEmpty(header))
            sb.AppendLine(header);

        sb.AppendLine();
        defaultImports.Each(x => sb.AppendLine($"require '{x}'"));

        var insertCode = InsertCodeFilter?.Invoke(AllTypes, Config);
        if (insertCode != null)
            sb.AppendLine(insertCode);

        sb.AppendLine();

        string lastNS = null;

        var existingTypes = new HashSet<string>();

        var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
        var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
        var responseTypes = metadata.Operations
            .Where(x => x.Response != null)
            .Select(x => x.Response).ToSet();
        var types = metadata.Types.ToSet();

        allTypes = new List<MetadataType>();
        allTypes.AddRange(AllTypes.Where(x => x.IsEnum == true));
        allTypes.AddRange(AllTypes.Where(x => x.IsEnum != true));

        // Ruby doesn't support Generic classes, generic Types are emitted with their Type
        // params erased, keeping the inherited properties of the Types extending them

        var orderedTypes = allTypes;

        foreach (var type in orderedTypes)
        {
            var fullTypeName = type.GetFullName();
            if (requestTypes.Contains(type))
            {
                if (!existingTypes.Contains(fullTypeName))
                {
                    MetadataType response = null;
                    if (requestTypesMap.TryGetValue(type, out var operation))
                    {
                        response = operation.Response;
                    }

                    lastNS = AppendType(ref sb, type, lastNS,
                        new CreateTypeOptions
                        {
                            Routes = metadata.Operations.GetRoutes(type),
                            ImplementsFn = () =>
                            {
                                if (!Config.AddReturnMarker
                                    && operation?.ReturnsVoid != true
                                    && operation?.ReturnType == null)
                                    return null;

                                if (operation?.ReturnsVoid == true)
                                    return nameof(IReturnVoid);
                                if (operation?.ReturnType != null)
                                {
                                    var retType = ReturnTypeAliases.TryGetValue(operation.ReturnType.Name, out var returnTypeAlias)
                                        ? returnTypeAlias
                                        : Type(operation.ReturnType.Name, operation.ReturnType.GenericArgs);
                                    return retType;
                                }
                                return response != null
                                    ? Type(response.Name, response.GenericArgs)
                                    : null;
                            },
                            IsRequest = true,
                            Op = operation,
                        });

                    existingTypes.Add(fullTypeName);
                }
            }
            else if (responseTypes.Contains(type))
            {
                if (!existingTypes.Contains(fullTypeName)
                    && !Config.IgnoreTypesInNamespaces.Contains(type.Namespace))
                {
                    lastNS = AppendType(ref sb, type, lastNS,
                        new CreateTypeOptions {
                            IsResponse = true,
                        });

                    existingTypes.Add(fullTypeName);
                }
            }
            else if (types.Contains(type) && !existingTypes.Contains(fullTypeName))
            {
                var ignoreType = IgnoreTypeInfosFor.Contains(type.Name);
                if (!ignoreType)
                {
                    lastNS = AppendType(ref sb, type, lastNS,
                        new CreateTypeOptions { IsType = true });
                }

                existingTypes.Add(fullTypeName);
            }
        }
        var addCode = AddCodeFilter?.Invoke(AllTypes, Config);
        if (addCode != null)
            sb.AppendLine(addCode);
        
        var ret = StringBuilderCache.ReturnAndFree(sbInner);
        return formatter != null ? formatter.Transform(ret, this, request) : ret;
    }

    private List<MetadataType> allTypes;

    string AsIReturn(string genericArg) => $"IReturn[{genericArg}]";

    private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS,
        CreateTypeOptions options)
    {
        sb.AppendLine();

        AppendComments(sb, type.Description);
        if (options?.Routes != null)
        {
            AppendAttributes(sb, options.Routes.ConvertAll(x => x.ToMetadataAttribute()));
        }
        AppendAttributes(sb, type.Attributes);
        AppendDataContract(sb, type.DataContract);

        var typeName = Type(type.Name, type.GenericArgs);

        sb.Emit(type, Lang.Ruby);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            sb.AppendLine($"module {typeName}");
            sb = sb.Indent();
            var hasIntValue = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty();

            if (type.EnumNames != null)
            {
                for (var i = 0; i < type.EnumNames.Count; i++)
                {
                    var name = type.EnumNames[i];
                    var value = hasIntValue
                        ? type.EnumValues?[i]
                        : name;

                    var enumName = EnumNameFormat(name);
                    sb.AppendLine($"{enumName} = {(hasIntValue ? value : $"'{value}'")}");
                }
            }

            sb = sb.UnIndent();
            sb.AppendLine("end");
        }
        else
        {
            var defType = "class";
            var extends = "";

            var interfaces = new List<string>();
            var implStr = options?.ImplementsFn?.Invoke();

            string responseTypeExpression = null;
            //? $"def method\n  return '{options?.Op?.Method}'\nend"
            string responseMethod = $"def get_type_name() = '{typeName}'";
            if (options?.Op?.Method != null)
            {
                responseMethod += $"\n    def get_method() = '{options.Op.Method}'";
            }
            if (!string.IsNullOrEmpty(implStr))
            {
                responseTypeExpression = "def response_type() = " + (implStr == nameof(IReturnVoid) ? "nil" : implStr);
            }
            if (responseTypeExpression == null && type.Type != null)
            {
                // need to emit type hint when a class contains a generic response type
                var genericIReturn = type.Type.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                if (genericIReturn != null)
                {
                    var existsInBase = type.Type.BaseType?.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                    if (existsInBase == null)
                    {
                        var retType = genericIReturn.GetGenericArguments()[0];
                        var rubyRetType = retType.IsGenericType
                            ? Type(retType.Name, retType.GetGenericArguments().Select(x => x.Name).ToArray())
                            : Type(retType.Name, TypeConstants.EmptyStringArray);
                        implStr = AsIReturn(rubyRetType);
                        responseTypeExpression = "def response_type() = " + rubyRetType;
                    }
                }
                else
                {
                    var returnVoid = type.Type.GetTypeWithInterfaceOf(typeof(IReturnVoid));
                    if (returnVoid != null)
                    {
                        var existsInBase = type.Type.BaseType?.GetTypeWithInterfaceOf(typeof(IReturnVoid));
                        if (existsInBase == null)
                        {
                            implStr = nameof(IReturnVoid);
                            responseTypeExpression = "def response_type() = nil";
                        }
                    }
                }
            }

            //Don't emit interface marker for DTO base classes
            if (type.IsInterface == true)
            {
                defType = "module";
            }
            else if (type.Inherits != null)
            {
                extends = $" < {Type(type.Inherits, includeNested: true)}";
            }

            // DTOs inheriting a collection extend Array or Hash, which the client sends
            // as the JSON Array or Object the API expects, e.g: class StoreContacts < Array
            var collectionBaseType = CollectionBaseType(type.Inherits);
            if (collectionBaseType != null)
            {
                extends = $" < {collectionBaseType}";
            }

            sb.AppendLine($"{defType} {typeName}{extends}");
            sb = sb.Indent();

            // DTO provides to_hash/from_hash conversions from the properties below
            if (type.IsInterface != true && string.IsNullOrEmpty(extends))
            {
                sb.AppendLine($"include {LibraryType("DTO")}");
                sb.AppendLine();
            }
            else if (collectionBaseType != null)
            {
                var elementTypeExpression = CollectionTypeExpression(type.Inherits);
                if (elementTypeExpression != null)
                {
                    // Populate the collection with instances of its element Type
                    sb.AppendLine($"def self.from_hash(json) = new.replace({LibraryType("DTO")}::Serializer.from_json_value({elementTypeExpression}, json))");
                    sb.AppendLine();
                }
            }

            InnerTypeFilter?.Invoke(sb, type);

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine($"  attr_accessor :version");
                sb.AppendLine();
            }
            
            if (collectionBaseType == null)
            {
                AddProperties(sb, type, 
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                if (type.IsInterface != true)
                {
                    AppendPropertyMetadata(sb, type);
                }
            }

            if (responseTypeExpression != null)
            {
                sb.AppendLine(responseTypeExpression);
                if (responseMethod != null)
                {
                    sb.AppendLine(responseMethod);
                }
            }
            else if (type.Properties.IsEmpty() && !addVersionInfo && type.Name != "IReturn`1" && type.Name != "IReturnVoid")
            {
            }
            
            sb = sb.UnIndent();
            sb.AppendLine("end");
        }

        PostTypeFilter?.Invoke(sb, type);

        return lastNS;
    }

    public virtual string GetPropertyType(MetadataPropertyType prop, out bool isNullable)
    {
        var propType = Type(prop.GetTypeName(Config, AllTypes), prop.GenericArgs);
        isNullable = propType.EndsWith("?");
        if (isNullable)
            propType = propType.Substring(0, propType.Length - 1);
        return propType;
    }

    static string asOptional(string type) => type.StartsWith("Optional[") ? type : $"Optional[{type}]";
        
    public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
    {
        var wasAdded = false;
        var modifier = "";

        var dataMemberIndex = 1;
        if (type.Properties != null)
        {
            foreach (var prop in type.Properties)
            {
                if (wasAdded) sb.AppendLine();

                var propType = Type(prop.Type, prop.GenericArgs);
                var optional = IsPropertyOptional(this, type, prop);

                wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                PrePropertyFilter?.Invoke(sb, prop, type);

                var propName = GetPropertyName(prop.Name);
                sb.AppendLine($"# @return [{propType}]");
                sb.AppendLine($"attr_accessor :{propName}");

                PostPropertyFilter?.Invoke(sb, prop, type);
            }
        }

        if (includeResponseStatus)
        {
            if (wasAdded) sb.AppendLine();

            AppendDataMember(sb, null, dataMemberIndex++);
            sb.AppendLine($"# @return [{LibraryType(nameof(ResponseStatus))}]");
            sb.AppendLine($"{modifier}attr_accessor :{GetPropertyName(nameof(ResponseStatus))}");
        }
    }
    
    public void AppendComments(StringBuilderWrapper sb, string desc)
    {
        if (desc != null && Config.AddDescriptionAsComments)
        {
            sb.AppendLine("#");
            sb.AppendLine($"# {desc.SafeComment()}");
            sb.AppendLine("#");
        }
    }

    public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
    {
        if (attributes == null || attributes.Count == 0 || IgnoreAllAttributes) return false;

        var existingAttrs = new HashSet<string>();

        foreach (var attr in attributes)
        {
            if (IgnoreAttributes.Contains(attr.Name))
                continue;

            var attrName = attr.Name;
            if (existingAttrs.Contains(attrName))
                continue;

            existingAttrs.Add(attrName);

            var args = StringBuilderCacheAlt.Allocate();
            if (attr.ConstructorArgs?.Count > 0)
            {
                foreach (var ctorArg in attr.ConstructorArgs)
                {
                    if (args.Length > 0)
                        args.Append(", ");
                    args.Append(TypeValue(ctorArg.Type, ctorArg.Value));
                }
            }
            else if (attr.Args?.Count > 0)
            {
                foreach (var attrArg in attr.Args)
                {
                    if (args.Length > 0)
                        args.Append(", ");
                    args.Append($"{attrArg.Name}: {TypeValue(attrArg.Type, attrArg.Value)}");
                }
            }

            var argsString = StringBuilderCacheAlt.ReturnAndFree(args);
            sb.AppendLine(argsString.Length > 0
                ? $"# @{attrName}({argsString})"
                : $"# @{attrName}");
        }

        return true;
    }

    public string TypeValue(string type, string value)
    {
        var alias = TypeAlias(type);
        if (value == null)
            return "nil";
        if (alias == "Integer" || alias == "Float" || alias == "BigDecimal")
            return value;

        if (value.StartsWith("typeof("))
        {
            //Only emit type as Namespaces are merged
            var typeNameOnly = value.Substring(7, value.Length - 8).LastRightPart('.');
            return $"'{typeNameOnly}'";
        }

        return value.QuotedSafeValue();
    }

    public string Type(MetadataTypeName typeName, bool includeNested = false)
    {
        return Type(typeName.Name, typeName.GenericArgs, includeNested: includeNested);
    }

    public string TypeAlias(string type)
    {
        typeAliasValues ??= new HashSet<string>(TypeAliases.Values);
        if (typeAliasValues.Contains(type))
            return type;

        type = type.SanitizeType();
        if (type == "Byte[]")
            return "String";

        var arrParts = type.SplitOnFirst('[');
        if (arrParts.Length > 1)
            return "Array";

        TypeAliases.TryGetValue(type, out var typeAlias);

        return typeAlias ?? NameOnly(type);
    }

    public string NameOnly(string type)
    {
        var name = ConflictTypeNames.Contains(type)
            ? type.Replace('`', '_')
            : type.LeftPart('`');

        return name.LastRightPart('.').SafeToken();
    }

    public string Type(string type, string[] genericArgs, bool includeNested = false)
    {
        if (TypeFilter != null)
        {
            type = TypeFilter(type, genericArgs);
        }

        if (genericArgs != null)
        {
            if (type == "Nullable`1")
                return TypeAlias(genericArgs[0]);

            TypeAliases.TryGetValue(type, out var typeAlias);

            var parts = typeAlias != null
                ? typeAlias.SplitOnFirst('[')
                : type.SplitOnFirst('`');

            if (parts.Length > 1)
            {
                var args = StringBuilderCacheAlt.Allocate();
                foreach (var arg in genericArgs)
                {
                    if (args.Length > 0)
                        args.Append(", ");

                    args.Append(TypeAlias(arg));
                }

                var typeName = TypeAlias(type);
                if (IsLibraryType(typeName))
                {
                    // Generic Library Types convert their results, e.g. QueryResponse.of(Booking)
                    return GenericLibraryTypes.Contains(typeName) && genericArgs.Length > 0
                        ? $"{LibraryType(typeName)}.of({Type(genericArgs[0], TypeConstants.EmptyStringArray)})"
                        : LibraryType(typeName);
                }
                return $"{typeName}";
            }
        }

        var result = TypeAlias(type);
        if (IsLibraryType(result))
            result = LibraryType(result);
        if (CookedTypeFilter != null)
            result = CookedTypeFilter(result);
        return result;
    }

    /// <summary>
    /// Generate the wire name and Type of each property, which the servicestack
    /// gem uses to convert DTOs to and from the JSON their APIs use, e.g:
    ///
    ///     def self.properties
    ///       {
    ///         id: { name: 'id' },
    ///         booking_start_date: { name: 'bookingStartDate', type: DateTime },
    ///       }
    ///     end
    /// </summary>
    public void AppendPropertyMetadata(StringBuilderWrapper sb, MetadataType type)
    {
        var props = type.Properties.Safe().ToList();
        if (props.Count == 0)
            return;

        sb.AppendLine();
        sb.AppendLine("def self.properties");
        sb = sb.Indent();
        sb.AppendLine("{");
        sb = sb.Indent();

        foreach (var prop in props)
        {
            var propName = GetPropertyName(prop.Name);
            var jsonName = prop.GetSerializedAlias() ?? prop.Name.ToCamelCase();
            var typeExpression = PropertyTypeExpression(prop);

            sb.AppendLine(typeExpression != null
                ? $"{propName}: {{ name: '{jsonName}', type: {typeExpression} }},"
                : $"{propName}: {{ name: '{jsonName}' }},");
        }

        sb = sb.UnIndent();
        sb.AppendLine("}");
        sb = sb.UnIndent();
        sb.AppendLine("end");
        sb.AppendLine();
    }

    public static HashSet<string> ArrayTypes { get; set; } =
    [
        "List`1", "IEnumerable`1", "ICollection`1", "HashSet`1", "Queue`1", "Stack`1", "IList`1", "IEnumerable",
    ];

    public static HashSet<string> DictionaryTypes { get; set; } =
    [
        "Dictionary`2", "IDictionary`2", "IOrderedDictionary`2", "OrderedDictionary", "StringDictionary",
        "IDictionary", "IOrderedDictionary",
    ];

    /// <summary>
    /// The Ruby Type expression used to convert a property's JSON value, or null
    /// when its JSON value is used as-is, e.g:
    ///
    ///     Coupon                        # a nested DTO
    ///     [Coupon]                      # a List of DTOs
    ///     { String =&gt; Coupon }          # a Dictionary of DTOs
    ///     DateTime
    /// </summary>
    public string PropertyTypeExpression(MetadataPropertyType prop)
    {
        if (prop.GenericArgs?.Length > 0)
        {
            if (ArrayTypes.Contains(prop.Type))
            {
                var elementType = ConvertedTypeExpression(prop.GenericArgs[0]);
                return elementType != null ? $"[{elementType}]" : null;
            }
            if (DictionaryTypes.Contains(prop.Type) && prop.GenericArgs.Length > 1)
            {
                var valueType = ConvertedTypeExpression(prop.GenericArgs[1]);
                return valueType != null ? $"{{ String => {valueType} }}" : null;
            }
            if (prop.Type == "Nullable`1")
                return ConvertedTypeExpression(prop.GenericArgs[0]);
        }

        if (prop.Type?.EndsWith("[]") == true)
        {
            var elementType = ConvertedTypeExpression(prop.Type.Substring(0, prop.Type.Length - 2));
            return elementType != null ? $"[{elementType}]" : null;
        }

        return ConvertedTypeExpression(prop.Type);
    }

    /// <summary>
    /// The Ruby collection a DTO inheriting a collection extends, or null when it doesn't
    /// inherit one, e.g: List&lt;Contact&gt; -&gt; Array
    /// </summary>
    public string CollectionBaseType(MetadataTypeName baseType)
    {
        if (baseType?.Name == null)
            return null;
        if (ArrayTypes.Contains(baseType.Name) || baseType.Name.EndsWith("[]"))
            return "Array";
        return DictionaryTypes.Contains(baseType.Name) ? "Hash" : null;
    }

    /// <summary>
    /// The Ruby Type expression of an inherited collection's elements, or null when
    /// its JSON values are used as-is, e.g: [Contact] or { String =&gt; Contact }
    /// </summary>
    public string CollectionTypeExpression(MetadataTypeName baseType)
    {
        if (baseType?.GenericArgs == null)
            return null;

        if (ArrayTypes.Contains(baseType.Name) && baseType.GenericArgs.Length > 0)
        {
            var elementType = ConvertedTypeExpression(baseType.GenericArgs[0]);
            return elementType != null ? $"[{elementType}]" : null;
        }
        if (DictionaryTypes.Contains(baseType.Name) && baseType.GenericArgs.Length > 1)
        {
            var valueType = ConvertedTypeExpression(baseType.GenericArgs[1]);
            return valueType != null ? $"{{ String => {valueType} }}" : null;
        }
        return null;
    }

    /// <summary>
    /// The Ruby Type a JSON value needs to be converted into, or null when it doesn't
    /// </summary>
    public string ConvertedTypeExpression(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        var name = typeName.SanitizeType().LeftPart('`');
        switch (name)
        {
            case "DateTime":
            case "DateTimeOffset":
            case "DateOnly":
                return "DateTime";
        }

        //Enums are serialized as their String value, primitives are used as-is
        var metaType = AllTypes.FirstOrDefault(x => x.Name.LeftPart('`') == name);
        if (metaType != null)
            return metaType.IsEnum == true ? null : Type(name, TypeConstants.EmptyStringArray);

        if (IsLibraryType(name))
            return LibraryType(name);

        return null;
    }

    private string GetPropertyName(string name)
    {
        return name.SafeToken().PropertyStyle();
    }

    public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dataMember, int dataMemberIndex)
    {
        if (dataMember == null)
        {
            if (Config.AddIndexesToDataMembers)
            {
                sb.AppendLine($"# @DataMember(Order={dataMemberIndex})");
                return true;
            }
            return false;
        }

        var sbDataMember = StringBuilderCacheAlt.Allocate();
        if (dataMember.Name != null)
        {
            if (sbDataMember.Length > 0)
                sbDataMember.Append(", ");
            sbDataMember.Append($"Name={dataMember.Name.QuotedSafeValue()}");
        }

        if (dataMember.Order != null || Config.AddIndexesToDataMembers)
        {
            if (sbDataMember.Length > 0)
                sbDataMember.Append(", ");
            sbDataMember.Append($"Order={dataMember.Order ?? dataMemberIndex}");
        }

        if (dataMember.IsRequired == true)
        {
            if (sbDataMember.Length > 0)
                sbDataMember.Append(", ");
            sbDataMember.Append("IsRequired=true");
        }

        if (dataMember.EmitDefaultValue != null)
        {
            if (sbDataMember.Length > 0)
                sbDataMember.Append(", ");
            sbDataMember.Append($"EmitDefaultValue={dataMember.EmitDefaultValue.ToString().ToLower()}");
        }

        if (sbDataMember.Length > 0)
        {
            sb.AppendLine($"# @DataMember({StringBuilderCacheAlt.ReturnAndFree(sbDataMember)})");
            return true;
        }

        StringBuilderCacheAlt.Free(sbDataMember);
        return false;
    }

    public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dataContract)
    {
        if (dataContract == null)
        {
            if (Config.AddDataContractAttributes)
                sb.AppendLine("# @DataContract");
            return;
        }

        var sbDataContract = StringBuilderCacheAlt.Allocate();
        if (dataContract.Name != null)
        {
            if (sbDataContract.Length > 0)
                sbDataContract.Append(", ");
            sbDataContract.Append($"Name={dataContract.Name.QuotedSafeValue()}");
        }

        if (dataContract.Namespace != null)
        {
            if (sbDataContract.Length > 0)
                sbDataContract.Append(", ");
            sbDataContract.Append($"Namespace={dataContract.Namespace.QuotedSafeValue()}");
        }

        if (sbDataContract.Length > 0)
        {
            sb.AppendLine($"# @DataContract({StringBuilderCacheAlt.ReturnAndFree(sbDataContract)})");
        }
        else
        {
            StringBuilderCacheAlt.Free(sbDataContract);
            sb.AppendLine("# @DataContract");
        }
    }
}

public static class RubyGeneratorExtensions
{
    public static string PropertyStyle(this string name)
    {
        return RubyGenerator.TextCase == TextCase.CamelCase
            ? name.ToCamelCase()
            : RubyGenerator.TextCase == TextCase.SnakeCase
                ? name.ToLowercaseUnderscore()
                : name;
    }
}