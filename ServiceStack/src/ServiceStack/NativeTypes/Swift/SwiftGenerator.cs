using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Host;

namespace ServiceStack.NativeTypes.Swift;

public class SwiftGenerator : ILangGenerator
{
    readonly MetadataTypesConfig Config;
    readonly NativeTypesFeature feature;
    List<MetadataType> allTypes;
    List<string> conflictTypeNames = new();

    public SwiftGenerator(MetadataTypesConfig config)
    {
        Config = config;
        feature = HostContext.GetPlugin<NativeTypesFeature>();
    }

    public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
    public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

    public static List<string> DefaultImports = new() {
        "Foundation",
        "ServiceStack", //Required when referencing ServiceStack.framework in CocoaPods, Carthage or SwiftPM
    };

    public static Func<string, string> EnumNameStrategy { get; set; } = CSharpStyleEnums;

    public static string CSharpStyleEnums(string enumName) => enumName;
    public static string SwiftStyleEnums(string enumName) => enumName.ToCamelCase();

    public static ConcurrentDictionary<string, string> TypeAliases = new Dictionary<string, string>
    {
        {"Boolean", "Bool"},
        {"DateTime", "Date"},
        {"DateOnly", "Date"},
        {"DateTimeOffset", "Date"},
        {"TimeSpan", "TimeInterval"},
        {"TimeOnly", "TimeInterval"},
        {"Guid", "String"},
        {"Char", "String"}, //no encoder/decoder for Char
        {"Byte", "UInt8"},
        {"Int16", "Int16"},
        {"Int32", "Int"},   // Keep as `Int` as only String/Int Keys use Dictionary, otherwise they use [K1,V1,K2,V2] Arrays
        {"Int64", "Int"},   // https://forums.swift.org/t/json-encoding-decoding-weird-encoding-of-dictionary-with-enum-values/12995/2
        {"UInt16", "UInt16"},
        {"UInt32", "UInt32"},
        {"UInt64", "UInt64"},
        {"Single", "Float"},
        {"Double", "Double"},
        {"Decimal", "Double"},
        {"IntPtr", "Int64"},
        {"Stream", "Data"},
        {"Type", "String"},
        {"QueryDb`1", "QueryDb"},
        {"QueryDb`2", "QueryDb2"},
        {"QueryData`1", "QueryData"},
        {"QueryData`2", "QueryData2"},
        {"Object", "String"}, // Any breaks Codable contract
    }.ToConcurrentDictionary();

    //Use built-in types already in net.servicestack.client package
    public static HashSet<string> IgnoreTypeNames = new[] {
        typeof(QueryBase),
        typeof(QueryDb<>),
        typeof(QueryDb<,>),
        typeof(QueryData<>),
        typeof(QueryData<,>),
        typeof(QueryResponse<>),
        typeof(AuditBase),
        typeof(EmptyResponse),
        typeof(ResponseStatus),
        typeof(ResponseError),
        typeof(ErrorResponse),
        typeof(Service),
    }.Map(x => x.Name).ToSet();
        
    /// <summary>
    /// Customize how types are encoded &amp; decoded with a Type Converter
    /// </summary>
    public static ConcurrentDictionary<string, SwiftTypeConverter> Converters = new Dictionary<string, SwiftTypeConverter> {
        ["TimeInterval"] = new() { Attribute = "@TimeSpan" },
    }.ToConcurrentDictionary();

    public static TypeFilterDelegate TypeFilter { get; set; }
        
    public static Func<SwiftGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

    public static HashSet<string> OverrideInitForBaseClasses = new() {
        "NSObject"
    };

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
    /// Include Additional QueryString Params in Header Options
    /// </summary>
    public List<string> AddQueryParamOptions { get; set; }

    /// <summary>
    /// Emit code without Header Options
    /// </summary>
    public bool WithoutOptions { get; set; }

    public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
    {
        var typeNamespaces = new HashSet<string>();
        var includeList = RemoveIgnoredTypes(metadata);
        metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
        metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

        var defaultImports = !Config.DefaultImports.IsEmpty()
            ? Config.DefaultImports
            : DefaultImports;

        string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

        var sbInner = StringBuilderCache.Allocate();
        var sb = new StringBuilderWrapper(sbInner);
        var sbExt = new StringBuilderWrapper(new StringBuilder());
        var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
        if (includeOptions)
        {
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("SwiftVersion: 5.0");
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            if (Config.UsePath != null)
                sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));

            sb.AppendLine();

            sb.AppendLine("{0}BaseClass: {1}".Fmt(defaultValue("BaseClass"), Config.BaseClass));
            sb.AppendLine("{0}AddModelExtensions: {1}".Fmt(defaultValue("AddModelExtensions"), Config.AddModelExtensions));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}MakePropertiesOptional: {1}".Fmt(defaultValue("MakePropertiesOptional"), Config.MakePropertiesOptional));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeGenericBaseTypes: {1}".Fmt(defaultValue("ExcludeGenericBaseTypes"), Config.ExcludeGenericBaseTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}TreatTypesAsStrings: {1}".Fmt(defaultValue("TreatTypesAsStrings"), Config.TreatTypesAsStrings.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));
            AddQueryParamOptions.Each(name => sb.AppendLine($"{defaultValue(name)}{name}: {request.QueryString[name]}"));

            sb.AppendLine("*/");
            sb.AppendLine();
        }

        foreach (var typeName in Config.TreatTypesAsStrings.Safe())
        {
            TypeAliases[typeName] = "String";
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

        //Swift doesn't support reusing same type name with different generic arity
        var conflictPartialNames = allTypes.Map(x => x.Name).Distinct()
            .GroupBy(g => g.LeftPart('`'))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        this.conflictTypeNames = allTypes
            .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
            .Map(x => x.Name);

        defaultImports.Each(x => sb.AppendLine($"import {x}"));

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

                    lastNS = AppendType(ref sb, ref sbExt, type, lastNS,
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
                            Op = operation
                        });

                    existingTypes.Add(fullTypeName);
                }
            }
            else if (responseTypes.Contains(type))
            {
                if (!existingTypes.Contains(fullTypeName)
                    && !Config.IgnoreTypesInNamespaces.Contains(type.Namespace))
                {
                    lastNS = AppendType(ref sb, ref sbExt, type, lastNS,
                        new CreateTypeOptions
                        {
                            IsResponse = true,
                        });

                    existingTypes.Add(fullTypeName);
                }
            }
            else if (types.Contains(type) && !existingTypes.Contains(fullTypeName))
            {
                lastNS = AppendType(ref sb, ref sbExt, type, lastNS,
                    new CreateTypeOptions { IsType = true });

                existingTypes.Add(fullTypeName);
            }
        }

        if (Config.AddModelExtensions)
        {
            sb.AppendLine();
            sb.AppendLine(sbExt.ToString());
        }

        var addCode = AddCodeFilter?.Invoke(allTypes, Config);
        if (addCode != null)
            sb.AppendLine(addCode);

        return StringBuilderCache.ReturnAndFree(sbInner);
    }

    private List<string> RemoveIgnoredTypes(MetadataTypes metadata)
    {
        var includeList = metadata.RemoveIgnoredTypes(Config);
        metadata.Types.RemoveAll(x => IgnoreTypeNames.Contains(x.Name));
        return includeList;
    }

    private string AppendType(ref StringBuilderWrapper sb, ref StringBuilderWrapper sbExt, MetadataType type, string lastNS,
        CreateTypeOptions options)
    {
        //sb = sb.Indent();
        if (IgnoreTypeNames.Contains(type.Name))
            return lastNS;

        var hasGenericBaseType = type.Inherits != null && !type.Inherits.GenericArgs.IsEmpty();
        if (Config.ExcludeGenericBaseTypes && hasGenericBaseType)
        {
            sb.AppendLine("//Excluded {0} : {1}<{2}>".Fmt(type.Name, type.Inherits.Name.LeftPart('`'), string.Join(",", type.Inherits.GenericArgs)));
            return lastNS;
        }

        sb.AppendLine();
        AppendComments(sb, type.Description);
        if (options?.Routes != null)
        {
            AppendAttributes(sb, options.Routes.ConvertAll(x => x.ToMetadataAttribute()));
        }
        AppendAttributes(sb, type.Attributes);
        AppendDataContract(sb, type.DataContract);

        sb.Emit(type, Lang.Swift);
        PreTypeFilter?.Invoke(sb, type);

        if (type.IsEnum.GetValueOrDefault())
        {
            var hasIntValues = type.EnumValues?.Count > 1;
            var enumType = hasIntValues ? "Int" : "String";
            sb.AppendLine($"public enum {Type(type.Name, type.GenericArgs)} : {enumType}, Codable");
            sb.AppendLine("{");
            sb = sb.Indent();

            if (type.EnumNames != null)
            {
                for (var i = 0; i < type.EnumNames.Count; i++)
                {
                    var name = EnumNameStrategy(type.EnumNames[i]);
                    var value = type.EnumValues?[i];
                    sb.AppendLine(value == null
                        ? $"case {name}"
                        : $"case {name} = {value}");
                }
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
        }
        else
        {
            var defType = "class";
            var typeName = AddGenericConstraints(Type(type.Name, type.GenericArgs));
            var extends = new List<string>();

            //: BaseClass, Interfaces
            if (type.Inherits != null)
            {
                var baseType = Type(type.Inherits).InheritedType();
                extends.Add(baseType);
            }
            else if (Config.BaseClass != null && !type.IsInterface())
            {
                extends.Add(Config.BaseClass);
            }

            var typeAliases = new List<string>();

            var isSelfRefType = false;
            if (options.ImplementsFn != null)
            {
                //Swift doesn't support Generic Interfaces like IReturn<T> 
                //Converting them into protocols with type aliases instead 
                ExtractTypeAliases(options, typeAliases, extends, ref sbExt);
                isSelfRefType = options.Op != null && type == options.Op.Response;
            }
            type.Implements.Each(x => extends.Add(Type(x)));

            if (type.IsInterface())
            {
                defType = "protocol";

                //Extract Protocol Arguments into different type aliases
                if (!type.GenericArgs.IsEmpty())
                {
                    typeName = Type(type.Name, null);
                    foreach (var arg in type.GenericArgs)
                    {
                        typeAliases.Add($"associatedtype {arg}");
                    }
                }
            }
            else if (type.Inherits == null) //redundant specifiers not allowed
            {
                extends.Add("Codable");
            }

            var extend = extends.Count > 0
                ? " : " + (string.Join(", ", extends.ToArray()))
                : "";

            var useTypeName = type.GenericArgs?.Length > 0
                ? typeName
                : typeName.LeftPart('<');
            sb.AppendLine($"public {defType} {useTypeName}{extend}");
            sb.AppendLine("{");

            sb = sb.Indent();
            InnerTypeFilter?.Invoke(sb, type);

            if (typeAliases.Count > 0)
            {
                foreach (var typeAlias in typeAliases)
                {
                    sb.AppendLine(typeAlias);
                }
                sb.AppendLine();
            }

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (addVersionInfo)
            {
                sb.AppendLine($"public var {GetPropertyName("Version")}:Int = {Config.AddImplicitVersion}");
            }

            var typeProps = type.Properties ?? TypeConstants<MetadataPropertyType>.EmptyList;
            var initCollections = !type.IsInterface() && Config.InitializeCollections && feature.ShouldInitializeCollection(type);
            AddProperties(sb, type,
                initCollections: initCollections,
                includeResponseStatus: Config.AddResponseStatus && options.IsResponse && 
                                       typeProps.All(x => x.Name != nameof(ResponseStatus)));

            if (typeProps.Count > 0)
                sb.AppendLine();
                
            if (!type.IsInterface())
            {
                if (extends.Count > 0 && (type.Inherits != null || OverrideInitForBaseClasses.Contains(extends[0])))
                {
                    sb.AppendLine("required public init(){ super.init() }");
                }
                else
                {
                    sb.AppendLine("required public init(){}");
                }
            }

            //Swift compiler wont synthesize Codable impls for inherited types or types with self-referencing type aliases
            var hasSynthesize = type.Type?.HasAttribute<SynthesizeAttribute>() == true;
            var needsExplicitCodableImpl = type.Inherits != null || isSelfRefType || hasSynthesize;
            if (!type.IsInterface() && needsExplicitCodableImpl)
            {
                sb.AppendLine();
                if (typeProps.Count > 0)
                {
                    sb.AppendLine("private enum CodingKeys : String, CodingKey {");
                    sb = sb.Indent();
                    foreach (var prop in typeProps)
                    {
                        sb.AppendLine($"case {GetPropertyName(prop)}");
                    }
                    sb = sb.UnIndent();
                    sb.AppendLine("}");
                    sb.AppendLine();
                }
                sb.AppendLine("required public init(from decoder: Decoder) throws {");
                sb = sb.Indent();
                if (type.Inherits != null)
                    sb.AppendLine("try super.init(from: decoder)");
                if (typeProps.Count > 0)
                {
                    sb.AppendLine("let container = try decoder.container(keyedBy: CodingKeys.self)");
                    foreach (var prop in typeProps)
                    {
                        var propTypeName = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
                        var realTypeName = propTypeName.TrimEnd('?');
                        var converter = Converters.TryGetValue(realTypeName, out var c)
                            ? c
                            : null;
                        var propName = GetPropertyName(prop);
                        var defaultValue = prop.IsArray()
                            ? " ?? []"
                            : !type.IsInterface() && !prop.GenericArgs.IsEmpty()
                                ? ArrayTypes.Contains(prop.Type)
                                    ? " ?? []"
                                    : DictionaryTypes.Contains(prop.Type)
                                        ? " ?? [:]"
                                        : ""
                                : "";
                        var method = converter?.DecodeMethod ?? "decodeIfPresent";
                        sb.AppendLine($"{propName} = try container.{method}({realTypeName}.self, forKey: .{propName}){defaultValue}");
                    }
                }
                sb = sb.UnIndent();
                sb.AppendLine("}");
                    
                sb.AppendLine();
                var sig = type.Inherits != null ? " override" : "";
                sb.AppendLine($"public{sig} func encode(to encoder: Encoder) throws {{");
                sb = sb.Indent();
                if (type.Inherits != null)
                    sb.AppendLine("try super.encode(to: encoder)");
                if (typeProps.Count > 0)
                {
                    sb.AppendLine("var container = encoder.container(keyedBy: CodingKeys.self)");
                    foreach (var prop in typeProps)
                    {
                        var propTypeName = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
                        var realTypeName = propTypeName.TrimEnd('?');
                        var converter = Converters.TryGetValue(realTypeName, out var c)
                            ? c
                            : null;
                            
                        var propName = GetPropertyName(prop);
                        var isCollection = prop.IsArray() || ArrayTypes.Contains(prop.Type) || DictionaryTypes.Contains(prop.Type);
                        var method = converter?.EncodeMethod ?? "encode";
                        
                        var (optional, defaultValue) = GetPropInfo(prop, initCollections);
                        if (propTypeName.EndsWith("?"))
                            optional = "?";
                        
                        sb.AppendLine(isCollection
                            ? (optional == "?"
                                ? $"if {propName} != nil && {propName}!.count > 0 "
                                : $"if {propName}.count > 0 ") + ('{' + $" try container.{method}({propName}, forKey: .{propName}) " + '}')
                            : $"if {propName} != nil {{ try container.{method}({propName}, forKey: .{propName}) }}");
                    }
                }
                sb = sb.UnIndent();
                sb.AppendLine("}");

            }
                
            sb = sb.UnIndent();
            sb.AppendLine("}");
        }

        PostTypeFilter?.Invoke(sb, type);

        //sb = sb.UnIndent();

        return lastNS;
    }

    private void ExtractTypeAliases(CreateTypeOptions options, List<string> typeAliases, List<string> extends, ref StringBuilderWrapper sbExt)
    {
        var implStr = options.ImplementsFn();
        if (!string.IsNullOrEmpty(implStr))
        {
            var interfaceParts = implStr.SplitOnFirst('<');
            if (interfaceParts.Length > 1)
            {
                implStr = interfaceParts[0];

                //Strip 'I' prefix for interfaces and use as typealias for protocol
                var alias = implStr.StartsWith("I")
                    ? implStr.Substring(1)
                    : implStr;

                var genericType = interfaceParts[1].Substring(0, interfaceParts[1].Length - 1);
                typeAliases.Add($"public typealias {alias} = {genericType}");
            }

            extends.Add(implStr);
        }
    }

    public List<MetadataPropertyType> GetProperties(MetadataType type)
    {
        var to = new List<MetadataPropertyType>();

        if (type.Properties != null)
            to.AddRange(type.Properties);

        if (type.Inherits != null)
        {
            var baseType = FindType(type.Inherits);
            if (baseType != null)
            {
                to.AddRange(GetProperties(baseType));
            }
        }

        return to;
    }

    public void AddProperties(StringBuilderWrapper sb, MetadataType type,
        bool initCollections, bool includeResponseStatus)
    {
        var wasAdded = false;

        var allBaseProps = new HashSet<string>();
        if (type.Type != null)
        {
            var baseType = type.Type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                foreach (var prop in baseType.GetProperties())
                {
                    allBaseProps.Add(prop.Name);
                }
                baseType = baseType.BaseType;
            }
        }
        
        var dataMemberIndex = 1;
        foreach (var prop in type.Properties.Safe())
        {
            if (allBaseProps.Contains(prop.Name)) continue;
            if (wasAdded) sb.AppendLine();

            var propTypeName = GetPropertyType(prop);
            propTypeName = PropertyTypeFilter?.Invoke(this, type, prop) ?? propTypeName;

            var propType = FindType(prop.Type, prop.Namespace, prop.GenericArgs);

            var (optional, defaultValue) = GetPropInfo(prop, initCollections);
            if (propTypeName.EndsWith("?"))
            {
                propTypeName = propTypeName.Substring(0, propTypeName.Length - 1);
                optional = "?";
            }

            if (propType.IsInterface() || IgnorePropertyNames.Contains(prop.Name))
            {
                sb.AppendLine("//{0}:{1} ignored. Swift doesn't support interface properties"
                    .Fmt(GetPropertyName(prop.Name), propTypeName));
                continue;
            }
            else if (IgnorePropertyTypeNames.Contains(propTypeName))
            {
                sb.AppendLine("//{0}:{1} ignored. Type could not be extended in Swift"
                    .Fmt(GetPropertyName(prop.Name), propTypeName));
                continue;
            }

            wasAdded = AppendComments(sb, prop.Description);
            wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
            wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

            sb.Emit(prop, Lang.Swift);
            PrePropertyFilter?.Invoke(sb, prop, type);
            if (type.IsInterface())
            {
                sb.AppendLine("var {0}:{1}{2} {{ get set }}".Fmt(
                    GetPropertyName(prop), propTypeName, optional));
            }
            else
            {
                var converter = Converters.TryGetValue(propTypeName, out var c)
                    ? c
                    : null;
                var attr = converter?.Attribute != null
                    ? converter.Attribute + " "
                    : "";
                sb.AppendLine(attr + "public var {0}:{1}{2}{3}".Fmt(
                    GetPropertyName(prop), propTypeName, optional, defaultValue));
            }
            PostPropertyFilter?.Invoke(sb, prop, type);
        }

        if (includeResponseStatus)
        {
            if (wasAdded) sb.AppendLine();

            AppendDataMember(sb, null, dataMemberIndex++);
            sb.AppendLine("public var {0}:ResponseStatus?".Fmt(GetPropertyName(nameof(ResponseStatus))));
        }
    }
    
    (string optional, string defaultValue) GetPropInfo(MetadataPropertyType prop, bool initCollections)
    {
        string optional = "";
        string defaultValue = "";
        if (Config.MakePropertiesOptional)
        {
            optional = "?";
        }
        if (prop.Attributes.Safe().FirstOrDefault(x => x.Name == "Required") != null)
        {
            optional = "?"; //always use optional
        }

        if (prop.IsArray())
        {
            optional = "";
            defaultValue = " = []";
        }
        else if (initCollections && !prop.GenericArgs.IsEmpty())
        {
            if (ArrayTypes.Contains(prop.Type))
            {
                optional = "";
                defaultValue = " = []";
            }
            if (DictionaryTypes.Contains(prop.Type))
            {
                optional = "";
                defaultValue = " = [:]";
            }
        }
        return (optional, defaultValue);
    }

    public virtual string GetPropertyType(MetadataPropertyType prop)
    {
        var propTypeName = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
        return propTypeName;
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
        if (type == nameof(Int32))
        {
            if (value == int.MinValue.ToString())
                return "Int32.min";
            if (value == int.MaxValue.ToString())
                return "Int32.max";
        }
        if (type == nameof(Int64))
        {
            if (value == long.MinValue.ToString())
                return "Int64.min";
            if (value == long.MaxValue.ToString())
                return "Int64.max";
        }
        if (alias == "string" || type == "String")
            return value.ToEscapedString();

        if (value.IsTypeValue())
        {
            //Only emit type as Namespaces are merged
            return "typeof(" + value.ExtractTypeName() + ")";
        }

        return value;
    }

    public string Type(MetadataTypeName typeName)
    {
        return Type(typeName.Name, typeName.GenericArgs);
    }

    public MetadataType FindType(string typeName, string typeNamespace, params string[] genericArgs)
    {
        return FindType(new MetadataTypeName
        {
            Name = typeName,
            Namespace = typeNamespace,
            GenericArgs = genericArgs,
        });
    }

    public MetadataType FindType(MetadataTypeName typeName)
    {
        if (typeName == null)
            return null;

        var foundType = allTypes
            .FirstOrDefault(x => (typeName.Namespace == null || x.Namespace == typeName.Namespace)
                                 && x.Name == typeName.Name
                                 && x.GenericArgs.Safe().Count() == typeName.GenericArgs.Safe().Count());

        if (foundType != null)
            return foundType;

#pragma warning disable 618
        if (typeName.Name == nameof(QueryBase) || 
            typeName.Name == typeof(QueryDb<>).Name)
            return CreateType(typeof(QueryBase)); //Properties are on QueryBase
#pragma warning restore 618


        if (typeName.Name == nameof(AuthUserSession))
            return CreateType(typeof(AuthUserSession));

        return null;
    }

    MetadataType CreateType(Type type)
    {
        if (HostContext.TryResolve<INativeTypesMetadata>() is NativeTypesMetadata nativeTypes)
        {
            var typesGenerator = nativeTypes.GetGenerator(Config);
            return typesGenerator.ToType(type);
        }

        return null;
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

    public static HashSet<string> IgnorePropertyTypeNames = new() {
        "Object",
    };

    public static HashSet<string> IgnorePropertyNames = new() {
        "ProviderOAuthAccess",
    };

    public static bool IgnoreArrayReturnTypes = true;

    public string ReturnType(string type, string[] genericArgs)
    {
        if (IgnoreArrayReturnTypes && genericArgs.Any(arg => arg.StartsWith("[")))
            return null; // does not recognize [Array] as conforming to IReturn : Codable

        return Type(type, genericArgs);
    }

    public string Type(string type, string[] genericArgs)
    {
        var useType = TypeFilter?.Invoke(type, genericArgs);
        if (useType != null)
            return useType;

        if (!genericArgs.IsEmpty())
        {
            if (type == "Nullable`1")
                return "{0}?".Fmt(TypeAlias(GenericArg(genericArgs[0])));
            if (type == "Nullable`1[]")
                return $"[{GenericArg(genericArgs[0])}?]";
            if (ArrayTypes.Contains(type))
                return "[{0}]".Fmt(TypeAlias(GenericArg(genericArgs[0]))).StripNullable();
            if (type.EndsWith("[]"))
                return $"[{Type(type.Substring(0,type.Length-2), genericArgs)}]".StripNullable();
            if (DictionaryTypes.Contains(type))
                return "[{0}:{1}]".Fmt(
                    TypeAlias(GenericArg(genericArgs[0])),
                    TypeAlias(GenericArg(genericArgs[1])));

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
                return "{0}<{1}>".Fmt(typeName, StringBuilderCacheAlt.ReturnAndFree(args));
            }
        }
        else if (DictionaryTypes.Contains(type))
        {
            return "[String:String]?";
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

        // Incorrectly converts: [[String:Poco]] -> [String:Poco]
        var isArray = type.EndsWith("[]");
        if (isArray)
            return "[{0}]".Fmt(TypeAlias(type.Substring(0, type.Length - 2)));

        TypeAliases.TryGetValue(type, out var typeAlias);

        return typeAlias ?? NameOnly(type);
    }

    public string NameOnly(string type)
    {
        var name = conflictTypeNames.Contains(type)
            ? type.Replace('`', '_')
            : type.LeftPart('`');

        return name.LastRightPart('.').SafeToken();
    }

    public bool AppendComments(StringBuilderWrapper sb, string desc)
    {
        if (desc != null && Config.AddDescriptionAsComments)
        {
            sb.AppendLine("/**");
            sb.AppendLine("* {0}".Fmt(desc.SafeComment()));
            sb.AppendLine("*/");
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
            return TypeAlias(node.Children[0].Text) + "?";

        if (node.Text == "List")
        {
            sb.Append("[");
            sb.Append(ConvertFromCSharp(node.Children[0]));
            sb.Append("]");
        }
        else if (node.Text == "Dictionary")
        {
            sb.Append("[");
            sb.Append(ConvertFromCSharp(node.Children[0]));
            sb.Append(":");
            sb.Append(ConvertFromCSharp(node.Children[1]));
            sb.Append("]");
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

    public static string AddGenericConstraints(string typeDef)
    {
        return typeDef
            .Replace(",", " : Codable,")
            .Replace(">", " : Codable>");
    }

    public string GetPropertyName(string name) => name.SafeToken().PropertyStyle(); 
    public string GetPropertyName(MetadataPropertyType prop) => 
        prop.GetSerializedAlias() ?? prop.Name.SafeToken().PropertyStyle();
}

public static class SwiftGeneratorExtensions
{
    public static string InheritedType(this string type)
    {
        var isArray = type.StartsWith("[");
        return isArray
            ? "List<{0}>".Fmt(type.Trim('[', ']'))
            : type;
    }

    public static readonly HashSet<string> SwiftKeyWords = new() {
        "class",
        "deinit",
        "enum",
        "extension",
        "func",
        "import",
        "init",
        "let",
        "protocol",
        "static",
        "struct",
        "subscript",
        "typealias",
        "associatedtype",
        "var",
        "break",
        "case",
        "continue",
        "default",
        "do",
        "else",
        "fallthrough",
        "if",
        "in",
        "for",
        "return",
        "switch",
        "where",
        "while",
        "dynamicType",
        "is",
        "new",
        "super",
        "self",
        "Self",
        "Type",
        "didSet",
        "get",
        "infix",
        "inout",
        "left",
        "mutating",
        "none",
        "nonmutating",
        "operator",
        "override",
        "postfix",
        "precedence",
        "prefix",
        "private",
        "public",
        "right",
        "set",
        "unowned",
        "weak",
        "willSet",
    };

    public static string PropertyStyle(this string name)
    {
        var propName = name.ToCamelCase(); //Always use Swift conventions for now

        //can't override NSObject's description computed property
        if (propName == "description")
            return "Description";

        return SwiftKeyWords.Contains(propName)
            ? "`{0}`".Fmt(propName)
            : propName;
    }

    public static string UnescapeReserved(this string name)
    {
        return string.IsNullOrEmpty(name)
            ? name
            : name.TrimStart('`').TrimEnd('`');
    }
}