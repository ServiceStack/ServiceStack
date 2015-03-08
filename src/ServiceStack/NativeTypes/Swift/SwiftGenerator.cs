using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Host;

namespace ServiceStack.NativeTypes.Swift
{
    public class SwiftGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly List<MetadataType> AllTypes;
        List<string> conflictTypeNames = new List<string>();

        public SwiftGenerator(MetadataTypesConfig config)
        {
            Config = config;
            AllTypes = new List<MetadataType>();
        }

        public static List<string> DefaultImports = new List<string>
        {
            "Foundation",    
        };

        public static Dictionary<string, string> TypeAliases = new Dictionary<string, string>
        {
            {"Boolean", "Bool"},
            {"DateTime", "NSDate"},
            {"TimeSpan", "NSTimeInterval"},
            {"DateTimeOffset", "NSDate"},
            {"Guid", "String"},
            {"Char", "Character"},
            {"Byte", "Int8"},
            {"Int16", "Int16"},
            {"Int32", "Int"},
            {"Int64", "Int64"},
            {"UInt16", "UInt16"},
            {"UInt32", "UInt32"},
            {"UInt64", "UInt64"},
            {"Single", "Float"},
            {"Double", "Double"},
            {"Decimal", "Double"},
        };

        public static HashSet<string> OverrideInitForBaseClasses = new HashSet<string> {
            "NSObject"
        };

        public string GetCode(MetadataTypes metadata, IRequest request)
        {
            var typeNamespaces = new HashSet<string>();
            metadata.RemoveIgnoredTypes(Config);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

            var defaultImports = !Config.DefaultImports.IsEmpty()
                ? Config.DefaultImports
                : DefaultImports;

            Func<string, string> defaultValue = k =>
                request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sb = new StringBuilderWrapper(new StringBuilder());
            var sbExt = new StringBuilderWrapper(new StringBuilder());
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(metadata.Version));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();

            sb.AppendLine("{0}BaseClass: {1}".Fmt(defaultValue("BaseClass"), Config.BaseClass));
            sb.AppendLine("{0}AddModelExtensions: {1}".Fmt(defaultValue("AddModelExtensions"), Config.AddModelExtensions));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));

            sb.AppendLine("*/");
            sb.AppendLine();

            string lastNS = null;

            var existingTypes = new HashSet<string>();

            var requestTypes = metadata.Operations.Select(x => x.Request).ToHashSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToHashSet();
            var types = metadata.Types.ToHashSet();

            AllTypes.AddRange(types);
            AllTypes.AddRange(responseTypes);
            AllTypes.AddRange(requestTypes);

            //Swift doesn't support reusing same type name with different generic airity
            var conflictPartialNames = AllTypes.Map(x => x.Name).Distinct()
                .GroupBy(g => g.SplitOnFirst('`')[0])
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            this.conflictTypeNames = AllTypes
                .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
                .Map(x => x.Name);

            defaultImports.Each(x => sb.AppendLine("import {0};".Fmt(x)));

            //ServiceStack core interfaces
            foreach (var type in AllTypes)
            {
                var fullTypeName = type.GetFullName();
                if (requestTypes.Contains(type))
                {
                    if (!existingTypes.Contains(fullTypeName))
                    {
                        MetadataType response = null;
                        MetadataOperationType operation;
                        if (requestTypesMap.TryGetValue(type, out operation))
                        {
                            response = operation.Response;
                        }

                        lastNS = AppendType(ref sb, ref sbExt, type, lastNS,
                            new CreateTypeOptions
                            {
                                ImplementsFn = () =>
                                {
                                    if (!Config.AddReturnMarker
                                        && !type.ReturnVoidMarker
                                        && type.ReturnMarkerTypeName == null)
                                        return null;

                                    if (type.ReturnVoidMarker)
                                        return "IReturnVoid";
                                    if (type.ReturnMarkerTypeName != null)
                                        return ReturnType("IReturn`1", new[] { Type(type.ReturnMarkerTypeName) });
                                    return response != null
                                        ? ReturnType("IReturn`1", new[] { Type(response.Name, response.GenericArgs) })
                                        : null;
                                },
                                IsRequest = true,
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

            return sb.ToString();
        }

        private string AppendType(ref StringBuilderWrapper sb, ref StringBuilderWrapper sbExt, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
            //sb = sb.Indent();

            sb.AppendLine();
            AppendComments(sb, type.Description);
            if (type.Routes != null)
            {
                AppendAttributes(sb, type.Routes.ConvertAll(x => x.ToMetadataAttribute()));
            }
            AppendAttributes(sb, type.Attributes);
            AppendDataContract(sb, type.DataContract);

            if (type.IsEnum.GetValueOrDefault())
            {
                sb.AppendLine("public enum {0} : Int".Fmt(Type(type.Name, type.GenericArgs)));
                sb.AppendLine("{");
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues != null ? type.EnumValues[i] : null;
                        sb.AppendLine(value == null
                            ? "case {0}".Fmt(name)
                            : "case {0} = {1}".Fmt(name, value));
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");

                AddEnumExtension(ref sbExt, type);
            }
            else
            {
                var defType = "class";
                var typeName = Type(type.Name, type.GenericArgs).AddGenericConstraints();
                var extends = new List<string>();

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                {
                    var baseType = Type(type.Inherits).InheritedType();

                    //Swift requires re-declaring base type generics definition on super type
                    var genericDefPos = baseType.IndexOf("<");
                    if (genericDefPos >= 0)
                    {
                        //Need to declare BaseType is JsonSerializable
                        var subBaseType = baseType.Substring(genericDefPos)
                            .AddGenericConstraints();

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
                    ExtractTypeAliases(options, typeAliases, extends);
                }

                if (type.IsInterface())
                {
                    defType = "protocol";

                    //Extract Protocol Arguments into different typealiases
                    if (!type.GenericArgs.IsEmpty())
                    {
                        typeName = Type(type.Name, null);
                        foreach (var arg in type.GenericArgs)
                        {
                            typeAliases.Add("typealias {0} = {0}".Fmt(arg));
                        }
                    }
                }

                var extend = extends.Count > 0
                    ? " : " + (string.Join(", ", extends.ToArray()))
                    : "";

                sb.AppendLine("public {0} {1}{2}".Fmt(defType, typeName, extend));
                sb.AppendLine("{");

                sb = sb.Indent();

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
                    sb.AppendLine("public var {0}:Int = {1}".Fmt("Version".PropertyStyle(), Config.AddImplicitVersion));
                }

                AddProperties(sb, type,
                    initCollections: !type.IsInterface() && Config.InitializeCollections,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != typeof(ResponseStatus).Name));

                sb = sb.UnIndent();
                sb.AppendLine("}");

                if (!type.IsInterface())
                {
                    AddTypeExtension(ref sbExt, type,
                        initCollections: Config.InitializeCollections);
                }
            }

            //sb = sb.UnIndent();

            return lastNS;
        }

        private static void ExtractTypeAliases(CreateTypeOptions options, List<string> typeAliases, List<string> extends)
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

                    typeAliases.Add("typealias {0} = {1}".Fmt(alias, genericType));
                }

                extends.Add(implStr);
            }
        }

        public List<MetadataPropertyType> GetPropertes(MetadataType type)
        {
            var to = new List<MetadataPropertyType>();

            if (type.Properties != null)
                to.AddRange(type.Properties);

            if (type.Inherits != null)
            {
                var baseType = FindType(type.Inherits);
                if (baseType != null)
                {
                    to.AddRange(GetPropertes(baseType));
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

            var typeNameOnly = typeName.SplitOnFirst('<')[0];

            sbExt.AppendLine();
            sbExt.AppendLine("extension {0} : JsonSerializable".Fmt(typeNameOnly));
            sbExt.AppendLine("{");
            sbExt = sbExt.Indent();

            sbExt.AppendLine("public class var typeName:String {{ return \"{0}\" }}".Fmt(typeName));

            //func typeConfig()
            sbExt.AppendLine("public class func reflect() -> Type<{0}> {{".Fmt(typeName));
            sbExt = sbExt.Indent();
            sbExt.AppendLine(
                "return TypeConfig.config() ?? TypeConfig.configure(Type<{0}>(".Fmt(typeName));
            sbExt = sbExt.Indent();

            sbExt.AppendLine("properties: [".Fmt(typeName));
            sbExt = sbExt.Indent();

            foreach (var prop in GetPropertes(type))
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

                sbExt.AppendLine("Type<{0}>.{1}(\"{2}\", get: {{ $0.{2} }}, set: {{ $0.{2} = $1 }}),".Fmt(
                        typeName, fnName, prop.Name.SafeToken().PropertyStyle()));
            }
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("]))");

            sbExt = sbExt.UnIndent();
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");


            //toJson()
            sbExt.AppendLine("public func toJson() -> String {");
            sbExt = sbExt.Indent();
            sbExt.AppendLine("return {0}.reflect().toJson(self)".Fmt(typeName));
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromJson()
            sbExt.AppendLine("public class func fromJson(json:String) -> {0}? {{".Fmt(typeName));
            sbExt = sbExt.Indent();
            sbExt.AppendLine("return {0}.reflect().fromJson({0}(), json: json)".Fmt(typeName));
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromObject()
            sbExt.AppendLine("public class func fromObject(any:AnyObject) -> {0}? {{".Fmt(typeName));
            sbExt = sbExt.Indent();
            sbExt.AppendLine("return {0}.reflect().fromObject({0}(), any:any)".Fmt(typeName));
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //toString()
            sbExt.AppendLine("public func toString() -> String {");
            sbExt = sbExt.Indent();
            sbExt.AppendLine("return {0}.reflect().toString(self)".Fmt(typeName));
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromString()
            sbExt.AppendLine("public class func fromString(string:String) -> {0}? {{".Fmt(typeName));
            sbExt = sbExt.Indent();
            sbExt.AppendLine("return {0}.reflect().fromString({0}(), string: string)".Fmt(typeName));
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

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
                sbExt.AppendLine("case .{0}: return \"{0}\"".Fmt(name));
            }
            sbExt.AppendLine("}");
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromString()
            sbExt.AppendLine("public static func fromString(strValue:String) -> {0}? {{".Fmt(typeName));
            sbExt = sbExt.Indent();

            sbExt.AppendLine("switch strValue {");
            foreach (var name in type.EnumNames)
            {
                sbExt.AppendLine("case \"{0}\": return .{0}".Fmt(name));
            }
            sbExt.AppendLine("default: return nil");

            sbExt.AppendLine("}");
            sbExt = sbExt.UnIndent();
            sbExt.AppendLine("}");

            //fromObject()
            sbExt.AppendLine("public static func fromObject(any:AnyObject) -> {0}? {{".Fmt(typeName));
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

                var propTypeName = Type(prop.Type, prop.GenericArgs);
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
                        .Fmt(prop.Name.SafeToken().PropertyStyle(), propTypeName));
                    continue;
                }
                else if (IgnorePropertyTypeNames.Contains(propTypeName))
                {
                    sb.AppendLine("//{0}:{1} ignored. Type could not be extended in Swift"
                        .Fmt(prop.Name.SafeToken().PropertyStyle(), propTypeName));
                    continue;
                }

                wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                if (type.IsInterface())
                {
                    sb.AppendLine("var {0}:{1}{2} {{ get set }}".Fmt(
                        prop.Name.SafeToken().PropertyStyle(), propTypeName, optional));
                }
                else
                {
                    sb.AppendLine("public var {0}:{1}{2}{3}".Fmt(
                        prop.Name.SafeToken().PropertyStyle(), propTypeName, optional, defaultValue));
                }
            }

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("public var {0}:ResponseStatus?".Fmt(typeof(ResponseStatus).Name.PropertyStyle()));
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
                    var args = new StringBuilder();
                    if (attr.ConstructorArgs != null)
                    {
                        foreach (var ctorArg in attr.ConstructorArgs)
                        {
                            if (args.Length > 0)
                                args.Append(", ");
                            args.Append("{0}".Fmt(TypeValue(ctorArg.Type, ctorArg.Value)));
                        }
                    }
                    else if (attr.Args != null)
                    {
                        foreach (var attrArg in attr.Args)
                        {
                            if (args.Length > 0)
                                args.Append(", ");
                            args.Append("{0}={1}".Fmt(attrArg.Name, TypeValue(attrArg.Type, attrArg.Value)));
                        }
                    }
                    sb.AppendLine("// @{0}({1})".Fmt(attr.Name, args));
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
                return value.QuotedSafeValue();

            if (value.StartsWith("typeof("))
            {
                //Only emit type as Namespaces are merged
                var typeNameOnly = value.Substring(7, value.Length - 8).SplitOnLast('.').Last();
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

            var foundType = AllTypes
                .FirstOrDefault(x => (typeName.Namespace == null || x.Namespace == typeName.Namespace)
                    && x.Name == typeName.Name
                    && x.GenericArgs.Safe().Count() == typeName.GenericArgs.Safe().Count());

            if (foundType != null)
                return foundType;

            if (typeName.Name == typeof(QueryBase).Name || typeName.Name == typeof(QueryBase<>).Name)
                return CreateType(typeof(QueryBase)); //Properties are on QueryBase


            if (typeName.Name == typeof(AuthUserSession).Name)
                return CreateType(typeof(AuthUserSession));

            return null;
        }

        MetadataType CreateType(Type type)
        {
            var nativeTypes = HostContext.TryResolve<INativeTypesMetadata>() as NativeTypesMetadata;
            if (nativeTypes != null)
            {
                var typesGenerator = nativeTypes.GetMetadataTypesGenerator(Config);
                return typesGenerator.ToType(type);
            }

            return null;
        }

        public static HashSet<string> ArrayTypes = new HashSet<string>
        {
            "List`1",
            "IEnumerable`1",
            "ICollection`1",
            "HashSet`1",
            "Queue`1",
            "Stack`1",
            "IEnumerable",
        };

        public static HashSet<string> DictionaryTypes = new HashSet<string>
        {
            "Dictionary`2",
            "IDictionary`2",
            "IOrderedDictionary`2",
            "OrderedDictionary",
            "StringDictionary",
            "IDictionary",
            "IOrderedDictionary",
        };

        public static HashSet<string> IgnorePropertyTypeNames = new HashSet<string>
        {
            "Object",
        };

        public static HashSet<string> IgnorePropertyNames = new HashSet<string>
        {
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
            if (!genericArgs.IsEmpty())
            {
                if (type == "Nullable`1")
                    return "{0}?".Fmt(TypeAlias(GenericArg(genericArgs[0])));
                if (ArrayTypes.Contains(type))
                    return "[{0}]".Fmt(TypeAlias(GenericArg(genericArgs[0])));
                if (DictionaryTypes.Contains(type))
                    return "[{0}:{1}]".Fmt(
                        TypeAlias(GenericArg(genericArgs[0])),
                        TypeAlias(GenericArg(genericArgs[1])));

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = new StringBuilder();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(GenericArg(arg));
                    }

                    var typeName = TypeAlias(type);
                    return "{0}<{1}>".Fmt(typeName, args);
                }
            }

            return TypeAlias(type);
        }

        private string TypeAlias(string type)
        {
            type = type.SanitizeType();
            var isArray = type.StartsWith("[") || type.EndsWith("[]");
            if (isArray)
                return "[{0}]".Fmt(TypeAlias(type.Trim('[', ']')));

            string typeAlias;
            TypeAliases.TryGetValue(type, out typeAlias);

            return typeAlias ?? NameOnly(type);
        }

        public string NameOnly(string type)
        {
            var name = conflictTypeNames.Contains(type)
                ? type.Replace('`', '_')
                : type.SplitOnFirst('`')[0];

            return name.SplitOnLast('.').Last().SafeToken();
        }

        public void AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc == null) return;

            sb.AppendLine("/**");
            sb.AppendLine("* {0}".Fmt(desc.SafeComment()));
            sb.AppendLine("*/");
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
            return typeName.SplitOnLast('.').Last(); //remove nested class
        }
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

        public static HashSet<string> SwiftKeyWords = new HashSet<string>
        {
            "continue",
            "description", //can't override NSObject's description computed property
        };

        public static string PropertyStyle(this string name)
        {
            var propName = name.ToCamelCase(); //Always use Swift conventions for now
            return SwiftKeyWords.Contains(propName)
                ? propName.ToPascalCase() 
                : propName;

            //return JsConfig.EmitCamelCaseNames
            //    ? name.ToCamelCase()
            //    : JsConfig.EmitLowercaseUnderscoreNames
            //        ? name.ToLowercaseUnderscore()
            //        : name;
        }

        public static string AddGenericConstraints(this string typeDef)
        {
            return typeDef
                .Replace(",", " : JsonSerializable,")
                .Replace(">", " : JsonSerializable>");
        }
    }
}