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
    public readonly MetadataTypesConfig Config;
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
    };

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
    public static HashSet<string> IgnoreTypeInfosFor =
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
    public static Func<RubyGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
        DefaultIsPropertyOptional;

    public void Init(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        AllTypes = metadata.GetAllTypesOrdered();
        AllTypes.RemoveAll(x => x.IgnoreType(Config, includeList));
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

        ConflictTypeNames.Add(typeof(QueryDb<,>).Name);
        ConflictTypeNames.Add(typeof(QueryData<,>).Name);
    }

    public static bool? DefaultIsPropertyOptional(RubyGenerator generator, MetadataType type, MetadataPropertyType prop)
    {
        if (prop.IsRequired == true)
            return false;

        return null;
    }

    public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
    {
        Init(metadata);

        var typeNamespaces = new HashSet<string>();
        metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
        metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

        var defaultImports = !Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports;

        Func<string, string> defaultValue = k =>
            request.QueryString[k].IsNullOrEmpty() ? "#" : "";

        var sb = new StringBuilder();
        sb.AppendLine("# frozen_string_literal: true");
        sb.AppendLine("# encoding: utf-8");
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

        //Ruby doesn't support Generic classes
        allTypes.RemoveAll(x => x.Name.IndexOf('`') >= 0);
        AllTypes.RemoveAll(x => x.Name.IndexOf('`') >= 0);

        var orderedTypes = allTypes;

        var sbInner = StringBuilderCacheAlt.Allocate();
        var sbServiceStackTypes = StringBuilderCacheAlt.Allocate();
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

                    lastNS = AppendType(ref sbInner, type, lastNS,
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
                    lastNS = AppendType(ref sbInner, type, lastNS,
                        new CreateTypeOptions
                        {
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
                    var sbTarget = ignoreType ? sbServiceStackTypes : sbInner;
                    lastNS = AppendType(ref sbTarget, type, lastNS,
                        new CreateTypeOptions { IsType = true });
                }

                existingTypes.Add(fullTypeName);
            }
        }

        var addHeader = AddHeader?.Invoke(request);
        if (addHeader != null)
        {
            sb.AppendLine(addHeader);
        }

        if (!WithoutOptions)
        {
            sb.AppendLine("=begin");
            sb.AppendLine("# Options:");
            sb.AppendLine("{0}BaseUrl: {1}".Fmt(defaultValue("BaseUrl"), Config.BaseUrl));


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

        sb.AppendLine();
        defaultImports.Each(x => sb.AppendLine($"require '{x}'"));

        var insertCode = InsertCodeFilter?.Invoke(AllTypes, Config);
        if (insertCode != null)
            sb.AppendLine(insertCode);

        sb.AppendLine();
        sb.Append(StringBuilderCacheAlt.ReturnAndFree(sbInner));
        StringBuilderCacheAlt.Free(sbServiceStackTypes);

        var addCode = AddCodeFilter?.Invoke(AllTypes, Config);
        if (addCode != null)
            sb.AppendLine(addCode);
        return sb.ToString();
    }

    private List<MetadataType> allTypes;

    string AsIReturn(string genericArg) => $"IReturn[{genericArg}]";

    private string AppendType(ref StringBuilder sbInner, MetadataType type, string lastNS,
        CreateTypeOptions options)
    {
        var sb = new StringBuilderWrapper(sbInner);
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
            string responseMethod = options?.Op?.Method != null
                ? $"def get_type_name() = '{options?.Op?.Method}'"
                : null;
            if (string.IsNullOrEmpty(implStr) && type.Type is {IsAbstract: true})
            {
                // need to emit type hint when a generic base class contains a generic response type
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

            sb.AppendLine($"{defType} {typeName}{extends}");
            sb = sb.Indent();

            InnerTypeFilter?.Invoke(sb, type);

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine($"  attr_accessor :version");
                sb.AppendLine();
            }
            
            AddProperties(sb, type, 
                includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                    && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

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
                sb.AppendLine("pass");
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
            sb.AppendLine($"{modifier}{GetPropertyName(nameof(ResponseStatus))}: ResponseStatus = None");
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

    public static string TypeAlias(string type)
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

    public static string NameOnly(string type)
    {
        var name = conflictTypeNames.Contains(type)
            ? type.Replace('`', '_')
            : type.LeftPart('`');

        return name.LastRightPart('.').SafeToken();
    }

    static HashSet<string> conflictTypeNames = new();

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
                return $"{typeName}";
            }
        }

        var result = TypeAlias(type);
        if (CookedTypeFilter != null)
            result = CookedTypeFilter(result);
        return result;
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
                sb.AppendLine($"  # @DataMember(Order={dataMemberIndex})");
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
            sb.AppendLine($"  # @DataMember({StringBuilderCacheAlt.ReturnAndFree(sbDataMember)})");
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