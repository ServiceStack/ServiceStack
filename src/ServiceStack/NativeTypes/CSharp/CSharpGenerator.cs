using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.CSharp
{
    public class CSharpGenerator
    {
        readonly MetadataTypesConfig Config;

        public CSharpGenerator(MetadataTypesConfig config)
        {
            Config = config;
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

        public string GetCode(MetadataTypes metadata, IRequest request)
        {
            var namespaces = Config.GetDefaultNamespaces(metadata);

            metadata.RemoveIgnoredTypes(Config);

            if (Config.GlobalNamespace == null)
            {
                metadata.Types.Each(x => namespaces.Add(x.Namespace));
                metadata.Operations.Each(x => namespaces.Add(x.Request.Namespace));
            }
            else
            {
                namespaces.Add(Config.GlobalNamespace);
            }

            Func<string,string> defaultValue = k =>
                request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sb = new StringBuilderWrapper(new StringBuilder());
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T"," ")));
            sb.AppendLine("Version: {0}".Fmt(metadata.Version));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}MakePartial: {1}".Fmt(defaultValue("MakePartial"), Config.MakePartial));
            sb.AppendLine("{0}MakeVirtual: {1}".Fmt(defaultValue("MakeVirtual"), Config.MakeVirtual));
            sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
            sb.AppendLine("{0}AddReturnMarker: {1}".Fmt(defaultValue("AddReturnMarker"), Config.AddReturnMarker));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}AddDataContractAttributes: {1}".Fmt(defaultValue("AddDataContractAttributes"), Config.AddDataContractAttributes));
            sb.AppendLine("{0}AddIndexesToDataMembers: {1}".Fmt(defaultValue("AddIndexesToDataMembers"), Config.AddIndexesToDataMembers));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));
            //sb.AppendLine("{0}DefaultNamespaces: {1}".Fmt(defaultValue("DefaultNamespaces"), Config.DefaultNamespaces.ToArray().Join(", ")));
            sb.AppendLine("*/");
            sb.AppendLine();

            namespaces.Each(x => sb.AppendLine("using {0};".Fmt(x)));

            if (Config.AddDataContractAttributes
                && Config.AddDefaultXmlNamespace != null)
            {
                sb.AppendLine();

                namespaces.Where(x => !Config.DefaultNamespaces.Contains(x)).ToList()
                    .ForEach(x =>
                        sb.AppendLine("[assembly: ContractNamespace(\"{0}\", ClrNamespace=\"{1}\")]"
                            .Fmt(Config.AddDefaultXmlNamespace, x)));
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

            var allTypes = new List<MetadataType>();
            allTypes.AddRange(requestTypes);
            allTypes.AddRange(responseTypes);
            allTypes.AddRange(types);

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
                        MetadataOperationType operation;
                        if (requestTypesMap.TryGetValue(type, out operation))
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

            return sb.ToString();
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS, List<MetadataType> allTypes, CreateTypeOptions options)
        {
            if (type.IsNested.GetValueOrDefault() && !options.IsNestedType)
                return lastNS;

            var ns = Config.GlobalNamespace ?? type.Namespace;
            if (ns != lastNS)
            {
                if (lastNS != null)
                    sb.AppendLine("}");

                lastNS = ns;

                sb.AppendLine();
                sb.AppendLine("namespace {0}".Fmt(ns.SafeToken()));
                sb.AppendLine("{");
            }

            sb = sb.Indent();

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
                sb.AppendLine("public enum {0}".Fmt(Type(type.Name, type.GenericArgs)));
                sb.AppendLine("{");
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues != null ? type.EnumValues[i] : null;
                        sb.AppendLine(value == null 
                            ? "{0},".Fmt(name) 
                            : "{0} = {1},".Fmt(name, value));
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }
            else
            {
                var partial = Config.MakePartial ? "partial " : "";
                var defType = type.IsInterface() ? "interface" : "class";
                sb.AppendLine("public {0}{1} {2}".Fmt(partial, defType, Type(type.Name, type.GenericArgs)));

                //: BaseClass, Interfaces
                var inheritsList = new List<string>();
                if (type.Inherits != null)
                {
                    inheritsList.Add(Type(type.Inherits, includeNested:true));
                }

                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        inheritsList.Add(implStr);
                }

                var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;
                if (makeExtensible)
                    inheritsList.Add("IExtensibleDataObject");
                if (inheritsList.Count > 0)
                    sb.AppendLine("    : {0}".Fmt(string.Join(", ", inheritsList.ToArray())));

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

            sb = sb.UnIndent();
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
                var @virtual = Config.MakeVirtual ? "virtual " : "";
                sb.AppendLine("public {0}int Version {{ get; set; }}".Fmt(@virtual));
                sb.AppendLine();
            }

            sb.AppendLine("public {0}()".Fmt(NameOnly(type.Name)));
            sb.AppendLine("{");
            sb = sb.Indent();

            if (addVersionInfo)
                sb.AppendLine("Version = {0};".Fmt(Config.AddImplicitVersion));

            foreach (var prop in collectionProps)
            {
                sb.AppendLine("{0} = new {1}{{}};".Fmt(
                prop.Name.SafeToken(),
                Type(prop.Type, prop.GenericArgs, includeNested: true)));
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
        {
            var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;

            var @virtual = Config.MakeVirtual && !type.IsInterface() ? "virtual " : "";
            var wasAdded = false;

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = Type(prop.Type, prop.GenericArgs, includeNested:true);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    var visibility = type.IsInterface() ? "" : "public ";
                    sb.AppendLine("{0}{1}{2} {3} {{ get; set; }}"
                        .Fmt(visibility, @virtual, propType, prop.Name.SafeToken()));
                }
            }

            if (type.IsInterface())
                return;

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("public {0}ResponseStatus ResponseStatus {{ get; set; }}".Fmt(@virtual));
            }

            if (makeExtensible
                && (type.Properties == null
                    || type.Properties.All(x => x.Name != "ExtensionData")))
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                sb.AppendLine("public {0}ExtensionDataObject ExtensionData {{ get; set; }}".Fmt(@virtual));
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
                    sb.AppendLine("[{0}]".Fmt(attr.Name));
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
                    sb.AppendLine("[{0}({1})]".Fmt(attr.Name, args));
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
                    return "{0}?".Fmt(TypeAlias(genericArgs[0], includeNested: includeNested));

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = new StringBuilder();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg.SanitizeType(), includeNested: includeNested));
                    }

                    var typeName = NameOnly(type, includeNested: includeNested).SanitizeType();
                    return "{0}<{1}>".Fmt(typeName, args);
                }
            }

            return TypeAlias(type, includeNested: includeNested);
        }

        private string TypeAlias(string type, bool includeNested = false)
        {
            type = type.SanitizeType();
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return "{0}[]".Fmt(TypeAlias(arrParts[0], includeNested: includeNested));

            string typeAlias;
            TypeAliases.TryGetValue(type, out typeAlias);

            return typeAlias ?? NameOnly(type, includeNested: includeNested);
        }

        public string NameOnly(string type, bool includeNested = false)
        {
            var name = type.SplitOnFirst('`')[0];

            if (!includeNested)
                name = name.SplitOnLast('.').Last();

            return name.SafeToken();
        }

        public void AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc == null) return;

            if (Config.AddDescriptionAsComments)
            {
                sb.AppendLine("///<summary>");
                sb.AppendLine("///{0}".Fmt(desc.SafeComment()));
                sb.AppendLine("///</summary>");
            }
            else
            {
                sb.AppendLine("[Description({0})]".Fmt(desc.QuotedSafeValue()));
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
                    dcArgs = "Name={0}".Fmt(dcMeta.Name.QuotedSafeValue());

                if (dcMeta.Namespace != null)
                {
                    if (dcArgs.Length > 0)
                        dcArgs += ", ";

                    dcArgs += "Namespace={0}".Fmt(dcMeta.Namespace.QuotedSafeValue());
                }

                dcArgs = "({0})".Fmt(dcArgs);
            }
            sb.AppendLine("[DataContract{0}]".Fmt(dcArgs));
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                                  ? "[DataMember(Order={0})]".Fmt(dataMemberIndex)
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
            sb.AppendLine("[DataMember{0}]".Fmt(dmArgs));

            return true;
        }
    }

    public static class CSharpGeneratorExtensions
    {

    }
}