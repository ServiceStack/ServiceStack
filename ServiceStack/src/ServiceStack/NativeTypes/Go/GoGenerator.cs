using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Go;

public class GoGenerator : ILangGenerator
{
    public readonly MetadataTypesConfig Config;
    readonly NativeTypesFeature feature;
    public List<string> ConflictTypeNames = new();
    public List<MetadataType> AllTypes { get; set; }

    public GoGenerator(MetadataTypesConfig config)
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
    };

    public static Dictionary<string, string> TypeAliases = new() {
        {"String", "string"},
        {"Boolean", "bool"},
        {"DateTime", "time.Time"},
        {"DateOnly", "time.Time"},
        {"DateTimeOffset", "time.Time"},
        {"TimeSpan", "time.Duration"},
        {"TimeOnly", "time.Duration"},
        {"Guid", "string"},
        {"Char", "string"},
        {"Byte", "byte"},
        {"Int16", "int16"},
        {"Int32", "int"},
        {"Int64", "int64"},
        {"UInt16", "uint16"},
        {"UInt32", "uint32"},
        {"UInt64", "uint64"},
        {"Single", "float32"},
        {"Double", "float64"},
        {"Decimal", "float64"},
        {"IntPtr", "int64"},
        {"Byte[]", "[]byte"},
        {"Stream", "[]byte"},
        {"HttpWebResponse", "[]byte"},
        {"Uri", "string"},
        {"Type", "string"},
    };

    internal static readonly Dictionary<string, string> primitiveDefaultValues = new() {
        {"string", "\"\""},
        {"bool", "false"},
        {"time.Time", "time.Time{}"},
        {"time.Duration", "0"},
        {"byte", "0"},
        {"int16", "0"},
        {"int", "0"},
        {"int64", "0"},
        {"uint16", "0"},
        {"uint32", "0"},
        {"uint64", "0"},
        {"float32", "0"},
        {"float64", "0"},
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

    public static Func<GoGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    /// <summary>
    /// Whether property should be marked optional (pointer types in Go)
    /// </summary>
    public static Func<GoGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
        DefaultIsPropertyOptional;

    public void Init(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        AllTypes = metadata.GetAllTypesOrdered();
        AllTypes.RemoveAll(x => x.IgnoreType(Config, includeList));
        AllTypes = FilterTypes(AllTypes);

        //Go doesn't support generics in the same way, track conflicts
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

        var packageName = Config.GlobalNamespace ?? "dtos";

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

        var header = AddHeader?.Invoke(request);
        if (!string.IsNullOrEmpty(header))
            sb.AppendLine(header);

        // Go package declaration
        sb.AppendLine($"package {packageName.SafeToken()}");
        sb.AppendLine();

        string lastNS = null;

        var existingTypes = new HashSet<string>();

        var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
        var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
        var responseTypes = metadata.Operations
            .Where(x => x.Response != null)
            .Select(x => x.Response).ToSet();

        var insertCode = InsertCodeFilter?.Invoke(AllTypes, Config);
        if (insertCode != null)
            sb.AppendLine(insertCode);


        // If DefaultImports is not specified, add time import if needed
        if (request.QueryString["DefaultImports"].IsNullOrEmpty())
        {
            foreach (var metaType in AllTypes)
            {
                foreach (var metaProp in metaType.Properties.Safe())
                {
                    var typeAlias = TypeAlias(metaProp.Type);
                    if (typeAlias.StartsWith("time."))
                    {
                        defaultImports.AddIfNotExists("time");
                    }
                }
            }
        }
        
        // Add imports
        if (defaultImports.Count > 0)
        {
            sb.AppendLine("import (");
            sb = sb.Indent();
            foreach (var import in defaultImports)
            {
                sb.AppendLine($"\"{import}\"");
            }
            sb = sb.UnIndent();
            sb.AppendLine(")");
            sb.AppendLine();
        }
        
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

        sb.Emit(type, Lang.Go);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            // Go enums are typically constants
            var typeName = Type(type.Name, type.GenericArgs);
            var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty();
            var enumType = isIntEnum ? "int" : "string";

            sb.AppendLine($"type {typeName} {enumType}");
            sb.AppendLine();

            if (type.EnumNames != null && type.EnumNames.Count > 0)
            {
                sb.AppendLine("const (");
                sb = sb.Indent();

                for (var i = 0; i < type.EnumNames.Count; i++)
                {
                    var name = type.EnumNames[i];
                    var value = type.EnumValues?[i];
                    var memberValue = type.GetEnumMemberValue(i);

                    if (i == 0)
                    {
                        if (isIntEnum)
                        {
                            sb.AppendLine($"{typeName}{name} {typeName} = {value ?? i.ToString()}");
                        }
                        else
                        {
                            var strValue = memberValue ?? name;
                            sb.AppendLine($"{typeName}{name} {typeName} = \"{strValue}\"");
                        }
                    }
                    else
                    {
                        if (isIntEnum)
                        {
                            sb.AppendLine($"{typeName}{name} {typeName} = {value ?? i.ToString()}");
                        }
                        else
                        {
                            var strValue = memberValue ?? name;
                            sb.AppendLine($"{typeName}{name} = \"{strValue}\"");
                        }
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine(")");
            }
        }
        else
        {
            // Go struct
            var typeName = Type(type.Name, type.GenericArgs);

            sb.AppendLine($"type {typeName} struct {{");
            sb = sb.Indent();

            InnerTypeFilter?.Invoke(sb, type);

            // Add embedded base type if inherits
            if (type.Inherits != null)
            {
                var baseType = Type(type.Inherits.Name, type.Inherits.GenericArgs);
                sb.AppendLine(baseType);
            }

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine($"Version int `json:\"version\"` //{Config.AddImplicitVersion}");
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
        var dataMemberIndex = 1;
        if (type.Properties != null)
        {
            foreach (var prop in type.Properties)
            {
                var propType = GetPropertyType(prop, out var isNullable);
                propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                // In Go, use pointer types for optional/nullable properties
                var usePointer = IsPropertyOptional(this, type, prop) ?? isNullable;
                if (usePointer && !propType.StartsWith("*") && !propType.StartsWith("[]") && !propType.StartsWith("map["))
                {
                    propType = "*" + propType;
                }

                AppendComments(sb, prop.Description);
                AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                AppendAttributes(sb, prop.Attributes);

                var fieldName = GetPropertyName(prop);
                var jsonFieldName = prop.GetSerializedAlias() ?? prop.Name.ToCamelCase();

                // Build JSON tag
                var jsonTag = $"`json:\"{jsonFieldName}";
                if (usePointer || !prop.IsRequired.GetValueOrDefault())
                {
                    jsonTag += ",omitempty";
                }
                jsonTag += "\"`";

                sb.Emit(prop, Lang.Go);
                PrePropertyFilter?.Invoke(sb, prop, type);
                sb.AppendLine($"{fieldName} {propType} {jsonTag}");
                PostPropertyFilter?.Invoke(sb, prop, type);
            }
        }

        if (includeResponseStatus)
        {
            sb.AppendLine($"ResponseStatus *ResponseStatus `json:\"responseStatus,omitempty\"`");
        }
    }

    public static bool? DefaultIsPropertyOptional(GoGenerator generator, MetadataType type, MetadataPropertyType prop)
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

        return Type(type, genericArgs);
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
            {
                // In Go, nullable is represented with pointer
                cooked = "*{0}".Fmt(GenericArg(genericArgs[0]));
            }
            else if (type == "Nullable`1[]")
            {
                cooked = "[]*" + GenericArg(genericArgs[0]);
            }
            else if (ArrayTypes.Contains(type))
            {
                cooked = "[]{0}".Fmt(GenericArg(genericArgs[0]));
            }
            else if (DictionaryTypes.Contains(type))
            {
                var keyType = GenericArg(genericArgs[0]);
                var valType = GenericArg(genericArgs[1]);
                cooked = $"map[{keyType}]{valType}";
            }
            else
            {
                // Go doesn't support generics in the same way, use interface{} or specific type
                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    // For generic types, just use the base name
                    cooked = TypeAlias(parts[0]);
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
            // Go array syntax: []Type
            return "[]{0}".Fmt(TypeAlias(arrParts[0]));
        }

        // Handle generic type parameters (single uppercase letter like T)
        // Convert them to interface{} since Go doesn't support generic type parameters in the same way
        if (type.Length == 1 && char.IsUpper(type[0]))
        {
            return "interface{}";
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
            sb.AppendLine("/** @description {0}".Fmt(desc.SafeComment()) + " */");
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
            return TypeAlias(node.Children[0].Text);

        if (node.Text == "List")
        {
            sb.Append(ConvertFromCSharp(node.Children[0]));
            sb.Append("[]");
        }
        else if (node.Text == "List`1")
        {
            var type = node.Children.Count > 0 ? node.Children[0].Text : "any";
            sb.Append(type).Append("[]");
        }
        else if (node.Text == "Dictionary")
        {
            sb.Append("{ [index:");
            var keyType = ConvertFromCSharp(node.Children[0]);
            sb.Append(GetKeyType(keyType));
            sb.Append("]: ");
            sb.Append(ConvertFromCSharp(node.Children[1]));
            sb.Append("; }");
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
                        sb.Append(",");

                    sb.Append(ConvertFromCSharp(childNode));
                }
                sb.Append(">");
            }
        }

        return sb.ToString();
    }

    private static string GetKeyType(string keyType)
    {
        var jsKeyType = AllowedKeyTypes.Contains(keyType)
            ? keyType
            : "string";
        return jsKeyType;
    }

    public string GetPropertyName(string name) => name.SafeToken().GoPropertyStyle();
    public string GetPropertyName(MetadataPropertyType prop)
    {
        var name = prop.GetSerializedAlias() ?? prop.Name;
        return name.SafeToken().GoPropertyStyle();
    }
}

public static class GoGeneratorExtensions
{
    public static string InReturnMarker(this string type)
    {
        var useType = GoGenerator.ReturnMarkerFilter?.Invoke(type);
        if (useType != null)
            return useType;

        if (type.StartsWith("{"))
            return "any";

        var pos = type.IndexOf("<{", StringComparison.Ordinal);
        if (pos >= 0)
        {
            var ret = type.LeftPart("<{") + "<any>" + type.LastRightPart("}>");
            return ret;
        }

        //Note: can only implement using Array short-hand notation: IReturn<Type[]>

        return type;
    }

    public static string PropertyStyle(this string name)
    {
        return JsConfig.TextCase == TextCase.CamelCase
            ? name.ToCamelCase()
            : JsConfig.TextCase == TextCase.SnakeCase
                ? name.ToLowercaseUnderscore()
                : name;
    }

    /// <summary>
    /// Convert property name to Go-style exported field name.
    /// In Go, exported fields must start with an uppercase letter.
    /// Go keywords are all lowercase, so capitalizing them makes them valid identifiers.
    /// </summary>
    public static string GoPropertyStyle(this string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Convert to PascalCase for Go exported fields
        // This automatically handles Go keywords since they're all lowercase
        // (e.g., "type" -> "Type", "func" -> "Func", "interface" -> "Interface")
        return name.ToPascalCase();
    }
}