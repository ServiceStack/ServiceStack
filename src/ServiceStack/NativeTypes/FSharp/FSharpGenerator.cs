using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.FSharp
{
    public class FSharpGenerator : ILangGenerator
    {
        readonly MetadataTypesConfig Config;
        private readonly NativeTypesFeature feature;
        private List<MetadataType> allTypes;

        public FSharpGenerator(MetadataTypesConfig config)
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
            ["List"] = "ResizeArray"
        };

        public static TypeFilterDelegate TypeFilter { get; set; }

        public static Func<FSharpGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

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

        /// <summary>
        /// Can only export "Empty" Marker Interfaces
        /// </summary>
        public static HashSet<string> ExportMarkerInterfaces { get; } = new[] {
            nameof(IGet),
            nameof(IPost),
            nameof(IPut),
            nameof(IDelete),
            nameof(IPatch),
            nameof(IOptions),
            nameof(IStream),
        }.ToSet();

        public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
        {
            var namespaces = Config.GetDefaultNamespaces(metadata);

            var typeNamespaces = new HashSet<string>();
            metadata.RemoveIgnoredTypesForNet(Config);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => {
                typeNamespaces.Add(x.Request.Namespace);
                if (x.Response != null)
                    typeNamespaces.Add(x.Response.Namespace);
            });

            // Look first for shortest Namespace ending with `ServiceModel` convention, else shortest ns
            var globalNamespace = Config.GlobalNamespace
                ?? typeNamespaces.Where(x => x.EndsWith("ServiceModel"))
                    .OrderBy(x => x).FirstOrDefault()
                ?? typeNamespaces.OrderBy(x => x).FirstOrDefault() ?? "ServiceModel";

            string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sbInner = new StringBuilder();
            var sb = new StringBuilderWrapper(sbInner);
            var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
            if (includeOptions)
            {
                sb.AppendLine("(* Options:");
                sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
                sb.AppendLine("Version: {0}".Fmt(Env.VersionString));
                sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
                sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
                if (Config.UsePath != null)
                    sb.AppendLine("UsePath: {0}".Fmt(Config.UsePath));

                sb.AppendLine();
                sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace)); // ExcludeNamespace=true excludes namespace
                sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
                sb.AppendLine("{0}AddReturnMarker: {1}".Fmt(defaultValue("AddReturnMarker"), Config.AddReturnMarker));
                sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
                sb.AppendLine("{0}AddDataContractAttributes: {1}".Fmt(defaultValue("AddDataContractAttributes"), Config.AddDataContractAttributes));
                sb.AppendLine("{0}AddIndexesToDataMembers: {1}".Fmt(defaultValue("AddIndexesToDataMembers"), Config.AddIndexesToDataMembers));
                sb.AppendLine("{0}AddGeneratedCodeAttributes: {1}".Fmt(defaultValue("AddGeneratedCodeAttributes"), Config.AddGeneratedCodeAttributes));
                sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
                sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
                sb.AppendLine("{0}ExportValueTypes: {1}".Fmt(defaultValue("ExportValueTypes"), Config.ExportValueTypes));
                sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
                sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
                sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
                //sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));
                sb.AppendLine("{0}AddNamespaces: {1}".Fmt(defaultValue("AddNamespaces"), Config.AddNamespaces.Safe().ToArray().Join(",")));
                AddQueryParamOptions.Each(name => sb.AppendLine($"{defaultValue(name)}{name}: {request.QueryString[name]}"));

                sb.AppendLine("*)");
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
            allTypes.AddRange(types);
            allTypes.AddRange(responseTypes);
            allTypes.AddRange(requestTypes);

            var orderedTypes = FilterTypes(allTypes);

            if (!Config.ExcludeNamespace)
            {
                sb.AppendLine("namespace {0}".Fmt(globalNamespace.SafeToken()));
                sb.AppendLine();
            }
            foreach (var ns in namespaces.Where(x => !string.IsNullOrEmpty(x)))
            {
                sb.AppendLine("open " + ns);
            }
            if (Config.AddGeneratedCodeAttributes)
                sb.AppendLine("open System.CodeDom.Compiler");

            var insertCode = InsertCodeFilter?.Invoke(allTypes, Config);
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

            sb.AppendLine();

            return StringBuilderCache.ReturnAndFree(sbInner);
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
            if (!Config.ExcludeNamespace)
                sb = sb.Indent();

            sb.AppendLine();
            AppendComments(sb, type.Description);
            if (options?.Routes != null)
            {
                AppendAttributes(sb, options.Routes.ConvertAll(x => x.ToMetadataAttribute()));
            }
            AppendAttributes(sb, type.Attributes);
            AppendDataContract(sb, type.DataContract);
            if (Config.AddGeneratedCodeAttributes)
            {
                sb.AppendLine($"[<GeneratedCode(\"AddServiceStackReference\", \"{Env.VersionString}\")>]");
            }

            sb.Emit(type, Lang.FSharp);
            PreTypeFilter?.Invoke(sb, type);

            if (type.IsEnum.GetValueOrDefault())
            {
                sb.AppendLine("type {0} =".Fmt(Type(type.Name, type.GenericArgs)));
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues != null ? type.EnumValues[i] : i.ToString();
                        sb.AppendLine($"| {name} = {value}");
                    }
                }

                sb = sb.UnIndent();
            }
            else
            {
                //sb.AppendLine("[<CLIMutable>]"); // only for Record Types
                var classCtor = type.IsInterface() ? "" : "()";
                sb.AppendLine("[<AllowNullLiteral>]");
                sb.AppendLine($"type {Type(type.Name, type.GenericArgs)}{classCtor} = ");
                sb = sb.Indent();
                var startLen = sb.Length;

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                    sb.AppendLine($"inherit {Type(type.Inherits)}()");

                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        sb.AppendLine($"interface {implStr}");
                }

                InnerTypeFilter?.Invoke(sb, type);

                if (!type.IsInterface())
                {
                    var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;
                    if (makeExtensible)
                    {
                        sb.AppendLine("interface IExtensibleDataObject with");
                        sb.AppendLine("    member val ExtensionData:ExtensionDataObject = null with get, set");
                        sb.AppendLine("end");
                    }

                    var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
                    if (addVersionInfo)
                    {
                        sb.AppendLine($"member val Version:int = {Config.AddImplicitVersion} with get, set");
                    }

                    if (type.Implements != null)
                    {
                        foreach (var iface in type.Implements)
                        {
                            if (ExportMarkerInterfaces.Contains(iface.Name))
                                sb.AppendLine($"interface {iface.Name}");
                        }
                    }
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != nameof(ResponseStatus)));

                if (sb.Length == startLen)
                    sb.AppendLine(type.IsInterface() ? "interface end" : "class end");

                sb = sb.UnIndent();
            }
            
            PostTypeFilter?.Invoke(sb, type);

            if (!Config.ExcludeNamespace)
                sb = sb.UnIndent();
            
            return lastNS;
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
        {
            var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;

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

                    sb.Emit(prop, Lang.FSharp);
                    PrePropertyFilter?.Invoke(sb, prop, type);
                    if (!type.IsInterface())
                    {
                        sb.AppendLine($"member val {GetPropertyName(prop.Name)}:{propType} = {GetDefaultLiteral(prop, type)} with get,set");
                    }
                    else
                    {
                        sb.AppendLine($"abstract {GetPropertyName(prop.Name)}:{propType} with get,set");                        
                    }
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
                sb.AppendLine("member val ResponseStatus:ResponseStatus = null with get,set");
            }

            if (makeExtensible
                && (type.Properties == null
                    || type.Properties.All(x => x.Name != "ExtensionData")))
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                sb.AppendLine("member val ExtensionData:ExtensionDataObject = null with get,set");
            }
        }

        public virtual string GetPropertyType(MetadataPropertyType prop)
        {
            var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);
            return propType;
        }

        private string GetDefaultLiteral(MetadataPropertyType prop, MetadataType type)
        {
            var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs);

            if (Config.InitializeCollections && prop.IsCollection())
            {
                return prop.IsArray()
                    ? "[||]" 
                    : $"new {propType}()";
            }
            return prop.IsValueType.GetValueOrDefault() && propType != "String"
                ? $"new {propType}()"
                : "null";
        }

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            foreach (var attr in attributes)
            {
                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine($"[<{attr.Name}>]");
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
                    sb.AppendLine($"[<{attr.Name}({StringBuilderCacheAlt.ReturnAndFree(args)})>]");
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
                    return "Int32.MinValue";
                if (value == int.MaxValue.ToString())
                    return "Int32.MaxValue";
            }
            if (type == nameof(Int64))
            {
                if (value == long.MinValue.ToString())
                    return "Int64.MinValue";
                if (value == long.MaxValue.ToString())
                    return "Int64.MaxValue";
            }
            if (value == null)
                return "null";
            if (alias == "string" || type == "String")
                return value.ToEscapedString();

            if (value.StartsWith("typeof("))
            {
                //Only emit type as Namespaces are merged
                var typeNameOnly = value.Substring(7, value.Length - 8).LastRightPart('.');
                return "typeof<" + typeNameOnly + ">";
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
                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = StringBuilderCacheAlt.Allocate();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg));
                    }

                    var typeName = NameOnly(type);
                    return $"{TypeAlias(typeName)}<{StringBuilderCacheAlt.ReturnAndFree(args)}>";
                }
            }

            return TypeAlias(type);
        }

        private string TypeAlias(string type)
        {
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return $"{TypeAlias(arrParts[0])}[]";

            TypeAliases.TryGetValue(type, out var typeAlias);

            return typeAlias ?? NameOnly(type);
        }

        public string NameOnly(string type)
        {
            return type.LeftPart('`').LastRightPart('.').SafeToken();
        }

        public bool AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc == null) return false;

            if (Config.AddDescriptionAsComments)
            {
                sb.AppendLine("///<summary>");
                sb.AppendLine($"///{desc.SafeComment()}");
                sb.AppendLine("///</summary>");
                return true;
            }
            else
            {
                sb.AppendLine($"[<Description({desc.QuotedSafeValue()})>]");
                return true;
            }
        }

        public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
        {
            if (dcMeta == null)
            {
                if (Config.AddDataContractAttributes)
                    sb.AppendLine("[<DataContract>]");
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
            sb.AppendLine($"[<DataContract{dcArgs}>]");
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                        ? $"[<DataMember(Order={dataMemberIndex})>]"
                        : "[<DataMember>]");
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
            sb.AppendLine($"[<DataMember{dmArgs}>]");

            return true;
        }

        public string GetPropertyName(string name) => name.SafeToken();
    }

    public static class FSharpGeneratorExtensions
    {
        public static bool Contains(this Dictionary<string, List<string>> map, string key, string value)
        {
            return map.TryGetValue(key, out var results) && results.Contains(value);
        }
    }

}