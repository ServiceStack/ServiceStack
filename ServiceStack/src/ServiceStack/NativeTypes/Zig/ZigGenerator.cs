using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Zig;

public class ZigGenerator : ILangGenerator
{
    public readonly MetadataTypesConfig Config;
    readonly NativeTypesFeature feature;
    public List<string> ConflictTypeNames = new();
    public List<MetadataType> AllTypes { get; set; }

    public ZigGenerator(MetadataTypesConfig config)
    {
        Config = config;
        feature = HostContext.GetPlugin<NativeTypesFeature>();
    }
        
    public static bool UseUnionTypeEnums { get; set; }

    public static bool EmitPartialConstructors { get; set; } = true;

    public static Func<IRequest,string> AddHeader { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
        
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

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
    
    public static List<string> DefaultImports = new() {
        "const std = @import(\"std\");",
    };

    public static Dictionary<string, string> TypeAliases = new() {
        {"String", "[]const u8"},
        {"Boolean", "bool"},
        {"DateTime", "[]const u8"},
        {"DateOnly", "[]const u8"},
        {"DateTimeOffset", "[]const u8"},
        {"TimeSpan", "[]const u8"},
        {"TimeOnly", "[]const u8"},
        {"Guid", "[]const u8"},
        {"Char", "u8"},
        {"Byte", "u8"},
        {"Int16", "i16"},
        {"Int32", "i32"},
        {"Int64", "i64"},
        {"UInt16", "u16"},
        {"UInt32", "u32"},
        {"UInt64", "u64"},
        {"Single", "f32"},
        {"Double", "f64"},
        {"Decimal", "f64"},
        {"IntPtr", "isize"},
        {"List", "[]"},
        {"Byte[]", "[]const u8"},
        {"Stream", "[]const u8"},
        {"HttpWebResponse", "[]const u8"},
        {"IDictionary", "std.StringHashMap"},
        {"OrderedDictionary", "std.StringHashMap"},
        {"Uri", "[]const u8"},
        {"Type", "[]const u8"},
    };

    public static Dictionary<string, string> ReturnTypeAliases = new() {
        {"Byte[]", "[]const u8"},
        {"Stream", "[]const u8"},
        {"HttpWebResponse", "[]const u8"},
    };

    private static string declaredEmptyString = "\"\"";
    internal static readonly Dictionary<string, string> primitiveDefaultValues = new() {
        {"String", declaredEmptyString},
        {"[]const u8", declaredEmptyString},
        {"Boolean", "false"},
        {"bool", "false"},
        {"DateTime", declaredEmptyString},
        {"DateTimeOffset", declaredEmptyString},
        {"TimeSpan", declaredEmptyString},
        {"Guid", declaredEmptyString},
        {"Char", "0"},
        {"u8", "0"},
        {"i16", "0"},
        {"i32", "0"},
        {"i64", "0"},
        {"u16", "0"},
        {"u32", "0"},
        {"u64", "0"},
        {"f32", "0"},
        {"f64", "0"},
        {"isize", "0"},
    };

    public HashSet<string> UseGenericDefinitionsFor { get; set; } = new()
    {
        typeof(QueryResponse<>).Name,
    };
        
    public static TypeFilterDelegate TypeFilter { get; set; }
    public static Func<string, string> CookedTypeFilter { get; set; }
    public static TypeFilterDelegate DeclarationTypeFilter { get; set; }
    public static Func<string, string> CookedDeclarationTypeFilter { get; set; }
    public static Func<string, string> ReturnMarkerFilter { get; set; }

    public static Func<List<MetadataType>, List<MetadataType>> FilterTypes { get; set; } = DefaultFilterTypes;

    public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types.OrderTypesByDeps();
        
    public static bool InsertTsNoCheck { get; set; }
        
    /// <summary>
    /// Add Code to top of generated code
    /// </summary>
    public static AddCodeDelegate InsertCodeFilter { get; set; }

    /// <summary>
    /// Add Code to bottom of generated code
    /// </summary>
    public static AddCodeDelegate AddCodeFilter { get; set; }

    /// <summary>
    /// Include Additional QueryString Params in Header Options
    /// </summary>
    public List<string> AddQueryParamOptions { get; set; }

    /// <summary>
    /// Emit code without Header Options
    /// </summary>
    public bool WithoutOptions { get; set; }

    public string DictionaryDeclaration { get; set; } = null; // Zig uses std.StringHashMap directly

    public HashSet<string> AddedDeclarations { get; set; } = new HashSet<string>();

    public static Func<ZigGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    /// <summary>
    /// Whether property should be marked optional
    /// </summary>
    public static Func<ZigGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
        DefaultIsPropertyOptional;

    /// <summary>
    /// Helper to make Nullable properties
    /// </summary>
    public static bool UseNullableProperties
    {
        set
        {
            if (value)
            {
                IsPropertyOptional = (gen, type, prop) => false;
                PropertyTypeFilter = (gen, type, prop) => 
                    prop.IsRequired == true
                        ? gen.GetPropertyType(prop, out _)
                        : gen.GetPropertyType(prop, out _) + "|null";
            }
        }
    }

    public void Init(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        AllTypes = metadata.GetAllTypesOrdered();
        AllTypes.RemoveAll(x => x.IgnoreType(Config, includeList));
        AllTypes = FilterTypes(AllTypes);

        //TypeScript doesn't support reusing same type name with different generic airity
        var conflictPartialNames = AllTypes.Map(x => x.Name).Distinct()
            .GroupBy(g => g.LeftPart('`'))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        this.ConflictTypeNames = AllTypes
            .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
            .Map(x => x.Name);
    }

    public MetadataType FindType(MetadataTypeName typeRef) =>
        typeRef == null ? null : FindType(typeRef.Name, typeRef.Namespace); 
    public MetadataType FindType(string name, string @namespace = null) => AllTypes.FirstOrDefault(x => x.Name == name 
        && (@namespace == null || @namespace == x.Namespace));

    public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
    {
        Init(metadata);

        List<string> defaultImports = new(!Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports);

        var globalNamespace = Config.GlobalNamespace;

        string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "/// " : "//";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);
        var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("/// Options:");
            sb.AppendLine("/// Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("/// Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("/// Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("/")));
            sb.AppendLine("/// BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}MakePropertiesOptional: {1}".Fmt(defaultValue("MakePropertiesOptional"), Config.MakePropertiesOptional));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));
            AddQueryParamOptions.Each(name => sb.AppendLine($"{defaultValue(name)}{name}: {request.QueryString[name]}"));

            sb.AppendLine("///");
            sb.AppendLine();
        }

        var header = AddHeader?.Invoke(request);
        if (!string.IsNullOrEmpty(header))
            sb.AppendLine(header);

        string lastNS = null;

        var existingTypes = new HashSet<string>();

        var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
        var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
        var responseTypes = metadata.Operations
            .Where(x => x.Response != null)
            .Select(x => x.Response).ToSet();

        foreach (var import in defaultImports)
        {
            sb.AppendLine(import);
        }

        if (defaultImports.Count > 0)
        {
            sb.AppendLine();
        }

        var insertCode = InsertCodeFilter?.Invoke(AllTypes, Config);
        if (insertCode != null)
            sb.AppendLine(insertCode);

        //ServiceStack core interfaces
        foreach (var type in AllTypes)
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
                                    return Type("IReturn`1", new[] {
                                        ReturnTypeAliases.TryGetValue(operation.ReturnType.Name, out var returnTypeAlias)
                                            ? returnTypeAlias
                                            : Type(operation.ReturnType)
                                    });
                                return response != null
                                    ? Type("IReturn`1", new[] { Type(response.Name, response.GenericArgs) })
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
                        new CreateTypeOptions
                        {
                            IsResponse = true,
                        });

                    existingTypes.Add(fullTypeName);
                }
            }
            else if (AllTypes.Contains(type) && !existingTypes.Contains(fullTypeName))
            {
                lastNS = AppendType(ref sb, type, lastNS,
                    new CreateTypeOptions { IsType = true });

                existingTypes.Add(fullTypeName);
            }
        }

        var addCode = AddCodeFilter?.Invoke(AllTypes, Config);
        if (addCode != null)
            sb.AppendLine(addCode);

        return StringBuilderCache.ReturnAndFree(sbInner);
    }

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
        if (type.IsInterface != true) AppendDataContract(sb, type.DataContract);

        sb.Emit(type, Lang.Zig);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty();

            // Zig enums - use integer backing type if specified
            if (isIntEnum)
            {
                sb.AppendLine($"pub const {Type(type.Name, type.GenericArgs)} = enum(i32) {{");
            }
            else
            {
                sb.AppendLine($"pub const {Type(type.Name, type.GenericArgs)} = enum {{");
            }

            sb = sb.Indent();

            if (type.EnumNames != null)
            {
                for (var i = 0; i < type.EnumNames.Count; i++)
                {
                    var name = type.EnumNames[i];
                    var value = type.EnumValues?[i];

                    var memberValue = type.GetEnumMemberValue(i);
                    if (memberValue != null)
                    {
                        sb.AppendLine($"{name.ToLowercaseUnderscore()},");
                        continue;
                    }

                    if (isIntEnum && value != null)
                    {
                        sb.AppendLine($"{name.ToLowercaseUnderscore()} = {value},");
                    }
                    else
                    {
                        sb.AppendLine($"{name.ToLowercaseUnderscore()},");
                    }
                }
            }

            sb = sb.UnIndent();
            sb.AppendLine("};");
        }
        else
        {
            var typeName = NameOnly(type.Name);

            // Check if this is a generic type
            if (type.GenericArgs != null && type.GenericArgs.Length > 0)
            {
                // In Zig, generic types are functions that return types
                var genericParams = new List<string>();

                // Check if the type has properties (not a marker interface)
                var hasProperties = type.Properties != null && type.Properties.Count > 0;

                foreach (var arg in type.GenericArgs)
                {
                    genericParams.Add($"comptime {arg}: type");
                }
                sb.AppendLine($"pub fn {typeName}({string.Join(", ", genericParams)}) type {{");
                sb = sb.Indent();

                // For marker interfaces (no properties), explicitly discard unused parameters
                if (!hasProperties)
                {
                    foreach (var arg in type.GenericArgs)
                    {
                        sb.AppendLine($"_ = {arg};");
                    }
                }

                sb.AppendLine("return struct {");
            }
            else
            {
                sb.AppendLine($"pub const {typeName} = struct {{");
            }

            sb = sb.Indent();
            InnerTypeFilter?.Invoke(sb, type);

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                // Zig doesn't support default values in struct definitions
                sb.AppendLine("{0}: i32, //{1}".Fmt(
                    GetPropertyName("Version"), Config.AddImplicitVersion));
            }

            AddProperties(sb, type,
                includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                                                                && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

            sb = sb.UnIndent();
            sb.AppendLine("};");

            // Close the function for generic types
            if (type.GenericArgs != null && type.GenericArgs.Length > 0)
            {
                sb = sb.UnIndent();
                sb.AppendLine("}");
            }
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

    public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
    {
        var wasAdded = false;

        var dataMemberIndex = 1;
        if (type.Properties != null)
        {
            foreach (var prop in type.Properties)
            {
                if (wasAdded) sb.AppendLine();

                var propType = GetPropertyType(prop, out var optionalProperty);
                propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                var optional = IsPropertyOptional(this, type, prop) ?? optionalProperty;

                wasAdded = AppendComments(sb, prop.Description);
                wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                sb.Emit(prop, Lang.TypeScript);
                PrePropertyFilter?.Invoke(sb, prop, type);

                // In Zig, optional fields use ?Type syntax (no default values allowed in struct definitions)
                var zigType = optional ? $"?{propType}" : propType;

                sb.AppendLine("{0}: {1},".Fmt(GetPropertyName(prop), zigType));
                PostPropertyFilter?.Invoke(sb, prop, type);
            }
        }

        if (includeResponseStatus)
        {
            if (wasAdded) sb.AppendLine();

            AppendDataMember(sb, null, dataMemberIndex++);
            sb.AppendLine("{0}: ?ResponseStatus,".Fmt(GetPropertyName(nameof(ResponseStatus))));
        }
    }

    public static bool? DefaultIsPropertyOptional(ZigGenerator generator, MetadataType type, MetadataPropertyType prop)
    {
        if (generator.Config.MakePropertiesOptional)
            return true;

        return prop.IsRequired == null
            ? null
            : !prop.IsRequired.Value;
    }

    public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
    {
        if (attributes == null || attributes.Count == 0) return false;

        foreach (var attr in attributes)
        {
            if ((attr.Args == null || attr.Args.Count == 0)
                && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
            {
                sb.AppendLine("// @{0}()".Fmt(attr.Name));
            }
            else
            {
                var args = StringBuilderCacheAlt.Allocate();
                if (attr.ConstructorArgs != null)
                {
                    foreach (var ctorArg in attr.ConstructorArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");
                        args.Append(TypeValue(ctorArg.Type, ctorArg.Value));
                    }
                }
                else if (attr.Args != null)
                {
                    foreach (var attrArg in attr.Args)
                    {
                        if (args.Length > 0)
                            args.Append(", ");
                        args.Append($"{attrArg.Name}={TypeValue(attrArg.Type, attrArg.Value)}");
                    }
                }
                sb.AppendLine("// @{0}({1})".Fmt(attr.Name, StringBuilderCacheAlt.ReturnAndFree(args)));
            }
        }

        return true;
    }

    public string TypeValue(string type, string value)
    {
        var alias = TypeAlias(type);
        if (value == null)
            return "null";
        if (alias == "string" || type == "String")
            return value.ToEscapedString();

        if (value.IsTypeValue())
        {
            //Only emit type as Namespaces are merged
            return "typeof(" + value.ExtractTypeName() + ")";
        }

        return value;
    }

    public static HashSet<string> ArrayTypes = new() {
        "List`1",
        "IEnumerable`1",
        "ICollection`1",
        "HashSet`1",
        "Queue`1",
        "Stack`1",
        "IEnumerable",
    };

    public static HashSet<string> DictionaryTypes = new() {
        "Dictionary`2",
        "IDictionary`2",
        "IOrderedDictionary`2",
        "OrderedDictionary",
        "StringDictionary",
        "IDictionary",
        "IOrderedDictionary",
    };

    public static HashSet<string> AllowedKeyTypes = new() {
        "string",
        "boolean",
        "number",
    };
        
    public string Type(MetadataTypeName typeName) => Type(typeName.Name, typeName.GenericArgs);

    public string DeclarationType(string type, string[] genericArgs, out string addDeclaration)
    {
        addDeclaration = null;
        var useType = DeclarationTypeFilter?.Invoke(type, genericArgs);
        if (useType != null)
            return useType;

        string cooked = null;

        if (genericArgs != null)
        {
            if (ArrayTypes.Contains(type))
            {
                cooked = "[]{0}".Fmt(GenericArg(genericArgs[0])).StripNullable();
            }
            if (DictionaryTypes.Contains(type))
            {
                cooked = "std.StringHashMap({0})".Fmt(GenericArg(genericArgs[1]));
            }
        }

        if (cooked == null)
            cooked = Type(type, genericArgs);

        useType = CookedDeclarationTypeFilter?.Invoke(cooked);
        if (useType != null)
            return useType;

        return cooked;
    }

    public string Type(string type, string[] genericArgs)
    {
        var useType = TypeFilter?.Invoke(type, genericArgs);
        if (useType != null)
            return useType;

        if (genericArgs != null)
        {
            string cooked = null;
            if (type == "Nullable`1")
                cooked = "?{0}".Fmt(GenericArg(genericArgs[0]));
            else if (type == "Nullable`1[]")
                cooked = "[]?" + GenericArg(genericArgs[0]);
            else if (ArrayTypes.Contains(type))
                cooked = "[]{0}".Fmt(GenericArg(genericArgs[0])).StripNullable();
            else if (DictionaryTypes.Contains(type))
            {
                var valArg = genericArgs[1];
                var valType = GenericArg(valArg);
                cooked = "std.StringHashMap(" + valType + ")";
            }
            else
            {
                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    // In Zig, we just use the base type name without generic args for now
                    // Generic types will be handled specially in AppendType
                    var typeName = TypeAlias(type);
                    var suffix = "";
                    if (typeName.StartsWith("[]"))
                    {
                        suffix = "[]";
                        typeName = typeName.Substring(2);
                    }

                    cooked = $"{typeName}{suffix}";
                }
            }

            if (cooked != null)
                return CookedTypeFilter?.Invoke(cooked) ?? cooked;
        }
        else
        {
            type = type.StripNullable();
        }

        return TypeAlias(type);
    }

    private string TypeAlias(string type)
    {
        type = type.SanitizeType();
        if (type == "Byte[]")
            return TypeAliases["Byte[]"];

        var arrParts = type.SplitOnFirst('[');
        if (arrParts.Length > 1)
        {
            var numberOfDimensions = type.Count(c => c == '[');
            var arrayPrefix = String.Concat(Enumerable.Range(0, numberOfDimensions).Select(_ => "[]"));

            return "{0}{1}".Fmt(arrayPrefix, TypeAlias(arrParts[0]));
        }

        TypeAliases.TryGetValue(type, out var typeAlias);

        var cooked = typeAlias ?? NameOnly(type);
        return CookedTypeFilter?.Invoke(cooked) ?? cooked;
    }

    public string NameOnly(string type)
    {
        var name = ConflictTypeNames.Contains(type)
            ? type.Replace('`','_')
            : type.LeftPart('`');

        return name.LastRightPart('.').SafeToken();
    }

    public bool AppendComments(StringBuilderWrapper sb, string desc)
    {
        if (desc != null && Config.AddDescriptionAsComments)
        {
            sb.AppendLine("/// {0}".Fmt(desc.SafeComment()));
        }
        return false;
    }

    public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
    {
        if (dcMeta == null)
        {
            if (Config.AddDataContractAttributes)
                sb.AppendLine("// @DataContract()");
            return;
        }

        var dcArgs = "";
        if (dcMeta.Name != null || dcMeta.Namespace != null)
        {
            if (dcMeta.Name != null)
                dcArgs = "Name={0}".Fmt(dcMeta.Name.QuotedSafeValue());

            if (dcMeta.Namespace != null)
            {
                if (dcArgs.Length > 0)
                    dcArgs += ", ";

                dcArgs += "Namespace={0}".Fmt(dcMeta.Namespace.QuotedSafeValue());
            }

            dcArgs = "({0})".Fmt(dcArgs);
        }
        sb.AppendLine("// @DataContract{0}".Fmt(dcArgs));
    }

    public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
    {
        if (dmMeta == null)
        {
            if (Config.AddDataContractAttributes)
            {
                sb.AppendLine(Config.AddIndexesToDataMembers
                    ? "// @DataMember(Order={0})".Fmt(dataMemberIndex)
                    : "// @DataMember()");
                return true;
            }
            return false;
        }

        var dmArgs = "";
        if (dmMeta.Name != null
            || dmMeta.Order != null
            || dmMeta.IsRequired != null
            || dmMeta.EmitDefaultValue != null
            || Config.AddIndexesToDataMembers)
        {
            if (dmMeta.Name != null)
                dmArgs = "Name={0}".Fmt(dmMeta.Name.QuotedSafeValue());

            if (dmMeta.Order != null || Config.AddIndexesToDataMembers)
            {
                if (dmArgs.Length > 0)
                    dmArgs += ", ";

                dmArgs += "Order={0}".Fmt(dmMeta.Order ?? dataMemberIndex);
            }

            if (dmMeta.IsRequired != null)
            {
                if (dmArgs.Length > 0)
                    dmArgs += ", ";

                dmArgs += "IsRequired={0}".Fmt(dmMeta.IsRequired.ToString().ToLower());
            }

            if (dmMeta.EmitDefaultValue != null)
            {
                if (dmArgs.Length > 0)
                    dmArgs += ", ";

                dmArgs += "EmitDefaultValue={0}".Fmt(dmMeta.EmitDefaultValue.ToString().ToLower());
            }

            dmArgs = "({0})".Fmt(dmArgs);
        }
        sb.AppendLine("// @DataMember{0}".Fmt(dmArgs));

        return true;
    }

    public string GenericArg(string arg)
    {
        return ConvertFromCSharp(arg.TrimStart('\'').ParseTypeIntoNodes());
    }

    public string ConvertFromCSharp(TextNode node)
    {
        var sb = new StringBuilder();

        if (node.Text == "Nullable")
            return "?" + TypeAlias(node.Children[0].Text);

        if (node.Text == "List")
        {
            sb.Append("[]");
            sb.Append(ConvertFromCSharp(node.Children[0]));
        }
        else if (node.Text == "List`1")
        {
            var type = node.Children.Count > 0 ? node.Children[0].Text : "[]const u8";
            sb.Append("[]").Append(type);
        }
        else if (node.Text == "Dictionary")
        {
            sb.Append("std.StringHashMap(");
            sb.Append(ConvertFromCSharp(node.Children[1]));
            sb.Append(")");
        }
        else
        {
            if (node.Text == "Tuple")
                node.Text += "`" + node.Children.Count;

            sb.Append(TypeAlias(node.Text));
            if (node.Children.Count > 0)
            {
                sb.Append("(");
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var childNode = node.Children[i];

                    if (i > 0)
                        sb.Append(", ");

                    sb.Append(ConvertFromCSharp(childNode));
                }
                sb.Append(")");
            }
        }

        return sb.ToString();
    }

    public string GetPropertyName(string name) => name.SafeToken().PropertyStyle();
    public string GetPropertyName(MetadataPropertyType prop) =>
        prop.GetSerializedAlias() ?? prop.Name.SafeToken().PropertyStyle();
}

public static class ZigGeneratorExtensions
{
    public static string InReturnMarker(this string type)
    {
        var useType = ZigGenerator.ReturnMarkerFilter?.Invoke(type);
        if (useType != null)
            return useType;

        return type;
    }

    public static string PropertyStyle(this string name)
    {
        // Zig uses snake_case for struct fields by convention
        return JsConfig.TextCase == TextCase.CamelCase
            ? name.ToCamelCase()
            : JsConfig.TextCase == TextCase.SnakeCase
                ? name.ToLowercaseUnderscore()
                : name.ToLowercaseUnderscore(); // Default to snake_case for Zig
    }
}