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
    public class VbNetGenerator : ILangGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        private List<MetadataType> allTypes;

        public VbNetGenerator(MetadataTypesConfig config)
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
            {"Int16", "Short"},
            {"Int32", "Integer"},
            {"Int64", "Long"},
            {"DateTime", "Date"},
        };

        public static HashSet<string> KeyWords = new() {
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
            "Is",
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
            "Class",
            "Date",
            "End",
            "end",
            "True",
            "False",
            "Mod",
        };

        public static TypeFilterDelegate TypeFilter { get; set; }
        
        public static Func<VbNetGenerator, MetadataType, MetadataPropertyType, string> PropertyTypeFilter { get; set; }

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

            string defaultValue(string k) => request.QueryString[k].IsNullOrEmpty() ? "'''" : "'";

            var sbInner = new StringBuilder();
            var sb = new StringBuilderWrapper(sbInner);
            var includeOptions = !WithoutOptions && request.QueryString[nameof(WithoutOptions)] == null;
if (includeOptions)
            {
                sb.AppendLine("' Options:");
                sb.AppendLine("'Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
                sb.AppendLine("'Version: {0}".Fmt(Env.VersionString));
                sb.AppendLine("'Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("''")));
                sb.AppendLine("'BaseUrl: {0}".Fmt(Config.BaseUrl));
                if (Config.UsePath != null)
                    sb.AppendLine("'UsePath: {0}".Fmt(Config.UsePath));

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
                sb.AppendLine("{0}ExportValueTypes: {1}".Fmt(defaultValue("ExportValueTypes"), Config.ExportValueTypes));
                sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(", ")));
                sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(", ")));
                sb.AppendLine("{0}AddNamespaces: {1}".Fmt(defaultValue("AddNamespaces"), Config.AddNamespaces.Safe().ToArray().Join(",")));
                sb.AppendLine("{0}AddDefaultXmlNamespace: {1}".Fmt(defaultValue("AddDefaultXmlNamespace"), Config.AddDefaultXmlNamespace));
                AddQueryParamOptions.Each(name => sb.AppendLine($"{defaultValue(name)}{name}: {request.QueryString[name]}"));
                sb.AppendLine();
            }

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

            var orderedTypes = allTypes
                .OrderBy(x => x.Namespace)
                .ThenBy(x => x.Name)
                .ToList();

            orderedTypes = FilterTypes(orderedTypes);

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

            if (lastNS != null)
                sb.AppendLine("End Namespace");

            var addCode = AddCodeFilter?.Invoke(allTypes, Config);
            if (addCode != null)
                sb.AppendLine(addCode);
            
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
                sb.AppendLine($"Namespace {MetadataExtensions.SafeToken(ns)}");
                //sb.AppendLine("{");
            }

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
                sb.AppendLine($"<GeneratedCode(\"AddServiceStackReference\", \"{Env.VersionString}\")>");

            sb.Emit(type, Lang.Vb);
            PreTypeFilter?.Invoke(sb, type);

            if (type.IsEnum.GetValueOrDefault())
            {
                sb.AppendLine("Public Enum {0}".Fmt(Type(type.Name, type.GenericArgs)));
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues?[i];
                        if (KeyWords.Contains(name))
                            name = $"[{name}]";
                        
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
                            ? $"{name}"
                            : $"{name} = {value}");
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
                    sb.AppendLine($"    Inherits {Type(type.Inherits, includeNested: true)}");
                }

                var implements = new List<string>();
                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        implements.Add(implStr);
                }

                type.Implements.Each(x => implements.Add(Type(x)));

                var makeExtensible = Config.MakeDataContractsExtensible && type.Inherits == null;
                if (makeExtensible)
                    implements.Add("IExtensibleDataObject");

                if (implements.Count > 0)
                {
                    foreach (var x in implements)
                    {
                        sb.AppendLine($"    Implements {x}");
                    }
                }

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
                sb.AppendLine(type.IsInterface() ? "End Interface" : "End Class");
            }

            PostTypeFilter?.Invoke(sb, type);
            
            sb = sb.UnIndent();
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
                sb.AppendLine($"{GetPropertyName(prop.Name)} = New {Type(prop.Type, prop.GenericArgs,true)}{suffix}");
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

                    var propType = GetPropertyType(prop);
                    propType = PropertyTypeFilter?.Invoke(this, type, prop) ?? propType;
                    
                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    var visibility = type.IsInterface() ? "" : "Public ";

                    sb.Emit(prop, Lang.Vb);
                    PrePropertyFilter?.Invoke(sb, prop, type);
                    sb.AppendLine("{0}{1}Property {2} As {3}".Fmt(
                        visibility,
                        @virtual,
                        GetPropertyName(prop.Name), 
                        propType));
                    PostPropertyFilter?.Invoke(sb, prop, type);
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

        public virtual string GetPropertyType(MetadataPropertyType prop)
        {
            var propType = Type(prop.GetTypeName(Config, allTypes), prop.GenericArgs, includeNested: true);
            return propType;
        }

        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            foreach (var attr in attributes)
            {
                var attrName = GetPropertyName(attr.Name);

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
                            args.Append(TypeValue(ctorArg.Type, ctorArg.Value));
                        }
                    }
                    else if (attr.Args != null)
                    {
                        foreach (var attrArg in attr.Args)
                        {
                            if (args.Length > 0)
                                args.Append(", ");
                            args.Append($"{attrArg.Name}:={TypeValue(attrArg.Type, attrArg.Value)}");
                        }
                    }
                    sb.AppendLine($"<{attrName}({StringBuilderCacheAlt.ReturnAndFree(args)})>");
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
                    return "Integer.MinValue";
                if (value == int.MaxValue.ToString())
                    return "Integer.MaxValue";
            }
            if (type == nameof(Int64))
            {
                if (value == long.MinValue.ToString())
                    return "Long.MinValue";
                if (value == long.MaxValue.ToString())
                    return "Long.MaxValue";
            }
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
            var useType = TypeFilter?.Invoke(type, genericArgs);
            if (useType != null)
                return useType;

            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return $"Nullable(Of {TypeAlias(genericArgs[0], includeNested: includeNested)})";

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
                    return $"{typeName}(Of {StringBuilderCacheAlt.ReturnAndFree(args)})";
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
                return $"{TypeAlias(arrParts[0], includeNested: includeNested)}()";

            TypeAliases.TryGetValue(type, out var typeAlias);

            return typeAlias ?? NameOnly(type, includeNested: includeNested);
        }

        public string NameOnly(string type, bool includeNested = false)
        {
            var name = type.LeftPart('`');

            if (!includeNested)
                name = name.LastRightPart('.');

            return name.SafeToken();
        }

        public string EscapeKeyword(string name) => KeyWords.Contains(name) ? $"[{name}]" : name;

        public string GetPropertyName(string name) => EscapeKeyword(name).SafeToken();

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
                    dcArgs = $"Name:={MetadataExtensions.QuotedSafeValue(dcMeta.Name)}";

                if (dcMeta.Namespace != null)
                {
                    if (dcArgs.Length > 0)
                        dcArgs += ", ";

                    dcArgs += $"Namespace:={MetadataExtensions.QuotedSafeValue(dcMeta.Namespace)}";
                }

                dcArgs = $"({dcArgs})";
            }
            sb.AppendLine($"<DataContract{dcArgs}>");
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                        ? $"<DataMember(Order:={dataMemberIndex})>"
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
                    dmArgs = $"Name:={MetadataExtensions.QuotedSafeValue(dmMeta.Name)}";

                if (dmMeta.Order != null || Config.AddIndexesToDataMembers)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += $"Order:={dmMeta.Order ?? dataMemberIndex}";
                }

                if (dmMeta.IsRequired != null)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += $"IsRequired:={dmMeta.IsRequired.ToString().ToLower()}";
                }

                if (dmMeta.EmitDefaultValue != null)
                {
                    if (dmArgs.Length > 0)
                        dmArgs += ", ";

                    dmArgs += $"EmitDefaultValue:={dmMeta.EmitDefaultValue.ToString().ToLower()}";
                }

                dmArgs = $"({dmArgs})";
            }
            sb.AppendLine($"<DataMember{dmArgs}>");

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

        public static string QuotedSafeValue(this string value) => $"\"{value.SafeValue()}\"";

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