using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.VbNet
{
    public class VbNetGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;

        public VbNetGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }

        public static Dictionary<string, string> TypeAliases = new Dictionary<string, string>
        {
            {"Int16", "Short"},
            {"Int32", "Integer"},
            {"Int64", "Long"},
            {"DateTime", "Date"},
        };

        public static HashSet<string> KeyWords = new HashSet<string>
        {
            "Default",
            "Dim",
            "Catch",
            "Byte",
            "Char",
            "Short",
            "Integer",
            "Long",
            "UShort",
            "ULong",
            "Double",
            "Decimal",
            "String",
            "Object",
            "Each",
            "Error",
            "Finally",
            "Function",
            "Global",
            "If",
            "Imports",
            "Inherits",
            "Not",
            "IsNot",
            "Module",
            "MyBase",
            "Option",
            "Out",
            "Protected",
            "Return",
            "Shadows",
            "Static",
            "Then",
            "With",
            "When",
            "Operator",
            "Class"
        };

        public static Func<List<MetadataType>, List<MetadataType>> FilterTypes = DefaultFilterTypes;

        public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types) => types;

        public string GetCode(MetadataTypes metadata, IRequest request)
        {
            var namespaces = Config.GetDefaultNamespaces(metadata);

            metadata.RemoveIgnoredTypesForNet(Config);

            if (Config.GlobalNamespace == null)
            {
                metadata.Types.Each(x => namespaces.Add(x.Namespace));
                metadata.Operations.Each(x => namespaces.Add(x.Request.Namespace));
            }
            else
            {
                namespaces.Add(Config.GlobalNamespace);
            }

            Func<string, string> defaultValue = k =>
                request.QueryString[k].IsNullOrEmpty() ? "'''" : "'";

            var sbInner = new StringBuilder();
            var sb = new StringBuilderWrapper(sbInner);
            sb.AppendLine("' Options:");
            sb.AppendLine("'Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("'Version: {0}".Fmt(Env.ServiceStackVersion));
            sb.AppendLine("'Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("''")));
            sb.AppendLine("'BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine("'");
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}MakePartial: {1}".Fmt(defaultValue("MakePartial"), Config.MakePartial));
            sb.AppendLine("{0}MakeVirtual: {1}".Fmt(defaultValue("MakeVirtual"), Config.MakeVirtual));
            sb.AppendLine("{0}MakeDataContractsExtensible: {1}".Fmt(defaultValue("MakeDataContractsExtensible"), Config.MakeDataContractsExtensible));
            sb.AppendLine("{0}AddReturnMarker: {1}".Fmt(defaultValue("AddReturnMarker"), Config.AddReturnMarker));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}AddDataContractAttributes: {1}".Fmt(defaultValue("AddDataContractAttributes"), Config.AddDataContractAttributes));
            sb.AppendLine("{0}AddIndexesToDataMembers: {1}".Fmt(defaultValue("AddIndexesToDataMembers"), Config.AddIndexesToDataMembers));
            sb.AppendLine("{0}AddGeneratedCodeAttributes: {1}".Fmt(defaultValue("AddGeneratedCodeAttributes"), Config.AddGeneratedCodeAttributes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}InitializeCollections: {1}".Fmt(defaultValue("InitializeCollections"), Config.InitializeCollections));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(", ")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(", ")));
            sb.AppendLine("{0}AddNamespaces: {1}".Fmt(defaultValue("AddNamespaces"), Config.AddNamespaces.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));
            sb.AppendLine();

            namespaces.Where(x => !string.IsNullOrEmpty(x))
                .Each(x => sb.AppendLine("Imports {0}".Fmt(x)));
            if (Config.AddGeneratedCodeAttributes)
                sb.AppendLine("Imports System.CodeDom.Compiler");

            if (Config.AddDataContractAttributes
                && Config.AddDefaultXmlNamespace != null)
            {
                sb.AppendLine();

                namespaces.Where(x => !Config.DefaultNamespaces.Contains(x)).ToList()
                    .ForEach(x =>
                        sb.AppendLine("<Assembly: ContractNamespace(\"{0}\", ClrNamespace:=\"{1}\")>"
                            .Fmt(Config.AddDefaultXmlNamespace, x)));
            }

            sb.AppendLine();

            sb.AppendLine("Namespace Global");
            sb = sb.Indent();

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
                .ThenBy(x => x.Name)
                .ToList();

            orderedTypes = FilterTypes(orderedTypes);

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
                sb.AppendLine("End Namespace");

            sb = sb.UnIndent();
            sb.AppendLine("End Namespace");

            sb.AppendLine();

            return StringBuilderCache.ReturnAndFree(sbInner);
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS, List<MetadataType> allTypes, CreateTypeOptions options)
        {
            if (type.IsNested.GetValueOrDefault() && !options.IsNestedType)
                return lastNS;

            var ns = Config.GlobalNamespace ?? type.Namespace;
            if (ns != lastNS)
            {
                if (lastNS != null)
                    sb.AppendLine("End Namespace");

                lastNS = ns;

                sb.AppendLine();
                sb.AppendLine("Namespace {0}".Fmt(ns.SafeToken()));
                //sb.AppendLine("{");
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
            if (Config.AddGeneratedCodeAttributes)
                sb.AppendLine("<GeneratedCode(\"AddServiceStackReference\", \"{0}\")>".Fmt(Env.VersionString));

            if (type.IsEnum.GetValueOrDefault())
            {
                sb.AppendLine("Public Enum {0}".Fmt(Type(type.Name, type.GenericArgs)));
                //sb.AppendLine("{");
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues != null ? type.EnumValues[i] : null;
                        sb.AppendLine(value == null
                            ? "{0}".Fmt(name)
                            : "{0} = {1}".Fmt(name, value));
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine("End Enum");
            }
            else
            {
                var partial = Config.MakePartial && !type.IsInterface() ? "Partial " : "";
                var defType = type.IsInterface() ? "Interface" : "Class";
                sb.AppendLine("Public {0}{1} {2}".Fmt(partial, defType, Type(type.Name, type.GenericArgs)));

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                {
                    sb.AppendLine("    Inherits {0}".Fmt(Type(type.Inherits, includeNested: true)));
                }

                var implements = new List<string>();
                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        implements.Add(implStr);
                    if (!type.Implements.IsEmpty())
                        type.Implements.Each(x => implements.Add(Type(x)));
                }

                var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;
                if (makeExtensible)
                    implements.Add("IExtensibleDataObject");

                if (implements.Count > 0)
                {
                    foreach (var x in implements)
                    {
                        sb.AppendLine("    Implements {0}".Fmt(x));
                    }
                }

                //sb.AppendLine("{");
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
                sb.AppendLine(type.IsInterface() ? "End Interface" : "End Class");
            }

            sb = sb.UnIndent();
            return lastNS;
        }

        private void AddConstuctor(StringBuilderWrapper sb, MetadataType type, CreateTypeOptions options)
        {
            if (type.IsInterface())
                return;
            var initCollections = feature.ShouldInitializeCollections(type, Config.InitializeCollections);
            if (Config.AddImplicitVersion == null && !initCollections)
                return;

            var collectionProps = new List<MetadataPropertyType>();
            if (type.Properties != null && initCollections)
                collectionProps = type.Properties.Where(x => x.IsCollection()).ToList();

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
            if (!addVersionInfo && collectionProps.Count <= 0) return;

            if (addVersionInfo)
            {
                var @virtual = Config.MakeVirtual ? "Overridable " : "";
                sb.AppendLine("Public {0}Property Version As Integer".Fmt(@virtual));
                sb.AppendLine();
            }

            sb.AppendLine("Public Sub New()".Fmt(NameOnly(type.Name)));
            //sb.AppendLine("{");
            sb = sb.Indent();

            if (addVersionInfo)
                sb.AppendLine("Version = {0}".Fmt(Config.AddImplicitVersion));

            foreach (var prop in collectionProps)
            {
                var suffix = prop.IsArray() ? "{}" : "";
                sb.AppendLine("{0} = New {1}{2}".Fmt(
                prop.Name.SafeToken(),
                Type(prop.Type, prop.GenericArgs, includeNested:true),
                suffix));
            }

            sb = sb.UnIndent();
            sb.AppendLine("End Sub");
            sb.AppendLine();
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
        {
            var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;

            var @virtual = Config.MakeVirtual && !type.IsInterface() ? "Overridable " : "";
            var wasAdded = false;

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = Type(prop.Type, prop.GenericArgs, includeNested:true);
                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    var visibility = type.IsInterface() ? "" : "Public ";
                    sb.AppendLine("{0}{1}Property {2} As {3}".Fmt(
                        visibility,
                        @virtual,
                        EscapeKeyword(prop.Name).SafeToken(), 
                        propType));
                }
            }

            if (type.IsInterface())
                return;

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                wasAdded = AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("Public {0}Property ResponseStatus As ResponseStatus".Fmt(@virtual));
            }

            if (makeExtensible
                && (type.Properties == null
                    || type.Properties.All(x => x.Name != "ExtensionData")))
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                sb.AppendLine("Public {0}Property ExtensionData As ExtensionDataObject Implements IExtensibleDataObject.ExtensionData".Fmt(@virtual));
            }
        }

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            foreach (var attr in attributes)
            {
                var attrName = EscapeKeyword(attr.Name);

                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine("<{0}>".Fmt(attrName));
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
                            args.Append("{0}".Fmt(TypeValue(ctorArg.Type, ctorArg.Value)));
                        }
                    }
                    else if (attr.Args != null)
                    {
                        foreach (var attrArg in attr.Args)
                        {
                            if (args.Length > 0)
                                args.Append(", ");
                            args.Append("{0}:={1}".Fmt(attrArg.Name, TypeValue(attrArg.Type, attrArg.Value)));
                        }
                    }
                    sb.AppendLine("<{0}({1})>".Fmt(attrName, StringBuilderCacheAlt.ReturnAndFree(args)));
                }
            }

            return true;
        }

        public string TypeValue(string type, string value)
        {
            var alias = TypeAlias(type);
            if (value == null)
                return "Nothing";
            if (alias == "String")
                return value.QuotedSafeValue();

            if (value.StartsWith("typeof("))
            {
                //Only emit type as Namespaces are merged
                var typeNameOnly = value.Substring(7, value.Length - 8).LastRightPart('.');
                return "GetType(" + typeNameOnly + ")";
            }

            return value;
        }

        public string Type(MetadataTypeName typeName, bool includeNested = false)
        {
            return Type(typeName.Name, typeName.GenericArgs, includeNested: includeNested);
        }

        public string Type(string type, string[] genericArgs, bool includeNested = false)
        {
            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return "Nullable(Of {0})".Fmt(TypeAlias(genericArgs[0], includeNested: includeNested));

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = StringBuilderCacheAlt.Allocate();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg.TrimStart('\''), includeNested: includeNested));
                    }

                    var typeName = NameOnly(type, includeNested: includeNested).SanitizeType();
                    return "{0}(Of {1})".Fmt(typeName, StringBuilderCacheAlt.ReturnAndFree(args));
                }
            }

            return TypeAlias(type, includeNested: includeNested);
        }

        private string TypeAlias(string type, bool includeNested = false)
        {
            type = type.SanitizeType();
            if (type.Contains("<"))
            {
                type = type.Replace("<", "(Of ").Replace(">", ")");
            }

            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return "{0}()".Fmt(TypeAlias(arrParts[0], includeNested: includeNested));

            string typeAlias;
            TypeAliases.TryGetValue(type, out typeAlias);

            return typeAlias ?? NameOnly(type, includeNested: includeNested);
        }

        public string NameOnly(string type, bool includeNested = false)
        {
            var name = type.LeftPart('`');

            if (!includeNested)
                name = name.LastRightPart('.');

            return name.SafeToken();
        }

        public string EscapeKeyword(string name)
        {
            return KeyWords.Contains(name) ? "[{0}]".Fmt(name) : name;
        }

        public bool AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc == null) return false;

            if (Config.AddDescriptionAsComments)
            {
                sb.AppendLine("'''<Summary>");
                sb.AppendLine("'''{0}".Fmt(desc.SafeComment()));
                sb.AppendLine("'''</Summary>");
                return false;
            }
            else
            {
                sb.AppendLine("<Description({0})>".Fmt(desc.QuotedSafeValue()));
                return true;
            }
        }

        public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
        {
            if (dcMeta == null)
            {
                if (Config.AddDataContractAttributes)
                    sb.AppendLine("<DataContract>");
                return;
            }

            var dcArgs = "";
            if (dcMeta.Name != null || dcMeta.Namespace != null)
            {
                if (dcMeta.Name != null)
                    dcArgs = "Name:={0}".Fmt(dcMeta.Name.QuotedSafeValue());

                if (dcMeta.Namespace != null)
                {
                    if (dcArgs.Length > 0)
                        dcArgs += ", ";

                    dcArgs += "Namespace:={0}".Fmt(dcMeta.Namespace.QuotedSafeValue());
                }

                dcArgs = "({0})".Fmt(dcArgs);
            }
            sb.AppendLine("<DataContract{0}>".Fmt(dcArgs));
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                                  ? "<DataMember(Order:={0})>".Fmt(dataMemberIndex)
                                  : "<DataMember>");
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
                    dmArgs = "Name:={0}".Fmt(dmMeta.Name.QuotedSafeValue());

                if (dmMeta.Order != null || Config.AddIndexesToDataMembers)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += "Order:={0}".Fmt(dmMeta.Order ?? dataMemberIndex);
                }

                if (dmMeta.IsRequired != null)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += "IsRequired:={0}".Fmt(dmMeta.IsRequired.ToString().ToLower());
                }

                if (dmMeta.EmitDefaultValue != null)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += "EmitDefaultValue:={0}".Fmt(dmMeta.EmitDefaultValue.ToString().ToLower());
                }

                dmArgs = "({0})".Fmt(dmArgs);
            }
            sb.AppendLine("<DataMember{0}>".Fmt(dmArgs));

            return true;
        }
    }

    public static class VbNetGeneratorExtensions
    {
        public static string SafeComment(this string comment)
        {
            return comment.Replace("\r", "").Replace("\n", "");
        }

        public static string SafeToken(this string token)
        {
            var t = token.Replace("Of ", ""); // remove Of from token so [space] character will work 
            if (t.ContainsAny("\"", "-", "+", "\\", "*", "=", "!"))
                throw new InvalidDataException("MetaData is potentially malicious. Expected token, Received: {0}".Fmt(token));

            return token;
        }

        public static string SafeValue(this string value)
        {
            if (value.Contains('"'))
                throw new InvalidDataException("MetaData is potentially malicious. Expected scalar value, Received: {0}".Fmt(value));

            return value;
        }

        public static string QuotedSafeValue(this string value)
        {
            return "\"{0}\"".Fmt(value.SafeValue());
        }

        public static MetadataAttribute ToMetadataAttribute(this MetadataRoute route)
        {
            var attr = new MetadataAttribute
            {
                Name = "Route",
                ConstructorArgs = new List<MetadataPropertyType>
                {
                    new MetadataPropertyType { Type = "String", Value = route.Path },
                },
            };

            if (route.Verbs != null)
            {
                attr.ConstructorArgs.Add(
                    new MetadataPropertyType { Type = "String", Value = route.Verbs });
            }

            return attr;
        }
    }
}