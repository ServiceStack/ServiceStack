using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Php;

public class PhpGenerator : ILangGenerator
{
    public readonly MetadataTypesConfig Config;
    readonly NativeTypesFeature feature;
    public List<string> ConflictTypeNames = new();
    public List<MetadataType> AllTypes { get; set; }
    public List<MetadataType> BuiltInTypes { get; set; }

    public PhpGenerator(MetadataTypesConfig config)
    {
        Config = config;
        feature = HostContext.GetPlugin<NativeTypesFeature>();
    }
    public static bool GenerateServiceStackTypes => IgnoreTypeInfosFor.Count == 0;
        
    public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
        
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }
        
    public static List<string> DefaultImports = new() {
    };

    public static List<string> ServiceStackImports = new() {
        "DateTime",
        "Exception",
        "DateInterval",
        "JsonSerializable",
        "ServiceStack\\{IReturn,IReturnVoid,IGet,IPost,IPut,IDelete,IPatch,IMeta,IHasSessionId,IHasBearerToken,IHasVersion}",
        "ServiceStack\\{ICrud,ICreateDb,IUpdateDb,IPatchDb,IDeleteDb,ISaveDb,AuditBase,QueryDb,QueryDb2,QueryData,QueryData2,QueryResponse}",
        "ServiceStack\\{ResponseStatus,ResponseError,EmptyResponse,IdResponse,ArrayList,KeyValuePair2,StringResponse,StringsResponse,Tuple2,Tuple3,ByteArray}",
        "ServiceStack\\{JsonConverters,Returns,TypeContext}",
    };
        
    //In _builtInTypes servicestack dart library 
    public static HashSet<string> IgnoreTypeInfosFor = new() {
        "dynamic",
        "String",
        "int",
        "bool",
        "double",
        "Map<String,String>",
        "List<String>",
        "List<int>",
        "List<double>",
        "DateTime",
        "Duration",
        "Tuple<T1,T2>",
        "Tuple2<T1,T2>",
        "Tuple3<T1,T2,T3>",
        "Tuple4<T1,T2,T3,T4>",
        "KeyValuePair<K,V>",
        "KeyValuePair<String,String>",
        "ResponseStatus",
        "ResponseError",
        "List<ResponseError>",
        "QueryBase",
        "QueryData<T>",
        "QueryDb<T>",
        "QueryDb1<T>",
        "QueryDb2<From,Into>",
        "QueryResponse<T>",
        "List<UserApiKey>",
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
        "List<NavItem>",
        "Map<String,List<NavItem>>",
        nameof(NavItem),
        nameof(GetNavItems),
        nameof(GetNavItemsResponse),
        nameof(EmptyResponse),
        nameof(IdResponse),
        nameof(StringResponse),
        nameof(StringsResponse),
        nameof(AuditBase),
    };
    
    public static Dictionary<string, string> TypeAliases = new() {
        {"String", "string"},
        {"Boolean", "bool"},
        {"DateTime", "DateTime"},
        {"DateOnly", "DateTime"},
        {"DateTimeOffset", "DateTime"},
        {"TimeSpan", "DateInterval"},
        {"TimeOnly", "DateInterval"},
        {"Guid", "string"},
        {"Char", "string"},
        {"Byte", "int"},
        {"Int16", "int"},
        {"Int32", "int"},
        {"Int64", "int"},
        {"UInt16", "int"},
        {"UInt32", "int"},
        {"UInt64", "int"},
        {"Single", "float"},
        {"Double", "float"},
        {"Decimal", "float"},
        {"IntPtr", "int"},
        {"List", "array"},
        {"Byte[]", "ByteArray"},
        {"Stream", "ByteArray"},
        {"HttpWebResponse", "ByteArray"},
        {"IDictionary", "array"},
        {"OrderedDictionary", "array"},
        {"Uri", "string"},
        {"Type", "string"},
    };

    public static Dictionary<string, string> ReturnTypeAliases = new() {
        {"Byte[]", "ByteArray"},
        {"Stream", "ByteArray"},
        {"HttpWebResponse", "ByteArray"},
    };
        
    private static string declaredEmptyString = "''";
    internal static readonly Dictionary<string, string> primitiveDefaultValues = new() {
        {"String", declaredEmptyString},
        {"string", declaredEmptyString},
        {"Boolean", "false"},
        {"boolean", "false"},
        {"DateTime", "new DateTime()"},
        {"DateOnly", "new DateTime()"},
        {"DateTimeOffset", "new DateTime()"},
        {"TimeSpan", "new DateInterval('P0D')"},
        {"TimeOnly", "new DateInterval('P0D')"},
        {"Guid", declaredEmptyString},
        {"Char", declaredEmptyString},
        {"int", "0"},
        {"float", "0.0"},
        {"double", "0"},
        {"Byte", "0"},
        {"Int16", "0"},
        {"Int32", "0"},
        {"Int64", "0"},
        {"UInt16", "0"},
        {"UInt32", "0"},
        {"UInt64", "0"},
        {"Single", "0"},
        {"Double", "0.0"},
        {"Decimal", "0.0"},
        {"IntPtr", "0"},
        {"List", "[]"},
    };

    public static HashSet<string> ConvertValueTypes { get; set; } = new()
    {
        "DateTime",
        "DateOnly",
        "DateTimeOffset",
        "TimeSpan",
        "TimeOnly",
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

    public HashSet<string> AddedDeclarations { get; set; } = new();

    public static Func<PhpGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    /// <summary>
    /// Whether property should be marked optional
    /// </summary>
    public static Func<PhpGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
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

        BuiltInTypes = NativeTypesService.BuiltInClientDtos.Map(x => x.ToMetadataType());
    }

    public MetadataType FindType(MetadataTypeName typeRef) =>
        typeRef == null ? null : FindType(typeRef.Name, typeRef.Namespace); 
    public MetadataType FindType(string name, string @namespace = null)
    {
        var type = AllTypes.FirstOrDefault(x => x.Name == name
                && (@namespace == null || @namespace == x.Namespace))
            ?? BuiltInTypes.FirstOrDefault(x => x.Name == name
                && (@namespace == null || @namespace == x.Namespace));
        return type;
    }

    public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
    {
        Init(metadata);

        var defaultImports = !Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports;

        var globalNamespace = Config.GlobalNamespace;

        string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);

        sb.AppendLine(!string.IsNullOrEmpty(globalNamespace)
            ? "<?php namespace {0};".Fmt(globalNamespace.SafeToken())
            : "<?php");

        var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            if (Config.UsePath != null)
                sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));

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

        string lastNS = null;

        var existingTypes = new HashSet<string>(IgnoreTypeInfosFor);
        //var existingTypes = new HashSet<string>();

        var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
        var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
        var responseTypes = metadata.Operations
            .Where(x => x.Response != null)
            .Select(x => x.Response).ToSet();
        var types = metadata.Types.CreateSortedTypeList();

        sb.AppendLine();

        foreach (var import in ServiceStackImports)
        {
            sb.AppendLine($"use {import};");
        }
        foreach (var import in defaultImports)
        {
            sb.AppendLine($"use {import};");
        }
        sb.AppendLine();

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
            else if (types.Contains(type) && !existingTypes.Contains(fullTypeName))
            {
                lastNS = AppendType(ref sb, type, lastNS,
                    new CreateTypeOptions { IsType = true });

                existingTypes.Add(fullTypeName);
            }
        }

        var addCode = AddCodeFilter?.Invoke(AllTypes, Config);
        if (addCode != null)
            sb.AppendLine(addCode);
            
        sb.AppendLine();

        return StringBuilderCache.ReturnAndFree(sbInner);
    }

    public string GetDefaultInitializer(string typeName)
    {
        // This is to avoid invalid syntax such as "return new string()"
        if (typeName == "any")
            return "(object)[]";
        if (typeName.EndsWith("[]"))
            return "[]";

        if (primitiveDefaultValues.TryGetValue(typeName, out var useValue))
            return useValue;

        return null;
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
        AppendDataContract(sb, type.DataContract);

        sb.Emit(type, Lang.Php);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty(); 
            var enumType = isIntEnum || type.EnumValues != null ? "int" : "string";
            sb.AppendLine($"enum {Type(type.Name, type.GenericArgs)} : {enumType}");
            sb.AppendLine("{");
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
                        sb.AppendLine($"{name} = '{memberValue}',");
                        continue;
                    }

                    sb.AppendLine(value == null 
                        ? $"case {name} = '{name}';"
                        : $"case {name} = {value};");
                }
            }
            sb = sb.UnIndent();
            sb.AppendLine("}");
        }
        else
        {
            var templates = new Dictionary<string, string>();
            var extends = new List<string>();

            //: BaseClass, Interfaces
            if (type.Inherits != null)
            {
                var baseType = DeclarationType(type.Inherits.Name, type.Inherits.GenericArgs);
                if (type.Inherits.GenericArgs?.Length > 0)
                {
                    var name = baseType.LeftPart('<');
                    extends.Add(name);
                    for (var i = 0; i < type.Inherits.GenericArgs.Length; i++)
                    {
                        templates[i == 0 ? name : name + i] = type.Inherits.GenericArgs[i];
                    }
                }
                else
                {
                    extends.Add(baseType);
                }
            }

            string responseTypeExpression = options?.Op != null
                ? "public function createResponse():void {}"
                : null;
            string responseMethod = options?.Op?.Method != null
                ? $"public function getMethod(): string {{ return '{options.Op.Method}'; }}"
                : null;

            string returnType = null;
            string returnTypeCls = null;
            var interfaces = new List<string>();
            var implStr = options.ImplementsFn?.Invoke();
            if (!string.IsNullOrEmpty(implStr))
            {
                interfaces.Add(implStr.LeftPart('<'));

                if (implStr.StartsWith("IReturn<"))
                {
                    var types = implStr.RightPart('<');
                    returnType = types.Substring(0, types.Length - 1);

                    var replaceReturnType = GetDefaultInitializer(returnType);

                    returnTypeCls = GetPhpType(returnType);
                    if (returnTypeCls.IndexOf('<') >= 0)
                    {
                        var argsCount = returnTypeCls.CountOccurrencesOf(',');
                        var args = returnTypeCls.RightPart('<').LastLeftPart('>').Split(',').Map(x => "'" + x + "'");
                        returnTypeCls = returnTypeCls.LeftPart('<') + (argsCount > 1 ? argsCount : "");
                        replaceReturnType = returnTypeCls + "::create(genericArgs:[" + string.Join(",", args) + "])";
                    }
                    if (replaceReturnType == "''")
                        replaceReturnType = "'string'";
                    if (returnTypeCls == "array")
                    {
                        var argTypes = implStr.RightPart('<').LastLeftPart('>').RightPart('<').LastLeftPart('>');
                        replaceReturnType = "ArrayList::create([" + 
                                string.Join(",", argTypes.Split(',').Map(x => '"' + x + '"')) 
                            + "])";
                    }
                    
                    responseTypeExpression = replaceReturnType == null ?
                        "public function createResponse(): mixed {{ return new {0}(); }}".Fmt(returnTypeCls) :
                        "public function createResponse(): mixed {{ return {0}; }}".Fmt(replaceReturnType);
                }
                else if (implStr == "IReturnVoid")
                {
                    responseTypeExpression = "public function createResponse(): void {}";
                }
            }

            foreach (var iface in type.Implements.Safe())
            {
                var ifaceType = Type(iface);
                if (iface.GenericArgs?.Length > 0)
                {
                    var name = ifaceType.LeftPart('<');
                    interfaces.Add(name);
                    for (var i = 0; i < iface.GenericArgs.Length; i++)
                    {
                        templates[i == 0 ? name : name + i] = iface.GenericArgs[i];
                    }
                }
                else
                {
                    interfaces.Add(ifaceType);
                }
            }

            var isClass = !type.IsInterface.GetValueOrDefault();

            if (isClass)
            {
                interfaces.Add("JsonSerializable");
            }
            var extendsListOf = type.Inherits?.Name == "List`1" ? type.Inherits.GenericArgs[0] : null;
            var extend = extends.Count > 0
                ? " extends " + (extendsListOf != null ? "\\ArrayObject" : extends[0])
                : "";

            if (interfaces.Count > 0)
            {
                if (isClass)
                {
                    extend += " implements " + string.Join(", ", interfaces.ToArray());
                }
                else
                {
                    if (string.IsNullOrEmpty(extend))
                        extend = " extends ";
                    else
                        extend += ", ";

                    extend += string.Join(", ", interfaces.ToArray());
                }
            }

            var typeDeclaration = isClass
                ? "class"
                : "interface";

            var typeName = GetPhpClassName(type.Name, type.GenericArgs);
            if (returnType != null)
            {
                sb.AppendLine($"#[Returns('{returnTypeCls ?? returnType}')]");
            }

            if (type.GenericArgs?.Length > 0 || (!isClass && type.Properties?.Count > 0) || templates.Count > 0)
            {
                sb.AppendLine("/**");
                if (!isClass && type.Properties?.Count > 0)
                {
                    foreach (var prop in type.Properties.Safe())
                    {
                        var propType = GetPropertyType(prop, out var optionalProperty);
                        propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;
                        if (IsPropertyOptional(this, type, prop) ?? optionalProperty)
                        {
                            propType += "|null";
                        }
                        sb.AppendLine(" * @property {0} ${1}".Fmt(propType, GetPropertyName(prop)));
                    }
                }
                if (type.GenericArgs?.Length > 0)
                {
                    foreach (var argType in type.GenericArgs)
                    {
                        sb.AppendLine($" * @template {argType}");
                    }
                }
                if (templates.Count > 0)
                {
                    foreach (var entry in templates)
                    {
                        sb.AppendLine($" * @template {entry.Key} of {entry.Value}");
                    }
                }
                sb.AppendLine(" */");
            }
            sb.AppendLine($"{typeDeclaration} {typeName}{extend}");
            sb.AppendLine("{");

            sb = sb.Indent();
            InnerTypeFilter?.Invoke(sb, type);

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (isClass && addVersionInfo)
            {
                sb.AppendLine("public int $version = {0};".Fmt(Config.AddImplicitVersion));
            }

            if (type.Name == "IReturn`1")
            {
                sb.AppendLine("function createResponse(): mixed;");
            }
            else if (type.Name == "IReturnVoid")
            {
                sb.AppendLine("function createResponse(): void;");
            }

            // PHP interfaces can't have properties
            if (extendsListOf != null)
            {
                var lines = new[]
                {
                    $"public function __construct({extendsListOf} ...$items) " + '{',
                    "    parent::__construct($items, \\ArrayObject::STD_PROP_LIST);",
                    "}",
                    "",
                    "/** @throws \\Exception */",
                    "public function append($value): void {",
                    $"    if ($value instanceof {extendsListOf})",
                    "        parent::append($value);",
                    "    else",
                    $"        throw new \\Exception(\"Can only append a {extendsListOf} to \" . __CLASS__);",
                    "}",
                    "",
                    "/** @throws Exception */",
                    "public function fromMap($o): void {",
                    "    foreach ($o as $item) {",
                    $"        $el = new {extendsListOf}();",
                    "        $el->fromMap($item);",
                    "        $this->append($el);",
                    "    }",
                    "}",
                    "",
                    "/** @throws Exception */",
                    "public function jsonSerialize(): array {",
                    "    return parent::getArrayCopy();",
                    "}",
                };
                foreach (var line in lines)
                {
                    sb.AppendLine(line);
                }
            }
            else if (isClass)
            {
                if (type.IsGenericTypeDef == true || type.GenericArgs?.Length > 0)
                {
                    sb.AppendLine("public array $genericArgs = [];");
                    sb.AppendLine($"public static function create(array $genericArgs=[]): {typeName} " + "{");
                    sb.AppendLine($"    $to = new {typeName}();");
                    sb.AppendLine("    $to->genericArgs = $genericArgs;");
                    sb.AppendLine("    return $to;");
                    sb.AppendLine("}");
                    sb.AppendLine();
                }
                
                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus 
                                           && options.IsResponse
                                           && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));
            }

            if (options?.Op != null)
            {
                sb.AppendLine("public function getTypeName(): string {{ return '{0}'; }}".Fmt(type.Name));
            }
            if (responseMethod != null)
            {
                sb.AppendLine(responseMethod);
            }
            if (responseTypeExpression != null)
            {
                sb.AppendLine(responseTypeExpression);
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
        }

        PostTypeFilter?.Invoke(sb, type);
            
        return lastNS;
    }

    public string GetPhpClassName(string type, string[] genericArgs = null)
    {
        var typeName = Type(type, null);
        //If GenericType has 1 arg, use same as TypeName, otherwise append arg types count to name 
        if (genericArgs?.Length > 1)
            typeName += genericArgs.Length;
        return typeName;
    }

    public string GetPhpType(MetadataPropertyType prop) => GetPhpType(GetPropertyType(prop, out _));
    public string GetPhpType(string typeName)
    {
        var cls = typeName.EqualsIgnoreCase("string")
            ? "string"
            : typeName.StartsWith("array")
                ? "array"
                : typeName.IndexOf('<') >= 0
                    ? $"{typeName}"
                    : primitiveDefaultValues.TryGetValue(typeName, out var defaultVal)
                        ? defaultVal == "0"
                            ? "int"
                            : defaultVal == "0.0"
                                ? "float"
                                : defaultVal == "false"
                                    ? "bool"
                                    : null
                        : null;
        return cls ?? typeName;
    }

    public virtual string GetPropertyType(MetadataPropertyType prop, out bool isNullable)
    {
        var propType = Type(prop.GetTypeName(Config, AllTypes), prop.GenericArgs);
        isNullable = propType.EndsWith("?");
        if (isNullable)
            propType = propType.Substring(0, propType.Length - 1);
        if (!isNullable)
            isNullable = prop.Type == "Nullable`1" || prop.IsValueType != true;
        return propType;
    }

    public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
    {
        var isGenericType = type.IsGenericTypeDef == true || type.GenericArgs?.Length > 0;
        var argIndexMap = new Dictionary<string, int>();
        if (isGenericType)
        {
            for (var argIndex = 0; argIndex < type.GenericArgs.Length; argIndex++)
            {
                argIndexMap[type.GenericArgs[argIndex]] = argIndex;
            }
        }
        string QuotedGenericArg(string type)
        {
            var phpType = GetPhpType(type);
            return argIndexMap.TryGetValue(phpType, out var index)
                ? $"$this->genericArgs[{index}]"
                : $"'{phpType}'";
        }
        
        var subTypes = new List<MetadataType>();
        var inheritsType = type.Inherits;
        while (inheritsType != null)
        {
            var subType = FindType(inheritsType);
            if (subType != null)
            {
                subTypes.Add(subType);
            }
            inheritsType = subType?.Inherits;
        }
        subTypes.Reverse();
        
        var toJsonLines = new List<string>
        {
            type.Inherits != null 
                ? "$o = parent::jsonSerialize();"
                : "$o = [];"
        };
            
        var hasProps = subTypes.Sum(x => x.Properties.Safe().Count()) + type.Properties.Safe().Count() > 0;
        if (hasProps)
        {
            var baseLines = new List<string>();
            var basePropNames = new List<string>();
            var hasBaseProps = subTypes.Sum(x => x.Properties.Safe().Count()) > 0;
            if (hasBaseProps)
            {
                var i = 0;
                sb.AppendLine("/**");
                foreach (var subType in subTypes)
                {
                    foreach (var prop in subType.Properties.Safe())
                    {
                        var propName = "$" + GetPropertyName(prop);
                        basePropNames.Add(propName);
                        var propType = GetPropertyType(prop, out var optionalProperty);
                        propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;
                        var isOptional = IsPropertyOptional(this, type, prop) ?? optionalProperty;
                        var defaultValue = (!isOptional ? GetDefaultInitializer(propType) : null) ?? "null";

                        sb.AppendLine(" * @param " + propType + (defaultValue == "null" ? "|null" : "") + $" {propName}");
                        baseLines.Add($"{propType.LeftPart('<')} {propName}={defaultValue},");
                    }
                }
                sb.AppendLine(" */");
            }
            
            sb.AppendLine("public function __construct(");
            sb = sb.Indent();
            foreach (var line in baseLines)
            {
                sb.AppendLine(line);
            }
            
            var wasAdded = false;
            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    var propName = "$" + GetPropertyName(prop);
                    if (basePropNames.Contains(propName))
                        continue;
                    
                    if (wasAdded) sb.AppendLine();
                    var propType = GetPropertyType(prop, out var optionalProperty);
                    propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;
                    var isOptional = IsPropertyOptional(this, type, prop) ?? optionalProperty;
                    var defaultValue = (!isOptional ? GetDefaultInitializer(propType) : null) ?? "null";

                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                    sb.Emit(prop, Lang.Php);
                    PrePropertyFilter?.Invoke(sb, prop, type);
                    
                    var phpType = !isGenericType
                        ? (isOptional || defaultValue == "null" ? "?" : "") + PhpPropType(prop, propType)
                        : "mixed";

                    var docType = !propType.StartsWith("array") && propType.IndexOf('<') >= 0
                        ? phpType.TrimStart('?') + '<' + propType.RightPart('<')
                        : propType;
                    sb.AppendLine("/** @var " + docType + (defaultValue == "null" ? "|null" : "") + " */");
                    sb.AppendLine($"public {phpType} {propName}={defaultValue},");
                    
                    PostPropertyFilter?.Invoke(sb, prop, type);
                }
            }
            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("/** @var ResponseStatus|null */");
                sb.AppendLine("public ${0}=ResponseStatus,".Fmt(GetPropertyName(nameof(ResponseStatus))));
            }
            sb.Chop(',');
        
            sb = sb.UnIndent();
            sb.AppendLine(") {");

            sb = sb.Indent();
            if (basePropNames.Count > 0)
            {
                sb.AppendLine($"parent::__construct({string.Join(",", basePropNames)});");
            }
            sb = sb.UnIndent();
            sb.AppendLine("}");

            sb.AppendLine();
            
            sb.AppendLine("/** @throws Exception */");
            sb.AppendLine("public function fromMap($o): void {");
            sb = sb.Indent();

            if (subTypes.Count > 0)
            {
                sb.AppendLine("parent::fromMap($o);");
            }

            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    var propName = GetPropertyName(prop);
                    var propType = GetPropertyType(prop, out var optionalProperty).TrimEnd('?');
                    
                    if ((prop.IsValueType == true || prop.Type == "String") 
                        && prop.IsEnum != true
                        && prop.GenericArgs.IsEmpty()
                        && !ConvertValueTypes.Contains(prop.Type))
                    {
                        sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = $o['{propName}'];");
                        toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = $this->{propName};");
                    }
                    else if (DictionaryTypes.Contains(prop.Type))
                    {
                        sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:[{QuotedGenericArg(prop.GenericArgs[0])},{QuotedGenericArg(prop.GenericArgs[1])}]), $o['{propName}']);");
                        toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:[{QuotedGenericArg(prop.GenericArgs[0])},{QuotedGenericArg(prop.GenericArgs[1])}]), $this->{propName});");
                    }
                    else if (ArrayTypes.Contains(prop.Type))
                    {
                        sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::fromArray({QuotedGenericArg(prop.GenericArgs[0])}, $o['{propName}']);");
                        toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::toArray({QuotedGenericArg(prop.GenericArgs[0])}, $this->{propName});");
                    }
                    else if (propType.EndsWith("[]"))
                    {
                        sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::fromArray({QuotedGenericArg(propType.LeftPart('['))}, $o['{propName}']);");
                        toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::toArray({QuotedGenericArg(propType.LeftPart('['))}, $this->{propName});");
                    }
                    else
                    {
                        var phpType = GetPhpType(prop);
                        if (isGenericType)
                        {
                            sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::from({QuotedGenericArg(GetPropertyType(prop, out _))}, $o['{propName}']);");
                            toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::to({QuotedGenericArg(GetPropertyType(prop, out _))}, $this->{propName});");
                        }
                        else if (phpType.StartsWith("array") && prop.GenericArgs?.Length > 0)
                        {
                            sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::fromArray({QuotedGenericArg(prop.GenericArgs[0])}, $o['{propName}']);");
                            toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::toArray({QuotedGenericArg(prop.GenericArgs[0])}, $this->{propName});");
                        }
                        else if (prop.GenericArgs?.Length > 0)
                        {
                            var clsName = GetPhpClassName(prop.Type, prop.GenericArgs);
                            if (clsName == "Nullable")
                            {
                                if (primitiveDefaultValues.ContainsKey(prop.GenericArgs[0]) 
                                    && !ConvertValueTypes.Contains(prop.Type))
                                {
                                    sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = $o['{propName}'];");
                                    toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = $this->{propName};");
                                }
                                else
                                {
                                    sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::from('{GetPhpType(prop.GenericArgs[0])}', $o['{propName}']);");
                                    toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::to('{GetPhpType(prop.GenericArgs[0])}', $this->{propName});");
                                }
                            }
                            else
                            {
                                var args = string.Join(",", prop.GenericArgs.Map(x => "'" + GetPhpType(x) + "'"));
                                sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::from(JsonConverters::context('{clsName}',genericArgs:[{args}]), $o['{propName}']);");
                                toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::to(JsonConverters::context('{clsName}',genericArgs:[{args}]), $this->{propName});");
                            }
                        }
                        else
                        {
                            sb.AppendLine($"if (isset($o['{propName}'])) $this->{propName} = JsonConverters::from('{phpType}', $o['{propName}']);");
                            toJsonLines.Add($"if (isset($this->{propName})) $o['{propName}'] = JsonConverters::to('{phpType}', $this->{propName});");
                        }
                    }
                }
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
        }
            
        if (type.Inherits?.Name != typeof(List<>).Name)
        {
            sb.AppendLine("");
            sb.AppendLine("/** @throws Exception */");
            sb.AppendLine("public function jsonSerialize(): array");
            sb.AppendLine("{");
            sb = sb.Indent();
            foreach (var line in toJsonLines)
            {
                sb.AppendLine(line);
            }
            sb.AppendLine("return $o;");
            sb = sb.UnIndent();
            sb.AppendLine("}");
        }
    }

    private string PhpPropType(MetadataPropertyType prop, string propType)
    {
        if (propType.StartsWith("array<") || propType.EndsWith("[]"))
            return "array";

        if (prop.GenericArgs?.Length > 0)
        {
            if (prop.GenericArgs.Length == 1)
                return propType.LeftPart('<');
            return propType.LeftPart('<') + prop.GenericArgs.Length;
        }
        
        return propType;
    }

    public static bool? DefaultIsPropertyOptional(PhpGenerator generator, MetadataType type, MetadataPropertyType prop)
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
        "IList`1",
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

    public string DeclarationType(string type, string[] genericArgs)
    {
        var useType = DeclarationTypeFilter?.Invoke(type, genericArgs);
        if (useType != null)
            return useType;

        string cooked = null;

        if (genericArgs != null)
        {
            if (ArrayTypes.Contains(type))
            {
                cooked = "array<{0}>".Fmt(GenericArg(genericArgs[0])).StripNullable();
            }
            if (DictionaryTypes.Contains(type))
            {
                cooked = "array<{0}>".Fmt(GenericArg(genericArgs[1])); //Key Type always string
            }
        }
            
        if (cooked == null)
            cooked = Type(type, genericArgs);
            
        useType = CookedDeclarationTypeFilter?.Invoke(cooked);
        if (useType != null)
            return useType;
            
        //TypeScript doesn't support short-hand Dictionary notation or has a Generic Dictionary Type
        if (cooked.StartsWith("{"))
            return "mixed";

        var arrParts = cooked.SplitOnFirst('[');
        return arrParts.Length > 1 
            ? "array<{0}>".Fmt(arrParts[0]) 
            : cooked;
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
                cooked = GenericArg(genericArgs[0]);
            else if (type == "Nullable`1[]")
                cooked = "array<" + GenericArg(genericArgs[0]) + ">";
            else if (ArrayTypes.Contains(type))
                cooked = "array<{0}>".Fmt(GenericArg(genericArgs[0])).StripNullable();
            else if (DictionaryTypes.Contains(type))
            {
                cooked = "array<{0},{1}>".Fmt(
                    GetKeyType(GenericArg(genericArgs[0])),
                    GenericArg(genericArgs[1]));
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

                        if (arg.StartsWith("{")) // { [name:T]: T }
                            args.Append(arg);
                        else
                            args.Append(GenericArg(arg));
                    }

                    var typeName = TypeAlias(type);
                    cooked = "{0}<{1}>".Fmt(typeName, StringBuilderCacheAlt.ReturnAndFree(args));
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
            var arrayDimensions = String.Concat(Enumerable.Range(0, numberOfDimensions).Select(_ => "[]"));
                
            return "{0}{1}".Fmt(TypeAlias(arrParts[0]), arrayDimensions);
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
            sb.Append("array<");
            var keyType = ConvertFromCSharp(node.Children[0]);
            sb.Append(GetKeyType(keyType));
            sb.Append(",");
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

    public string GetPropertyName(string name) => name.SafeToken().PropertyStyle(); 
    public string GetPropertyName(MetadataPropertyType prop) => 
        prop.GetSerializedAlias() ?? prop.Name.SafeToken().PropertyStyle();
}

public static class PhpGeneratorExtensions
{
    public static string InReturnMarker(this string type)
    {
        var useType = PhpGenerator.ReturnMarkerFilter?.Invoke(type);
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
}