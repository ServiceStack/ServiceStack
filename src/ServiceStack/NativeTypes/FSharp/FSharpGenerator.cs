using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.FSharp
{
    public class FSharpGenerator
    {
        readonly MetadataTypesConfig Config;

        public FSharpGenerator(MetadataTypesConfig config)
        {
            Config = config;
        }

        public static Dictionary<string, string> TypeAliases = new Dictionary<string, string> 
        {
        };

        public string GetCode(MetadataTypes metadata, IRequest request)
        {
            var namespaces = Config.GetDefaultNamespaces(metadata);

            var typeNamespaces = new HashSet<string>();
            metadata.RemoveIgnoredTypes(Config);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

            // Look first for shortest Namespace ending with `ServiceModel` convention, else shortest ns
            var globalNamespace = Config.GlobalNamespace
                ?? typeNamespaces.Where(x => x.EndsWith("ServiceModel"))
                    .OrderBy(x => x).FirstOrDefault()
                ?? typeNamespaces.OrderBy(x => x).First();

            Func<string, string> defaultValue = k =>
                request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sb = new StringBuilderWrapper(new StringBuilder());
            sb.AppendLine("(* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(metadata.Version));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
            sb.AppendLine("{0}AddReturnMarker: {1}".Fmt(defaultValue("AddReturnMarker"), Config.AddReturnMarker));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}AddDataContractAttributes: {1}".Fmt(defaultValue("AddDataContractAttributes"), Config.AddDataContractAttributes));
            sb.AppendLine("{0}AddIndexesToDataMembers: {1}".Fmt(defaultValue("AddIndexesToDataMembers"), Config.AddIndexesToDataMembers));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            //sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));
            //sb.AppendLine("{0}DefaultNamespaces: {1}".Fmt(defaultValue("DefaultNamespaces"), Config.DefaultNamespaces.ToArray().Join(", ")));
            sb.AppendLine("*)");
            sb.AppendLine();

            //if (Config.AddDataContractAttributes
            //    && Config.AddDefaultXmlNamespace != null)
            //{
            //    sb.AppendLine();

            //    var list = namespaces.Where(x => !Config.DefaultNamespaces.Contains(x)).ToList();
            //    list.ForEach(x =>
            //        sb.AppendLine("[<assembly: ContractNamespace(\"{0}\", ClrNamespace=\"{1}\")>]"
            //            .Fmt(Config.AddDefaultXmlNamespace, x)));

            //    if (list.Count > 0)
            //    {
            //        sb.AppendLine("do()"); //http://scottseely.com/2009/01/23/f-assembly-level-attributes-assemblyinfo-fs-and-do/
            //    }
            //}
            //sb.AppendLine();

            string lastNS = null;

            var existingTypes = new HashSet<string>();

            var requestTypes = metadata.Operations.Select(x => x.Request).ToHashSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToHashSet();
            var types = metadata.Types.ToHashSet();

            var allTypes = new List<MetadataType>();
            allTypes.AddRange(types);
            allTypes.AddRange(responseTypes);
            allTypes.AddRange(requestTypes);

            var orderedTypes = allTypes.OrderTypesByDeps();

            sb.AppendLine("namespace {0}".Fmt(globalNamespace.SafeToken()));
            sb.AppendLine();
            foreach (var ns in namespaces)
            {
                sb.AppendLine("open " + ns);
            }

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

                        lastNS = AppendType(ref sb, type, lastNS,
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

            sb.AppendLine();

            return sb.ToString();
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
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
                sb.AppendLine("type {0} =".Fmt(Type(type.Name, type.GenericArgs)));
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues != null ? type.EnumValues[i] : i.ToString();
                        sb.AppendLine("| {0} = {1}".Fmt(name, value));
                    }
                }

                sb = sb.UnIndent();
            }
            else
            {
                //sb.AppendLine("[<CLIMutable>]"); // only for Record Types
                var classCtor = type.IsInterface() ? "" : "()";
                sb.AppendLine("[<AllowNullLiteral>]");
                sb.AppendLine("type {0}{1} = ".Fmt(Type(type.Name, type.GenericArgs), classCtor));
                sb = sb.Indent();
                var startLen = sb.Length;

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                    sb.AppendLine("inherit {0}()".Fmt(Type(type.Inherits)));

                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        sb.AppendLine("interface {0}".Fmt(implStr));
                }

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
                        sb.AppendLine("member val Version:int = {0} with get, set".Fmt(Config.AddImplicitVersion));
                    }
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != typeof(ResponseStatus).Name));

                if (sb.Length == startLen)
                    sb.AppendLine(type.IsInterface() ? "interface end" : "class end");

                sb = sb.UnIndent();
            }

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

                    var propType = Type(prop.Type, prop.GenericArgs);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                    if (!type.IsInterface())
                    {
                        sb.AppendLine("member val {1}:{0} = {2} with get,set".Fmt(
                            propType, prop.Name.SafeToken(), GetDefaultLiteral(prop)));
                    }
                    else
                    {
                        sb.AppendLine("abstract {1}:{0} with get,set".Fmt(
                            propType, prop.Name.SafeToken()));                        
                    }
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

        private string GetDefaultLiteral(MetadataPropertyType prop)
        {
            var propType = Type(prop.Type, prop.GenericArgs);
            if (Config.InitializeCollections && prop.IsCollection())
            {
                return prop.IsArray()
                    ? "[||]" 
                    : "new {0}()".Fmt(propType);
            }
            return prop.IsValueType.GetValueOrDefault()
                ? "new {0}()".Fmt(propType)
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
                    sb.AppendLine("[<{0}>]".Fmt(attr.Name));
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
                    sb.AppendLine("[<{0}({1})>]".Fmt(attr.Name, args));
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
            if (genericArgs != null)
            {
                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = new StringBuilder();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg));
                    }

                    var typeName = NameOnly(type);
                    return "{0}<{1}>".Fmt(typeName, args);
                }
            }

            return TypeAlias(type);
        }

        private string TypeAlias(string type)
        {
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return "{0}[]".Fmt(TypeAlias(arrParts[0]));

            string typeAlias;
            TypeAliases.TryGetValue(type, out typeAlias);

            return typeAlias ?? NameOnly(type);
        }

        public string NameOnly(string type)
        {
            return type.SplitOnFirst('`')[0].SplitOnLast('.').Last().SafeToken();
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
                sb.AppendLine("[<Description({0})>]".Fmt(desc.QuotedSafeValue()));
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
                    dcArgs = "Name={0}".Fmt(dcMeta.Name.QuotedSafeValue());

                if (dcMeta.Namespace != null)
                {
                    if (dcArgs.Length > 0)
                        dcArgs += ", ";

                    dcArgs += "Namespace={0}".Fmt(dcMeta.Namespace.QuotedSafeValue());
                }

                dcArgs = "({0})".Fmt(dcArgs);
            }
            sb.AppendLine("[<DataContract{0}>]".Fmt(dcArgs));
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                                  ? "[<DataMember(Order={0})>]".Fmt(dataMemberIndex)
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
            sb.AppendLine("[<DataMember{0}>]".Fmt(dmArgs));

            return true;
        }
    }

    public static class FSharpGeneratorExtensions
    {
        public static void Push(this Dictionary<string, List<string>> map, string key, string value)
        {
            List<string> results;
            if (!map.TryGetValue(key, out results))
                map[key] = results = new List<string>();

            if (!results.Contains(value))
                results.Add(value);
        }

        public static bool Contains(this Dictionary<string, List<string>> map, string key, string value)
        {
            List<string> results;
            return map.TryGetValue(key, out results) && results.Contains(value);
        }

        public static List<string> GetValues(this Dictionary<string, List<string>> map, string key)
        {
            List<string> results;
            map.TryGetValue(key, out results);            
            return results ?? new List<string>();
        }

        public static List<MetadataType> OrderTypesByDeps(this List<MetadataType> types)
        {
            var deps = new Dictionary<string, List<string>>();

            foreach (var type in types)
            {
                var typeName = type.Name;

                if (type.ReturnMarkerTypeName != null)
                {
                    if (!type.ReturnMarkerTypeName.GenericArgs.IsEmpty())
                        type.ReturnMarkerTypeName.GenericArgs.Each(x => deps.Push(typeName, x));
                    else
                        deps.Push(typeName, type.ReturnMarkerTypeName.Name);
                }
                if (type.Inherits != null)
                {
                    if (!type.Inherits.GenericArgs.IsEmpty())
                        type.Inherits.GenericArgs.Each(x => deps.Push(typeName, x));
                    else
                        deps.Push(typeName, type.Inherits.Name);
                }
                foreach (var p in type.Properties.Safe())
                {
                    if (!p.GenericArgs.IsEmpty())
                        p.GenericArgs.Each(x => deps.Push(typeName, x));
                    else
                        deps.Push(typeName, p.Type);
                }
            }

            var typesMap = types.ToSafeDictionary(x => x.Name);
            var considered = new HashSet<string>();
            var to = new List<MetadataType>();

            foreach (var type in types)
            {
                foreach (var depType in GetDepTypes(deps, typesMap, considered, type))
                {
                    if (!to.Contains(depType))
                        to.Add(depType);
                }

                if (!to.Contains(type))
                    to.Add(type);

                considered.Add(type.Name);
            }

            return to;
        }

        public static IEnumerable<MetadataType> GetDepTypes(
            Dictionary<string, List<string>> deps,
            Dictionary<string, MetadataType> typesMap, 
            HashSet<string> considered, 
            MetadataType type)
        {
            if (type == null) yield break;

            var typeDeps = deps.GetValues(type.Name);
            foreach (var typeDep in typeDeps)
            {
                MetadataType depType;
                if (!typesMap.TryGetValue(typeDep, out depType)
                    || considered.Contains(typeDep))
                    continue;

                considered.Add(typeDep);

                foreach (var childDepType in GetDepTypes(deps, typesMap, considered, depType))
                {
                    yield return childDepType;
                }

                yield return depType;
            }
        }
    }

}