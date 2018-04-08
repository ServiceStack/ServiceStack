using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.CSharp
{
    public class CSharpGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        private List<MetadataType> allTypes;

        public CSharpGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }

        public static Dictionary<string, string> TypeAliases = new Dictionary<string, string> 
        {
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

        public static Func<List<MetadataType>, List<MetadataType>> FilterTypes = DefaultFilterTypes;

        public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types;

        public string GetCode(MetadataTypes metadata, IRequest request)
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
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T"," ")));
            sb.AppendLine("Version: {0}".Fmt(Env.ServiceStackVersion));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}MakePartial: {1}".Fmt(defaultValue("MakePartial"), Config.MakePartial));
            sb.AppendLine("{0}MakeVirtual: {1}".Fmt(defaultValue("MakeVirtual"), Config.MakeVirtual));
            sb.AppendLine("{0}MakeInternal: {1}".Fmt(defaultValue("MakeInternal"), Config.MakeInternal));
            sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
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

            sb.AppendLine("*/");
            sb.AppendLine();

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

            var requestTypes = metadata.Operations.Select(x => x.Request).ToHashSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToHashSet();
            var types = metadata.Types.ToHashSet();

            allTypes = new List<MetadataType>();
            allTypes.AddRange(requestTypes);
            allTypes.AddRange(responseTypes);
            allTypes.AddRange(types);

            allTypes = FilterTypes(allTypes);

            var orderedTypes = allTypes
                .OrderBy(x => x.Namespace)
                .ThenBy(x => x.Name);

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
                                ImplementsFn = () =>
                                    {
                                        if (!Config.AddReturnMarker
                                            && !type.ReturnVoidMarker
                                            && type.ReturnMarkerTypeName == null)
                                            return null;

                                        if (type.ReturnVoidMarker)
                                            return "IReturnVoid";
                                        if (type.ReturnMarkerTypeName != null)
                                            return Type("IReturn`1", new[] { Type(type.ReturnMarkerTypeName) });
                                        return response != null
                                            ? Type("IReturn`1", new[] { Type(response.Name, response.GenericArgs) })
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
                        sb.AppendLine("}");

                    lastNS = ns;

                    sb.AppendLine();
                    sb.AppendLine($"namespace {ns.SafeToken()}");
                    sb.AppendLine("{");
                }

                sb = sb.Indent();
            }

            sb.AppendLine();
            AppendComments(sb, type.Description);
            if (type.Routes != null)
            {
                AppendAttributes(sb, type.Routes.ConvertAll(x => x.ToMetadataAttribute()));
            }
            AppendAttributes(sb, type.Attributes);
            AppendDataContract(sb, type.DataContract);
            if (Config.AddGeneratedCodeAttributes)
                sb.AppendLine($"[GeneratedCode(\"AddServiceStackReference\", \"{Env.VersionString}\")]");

            var typeAccessor = !Config.MakeInternal ? "public" : "internal";

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

                    type.Implements.Each(x => inheritsList.Add(Type(x)));
                }

                var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;
                if (makeExtensible)
                    inheritsList.Add("IExtensibleDataObject");
                if (inheritsList.Count > 0)
                    sb.AppendLine($"    : {string.Join(", ", inheritsList.ToArray())}");

                sb.AppendLine("{");
                sb = sb.Indent();

                AddConstuctor(sb, type, options);
                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != typeof(ResponseStatus).Name));

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

            if (!Config.ExcludeNamespace)
            {
                sb = sb.UnIndent();
            }

            return lastNS;
        }

        private void AddConstuctor(StringBuilderWrapper sb, MetadataType type, CreateTypeOptions options)
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
                sb.AppendLine($"{prop.Name.SafeToken()} = new {Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs,includeNested:true)}{{}};");
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

                    var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs, includeNested:true);
                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    var visibility = type.IsInterface() ? "" : "public ";
                    sb.AppendLine($"{visibility}{virt}{propType} {prop.Name.SafeToken()} {{ get; set; }}");
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

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            foreach (var attr in attributes)
            {
                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine($"[{attr.Name}]");
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
                            args.Append($"{TypeValue(ctorArg.Type, ctorArg.Value)}");
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
                    sb.AppendLine($"[{attr.Name}({StringBuilderCacheAlt.ReturnAndFree(args)})]");
                }
            }

            return true;
        }

        public string TypeValue(string type, string value)
        {
            var alias = TypeAlias(type);
            if (value == null)
                return "null";
            if (alias == "string")
                return value.QuotedSafeValue();
            return value;
        }

        public string Type(MetadataTypeName typeName, bool includeNested = false)
        {
            return Type(typeName.Name, typeName.GenericArgs, includeNested: includeNested);
        }

        public string Type(string type, string[] genericArgs, bool includeNested=false)
        {
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