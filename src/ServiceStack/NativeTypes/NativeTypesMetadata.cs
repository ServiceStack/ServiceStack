using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes
{
    public class NativeTypesMetadata : INativeTypesMetadata
    {
        private readonly ServiceMetadata meta;
        private readonly MetadataTypesConfig defaults;

        public NativeTypesMetadata(ServiceMetadata meta, MetadataTypesConfig defaults)
        {
            this.meta = meta;
            this.defaults = defaults;
        }

        public MetadataTypesConfig GetConfig(NativeTypesBase req)
        {
            return new MetadataTypesConfig
            {
                BaseUrl = req.BaseUrl ?? defaults.BaseUrl,
                MakePartial = req.MakePartial ?? defaults.MakePartial,
                MakeVirtual = req.MakeVirtual ?? defaults.MakeVirtual,
                MakeInternal = req.MakeInternal ?? defaults.MakeInternal,
                AddReturnMarker = req.AddReturnMarker ?? defaults.AddReturnMarker,
                AddDescriptionAsComments = req.AddDescriptionAsComments ?? defaults.AddDescriptionAsComments,
                AddDataContractAttributes = req.AddDataContractAttributes ?? defaults.AddDataContractAttributes,
                MakeDataContractsExtensible = req.MakeDataContractsExtensible ?? defaults.MakeDataContractsExtensible,
                AddIndexesToDataMembers = req.AddIndexesToDataMembers ?? defaults.AddIndexesToDataMembers,
                AddGeneratedCodeAttributes = req.AddGeneratedCodeAttributes ?? defaults.AddGeneratedCodeAttributes,
                InitializeCollections = req.InitializeCollections ?? defaults.InitializeCollections,
                AddImplicitVersion = req.AddImplicitVersion ?? defaults.AddImplicitVersion,
                BaseClass = req.BaseClass ?? defaults.BaseClass,
                Package = req.Package ?? defaults.Package,
                AddResponseStatus = req.AddResponseStatus ?? defaults.AddResponseStatus,
                AddServiceStackTypes = req.AddServiceStackTypes ?? defaults.AddServiceStackTypes,
                AddModelExtensions = req.AddModelExtensions ?? defaults.AddModelExtensions,
                AddPropertyAccessors = req.AddPropertyAccessors ?? defaults.AddPropertyAccessors,
                ExcludeGenericBaseTypes = req.ExcludeGenericBaseTypes ?? defaults.ExcludeGenericBaseTypes,
                SettersReturnThis = req.SettersReturnThis ?? defaults.SettersReturnThis,
                MakePropertiesOptional = req.MakePropertiesOptional ?? defaults.MakePropertiesOptional,
                ExportAsTypes = req.ExportAsTypes ?? defaults.ExportAsTypes,
                ExcludeImplementedInterfaces = defaults.ExcludeImplementedInterfaces,
                AddDefaultXmlNamespace = req.AddDefaultXmlNamespace ?? defaults.AddDefaultXmlNamespace,
                AddNamespaces = req.AddNamespaces ?? defaults.AddNamespaces,
                DefaultNamespaces = req.DefaultNamespaces ?? defaults.DefaultNamespaces,
                DefaultImports = req.DefaultImports ?? defaults.DefaultImports,
                IncludeTypes = TrimArgs(req.IncludeTypes ?? defaults.IncludeTypes),
                ExcludeTypes = TrimArgs(req.ExcludeTypes ?? defaults.ExcludeTypes),
                TreatTypesAsStrings = TrimArgs(req.TreatTypesAsStrings ?? defaults.TreatTypesAsStrings),
                ExportValueTypes = req.ExportValueTypes ?? defaults.ExportValueTypes,
                ExportAttributes = defaults.ExportAttributes,
                ExportTypes = defaults.ExportTypes,
                IgnoreTypes = defaults.IgnoreTypes,
                IgnoreTypesInNamespaces = defaults.IgnoreTypesInNamespaces,
                GlobalNamespace = req.GlobalNamespace ?? defaults.GlobalNamespace,
                ExcludeNamespace = req.ExcludeNamespace ?? defaults.ExcludeNamespace
            };
        }

        public static List<string> TrimArgs(List<string> from)
        {
            var to = from?.Map(x => x?.Trim());
            return to;
        }

        public MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null, Func<Operation, bool> predicate = null)
        {
            return GetMetadataTypesGenerator(config).GetMetadataTypes(req, predicate);
        }

        internal MetadataTypesGenerator GetMetadataTypesGenerator(MetadataTypesConfig config)
        {
            return new MetadataTypesGenerator(meta, config ?? defaults);
        }
    }

    public class MetadataTypesGenerator
    {
        private readonly ServiceMetadata meta;
        private readonly MetadataTypesConfig config;

        public MetadataTypesGenerator(ServiceMetadata meta, MetadataTypesConfig config)
        {
            this.meta = meta;
            this.config = config;
        }

        public MetadataTypes GetMetadataTypes(IRequest req, Func<Operation, bool> predicate = null)
        {
            var metadata = new MetadataTypes
            {
                Config = config,
            };

            var skipTypes = config.IgnoreTypes ?? new HashSet<Type>();
            var opTypes = new HashSet<Type>();
            var ignoreNamespaces = config.IgnoreTypesInNamespaces ?? new List<string>();
            var exportTypes = config.ExportTypes ?? new HashSet<Type>();

            foreach (var operation in meta.Operations)
            {
                if (predicate != null && !predicate(operation))
                    continue;

                if (!meta.IsVisible(req, operation))
                    continue;

                if (opTypes.Contains(operation.RequestType))
                    continue;

                if (skipTypes.Contains(operation.RequestType))
                    continue;

                if (ignoreNamespaces.Contains(operation.RequestType.Namespace))
                    continue;

                var opType = new MetadataOperationType
                {
                    Actions = operation.Actions,
                    Request = ToType(operation.RequestType),
                    Response = ToType(operation.ResponseType),
                };
                metadata.Operations.Add(opType);
                opTypes.Add(operation.RequestType);

                if (operation.ResponseType != null)
                {
                    if (skipTypes.Contains(operation.ResponseType))
                    {
                        //Config.IgnoreTypesInNamespaces in CSharpGenerator
                        opType.Response = null;
                    }
                    else
                    {
                        opTypes.Add(operation.ResponseType);
                    }
                }
            }

            var considered = new HashSet<Type>(opTypes);
            var queue = new Queue<Type>(opTypes);

            bool ignoreTypeFn(Type t) => t == null 
                || t.IsGenericParameter 
                || t == typeof(Enum) 
                || considered.Contains(t) 
                || skipTypes.Contains(t) 
                || (ignoreNamespaces.Contains(t.Namespace) && !exportTypes.ContainsMatch(t));

            void registerTypeFn(Type t)
            {
                if (t.IsArray || t == typeof(Array))
                    return;

                considered.Add(t);
                queue.Enqueue(t);

                if ((!(t.IsSystemType() && !t.IsTuple()) && (t.IsClass || t.IsEnum || t.IsInterface) && !t.IsGenericParameter) || exportTypes.ContainsMatch(t))
                {
                    metadata.Types.Add(ToType(t));

                    foreach (var ns in GetNamespacesUsed(t))
                    {
                        if (!metadata.Namespaces.Contains(ns))
                            metadata.Namespaces.Add(ns);
                    }
                }
            }

            while (queue.Count > 0)
            {
                var type = queue.Dequeue();

                if (IsSystemCollection(type))
                {
                    type = type.GetCollectionType();
                    if (type != null && !ignoreTypeFn(type))
                        registerTypeFn(type);
                    continue;
                }

                if (IsSystemWhitespaceNamespace(type))
                {
                    metadata.Namespaces.AddIfNotExists(type.Namespace);
                }

                if (type.DeclaringType != null)
                {
                    if (!ignoreTypeFn(type.DeclaringType))
                        registerTypeFn(type.DeclaringType);
                }

                if (type.HasInterface(typeof(IService)) && type.GetNestedTypes(BindingFlags.Public | BindingFlags.Instance).IsEmpty())
                    continue;

                if (!type.IsUserType() && !type.IsInterface
                    && !exportTypes.ContainsMatch(type))
                    continue;

                if (!type.HasInterface(typeof(IService)))
                {
                    foreach (var pi in type.GetSerializableProperties()
                        .Where(pi => !ignoreTypeFn(pi.PropertyType)))
                    {
                        registerTypeFn(pi.PropertyType);

                        //Register Property Array Element Types 
                        if (pi.PropertyType.IsArray && !ignoreTypeFn(pi.PropertyType.GetElementType()))
                        {
                            registerTypeFn(pi.PropertyType.GetElementType());
                        }

                        //Register Property Generic Arg Types 
                        if (!pi.PropertyType.IsGenericType) continue;
                        var propArgs = pi.PropertyType.GetGenericArguments();
                        foreach (var arg in propArgs.Where(arg => !ignoreTypeFn(arg)))
                        {
                            registerTypeFn(arg);
                        }
                    }
                }

                var genericBaseTypeDef = type.BaseType != null && type.BaseType.IsGenericType
                    ? type.BaseType.GetGenericTypeDefinition()
                    : null;

#pragma warning disable 618
                if (!ignoreTypeFn(type.BaseType) || 
                    genericBaseTypeDef == typeof(QueryDb<,>) ||
                    genericBaseTypeDef == typeof(QueryData<,>))
#pragma warning restore 618
                {
                    if (genericBaseTypeDef != null)
                    {
                        if (!ignoreTypeFn(genericBaseTypeDef))
                            registerTypeFn(genericBaseTypeDef);

                        foreach (var arg in type.BaseType.GetGenericArguments()
                            .Where(arg => !ignoreTypeFn(arg)))
                        {
                            registerTypeFn(arg);
                        }
                    }
                    else
                    {
                        registerTypeFn(type.BaseType);
                    }
                }

                if (!config.ExcludeImplementedInterfaces)
                {
                    foreach (var iface in type.GetInterfaces())
                    {
                        if (!iface.IsGenericType && !iface.IsSystemType() && !iface.IsServiceStackType())
                        {
                            registerTypeFn(iface);
                        }
                    }
                }

                if (!type.IsGenericType)
                    continue;

                //Register Generic Arg Types 
                var args = type.GetGenericArguments();
                foreach (var arg in args.Where(arg => !ignoreTypeFn(arg)))
                {
                    registerTypeFn(arg);
                }
            }

            return metadata;
        }

        private static bool IsSystemCollection(Type type)
        {
            return type.IsArray
                || (type.Namespace != null
                    && type.Namespace.StartsWith("System")
                    && type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>)));
        }

        private static bool IsSystemWhitespaceNamespace(Type type)
        {
            return type.Namespace == "System.IO";
        }

        public MetadataTypeName ToTypeName(Type type)
        {
            if (type == null) return null;

            return new MetadataTypeName
            {
                Name = type.GetOperationName(),
                Namespace = type.Namespace,
                GenericArgs = type.IsGenericType
                    ? type.GetGenericArguments().Select(x => x.GetFullyQualifiedName()).ToArray()
                    : null,
            };
        }

        public MetadataType ToType(Type type)
        {
            if (type == null) 
                return null;

            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();

            var metaType = new MetadataType
            {
                Name = type.GetOperationName(),
                Namespace = type.Namespace,
                GenericArgs = type.IsGenericType ? GetGenericArgs(type) : null,
                Implements = ToInterfaces(type),
                Attributes = ToAttributes(type),
                Properties = ToProperties(type),
                IsNested = type.IsNested ? true : (bool?)null,
                IsEnum = type.IsEnum ? true : (bool?)null,
                IsEnumInt = JsConfig.TreatEnumAsInteger || type.IsEnumFlags() ? true : (bool?)null,
                IsInterface = type.IsInterface ? true : (bool?)null,
                IsAbstract = type.IsAbstract ? true : (bool?)null,
            };

            if (type.BaseType != null && 
                type.BaseType != typeof(object) && 
                type.BaseType != typeof(ValueType) &&
                !type.IsEnum && 
                !type.HasInterface(typeof(IService)))
            {
                metaType.Inherits = ToTypeName(type.BaseType);
            }

            if (type.GetTypeWithInterfaceOf(typeof(IReturnVoid)) != null)
            {
                metaType.ReturnVoidMarker = true;
            }
            else
            {
                var genericMarker = type != typeof(IReturn<>)
                    ? type.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>))
                    : null;

                if (genericMarker != null)
                {
                    var returnType = genericMarker.GetGenericArguments().First();
                    metaType.ReturnMarkerTypeName = ToTypeName(returnType);
                }
            }

            var routeAttrs = HostContext.AppHost.GetRouteAttributes(type).ToList();
            if (routeAttrs.Count > 0)
            {
                metaType.Routes = routeAttrs.ConvertAll(x =>
                    new MetadataRoute
                    {
                        Path = x.Path,
                        Notes = x.Notes,
                        Summary = x.Summary,
                        Verbs = x.Verbs,
                    });
            }

            metaType.Description = type.GetDescription();

            var dcAttr = type.GetDataContract();
            if (dcAttr != null)
            {
                metaType.DataContract = new MetadataDataContract
                {
                    Name = dcAttr.Name,
                    Namespace = dcAttr.Namespace,
                };
            }

            if (type.IsEnum)
            {
                metaType.EnumNames = new List<string>();
                metaType.EnumValues = new List<string>();

                var isDefaultLayout = true;
                var values = Enum.GetValues(type);
                for (var i = 0; i < values.Length; i++)
                {
                    var value = values.GetValue(i);
                    var name = value.ToString();
                    var enumValue = Convert.ToInt64(value).ToString();

                    if (enumValue != i.ToString())
                        isDefaultLayout = false;

                    metaType.EnumNames.Add(name);
                    metaType.EnumValues.Add(enumValue);
                }

                if (isDefaultLayout)
                    metaType.EnumValues = null;
            }

            var innerTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var innerType in innerTypes)
            {
                if (metaType.InnerTypes == null)
                    metaType.InnerTypes = new List<MetadataTypeName>();

                metaType.InnerTypes.Add(new MetadataTypeName
                {
                    Name = innerType.GetOperationName(),
                    Namespace = innerType.Namespace,
                    GenericArgs = innerType.IsGenericType
                        ? innerType.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                        : null,
                });
            }

            return metaType;
        }

        private static string[] GetGenericArgs(Type type)
        {
            return type.GetGenericArguments().Select(x => x.GetOperationName()).ToArray();
        }

        private MetadataTypeName[] ToInterfaces(Type type)
        {
            return type.GetInterfaces().Where(x => 
            (!config.ExcludeImplementedInterfaces && !x.IsGenericType && !x.IsSystemType() && !x.IsServiceStackType()) 
            || config.ExportTypes.ContainsMatch(x))
            .Map(x =>
                new MetadataTypeName {
                    Name = x.Name,
                    Namespace = x.Namespace,
                    GenericArgs = GetGenericArgs(x),
                }).ToArray();
        }

        public List<MetadataAttribute> ToAttributes(Type type)
        {
            return !(type.IsUserType() || type.IsUserEnum() || type.IsInterface) 
                    || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                ? null
                : ToAttributes(type.AllAttributes());
        }

        public List<MetadataPropertyType> ToProperties(Type type)
        {
            var props = (!type.IsUserType() && 
                         !type.IsInterface && 
                         !type.IsTuple() &&
                         !(config.ExportTypes.ContainsMatch(type) && JsConfig.TreatValueAsRefTypes.ContainsMatch(type))) 
                || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                ? null
                : GetInstancePublicProperties(type).Select(x => ToProperty(x)).ToList();

            return props == null || props.Count == 0 ? null : props;
        }

        public HashSet<string> GetNamespacesUsed(Type type)
        {
            var to = new HashSet<string>();

            if (type.IsUserType() || type.IsInterface || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>)))
            {
                foreach (var pi in GetInstancePublicProperties(type))
                {
                    if (pi.PropertyType.Namespace != null)
                    {
                        to.Add(pi.PropertyType.Namespace);
                    }

                    if (pi.PropertyType.IsGenericType)
                    {
                        pi.PropertyType.GetGenericArguments()
                            .Where(x => x.Namespace != null).Each(x => to.Add(x.Namespace));
                    }
                }

                if (type.IsGenericType)
                {
                    type.GetGenericArguments()
                        .Where(x => x.Namespace != null).Each(x => to.Add(x.Namespace));
                }
            }

            if (type.Namespace != null)
            {
                to.Add(type.Namespace);
            }

            return to;
        }

        public bool IncludeAttrsFilter(Attribute x)
        {
            var type = x.GetType();
            return config.ExportAttributes.Contains(type);
        }

        public List<MetadataAttribute> ToAttributes(object[] attrs)
        {
            var to = attrs.OfType<Attribute>()
                .Where(IncludeAttrsFilter)
                .Select(ToAttribute)
                .ToList();

            return to.Count == 0 ? null : to;
        }

        public List<MetadataAttribute> ToAttributes(IEnumerable<Attribute> attrs)
        {
            var to = attrs
                .Where(IncludeAttrsFilter)
                .Select(ToAttribute)
                .ToList();

            return to.Count == 0 ? null : to;
        }

        public MetadataAttribute ToAttribute(Attribute attr)
        {
            var firstCtor = attr.GetType().GetConstructors()
                //.OrderBy(x => x.GetParameters().Length)
                .FirstOrDefault();
            var metaAttr = new MetadataAttribute
            {
                Name = attr.GetType().Name.Replace("Attribute", ""),
                ConstructorArgs = firstCtor != null
                    ? firstCtor.GetParameters().ToList().ConvertAll(ToProperty)
                    : null,
                Args = NonDefaultProperties(attr),
            };

            //Populate ctor Arg values from matching properties
            var argValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            metaAttr.Args.Each(x => argValues[x.Name] = x.Value);
            metaAttr.Args.RemoveAll(x => x.ReadOnly == true);

            if (metaAttr.ConstructorArgs != null)
            {
                foreach (var arg in metaAttr.ConstructorArgs)
                {
                    if (argValues.TryGetValue(arg.Name, out var value))
                    {
                        arg.Value = value;
                    }
                }
                metaAttr.ConstructorArgs.RemoveAll(x => x.Value == null);
                if (metaAttr.ConstructorArgs.Count == 0)
                    metaAttr.ConstructorArgs = null;
            }

            //Only emit ctor args or property args
            if (metaAttr.ConstructorArgs == null
                || metaAttr.ConstructorArgs.Count != metaAttr.Args.Count)
            {
                metaAttr.ConstructorArgs = null;
            }
            else
            {
                metaAttr.Args = null;
            }

            return metaAttr;
        }

        public List<MetadataPropertyType> NonDefaultProperties(Attribute attr)
        {
            return attr.GetType().GetPublicProperties()
                .Select(pi => ToProperty(pi, attr))
                .Where(property => property.Name != "TypeId"
                    && property.Value != null)
                .OrderBy(property => property.Name)
                .ToList();
        }

        public MetadataPropertyType ToProperty(PropertyInfo pi, object instance = null)
        {
            var genericArgs = pi.PropertyType.IsGenericType
                ? pi.PropertyType.GetGenericArguments().Select(x => x.ExpandTypeName()).ToArray()
                : null;

            var property = new MetadataPropertyType
            {
                Name = pi.Name,
                Attributes = ToAttributes(pi.GetCustomAttributes(false)),
                Type = pi.PropertyType.GetMetadataPropertyType(),
                IsValueType = pi.PropertyType.IsValueType ? true : (bool?)null,
                IsSystemType = pi.PropertyType.IsSystemType() ? true : (bool?)null,
                IsEnum = pi.PropertyType.IsEnum ? true : (bool?)null,
                TypeNamespace = pi.PropertyType.Namespace,
                DataMember = ToDataMember(pi.GetDataMember()),
                GenericArgs = genericArgs,
                Description = pi.GetDescription(),
            };

            var apiMember = pi.FirstAttribute<ApiMemberAttribute>();
            if (apiMember != null)
            {
                if (apiMember.IsRequired)
                    property.IsRequired = true;

                property.ParamType = apiMember.ParameterType;
                property.DisplayType = apiMember.DataType;
            }

            var apiAllowableValues = pi.FirstAttribute<ApiAllowableValuesAttribute>();
            if (apiAllowableValues != null)
            {
                property.AllowableValues = apiAllowableValues.Values;
                property.AllowableMin = apiAllowableValues.Min;
                property.AllowableMax = apiAllowableValues.Max;
            }

            if (instance != null)
            {
                var value = pi.GetValue(instance, null);
                if (value != null
                    && !value.Equals(pi.PropertyType.GetDefaultValue()))
                {
                    if (pi.PropertyType.IsEnum)
                    {
                        property.Value = "{0}.{1}".Fmt(pi.PropertyType.Name, value);
                    }
                    else if (pi.PropertyType == typeof(Type))
                    {
                        var type = (Type)value;
                        property.Value = $"typeof({type.FullName})";
                    }
                    else
                    {
                        var strValue = value as string;
                        property.Value = strValue ?? value.ToJson();
                    }
                }

                if (pi.GetSetMethod() == null) //ReadOnly is bool? to minimize serialization
                    property.ReadOnly = true;
            }
            return property;
        }

        public MetadataPropertyType ToProperty(ParameterInfo pi)
        {
            var propertyAttrs = pi.AllAttributes();
            var property = new MetadataPropertyType
            {
                Name = pi.Name,
                Attributes = ToAttributes(propertyAttrs),
                Type = pi.ParameterType.GetOperationName(),
                IsValueType = pi.ParameterType.IsValueType ? true : (bool?)null,
                IsSystemType = pi.ParameterType.IsSystemType() ? true : (bool?)null,
                IsEnum = pi.ParameterType.IsEnum ? true : (bool?)null,
                TypeNamespace = pi.ParameterType.Namespace,
                Description = pi.GetDescription(),
            };

            return property;
        }

        public static MetadataDataMember ToDataMember(DataMemberAttribute attr)
        {
            if (attr == null) return null;

            var metaAttr = new MetadataDataMember
            {
                Name = attr.Name,
                EmitDefaultValue = attr.EmitDefaultValue != true ? attr.EmitDefaultValue : (bool?)null,
                Order = attr.Order >= 0 ? attr.Order : (int?)null,
                IsRequired = attr.IsRequired != false ? attr.IsRequired : (bool?)null,
            };

            return metaAttr;
        }

        public static PropertyInfo[] GetInstancePublicProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .OnlySerializableProperties(type)
                .Where(t => 
                    t.GetIndexParameters().Length == 0 && // ignore indexed properties
                    !t.HasAttribute<ExcludeMetadataAttribute>())
                .ToArray();
        }
    }

    public class CreateTypeOptions
    {
        public Func<string> ImplementsFn { get; set; }
        public bool IsRequest { get; set; }
        public bool IsResponse { get; set; }
        public bool IsType { get; set; }
        public bool IsNestedType { get; set; }
    }

    public class TextNode
    {
        public TextNode()
        {
            Children = new List<TextNode>();
        }

        public string Text { get; set; }

        public List<TextNode> Children { get; set; }
    }

    public static class MetadataExtensions
    {
        public static MetadataTypeName ToMetadataTypeName(this MetadataType type)
        {
            if (type == null) return null;

            return new MetadataTypeName
            {
                Name = type.Name,
                Namespace = type.Namespace,
                GenericArgs = type.GenericArgs
            };
        }

        public static MetadataType ToMetadataType(this MetadataTypeName type)
        {
            if (type == null) return null;

            return new MetadataType
            {
                Name = type.Name,
                Namespace = type.Namespace,
                GenericArgs = type.GenericArgs
            };
        }

        public static List<MetadataType> GetAllMetadataTypes(this MetadataTypes metadata)
        {
            var allTypes = new List<MetadataType>();
            allTypes.AddRange(metadata.Types);
            allTypes.AddRange(metadata.Operations.Where(x => x.Request != null).Select(x => x.Request));
            allTypes.AddRange(metadata.Operations.Where(x => x.Response != null).Select(x => x.Response));
            allTypes.AddRange(metadata.Operations.Where(x => x.Request?.ReturnMarkerTypeName != null).Select(
                x => x.Request.ReturnMarkerTypeName.ToMetadataType()));
            return allTypes;
        }

        public static HashSet<string> GetReferencedTypeNames(this MetadataType type)
        {
            var to = new HashSet<string>();

            if (type.Inherits != null)
            {
                to.Add(type.Inherits.Name);

                foreach (var genericArg in type.Inherits.GenericArgs.Safe())
                {
                    to.Add(genericArg);
                }
            }

            foreach (var pi in type.Properties.Safe())
            {
                to.Add(pi.Type);

                foreach (var genericArg in pi.GenericArgs.Safe())
                {
                    to.Add(genericArg);
                }
            }

            return to;
        }

        public static bool IgnoreSystemType(this MetadataType type)
        {
            return type == null
                || (type.Namespace != null && type.Namespace.StartsWith("System"))
                || (type.Inherits != null && type.Inherits.Name == "Array");
        }

        public static HashSet<string> GetDefaultNamespaces(this MetadataTypesConfig config, MetadataTypes metadata)
        {
            var namespaces = config.DefaultNamespaces.ToHashSet();
            config.AddNamespaces.Safe().Each(x => namespaces.Add(x));

            //Add any ignored namespaces used
            foreach (var ns in metadata.Namespaces)
            {
                //Ignored by IsUserType()
                if (!ns.StartsWith("System") && !config.IgnoreTypesInNamespaces.Contains(ns))
                    continue;

                if (!namespaces.Contains(ns))
                {
                    namespaces.Add(ns);
                }
            }

            return namespaces;
        }

        static char[] blockChars = new[] { '<', '>' };
        public static TextNode ParseTypeIntoNodes(this string typeDef)
        {
            if (string.IsNullOrEmpty(typeDef))
                return null;

            var node = new TextNode();
            var lastBlockPos = typeDef.IndexOf('<');

            if (lastBlockPos >= 0)
            {
                node.Text = typeDef.Substring(0, lastBlockPos);

                var blockStartingPos = new Stack<int>();
                blockStartingPos.Push(lastBlockPos);

                while (lastBlockPos != -1 || blockStartingPos.Count == 0)
                {
                    var nextPos = typeDef.IndexOfAny(blockChars, lastBlockPos + 1);
                    if (nextPos == -1)
                        break;

                    var blockChar = typeDef.Substring(nextPos, 1);

                    if (blockChar == "<")
                    {
                        blockStartingPos.Push(nextPos);
                    }
                    else
                    {
                        var startPos = blockStartingPos.Pop();
                        if (blockStartingPos.Count == 0)
                        {
                            var endPos = nextPos;
                            var childBlock = typeDef.Substring(startPos + 1, endPos - startPos - 1);

                            var args = SplitGenericArgs(childBlock);
                            foreach (var arg in args)
                            {
                                if (arg.IndexOfAny(blockChars) >= 0)
                                {
                                    var childNode = ParseTypeIntoNodes(arg);
                                    if (childNode != null)
                                    {
                                        node.Children.Add(childNode);
                                    }
                                }
                                else
                                {
                                    node.Children.Add(new TextNode { Text = arg });
                                }
                            }

                        }
                    }

                    lastBlockPos = nextPos;
                }
            }
            else
            {
                node.Text = typeDef;
            }

            return node;
        }

        public static string ToPrettyName(this Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            
            var genericTypeName = type.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.LeftPart('`');
            var genericArgs = string.Join(",",
                type.GetGenericArguments()
                    .Select(ToPrettyName).ToArray());
            return genericTypeName + "<" + genericArgs + ">";
        }

        public static List<string> SplitGenericArgs(string argList)
        {
            var to = new List<string>();
            if (string.IsNullOrEmpty(argList))
                return to;

            var lastPos = 0;
            var blockCount = 0;
            for (var i = 0; i < argList.Length; i++)
            {
                var argChar = argList[i];
                switch (argChar)
                {
                    case ',':
                        if (blockCount == 0)
                        {
                            var arg = argList.Substring(lastPos, i - lastPos);
                            to.Add(arg);
                            lastPos = i + 1;
                        }
                        break;
                    case '<':
                        blockCount++;
                        break;
                    case '>':
                        blockCount--;
                        break;
                }
            }

            if (lastPos > 0)
            {
                var arg = argList.Substring(lastPos);
                to.Add(arg);
            }
            else
            {
                to.Add(argList);
            }

            return to;
        }

        public static void RemoveIgnoredTypesForNet(this MetadataTypes metadata, MetadataTypesConfig config)
        {
            metadata.RemoveIgnoredTypes(config);
            //Don't include Exported Types in System 
            metadata.Types.RemoveAll(x => x.IgnoreSystemType()); 
        }

        public static void RemoveIgnoredTypes(this MetadataTypes metadata, MetadataTypesConfig config)
        {
            var includeList = GetIncludeList(metadata, config);

            metadata.Types.RemoveAll(x => x.IgnoreType(config, includeList));

            var matchingResponseTypes = includeList != null 
                ? metadata.Operations.Where(x => x.Response != null && includeList.Contains(x.Response.Name))
                    .Map(x => x.Response).ToArray()
                : TypeConstants<MetadataType>.EmptyArray;

            metadata.Operations.RemoveAll(x => x.Request.IgnoreType(config, includeList));
            metadata.Operations.Each(x => {
                if (x.Response != null && x.Response.IgnoreType(config, includeList))
                {
                    x.Response = null;
                }
            });

            //When the included Type is a Response Type because defined in another Service that's not included
            //ref: https://forums.servicestack.net/t/class-is-missing-from-generated-code/3030
            foreach (var responseType in matchingResponseTypes)
            {
                if (!metadata.Operations.Any(x => x.Response != null && x.Response.Name == responseType.Name)
                    && metadata.Types.All(x => x.Name != responseType.Name))
                {
                    metadata.Types.Add(responseType);
                }
            }
        }

        public static List<string> GetIncludeList(MetadataTypes metadata, MetadataTypesConfig config)
        {
            const string wildCard = ".*";

            if (config.IncludeTypes == null)
                return null;

            var typesToExpand = config.IncludeTypes
                .Where(s => s.Length > 2 && s.EndsWith(wildCard))
                .Map(s => s.Substring(0, s.Length - 2));

            if (typesToExpand.Count == 0)
                return config.IncludeTypes;

            // From IncludeTypes get the corresponding MetadataTypes
            var includedMetadataTypes = metadata.Operations
                .Select(o => o.Request)
                .Where(t => typesToExpand.Contains(t.Name))
                .ToList();

            var includeSet = includedMetadataTypes
                .Where(x => x.ReturnMarkerTypeName != null)
                .Select(x => x.ReturnMarkerTypeName.Name)
                .ToHashSet();

            var includedResponses = metadata.Operations
                .Where(t => typesToExpand.Contains(t.Request.Name) && t.Response != null)
                .Select(o => o.Response)
                .ToList();
            includedResponses.ForEach(x => includeSet.Add(x.Name));

            var returnTypesForInclude = metadata.Operations
                .Where(x => x.Response != null && includeSet.Contains(x.Response.Name))
                .Map(x => x.Response);

            // GetReferencedTypes for both request + response objects
            var referenceTypes = includedMetadataTypes
                .Union(returnTypesForInclude)
                .Where(x => x != null)
                .SelectMany(x => x.GetReferencedTypeNames());

            return referenceTypes
                .Union(config.IncludeTypes)
                .Union(typesToExpand)
                .Union(returnTypesForInclude.Select(x => x.Name))
                .Distinct()
                .ToList();
        }

        public static bool IgnoreType(this MetadataType type, MetadataTypesConfig config, List<string> overrideIncludeType = null)
        {
            // If is a systemType and export types doesn't include this 
            if (type.IgnoreSystemType() && config.ExportTypes.All(x => x.Name != type.Name && !type.Name.StartsWith(x.Name + "`")))
                return true;

            var includes = overrideIncludeType ?? config.IncludeTypes;
            if (includes != null && !includes.Contains(type.Name))
                return true;

            if (config.ExcludeTypes != null &&
                config.ExcludeTypes.Any(x => type.Name == x || type.Name.StartsWith(x + "`")))
                return true;

            return false;
        }

        public static string SanitizeType(this string typeName)
        {
            return typeName?.TrimStart('\'');
        }

        public static string SafeComment(this string comment)
        {
            return comment.Replace("\r", "").Replace("\n", "");
        }

        public static string SafeToken(this string token)
        {
            if (!NativeTypesFeature.DisableTokenVerification && token.ContainsAny("\"", "-", "+", "\\", "*", "=", "!"))
                throw new InvalidDataException($"MetaData is potentially malicious. Expected token, Received: {token}");

            return token;
        }

        public static string SafeValue(this string value)
        {
            if (!NativeTypesFeature.DisableTokenVerification && value.Contains('"'))
                throw new InvalidDataException($"MetaData is potentially malicious. Expected scalar value, Received: {value}");

            return value;
        }

        public static string QuotedSafeValue(this string value)
        {
            value = value.Replace("\r", "").Replace("\n", "");
            return $"\"{value.SafeValue()}\"";
        }

        public static MetadataAttribute ToMetadataAttribute(this MetadataRoute route)
        {
            var attr = new MetadataAttribute
            {
                Name = "Route",
                ConstructorArgs = new List<MetadataPropertyType>
                {
                    new MetadataPropertyType { Type = "string", Value = route.Path },
                },
            };

            if (route.Verbs != null)
            {
                attr.ConstructorArgs.Add(
                    new MetadataPropertyType { Type = "string", Value = route.Verbs });
            }

            return attr;
        }

        public static List<MetadataType> GetAllTypes(this MetadataTypes metadata)
        {
            var map = new Dictionary<string, MetadataType>();
            foreach (var op in metadata.Operations)
            {
                if (!(op.Request.Namespace ?? "").StartsWith("System"))
                    map[op.Request.Name] = op.Request;
                if (op.Response != null && !(op.Response.Namespace ?? "").StartsWith("System"))
                    map[op.Response.Name] = op.Response;
            }
            metadata.Types.Each(x => map[x.Name] = x);
            return map.Values.ToList();
        }

        public static void Push(this Dictionary<string, List<string>> map, string key, string value)
        {
            if (!map.TryGetValue(key, out var results))
                map[key] = results = new List<string>();

            if (!results.Contains(value))
                results.Add(value);
        }

        public static List<string> GetValues(this Dictionary<string, List<string>> map, string key)
        {
            map.TryGetValue(key, out var results);
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

        public static string GetTypeName(this MetadataPropertyType prop, MetadataTypesConfig config, List<MetadataType> allTypes)
        {
            if (prop.IsValueType != true || prop.IsEnum == true)
                return prop.Type;

            if (prop.IsSystemType == true)
            {
                if (prop.Type != "Nullable`1" || prop.GenericArgs?.Length != 1)
                    return prop.Type;

                if (config.ExportValueTypes)
                    return prop.Type;

                // Find out if the ValueType is not a SystemType or Enum by looking if this Info is declared in another prop
                var genericArg = prop.GenericArgs[0];
                var typeInfo = allTypes.Where(x => x.Properties != null)
                    .SelectMany(x => x.Properties)
                    .FirstOrDefault(x => x.Type == genericArg);

                return typeInfo != null && typeInfo.IsSystemType != true && typeInfo.IsEnum != true
                    ? "String"
                    : prop.Type;
            }

            //Whether or not to emit the Struct Type Name, info: https://github.com/ServiceStack/Issues/issues/503#issuecomment-262133343
            return config.ExportValueTypes
                ? prop.Type
                : "String";
        }

        internal static bool ContainsMatch(this HashSet<Type> types, Type target)
        {
            if (types == null)
                return false;

            if (types.Contains(target))
                return true;

            return types.Any(x => x.IsGenericTypeDefinition && target.IsOrHasGenericInterfaceTypeOf(x));
        }

        //Workaround to handle Nullable<T>[] arrays. Most languages don't support Nullable in nested types
        internal static string StripNullable(this string type)
        {
            return StripGenericType(type, "Nullable");
        }

        public static string StripGenericType(string type, string subType)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(subType))
                return type;

            var pos = type.IndexOf(subType, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0)
            {
                var endsToEat = 1;
                var startPos = pos + subType.Length + 1;
                for (var i = startPos; i < type.Length; i++)
                {
                    if (type[i] == '<')
                        endsToEat++;
                    if (type[i] == '>')
                    {
                        if (--endsToEat == 0)
                        {
                            return type.Substring(0, pos)
                                + type.Substring(startPos, i - startPos) 
                                + type.Substring(i + 1);
                        }
                    }
                }
            }

            return type;
        }

        public static bool IsServiceStackType(this Type type) => type.Namespace?.StartsWith("ServiceStack") == true;
    }
}