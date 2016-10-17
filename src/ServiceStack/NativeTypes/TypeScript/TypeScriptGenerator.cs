using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.TypeScript
{
    public class TypeScriptGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly NativeTypesFeature feature;
        List<string> conflictTypeNames = new List<string>();

        public TypeScriptGenerator(MetadataTypesConfig config)
        {
            Config = config;
            feature = HostContext.GetPlugin<NativeTypesFeature>();
        }

        public static List<string> DefaultImports = new List<string>
        {
        };

        public static Dictionary<string, string> TypeAliases = new Dictionary<string, string>
        {
            {"String", "string"},
            {"Boolean", "boolean"},
            {"DateTime", "string"},
            {"DateTimeOffset", "string"},
            {"TimeSpan", "string"},
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
            {"List", "Array"},
        };
        private static string declaredEmptyString = "\"\"";
        private static Dictionary<string, string> primitiveDefaultValues = new Dictionary<string, string>
        {
            {"String", declaredEmptyString},
            {"string", declaredEmptyString},
            {"Boolean", "false"},
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
            {"number", "0"},
            {"List", "[]"}
        };

        public static Func<List<MetadataType>, List<MetadataType>> FilterTypes = DefaultFilterTypes;

        public static List<MetadataType> DefaultFilterTypes(List<MetadataType> types)
        {
            return types.OrderTypesByDeps();
        }

        private void AddTypeToSortedList(List<MetadataType> allTypes, List<MetadataType> sortedTypes, MetadataType metadataType)
        {
            if (sortedTypes.Contains(metadataType))
                return;

            if (metadataType == null)
                return;

            if (metadataType.Inherits == null)
            {
                sortedTypes.Add(metadataType);
                return;
            }

            var inheritedMetadataType = FindMetadataTypeByMetadataTypeName(allTypes, metadataType.Inherits);
            // Find and add base class first
            AddTypeToSortedList(allTypes,sortedTypes, inheritedMetadataType);

            if (!sortedTypes.Contains(metadataType))
                sortedTypes.Add(metadataType);
        }

        private MetadataType FindMetadataTypeByMetadataTypeName(List<MetadataType> allTypes,
            MetadataTypeName metadataTypeName)
        {
            if (metadataTypeName == null)
                return null;
            var metaDataType = allTypes.Where(x => x.Name == metadataTypeName.Name &&
                                                   x.Namespace == metadataTypeName.Namespace)
                .FirstNonDefault();
            return metaDataType;
        }

        private List<MetadataType> CreateSortedTypeList(List<MetadataType> allTypes)
        {
            List<MetadataType> result = new List<MetadataType>();
            foreach (var metadataType in allTypes)
            {
                AddTypeToSortedList(allTypes,result,metadataType);
            }
            return result;
        }

        public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
        {
            var typeNamespaces = new HashSet<string>();
            metadata.RemoveIgnoredTypes(Config);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

            var defaultImports = !Config.DefaultImports.IsEmpty()
                ? Config.DefaultImports
                : DefaultImports;

            var globalNamespace = Config.GlobalNamespace;

            Func<string, string> defaultValue = k =>
                request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sbInner = StringBuilderCache.Allocate();
            var sb = new StringBuilderWrapper(sbInner);
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.ServiceStackVersion));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            //sb.AppendLine("{0}ExportAsTypes: {1}".Fmt(defaultValue("ExportAsTypes"), Config.ExportAsTypes));
            sb.AppendLine("{0}MakePropertiesOptional: {1}".Fmt(defaultValue("MakePropertiesOptional"), Config.MakePropertiesOptional));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}AddDescriptionAsComments: {1}".Fmt(defaultValue("AddDescriptionAsComments"), Config.AddDescriptionAsComments));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));

            sb.AppendLine("*/");
            sb.AppendLine();

            string lastNS = null;

            var existingTypes = new HashSet<string>();

            var requestTypes = metadata.Operations.Select(x => x.Request).ToHashSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToHashSet();

            // Base Types need to be written first
            var types = CreateSortedTypeList(metadata.Types);

            var allTypes = new List<MetadataType>();
            allTypes.AddRange(types);
            allTypes.AddRange(responseTypes);
            allTypes.AddRange(requestTypes);
            allTypes.RemoveAll(x => x.IgnoreType(Config));

            allTypes = FilterTypes(allTypes);

            //TypeScript doesn't support reusing same type name with different generic airity
            var conflictPartialNames = allTypes.Map(x => x.Name).Distinct()
                .GroupBy(g => g.LeftPart('`'))
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            this.conflictTypeNames = allTypes
                .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
                .Map(x => x.Name);

            defaultImports.Each(x => sb.AppendLine("import {0};".Fmt(x)));

            if (!string.IsNullOrEmpty(globalNamespace))
            {
                var moduleDef = Config.ExportAsTypes ? "" : "declare ";
                sb.AppendLine();
                sb.AppendLine("{0}module {1}".Fmt(moduleDef, globalNamespace.SafeToken()));
                sb.AppendLine("{");

                sb = sb.Indent();
            }

            //ServiceStack core interfaces
            foreach (var type in allTypes)
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
                                        return Type("IReturn`1", new[] { Type(type.ReturnMarkerTypeName).InDeclarationType() });
                                    return response != null
                                        ? Type("IReturn`1", new[] { Type(response.Name, response.GenericArgs).InDeclarationType() })
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

            if (!string.IsNullOrEmpty(globalNamespace))
            {
                sb = sb.UnIndent();
                sb.AppendLine();
                sb.AppendLine("}");
            }

            return StringBuilderCache.ReturnAndFree(sbInner);
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
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
                if (type.IsEnumInt.GetValueOrDefault() || type.EnumNames.IsEmpty())
                {
                    var typeDeclaration = !Config.ExportAsTypes
                        ? "enum"
                        : "export enum";

                    sb.AppendLine("{0} {1}".Fmt(typeDeclaration, Type(type.Name, type.GenericArgs)));
                    sb.AppendLine("{");
                    sb = sb.Indent();

                    if (type.EnumNames != null)
                    {
                        for (var i = 0; i < type.EnumNames.Count; i++)
                        {
                            var name = type.EnumNames[i];
                            var value = type.EnumValues != null ? type.EnumValues[i] : null;

                            sb.AppendLine(value == null //Enum Value's are not impacted by JS Style
                                ? "{0},".Fmt(name)
                                : "{0} = {1},".Fmt(name, value));
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

                    sbType.Append("{0} {1} = ".Fmt(typeDeclaration, Type(type.Name, type.GenericArgs)));

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
                    extends.Add(Type(type.Inherits).InDeclarationType());

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

                        if (returnType == "any")
                            returnType = "Object";

                        // This is to avoid invalid syntax such as "return new string()"
                        string replaceReturnType;
                        if (primitiveDefaultValues.TryGetValue(returnType, out replaceReturnType))
                            returnType = replaceReturnType;

                        responseTypeExpression = replaceReturnType == null ?
                            "createResponse() {{ return new {0}(); }}".Fmt(returnType) :
                            "createResponse() {{ return {0}; }}".Fmt(returnType);
                    }
                    else if (implStr == "IReturnVoid")
                    {
                        responseTypeExpression = "createResponse() {}";
                    }
                }

                var isClass = Config.ExportAsTypes && !type.IsInterface.GetValueOrDefault();
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

                sb.AppendLine("{0} {1}{2}".Fmt(typeDeclaration, Type(type.Name, type.GenericArgs), extend));
                sb.AppendLine("{");

                sb = sb.Indent();

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
                if (addVersionInfo)
                {
                    sb.AppendLine("{0}{1}: number; //{2}".Fmt(
                        "Version".PropertyStyle(), isClass ? "" : "?", Config.AddImplicitVersion));
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != typeof(ResponseStatus).Name));

                if (Config.ExportAsTypes && responseTypeExpression != null)
                {
                    sb.AppendLine(responseTypeExpression);
                    sb.AppendLine("getTypeName() {{ return \"{0}\"; }}".Fmt(type.Name));
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }

            return lastNS;
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
        {
            var wasAdded = false;

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = Type(prop.Type, prop.GenericArgs);
                    var optional = "";
                    if (propType.EndsWith("?"))
                    {
                        propType = propType.Substring(0, propType.Length - 1);
                        optional = "?";
                    }

                    if (Config.MakePropertiesOptional)
                        optional = "?";

                    if (prop.Attributes.Safe().FirstOrDefault(x => x.Name == "Required") != null)
                        optional = "";

                    if (Config.ExportAsTypes && !type.IsInterface.GetValueOrDefault())
                        optional = "";

                    wasAdded = AppendComments(sb, prop.Description);
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++) || wasAdded;
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    sb.AppendLine("{1}{2}: {0};".Fmt(propType, prop.Name.SafeToken().PropertyStyle(), optional));
                }
            }

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("{0}{1}: ResponseStatus;".Fmt(
                    typeof(ResponseStatus).Name.PropertyStyle(), Config.ExportAsTypes ? "" : "?"));
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
                return value.QuotedSafeValue();

            if (value.StartsWith("typeof("))
            {
                //Only emit type as Namespaces are merged
                var typeNameOnly = value.Substring(7, value.Length - 8).LastRightPart('.');
                return "typeof(" + typeNameOnly + ")";
            }

            return value;
        }

        public string Type(MetadataTypeName typeName)
        {
            return Type(typeName.Name, typeName.GenericArgs);
        }

        public static HashSet<string> ArrayTypes = new HashSet<string>
        {
            "List`1",
            "IEnumerable`1",
            "ICollection`1",
            "HashSet`1",
            "Queue`1",
            "Stack`1",
            "IEnumerable",
        };

        public static HashSet<string> DictionaryTypes = new HashSet<string>
        {
            "Dictionary`2",
            "IDictionary`2",
            "IOrderedDictionary`2",
            "OrderedDictionary",
            "StringDictionary",
            "IDictionary",
            "IOrderedDictionary",
        };

        public string Type(string type, string[] genericArgs)
        {
            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return "{0}?".Fmt(GenericArg(genericArgs[0]));
                if (ArrayTypes.Contains(type))
                    return "{0}[]".Fmt(GenericArg(genericArgs[0]));
                if (DictionaryTypes.Contains(type))
                    return "{{ [index:{0}]: {1}; }}".Fmt(
                        GenericArg(genericArgs[0]),
                        GenericArg(genericArgs[1]));

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
                    return "{0}<{1}>".Fmt(typeName, StringBuilderCacheAlt.ReturnAndFree(args));
                }
            }

            return TypeAlias(type);
        }

        private string TypeAlias(string type)
        {
            type = type.SanitizeType();
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return "{0}[]".Fmt(TypeAlias(arrParts[0]));

            string typeAlias;
            TypeAliases.TryGetValue(type, out typeAlias);

            return typeAlias ?? NameOnly(type);
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

            if (node.Text == "List")
            {
                sb.Append(ConvertFromCSharp(node.Children[0]));
                sb.Append("[]");
            }
            else if (node.Text == "Dictionary")
            {
                sb.Append("{ [index:");
                sb.Append(ConvertFromCSharp(node.Children[0]));
                sb.Append("]: ");
                sb.Append(ConvertFromCSharp(node.Children[1]));
                sb.Append("; }");
            }
            else
            {
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
    }

    public static class TypeScriptGeneratorExtensions
    {
        public static string InDeclarationType(this string type)
        {
            //TypeScript doesn't support short-hand Dictionary notation or has a Generic Dictionary Type
            if (type.StartsWith("{"))
                return "any";

            //TypeScript doesn't support short-hand T[] notation in extension list
            var arrParts = type.SplitOnFirst('[');
            return arrParts.Length > 1 
                ? "Array<{0}>".Fmt(arrParts[0]) 
                : type;
        }

        public static string PropertyStyle(this string name)
        {
            return JsConfig.EmitCamelCaseNames
                ? name.ToCamelCase()
                : JsConfig.EmitLowercaseUnderscoreNames
                    ? name.ToLowercaseUnderscore()
                    : name;
        }
    }
}
