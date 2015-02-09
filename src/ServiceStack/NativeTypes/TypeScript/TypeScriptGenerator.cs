using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes.TypeScript
{
    public class TypeScriptGenerator
    {
        readonly MetadataTypesConfig Config;
        List<string> conflictTypeNames = new List<string>();

        public TypeScriptGenerator(MetadataTypesConfig config)
        {
            Config = config;
        }

        class CreateTypeOptions
        {
            public Func<string> ImplementsFn { get; set; }
            public bool IsRequest { get; set; }
            public bool IsResponse { get; set; }
            public bool IsOperation { get { return IsRequest || IsResponse; } }
            public bool IsType { get; set; }
        }

        public string GetCode(MetadataTypes metadata, IRequest request, INativeTypesMetadata nativeTypes)
        {
            var namespaces = new HashSet<string>();
            Config.DefaultNamespaces.Each(x => namespaces.Add(x));

            var typeNamespaces = new HashSet<string>();
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
            sb.AppendLine("/* Options:");
            sb.AppendLine("Date: {0}".Fmt(DateTime.Now.ToString("s").Replace("T", " ")));
            sb.AppendLine("Version: {0}".Fmt(metadata.Version));
            sb.AppendLine("BaseUrl: {0}".Fmt(Config.BaseUrl));
            sb.AppendLine();
            sb.AppendLine("{0}GlobalNamespace: {1}".Fmt(defaultValue("GlobalNamespace"), Config.GlobalNamespace));
            sb.AppendLine("{0}AddServiceStackTypes: {1}".Fmt(defaultValue("AddServiceStackTypes"), Config.AddServiceStackTypes));
            sb.AppendLine("{0}AddResponseStatus: {1}".Fmt(defaultValue("AddResponseStatus"), Config.AddResponseStatus));
            sb.AppendLine("{0}AddImplicitVersion: {1}".Fmt(defaultValue("AddImplicitVersion"), Config.AddImplicitVersion));

            sb.AppendLine("*/");
            sb.AppendLine();

            string lastNS = null;

            var existingOps = new HashSet<string>();

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

            //TypeScript doesn't support reusing same type name with different generic airity
            var conflictPartialNames = allTypes.Map(x => x.Name).Distinct()
                .GroupBy(g => g.SplitOnFirst('`')[0])
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            this.conflictTypeNames = allTypes
                .Where(x => conflictPartialNames.Any(name => x.Name.StartsWith(name)))
                .Map(x => x.Name);

            sb.AppendLine("declare module {0}".Fmt(globalNamespace.SafeToken()));
            sb.AppendLine("{");

            //ServiceStack core interfaces
            foreach (var type in allTypes)
            {
                var fullTypeName = type.GetFullName();
                if (requestTypes.Contains(type))
                {
                    if (!existingOps.Contains(fullTypeName))
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
                                        ? Type("IReturn`1", new[] { Type(type.Name, type.GenericArgs) })
                                        : null;
                                },
                                IsRequest = true,
                            });

                        existingOps.Add(fullTypeName);
                    }
                }
                else if (responseTypes.Contains(type))
                {
                    if (!existingOps.Contains(fullTypeName)
                        && !Config.IgnoreTypesInNamespaces.Contains(type.Namespace))
                    {
                        lastNS = AppendType(ref sb, type, lastNS,
                            new CreateTypeOptions
                            {
                                IsResponse = true,
                            });

                        existingOps.Add(fullTypeName);
                    }
                }
                else if (types.Contains(type) && !existingOps.Contains(fullTypeName))
                {
                    lastNS = AppendType(ref sb, type, lastNS,
                        new CreateTypeOptions { IsType = true });
                }
            }

            sb.AppendLine();
            sb.AppendLine("}");

            return sb.ToString();
        }

        private string AppendType(ref StringBuilderWrapper sb, MetadataType type, string lastNS,
            CreateTypeOptions options)
        {
            if (type == null || (type.Namespace != null && type.Namespace.StartsWith("System")))
                return lastNS;

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
                sb.AppendLine("enum {0}".Fmt(Type(type.Name, type.GenericArgs)));
                sb.AppendLine("{");
                sb = sb.Indent();

                if (type.EnumNames != null)
                {
                    for (var i = 0; i < type.EnumNames.Count; i++)
                    {
                        var name = type.EnumNames[i];
                        var value = type.EnumValues != null ? type.EnumValues[i] : null;
                        sb.AppendLine(value == null
                            ? "{0},".Fmt(name.PropertyStyle())
                            : "{0} = {1},".Fmt(name.PropertyStyle(), value));
                    }
                }

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }
            else
            {
                var extends = new List<string>();

                //: BaseClass, Interfaces
                if (type.Inherits != null)
                    extends.Add(Type(type.Inherits).InheritedType());

                if (options.ImplementsFn != null)
                {
                    var implStr = options.ImplementsFn();
                    if (!string.IsNullOrEmpty(implStr))
                        extends.Add(implStr);
                }

                var extend = extends.Count > 0
                    ? " extends " + (string.Join(", ", extends.ToArray()))
                    : "";

                sb.AppendLine("interface {0}{1}".Fmt(Type(type.Name, type.GenericArgs), extend));
                sb.AppendLine("{");

                sb = sb.Indent();

                var addVersionInfo = Config.AddImplicitVersion != null && options.IsOperation;
                if (addVersionInfo)
                {
                    sb.AppendLine("{0}:number; //{1}".Fmt("Version".PropertyStyle(), Config.AddImplicitVersion));
                }

                AddProperties(sb, type);

                sb = sb.UnIndent();
                sb.AppendLine("}");
            }

            sb = sb.UnIndent();

            return lastNS;
        }

        public void AddProperties(StringBuilderWrapper sb, MetadataType type)
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
                    wasAdded = AppendDataMember(sb, prop.DataMember, dataMemberIndex++);
                    wasAdded = AppendAttributes(sb, prop.Attributes) || wasAdded;
                    sb.AppendLine("{1}{2}:{0};".Fmt(propType, prop.Name.SafeToken().PropertyStyle(), optional));
                }
            }

            if (Config.AddResponseStatus
                && (type.Properties == null
                    || type.Properties.All(x => x.Name != "ResponseStatus")))
            {
                if (wasAdded) sb.AppendLine();

                AppendDataMember(sb, null, dataMemberIndex++);
                sb.AppendLine("{0}:ResponseStatus;".Fmt("ResponseStatus".PropertyStyle()));
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
                    sb.AppendLine("// @{0}({1})".Fmt(attr.Name, args));
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
            IDictionary<string, string> d;
            if (genericArgs != null)
            {
                if (type == "Nullable`1")
                    return "{0}?".Fmt(TypeAlias(genericArgs[0].GenericArg()));
                if (ArrayTypes.Contains(type))
                    return "{0}[]".Fmt(TypeAlias(genericArgs[0].GenericArg()));
                if (DictionaryTypes.Contains(type))
                    return "{{ [index:{0}]: {1}; }}".Fmt(TypeAlias(genericArgs[0].GenericArg()), TypeAlias(genericArgs[1].TrimStart('\'')));

                var parts = type.Split('`');
                if (parts.Length > 1)
                {
                    var args = new StringBuilder();
                    foreach (var arg in genericArgs)
                    {
                        if (args.Length > 0)
                            args.Append(", ");

                        args.Append(TypeAlias(arg.GenericArg()));
                    }

                    var typeName = TypeAlias(type);
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
            Config.TypeScriptTypeAlias.TryGetValue(type, out typeAlias);

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
    }

    public static class TypeScriptGeneratorExtensions
    {
        public static string GenericArg(this string arg)
        {
            return arg.TrimStart('\'');
        }

        //TypeScript doesn't support short-hand T[] notation in extension list
        public static string InheritedType(this string type)
        {
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