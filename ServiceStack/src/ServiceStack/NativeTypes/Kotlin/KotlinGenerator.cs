using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Host;

namespace ServiceStack.NativeTypes.Kotlin;

public class KotlinGenerator : ILangGenerator
{
    readonly MetadataTypesConfig Config;
    readonly NativeTypesFeature feature;
    List<string> conflictTypeNames = new();
    List<MetadataType> allTypes;

    public KotlinGenerator(MetadataTypesConfig config)
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

    public static List<string> DefaultImports = new() {
        /* built-in types used
        "java.math.BigInteger",
        "java.math.BigDecimal",
        "java.util.Date",
        "java.util.ArrayList",
        "java.util.HashMap",
        */

        "java.math.*",
        "java.util.*",
        "java.io.InputStream",
        "net.servicestack.client.*",
    };

    public static string JavaIoNamespace = "java.io.*";
    public static string GSonAnnotationsNamespace = "com.google.gson.annotations.*";
    public static string GSonReflectNamespace = "com.google.gson.reflect.*";

    public static bool AddGsonImport
    {
        set
        {
            //Used by @SerializedName() annotation, but requires Android dep
            DefaultImports.Add(GSonAnnotationsNamespace);
            //Used by TypeToken<T>
            DefaultImports.Add(GSonReflectNamespace);
        }
    }

    //http://java.interoperabilitybridges.com/articles/data-types-interoperability-between-net-and-java#h4Section2
    public static ConcurrentDictionary<string, string> TypeAliases = new Dictionary<string, string>
    {
        {"String", "String"},
        {"Boolean", "Boolean"},
        {"Char", "String"},
        {"SByte", "Byte"},
        {"Byte", "Short"},
        {"Byte[]", "byte[]"},
        {"Int16", "Short"},
        {"Int32", "Int"},
        {"Int64", "Long"},
        {"UInt16", "Int"},
        {"UInt32", "Long"},
        {"UInt64", "BigInteger"},
        {"Single", "Float"},
        {"Double", "Double"},
        {"Decimal", "BigDecimal"},
        {"IntPtr", "Long"},
        {"Guid", "UUID"},
        {"DateTime", "Date"},
        {"DateOnly", "Date"},
        {"DateTimeOffset", "Date"},
        {"TimeSpan", "TimeSpan"},
        {"TimeOnly", "TimeSpan"},
        {"Type", "Class"},
        {"List", "ArrayList"},
        {"Dictionary", "HashMap"},
        {"Stream", "InputStream"},
    }.ToConcurrentDictionary();

    public static ConcurrentDictionary<string, string> ArrayAliases = new Dictionary<string, string> {
        { "Byte[]", "ByteArray" },
        { "byte[]", "ByteArray" }, //GenericArg()
    }.ToConcurrentDictionary();

    public static TypeFilterDelegate TypeFilter { get; set; }

    public static Func<KotlinGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    public static Func<List<MetadataType>, List<MetadataType>> FilterTypes = DefaultFilterTypes;

    public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types;
        
    /// <summary>
    /// Add Code to top of generated code
    /// </summary>
    public static AddCodeDelegate InsertCodeFilter { get; set; }

    /// <summary>
    /// Add Code to bottom of generated code
    /// </summary>
    public static AddCodeDelegate AddCodeFilter { get; set; }

    /// <summary>
    /// Additional Options in Header Options
    /// </summary>
    public List<string> AddQueryParamOptions { get; set; }

    /// <summary>
    /// Emit code without Header Options
    /// </summary>
    public bool WithoutOptions { get; set; }

    public static List<Type> IgnoreInterfaces { get; set; } =
    [
        typeof(IHasResponseStatus),
    ]; 

    public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
    {
        var typeNamespaces = new HashSet<string>();
        var includeList = RemoveIgnoredTypes(metadata);
        metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
        metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

        var defaultImports = new List<string>(DefaultImports);
        if (!Config.DefaultImports.IsEmpty())
        {
            defaultImports = Config.DefaultImports;
        }
        else
        {
            if (ReferencesGson(metadata))
            {
                defaultImports.AddIfNotExists(GSonAnnotationsNamespace);
                defaultImports.AddIfNotExists(GSonReflectNamespace);
            }
            if (ReferencesStream(metadata))
            {
                defaultImports.AddIfNotExists(JavaIoNamespace);
            }
        }

        string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);
        var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("/* Options:");
            sb.AppendLine($"Date: {DateTime.Now.ToString("s").Replace("T", " ")}");
            sb.AppendLine($"Version: {Env.VersionString}");
            sb.AppendLine($"Tip: {HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")}");
            sb.AppendLine($"BaseUrl: {Config.BaseUrl}");
            if (Config.UsePath != null)
                sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));

            sb.AppendLine();
            sb.AppendLine("{0}Package: {1}".Fmt(defaultValue("Package"), Config.Package));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}TreatTypesAsStrings: {1}".Fmt(defaultValue("TreatTypesAsStrings"), Config.TreatTypesAsStrings.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));
            AddQueryParamOptions.Each(name => sb.AppendLine($"{defaultValue(name)}{name}: {request.QueryString[name]}"));

            sb.AppendLine("*/");
            sb.AppendLine();
        }

        var header = AddHeader?.Invoke(request);
        if (!string.IsNullOrEmpty(header))
            sb.AppendLine(header);

        foreach (var typeName in Config.TreatTypesAsStrings.Safe())
        {
            TypeAliases[typeName] = "String";
        }

        if (Config.Package != null)
        {
            sb.AppendLine($"package {Config.Package}");
            sb.AppendLine();
        }

        string lastNS = null;

        var existingTypes = new HashSet<string>();

        var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
        var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
        var responseTypes = metadata.Operations
            .Where(x => x.Response != null)
            .Select(x => x.Response).ToSet();
        var types = metadata.Types.ToSet();

        allTypes = new List<MetadataType>();
        allTypes.AddRange(requestTypes);
        allTypes.AddRange(responseTypes);
        allTypes.AddRange(types);

        allTypes = FilterTypes(allTypes);

        //TypeScript doesn't support reusing same type name with different generic airity
        var conflictPartialNames = allTypes.Map(x => x.Name).Distinct()
            .GroupBy(g => g.LeftPart('`'))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        this.conflictTypeNames = allTypes
            .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
            .Map(x => x.Name);

        defaultImports.Each(x => sb.AppendLine($"import {x}"));
        sb.AppendLine();

        var insertCode = InsertCodeFilter?.Invoke(allTypes, Config);
        if (insertCode != null)
            sb.AppendLine(insertCode);

        //ServiceStack core interfaces
        foreach (var type in allTypes)
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
                                    return Type("IReturn`1", new[] { Type(operation.ReturnType) });
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

        var addCode = AddCodeFilter?.Invoke(allTypes, Config);
        if (addCode != null)
            sb.AppendLine(addCode);

        return StringBuilderCache.ReturnAndFree(sbInner);
    }

    private bool ReferencesGson(MetadataTypes metadata)
    {
        return metadata.GetAllMetadataTypes()
            .Any(x => x.Properties.Safe().Any(p => p.Name.PropertyStyle().IsKeyWord())
                      || x.Properties.Safe().Any(p => p.DataMember?.Name != null)
                      || (x.RequestType?.ReturnType != null && x.RequestType?.ReturnType.Name.IndexOf('`') >= 0)); //uses TypeToken<T>
    }

    private static bool ReferencesStream(MetadataTypes metadata)
    {
        return metadata.GetAllMetadataTypes().Any(x => x.Name == "Stream" && x.Namespace == "System.IO");
    }

    //Use built-in types already in net.servicestack.client package
    public static HashSet<string> IgnoreTypeNames = new() {
        nameof(ResponseStatus),
        nameof(ResponseError),
        nameof(ErrorResponse),
    }; 

    private List<string> RemoveIgnoredTypes(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        metadata.Types.RemoveAll(x => IgnoreTypeNames.Contains(x.Name));
        return includeList;
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

        var typeName = Type(type.Name, type.GenericArgs);

        sb.Emit(type, Lang.Kotlin);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            var hasIntValue = type.EnumNames.Count == (type.EnumValues?.Count ?? 0);
            var enumConstructor = hasIntValue ? "(val value:Int)" : "";

            sb.AppendLine($"enum class {typeName}{enumConstructor}");
            sb.AppendLine("{");
            sb = sb.Indent();

            if (type.EnumNames != null)
            {
                for (var i = 0; i < type.EnumNames.Count; i++)
                {
                    var name = type.EnumNames[i];
                    var value = hasIntValue ? type.EnumValues[i] : null;

                    var serializeAs = JsConfig.TreatEnumAsInteger || type.Attributes.Safe().Any(x => x.Name == "Flags")
                        ? $"@SerializedName(\"{value}\") "
                        : "";

                    sb.AppendLine(value == null
                        ? $"{name.ToPascalCase()},"
                        : serializeAs + $"{name.ToPascalCase()}({value}),");
                }

                //if (hasIntValue)
                //{
                //    sb.AppendLine();
                //    sb.AppendLine("private final int value;");
                //    sb.AppendLine("{0}(final int intValue) {{ value = intValue; }}".Fmt(typeName));
                //    sb.AppendLine("public int getValue() { return value; }");
                //}
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
        }
        else
        {
            var defType = type.IsInterface()
                ? "interface"
                : "open class";
            var extends = new List<string>();
            var extendTypes = new List<Type>();

            //: BaseClass, Interfaces
            if (type.Inherits != null)
            {
                extends.Add(Type(type.Inherits).InheritedType());
                if (type.Type.BaseType != null)
                    extendTypes.Add(type.Type.BaseType);
            }

            string responseTypeExpression = null;

            var interfaces = new List<string>();
            if (options.ImplementsFn != null)
            {
                var implStr = options.ImplementsFn();
                if (!string.IsNullOrEmpty(implStr))
                {
                    interfaces.Add(implStr);

                    if (implStr.StartsWith("IReturn<"))
                    {
                        var types = implStr.RightPart('<');
                        var returnType = types.Substring(0, types.Length - 1);

                        //Can't get .class from Generic Type definition
                        responseTypeExpression = returnType.Contains("<")
                            ? $"object : TypeToken<{returnType}>(){{}}.type"
                            : $"{returnType}::class.java";
                    }
                }
            }

            foreach (var x in type.Implements)
            {
                interfaces.Add(Type(x));
                if (x.Type != null)
                {
                    extendTypes.Add(x.Type);
                }
            }

            var extend = extends.Count > 0 
                ? " : " + extends[0] + "()"
                : "";

            if (interfaces.Count > 0)
                extend += (extend.IsNullOrEmpty() ? " : " : ", ") + string.Join(", ", interfaces.ToArray());

            sb.AppendLine($"{defType} {typeName}{extend}");
            sb.AppendLine("{");

            sb = sb.Indent();
            InnerTypeFilter?.Invoke(sb, type);

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine($"val {GetPropertyName("Version")}:Int = {Config.AddImplicitVersion}");
            }

            AddProperties(sb, type,
                includeResponseStatus: Config.AddResponseStatus && 
                                       options.IsResponse && 
                                       type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)), 
                extendTypes);

            if (responseTypeExpression != null)
            {
                sb.AppendLine($"companion object {{ private val responseType = {responseTypeExpression} }}");
                sb.AppendLine($"override fun getResponseType(): Any? = {typeName}.responseType");
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
        }

        PostTypeFilter?.Invoke(sb, type);

        return lastNS;
    }

    public void AddProperties(StringBuilderWrapper sb, MetadataType type,
        bool includeResponseStatus, List<Type> extendTypes)
    {
        var wasAdded = false;

        var defaultValue = type.IsInterface != true ? " = null" : "";

        var dataMemberIndex = 1;
        if (type.Properties != null)
        {
            var allBaseProps = new HashSet<string>();
            if (type.Type != null)
            {
                // Ignore built-in Interfaces in ServiceStack.Java which do not contain properties
                var ifaces = type.Type.GetInterfaces()
                    .Where(x => extendTypes.Contains(x) && x.Namespace?.StartsWith("ServiceStack") != true);
                foreach (var iface in ifaces)
                {
                    foreach (var prop in iface.GetProperties())
                    { 
                        allBaseProps.Add(prop.Name);
                    }
                }
                var baseType = type.Type.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    if (extendTypes.Contains(baseType))
                    {
                        foreach (var prop in baseType.GetProperties())
                        {
                            allBaseProps.Add(prop.Name);
                        }
                    }
                    baseType = baseType.BaseType;
                }
            }
            
            foreach (var prop in type.Properties)
            {
                if (wasAdded) sb.AppendLine();

                var propType = GetPropertyType(prop);
                propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                wasAdded = AppendComments(sb, prop.Description);
                wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                var initProp = (prop.IsRequired == true || Config.InitializeCollections) 
                    && prop.IsEnumerable() && feature.ShouldInitializeCollection(type) && !prop.IsInterface();
                var initializer = initProp 
                    ? propType == "ByteArray"
                        ? "ByteArray(0)"
                        : propType.EndsWith("[]")
                            ? $"{propType}{{}}"
                            : $"{propType}()"
                    : "null";

                sb.Emit(prop, Lang.Kotlin);
                PrePropertyFilter?.Invoke(sb, prop, type);

                var defaultName = prop.Name.PropertyStyle();
                var fieldName = GetPropertyName(prop.Name);
                var overrides = allBaseProps.Contains(prop.Name) 
                    ? "override " 
                    : type.IsInterface() 
                        ? "" 
                        : "open ";
                if (fieldName == defaultName || prop.DataMember?.Name != null)
                {
                    sb.AppendLine(!initProp
                        ? $"{overrides}var {fieldName}:{propType}?{defaultValue}"
                        : $"{overrides}var {fieldName}:{propType} = {initializer}");
                }
                else
                {
                    sb.AppendLine(!initProp
                        ? $"@SerializedName(\"{defaultName}\") {overrides}var {fieldName}:{propType}?{defaultValue}"
                        : $"@SerializedName(\"{defaultName}\") {overrides}var {fieldName}:{propType} = {initializer}");
                }
                PostPropertyFilter?.Invoke(sb, prop, type);
            }
        }

        if (includeResponseStatus)
        {
            if (wasAdded) sb.AppendLine();

            AppendDataMember(sb, null, dataMemberIndex++);
            sb.AppendLine($"var {GetPropertyName(nameof(ResponseStatus))}:ResponseStatus?{defaultValue}");
        }
    }

    public virtual string GetPropertyType(MetadataPropertyType prop)
    {
        var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
        return propType;
    }

    public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
    {
        if (attributes == null || attributes.Count == 0) return false;

        var existingAttrs = new HashSet<string>();

        foreach (var attr in attributes)
        {
            //Java 7 doesn't allow repeating attrs 
            var prefix = existingAttrs.Contains(attr.Name)
                ? "// "
                : "";
            existingAttrs.Add(attr.Name);

            if ((attr.Args == null || attr.Args.Count == 0)
                && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
            {
                sb.AppendLine(prefix + $"@{attr.Name}()");
            }
            else
            {
                var args = StringBuilderCacheAlt.Allocate();
                if (attr.ConstructorArgs != null)
                {
                    var props = attr.Attribute?.GetType().GetProperties() ?? [];

                    if (attr.ConstructorArgs.Count > 1)
                        prefix = "// ";

                    foreach (var ctorArg in attr.ConstructorArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");
                        var prop = props.FirstOrDefault(x => string.Equals(x.Name, ctorArg.Name, StringComparison.OrdinalIgnoreCase));
                        args.Append($"{prop?.Name ?? ctorArg.Name}={TypeValue(ctorArg.Type, ctorArg.Value)}");
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
                sb.AppendLine(prefix + $"@{attr.Name}({StringBuilderCacheAlt.ReturnAndFree(args)})");
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
            return value.ExtractTypeName() + "::class";
        }

        return value;
    }

    public string Type(MetadataTypeName typeName)
    {
        return Type(typeName.Name, typeName.GenericArgs);
    }

    public string Type(string type, string[] genericArgs)
    {
        var useType = TypeFilter?.Invoke(type, genericArgs);
        if (useType != null)
            return useType;

        if (genericArgs != null)
        {
            if (type == "Nullable`1")
                return /*@Nullable*/ GenericArg(genericArgs[0]);
            if (type == "Nullable`1[]")
                return $"ArrayList<{GenericArg(genericArgs[0])}>";
            if (ServiceMetadata.AnyCollectionTypes.Contains(type))
                return $"ArrayList<{GenericArg(genericArgs[0])}>".StripNullable();
            if (type.EndsWith("[]"))
                return $"ArrayList<{Type(type.Substring(0,type.Length-2), genericArgs)}>".StripNullable();
            if (ServiceMetadata.AnyDictionaryTypes.Contains(type))
                return $"HashMap<{GenericArg(genericArgs[0])},{GenericArg(genericArgs[1])}>";

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

                var typeName = TypeAlias(type);
                return $"{typeName}<{StringBuilderCacheAlt.ReturnAndFree(args)}>";
            }
        }
        else if (ServiceMetadata.AnyDictionaryTypes.Contains(type))
        {
            return "HashMap<String,Object>";
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
        var arrParts = type.SplitOnFirst('[');
        if (arrParts.Length > 1)
        {
            return ArrayAliases.TryGetValue(type, out var arrayAlias) 
                ? arrayAlias
                : $"ArrayList<{TypeAlias(arrParts[0])}>";
        }

        TypeAliases.TryGetValue(type, out var typeAlias);
        return typeAlias ?? NameOnly(type);
    }

    public string NameOnly(string type)
    {
        var name = conflictTypeNames.Contains(type)
            ? type.Replace('`','_')
            : type.SplitOnFirst('`')[0];

        return name.LastRightPart('.').SafeToken();
    }

    public bool AppendComments(StringBuilderWrapper sb, string desc)
    {
        if (desc != null && Config.AddDescriptionAsComments)
        {
            sb.AppendLine("/**");
            sb.AppendLine($"* {desc.SafeComment()}");
            sb.AppendLine("*/");
        }

        return false;
    }

    public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
    {
        if (dcMeta == null)
        {
            if (Config.AddDataContractAttributes)
                sb.AppendLine("@DataContract()");
            return;
        }

        var dcArgs = "";
        if (dcMeta.Name != null || dcMeta.Namespace != null)
        {
            if (dcMeta.Name != null)
                dcArgs = $"Name={dcMeta.Name.QuotedSafeValue()}";

            if (dcMeta.Namespace != null)
            {
                if (dcArgs.Length > 0)
                    dcArgs += ", ";

                dcArgs += $"Namespace={dcMeta.Namespace.QuotedSafeValue()}";
            }

            dcArgs = $"({dcArgs})";
        }
        sb.AppendLine($"@DataContract{dcArgs}");
    }

    public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
    {
        if (dmMeta == null)
        {
            if (Config.AddDataContractAttributes)
            {
                sb.AppendLine(Config.AddIndexesToDataMembers
                    ? $"@DataMember(Order={dataMemberIndex})"
                    : "@DataMember()");
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
                dmArgs = $"Name={dmMeta.Name.QuotedSafeValue()}";

            if (dmMeta.Order != null || Config.AddIndexesToDataMembers)
            {
                if (dmArgs.Length > 0)
                    dmArgs += ", ";

                dmArgs += $"Order={dmMeta.Order ?? dataMemberIndex}";
            }

            if (dmMeta.IsRequired != null)
            {
                if (dmArgs.Length > 0)
                    dmArgs += ", ";

                dmArgs += $"IsRequired={dmMeta.IsRequired.ToString().ToLower()}";
            }

            if (dmMeta.EmitDefaultValue != null)
            {
                if (dmArgs.Length > 0)
                    dmArgs += ", ";

                dmArgs += $"EmitDefaultValue={dmMeta.EmitDefaultValue.ToString().ToLower()}";
            }

            dmArgs = $"({dmArgs})";
        }
        sb.AppendLine($"@DataMember{dmArgs}");

        if (dmMeta.Name != null)
        {
            sb.AppendLine($"@SerializedName(\"{dmMeta.Name}\")");
        }

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
            return TypeAlias(node.Children[0].Text) + "?";

        if (node.Text == "List")
        {
            sb.Append("ArrayList<");
            if (!node.Children.IsEmpty())
                sb.Append(ConvertFromCSharp(node.Children[0]));
            else
                sb.Append(ConvertFromCSharp(new TextNode { Text = "Object" })); //error fallback
            sb.Append(">");
        }
        else if (node.Text == "Dictionary")
        {
            sb.Append("HashMap<");
            sb.Append(ConvertFromCSharp(node.Children[0]));
            sb.Append(",");
            sb.Append(ConvertFromCSharp(node.Children[1]));
            sb.Append(">");
        }
        else
        {
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

        var typeName = sb.ToString();
        return typeName.LastRightPart('.'); //remove nested class
    }

    public string GetPropertyName(string name)
    {
        var fieldName = name.SafeToken().PropertyStyle();
        if (fieldName.IsKeyWord())
            fieldName = char.ToUpper(fieldName[0]) + fieldName.SafeSubstring(1);
        return fieldName;
    }
}

public static class KotlinGeneratorExtensions
{
    public static string InheritedType(this string type)
    {
        return type;
    }

    public static HashSet<string> KotlinKeyWords = new() {
        //Java Keywords
        "abstract",
        "assert",
        "boolean",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "class",
        "continue",
        "default",
        "do",
        "double",
        "else",
        "enum",
        "extends",
        "final",
        "finally",
        "float",
        "for",
        "if",
        "implements",
        "import",
        "instanceof",
        "int",
        "interface",
        "long",
        "native",
        "new",
        "package",
        "private",
        "protected",
        "public",
        "return",
        "short",
        "static",
        "strictfp",
        "super",
        "switch",
        "synchronized",
        "this",
        "throw",
        "throws",
        "transient",
        "try",
        "void",
        "volatile",
        "while",

        //Kotlin extra Keywords https://bitbucket.org/birkenfeld/pygments-main/pull-requests/505/update-list-of-kotlin-keywords-according/diff
        "annotation",
        "as",
        "by",
        "companion",
        "const",
        "constructor",
        "crossinline",
        "data",
        "dynamic",
        "external",
        "fun",
        "get",
        "in",
        "inner",
        "internal",
        "in",
        "infix",
        "inline",
        "inner",
        "is",
        "lateinit",
        "noinline",
        "object",
        "open",
        "operator",
        "out",
        "reified",
        "set",
        "sealed",
        "tailrec",
        "trait",
        "type",
        "val",
        "var",
        "vararg",
        "when",
        "where",
    };
        
    public static bool IsKeyWord(this string name)
    {
        return KotlinKeyWords.Contains(name);
    }

    public static string PropertyStyle(this string name)
    {
        //Gson is case-sensitive, fieldName needs to match json
        var fieldName = JsConfig.TextCase == TextCase.CamelCase
            ? name.ToCamelCase()
            : JsConfig.TextCase == TextCase.SnakeCase
                ? name.ToLowercaseUnderscore()
                : name;

        return fieldName;
    }

    public static MetadataAttribute ToMetadataAttribute(this MetadataRoute route)
    {
        if (route.Verbs != null)
        {
            return new MetadataAttribute
            {
                Name = "Route",
                Args = [
                    new() { Name = "Path", Type = "string", Value = route.Path },
                    new() { Name = "Verbs", Type = "string", Value = route.Verbs }
                ],
            };
        }

        return new MetadataAttribute
        {
            Name = "Route",
            ConstructorArgs = [
                new() { Name = "Path", Type = "string", Value = route.Path }
            ],
        };
    }

    public static StringBuilderWrapper AppendPropertyAccessor(this StringBuilderWrapper sb, string type, string fieldName, string settersReturnThis)
    {
        return sb.AppendPropertyAccessor(type, fieldName.PropertyStyle(), fieldName.ToPascalCase(), settersReturnThis);
    }

    public static StringBuilderWrapper AppendPropertyAccessor(this StringBuilderWrapper sb, string type, string fieldName, string accessorName, string settersReturnThis)
    {
        var getter = type.StartsWithIgnoreCase("bool") && !accessorName.StartsWithIgnoreCase("is") 
            ? "is" 
            : "get";
        sb.AppendLine($"public {type} {getter}{accessorName}() {{ return {fieldName}; }}");
        sb.AppendLine(settersReturnThis != null
            ? $"public {settersReturnThis} set{accessorName}({type} value) {{ this.{fieldName} = value; return this; }}"
            : $"public void set{accessorName}({type} value) {{ this.{fieldName} = value; }}");
        return sb;
    }
}