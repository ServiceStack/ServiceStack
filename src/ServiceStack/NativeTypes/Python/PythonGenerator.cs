using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.Python
{
    public class PythonGenerator
    {
        public readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        List<string> conflictTypeNames = new();
        public List<MetadataType> AllTypes { get; set; }

        public PythonGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }
        
        public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
        
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

        public static HashSet<string> IgnoreAttributes { get; private set; } = new() {
            nameof(DataContractAttribute),
            nameof(DataMemberAttribute),
        };
        public static bool IgnoreAllAttributes
        {
            get => IgnoreAttributes == null;
            set => IgnoreAttributes = null;
        }
        
        public static List<string> DefaultImports = new() {
            "typing:TypeVar/Generic/Optional/Dict/List/Tuple",
            "dataclasses:dataclass/field",
            "dataclasses_json:dataclass_json/LetterCase",
            "enum:Enum",
            "datetime:datetime/timedelta",
        };

        public static Dictionary<string, string> TypeAliases = new() {
            {"String", "str"},
            {"Boolean", "bool"},
            {"DateTime", "datetime"},
            {"DateTimeOffset", "datetime"},
            {"TimeSpan", "timedelta"},
            {"Guid", "str"},
            {"Char", "str"},
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
            {"IntPtr", "number"},
            {"List", "list"},
            {"Byte[]", "bytes"},
            {"Stream", "bytes"},
            {"HttpWebResponse", "bytes"},
            {"IDictionary", "Dict"},
            {"OrderedDictionary", "Dict"},
            {"Uri", "str"},
            {"Type", "str"},
        };

        public static Dictionary<string, string> ReturnTypeAliases = new() {
        };

        public static HashSet<string> KeyWords = new() {
            "and",
            "as",
            "assert",
            "break",
            "class",
            "continue",
            "def",
            "del",
            "elif",
            "else",
            "except",
            "False",
            "finally",
            "for",
            "from",
            "global",
            "if",
            "import",
            "in",
            "is",
            "lambda",
            "None",
            "nonlocal",
            "not",
            "or",
            "pass",
            "raise",
            "return",
            "True",
            "try",
            "while",
            "with",
            "yield",
        };
        
        private static string declaredEmptyString = "''";
        private static readonly Dictionary<string, string> primitiveDefaultValues = new() {
            {"String", declaredEmptyString},
            {"string", declaredEmptyString},
            {"Boolean", "False"},
            {"boolean", "False"},
            {"DateTime", "datetime(1,1,1)"},
            {"DateTimeOffset", "datetime(1,1,1)"},
            {"TimeSpan", "timedelta()"},
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
            {"List", "field(default_factory=lambda:([]))"},
            {"Dictionary", "field(default_factory=lambda:({}))"},
        };
        
        public static TypeFilterDelegate TypeFilter { get; set; }
        public static Func<string, string> CookedTypeFilter { get; set; }
        public static TypeFilterDelegate DeclarationTypeFilter { get; set; }
        public static Func<string, string> CookedDeclarationTypeFilter { get; set; }
        public static Func<string, string> ReturnMarkerFilter { get; set; }

        public static Func<List<MetadataType>, List<MetadataType>> FilterTypes { get; set; } = DefaultFilterTypes;

        public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types.OrderTypesByDeps();

        public static TextCase TextCase { get; set; } = TextCase.SnakeCase;
        
        /// <summary>
        /// Add Code to top of generated code
        /// </summary>
        public static AddCodeDelegate InsertCodeFilter { get; set; }

        /// <summary>
        /// Add Code to bottom of generated code
        /// </summary>
        public static AddCodeDelegate AddCodeFilter { get; set; }

        public HashSet<string> AddedDeclarations { get; set; } = new HashSet<string>();

        public static Func<PythonGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

        /// <summary>
        /// Whether property should be marked optional
        /// </summary>
        public static Func<PythonGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
            DefaultIsPropertyOptional;

        /// <summary>
        /// Helper to make Nullable properties
        /// </summary>
        public static bool UseOptionalProperties
        {
            set
            {
                if (value)
                {
                    IsPropertyOptional = (gen, type, prop) => false;
                    PropertyTypeFilter = (gen, type, prop) => 
                        prop.IsRequired == true
                            ? gen.GetPropertyType(prop, out _)
                            : gen.GetPropertyType(prop, out _) + "|None";
                }
            }
        }

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

            string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "#" : "";

            var sbInner = StringBuilderCache.Allocate();
            var sb = new StringBuilderWrapper(sbInner);
            sb.AppendLine("\"\"\" Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            if (Config.UsePath != null)
                sb.AppendLine("#UsePath: {0}".Fmt(Config.UsePath));

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

            sb.AppendLine("\"\"\"");
            sb.AppendLine();

            string lastNS = null;

            var existingTypes = new HashSet<string>();

            var requestTypes = metadata.Operations.Select(x => x.Request).ToSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToSet();
            var types = metadata.Types.CreateSortedTypeList();

            AllTypes = metadata.GetAllTypesOrdered();
            AllTypes.RemoveAll(x => x.IgnoreType(Config, includeList));
            AllTypes = FilterTypes(AllTypes);

            //Python doesn't support reusing same type name with different generic airity
            var conflictPartialNames = AllTypes.Map(x => x.Name).Distinct()
                .GroupBy(g => g.LeftPart('`'))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            this.conflictTypeNames = AllTypes
                .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
                .Map(x => x.Name);

            foreach (var import in defaultImports)
            {
                var pos = import.IndexOf(':');
                sb.AppendLine(pos == -1
                    ? $"import {import}"
                    : $"from {import.Substring(0, pos)} import {import.Substring(pos + 1).StripQuotes().Replace("/",", ")}");
            }

            if (!string.IsNullOrEmpty(globalNamespace))
            {
                sb.AppendLine();
                sb.AppendLine($"# module {globalNamespace.SafeToken()}");
                // sb = sb.Indent();
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

            if (!string.IsNullOrEmpty(globalNamespace))
            {
                // sb = sb.UnIndent();
                sb.AppendLine();
            }
            
            sb.AppendLine();

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

            sb.Emit(type, Lang.Python);
            PreTypeFilter?.Invoke(sb, type);

            if (type.IsEnum.GetValueOrDefault())
            {
                var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumValues != null; 
                var enumDec = isIntEnum
                    ? "Enum"
                    : "str,Enum";
                    
                sb.AppendLine($"class {Type(type.Name, type.GenericArgs)}({enumDec}):");
                sb = sb.Indent();
                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i].PropertyStyle().ToUpper();
                        var value = type.EnumValues?[i];

                        var memberValue = type.GetEnumMemberValue(i);
                        if (memberValue != null)
                        {
                            sb.AppendLine($"{name} = '{memberValue}'");
                            continue;
                        }

                        sb.AppendLine(value == null 
                            ? $"{name} = '{name}'"
                            : $"{name} = {value}");
                    }
                }
                else
                {
                    sb.AppendLine("pass");
                }
                sb = sb.UnIndent();
            }
            else
            {
                var extends = new List<string>();

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                {
                    extends.Add(DeclarationType(type.Inherits.Name, type.Inherits.GenericArgs, out var addDeclaration));

                    if (addDeclaration != null && !AddedDeclarations.Contains(addDeclaration))
                    {
                        AddedDeclarations.Add(addDeclaration);
                        sb.AppendLine(addDeclaration);
                        sb.AppendLine();
                    }
                }

                string responseTypeExpression = null;

                var interfaces = new List<string>();
                var implStr = options.ImplementsFn?.Invoke();
                if (!string.IsNullOrEmpty(implStr))
                {
                    interfaces.Add(implStr);

                    if (implStr.StartsWith("IReturn<"))
                    {
                        var types = implStr.RightPart('<');
                        var returnType = types.Substring(0, types.Length - 1);

                        // This is to avoid invalid syntax such as "return new string()"
                        primitiveDefaultValues.TryGetValue(returnType, out var replaceReturnType);
                            
                        responseTypeExpression = replaceReturnType == null 
                            ? $"def createResponse(): return new {returnType}()"
                            : $"def createResponse(): return {replaceReturnType}";
                    }
                    else if (implStr == "IReturnVoid")
                    {
                        responseTypeExpression = "def createResponse(): return None";
                    }
                }

                type.Implements.Each(x => interfaces.Add(Type(x)));

                var modifier = "";
                var extend = extends.Count > 0
                    ? extends[0]
                    : "";

                if (!string.IsNullOrEmpty(extend))
                {
                    extend = ConvertSelfReferences(type.Name, extend);
                }

                if (interfaces.Count > 0)
                {
                    for (int i = 0; i < interfaces.Count; i++)
                    {
                        interfaces[i] = ConvertSelfReferences(type.Name, interfaces[i]);
                    }
                    
                    if (!string.IsNullOrEmpty(extend)) 
                        extend += ",";
                    extend += string.Join(",", interfaces.ToArray());
                }

                var typeName = Type(type.Name, type.GenericArgs);
                var className = ClassType(typeName, extend, out var typeArgs);
                foreach (var genericArg in typeArgs)
                {
                    sb.AppendLine($"{genericArg} = TypeVar('{genericArg}')");
                }
                if (!type.IsInterface.GetValueOrDefault())
                {
                    sb.AppendLine("@dataclass_json(letter_case=LetterCase.CAMEL)");
                    sb.AppendLine("@dataclass");
                }
                sb.AppendLine($"class {className}:");

                sb = sb.Indent();
                InnerTypeFilter?.Invoke(sb, type);

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
                if (addVersionInfo)
                {
                    sb.AppendLine($"{modifier}{GetPropertyName("Version")}: int = {Config.AddImplicitVersion}");
                }

                if (type.Name == "IReturn`1")
                {
                    sb.AppendLine("def createResponse(): pass");
                }
                else if (type.Name == "IReturnVoid")
                {
                    sb.AppendLine("def createResponse(): pass");
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                if (responseTypeExpression != null)
                {
                    sb.AppendLine(responseTypeExpression);
                    sb.AppendLine($"def getTypeName(): return '{type.Name}'");
                }
                else if (type.Properties.IsEmpty() && !addVersionInfo && type.Name != "IReturn`1" && type.Name != "IReturnVoid")
                {
                    sb.AppendLine("pass");
                }

                sb = sb.UnIndent();
            }

            PostTypeFilter?.Invoke(sb, type);
            
            return lastNS;
        }

        private static string ConvertSelfReferences(string typeName, string cls)
        {
            // Workaround to avoid self-referencing class by referencing the string name instead.
            // E.g. class C : IReturn<C> -> class C(IReturn["C"])
            if (cls.IndexOf('[') >= 0)
            {
                var arg = cls.RightPart('[').LeftPart(']');
                if (arg == typeName)
                {
                    return cls.LeftPart('[') + $"[\"{arg}\"" + cls.RightPart('[').Substring(arg.Length);
                }
            }
            return cls;
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
            var isClass = Config.ExportAsTypes && !type.IsInterface.GetValueOrDefault();
            var modifier = "";

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = GetPropertyType(prop, out var optionalProperty);
                    propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                    sb.Emit(prop, Lang.Python);
                    PrePropertyFilter?.Invoke(sb, prop, type);

                    var defaultValue = " = None";
                    if (IsPropertyOptional(this, type, prop) ?? optionalProperty)
                    {
                        propType = $"Optional[{propType}]";
                    }

                    sb.AppendLine($"{GetPropertyName(prop.Name)}: {propType}{defaultValue}");
                    PostPropertyFilter?.Invoke(sb, prop, type);
                }
            }

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine($"{modifier}{GetPropertyName(nameof(ResponseStatus))}: ResponseStatus = None");
            }
        }

        public static bool? DefaultIsPropertyOptional(PythonGenerator generator, MetadataType type, MetadataPropertyType prop)
        {
            if (prop.IsRequired == true)
                return false;
            
            if (generator.Config.MakePropertiesOptional)
                return true;

            return null;
        }

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0 || IgnoreAllAttributes) 
                return false;

            foreach (var attr in attributes)
            {
                if (IgnoreAttributes.Contains(attr.Name) || IgnoreAttributes.Contains(attr.Name + "Attribute"))
                    continue;
                
                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine($"# @{attr.Name}()");
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
                    sb.AppendLine($"# @{attr.Name}({StringBuilderCacheAlt.ReturnAndFree(args)})");
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
            "str",
            "bool",
            "int",
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
                    cooked = $"List[{GenericArg(genericArgs[0])}]".StripNullable();
                }
                if (DictionaryTypes.Contains(type))
                {
                    cooked = $"Dict[{GenericArg(genericArgs[0])},{GenericArg(genericArgs[1])}]";
                }
            }
            
            if (cooked == null)
                cooked = Type(type, genericArgs);
            
            useType = CookedDeclarationTypeFilter?.Invoke(cooked);
            if (useType != null)
                return useType;

            return cooked;
        }

        public string ClassType(string typeName, string extend, out string[] genericArgs)
        {
            genericArgs = TypeConstants.EmptyStringArray;
            if (typeName.IndexOf('[') >= 0)
            {
                var argsDef = typeName.RightPart('[');
                var classDef = typeName.LeftPart('[') + "(Generic[" + argsDef + (!string.IsNullOrEmpty(extend) ? "," : "") + extend + ")";
                genericArgs = argsDef.LastLeftPart(']').Split(',').Select(x => x.Trim()).ToArray();
                return classDef;
            }
            return typeName + (!string.IsNullOrEmpty(extend) ? "(" + extend + ")" : "");
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
                    cooked = $"Optional[{GenericArg(genericArgs[0])}]";
                else if (ArrayTypes.Contains(type))
                    cooked = $"List[{GenericArg(genericArgs[0])}]".StripNullable();
                else if (DictionaryTypes.Contains(type))
                {
                    cooked = $"Dict[{GetKeyType(GenericArg(genericArgs[0]))},{GenericArg(genericArgs[1])}]";
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

                        var typeName = TypeAlias(type);
                        cooked = $"{typeName}[{StringBuilderCacheAlt.ReturnAndFree(args)}]";
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
                return $"List[{TypeAlias(arrParts[0])}]";

            TypeAliases.TryGetValue(type, out var typeAlias);

            var cooked = typeAlias ?? NameOnly(type);
            return CookedTypeFilter?.Invoke(cooked) ?? cooked;
        }

        public string NameOnly(string type)
        {
            var name = conflictTypeNames.Contains(type)
                ? type.Replace('`','_')
                : type.LeftPart('`');

            return name.LastRightPart('.').SafeToken();
        }

        public bool AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc != null && Config.AddDescriptionAsComments)
            {
                sb.AppendLine("#");
                sb.AppendLine($"# {desc.SafeComment()}");
                sb.AppendLine("#");
            }
            return false;
        }

        public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
        {
            if (IgnoreAllAttributes || IgnoreAttributes.Contains("DataContract") || IgnoreAttributes.Contains("DataContractAttribute"))
                return;
                
            if (dcMeta == null)
            {
                if (Config.AddDataContractAttributes)
                    sb.AppendLine("# @DataContract()");
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
            sb.AppendLine($"# @DataContract{dcArgs}");
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (IgnoreAllAttributes || IgnoreAttributes.Contains("DataMember") || IgnoreAttributes.Contains("DataMemberAttribute"))
                return false;
                
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                                  ? $"# @DataMember(Order={dataMemberIndex})"
                                  : "# @DataMember()");
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
            sb.AppendLine($"# @DataMember{dmArgs}");

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
                sb.Append("List").Append('[').Append(ConvertFromCSharp(node.Children[0])).Append(']');
            }
            else if (node.Text == "List`1")
            {
                var type = node.Children.Count > 0 ? node.Children[0].Text : "any";
                sb.Append("List").Append('[').Append(type).Append(']');
            }
            else if (node.Text == "Dictionary")
            {
                sb.Append("Dict[");
                var keyType = ConvertFromCSharp(node.Children[0]);
                sb.Append(GetKeyType(keyType));
                sb.Append(",");
                sb.Append(ConvertFromCSharp(node.Children[1]));
                sb.Append("]");
            }
            else
            {
                sb.Append(TypeAlias(node.Text));
                if (node.Children.Count > 0)
                {
                    sb.Append("[");
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        var childNode = node.Children[i];

                        if (i > 0)
                            sb.Append(",");

                        sb.Append(ConvertFromCSharp(childNode));
                    }
                    sb.Append("]");
                }
            }

            return sb.ToString();
        }

        private static string GetKeyType(string keyType)
        {
            var jsKeyType = AllowedKeyTypes.Contains(keyType)
                ? keyType
                : "str";
            return jsKeyType;
        }

        public string GetPropertyName(string name) => name.SafeToken().PropertyStyle();
    }

    public static class PythonGeneratorExtensions
    {
        public static string PropertyStyle(this string name)
        {
            var toName = PythonGenerator.TextCase == TextCase.CamelCase
                ? name.ToCamelCase()
                : PythonGenerator.TextCase == TextCase.SnakeCase
                    ? name.ToLowercaseUnderscore()
                    : name;

            if (PythonGenerator.KeyWords.Contains(toName))
                toName += "_";
            return toName;
        }
    }
}
