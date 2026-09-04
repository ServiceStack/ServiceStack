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
    public Lang Lang => Lang.Go;
    public MetadataTypesConfig Config { get; }
    readonly NativeTypesFeature feature;
    public List<string> ConflictTypeNames = new();
    public List<MetadataType> AllTypes { get; set; }

    public GoGenerator(MetadataTypesConfig config)
    {
        Config = config;
        feature = HostContext.GetPlugin<NativeTypesFeature>();
    }

    public static Func<IRequest, string> AddHeader { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }

    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

    public static bool GenerateServiceStackTypes => IgnoreTypeInfosFor.Count == 0;

    //Types in the servicestack-go library are filtered out in LibraryTypes below
    public static HashSet<string> IgnoreTypeInfosFor = [];

    /// <summary>
    /// The Go module of the ServiceStack Go Client Library that generated DTOs reference
    /// </summary>
    public static string LibraryPackage { get; set; } = "github.com/ServiceStack/servicestack-go";

    /// <summary>
    /// The package alias the ServiceStack Go Client Library is imported as
    /// </summary>
    public static string LibraryAlias { get; set; } = "ss";

    /// <summary>
    /// Built-in ServiceStack Types implemented in the servicestack-go library which
    /// are referenced instead of being emitted in generated DTOs
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
        nameof(ConvertSessionToToken),
        nameof(ConvertSessionToTokenResponse),
        nameof(GetAccessToken),
        nameof(GetAccessTokenResponse),
        nameof(GetApiKeys),
        nameof(GetApiKeysResponse),
        nameof(RegenerateApiKeys),
        nameof(RegenerateApiKeysResponse),
        nameof(UserApiKey),
        nameof(NavItem),
        nameof(GetNavItems),
        nameof(GetNavItemsResponse),
    ];

    /// <summary>
    /// Library Types implemented as Go generic types, e.g. ss.QueryResponse[Booking]
    /// </summary>
    public static HashSet<string> GenericLibraryTypes { get; set; } =
    [
        "QueryResponse",
    ];

    /// <summary>
    /// Method names generated on Request DTOs, which are omitted when they would
    /// conflict with an existing property of the same name
    /// </summary>
    public const string CreateResponseMethod = "CreateResponse";

    public const string CreateResponseVoidMethod = "CreateResponseVoid";
    public const string HttpMethodMethod = "HttpMethod";

    public static List<string> DefaultImports = new()
    {
    };

    public static Dictionary<string, string> TypeAliases = new()
    {
        { "String", "string" },
        { "Object", "interface{}" },
        { "Boolean", "bool" },
        { "DateTime", "time.Time" },
        { "DateOnly", "time.Time" },
        { "DateTimeOffset", "time.Time" },
        { "TimeSpan", "time.Duration" },
        { "TimeOnly", "time.Duration" },
        { "Guid", "string" },
        { "Char", "string" },
        { "Byte", "byte" },
        { "Int16", "int16" },
        { "Int32", "int" },
        { "Int64", "int64" },
        { "UInt16", "uint16" },
        { "UInt32", "uint32" },
        { "UInt64", "uint64" },
        { "Single", "float32" },
        { "Double", "float64" },
        { "Decimal", "float64" },
        { "IntPtr", "int64" },
        { "Byte[]", "[]byte" },
        { "Stream", "[]byte" },
        { "HttpWebResponse", "[]byte" },
        { "Uri", "string" },
        { "Type", "string" },
    };

    internal static readonly Dictionary<string, string> primitiveDefaultValues = new()
    {
        { "string", "\"\"" },
        { "bool", "false" },
        { "time.Time", "time.Time{}" },
        { "time.Duration", "0" },
        { "byte", "0" },
        { "int16", "0" },
        { "int", "0" },
        { "int64", "0" },
        { "uint16", "0" },
        { "uint32", "0" },
        { "uint64", "0" },
        { "float32", "0" },
        { "float64", "0" },
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
    public static Func<GoGenerator, MetadataType, MetadataPropertyType, bool> IsPropertyOptional { get; set; } =
        DefaultIsPropertyOptional;

    public static bool DefaultIsPropertyOptional(GoGenerator generator, MetadataType type, MetadataPropertyType prop)
    {
        return !prop.IsRequired();
    }

    /// <summary>
    /// Library Types referenced by the generated DTOs, resolved in Init()
    /// </summary>
    public HashSet<string> UseLibraryTypes { get; set; } = new();

    /// <summary>
    /// Whether properties of abstract Types with sub types are emitted as interface{}.
    /// Go doesn't support sub classing so a property of an abstract Type can only ever
    /// hold the abstract Type's own properties, losing the sub types data
    /// </summary>
    public static bool PolymorphicPropertiesAsAny { get; set; } = true;

    /// <summary>
    /// Abstract Types with sub types in the generated DTOs, resolved in Init()
    /// </summary>
    public HashSet<string> PolymorphicTypes { get; set; } = new();

    private bool usesLibrary;
    private bool usesTime;
    private bool resolvingPropertyType;

    /// <summary>
    /// Whether the Type Name refers to a Type implemented in the servicestack-go library
    /// </summary>
    public bool IsLibraryType(string typeName) => UseLibraryTypes.Contains(typeName);

    /// <summary>
    /// Reference a Type implemented in the servicestack-go library, e.g. ss.ResponseStatus
    /// </summary>
    public string LibraryType(string typeName)
    {
        usesLibrary = true;
        return LibraryAlias + "." + typeName;
    }

    public void Init(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        AllTypes = metadata.GetAllTypesOrdered();
        AllTypes.RemoveAll(x => x.IgnoreType(Config, includeList));

        //Interfaces that are only used as markers in .NET aren't needed in Go
        if (AllTypes.Any(x => x.IsInterface == true))
        {
            var referencedTypes = new HashSet<string>();
            foreach (var metaType in AllTypes)
            {
                if (metaType.Inherits != null)
                    referencedTypes.Add(metaType.Inherits.Name.LeftPart('`'));

                foreach (var metaProp in metaType.Properties.Safe())
                {
                    referencedTypes.Add(metaProp.Type.LeftPart('`').TrimEnd('[', ']'));
                    foreach (var genericArg in metaProp.GenericArgs.Safe())
                    {
                        referencedTypes.Add(genericArg.LeftPart('`').TrimEnd('[', ']'));
                    }
                }
            }

            AllTypes.RemoveAll(x => x.IsInterface == true && !referencedTypes.Contains(x.Name.LeftPart('`')));
        }

        //Only use Library Types when they're not shadowed by a User-defined Type of the same name
        var userTypeNames = AllTypes
            .Where(x => x.Namespace != nameof(ServiceStack))
            .Map(x => x.Name.LeftPart('`'))
            .ToSet();
        UseLibraryTypes = LibraryTypes.Where(x => !userTypeNames.Contains(x)).ToSet();

        //Library Types are implemented in the servicestack-go library
        AllTypes.RemoveAll(x => UseLibraryTypes.Contains(x.Name.LeftPart('`')));

        AllTypes = FilterTypes(AllTypes);

        //Properties of abstract Types with sub types can only be represented as interface{}
        PolymorphicTypes = !PolymorphicPropertiesAsAny
            ? new HashSet<string>()
            : AllTypes.Where(x => x.IsAbstract == true)
                .Map(x => x.Name.LeftPart('`'))
                .Where(name => AllTypes.Any(x => x.Inherits?.Name.LeftPart('`') == name))
                .ToSet();

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
        var formatter = request?.TryResolve<INativeTypesFormatter>();
        Init(metadata);

        List<string> defaultImports = new(!Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports);

        var packageName = Config.GlobalNamespace ?? "dtos";

        string defaultValue(string k) => request?.QueryString[k].IsNullOrEmpty() != false ? "//" : "";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);
        var includeOptions = !WithoutOptions && request?.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}MakePropertiesOptional: {1}".Fmt(defaultValue("MakePropertiesOptional"),
                Config.MakePropertiesOptional));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"),
                Config.AddServiceStackTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"),
                Config.AddImplicitVersion));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"),
                Config.AddDescriptionAsComments));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"),
                Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"),
                Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));
            AddQueryParamOptions.Each(name =>
                sb.AppendLine($"{defaultValue(name)}{name}: {request?.QueryString[name]}"));

            sb.AppendLine("*/");
            sb.AppendLine();
        }

        formatter?.AddHeader(sb, this, request);

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

        // Types are generated first so only the imports they use are emitted
        var sbTypesInner = StringBuilderCacheAlt.Allocate();
        var sbTypes = new StringBuilderWrapper(sbTypesInner);

        var insertCode = InsertCodeFilter?.Invoke(AllTypes, Config);
        if (insertCode != null)
            sbTypes.AppendLine(insertCode);

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

                    lastNS = AppendType(ref sbTypes, type, lastNS,
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
                    lastNS = AppendType(ref sbTypes, type, lastNS,
                        new CreateTypeOptions
                        {
                            IsResponse = true,
                        });

                    existingTypes.Add(fullTypeName);
                }
            }
            else if (AllTypes.Contains(type) && !existingTypes.Contains(fullTypeName))
            {
                lastNS = AppendType(ref sbTypes, type, lastNS,
                    new CreateTypeOptions { IsType = true });

                existingTypes.Add(fullTypeName);
            }
        }

        var addCode = AddCodeFilter?.Invoke(AllTypes, Config);
        if (addCode != null)
            sbTypes.AppendLine(addCode);

        // The time and servicestack packages are required by the generated Types that
        // reference them, DefaultImports only overrides the packages they don't
        if (usesTime)
            defaultImports.AddIfNotExists("time");
        if (usesLibrary)
            defaultImports.AddIfNotExists($"{LibraryAlias} {LibraryPackage}");

        if (defaultImports.Count > 0)
        {
            sb.AppendLine("import (");
            sb = sb.Indent();
            //gofmt sorts imports by package path
            foreach (var import in defaultImports.OrderBy(x => x.RightPart(' '), StringComparer.Ordinal))
            {
                // Imports can be aliased with an "alias package" prefix, e.g. ss github.com/org/pkg
                var alias = import.LeftPart(' ');
                var package = import.RightPart(' ');
                sb.AppendLine(alias == package
                    ? $"\"{package}\""
                    : $"{alias} \"{package}\"");
            }

            sb = sb.UnIndent();
            sb.AppendLine(")");
            sb.AppendLine();
        }

        sb.AppendLine(StringBuilderCacheAlt.ReturnAndFree(sbTypesInner).TrimEnd());

        var ret = GoFormat(StringBuilderCache.ReturnAndFree(sbInner));
        return formatter != null ? formatter.Transform(ret, this, request) : ret;
    }

    /// <summary>
    /// Format generated source with gofmt conventions, i.e. tabs for indentation,
    /// no trailing whitespace, no consecutive blank lines and struct fields
    /// aligned in columns
    /// </summary>
    public static string GoFormat(string src)
    {
        var lines = new List<string>();
        var lastLineEmpty = false;
        foreach (var line in src.ReadLines())
        {
            var indent = 0;
            while ((indent + 1) * 4 <= line.Length && line.Substring(indent * 4, 4) == "    ")
            {
                indent++;
            }

            var content = line.Substring(indent * 4).TrimEnd();
            var isEmpty = content.Length == 0;
            if (isEmpty && lastLineEmpty)
                continue;
            lastLineEmpty = isEmpty;

            lines.Add(isEmpty ? "" : new string('\t', indent) + content);
        }

        AlignStructFields(lines);

        var sb = StringBuilderCacheAlt.Allocate();
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }

        return StringBuilderCacheAlt.ReturnAndFree(sb);
    }

    /// <summary>
    /// Align the Name and Type columns of adjacent struct fields like gofmt, e.g:
    ///
    ///     Id   int    `json:"id,omitempty"`
    ///     Name string `json:"name"`
    /// </summary>
    private static void AlignStructFields(List<string> lines)
    {
        var inStruct = false;
        var start = -1;

        void alignFields(int from, int to)
        {
            if (to - from < 2)
                return;

            var names = new List<string>();
            var types = new List<string>();
            var rest = new List<string>();
            for (var i = from; i < to; i++)
            {
                var field = lines[i].Substring(1); //strip the field's tab indent
                var name = field.LeftPart(' ');

                // The Type can itself contain spaces, e.g. KeyValuePair[string, string],
                // so it's everything up to the field's JSON tag
                var remainder = field.Substring(name.Length).TrimStart();
                var tagPos = remainder.IndexOf('`');

                names.Add(name);
                types.Add(tagPos >= 0 ? remainder.Substring(0, tagPos).TrimEnd() : remainder.LeftPart(' '));
                rest.Add(tagPos >= 0 ? remainder.Substring(tagPos) : remainder.RightPart(' ').TrimStart());
            }

            var nameWidth = names.Max(x => x.Length);
            var typeWidth = types.Max(x => x.Length);
            for (var i = from; i < to; i++)
            {
                lines[i] = "\t" + names[i - from].PadRight(nameWidth) + " " +
                           types[i - from].PadRight(typeWidth) + " " + rest[i - from];
            }
        }

        //Fields are only aligned within adjacent runs of fields, comments and blank lines break the run
        bool isField(string line) => line.StartsWith("\t") && !line.StartsWith("\t\t")
                                                           && !line.TrimStart().StartsWith("/") &&
                                                           line.Substring(1).Trim().CountOccurrencesOf(' ') >= 2;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (!inStruct)
            {
                inStruct = line.StartsWith("type ") && line.EndsWith(" struct {");
                continue;
            }

            if (isField(line))
            {
                if (start == -1)
                    start = i;
                continue;
            }

            if (start >= 0)
            {
                alignFields(start, i);
                start = -1;
            }

            if (line == "}")
                inStruct = false;
        }
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
                // Go's const declarations are aligned in columns, e.g:
                //     RoomTypeSingle RoomType = "Single"
                //     RoomTypeDouble           = "Double"
                var constNames = new List<string>();
                var constTypes = new List<string>();
                var constValues = new List<string>();

                for (var i = 0; i < type.EnumNames.Count; i++)
                {
                    var name = type.EnumNames[i];
                    var value = type.EnumValues?[i];
                    var memberValue = type.GetEnumMemberValue(i);

                    constNames.Add(typeName + name);
                    //Only the first const needs to declare the Type, the rest inherit it
                    constTypes.Add(isIntEnum || i == 0 ? typeName : "");
                    constValues.Add(isIntEnum
                        ? value ?? i.ToString()
                        : $"\"{memberValue ?? name}\"");
                }

                var nameWidth = constNames.Max(x => x.Length);
                var typeWidth = constTypes.Max(x => x.Length);

                sb.AppendLine("const (");
                sb = sb.Indent();

                for (var i = 0; i < constNames.Count; i++)
                {
                    sb.AppendLine(
                        $"{constNames[i].PadRight(nameWidth)} {constTypes[i].PadRight(typeWidth)} = {constValues[i]}");
                }

                sb = sb.UnIndent();
                sb.AppendLine(")");
            }
        }
        else
        {
            // Go struct
            var typeName = Type(type.Name, type.GenericArgs);

            // Generic Type declarations need a constraint on each Type param, e.g:
            //     type IdentityUser_1[TKey any] struct { ... }
            var declarationName = type.GenericArgs?.Length > 0
                ? $"{NameOnly(type.Name)}[{string.Join(", ", type.GenericArgs.Map(x => $"{GenericArg(x)} any"))}]"
                : typeName;

            var baseTypeName = type.Inherits != null
                ? Type(type.Inherits.Name, type.Inherits.GenericArgs)
                : null;

            // Go can't embed collections, DTOs inheriting a collection are instead emitted
            // as a named collection Type, e.g: type StoreRockstars []Rockstar
            if (baseTypeName != null && (baseTypeName.StartsWith("[]") || baseTypeName.StartsWith("map[")))
            {
                sb.AppendLine($"type {declarationName} {baseTypeName}");

                if (options.IsRequest)
                {
                    AppendRequestMethods(sb, type, typeName, options.Op);
                }

                PostTypeFilter?.Invoke(sb, type);
                return lastNS;
            }

            sb.AppendLine($"type {declarationName} struct {{");
            sb = sb.Indent();

            InnerTypeFilter?.Invoke(sb, type);

            // Add embedded base type if inherits
            if (baseTypeName != null)
            {
                sb.AppendLine(baseTypeName);
            }

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine($"Version int `json:\"version\"` //{Config.AddImplicitVersion}");
            }

            AddProperties(sb, type,
                includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                                                                && type.Properties.Safe().All(x =>
                                                                    x.Name != nameof(ResponseStatus)));

            sb = sb.UnIndent();
            sb.AppendLine("}");

            if (options.IsRequest)
            {
                AppendRequestMethods(sb, type, typeName, options.Op);
            }
        }

        PostTypeFilter?.Invoke(sb, type);

        return lastNS;
    }

    /// <summary>
    /// Generate the methods implementing the servicestack-go client interfaces which lets
    /// the Response Type and HTTP Method of a Request DTO be resolved from the Request DTO, e.g:
    ///
    ///     func (Hello) CreateResponse() (r HelloResponse) { return }
    ///     func (Hello) HttpMethod() string { return "GET" }
    /// </summary>
    public void AppendRequestMethods(StringBuilderWrapper sb, MetadataType type, string typeName,
        MetadataOperationType op)
    {
        if (op == null)
            return;

        // Go doesn't allow a method and a field of the same name
        bool hasProperty(string name) => type.Properties.Safe().Any(x => GetPropertyName(x) == name);

        //Method Signature -> Body statement, emitted in aligned columns like gofmt
        var methods = new List<KeyValuePair<string, string>>();

        var returnsVoid = op.ReturnsVoid == true || (op.ReturnType == null && op.Response == null);
        if (returnsVoid)
        {
            if (!hasProperty(CreateResponseVoidMethod))
                methods.Add(new($"func ({typeName}) {CreateResponseVoidMethod}()", ""));
        }
        else if (!hasProperty(CreateResponseMethod))
        {
            var responseType = op.ReturnType != null
                ? Type(op.ReturnType.Name, op.ReturnType.GenericArgs)
                : Type(op.Response.Name, op.Response.GenericArgs);

            // A Request DTO can't return itself as it would recurse in its own method signature
            if (responseType != typeName)
            {
                methods.Add(new($"func ({typeName}) {CreateResponseMethod}() (r {responseType})", "return"));
            }
        }

        var method = op.Method ?? op.Routes?.FirstOrDefault(x => !string.IsNullOrEmpty(x.Verbs))?.Verbs.LeftPart(',');
        if (!string.IsNullOrEmpty(method) && method != "ANY" && !hasProperty(HttpMethodMethod))
        {
            methods.Add(new($"func ({typeName}) {HttpMethodMethod}() string", $"return \"{method.ToUpper()}\""));
        }

        if (methods.Count == 0)
            return;

        sb.AppendLine();
        AppendMethods(sb, methods);
    }

    /// <summary>
    /// gofmt only keeps a method's body on the same line when its declaration fits within
    /// 100 chars, and aligns the bodies of the adjacent methods that do, e.g:
    ///
    ///     func (Hello) CreateResponse() (r HelloResponse) { return }
    ///     func (Hello) HttpMethod() string                { return "GET" }
    /// </summary>
    public static void AppendMethods(StringBuilderWrapper sb, List<KeyValuePair<string, string>> methods)
    {
        const int MaxOneLineDeclaration = 100;
        bool isOneLine(KeyValuePair<string, string> method) =>
            method.Key.Length + 1 + method.Value.Length <= MaxOneLineDeclaration;

        string body(string statement) => statement.Length == 0 ? "{}" : $"{{ {statement} }}";

        for (var i = 0; i < methods.Count; i++)
        {
            if (!isOneLine(methods[i]))
            {
                sb.AppendLine($"{methods[i].Key} {{");
                sb.AppendLine($"\t{methods[i].Value}");
                sb.AppendLine("}");
                continue;
            }

            //Only the adjacent methods gofmt keeps on one line are aligned together
            var last = i;
            while (last + 1 < methods.Count && isOneLine(methods[last + 1]))
                last++;

            var signatureWidth = 0;
            for (var x = i; x <= last; x++)
                signatureWidth = Math.Max(signatureWidth, methods[x].Key.Length);

            for (; i <= last; i++)
                sb.AppendLine($"{methods[i].Key.PadRight(signatureWidth)} {body(methods[i].Value)}");
            i = last;
        }
    }

    public virtual string GetPropertyType(MetadataPropertyType prop, out bool isNullable)
    {
        //Only properties reference abstract Types as interface{}, sub types still embed them
        resolvingPropertyType = true;
        string propType;
        try
        {
            propType = Type(prop.GetTypeName(Config, AllTypes), prop.GenericArgs);
        }
        finally
        {
            resolvingPropertyType = false;
        }

        isNullable = propType.EndsWith("?");
        if (isNullable)
        {
            propType = propType.Substring(0, propType.Length - 1);
        }
        else
        {
            isNullable = prop.IsRequired != true;
        }

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
                var usePointer = IsPropertyOptional(this, type, prop);
                if (usePointer && !propType.StartsWith("*") && !propType.StartsWith("[]") &&
                    !propType.StartsWith("map["))
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
            sb.AppendLine($"ResponseStatus *{TypeAlias(nameof(ResponseStatus))} `json:\"responseStatus,omitempty\"`");
        }
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

    public static HashSet<string> ArrayTypes = new()
    {
        "List`1",
        "IEnumerable`1",
        "ICollection`1",
        "HashSet`1",
        "Queue`1",
        "Stack`1",
        "IEnumerable",
    };

    public static HashSet<string> DictionaryTypes = new()
    {
        "Dictionary`2",
        "IDictionary`2",
        "IOrderedDictionary`2",
        "OrderedDictionary",
        "StringDictionary",
        "IDictionary",
        "IOrderedDictionary",
    };

    public static HashSet<string> AllowedKeyTypes = new()
    {
        "string",
        "bool",
        "byte",
        "int16", "int", "int64",
        "uint16", "uint32", "uint64",
        "float32", "float64",
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
                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var baseName = parts[0];
                    if (IsLibraryType(baseName))
                    {
                        // Library Types implemented as Go generics, e.g. ss.QueryResponse[Booking]
                        cooked = GenericLibraryTypes.Contains(baseName) && genericArgs.Length > 0
                            ? $"{LibraryType(baseName)}[{GenericArg(genericArgs[0])}]"
                            : TypeAlias(baseName);
                    }
                    else
                    {
                        // Generic Types are emitted as Go generics, e.g. KeyValuePair[string, string]
                        cooked = genericArgs.Length > 0
                            ? $"{NameOnly(type)}[{string.Join(", ", genericArgs.Map(GenericArg))}]"
                            : NameOnly(type);
                    }
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

        TypeAliases.TryGetValue(type, out var typeAlias);

        var cooked = typeAlias ?? NameOnly(type);
        if (resolvingPropertyType && PolymorphicTypes.Contains(cooked))
            return TypeAliases["Object"];

        if (cooked.StartsWith("time."))
        {
            usesTime = true;
        }
        else if (IsLibraryType(cooked))
        {
            cooked = LibraryType(cooked);
        }

        return CookedTypeFilter?.Invoke(cooked) ?? cooked;
    }

    public string NameOnly(string type)
    {
        var name = ConflictTypeNames.Contains(type)
            ? type.Replace('`', '_')
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
        var name = node.Text.LeftPart('`');

        // Nullable<T> is represented by the property being a pointer
        if (name == "Nullable")
            return node.Children.Count > 0 ? ConvertFromCSharp(node.Children[0]) : TypeAliases["Object"];

        if (IsArrayType(name))
            return "[]" + (node.Children.Count > 0
                ? ConvertFromCSharp(node.Children[0])
                : TypeAliases["Object"]);

        if (IsDictionaryType(name))
        {
            var keyType = node.Children.Count > 0 ? ConvertFromCSharp(node.Children[0]) : "string";
            var valueType = node.Children.Count > 1 ? ConvertFromCSharp(node.Children[1]) : TypeAliases["Object"];
            return $"map[{GetKeyType(keyType)}]{valueType}";
        }

        if (node.Children.Count > 0)
        {
            // Library Types implemented as Go generics, e.g. ss.QueryResponse[Booking]
            if (IsLibraryType(name) && GenericLibraryTypes.Contains(name))
                return $"{LibraryType(name)}[{ConvertFromCSharp(node.Children[0])}]";

            // Go doesn't support generics in the same way, just use the base name
            return TypeAlias(node.Text);
        }

        return TypeAlias(node.Text);
    }

    /// <summary>
    /// Whether the Type is a collection, with or without its generic arity, e.g. List or List`1
    /// </summary>
    public static bool IsArrayType(string name) =>
        ArrayTypes.Contains(name) || ArrayTypes.Contains(name + "`1");

    /// <summary>
    /// Whether the Type is a dictionary, with or without its generic arity, e.g. Dictionary or Dictionary`2
    /// </summary>
    public static bool IsDictionaryType(string name) =>
        DictionaryTypes.Contains(name) || DictionaryTypes.Contains(name + "`2");

    private static string GetKeyType(string keyType)
    {
        // Go map keys have to be comparable Types
        return AllowedKeyTypes.Contains(keyType)
            ? keyType
            : "string";
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