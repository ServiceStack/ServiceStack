using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Host;

namespace ServiceStack.NativeTypes.Swift
{
    public class Swift4Generator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        List<MetadataType> allTypes;
        List<string> conflictTypeNames = new List<string>();

        public Swift4Generator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }

        public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

        public static List<string> DefaultImports = new List<string>
        {
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
            {"TimeSpan", "TimeInterval"},
            {"DateTimeOffset", "Date"},
            {"Guid", "String"},
            {"Char", "Character"},
            {"Byte", "UInt8"},
            {"Int16", "Int16"},
            {"Int32", "Int"},
            {"Int64", "Int64"},
            {"UInt16", "UInt16"},
            {"UInt32", "UInt32"},
            {"UInt64", "UInt64"},
            {"Single", "Float"},
            {"Double", "Double"},
            {"Decimal", "Double"},
            {"IntPtr", "Int64"},
            {"Stream", "Data"},
            {"Type", "String"},
        }.ToConcurrentDictionary();

        public static TypeFilterDelegate TypeFilter { get; set; }

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

        public string GetCode(MetadataTypes metadata, IRequest request)
        {
            var typeNamespaces = new HashSet<string>();
            var includeList = RemoveIgnoredTypes(metadata);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

            var defaultImports = !Config.DefaultImports.IsEmpty()
                ? Config.DefaultImports
                : DefaultImports;

            string DefaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sbInner = StringBuilderCache.Allocate();
            var sb = new StringBuilderWrapper(sbInner);
            var sbExt = new StringBuilderWrapper(new StringBuilder());
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("SwiftVersion: 4.0");
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            if (Config.UsePath != null)
                sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));

            sb.AppendLine();

            sb.AppendLine("{0}BaseClass: {1}".Fmt(DefaultValue("BaseClass"), Config.BaseClass));
            sb.AppendLine("{0}AddModelExtensions: {1}".Fmt(DefaultValue("AddModelExtensions"), Config.AddModelExtensions));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(DefaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(DefaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(DefaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeGenericBaseTypes: {1}".Fmt(DefaultValue("ExcludeGenericBaseTypes"), Config.ExcludeGenericBaseTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(DefaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(DefaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(DefaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(DefaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}TreatTypesAsStrings: {1}".Fmt(DefaultValue("TreatTypesAsStrings"), Config.TreatTypesAsStrings.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(DefaultValue("DefaultImports"), defaultImports.Join(",")));

            sb.AppendLine("*/");
            sb.AppendLine();

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

        private string AppendType(ref StringBuilderWrapper sb, ref StringBuilderWrapper sbExt, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
            //sb = sb.Indent();

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
                sb.AppendLine($"public enum {Type(type.Name, type.GenericArgs)} : Int");
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

                AddEnumExtension(ref sbExt, type);
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

                    //Swift requires re-declaring base type generics definition on super type
                    var genericDefPos = baseType.IndexOf("<", StringComparison.Ordinal);
                    if (genericDefPos >= 0)
                    {
                        //Need to declare BaseType is JsonSerializable
                        var subBaseType = AddGenericConstraints(baseType.Substring(genericDefPos));

                        typeName += subBaseType;
                    }

                    extends.Add(baseType);
                }
                else if (Config.BaseClass != null && !type.IsInterface())
                {
                    extends.Add(Config.BaseClass);
                }

                var typeAliases = new List<string>();

                if (options.ImplementsFn != null)
                {
                    //Swift doesn't support Generic Interfaces like IReturn<T> 
                    //Converting them into protocols with typealiases instead 
                    ExtractTypeAliases(options, typeAliases, extends, ref sbExt);
                }
                type.Implements.Each(x => extends.Add(Type(x)));

                if (type.IsInterface())
                {
                    defType = "protocol";

                    //Extract Protocol Arguments into different typealiases
                    if (!type.GenericArgs.IsEmpty())
                    {
                        typeName = Type(type.Name, null);
                        foreach (var arg in type.GenericArgs)
                        {
                            typeAliases.Add($"associatedtype {arg}");
                        }
                    }
                }

                var extend = extends.Count > 0
                    ? " : " + (string.Join(", ", extends.ToArray()))
                    : "";

                sb.AppendLine($"public {defType} {typeName}{extend}");
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

                if (!type.IsInterface())
                {
                    if (extends.Count > 0 && OverrideInitForBaseClasses.Contains(extends[0]))
                    {
                        sb.AppendLine("required public override init(){}");
                    }
                    else
                    {
                        sb.AppendLine("required public init(){}");
                    }
                }

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
                if (addVersionInfo)
                {
                    sb.AppendLine($"public var {GetPropertyName("Version")}:Int = {Config.AddImplicitVersion}");
                }

                AddProperties(sb, type,
                    initCollections: !type.IsInterface() && Config.InitializeCollections,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                sb = sb.UnIndent();
                sb.AppendLine("}");

                if (!type.IsInterface())
                {
                    AddTypeExtension(ref sbExt, type,
                        initCollections: Config.InitializeCollections);
                }
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

        private void AddTypeExtension(ref StringBuilderWrapper sbExt, MetadataType type, bool initCollections)
        {
            //Swift doesn't support extensions on same protocol used by Base and Sub types
            if (type.IsAbstract())
                return;

            var typeName = Type(type.Name, type.GenericArgs);
            typeName = typeName.LeftPart('<'); // Type<QueryResponse<T>> into correct mapping Type<QueryResponse>

            var typeNameOnly = typeName.LeftPart('<');

            sbExt.AppendLine();
            sbExt.AppendLine("extension {0} : JsonSerializable".Fmt(typeNameOnly));
            sbExt.AppendLine("{");
            sbExt = sbExt.Indent();

            sbExt.AppendLine("public static var typeName:String {{ return \"{0}\" }}".Fmt(typeName));

            //func typeConfig()

            var isGenericType = type.GenericArgs?.Length > 0 || type.Inherits?.GenericArgs?.Length > 0;
            if (!isGenericType)
            {
                sbExt.AppendLine("public static var metadata = Metadata.create([");
                sbExt = sbExt.Indent();
            }
            else
            {
                //Swift 2.0 doesn't allow stored static properties on generic types yet
                sbExt.AppendLine("public static var metadata:Metadata {");
                sbExt = sbExt.Indent();
                sbExt.AppendLine("return Metadata.create([");
            }

            sbExt = sbExt.Indent();

            foreach (var prop in GetProperties(type))
            {
                var propType = FindType(prop.Type, prop.TypeNamespace, prop.GenericArgs);
                if (propType.IsInterface()
                    || IgnorePropertyTypeNames.Contains(prop.Type)
                    || IgnorePropertyNames.Contains(prop.Name))
                    continue;

                var fnName = "property";
                if (prop.IsArray() || ArrayTypes.Contains(prop.Type))
                {
                    fnName = initCollections
                        ? "arrayProperty"
                        : "optionalArrayProperty";
                }
                else if (DictionaryTypes.Contains(prop.Type))
                {
                    fnName = initCollections
                        ? "objectProperty"
                        : "optionalObjectProperty";
                }
                else
                {
                    if (propType != null && !propType.IsEnum.GetValueOrDefault())
                    {
                        fnName = "optionalObjectProperty";
                    }
                    else
                    {
                        fnName = "optionalProperty";
                    }
                }

                var propName = GetPropertyName(prop.Name);
                var unescapedName = propName.UnescapeReserved();
                sbExt.AppendLine("Type<{0}>.{1}(\"{2}\", get: {{ $0.{3} }}, set: {{ $0.{3} = $1 }}),".Fmt(
                        typeName, fnName, unescapedName, propName));
            }
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("])");
            sbExt = sbExt.UnIndent();

            if (isGenericType)
            {
                sbExt.AppendLine("}");
            }

            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");
        }

        private void AddEnumExtension(ref StringBuilderWrapper sbExt, MetadataType type)
        {
            if (type.EnumNames == null) return;

            sbExt.AppendLine();
            var typeName = Type(type.Name, type.GenericArgs);
            sbExt.AppendLine("extension {0} : StringSerializable".Fmt(typeName));
            sbExt.AppendLine("{");
            sbExt = sbExt.Indent();

            sbExt.AppendLine("public static var typeName:String {{ return \"{0}\" }}".Fmt(typeName));

            //toJson()
            sbExt.AppendLine("public func toJson() -> String {");
            sbExt = sbExt.Indent();
            sbExt.AppendLine("return jsonStringRaw(toString())");
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //toString()
            sbExt.AppendLine("public func toString() -> String {");
            sbExt = sbExt.Indent();
            sbExt.AppendLine("switch self {");
            foreach (var name in type.EnumNames)
            {
                var swiftName = EnumNameStrategy(name);
                sbExt.AppendLine($"case .{swiftName}: return \"{name}\"");
            }
            sbExt.AppendLine("}");
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromString()
            sbExt.AppendLine("public static func fromString(_ strValue:String) -> {0}? {{".Fmt(typeName));
            sbExt = sbExt.Indent();

            sbExt.AppendLine("switch strValue {");
            foreach (var name in type.EnumNames)
            {
                var swiftName = EnumNameStrategy(name);
                sbExt.AppendLine($"case \"{name}\": return .{swiftName}");
            }
            sbExt.AppendLine("default: return nil");

            sbExt.AppendLine("}");
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromObject()
            sbExt.AppendLine("public static func fromObject(_ any:Any) -> {0}? {{".Fmt(typeName));
            sbExt = sbExt.Indent();
            sbExt.AppendLine("switch any {");
            sbExt.AppendLine("case let i as Int: return {0}(rawValue: i)".Fmt(typeName));
            sbExt.AppendLine("case let s as String: return fromString(s)");
            sbExt.AppendLine("default: return nil");
            sbExt.AppendLine("}");
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type,
            bool initCollections, bool includeResponseStatus)
        {
            var wasAdded = false;

            var dataMemberIndex = 1;
            foreach (var prop in type.Properties.Safe())
            {
                if (wasAdded) sb.AppendLine();

                var propTypeName = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
                var propType = FindType(prop.Type, prop.TypeNamespace, prop.GenericArgs);
                var optional = "";
                var defaultValue = "";
                if (propTypeName.EndsWith("?"))
                {
                    propTypeName = propTypeName.Substring(0, propTypeName.Length - 1);
                    optional = "?";
                }
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
                        GetPropertyName(prop.Name), propTypeName, optional));
                }
                else
                {
                    sb.AppendLine("public var {0}:{1}{2}{3}".Fmt(
                        GetPropertyName(prop.Name), propTypeName, optional, defaultValue));
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

            if (value.StartsWith("typeof("))
            {
                //Only emit type as Namespaces are merged
                var typeNameOnly = value.Substring(7, value.Length - 8).LastRightPart('.');
                return "typeof(" + typeNameOnly + ")";
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
                return null; // does not recognize [Array] as conforming to IReturn : JsonSerializable

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
                if (ArrayTypes.Contains(type))
                    return "[{0}]".Fmt(TypeAlias(GenericArg(genericArgs[0]))).StripNullable();
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
            else
            {
                type = type.StripNullable();
            }

            return TypeAlias(type);
        }

        private string TypeAlias(string type)
        {
            type = type.SanitizeType();
            var isArray = type.StartsWith("[") || type.EndsWith("[]");
            if (isArray)
                return "[{0}]".Fmt(TypeAlias(type.Trim('[', ']')));

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
                .Replace(",", " : JsonSerializable,")
                .Replace(">", " : JsonSerializable>");
        }

        public string GetPropertyName(string name) => name.SafeToken().PropertyStyle();
    }
}