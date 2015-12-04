using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Host;

namespace ServiceStack.NativeTypes.Kotlin
{
    public class KotlinGenerator
    {
        readonly MetadataTypesConfig Config;
        readonly List<MetadataType> AllTypes;
        List<string> conflictTypeNames = new List<string>();

        public KotlinGenerator(MetadataTypesConfig config)
        {
            Config = config;
            AllTypes = new List<MetadataType>();
        }

        public static List<string> DefaultImports = new List<string>
        {
            /* built-in types used
            "java.math.BigInteger",
            "java.math.BigDecimal",
            "java.util.Date",
            "java.util.ArrayList",
            "java.util.HashMap",
            */

            "java.math.*",
            "java.util.*",
            "net.servicestack.client.*",
        };

        public static string GSonAnnotationsNamespace = "com.google.gson.annotations.*";
        public static string GSonReflectNamespace = "com.google.gson.reflect.*";

        public static bool AddGsonImport
        {
            set
            {
                //Used by @SerializedName() annotation, but requires Android dep
                DefaultImports.Add(GSonAnnotationsNamespace);
                //Used by TypeToken<T>
                DefaultImports.Add(GSonReflectNamespace);
            }
        }

        //http://java.interoperabilitybridges.com/articles/data-types-interoperability-between-net-and-java#h4Section2
        public static ConcurrentDictionary<string, string> TypeAliases = new Dictionary<string, string>
        {
            {"String", "String"},
            {"Boolean", "Boolean"},
            {"Char", "String"},
            {"SByte", "Byte"},
            {"Byte", "Short"},
            {"Int16", "Short"},
            {"Int32", "Int"},
            {"Int64", "Long"},
            {"UInt16", "Int"},
            {"UInt32", "Long"},
            {"UInt64", "BigInteger"},
            {"Single", "Float"},
            {"Double", "Double"},
            {"Decimal", "BigDecimal"},
            {"Guid", "UUID"},
            {"DateTime", "Date"},
            {"DateTimeOffset", "Date"},
            {"TimeSpan", "TimeSpan"},
            {"Type", "Class"},
            {"List", "ArrayList"},
            {"Dictionary", "HashMap"},
        }.ToConcurrentDictionary();

        public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
        {
            var typeNamespaces = new HashSet<string>();
            RemoveIgnoredTypes(metadata);
            metadata.Types.Each(x => typeNamespaces.Add(x.Namespace));
            metadata.Operations.Each(x => typeNamespaces.Add(x.Request.Namespace));

            var defaultImports = new List<string>(DefaultImports);
            if (!Config.DefaultImports.IsEmpty())
            {
                defaultImports = Config.DefaultImports;
            }
            else if (ReferencesGson(metadata))
            {
                if (!defaultImports.Contains(GSonAnnotationsNamespace))
                    defaultImports.Add(GSonAnnotationsNamespace);
                if (!defaultImports.Contains(GSonReflectNamespace))
                    defaultImports.Add(GSonReflectNamespace);
            }

            Func<string, string> defaultValue = k =>
                request.QueryString[k].IsNullOrEmpty() ? "//" : "";

            var sb = new StringBuilderWrapper(new StringBuilder());
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(Env.ServiceStackVersion));
            sb.AppendLine("Tip: {0}".Fmt(HelpMessages.NativeTypesDtoOptionsTip.Fmt("//")));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}Package: {1}".Fmt(defaultValue("Package"), Config.Package));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));
            sb.AppendLine("{0}IncludeTypes: {1}".Fmt(defaultValue("IncludeTypes"), Config.IncludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}ExcludeTypes: {1}".Fmt(defaultValue("ExcludeTypes"), Config.ExcludeTypes.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}TreatTypesAsStrings: {1}".Fmt(defaultValue("TreatTypesAsStrings"), Config.TreatTypesAsStrings.Safe().ToArray().Join(",")));
            sb.AppendLine("{0}DefaultImports: {1}".Fmt(defaultValue("DefaultImports"), defaultImports.Join(",")));

            sb.AppendLine("*/");
            sb.AppendLine();

            foreach (var typeName in Config.TreatTypesAsStrings.Safe())
            {
                TypeAliases[typeName] = "String";
            }

            if (Config.Package != null)
            {
                sb.AppendLine("package {0}".Fmt(Config.Package));
                sb.AppendLine();
            }

            string lastNS = null;

            var existingTypes = new HashSet<string>();

            var requestTypes = metadata.Operations.Select(x => x.Request).ToHashSet();
            var requestTypesMap = metadata.Operations.ToSafeDictionary(x => x.Request);
            var responseTypes = metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response).ToHashSet();
            var types = metadata.Types.ToHashSet();

            AllTypes.AddRange(requestTypes);
            AllTypes.AddRange(responseTypes);
            AllTypes.AddRange(types);

            //TypeScript doesn't support reusing same type name with different generic airity
            var conflictPartialNames = AllTypes.Map(x => x.Name).Distinct()
                .GroupBy(g => g.SplitOnFirst('`')[0])
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            this.conflictTypeNames = AllTypes
                .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
                .Map(x => x.Name);

            defaultImports.Each(x => sb.AppendLine("import {0}".Fmt(x)));
            sb.AppendLine();

            //ServiceStack core interfaces
            foreach (var type in AllTypes)
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


            return sb.ToString();
        }

        private bool ReferencesGson(MetadataTypes metadata)
        {
            var allTypes = GetAllMetadataTypes(metadata);
            return allTypes.Any(x => KotlinGeneratorExtensions.KotlinKeyWords.Contains(x.Name)
                || x.Properties.Safe().Any(p => p.DataMember != null && p.DataMember.Name != null)
                || (x.ReturnMarkerTypeName != null && x.ReturnMarkerTypeName.Name.IndexOf('`') >= 0)); //uses TypeToken<T>
        }

        private static List<MetadataType> GetAllMetadataTypes(MetadataTypes metadata)
        {
            var allTypes = new List<MetadataType>();
            allTypes.AddRange(metadata.Types);
            allTypes.AddRange(metadata.Operations.Where(x => x.Request != null).Select(x => x.Request));
            allTypes.AddRange(metadata.Operations.Where(x => x.Response != null).Select(x => x.Request));
            return allTypes;
        }

        //Use built-in types already in net.servicestack.client package
        public static HashSet<string> IgnoreTypeNames = new HashSet<string>
        {
            typeof(ResponseStatus).Name,
            typeof(ResponseError).Name,
            typeof(ErrorResponse).Name,
        }; 

        private void RemoveIgnoredTypes(MetadataTypes metadata)
        {
            metadata.RemoveIgnoredTypes(Config);
            metadata.Types.RemoveAll(x => IgnoreTypeNames.Contains(x.Name));
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

            var typeName = Type(type.Name, type.GenericArgs);

            if (type.IsEnum.GetValueOrDefault())
            {
                var hasIntValue = type.EnumNames.Count == (type.EnumValues != null ? type.EnumValues.Count : 0);
                var enumConstructor = hasIntValue ? "(val value:Int)" : "";

                sb.AppendLine("enum class {0}{1}".Fmt(typeName, enumConstructor));
                sb.AppendLine("{");
                sb = sb.Indent();


                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = hasIntValue ? type.EnumValues[i] : null;

                        var serializeAs = JsConfig.TreatEnumAsInteger || (type.Attributes.Safe().Any(x => x.Name == "Flags"))
                            ? "@SerializedName(\"{0}\") ".Fmt(value)
                            : "";

                        sb.AppendLine(value == null
                            ? "{0},".Fmt(name.ToPascalCase())
                            : serializeAs + "{0}({1}),".Fmt(name.ToPascalCase(), value));
                    }

                    //if (hasIntValue)
                    //{
                    //    sb.AppendLine();
                    //    sb.AppendLine("private final int value;");
                    //    sb.AppendLine("{0}(final int intValue) {{ value = intValue; }}".Fmt(typeName));
                    //    sb.AppendLine("public int getValue() { return value; }");
                    //}
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }
            else
            {
                var defType = type.IsInterface()
                    ? "interface"
                    : "class";
                var extends = new List<string>();

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                    extends.Add(Type(type.Inherits).InheritedType());

                string responseTypeExpression = null;

                var interfaces = new List<string>();
                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                    {
                        interfaces.Add(implStr);

                        if (implStr.StartsWith("IReturn<"))
                        {
                            var parts = implStr.SplitOnFirst('<');
                            var returnType = parts[1].Substring(0, parts[1].Length - 1);

                            //Can't get .class from Generic Type definition
                            responseTypeExpression = returnType.Contains("<")
                                ? "object : TypeToken<{0}>(){{}}.type".Fmt(returnType)
                                : "{0}::class.java".Fmt(returnType);
                        }
                    }
                    if (!type.Implements.IsEmpty())
                    {
                        foreach (var interfaceRef in type.Implements)
                        {
                            interfaces.Add(Type(interfaceRef));
                        }
                    }
                }

                var extend = extends.Count > 0 
                    ? " : " + extends[0] + "()"
                    : "";

                if (interfaces.Count > 0)
                    extend += (extend.IsNullOrEmpty() ? " : " : ", ") + string.Join(", ", interfaces.ToArray());

                sb.AppendLine("open {0} {1}{2}".Fmt(defType, typeName, extend));
                sb.AppendLine("{");

                sb = sb.Indent();

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsRequest;
                if (addVersionInfo)
                {
                    sb.AppendLine("val {0}:Int = {1}".Fmt("Version".PropertyStyle(), Config.AddImplicitVersion));
                }

                AddProperties(sb, type,
                    includeResponseStatus: Config.AddResponseStatus && options.IsResponse
                        && type.Properties.Safe().All(x => x.Name != typeof(ResponseStatus).Name));

                if (responseTypeExpression != null)
                {
                    sb.AppendLine("companion object {{ private val responseType = {0} }}".Fmt(responseTypeExpression));
                    sb.AppendLine("override fun getResponseType(): Any? = responseType");
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }

            return lastNS;
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type, bool includeResponseStatus)
        {
            var wasAdded = false;

            var defaultValue = type.IsInterface != true ? " = null" : "";

            var dataMemberIndex = 1;
            if (type.Properties != null)
            {
                foreach (var prop in type.Properties)
                {
                    if (wasAdded) sb.AppendLine();

                    var propType = Type(prop.Type, prop.GenericArgs);

                    var fieldName = prop.Name.SafeToken().PropertyStyle();

                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;

                    if (!fieldName.IsKeyWord())
                    {
                        sb.AppendLine("var {0}:{1}?{2}".Fmt(fieldName, propType, defaultValue));
                    }
                    else
                    {
                        var originalName = fieldName;
                        fieldName = char.ToUpper(fieldName[0]) + fieldName.SafeSubstring(1);
                        sb.AppendLine("@SerializedName(\"{0}\") var {1}:{2}?{3}".Fmt(originalName, fieldName, propType, defaultValue));
                    }
                }
            }

            if (includeResponseStatus)
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("var {0}:ResponseStatus{1}".Fmt(typeof(ResponseStatus).Name.PropertyStyle(), defaultValue));
            }
        }
        
        public bool AppendAttributes(StringBuilderWrapper sb, List<MetadataAttribute> attributes)
        {
            if (attributes == null || attributes.Count == 0) return false;

            var existingAttrs = new HashSet<string>();

            foreach (var attr in attributes)
            {
                //Java 7 doesn't allow repeating attrs 
                var prefix = existingAttrs.Contains(attr.Name)
                    ? "// "
                    : "";
                existingAttrs.Add(attr.Name);

                if ((attr.Args == null || attr.Args.Count == 0)
                    && (attr.ConstructorArgs == null || attr.ConstructorArgs.Count == 0))
                {
                    sb.AppendLine(prefix + "@{0}()".Fmt(attr.Name));
                }
                else
                {
                    var args = new StringBuilder();
                    if (attr.ConstructorArgs != null)
                    {
                        if (attr.ConstructorArgs.Count > 1)
                            prefix = "// ";

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
                    sb.AppendLine(prefix + "@{0}({1})".Fmt(attr.Name, args));
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
                return typeNameOnly + ".class";
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
                    return /*@Nullable*/ "{0}".Fmt(GenericArg(genericArgs[0]));
                if (ArrayTypes.Contains(type))
                    return "ArrayList<{0}>".Fmt(GenericArg(genericArgs[0]));
                if (DictionaryTypes.Contains(type))
                    return "HashMap<{0},{1}>".Fmt(
                        GenericArg(genericArgs[0]),
                        GenericArg(genericArgs[1]));

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = new StringBuilder();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(GenericArg(arg));
                    }

                    var typeName = TypeAlias(type);
                    return "{0}<{1}>".Fmt(typeName, args);
                }
            }

            return TypeAlias(type);
        }

        private string TypeAlias(string type)
        {
            type = type.SanitizeType();
            var arrParts = type.SplitOnFirst('[');
            if (arrParts.Length > 1)
                return "ArrayList<{0}>".Fmt(TypeAlias(arrParts[0]));

            string typeAlias;
            TypeAliases.TryGetValue(type, out typeAlias);

            return typeAlias ?? NameOnly(type);
        }

        public string NameOnly(string type)
        {
            var name = conflictTypeNames.Contains(type)
                ? type.Replace('`','_')
                : type.SplitOnFirst('`')[0];

            return name.SplitOnLast('.').Last().SafeToken();
        }

        public void AppendComments(StringBuilderWrapper sb, string desc)
        {
            if (desc == null) return;

            sb.AppendLine("/**");
            sb.AppendLine("* {0}".Fmt(desc.SafeComment()));
            sb.AppendLine("*/");
        }

        public void AppendDataContract(StringBuilderWrapper sb, MetadataDataContract dcMeta)
        {
            if (dcMeta == null)
            {
                if (Config.AddDataContractAttributes)
                    sb.AppendLine("@DataContract()");
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
            sb.AppendLine("@DataContract{0}".Fmt(dcArgs));
        }

        public bool AppendDataMember(StringBuilderWrapper sb, MetadataDataMember dmMeta, int dataMemberIndex)
        {
            if (dmMeta == null)
            {
                if (Config.AddDataContractAttributes)
                {
                    sb.AppendLine(Config.AddIndexesToDataMembers
                                  ? "@DataMember(Order={0})".Fmt(dataMemberIndex)
                                  : "@DataMember()");
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
            sb.AppendLine("@DataMember{0}".Fmt(dmArgs));

            if (dmMeta.Name != null)
            {
                sb.AppendLine("@SerializedName(\"{0}\")".Fmt(dmMeta.Name));
            }

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
                sb.Append("ArrayList<");
                sb.Append(ConvertFromCSharp(node.Children[0]));
                sb.Append(">");
            }
            else if (node.Text == "Dictionary")
            {
                sb.Append("HashMap<");
                sb.Append(ConvertFromCSharp(node.Children[0]));
                sb.Append(",");
                sb.Append(ConvertFromCSharp(node.Children[1]));
                sb.Append(">");
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

            var typeName = sb.ToString();
            return typeName.SplitOnLast('.').Last(); //remove nested class
        }
    }

    public static class KotlinGeneratorExtensions
    {
        public static string InheritedType(this string type)
        {
            return type;
        }

        public static HashSet<string> KotlinKeyWords = new HashSet<string>
        {
            //Java Keywords
            "abstract",
            "assert",
            "boolean",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "class",
            "continue",
            "default",
            "do",
            "double",
            "else",
            "enum",
            "extends",
            "final",
            "finally",
            "float",
            "for",
            "if",
            "implements",
            "import",
            "instanceof",
            "int",
            "interface",
            "long",
            "native",
            "new",
            "package",
            "private",
            "protected",
            "public",
            "return",
            "short",
            "static",
            "strictfp",
            "super",
            "switch",
            "synchronized",
            "this",
            "throw",
            "throws",
            "transient",
            "try",
            "void",
            "volatile",
            "while",

            //Kotlin extra Keywords https://bitbucket.org/birkenfeld/pygments-main/pull-requests/505/update-list-of-kotlin-keywords-according/diff
            "annotation",
            "as",
            "by",
            "companion",
            "const",
            "constructor",
            "crossinline",
            "data",
            "dynamic",
            "external",
            "fun",
            "get",
            "in",
            "inner",
            "internal",
            "in",
            "infix",
            "inline",
            "inner",
            "is",
            "lateinit",
            "noinline",
            "object",
            "open",
            "operator",
            "out",
            "reified",
            "set",
            "sealed",
            "tailrec",
            "trait",
            "type",
            "val",
            "var",
            "vararg",
            "when",
            "where",
        };
        
        public static bool IsKeyWord(this string name)
        {
            return KotlinKeyWords.Contains(name);
        }

        public static string PropertyStyle(this string name)
        {
            //Gson is case-sensitive, fieldName needs to match json
            var fieldName = JsConfig.EmitCamelCaseNames
                ? name.ToCamelCase()
                : JsConfig.EmitLowercaseUnderscoreNames
                    ? name.ToLowercaseUnderscore()
                    : name;

            return fieldName;
        }

        public static MetadataAttribute ToMetadataAttribute(this MetadataRoute route)
        {
            if (route.Verbs != null)
            {
                return new MetadataAttribute
                {
                    Name = "Route",
                    Args = new List<MetadataPropertyType> {
                        new MetadataPropertyType { Name = "Path", Type = "string", Value = route.Path },
                        new MetadataPropertyType { Name = "Verbs", Type = "string", Value = route.Verbs },
                    },
                };
            }

            return new MetadataAttribute
            {
                Name = "Route",
                ConstructorArgs = new List<MetadataPropertyType>
                {
                    new MetadataPropertyType { Type = "string", Value = route.Path },
                },
            };
        }

        public static StringBuilderWrapper AppendPropertyAccessor(this StringBuilderWrapper sb, string type, string fieldName, string settersReturnThis)
        {
            return sb.AppendPropertyAccessor(type, fieldName.PropertyStyle(), fieldName.ToPascalCase(), settersReturnThis);
        }

        public static StringBuilderWrapper AppendPropertyAccessor(this StringBuilderWrapper sb, string type, string fieldName, string accessorName, string settersReturnThis)
        {
            var getter = type.StartsWithIgnoreCase("bool") && !accessorName.StartsWithIgnoreCase("is") 
                ? "is" 
                : "get";
            sb.AppendLine("public {0} {3}{1}() {{ return {2}; }}".Fmt(type, accessorName, fieldName, getter));
            if (settersReturnThis != null)
            {
                sb.AppendLine("public {3} set{1}({0} value) {{ this.{2} = value; return this; }}".Fmt(type, accessorName, fieldName, settersReturnThis));
            }
            else
            {
                sb.AppendLine("public void set{1}({0} value) {{ this.{2} = value; }}".Fmt(type, accessorName, fieldName));
            }
            return sb;
        }
    }
}