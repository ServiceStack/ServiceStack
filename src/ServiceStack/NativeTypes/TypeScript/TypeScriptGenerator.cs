using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.TypeScript
{
    public class TypeScriptGenerator : ILangGenerator
    {
        public readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        List<string> conflictTypeNames = new();
        public List<MetadataType> AllTypes { get; set; }

        public TypeScriptGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }
        
        public static bool UseUnionTypeEnums { get; set; }

        public static bool EmitPartialConstructors { get; set; } = true;

        public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
        
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }
        
        public static List<string> DefaultImports = new() {
        };

        public static Dictionary<string, string> TypeAliases = new() {
            {"String", "string"},
            {"Boolean", "boolean"},
            {"DateTime", "string"},
            {"DateOnly", "string"},
            {"DateTimeOffset", "string"},
            {"TimeSpan", "string"},
            {"TimeOnly", "string"},
            {"Guid", "string"},
            {"Char", "string"},
            {"Byte", "number"},
            {"Int16", "number"},
            {"Int32", "number"},
            {"Int64", "number"},
            {"UInt16", "number"},
            {"UInt32", "number"},
            {"UInt64", "number"},
            {"Single", "number"},
            {"Double", "number"},
            {"Decimal", "number"},
            {"IntPtr", "number"},
            {"List", "Array"},
            {"Byte[]", "string"},
            {"Stream", "string"},
            {"HttpWebResponse", "Blob"},
            {"IDictionary", "any"},
            {"OrderedDictionary", "any"},
            {"Uri", "string"},
            {"Type", "string"},
        };

        public static Dictionary<string, string> ReturnTypeAliases = new() {
            {"Byte[]", "Blob"},
            {"Stream", "Blob"},
            {"HttpWebResponse", "Blob"},
        };
        
        private static string declaredEmptyString = "''";
        internal static readonly Dictionary<string, string> primitiveDefaultValues = new() {
            {"String", declaredEmptyString},
            {"string", declaredEmptyString},
            {"Boolean", "false"},
            {"boolean", "false"},
            {"DateTime", declaredEmptyString},
            {"DateTimeOffset", declaredEmptyString},
            {"TimeSpan", declaredEmptyString},
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
            {"Uint8Array", "new Uint8Array(0)"},
        };
        
        public static TypeFilterDelegate TypeFilter { get; set; }
        public static Func<string, string> CookedTypeFilter { get; set; }
        public static TypeFilterDelegate DeclarationTypeFilter { get; set; }
        public static Func<string, string> CookedDeclarationTypeFilter { get; set; }
        public static Func<string, string> ReturnMarkerFilter { get; set; }

        public static Func<List<MetadataType>, List<MetadataType>> FilterTypes { get; set; } = DefaultFilterTypes;

        public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types.OrderTypesByDeps();
        
        public static bool InsertTsNoCheck { get; set; }
        
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

        public string DictionaryDeclaration { get; set; } = "export class Dictionary<T> { [Key: string]: T; }";
        
        public HashSet<string> AddedDeclarations { get; set; } = new HashSet<string>();

        public static Func<TypeScriptGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

        /// <summary>
        /// Whether property should be marked optional
        /// </summary>
        public static Func<TypeScriptGenerator, MetadataType, MetadataPropertyType, bool?> IsPropertyOptional { get; set; } =
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

            //TypeScript doesn't support reusing same type name with different generic airity
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
                    ? $"import {import};"
                    : $"import {{ {import.Substring(0, pos)} }} from \"{import.Substring(pos + 1).StripQuotes()}\";");
            }

            if (!string.IsNullOrEmpty(globalNamespace))
            {
                var moduleDef = Config.ExportAsTypes ? "export " : "declare ";
                sb.AppendLine();
                sb.AppendLine("{0}module {1}".Fmt(moduleDef, globalNamespace.SafeToken()));
                sb.AppendLine("{");

                sb = sb.Indent();
            }

            var insertCode = InsertCodeFilter?.Invoke(AllTypes, Config);
            if (insertCode != null)
                sb.AppendLine(insertCode);
            
            if (InsertTsNoCheck)
                sb.AppendLine("// @ts-nocheck");

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
                sb = sb.UnIndent();
                sb.AppendLine();
                sb.AppendLine("}");
            }
            
            sb.AppendLine(); //tslint

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

            sb.Emit(type, Lang.TypeScript);
            PreTypeFilter?.Invoke(sb, type);

            if (type.IsEnum.GetValueOrDefault())
            {
                var isIntEnum = type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty(); 
                if ((isIntEnum || !UseUnionTypeEnums) && Config.ExportAsTypes)
                {
                    var typeDeclaration = !Config.ExportAsTypes
                        ? "enum"
                        : "export enum";

                    sb.AppendLine($"{typeDeclaration} {Type(type.Name, type.GenericArgs)}");
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
                                ? $"{name} = '{name}',"
                                : $"{name} = {value},");
                        }
                    }

                    sb = sb.UnIndent();
                    sb.AppendLine("}");
                }
                else
                {
                    var sbType = StringBuilderCache.Allocate();

                    var typeDeclaration = !Config.ExportAsTypes
                        ? "type"
                        : "export type";

                    sbType.Append($"{typeDeclaration} {Type(type.Name, type.GenericArgs)} = ");

                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        if (i > 0)
                            sbType.Append(" | ");

                        sbType.Append('"').Append(type.EnumNames[i]).Append('"');
                    }

                    sbType.Append(";");

                    sb.AppendLine(StringBuilderCache.ReturnAndFree(sbType));
                }
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

                string responseTypeExpression = options?.Op != null
                    ? "public createResponse() {}"
                    : null;
                string responseMethod = options?.Op?.Method != null
                    ? $"public getMethod() {{ return '{options.Op.Method}'; }}"
                    : null;

                var interfaces = new List<string>();
                var implStr = options.ImplementsFn?.Invoke();
                if (!string.IsNullOrEmpty(implStr))
                {
                    interfaces.Add(implStr);

                    if (Config.ExportAsTypes)
                    { 
                        if (implStr.StartsWith("IReturn<"))
                        {
                            var types = implStr.RightPart('<');
                            var returnType = types.Substring(0, types.Length - 1);

                            // This is to avoid invalid syntax such as "return new string()"
                            primitiveDefaultValues.TryGetValue(returnType, out var replaceReturnType);

                            if (returnType == "any")
                                replaceReturnType = "{}";
                            else if (returnType.EndsWith("[]"))
                                replaceReturnType = $"new Array<{returnType.Substring(0, returnType.Length -2)}>()";
                            
                            responseTypeExpression = replaceReturnType == null ?
                                "public createResponse() {{ return new {0}(); }}".Fmt(returnType) :
                                "public createResponse() {{ return {0}; }}".Fmt(replaceReturnType);
                        }
                        else if (implStr == "IReturnVoid")
                        {
                            responseTypeExpression = "public createResponse() {}";
                        }
                    }
                }

                type.Implements.Each(x => interfaces.Add(Type(x)));

                var isClass = Config.ExportAsTypes && !type.IsInterface.GetValueOrDefault();
                var modifier = isClass ? "public " : "";
                var extend = extends.Count > 0
                    ? " extends " + extends[0]
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

                var typeDeclaration = !Config.ExportAsTypes
                    ? "interface"
                    : $"export {(isClass ? "class" : "interface")}";

                var typeName = Type(type.Name, type.GenericArgs);
                sb.AppendLine($"{typeDeclaration} {typeName}{extend}");
                sb.AppendLine("{");

                sb = sb.Indent();
                InnerTypeFilter?.Invoke(sb, type);

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
                if (addVersionInfo)
                {
                    sb.AppendLine(modifier + "{0}{1}: number; //{2}".Fmt(
                        GetPropertyName("Version"), isClass ? "" : "?", Config.AddImplicitVersion));
                }

                if (Config.ExportAsTypes)
                {
                    if (type.Name == "IReturn`1")
                    {
                        sb.AppendLine("createResponse(): T;");
                    }
                    else if (type.Name == "IReturnVoid")
                    {
                        sb.AppendLine("createResponse(): void;");
                    }
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                if (EmitPartialConstructors && Config.ExportAsTypes && isClass)
                {
                    sb.AppendLine();
                    var callSuper = type.Inherits != null 
                        ? !(extend.StartsWith(" extends Array<") || extend.StartsWith(" extends Dictionary<"))
                            ? "super(init); " 
                            : "super(); "
                        : "";
                    sb.AppendLine($"public constructor(init?: Partial<{typeName}>) {{ {callSuper}(Object as any).assign(this, init); }}");
                }

                if (Config.ExportAsTypes)
                {
                    if (options?.Op != null)
                    {
                        sb.AppendLine("public getTypeName() {{ return '{0}'; }}".Fmt(type.Name));
                    }
                    if (responseMethod != null)
                    {
                        sb.AppendLine(responseMethod);
                    }
                    if (responseTypeExpression != null)
                    {
                        sb.AppendLine(responseTypeExpression);
                    }
                }

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
            var isClass = Config.ExportAsTypes && !type.IsInterface.GetValueOrDefault();
            var modifier = isClass ? "public " : "";

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = GetPropertyType(prop, out var optionalProperty);
                    propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;

                    var optional = IsPropertyOptional(this, type, prop) ?? optionalProperty
                        ? "?"
                        : "";

                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                    sb.Emit(prop, Lang.TypeScript);
                    PrePropertyFilter?.Invoke(sb, prop, type);
                    sb.AppendLine(modifier + "{1}{2}: {0};".Fmt(propType, GetPropertyName(prop.Name), optional));
                    PostPropertyFilter?.Invoke(sb, prop, type);
                }
            }

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine(modifier + "{0}{1}: ResponseStatus;".Fmt(
                    GetPropertyName(nameof(ResponseStatus)), Config.ExportAsTypes ? "" : "?"));
            }
        }

        public static bool? DefaultIsPropertyOptional(TypeScriptGenerator generator, MetadataType type, MetadataPropertyType prop)
        {
            if (prop.IsRequired == true)
            {
                if (prop.Type == "string" || generator.TypeAlias(prop.Type) == "string")
                    return generator.Config.MakePropertiesOptional;
                return false;
            }
            
            if (generator.Config.MakePropertiesOptional)
                return true;

            return null;
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

            string cooked = null;

            if (genericArgs != null)
            {
                if (ArrayTypes.Contains(type))
                {
                    cooked = "Array<{0}>".Fmt(GenericArg(genericArgs[0])).StripNullable();
                }
                if (DictionaryTypes.Contains(type))
                {
                    addDeclaration = DictionaryDeclaration;
                    cooked = "Dictionary<{0}>".Fmt(GenericArg(genericArgs[1])); //Key Type always string
                }
            }
            
            if (cooked == null)
                cooked = Type(type, genericArgs);
            
            useType = CookedDeclarationTypeFilter?.Invoke(cooked);
            if (useType != null)
                return useType;
            
            //TypeScript doesn't support short-hand Dictionary notation or has a Generic Dictionary Type
            if (cooked.StartsWith("{"))
                return "any";

            //TypeScript doesn't support short-hand T[] notation in extension list 
            //E.g. need to use `extends Array<Type>` instead of `extends Type[]`
            var arrParts = cooked.SplitOnFirst('[');
            return arrParts.Length > 1 
                ? "Array<{0}>".Fmt(arrParts[0]) 
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
                    cooked = "{0}?".Fmt(GenericArg(genericArgs[0]));
                else if (ArrayTypes.Contains(type))
                    cooked = "{0}[]".Fmt(GenericArg(genericArgs[0])).StripNullable();
                else if (DictionaryTypes.Contains(type))
                {
                    cooked = "{{ [index: {0}]: {1}; }}".Fmt(
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
                return "{0}[]".Fmt(TypeAlias(arrParts[0]));

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

        public string GetPropertyName(string name) => name.SafeToken().PropertyStyle();
    }

    public static class TypeScriptGeneratorExtensions
    {
        public static string InReturnMarker(this string type)
        {
            var useType = TypeScriptGenerator.ReturnMarkerFilter?.Invoke(type);
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
}
