using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.CSharp
{
    public class CSharpGenerator : ILangGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        private List<MetadataType> allTypes;

        public CSharpGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }
        
        public static Action<StringBuilderWrapper, MetadataType> PreTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> InnerTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataType> PostTypeFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PrePropertyFilter { get; set; }
        public static Action<StringBuilderWrapper, MetadataPropertyType, MetadataType> PostPropertyFilter { get; set; }

        public static Dictionary<string, string> TypeAliases = new() {
            { "String", "string" },    
            { "Boolean", "bool" },    
            { "Byte", "byte" },    
            { "Int16", "short" },    
            { "Int32", "int" },    
            { "Int64", "long" },    
            { "UInt16", "ushort" },    
            { "UInt32", "uint" },    
            { "UInt64", "ulong" },    
            { "Single", "float" },    
            { "Double", "double" },    
            { "Decimal", "decimal" },
        };

        public static TypeFilterDelegate TypeFilter { get; set; }

        public static Func<CSharpGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

        /// <summary>
        /// Helper to make Nullable Reference Type Annotations
        /// </summary>
        [Obsolete("Use ConfigurePlugin<NativeTypesFeature>(feature => feature.MetadataTypesConfig.AddNullableAnnotations = true);")]
        public static bool UseNullableAnnotations
        {
            set
            {
                if (value)
                {
                    PropertyTypeFilter = (gen, type, prop) => 
                        prop.IsRequired == true || prop.PropertyInfo?.PropertyType.IsValueType == true
                            ? gen.GetPropertyType(prop)
                            : gen.GetPropertyType(prop).EnsureSuffix('?');
                }
            }
        }

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
            var namespaces = Config.GetDefaultNamespaces(metadata);

            metadata.RemoveIgnoredTypesForNet(Config);

            if (!Config.ExcludeNamespace)
            {
                if (Config.GlobalNamespace == null)
                {
                    metadata.Types.Each(x => namespaces.Add(x.Namespace));
                    metadata.Operations.Each(x => {
                        namespaces.Add(x.Request.Namespace);
                        if (x.Response != null)
                            namespaces.Add(x.Response.Namespace);
                    });
                }
                else
                {
                    namespaces.Add(Config.GlobalNamespace);
                }
            }

            string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sbInner = StringBuilderCache.Allocate();
            var sb = new StringBuilderWrapper(sbInner);
            var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
            if (includeOptions)
            {
                sb.AppendLine("/* Options:");
                sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T"," ")));
                sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
                sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
                sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
                if (Config.UsePath != null)
                    sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));
                    
                sb.AppendLine();
                sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace)); // ExcludeNamespace=true excludes namespace
                sb.AppendLine("{0}MakePartial: {1}".Fmt(defaultValue("MakePartial"), Config.MakePartial));
                sb.AppendLine("{0}MakeVirtual: {1}".Fmt(defaultValue("MakeVirtual"), Config.MakeVirtual));
                sb.AppendLine("{0}MakeInternal: {1}".Fmt(defaultValue("MakeInternal"), Config.MakeInternal));
                sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
                sb.AppendLine("{0}AddNullableAnnotations: {1}".Fmt(defaultValue("AddNullableAnnotations"), Config.AddNullableAnnotations));
                sb.AppendLine("{0}AddReturnMarker: {1}".Fmt(defaultValue("AddReturnMarker"), Config.AddReturnMarker));
                sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
                sb.AppendLine("{0}AddDataContractAttributes: {1}".Fmt(defaultValue("AddDataContractAttributes"), Config.AddDataContractAttributes));
                sb.AppendLine("{0}AddIndexesToDataMembers: {1}".Fmt(defaultValue("AddIndexesToDataMembers"), Config.AddIndexesToDataMembers));
                sb.AppendLine("{0}AddGeneratedCodeAttributes: {1}".Fmt(defaultValue("AddGeneratedCodeAttributes"), Config.AddGeneratedCodeAttributes));
                sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
                sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
                sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
                sb.AppendLine("{0}ExportValueTypes: {1}".Fmt(defaultValue("ExportValueTypes"), Config.ExportValueTypes));
                sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
                sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
                sb.AppendLine("{0}AddNamespaces: {1}".Fmt(defaultValue("AddNamespaces"), Config.AddNamespaces.Safe().ToArray().Join(",")));
                sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));
                AddQueryParamOptions.Each(name => sb.AppendLine($"{defaultValue(name)}{name}: {request.QueryString[name]}"));
                sb.AppendLine("*/");
                sb.AppendLine();
            }

            namespaces.Where(x => !string.IsNullOrEmpty(x))
                .Each(x => sb.AppendLine($"using {x};"));
            if (Config.AddGeneratedCodeAttributes)
                sb.AppendLine("using System.CodeDom.Compiler;");

            if (Config.AddDataContractAttributes
                && Config.AddDefaultXmlNamespace != null)
            {
                sb.AppendLine();

                namespaces.Where(x => !Config.DefaultNamespaces.Contains(x)).ToList()
                    .ForEach(x => sb.AppendLine(
                        $"[assembly: ContractNamespace(\"{Config.AddDefaultXmlNamespace}\", ClrNamespace=\"{x}\")]"));
            }

            sb.AppendLine();

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

            var orderedTypes = allTypes
                .OrderBy(x => x.Namespace)
                .ThenBy(x => x.Name)
                .ToList();

            var insertCode = InsertCodeFilter?.Invoke(orderedTypes, Config);
            if (insertCode != null)
                sb.AppendLine(insertCode);
            
            foreach (var type in orderedTypes)
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

                        lastNS = AppendType(ref sb, type, lastNS, allTypes, 
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
                        lastNS = AppendType(ref sb, type, lastNS, allTypes, 
                            new CreateTypeOptions { IsResponse = true, });

                        existingTypes.Add(fullTypeName);
                    }
                }
                else if (types.Contains(type) && !existingTypes.Contains(fullTypeName))
                {
                    lastNS = AppendType(ref sb, type, lastNS, allTypes, 
                        new CreateTypeOptions { IsType = true });
                    
                    existingTypes.Add(fullTypeName);
                }
            }

            var addCode = AddCodeFilter?.Invoke(orderedTypes, Config);
            if (addCode != null)
                sb.AppendLine(addCode);

            if (lastNS != null)
                sb.AppendLine("}");
            sb.AppendLine();

            return StringBuilderCache.ReturnAndFree(sbInner);
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS, List<MetadataType> allTypes, CreateTypeOptions options)
        {
            if (type.IsNested.GetValueOrDefault() && !options.IsNestedType)
                return lastNS;

            if (!Config.ExcludeNamespace)
            {
                var ns = Config.GlobalNamespace ?? type.Namespace;
                if (ns != lastNS)
                {
                    if (lastNS != null)
                    {
                        sb.AppendLine("}");
                        sb.AppendLine();
                    }

                    lastNS = ns;

                    sb.AppendLine($"namespace {ns.SafeToken()}");
                    sb.AppendLine("{");
                }

                sb = sb.Indent();
            }

            AppendComments(sb, type.Description);
            if (options?.Routes != null)
            {
                AppendAttributes(sb, options.Routes.ConvertAll(x => x.ToMetadataAttribute()));
            }
            AppendAttributes(sb, type.Attributes);
            AppendDataContract(sb, type.DataContract);
            if (Config.AddGeneratedCodeAttributes)
                sb.AppendLine($"[GeneratedCode(\"AddServiceStackReference\", \"{Env.VersionString}\")]");

            var typeAccessor = !Config.MakeInternal ? "public" : "internal";

            sb.Emit(type, Lang.CSharp);
            PreTypeFilter?.Invoke(sb, type);

            if (type.IsEnum.GetValueOrDefault())
            {
                sb.AppendLine($"{typeAccessor} enum {Type(type.Name, type.GenericArgs)}");
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
                            AppendAttributes(sb, new List<MetadataAttribute> {
                                new MetadataAttribute {
                                    Name = "EnumMember",
                                    Args = new List<MetadataPropertyType> {
                                        new() {
                                            Name = "Value",
                                            Value = memberValue,
                                            Type = "String",
                                        }
                                    }
                                }
                            });
                        }
                        sb.AppendLine(value == null 
                            ? $"{name},"
                            : $"{name} = {value},");
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }
            else
            {
                var partial = Config.MakePartial ? "partial " : "";
                var defType = type.IsInterface() ? "interface" : "class";
                sb.AppendLine($"{typeAccessor} {partial}{defType} {Type(type.Name, type.GenericArgs)}");

                //: BaseClass, Interfaces
                var inheritsList = new List<string>();
                if (type.Inherits != null)
                {
                    inheritsList.Add(Type(type.GetInherits(), includeNested:true));
                }

                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        inheritsList.Add(implStr);
                }

                type.Implements.Each(x => inheritsList.Add(Type(x)));

                var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;
                if (makeExtensible)
                    inheritsList.Add("IExtensibleDataObject");
                if (inheritsList.Count > 0)
                    sb.AppendLine($"    : {string.Join(", ", inheritsList.ToArray())}");

                sb.AppendLine("{");

                sb = sb.Indent();
                InnerTypeFilter?.Invoke(sb, type);

                AddConstructor(sb, type, options);
                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                foreach (var innerTypeRef in type.InnerTypes.Safe())
                {
                    var innerType = allTypes.FirstOrDefault(x => x.Name == innerTypeRef.Name);
                    if (innerType == null)
                        continue;

                    sb = sb.UnIndent();
                    AppendType(ref sb, innerType, lastNS, allTypes,
                        new CreateTypeOptions { IsNestedType = true });
                    sb = sb.Indent();
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }

            PostTypeFilter?.Invoke(sb, type);

            if (!Config.ExcludeNamespace)
            {
                sb = sb.UnIndent();
            }
            sb.AppendLine();

            return lastNS;
        }

        private void AddConstructor(StringBuilderWrapper sb, MetadataType type, CreateTypeOptions options)
        {
            if (type.IsInterface())
                return;

            if (Config.AddImplicitVersion == null && !Config.InitializeCollections)
                return;

            var collectionProps = new List<MetadataPropertyType>();
            if (type.Properties != null && Config.InitializeCollections)
                collectionProps = type.Properties.Where(x => x.IsCollection()).ToList();

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (!addVersionInfo && collectionProps.Count <= 0) return;

            if (addVersionInfo)
            {
                var virt = Config.MakeVirtual ? "virtual " : "";
                sb.AppendLine($"public {virt}int Version {{ get; set; }}");
                sb.AppendLine();
            }

            sb.AppendLine($"public {NameOnly(type.Name)}()");
            sb.AppendLine("{");
            sb = sb.Indent();

            if (addVersionInfo)
                sb.AppendLine($"Version = {Config.AddImplicitVersion};");

            foreach (var prop in collectionProps)
            {
                sb.AppendLine($"{GetPropertyName(prop.Name)} = new {Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs,includeNested:true)}{{}};");
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
        {
            var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;

            var virt = Config.MakeVirtual && !type.IsInterface() ? "virtual " : "";
            var wasAdded = false;

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = GetPropertyType(prop);
                    propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;
                    
                    if (Config.AddNullableAnnotations)
                    {
                        if (prop.IsRequired != true && (prop.PropertyInfo?.PropertyType.IsValueType) != true)
                        {
                            propType = GetPropertyType(prop).EnsureSuffix('?');
                        }
                    }    

                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    var visibility = type.IsInterface() ? "" : "public ";
                    
                    sb.Emit(prop, Lang.CSharp);
                    PrePropertyFilter?.Invoke(sb, prop, type);
                    sb.AppendLine($"{visibility}{virt}{propType} {GetPropertyName(prop.Name)} {{ get; set; }}");
                    PostPropertyFilter?.Invoke(sb, prop, type);
                }
            }

            if (type.IsInterface())
                return;

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine($"public {virt}ResponseStatus ResponseStatus {{ get; set; }}");
            }

            if (makeExtensible
                && (type.Properties == null
                    || type.Properties.All(x => x.Name != "ExtensionData")))
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                sb.AppendLine($"public {virt}ExtensionDataObject ExtensionData {{ get; set; }}");
            }
        }

        public virtual string GetPropertyType(MetadataPropertyType prop)
        {
            var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs, includeNested: true);
            return propType;
        }

        public static Dictionary<string,string[]> AttributeConstructorArgs { get; set; } = new Dictionary<string, string[]> {
            ["ValidateRequest"] = new[] { nameof(ValidateRequestAttribute.Validator) },
            ["Validate"] = new[] { nameof(ValidateRequestAttribute.Validator) },
        };

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            foreach (var attr in attributes)
            {
                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine($"[{GetPropertyName(attr.Name)}]");
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
                        AttributeConstructorArgs.TryGetValue(attr.Name, out var attrCtorArgs);
                        
                        foreach (var attrArg in attr.Args)
                        {
                            if (args.Length > 0)
                                args.Append(", ");
                            
                            if (attrCtorArgs?.Contains(attrArg.Name) == true)
                                args.Append(TypeValue(attrArg.Type, attrArg.Value));
                            else
                                args.Append($"{attrArg.Name}={TypeValue(attrArg.Type, attrArg.Value)}");
                        }
                    }
                    sb.AppendLine($"[{attr.Name}({StringBuilderCacheAlt.ReturnAndFree(args)})]");
                }
            }

            return true;
        }

        public string TypeValue(string type, string value)
        {
            var alias = TypeAlias(type);
            if (type == nameof(Int32))
            {
                if (value == int.MinValue.ToString())
                    return "int.MinValue";
                if (value == int.MaxValue.ToString())
                    return "int.MaxValue";
            }
            if (type == nameof(Int64))
            {
                if (value == long.MinValue.ToString())
                    return "long.MinValue";
                if (value == long.MaxValue.ToString())
                    return "long.MaxValue";
            }
            if (value == null)
                return "null";
            if (alias == "string")
                return value.ToEscapedString();
            
            if (value.IsTypeValue() && !string.IsNullOrEmpty(Config.GlobalNamespace))
            {
                //Only emit type as Namespaces are merged
                return "typeof(" + value.ExtractTypeName() + ")";
            }
            
            return value;
        }

        public string Type(MetadataTypeName typeName, bool includeNested = false)
        {
            return Type(typeName.Name, typeName.GenericArgs, includeNested: includeNested);
        }

        public string Type(string type, string[] genericArgs, bool includeNested=false)
        {
            var useType = TypeFilter?.Invoke(type, genericArgs);
            if (useType != null)
                return useType;

            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return $"{TypeAlias(genericArgs[0], includeNested: includeNested)}?";

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = StringBuilderCacheAlt.Allocate();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg.SanitizeType(), includeNested: includeNested));
                    }

                    var typeName = NameOnly(type, includeNested: includeNested).SanitizeType();
                    return $"{typeName}<{StringBuilderCacheAlt.ReturnAndFree(args)}>";
                }
            }

            return TypeAlias(type, includeNested: includeNested);
        }

        public static string TypeAlias(string type, bool includeNested = false)
        {
            type = type.SanitizeType();
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return $"{TypeAlias(arrParts[0], includeNested: includeNested)}[]";

            TypeAliases.TryGetValue(type, out var typeAlias);

            return typeAlias ?? NameOnly(type, includeNested: includeNested);
        }

        public static string NameOnly(string type, bool includeNested = false)
        {
            var name = type.LeftPart('`');

            if (!includeNested)
                name = name.LastRightPart('.');

            return name.SafeToken();
        }

        public bool AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc == null) return false;

            if (Config.AddDescriptionAsComments)
            {
                sb.AppendLine("///<summary>");
                sb.AppendLine($"///{desc.SafeComment()}");
                sb.AppendLine("///</summary>");
                return false;
            }
            else
            {
                sb.AppendLine($"[Description({desc.QuotedSafeValue()})]");
                return true;
            }
        }

        public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
        {
            if (dcMeta == null)
            {
                if (Config.AddDataContractAttributes)
                    sb.AppendLine("[DataContract]");
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
            sb.AppendLine($"[DataContract{dcArgs}]");
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                        ? $"[DataMember(Order={dataMemberIndex})]"
                        : "[DataMember]");
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

                    dmArgs += $"IsRequired={dmMeta.IsRequired.ToString().ToLowerInvariant()}";
                }

                if (dmMeta.EmitDefaultValue != null)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += $"EmitDefaultValue={dmMeta.EmitDefaultValue.ToString().ToLowerInvariant()}";
                }

                dmArgs = $"({dmArgs})";
            }
            sb.AppendLine($"[DataMember{dmArgs}]");

            return true;
        }

        public string GetPropertyName(string name) => name.SafeToken();
    }

    public static class CSharpGeneratorExtensions
    {
        // Handle inheriting from a nested class which needs to be fully-qualified in C#
        public static MetadataTypeName GetInherits(this MetadataType type)
        {
            if (type.Inherits == null || type.Inherits.GenericArgs.IsEmpty() || type.InnerTypes.IsEmpty())
                return type.Inherits;

            for (var i = 0; i < type.Inherits.GenericArgs.Length; i++)
            {
                foreach (var innerType in type.InnerTypes)
                {
                    if (innerType.Name.LastRightPart('.') == type.Inherits.GenericArgs[i])
                    {
                        type.Inherits.GenericArgs[i] = innerType.Name;
                    }
                }
            }

            return type.Inherits;
        }
    }
}