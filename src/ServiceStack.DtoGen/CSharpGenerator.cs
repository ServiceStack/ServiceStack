using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceStack.DtoGen
{
    public static class CSharpGenerator
    {
        private const int Version = 1;

        public static MetadataTypesConfig Config;

        class CreateTypeOptions
        {
            public Func<string> ImplementsFn { get; set; }
            public bool IsRequest { get; set; }
            public bool IsResponse { get; set; }
            public bool IsOperation { get { return IsRequest || IsResponse; } }
            public bool IsType { get; set; }
        }

        public static string GetCode(MetadataTypes metadata, string defaultBaseUrl)
        {
            Config = metadata.Config;

            var namespaces = new HashSet<string>();
            Config.DefaultNamespaces.ForEach(x => namespaces.Add(x));
            metadata.Types.ForEach(x => namespaces.Add(x.Namespace));
            metadata.Operations.ForEach(x => namespaces.Add(x.Request.Namespace));

            const bool flag = false;
            Config.AddReturnMarker = flag;
            Config.MakeVirtual = Config.MakePartial = flag;
            Config.AddDescriptionAsComments = flag;
            Config.AddDefaultXmlNamespace = "http://schemas.ubixar.com/types";
            Config.AddDataContractAttributes = flag;
            Config.MakeDataContractsExtensible = flag;
            Config.AddIndexesToDataMembers = flag;
            Config.InitializeCollections = flag;
            Config.AddResponseStatus = flag;
            Config.AddImplicitVersion = null;

            var sb = new StringBuilderWrapper(new StringBuilder());
            sb.AppendLine("/* Options:");
            sb.AppendLine("Version: {0}".Fmt(Version));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl ?? defaultBaseUrl));
            sb.AppendLine();
            sb.AppendLine("ServerVersion: {0}".Fmt(metadata.Version));
            sb.AppendLine("MakePartial: {0}".Fmt(Config.MakePartial));
            sb.AppendLine("MakeVirtual: {0}".Fmt(Config.MakeVirtual));
            sb.AppendLine("AddReturnMarker: {0}".Fmt(Config.AddReturnMarker));
            sb.AppendLine("AddDescriptionAsComments: {0}".Fmt(Config.AddDescriptionAsComments));
            sb.AppendLine("AddDataContractAttributes: {0}".Fmt(Config.AddDataContractAttributes));
            sb.AppendLine("AddDefaultXmlNamespace: {0}".Fmt(Config.AddDefaultXmlNamespace));
            sb.AppendLine("MakeDataContractsExtensible: {0}".Fmt(Config.MakeDataContractsExtensible));
            sb.AppendLine("AddIndexesToDataMembers: {0}".Fmt(Config.AddIndexesToDataMembers));
            sb.AppendLine("InitializeCollections: {0}".Fmt(Config.InitializeCollections));
            sb.AppendLine("AddResponseStatus: {0}".Fmt(Config.AddResponseStatus));
            sb.AppendLine("AddImplicitVersion: {0}".Fmt(Config.AddImplicitVersion));
            sb.AppendLine("DefaultNamespaces: {0}".Fmt(Config.DefaultNamespaces.ToArray().Join(", ")));
            sb.AppendLine("*/");
            sb.AppendLine();

            namespaces.ToList().ForEach(x => sb.AppendLine("using {0};".Fmt(x)));

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

            sb.AppendLine("#region Operations");
            sb.AppendLine();
            foreach (var operation in metadata.Operations
                .OrderBy(x => x.Request.Namespace)
                .OrderBy(x => x.Request.Name))
            {
                var request = operation.Request;
                var response = operation.Response;
                lastNS = AppendType(ref sb, request, lastNS,
                    new CreateTypeOptions {
                        ImplementsFn = () => {
                            if (!Config.AddReturnMarker
                                && !request.ReturnVoidMarker
                                && request.ReturnMarkerGenericArgs == null)
                                return null;

                            if (request.ReturnVoidMarker)
                                return "IReturnVoid";
                            if (request.ReturnMarkerGenericArgs != null)
                                return Type("IReturn`1", request.ReturnMarkerGenericArgs);
                            return response != null
                                ? Type("IReturn`1", new[] { response.Name })
                                : null;
                        },
                        IsRequest = true,
                    });
                lastNS = AppendType(ref sb, operation.Response, lastNS,
                    new CreateTypeOptions {
                        IsResponse = true,
                    });
            }
            if (lastNS != null)
                sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#endregion");

            sb.AppendLine();
            sb.AppendLine();

            lastNS = null;
            sb.AppendLine("#region Types");
            sb.AppendLine();
            foreach (var type in metadata.Types
                .OrderBy(x => x.Namespace)
                .OrderBy(x => x.Name))
            {
                lastNS = AppendType(ref sb, type, lastNS, 
                    new CreateTypeOptions { IsType = true });
            }
            if (lastNS != null)
                sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#endregion");

            return sb.ToString();
        }

        private static string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
            if (type == null || (type.Namespace != null && type.Namespace.StartsWith("System")))
                return lastNS;

            if (type.Namespace != lastNS)
            {
                if (lastNS != null)
                    sb.AppendLine("}");

                lastNS = type.Namespace;

                sb.AppendLine();
                sb.AppendLine("namespace {0}".Fmt(type.Namespace.SafeToken()));
                sb.AppendLine("{");
            }

            sb = sb.Indent();

            sb.AppendLine();
            sb.AppendComments(type.Description);
            sb.AppendDataContract(type.DataContract);

            var partial = Config.MakePartial ? "partial " : "";
            sb.AppendLine("public {0}class {1}".Fmt(partial, type.Name.SafeToken()));

            //: BaseClass, Interfaces
            var inheritsList = new List<string>();
            if (type.Inherits != null)
                inheritsList.Add(Type(type.Inherits, type.InheritsGenericArgs));
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

            sb.AddConstuctor(type, options);
            sb.AddProperties(type);

            sb = sb.UnIndent();
            sb.AppendLine("}");

            sb = sb.UnIndent();
            return lastNS;
        }

        private static void AddConstuctor(this StringBuilderWrapper sb, MetadataType type, CreateTypeOptions options)
        {
            if (Config.AddImplicitVersion == null && !Config.InitializeCollections) 
                return;

            var collectionProps = new List<MetadataPropertyType>();
            if (type.Properties != null && Config.InitializeCollections)
                collectionProps = type.Properties.Where(IsCollection).ToList();

            var addVersionInfo = Config.AddImplicitVersion != null && options.IsOperation;
            if (!addVersionInfo && collectionProps.Count <= 0) return;

            if (addVersionInfo)
            {
                var @virtual = Config.MakeVirtual ? "virtual " : "";
                sb.AppendLine("public {0}int Version {{ get; set; }}".Fmt(@virtual));
                sb.AppendLine();
            }

            sb.AppendLine("public {0}()".Fmt(type.Name.SafeToken()));
            sb.AppendLine("{");
            sb = sb.Indent();

            if (addVersionInfo)
                sb.AppendLine("Version = {0};".Fmt(Config.AddImplicitVersion));

            foreach (var prop in collectionProps)
            {
                sb.AppendLine("{0} = new {1}{{}};".Fmt(
                prop.Name.SafeToken(),
                Type(prop.Type, prop.GenericArgs)));
            }

            sb = sb.UnIndent();
            sb.AppendLine("}");
            sb.AppendLine();
        }

        public static HashSet<string> CollectionTypes = new HashSet<string> {
            "List`1",
            "HashSet`1",
            "Dictionary`2",
            "Queue`1",
            "Stack`1",
        };

        public static bool IsCollection(MetadataPropertyType prop)
        {
            return CollectionTypes.Contains(prop.Type)
                || prop.Type.SplitOnFirst('[').Length > 1;
        }

        public static void AddProperties(this StringBuilderWrapper sb, MetadataType type)
        {
            var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;

            var @virtual = Config.MakeVirtual ? "virtual " : "";
            var wasAdded = false;

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = Type(prop.Type, prop.GenericArgs);
                    wasAdded = sb.AppendDataMember(prop.DataMember, dataMemberIndex++);
                    wasAdded = sb.AppendAttributes(prop.Attributes) || wasAdded;
                    sb.AppendLine("public {0}{1} {2} {{ get; set; }}".Fmt(@virtual, propType, prop.Name.SafeToken()));
                }
            }

            if (Config.AddResponseStatus
                && (type.Properties == null
                    || !type.Properties.Any(x => x.Name == "ResponseStatus")))
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                sb.AppendDataMember(null, dataMemberIndex++);
                sb.AppendLine("public {0}ResponseStatus ResponseStatus {{ get; set; }}".Fmt(@virtual));
            }

            if (makeExtensible
                && (type.Properties == null
                    || !type.Properties.Any(x => x.Name == "ExtensionData")))
            {
                if (wasAdded) sb.AppendLine();
                wasAdded = true;

                sb.AppendLine("public {0}ExtensionDataObject ExtensionData {{ get; set; }}".Fmt(@virtual));
            }
        }

        public static bool AppendAttributes(this StringBuilderWrapper sb, List<MetadataAttribute> attributes)
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
                    if (attr.Args != null)
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

        public static string TypeValue(string type, string value)
        {
            var alias = TypeAlias(type);
            if (alias == "string")
                return value.QuotedSafeValue();
            return value;
        }

        public static string Type(string type, string[] genericArgs)
        {
            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return "{0}?".Fmt(TypeAlias(genericArgs[0]));

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var typeName = parts[0];
                    var args = new StringBuilder();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg));
                    }

                    return "{0}<{1}>".Fmt(typeName.SafeToken(), args);
                }
            }

            return TypeAlias(type);
        }

        private static string TypeAlias(string type)
        {
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return "{0}[]".Fmt(TypeAlias(arrParts[0]));

            if (type == "String")
                return "string";
            if (type == "Byte")
                return "byte";
            if (type == "Int16")
                return "short";
            if (type == "Int32")
                return "int";
            if (type == "Int64")
                return "long";
            if (type == "UShort")
                return "ushort";
            if (type == "UInt32")
                return "uint";
            if (type == "UInt64")
                return "ulong";
            if (type == "Single")
                return "float";
            if (type == "Double")
                return "double";
            if (type == "Decimal")
                return "decimal";

            return type.SafeToken();
        }

        public static void AppendComments(this StringBuilderWrapper sb, string desc)
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

        public static void AppendDataContract(this StringBuilderWrapper sb, MetadataDataContract dcMeta)
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

        public static bool AppendDataMember(this StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
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

        public static string SafeComment(this string comment)
        {
            return comment.Replace("\r", "").Replace("\n", "");
        }

        public static string SafeToken(this string token)
        {
            if (token.ContainsAny("\"", " ", "-", "+", "\\", "*", "=", "!"))
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
    }
}