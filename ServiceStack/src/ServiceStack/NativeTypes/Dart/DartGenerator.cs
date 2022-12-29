using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Dart 
{
    public class DartGenerator : ILangGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        List<string> conflictTypeNames = new();
        List<MetadataType> allTypes;
        Dictionary<string, MetadataType> allTypesMap;
        private HashSet<string> existingTypeInfos;
        private StringBuilder sbTypeInfos;

        public DartGenerator(MetadataTypesConfig config)
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
//            "dart:collection",  Required for inheriting List<T> / ListBase 
//            "dart:typed_data",  Required for byte[] / Uint8List
            "package:servicestack/servicestack.dart"
        };
        
        public static Dictionary<string, string> TypeAliases = new() {
            {"Object", "dynamic"},
            {"String", "String"},
            {"Boolean", "bool"},
            {"DateTime", "DateTime"},
            {"DateOnly", "DateTime"},
            {"DateTimeOffset", "DateTime"},
            {"TimeSpan", "Duration"},
            {"TimeOnly", "Duration"},
            {"Guid", "String"},
            {"Char", "String"},
            {"Byte", "int"},
            {"Int16", "int"},
            {"Int32", "int"},
            {"Int64", "int"},
            {"UInt16", "int"},
            {"UInt32", "int"},
            {"UInt64", "int"},
            {"Single", "double"},
            {"Double", "double"},
            {"Decimal", "double"},
            {"IntPtr", "int"},
            {"List", "List"},
            {"Byte[]", "Uint8List"},
            {"Stream", "Uint8List"},
            {"HttpWebResponse", "Uint8List"},
            {"IDictionary", "dynamic"},
            {"Type", "String"},
        };
        private static string declaredEmptyString = "\"\"";
        private static readonly Dictionary<string, string> defaultValues = new() {
            {"String", declaredEmptyString},
            {"string", declaredEmptyString},
            {"Boolean", "false"},
            {"boolean", "false"},
            {"Guid", declaredEmptyString},
            {"Char", declaredEmptyString},
            {"int", "0"},
            {"float", "0"},
            {"double", "0"},
            {"Byte", "0"},
            {"Int16", "0"},
            {"Int32", "0"},
            {"Int64", "0"},
            {"UInt16", "0"},
            {"UInt32", "0"},
            {"UInt64", "0"},
            {"Single", "0"},
            {"Double", "0"},
            {"Decimal", "0"},
            {"IntPtr", "0"},
            {"number", "0"},
            {"List", "[]"},
            {"Byte[]", "Uint8List(0)"},
            {"Stream", "Uint8List(0)"},
            {"Uint8List", "Uint8List(0)"},
            {"DateTime", "DateTime(0)"},
            {"DateOnly", "DateTime(0)"},
            {"DateTimeOffset", "DateTime(0)"},
        };
        
        static HashSet<string> BasicJsonTypes = new() {
            nameof(String),
            nameof(Boolean),
            nameof(Guid),
            nameof(Char),
            nameof(Byte),
            nameof(Int16),
            nameof(Int32),
            nameof(Int64),
            nameof(UInt16),
            nameof(UInt32),
            nameof(UInt64),
            nameof(Single),
            nameof(Double),
            nameof(Decimal),
            "int",
            "bool",
            "Dictionary<String,String>",
        };
        
        public static Dictionary<string,string> DartToJsonConverters = new() {
            { "double", "toDouble" },
            { "Map<String,String?>", "toStringMap" },
        };
        
        public static bool GenerateServiceStackTypes => IgnoreTypeInfosFor.Count == 0;

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
        
        public static TypeFilterDelegate TypeFilter { get; set; }

        public static Func<DartGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

        public static Func<List<MetadataType>, List<MetadataType>> FilterTypes = DefaultFilterTypes;

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
        /// Additional Options in Header Options
        /// </summary>
        public List<string> AddQueryParamOptions { get; set; }

        /// <summary>
        /// Emit code without Header Options
        /// </summary>
        public bool WithoutOptions { get; set; }

        public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
        {
            var typeNamespaces = new HashSet<string>();
            var includeList = metadata.RemoveIgnoredTypes(Config);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

            var defaultImports = !Config.DefaultImports.IsEmpty()
                ? Config.DefaultImports
                : DefaultImports;

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
                if (Config.UsePath != null)
                    sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));

                sb.AppendLine();
                sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
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

            var existingTypes = new HashSet<string>();

            var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToSet();
            var types = metadata.Types.CreateSortedTypeList();

            allTypes = metadata.GetAllTypesOrdered();
            allTypes.RemoveAll(x => x.IgnoreType(Config, includeList));

            allTypes = FilterTypes(allTypes);
            
            allTypesMap = new Dictionary<string, MetadataType>();
            foreach (var allType in allTypes)
            {
                allTypesMap[allType.Name] = allType;
            }

            //TypeScript doesn't support reusing same type name with different generic airity
            var conflictPartialNames = allTypes.Map(x => x.Name).Distinct()
                .GroupBy(g => g.LeftPart('`'))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            this.conflictTypeNames = allTypes
                .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
                .Map(x => x.Name);
            
            //Need to add removed built-in Types
            this.conflictTypeNames.Add(typeof(QueryDb<,>).Name);
            this.conflictTypeNames.Add(typeof(QueryData<,>).Name);
            this.conflictTypeNames.Add(typeof(Tuple<>).Name);
            this.conflictTypeNames.Add(typeof(Tuple<,>).Name);
            this.conflictTypeNames.Add(typeof(Tuple<,,>).Name);
            this.conflictTypeNames.Add(typeof(Tuple<,,,>).Name);

            if (!string.IsNullOrEmpty(globalNamespace))
            {
                sb.AppendLine();
                sb.AppendLine($"library {globalNamespace.SafeToken()};");
            }

            if (requestTypes.Any(x => x.Inherits?.Name == "List`1"))
            {
                defaultImports.AddIfNotExists("dart:collection");
            }
            if (allTypes.Any(x => x.Properties?.Any(p => p.Type == "Byte[]") == true)
                || requestTypes.Any(x => x.RequestType?.ReturnType?.Name == "Byte[]")
                || responseTypes.Any(x => x.Name == "Byte[]"))
            {
                defaultImports.AddIfNotExists("dart:typed_data");
            }
            
            defaultImports.Each(x => sb.AppendLine($"import '{x}';"));

            var insertCode = InsertCodeFilter?.Invoke(allTypes, Config);
            if (insertCode != null)
                sb.AppendLine(insertCode);

            existingTypeInfos = new HashSet<string>(IgnoreTypeInfosFor);
            sbTypeInfos = new StringBuilder();
            var dtosName = Config.GlobalNamespace ?? new Uri(Config.BaseUrl).Host;
            sbTypeInfos.AppendLine().AppendLine("TypeContext _ctx = TypeContext(library: '" + dtosName.SafeVarRef() + "', types: <String, TypeInfo> {");

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

            if (existingTypes.Count > 0)
            {
                sbTypeInfos.AppendLine("});");
                sb.AppendLine(sbTypeInfos.ToString());
            }

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
            AppendDataContract(sb, type.DataContract);

            sb.Emit(type, Lang.Dart);
            PreTypeFilter?.Invoke(sb, type);

            if (type.IsEnum.GetValueOrDefault())
            {
                var enumType = Type(type.Name, type.GenericArgs);
                RegisterType(type, enumType);

                var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty();
                if (!isIntEnum)
                {
                    sb.AppendLine($"enum {enumType}");
                    sb.AppendLine("{");
                    sb = sb.Indent();
    
                    foreach (var name in type.EnumNames.Safe())
                    {
                        sb.AppendLine($"{name},");
                    }
                    sb = sb.UnIndent();
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"class {enumType}");
                    sb.AppendLine("{");
                    sb = sb.Indent();

                    if (type.EnumNames != null)
                    {
                        for (var i = 0; i < type.EnumNames.Count; i++)
                        {
                            var name = type.EnumNames[i];
                            var value = type.EnumValues?[i];

                            sb.AppendLine($"static const {enumType} {name} = const {enumType}._({value});");
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine("final int _value;");
                    sb.AppendLine($"const {enumType}._(this._value);");
                    sb.AppendLine($"int get value => _value;");

                    var enumNames = (type.EnumNames ?? TypeConstants.EmptyStringList).Join(",");
                    sb.AppendLine($"static List<{enumType}> get values => const [{enumNames}];");

                    sb = sb.UnIndent();
                    sb.AppendLine("}");
                }
            }
            else
            {
                var extends = new List<string>();

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                    extends.Add(Type(type.Inherits).InDeclarationType());

                string responseTypeExpression = null;
                string responseTypeName = null;

                var interfaces = new List<string>();
                var implStr = options.ImplementsFn?.Invoke();
                if (!string.IsNullOrEmpty(implStr))
                {
                    interfaces.Add(implStr);

                    if (implStr.StartsWith("IReturn<"))
                    {
                        var types = implStr.RightPart('<');
                        var returnType = types.Substring(0, types.Length - 1);

                        if (returnType == "any")
                            returnType = "dynamic";

                        // This is to avoid invalid syntax such as "return new string()"
                        responseTypeExpression = defaultValues.TryGetValue(returnType, out var newReturnInstance)
                            ? $"createResponse() => {newReturnInstance};"
                            : $"createResponse() => {DartLiteral(returnType + "()")};";
                        responseTypeName = $"getResponseTypeName() => \"{returnType}\";";
                        
                        var isGeneric = returnType.IndexOf('<') >= 0;
                        //Don't register non-existent 'T Generic Type
                        var hasGenericBase = type.Inherits != null && type.Inherits?.Name.IndexOf('`') >= 0;  
                        if (isGeneric && hasGenericBase)
                            RegisterType(null, returnType);
                    }
                    else if (implStr == "IReturnVoid")
                    {
                        responseTypeExpression = "createResponse() {}";
                    }
                }

                type.Implements.Each(x => interfaces.Add(Type(x)));

                var isClass = type.IsInterface != true;
                var isAbstractClass = type.IsInterface == true || type.IsAbstract == true;
                var baseClass = extends.Count > 0 ? extends[0] : null;
                var hasDtoBaseClass = baseClass != null;
                var hasListBase = baseClass != null && baseClass.StartsWith("List<");
                if (hasListBase)
                {
                    baseClass = "ListBase" + baseClass.Substring(4);
                    hasDtoBaseClass = false;
                }
                if (!isAbstractClass)
                {
                    interfaces.Add("IConvertible");
                }
                var extend = baseClass != null
                    ? " extends " + baseClass
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

                var typeDeclaration = !isAbstractClass ? "class" : "abstract class";

                var typeName = Type(type.Name, type.GenericArgs);
 
                RegisterType(type, typeName);
                
                sb.AppendLine($"{typeDeclaration} {typeName}{extend}");
                sb.AppendLine("{");

                sb = sb.Indent();
                InnerTypeFilter?.Invoke(sb, type);

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest && !isAbstractClass;
                if (addVersionInfo)
                {
                    sb.AppendLine($"int {GetPropertyName("Version")};");
                }

                if (type.Name == "IReturn`1")
                {
                    sb.AppendLine("T createResponse();");
                    sb.AppendLine("String getTypeName();");
                }
                else if (type.Name == "IReturnVoid")
                {
                    sb.AppendLine("void createResponse();");
                    sb.AppendLine("String getTypeName();");
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                                           && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                if (isClass)
                {
                    var typeNameWithoutGenericArgs = typeName.LeftPart('<');
                    var props = (type.Properties ?? TypeConstants<MetadataPropertyType>.EmptyList).ToList();

                    if (addVersionInfo)
                    {
                        props.Insert(0, new MetadataPropertyType {
                            Name = GetPropertyName("Version"),
                            Type = "Int32",
                            Namespace = "System",
                            IsValueType = true,            
                            Value = Config.AddImplicitVersion.ToString()
                        });
                    }

                    if (props.Count > 0)
                        sb.AppendLine();

                    if (hasListBase)
                    {
                        var genericArg = baseClass.Substring(9, baseClass.Length - 10);
                        sb.AppendLine($"final List<{genericArg}> l = [];");
                        sb.AppendLine("set length(int newLength) { l.length = newLength; }");
                        sb.AppendLine("int get length => l.length;");
                        sb.AppendLine($"{genericArg} operator [](int index) => l[index];");
                        sb.AppendLine($"void operator []=(int index, {genericArg} value) {{ l[index] = value; }}");
                    }

                    var sbBody = StringBuilderCacheAlt.Allocate();
                    if (props.Count > 0)
                    {
                        foreach (var prop in props)
                        {
                            if (sbBody.Length == 0)
                                sbBody.Append(typeNameWithoutGenericArgs + "({");
                            else
                                sbBody.Append(",");
                            sbBody.Append($"this.{GetPropertyName(prop)}");
                            if (!string.IsNullOrEmpty(prop.Value))
                            {
                                sbBody.Append("=" + prop.Value);
                            }
                        }
                        if (sbBody.Length > 0)
                        {
                            sb.AppendLine(StringBuilderCacheAlt.ReturnAndFree(sbBody) + "});");
                        }
                    }
                    else
                    {
                        sb.AppendLine(typeNameWithoutGenericArgs + "();");
                    }

                    if (props.Count > 0)
                    {
                        sbBody = StringBuilderCacheAlt.Allocate();
                        sbBody.Append(typeNameWithoutGenericArgs + ".fromJson(Map<String, dynamic> json)");
                        sbBody.Append(" { fromMap(json); }");
                        sb.AppendLine(StringBuilderCacheAlt.ReturnAndFree(sbBody));
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine(typeNameWithoutGenericArgs + ".fromJson(Map<String, dynamic> json) : " + 
                                      (hasDtoBaseClass ? "super.fromJson(json);" : "super();"));
                    }

                    sbBody = StringBuilderCacheAlt.Allocate();
                    sbBody.AppendLine("fromMap(Map<String, dynamic> json) {");
                    if (hasDtoBaseClass)
                        sbBody.AppendLine("        super.fromMap(json);");
                    foreach (var prop in props)
                    {
                        var propType = GetPropertyType(prop);
                        var jsonName = prop.Name.PropertyStyle();
                        var propName = GetPropertyName(prop);
                        if (UseTypeConversion(prop))
                        {
                            bool registerType = true;
                            if (type.GenericArgs?.Length > 0 && prop.GenericArgs?.Length > 0)
                            {
                                var argIndexes = new List<int>();
                                foreach (var arg in prop.GenericArgs)
                                {
                                    var argIndex = Array.IndexOf(type.GenericArgs, arg);
                                    argIndexes.Add(argIndex);
                                }
                                if (argIndexes.All(x => x != -1))
                                {
                                    propType = prop.Type.LeftPart('`') + "<${runtimeGenericTypeDefs(this,[" + argIndexes.Join(",") +"]).join(\",\")}>";
                                    registerType = false;
                                }
                            }

                            if (registerType)
                            {
                                RegisterPropertyType(prop, propType);
                            }
                            
                            sbBody.AppendLine($"        {propName} = JsonConverters.fromJson(json['{jsonName}'],'{propType}',context!);");
                        }
                        else
                        {
                            if (DartToJsonConverters.TryGetValue(propType, out var conversionFn))
                            {
                                sbBody.AppendLine($"        {propName} = JsonConverters.{conversionFn}(json['{jsonName}']);");
                            }
                            else
                            {
                                sbBody.AppendLine($"        {propName} = json['{jsonName}'];");
                            }
                        }
                    }
                    sbBody.AppendLine("        return this;");
                    sbBody.AppendLine("    }");
                    sb.AppendLine(StringBuilderCacheAlt.ReturnAndFree(sbBody));
                    
                    sbBody = StringBuilderCacheAlt.Allocate();
                    if (props.Count > 0)
                    {
                        foreach (var prop in props)
                        {
                            if (sbBody.Length == 0)
                            {
                                sbBody.Append("Map<String, dynamic> toJson() => ");
                                if (hasDtoBaseClass)
                                    sbBody.Append("super.toJson()..addAll(");
    
                                sbBody.AppendLine("{");
                            }
                            else
                            {
                                sbBody.AppendLine(",");
                            }
    
                            var propType = GetPropertyType(prop);
                            var propName = GetPropertyName(prop);
                            var jsonName = propName.PropertyStyle();
                            if (UseTypeConversion(prop))
                            {
                                sbBody.Append($"        '{jsonName}': JsonConverters.toJson({propName},'{propType}',context!)");
                            }
                            else
                            {
                                sbBody.Append($"        '{jsonName}': {propName}");
                            }
                        }
                        if (sbBody.Length > 0)
                        {
                            sb.AppendLine(StringBuilderCacheAlt.ReturnAndFree(sbBody));
                            sb.AppendLine(hasDtoBaseClass ? "});" : "};");
                            sb.AppendLine();
                        }
                    }
                    else
                    {
                        sb.AppendLine("Map<String, dynamic> toJson() => " + 
                            (hasDtoBaseClass ? "super.toJson();" : "{};"));
                    }
                    
                    if (responseTypeExpression != null)
                    {
                        sb.AppendLine(responseTypeExpression);
                        if (responseTypeName != null)
                            sb.AppendLine(responseTypeName);
                    }
                    if ((type.GenericArgs?.Length ?? 0) == 0)
                    {
                        sb.AppendLine($"getTypeName() => \"{typeName}\";");
                    }
                    else
                    {
                        // Return the reified generic type name instead of the non-existent generic template arg type
                        var sbGeneric = StringBuilderCacheAlt.Allocate();
                        foreach (var arg in type.GenericArgs)
                        {
                            if (sbGeneric.Length > 0)
                                sbGeneric.Append(',');
                            sbGeneric.Append("$").Append(arg.TrimStart('\''));
                        }
                        var genericTypeName = type.Name.LeftPart('`') + '<' + StringBuilderCacheAlt.ReturnAndFree(sbGeneric) + '>';
                        sb.AppendLine($"getTypeName() => \"{genericTypeName}\";");
                    }

                    if (isClass)
                    {
                        sb.AppendLine("TypeContext? context = _ctx;");
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }

            PostTypeFilter?.Invoke(sb, type);

            return lastNS;
        }

        public void RegisterPropertyType(MetadataPropertyType prop, string dartType)
        {
            if (existingTypeInfos.Contains(dartType))
                return;

            var csharpType = CSharpPropertyType(prop);
            var factoryFn = defaultValues.TryGetValue(csharpType, out string defaultValue)
                ? $"() => {defaultValue}"
                : null;

            allTypesMap.TryGetValue(csharpType, out var metaType);
            RegisterType(metaType, dartType, factoryFn);
        }

        public string DartLiteral(string typeName)
        {
            // List<T>() is deprecated for literal: <T>[]
            if (typeName.StartsWith("List<"))
            {
                var listLiteral = typeName.Substring("List".Length);
                return listLiteral.Substring(0,listLiteral.Length - 2) + "[]";
            }
            return typeName;
        }

        private void RegisterType(MetadataType metaType, string dartType, string factoryFn = null)
        {
            dartType = dartType.TrimEnd('?');
            if (existingTypeInfos.Contains(dartType))
                return;
            existingTypeInfos.Add(dartType);

            if (factoryFn == null)
                factoryFn = $"() => {dartType}()";

            if (factoryFn.StartsWith("() => List<"))
            {
                var listLiteral = DartLiteral(factoryFn.Substring("() => ".Length));
                factoryFn = "() => " + listLiteral;
            }
            
            if (metaType == null)
            {
                sbTypeInfos.AppendLine($"    '{dartType}': TypeInfo(TypeOf.Class, create:{factoryFn}),");

                var hasGenericArgs = dartType.IndexOf("<", StringComparison.Ordinal) >= 0;
                if (hasGenericArgs)
                {
                    var nodes = dartType.ParseTypeIntoNodes();
                    foreach (var genericArgNode in nodes.Children.Safe())
                    {
                        if (BasicJsonTypes.Contains(genericArgNode.Text))
                            continue;

                        var genericArg = RawType(genericArgNode);

                        var genericArgFactoryFn = defaultValues.TryGetValue(genericArg, out string defaultValue)
                            ? $"() => {defaultValue}"
                            : null;
                        
                        RegisterType(null, genericArg, genericArgFactoryFn);
                    }
                }
                return;
            }

            var isClass = metaType.IsAbstract != true && metaType.IsInterface != true && metaType.IsEnum != true;
            var isGenericTypeDef = isClass && metaType.GenericArgs?.Length > 0 && metaType.GenericArgs.Any(x => x.StartsWith("'"));

            if (isGenericTypeDef)
            {
                var dartGenericBaseType = dartType.LeftPart("<");
                sbTypeInfos.AppendLine($"    '{dartType}': TypeInfo(TypeOf.GenericDef,create:() => {dartGenericBaseType}()),");
            }
            else if (metaType?.IsInterface == true)
            {
                sbTypeInfos.AppendLine($"    '{dartType}': TypeInfo(TypeOf.Interface),");
            }
            else if (metaType?.IsAbstract == true)
            {
                sbTypeInfos.AppendLine($"    '{dartType}': TypeInfo(TypeOf.AbstractClass),");
            }
            else if (metaType?.IsEnum == true)
            {
                sbTypeInfos.AppendLine($"    '{dartType}': TypeInfo(TypeOf.Enum, enumValues:{dartType}.values),");
            }
            else
            {
                sbTypeInfos.AppendLine($"    '{dartType}': TypeInfo(TypeOf.Class, create:{factoryFn}),");
            }

            //base classes need to be abstract and can't be instantiated in TypeContext mappings
            if (metaType.Inherits != null)
            {
                //AutoQuery Base types are existing but still need to register the List<Result> type
                var baseType = metaType.Inherits;
                if ((baseType.Name.StartsWith("QueryDb`") || baseType.Name.StartsWith("QueryData`"))
                    && metaType.IsAbstract != true) //Don't register non-existent 'T Generic Type
                {
                    var listArgType = baseType.GenericArgs.Last();
                    var listType = new MetadataType
                    {
                        Name = $"List<{listArgType}>",
                        Namespace = typeof(List<>).Namespace,
                        GenericArgs = new[] { listArgType },
                    };
                    RegisterType(listType, listType.Name);
                }
            }
        }

        public bool UseTypeConversion(MetadataPropertyType prop)
        {
            var typeName = prop.Type;
            if (prop.GenericArgs != null)
            {
                if (prop.Type == "Nullable`1")
                    typeName = GenericArg(prop.GenericArgs[0]);
            }
            var rawType = RawGenericType(prop.Type, prop.GenericArgs);
            return !BasicJsonTypes.Contains(typeName) && !typeName.Contains("'") && !BasicJsonTypes.Contains(rawType);
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

                    var propType = GetPropertyType(prop);
                    propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                    sb.Emit(prop, Lang.Dart);
                    PrePropertyFilter?.Invoke(sb, prop, type);
                    sb.AppendLine($"{propType}? {GetPropertyName(prop)};");
                    PostPropertyFilter?.Invoke(sb, prop, type);
                }
            }

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine($"ResponseStatus? {GetPropertyName(nameof(ResponseStatus))};");
            }
        }

        public virtual string GetPropertyType(MetadataPropertyType prop)
        {
            var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
            if (propType.EndsWith("?"))
                propType = propType.Substring(0, propType.Length - 1);
            return propType;
        }

        private string CSharpPropertyType(MetadataPropertyType prop)
        {
            var propType = RawGenericType(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
            if (propType.EndsWith("?"))
                propType = propType.Substring(0, propType.Length - 1);
            return propType;
        }

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            foreach (var attr in attributes)
            {
                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine($"// @{GetPropertyName(attr.Name)}()");
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
                    sb.AppendLine($"// @{attr.Name}({StringBuilderCacheAlt.ReturnAndFree(args)})");
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

        public static HashSet<string> ArrayTypes = new() {
            "List`1",
            "IList`1",
            "IEnumerable`1",
            "ICollection`1",
            "HashSet`1",
            "Queue`1",
            "ConcurrentQueue`1",
            "Stack`1",
            "ConcurrentStack`1",
            "IEnumerable",
            "ArrayList",
        };

        public static HashSet<string> DictionaryTypes = new() {
            "Dictionary`2",
            "IDictionary`2",
            "IOrderedDictionary`2",
            "ConcurrentDictionary`2",
            "OrderedDictionary",
            "SortedList`2",
            "SortedList",
            "StringDictionary",
            "IDictionary",
            "IOrderedDictionary",
            "Hashtable",
        };

        public static HashSet<string> SetTypes = new() {
            "HashSet`1",
        };

        public string Type(string type, string[] genericArgs)
        {
            var useType = TypeFilter?.Invoke(type, genericArgs);
            if (useType != null)
                return useType;

            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return GenericArg(genericArgs[0]);
                if (ArrayTypes.Contains(type))
                    return $"List<{GenericArg(genericArgs[0])}>".StripNullable();
                if (DictionaryTypes.Contains(type))
                    return $"Map<{GenericArg(genericArgs[0])},{GenericArg(genericArgs[1])}?>";
                if (SetTypes.Contains(type))
                    return $"Set<{GenericArg(genericArgs[0])}>".StripNullable();

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = StringBuilderCacheAlt.Allocate();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(",");

                        args.Append(GenericArg(arg));
                    }

                    var typeName = TypeAlias(type);
                    return $"{typeName}<{StringBuilderCacheAlt.ReturnAndFree(args)}>";
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
            if (type == "Byte[]")
                return TypeAliases["Byte[]"];

            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return $"List<{TypeAlias(arrParts[0])}>";

            TypeAliases.TryGetValue(type, out var typeAlias);

            return typeAlias ?? NameOnly(type);
        }

        public string RawGenericType(string type, string[] genericArgs)
        {
            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return RawGenericArg(genericArgs[0]);

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = StringBuilderCacheAlt.Allocate();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(",");

                        args.Append(RawGenericArg(arg));
                    }

                    var typeName = NameOnly(type);
                    return $"{typeName}<{StringBuilderCacheAlt.ReturnAndFree(args)}>";
                }
            }
            else
            {
                return type.StripNullable();
            }

            return type;
        }

        public string NameOnly(string type)
        {
            var name = conflictTypeNames.Contains(type)
                ? type.Replace("`","")
                : type.LeftPart('`');

            return name.LastRightPart('.').SafeToken();
        }

        public bool AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc != null && Config.AddDescriptionAsComments)
            {
                sb.AppendLine("/**");
                sb.AppendLine("* " + desc.SafeComment());
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
                    dcArgs = $"Name={dcMeta.Name.QuotedSafeValue()}";

                if (dcMeta.Namespace != null)
                {
                    if (dcArgs.Length > 0)
                        dcArgs += ", ";

                    dcArgs += $"Namespace={dcMeta.Namespace.QuotedSafeValue()}";
                }

                dcArgs = $"({dcArgs})";
            }
            sb.AppendLine($"// @DataContract{dcArgs}");
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                        ? $"// @DataMember(Order={dataMemberIndex})"
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
            sb.AppendLine($"// @DataMember{dmArgs}");

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
            
            if (node.Text == "Dictionary")
                node.Text = "Map";
            else if (node.Text == "HashSet")
                node.Text = "Set";
            if (conflictTypeNames.Contains(node.Text + "`" + node.Children.Count))
                node.Text += node.Children.Count;

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

            return sb.ToString();
        }

        public string RawGenericArg(string arg)
        {
            return RawType(arg.TrimStart('\'').ParseTypeIntoNodes());
        }

        public string RawType(TextNode node)
        {
            var sb = new StringBuilder();

            sb.Append(NameOnly(node.Text));
            if (node.Children.Count > 0)
            {
                sb.Append("<");
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var childNode = node.Children[i];

                    if (i > 0)
                        sb.Append(",");

                    sb.Append(RawType(childNode));
                }
                sb.Append(">");
            }

            return sb.ToString();
        }

        public string GetPropertyName(string name) => name.SafeToken().PropertyStyle(); 
        public string GetPropertyName(MetadataPropertyType prop) => 
            prop.GetSerializedAlias() ?? prop.Name.SafeToken().PropertyStyle();
    }
    
    public static class DartGeneratorExtensions
    {
        public static HashSet<string> DartKeyWords = new() {
            "context", // IConvertible code-gen property
            "abstract",
            "deferred",
            "if",
            "super",
            "as",
            "do",
            "implements",
            "switch",
            "assert",
            "dynamic",
            "import",
            "sync",
            "async",
            "else",
            "in",
            "this",
            "async",
            "enum",
            "is",
            "throw",
            "await",
            "export",
            "library",
            "true",
            "break",
            "external",
            "new",
            "try",
            "case",
            "extends",
            "null",
            "typedef",
            "catch",
            "factory",
            "operator",
            "var",
            "class",
            "false",
            "part",
            "void",
            "const",
            "final",
            "rethrow",
            "while",
            "continue",
            "finally",
            "return",
            "with",
            "covariant",
            "for",
            "set",
            "yield",
            "default",
            "get",
            "static",
            "yield",
            "int",
            "double",
            "bool",
        };

        public static bool IsKeyWord(string name) => DartKeyWords.Contains(name);

        public static string PropertyName(this string name) => IsKeyWord(name) 
            ? char.ToUpper(name[0]) + name.Substring(1) 
            : name;

        public static string InDeclarationType(this string type)
        {
            //TypeScript doesn't support short-hand Dictionary notation or has a Generic Dictionary Type
            if (type.StartsWith("{"))
                return "any";

            //TypeScript doesn't support short-hand T[] notation in extension list
            var arrParts = type.SplitOnFirst('[');
            return arrParts.Length > 1 
                ? $"List<{arrParts[0]}>"
                : type;
        }

        public static string PropertyStyle(this string name)
        {
            var formattedName = JsConfig.TextCase == TextCase.CamelCase
                ? name.ToCamelCase()
                : JsConfig.TextCase == TextCase.SnakeCase
                    ? name.ToLowercaseUnderscore()
                    : name;

            return formattedName;
        }

        public static bool HasEnumFlags(MetadataType type)
        {
            return false;
        }
    }

}