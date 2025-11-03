using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Rust;

public class RustGenerator : ILangGenerator
{
    public Lang Lang => Lang.Rust;
    public MetadataTypesConfig Config { get; }

    readonly NativeTypesFeature feature;
    public List<string> ConflictTypeNames = new();
    public List<MetadataType> AllTypes { get; set; }

    public RustGenerator(MetadataTypesConfig config)
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
        "serde::{Serialize, Deserialize}",
        "std::collections::HashMap",
    };

    public static Dictionary<string, string> TypeAliases = new() {
        {"String", "String"},
        {"Boolean", "bool"},
        {"DateTime", "String"},  // Will use String for DateTime, can be customized
        {"DateOnly", "String"},
        {"DateTimeOffset", "String"},
        {"TimeSpan", "String"},
        {"TimeOnly", "String"},
        {"Guid", "String"},
        {"Char", "char"},
        {"Byte", "u8"},
        {"Int16", "i16"},
        {"Int32", "i32"},
        {"Int64", "i64"},
        {"UInt16", "u16"},
        {"UInt32", "u32"},
        {"UInt64", "u64"},
        {"Single", "f32"},
        {"Double", "f64"},
        {"Decimal", "f64"},  // Rust doesn't have built-in decimal, use f64
        {"IntPtr", "i64"},
        {"Byte[]", "Vec<u8>"},
        {"Stream", "Vec<u8>"},
        {"HttpWebResponse", "Vec<u8>"},
        {"Uri", "String"},
        {"Type", "String"},
    };

    public static Dictionary<string, string> ReturnTypeAliases = new() {
        {"Byte[]", "Vec<u8>"},
        {"Stream", "Vec<u8>"},
        {"HttpWebResponse", "Vec<u8>"},
    };

    // Rust keywords that need to be escaped with r# prefix
    public static HashSet<string> RustKeywords = new() {
        "as", "break", "const", "continue", "crate", "else", "enum", "extern",
        "false", "fn", "for", "if", "impl", "in", "let", "loop", "match",
        "mod", "move", "mut", "pub", "ref", "return", "self", "Self", "static",
        "struct", "super", "trait", "true", "type", "unsafe", "use", "where",
        "while", "async", "await", "dyn", "abstract", "become", "box", "do",
        "final", "macro", "override", "priv", "typeof", "unsized", "virtual", "yield"
    };

    private static string declaredEmptyString = "String::new()";
    internal static readonly Dictionary<string, string> primitiveDefaultValues = new() {
        {"String", declaredEmptyString},
        {"bool", "false"},
        {"char", "'\\0'"},
        {"u8", "0"},
        {"i16", "0"},
        {"i32", "0"},
        {"i64", "0"},
        {"u16", "0"},
        {"u32", "0"},
        {"u64", "0"},
        {"f32", "0.0"},
        {"f64", "0.0"},
        {"Vec", "Vec::new()"},
    };
    
    public static List<string> IgnoreAttributeNames { get; set; } =
    [
        "DataContract",
        "DataMember"
    ];

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

    public static Func<RustGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    /// <summary>
    /// Whether property should be marked optional
    /// </summary>
    public static Func<RustGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
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
        var formatter = request.TryResolve<INativeTypesFormatter>();
        Init(metadata);

        List<string> defaultImports = new(!Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports);

        var globalNamespace = Config.GlobalNamespace;

        string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);
        var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
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

            sb.AppendLine("*/");
            sb.AppendLine();
        }

        formatter?.AddHeader(sb, this, request);

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
            sb.AppendLine($"use {import};");
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
        
        var ret = StringBuilderCache.ReturnAndFree(sbInner);
        return formatter != null ? formatter.Transform(ret, this, request) : ret;
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

        sb.Emit(type, Lang.Rust);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty();

            // Rust enums with #[derive(Serialize, Deserialize)]
            sb.AppendLine("#[derive(Serialize, Deserialize, Debug, Clone, PartialEq)]");

            if (isIntEnum)
            {
                // For integer enums, use repr attribute
                sb.AppendLine("#[repr(i32)]");
            }

            sb.AppendLine($"pub enum {Type(type.Name, type.GenericArgs)} {{");
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
                        sb.AppendLine($"#[serde(rename = \"{memberValue}\")]");
                        sb.AppendLine($"{name},");
                        continue;
                    }

                    if (value != null && isIntEnum)
                    {
                        sb.AppendLine($"{name} = {value},");
                    }
                    else
                    {
                        // For string enums, use serde rename
                        if (!isIntEnum)
                        {
                            sb.AppendLine($"#[serde(rename = \"{name}\")]");
                        }
                        sb.AppendLine($"{name},");
                    }
                }
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
        }
        else
        {
            // Rust struct generation
            sb.AppendLine("#[derive(Serialize, Deserialize, Debug, Clone, PartialEq)]");

            var typeName = Type(type.Name, type.GenericArgs);
            sb.AppendLine($"pub struct {typeName} {{");

            sb = sb.Indent();
            InnerTypeFilter?.Invoke(sb, type);

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine("pub {0}: i32, //{1}".Fmt(
                    GetPropertyName("Version"), Config.AddImplicitVersion));
            }

            AddProperties(sb, type,
                includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                                                                && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

            sb = sb.UnIndent();
            sb.AppendLine("}");
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
                var propType = GetPropertyType(prop, out var optionalProperty);
                propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                var isOptional = IsPropertyOptional(this, type, prop) ?? optionalProperty;

                wasAdded = AppendComments(sb, prop.Description);
                wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                // Determine the serialized name (what appears in JSON/wire format)
                var serializedName = (prop.GetSerializedAlias() ?? prop.Name).ToCamelCase();

                // Get the Rust field name (snake_case)
                var rustFieldName = GetPropertyName(prop);

                // Remove r# prefix if present for comparison
                var rustFieldNameWithoutPrefix = rustFieldName.StartsWith("r#")
                    ? rustFieldName.Substring(2)
                    : rustFieldName;

                // Add serde rename if the Rust field name differs from serialized name
                // This handles both explicit DataMember renames and snake_case conversion
                if (serializedName != rustFieldNameWithoutPrefix)
                {
                    sb.AppendLine($"#[serde(rename = \"{serializedName}\")]");
                }

                // Handle optional fields with Option<T>
                if (isOptional)
                {
                    propType = $"Option<{propType}>";
                }

                sb.Emit(prop, Lang.Rust);
                PrePropertyFilter?.Invoke(sb, prop, type);
                sb.AppendLine("pub {0}: {1},".Fmt(rustFieldName, propType));
                PostPropertyFilter?.Invoke(sb, prop, type);
            }
        }

        if (includeResponseStatus)
        {
            AppendDataMember(sb, null, dataMemberIndex++);
            var responseStatusFieldName = GetPropertyName(nameof(ResponseStatus));
            if (nameof(ResponseStatus) != responseStatusFieldName)
            {
                sb.AppendLine($"#[serde(rename = \"{nameof(ResponseStatus)}\")]");
            }
            sb.AppendLine("pub {0}: Option<ResponseStatus>,".Fmt(responseStatusFieldName));
        }

        // Add PhantomData for generic types with no properties
        if (type.GenericArgs?.Length > 0 &&
            (type.Properties == null || type.Properties.Count == 0) &&
            !includeResponseStatus)
        {
            for (int i = 0; i < type.GenericArgs.Length; i++)
            {
                sb.AppendLine("#[serde(skip)]");
                sb.AppendLine($"pub _phantom{(i > 0 ? i.ToString() : "")}: std::marker::PhantomData<{type.GenericArgs[i]}>,");
            }
        }
    }

    public static bool? DefaultIsPropertyOptional(RustGenerator generator, MetadataType type, MetadataPropertyType prop)
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
                sb.AppendLine("// {0}".Fmt(attr.Name));
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
                sb.AppendLine("// {0}({1})".Fmt(attr.Name, StringBuilderCacheAlt.ReturnAndFree(args)));
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
                cooked = "Vec<{0}>".Fmt(GenericArg(genericArgs[0])).StripNullable();
            }
            if (DictionaryTypes.Contains(type))
            {
                var keyType = GenericArg(genericArgs[0]);
                var valType = GenericArg(genericArgs[1]);
                cooked = "HashMap<{0}, {1}>".Fmt(keyType, valType);
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
                cooked = "Option<{0}>".Fmt(GenericArg(genericArgs[0]));
            else if (type == "Nullable`1[]")
                cooked = "Vec<Option<{0}>>".Fmt(GenericArg(genericArgs[0]));
            else if (ArrayTypes.Contains(type))
                cooked = "Vec<{0}>".Fmt(GenericArg(genericArgs[0])).StripNullable();
            else if (DictionaryTypes.Contains(type))
            {
                var keyType = GenericArg(genericArgs[0]);
                var valType = GenericArg(genericArgs[1]);
                cooked = "HashMap<{0}, {1}>".Fmt(keyType, valType);
            }
            else
            {
                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = StringBuilderCacheAlt.Allocate();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(GenericArg(arg));
                    }
                    var genericArgsList = StringBuilderCacheAlt.ReturnAndFree(args);

                    var typeName = TypeAlias(type);

                    cooked = $"{typeName}<{genericArgsList}>";
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
            // Convert C# array syntax to Rust Vec
            var baseType = TypeAlias(arrParts[0]);
            var numberOfDimensions = type.Count(c => c == '[');

            var result = baseType;
            for (int i = 0; i < numberOfDimensions; i++)
            {
                result = $"Vec<{result}>";
            }
            return result;
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
            // Use Rust doc comments
            sb.AppendLine("/// {0}".Fmt(desc.SafeComment()));
        }
        return false;
    }

    public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
    {
        if (IgnoreAttributeNames.Contains("DataContract"))
            return;
        
        if (dcMeta == null)
        {
            if (Config.AddDataContractAttributes)
                sb.AppendLine("// DataContract");
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
        sb.AppendLine("// DataContract{0}".Fmt(dcArgs));
    }

    public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
    {
        if (IgnoreAttributeNames.Contains("DataMember"))
            return false;
        
        if (dmMeta == null)
        {
            if (Config.AddDataContractAttributes)
            {
                sb.AppendLine(Config.AddIndexesToDataMembers
                    ? "// DataMember(Order={0})".Fmt(dataMemberIndex)
                    : "// DataMember");
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
        sb.AppendLine("// DataMember{0}".Fmt(dmArgs));

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
            return "Option<{0}>".Fmt(TypeAlias(node.Children[0].Text));

        if (node.Text == "List")
        {
            sb.Append("Vec<");
            sb.Append(ConvertFromCSharp(node.Children[0]));
            sb.Append(">");
        }
        else if (node.Text == "List`1")
        {
            var type = node.Children.Count > 0 ? ConvertFromCSharp(node.Children[0]) : "String";
            sb.Append("Vec<").Append(type).Append(">");
        }
        else if (node.Text == "Dictionary")
        {
            sb.Append("HashMap<");
            sb.Append(ConvertFromCSharp(node.Children[0]));
            sb.Append(", ");
            sb.Append(ConvertFromCSharp(node.Children[1]));
            sb.Append(">");
        }
        else
        {
            if (node.Text == "Tuple")
                node.Text += "`" + node.Children.Count;

            sb.Append(TypeAlias(node.Text));
            if (node.Children.Count > 0)
            {
                sb.Append("<");
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var childNode = node.Children[i];

                    if (i > 0)
                        sb.Append(", ");

                    sb.Append(ConvertFromCSharp(childNode));
                }
                sb.Append(">");
            }
        }

        return sb.ToString();
    }

    public string EscapeKeyword(string name) => RustKeywords.Contains(name) ? $"r#{name}" : name;

    public string GetPropertyName(string name) => EscapeKeyword(name.SafeToken().PropertyStyle());
    public string GetPropertyName(MetadataPropertyType prop) =>
        EscapeKeyword(prop.Name.SafeToken().PropertyStyle());
}

public static class RustGeneratorExtensions
{
    public static string InReturnMarker(this string type)
    {
        var useType = RustGenerator.ReturnMarkerFilter?.Invoke(type);
        if (useType != null)
            return useType;

        return type;
    }

    public static string PropertyStyle(this string name)
    {
        // Rust always uses snake_case for field names
        return name.ToLowercaseUnderscore();
    }
}