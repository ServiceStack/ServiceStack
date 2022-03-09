using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.NativeTypes
{
    public delegate string TypeFilterDelegate(string typeName, string[] genericArgs);
    public delegate string AddCodeDelegate(List<MetadataType> allTypes, MetadataTypesConfig config);

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
            return new() {
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
                AddNamespaces = req.AddNamespaces ?? defaults.AddNamespaces?.ToList(),
                DefaultNamespaces = req.DefaultNamespaces ?? defaults.DefaultNamespaces?.ToList(),
                DefaultImports = req.DefaultImports ?? defaults.DefaultImports?.ToList(),
                IncludeTypes = TrimArgs(req.IncludeTypes ?? defaults.IncludeTypes?.ToList()),
                ExcludeTypes = TrimArgs(req.ExcludeTypes ?? defaults.ExcludeTypes?.ToList()),
                TreatTypesAsStrings = TrimArgs(req.TreatTypesAsStrings ?? defaults.TreatTypesAsStrings?.ToList()),
                ExportValueTypes = req.ExportValueTypes ?? defaults.ExportValueTypes,
                ExportAttributes = defaults.ExportAttributes?.ToSet(),
                ExportTypes = defaults.ExportTypes?.ToSet(),
                IgnoreTypes = defaults.IgnoreTypes?.ToSet(),
                IgnoreTypesInNamespaces = defaults.IgnoreTypesInNamespaces?.ToList(),
                GlobalNamespace = req.GlobalNamespace ?? defaults.GlobalNamespace,
                ExcludeNamespace = req.ExcludeNamespace ?? defaults.ExcludeNamespace,
                DataClass = req.DataClass ?? defaults.DataClass,
                DataClassJson = req.DataClassJson ?? defaults.DataClassJson,
            };
        }

        public static List<string> TrimArgs(List<string> from)
        {
            var to = from?.Map(x => x?.Trim());
            return to;
        }

        public MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null, Func<Operation, bool> predicate = null)
        {
            return GetGenerator(config).GetMetadataTypes(req, predicate);
        }
        
        public MetadataTypesGenerator GetGenerator() => new MetadataTypesGenerator(meta, defaults);

        public MetadataTypesGenerator GetGenerator(MetadataTypesConfig config)
        {
            return new MetadataTypesGenerator(meta, config ?? defaults);
        }
    }

    public class MetadataTypesGenerator
    {
        private static ILog log = LogManager.GetLogger(typeof(MetadataTypesGenerator));
        
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

            bool ShouldIgnoreOperation(Operation op)
            {
                if (predicate != null && !predicate(op))
                    return true;

                if (!meta.IsVisible(req, op))
                    return true;

                if (skipTypes.Contains(op.RequestType))
                    return true;

                if (ignoreNamespaces.Contains(op.RequestType.Namespace))
                    return true;
                return false;
            }

            foreach (var operation in meta.Operations)
            {
                if (ShouldIgnoreOperation(operation)) 
                    continue;

                if (opTypes.Contains(operation.RequestType))
                    continue;

                var opType = new MetadataOperationType
                {
                    Actions = operation.Actions,
                    Method = operation.Method,
                    Request = ToType(operation.RequestType),
                    Response = ToType(operation.ResponseType),
                    DataModel = ToTypeName(operation.DataModelType),
                    ViewModel = ToTypeName(operation.ViewModelType),
                    RequiresAuth = operation.RequiresAuthentication.NullIfFalse(),
                    RequiredRoles = operation.RequiredRoles.NullIfEmpty(),
                    RequiresAnyRole = operation.RequiresAnyRole.NullIfEmpty(),
                    RequiredPermissions = operation.RequiredPermissions.NullIfEmpty(),
                    RequiresAnyPermission = operation.RequiresAnyPermission.NullIfEmpty(),
                    Tags = operation.Tags.Count > 0 ? operation.Tags.Map(x => x.Name) : null,
                    Ui = operation.QueryCss == null && operation.ExplorerCss == null && operation.FormLayout == null 
                        ? null 
                        : new ApiUiInfo {
                            QueryCss = operation.QueryCss,
                            ExplorerCss = operation.ExplorerCss,
                            FormLayout = operation.FormLayout,
                        },
                };
                opType.Request.RequestType = opType;
                metadata.Operations.Add(opType);
                opTypes.Add(operation.RequestType);

                if (operation.ResponseType != null)
                {
                    if (skipTypes.Contains(operation.ResponseType))
                    {
                        //Config.IgnoreTypesInNamespaces in CSharpGenerator
                        opType.Response = null;
                    }
                }
                if (operation.RequestType.GetTypeWithInterfaceOf(typeof(IReturnVoid)) != null)
                {
                    opType.ReturnsVoid = true;
                }
                else
                {
                    var genericMarker = operation.RequestType != typeof(IReturn<>)
                        ? operation.RequestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>))
                        : null;

                    if (genericMarker != null)
                    {
                        var returnType = genericMarker.GetGenericArguments().First();
                        opType.ReturnType = ToTypeName(returnType);
                    }
                }

                var routeAttrs = HostContext.AppHost.GetRouteAttributes(operation.RequestType).ToList();
                if (routeAttrs.Count > 0)
                {
                    opType.Routes = routeAttrs.ConvertAll(x =>
                        new MetadataRoute
                        {
                            RouteAttribute = x,
                            Path = x.Path,
                            Notes = x.Notes,
                            Summary = x.Summary,
                            Verbs = x.Verbs,
                        });
                }
            }
            // Add all Request Types before Response Types
            foreach (var operation in meta.Operations)
            {
                if (ShouldIgnoreOperation(operation)) 
                    continue;

                if (operation.ResponseType != null)
                {
                    opTypes.Add(operation.ResponseType);
                }
            }

            var considered = new HashSet<Type>(opTypes);
            var queue = new Queue<Type>(opTypes);
            var registeredTypes = new HashSet<string>();

            bool ignoreTypeFn(Type t) => t == null 
                || t.IsGenericParameter 
                || t == typeof(Enum) 
                || considered.Contains(t) 
                || skipTypes.Contains(t) 
                || (ignoreNamespaces.Contains(t.Namespace) && !exportTypes.ContainsMatch(t));

            void registerTypeFn(Type t)
            {
                if (t.IsArray || t == typeof(Array))
                {
                    t = t.GetElementType();
                    if (t == null)
                        return;
                }

                if (!considered.Contains(t))
                {
                    considered.Add(t);
                    queue.Enqueue(t);
                }

                var typeKey = t.Namespace + "." + t.Name; //codegen-ed types have different identity, need to check on full Type Name
                if (registeredTypes.Contains(typeKey))
                    return;

                if (!(t.IsSystemType() && !t.IsTuple())
                    && (t.IsClass || t.IsEnum || t.IsInterface) 
                    && !t.IsGenericParameter || exportTypes.ContainsMatch(t))
                {
                    metadata.Types.Add(ToType(t));
                    registeredTypes.Add(typeKey);

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

                // Include Types defined in AutoCrud interfaces
                if (genericBaseTypeDef == null && type.HasInterface(typeof(ICrud)))
                {
                    foreach (var iface in type.GetInterfaces())
                    {
                        if (iface.IsGenericType && iface.HasInterface(typeof(ICrud)))
                        {
                            registerTypeFn(iface.GetGenericArguments()[0]);
                        }
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
                Type = type,
                Name = type.GetOperationName(),
                Namespace = type.Namespace,
                GenericArgs = type.IsGenericType
                    ? type.GetGenericArguments().Select(x => x.GetFullyQualifiedName()).ToArray()
                    : null,
            };
        }

        public MetadataType ToFlattenedType(Type type)
        {
            if (type == null) 
                return null;

            MetadataType to = null;
            do
            {
                var metaType = ToType(type);
                if (to == null)
                {
                    to = metaType;
                }
                else
                {
                    if (metaType.Properties != null)
                    {
                        to.Properties ??= new List<MetadataPropertyType>();
                        foreach (var metaProp in metaType.Properties)
                        {
                            to.Properties.Add(metaProp);
                        }
                    }
                }
            } while ((type = type.BaseType) != typeof(object) && type != null);
            return to;
        }

        public MetadataType ToType(Type type)
        {
            if (type == null) 
                return null;

            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();

            var metaType = new MetadataType
            {
                Type = type,
                Name = type.GetOperationName(),
                Namespace = type.Namespace,
                GenericArgs = type.IsGenericType ? GetGenericArgs(type) : null,
                Implements = ToInterfaces(type),
                Attributes = ToAttributes(type),
                Properties = ToProperties(type),
                IsNested = type.IsNested.NullIfFalse(),
                IsEnum = type.IsEnum.NullIfFalse(),
                IsEnumInt = (JsConfig.TreatEnumAsInteger || type.IsEnumFlags()).NullIfFalse(),
                IsInterface = type.IsInterface.NullIfFalse(),
                IsAbstract = type.IsAbstract.NullIfFalse(),
            };

            var propsToAdd = new List<MetadataPropertyType>();
            var fieldAttrs = type.AllAttributes<FieldAttribute>();
            foreach (var attr in fieldAttrs)
            {
                var property = metaType.Properties?.FirstOrDefault(x => x.Name == attr.Name);
                if (property == null)
                {
                    // If it's in a base property we need to hoist it to attach Input info
                    var pi = type.GetPublicProperties().FirstOrDefault(p => p.Name == attr.Name);
                    if (pi != null)
                    {
                        property = ToProperty(pi);
                        property.Input = null; // Always use [Field]
                        propsToAdd.Add(property);
                    }
                    else
                    {
                        log.Warn($"Ignoring non-existing '{attr.Name}' Property not found on DTO '{type.Name}'");
                        continue;
                    }
                }
                if (property.Input != null)
                {
                    log.Warn($"Ignoring populating existing Input of Property '{type.Name}.{attr.Name}'");
                    continue;
                }
                property.Input = attr.ToInput(c => {
                    c.Required ??= property.IsRequired;
                    c.MinLength ??= property.AllowableMin;
                    c.MaxLength ??= property.AllowableMax;
                    if (attr.FieldCss != null || attr.InputCss != null || attr.LabelCss != null)
                    {
                        c.Css ??= new FieldCss();
                        c.Css.Field ??= attr.FieldCss;
                        c.Css.Input ??= attr.InputCss;
                        c.Css.Label ??= attr.LabelCss;
                    }
                });
            }

            if (propsToAdd.Count > 0)
            {
                metaType.Properties ??= new();
                metaType.Properties.InsertRange(0, propsToAdd);
            }

            if (type.BaseType != null && 
                type.BaseType != typeof(object) && 
                type.BaseType != typeof(ValueType) &&
                !type.IsEnum && 
                !type.HasInterface(typeof(IService)))
            {
                metaType.Inherits = ToTypeName(type.BaseType);
            }

            metaType.Description = type.GetDescription();
            metaType.Notes = type.FirstAttribute<NotesAttribute>()?.Notes;
            metaType.Icon = X.Map(type.FirstAttribute<IconAttribute>(),
                x => new ImageInfo { Svg = x.Svg, Uri = x.Uri, Cls = x.Cls, Alt = x.Alt });

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
                metaType.EnumMemberValues = new List<string>();
                var enumDescriptions = new List<string>();

                var isDefaultLayout = true;
                var isIntEnum = JsConfig.TreatEnumAsInteger || type.IsEnumFlags();
                var names = Enum.GetNames(type);
                for (var i = 0; i < names.Length; i++)
                {
                    var name = names[i];
                    metaType.EnumNames.Add(name);
                    metaType.EnumMemberValues.Add(name);

                    var enumMember = GetEnumMember(type, name);
                    var enumDesc = enumMember.GetDescription();
                    enumDescriptions.Add(enumDesc);

                    var value = enumMember.GetRawConstantValue();
                    var enumValue = Convert.ToInt64(value).ToString();
                    metaType.EnumValues.Add(enumValue);
                    
                    var enumMemberValue = enumMember.FirstAttribute<EnumMemberAttribute>()?.Value;
                    if (enumMemberValue != null)
                        metaType.EnumMemberValues[i] = enumMemberValue;

                    if (enumValue != i.ToString())
                        isDefaultLayout = false;
                }

                if (enumDescriptions.Any(x => !string.IsNullOrEmpty(x)))
                    metaType.EnumDescriptions = enumDescriptions;

                if (!isIntEnum && isDefaultLayout)
                    metaType.EnumValues = null;

                var requiresMemberValues = false;
                for (int i = 0; i < metaType.EnumNames.Count; i++)
                {
                    if (metaType.EnumNames[i] != metaType.EnumMemberValues[i])
                    {
                        requiresMemberValues = true;
                        break;
                    }
                }

                if (!requiresMemberValues)
                    metaType.EnumMemberValues = null;
            }

            var innerTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var innerType in innerTypes)
            {
                if (metaType.InnerTypes == null)
                    metaType.InnerTypes = new List<MetadataTypeName>();

                metaType.InnerTypes.Add(new MetadataTypeName    
                {
                    Type = innerType,
                    Name = innerType.GetOperationName(),
                    Namespace = innerType.Namespace,
                    GenericArgs = innerType.IsGenericType
                        ? innerType.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                        : null,
                });
            }

            foreach (var configure in HostContext.AppHost.Metadata.ConfigureMetadataTypes)
            {
                configure(metaType);
            }
            return metaType;
        }

        public static FieldInfo GetEnumMember(Type type, string name) => 
            (FieldInfo) type.GetMember(name, BindingFlags.Public | BindingFlags.Static).First();

        private static string[] GetGenericArgs(Type type)
        {
            return type.GetGenericArguments().Select(x => x.GetOperationName()).ToArray();
        }

        private MetadataTypeName[] ToInterfaces(Type type)
        {
            return type.GetDirectInterfaces().Where(x => 
            (!config.ExcludeImplementedInterfaces && !x.IsGenericType && !x.IsSystemType() && !x.IsServiceStackType()) 
            || config.ExportTypes.ContainsMatch(x))
            .Map(x =>
                new MetadataTypeName {
                    Type = x,
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
                : GetInstancePublicProperties(type).Select(x => ToProperty(x))
                    .ToList().PopulatePrimaryKey();

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
        
        public static Dictionary<Type, Func<Attribute, MetadataAttribute>> AttributeConverters { get; } = new();

        public MetadataAttribute ToAttribute(Attribute attr)
        {
            if (AttributeConverters.TryGetValue(attr.GetType(), out var converter))
                return converter(attr);

            return ToMetadataAttribute(attr);
        }

        public MetadataAttribute ToMetadataAttribute(Attribute attr)
        {
            if (attr is IReflectAttributeConverter converter)
            {
                var ret = converter.ToReflectAttribute();
                if (ret != null)
                {
                    MetadataPropertyType metaProp(KeyValuePair<PropertyInfo, object> entry)
                    {
                        var to = ToProperty(entry.Key);
                        to.Value = PropertyStringValue(entry.Key, entry.Value);
                        return to;
                    }
                    
                    return new MetadataAttribute {
                        Name = ret.Name ?? attr.GetAttributeName(),
                        ConstructorArgs = ret.ConstructorArgs?.Map(metaProp),
                        Args = ret.PropertyArgs?.Map(metaProp),
                    };
                }
            }
            
            var attrType = attr.GetType();
            var firstCtor = attrType.GetConstructors()
                //.OrderBy(x => x.GetParameters().Length)
                .FirstOrDefault();
            var emptyCtor = attrType.GetConstructor(Type.EmptyTypes);
            var metaAttr = new MetadataAttribute
            {
                Attribute = attr,
                Name = attr.GetAttributeName(),
                ConstructorArgs = firstCtor != null
                    ? firstCtor.GetParameters().ToList().ConvertAll(ToProperty)
                    : null,
            };

            var attrProps = Properties(attr);
            var metaArgs = attrProps
                .Select(x => ToProperty(x, attr))
                .Where(x => x.Value != null && x.ReadOnly != true);

            if (attr is IReflectAttributeFilter reflectAttr)
                metaArgs = metaArgs.Where(x => reflectAttr.ShouldInclude(x.PropertyInfo, x.Value));

            metaAttr.Args = metaArgs.ToList();

            //Populate ctor Arg values from matching properties
            var argValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            attrProps.Each(x => argValues[x.Name] = PropertyValue(x, attr));

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
            if (emptyCtor != null //empty ctor required for Property Args 
                && (metaAttr.ConstructorArgs == null
                    || metaAttr.ConstructorArgs.Count <= metaAttr.Args.Count))
            {
                metaAttr.ConstructorArgs = null;
            }
            else
            {
                metaAttr.Args = null;
            }

            return metaAttr;
        }

        public List<PropertyInfo> Properties(Attribute attr)
        {
            var props = attr.GetType().GetPublicProperties()
                .Where(property => property.Name != "TypeId" && !property.HasAttribute<IgnoreAttribute>());
                
            return attr.GetType().FirstAttribute<TagAttribute>()?.Name == "PropertyOrder"
                ? props.ToList()
                : props.OrderBy(property => property.Name).ToList();
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

        public MetadataPropertyType ToProperty(PropertyInfo pi, object instance = null, Dictionary<string, object> ignoreValues = null)
        {
            var propType = pi.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
            var property = new MetadataPropertyType
            {
                PropertyInfo = pi,
                PropertyType = propType,
                Name = pi.Name,
                Attributes = ToAttributes(pi.GetCustomAttributes(false)),
                Type = propType.GetMetadataPropertyType(),
                IsValueType = underlyingType.IsValueType.NullIfFalse(),
                IsEnum = underlyingType.IsEnum.NullIfFalse(),
                Namespace = propType.Namespace,
                DataMember = ToDataMember(pi.GetDataMember()),
                GenericArgs = ToGenericArgs(propType),
                Description = pi.GetDescription(),
            };

            property.Format ??= pi.FirstAttribute<Intl>().ToFormat();
            property.Format ??= pi.FirstAttribute<FormatAttribute>().ToFormat();

            var apiMember = pi.FirstAttribute<ApiMemberAttribute>();
            if (apiMember != null)
            {
                if (apiMember.IsRequired)
                    property.IsRequired = true;

                property.ParamType = apiMember.ParameterType;
                property.DisplayType = apiMember.DataType;
                property.Format ??= new FormatInfo { Method = apiMember.Format };
            }

            var validateProp = pi.AllAttributes<ValidateAttribute>();
            if (validateProp.Any(x => x.Validator != null && ValidateScripts.RequiredValidators.Contains(x.Validator)))
                property.IsRequired = true;

            var requiredProp = pi.FirstAttribute<RequiredAttribute>();
            if (requiredProp != null)
                property.IsRequired = true;

            if (HostContext.AppHost.Config.TreatNonNullableRefTypesAsRequired)
            {
                var notNullRefType = pi.IsNotNullable();
                if (notNullRefType == true)
                    property.IsRequired = true;
            }

            var apiAllowableValues = pi.FirstAttribute<ApiAllowableValuesAttribute>();
            if (apiAllowableValues != null)
            {
                property.AllowableValues = apiAllowableValues.Values;
                property.AllowableMin = apiAllowableValues.Min;
                property.AllowableMax = apiAllowableValues.Max;
            }
            
            var inputProp = pi.FirstAttribute<InputAttribute>();
            if (inputProp != null)
            {
                property.Input = inputProp.ToInput(c => {
                    c.Required ??= property.IsRequired;
                    c.MinLength ??= property.AllowableMin;
                    c.MaxLength ??= property.AllowableMax;
                });
                if (pi.PropertyType.IsEnum && property.Input.AllowableValues == null) 
                {
                    property.Input.Type ??= Html.Input.Types.Select;
                    if (Html.Input.GetEnumEntries(pi.PropertyType, out var entries))
                        property.Input.AllowableEntries = entries;
                    else
                        property.Input.AllowableValues = entries.Select(x => x.Value).ToArray();
                }
            }
            var css = pi.FirstAttribute<FieldCssAttribute>().ToCss();
            if (css != null)
            {
                property.Input ??= new InputInfo();
                property.Input.Css = css;
            }

            if (instance != null)
            {
                var ignoreValue = ignoreValues != null && ignoreValues.TryGetValue(pi.Name, out var oValue)
                    ? oValue
                    : propType.GetDefaultValue();
                property.Value = PropertyValue(pi, instance, ignoreValue);

                if (pi.GetSetMethod() == null) //ReadOnly is bool? to minimize serialization
                    property.ReadOnly = true;
            }
            return property;
        }

        public static string[] ToGenericArgs(Type propType)
        {
            var genericArgs = propType.IsGenericType
                ? propType.GetGenericArguments().Select(x => x.ExpandTypeName()).ToArray()
                : null;
            return genericArgs;
        }

        public static string PropertyValue(PropertyInfo pi, object instance, object ignoreIfValue=null)
        {
            try
            {
                var value = pi.GetValue(instance, null);
                if (value != null && !value.Equals(ignoreIfValue))
                {
                    return PropertyStringValue(pi, value);
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Could not get value for property '{pi.PropertyType}.{pi.Name}'", ex);
            }

            return null;
        }

        public static string PropertyStringValue(PropertyInfo pi, object value)
        {
            if (pi.PropertyType.IsEnum)
                return pi.PropertyType.Name + "." + value;

            if (pi.PropertyType == typeof(Type))
            {
                var type = (Type) value;
                return $"typeof({type.FullName})";
            }

            var strValue = value as string;
            return strValue ?? value.ToJson();
        }

        public MetadataPropertyType ToProperty(ParameterInfo pi)
        {
            var paramType = pi.ParameterType;
            var underlyingType = Nullable.GetUnderlyingType(paramType) ?? paramType;
            var propertyAttrs = pi.AllAttributes();
            var property = new MetadataPropertyType
            {
                PropertyType = paramType,
                Name = pi.Name,
                Attributes = ToAttributes(propertyAttrs),
                Type = paramType.GetOperationName(),
                IsValueType = underlyingType.IsValueType.NullIfFalse(),
                IsEnum = underlyingType.IsEnum.NullIfFalse(),
                Namespace = paramType.Namespace,
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
                EmitDefaultValue = attr.EmitDefaultValue == false ? false : (bool?)null, //true by default
                Order = attr.Order >= 0 ? attr.Order : (int?)null,
                IsRequired = attr.IsRequired.NullIfFalse(),
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
        public List<MetadataRoute> Routes { get; set; }
        public Func<string> ImplementsFn { get; set; }
        public bool IsRequest { get; set; }
        public bool IsResponse { get; set; }
        public bool IsType { get; set; }
        public bool IsNestedType { get; set; }
        public MetadataOperationType Op { get; set; }
    }

    public static class MetadataExtensions
    {
        public static List<MetadataPropertyType> PopulatePrimaryKey(this List<MetadataPropertyType> props)
        {
            //sync with https://github.com/ServiceStack/ServiceStack.OrmLite/blob/master/src/ServiceStack.OrmLite/OrmLiteConfigExtensions.cs
            var hasPkAttr = props
                .FirstOrDefault(p => p.PropertyType?.HasAttributeCached<PrimaryKeyAttribute>() == true);

            if (hasPkAttr != null)
            {
                hasPkAttr.IsPrimaryKey = true;
            }
            else
            {
                var objProperties = props
                    .Where(x => x.PropertyInfo != null).Map(x => x.PropertyInfo);
                if (objProperties.Count == 0)
                    return props;

                foreach (var prop in props)
                {
                    var propertyInfo = prop.PropertyInfo;
                    if (propertyInfo == null) 
                        continue;
                    
                    var isNullableType = propertyInfo.PropertyType.IsNullableType();
                    
                    var propertyType = isNullableType
                        ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                        : propertyInfo.PropertyType;
                    
                    var referenceAttr = propertyInfo.FirstAttribute<ReferenceAttribute>();
                    var isReference = referenceAttr != null && propertyType?.IsClass == true;
                    var isIgnored = propertyInfo.HasAttributeCached<IgnoreAttribute>() || isReference;

                    var isAutoId = propertyInfo.HasAttributeCached<AutoIdAttribute>();
                    
                    var isPrimaryKey = propertyInfo.Name == Keywords.Id
                       || propertyInfo.HasAttributeNamed(nameof(PrimaryKeyAttribute))
                       || isAutoId;

                    if (isPrimaryKey)
                    {
                        prop.IsPrimaryKey = true;
                        return props;
                    }
                }
            }
            
            return props;
        }

        public static MetadataTypeName ToMetadataTypeName(this MetadataType type)
        {
            if (type == null) return null;

            return new MetadataTypeName
            {
                Type = type.Type,
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
                Type = type.Type,
                Name = type.Name,
                Namespace = type.Namespace,
                GenericArgs = type.GenericArgs
            };
        }

        public static List<MetadataType> GetAllMetadataTypes(this MetadataTypes metadata)
        {
            var allTypes = metadata.Operations.Where(x => x.Request != null).Select(x => x.Request)
                .Union(metadata.Operations.Where(x => x.Response != null).Select(x => x.Response))
                .Union(metadata.Operations.Where(x => x.ReturnType != null).Select(
                    x => x.ReturnType.ToMetadataType()))
                .Union(metadata.Types);
            
            return allTypes.ToList();
        }

        public static HashSet<string> GetReferencedTypeNames(this MetadataType type, MetadataTypes metadataTypes)
        {
            var to = new HashSet<string>();

            void Add(string name)
            {
                if (to.Contains(name))
                    return;
                
                to.Add(name);
                var metaTypes = metadataTypes.Types.Where(x => x.Name == name).ToList();
                if (metaTypes.Count == 1)
                {
                    var metaType = metaTypes[0];
                    if (metaType.Inherits != null)
                    {
                        Add(metaType.Inherits.Name);
                        foreach (var genericArg in metaType.Inherits.GenericArgs.Safe())
                        {
                            Add(genericArg);
                        }
                    }
                    foreach (var iface in metaType.Implements.Safe())
                    {
                        if (!iface.IsSystemOrServiceStackType())
                        {
                            Add(iface.Name);
                        }
                    }
                    foreach (var pi in metaType.Properties.Safe())
                    {
                        Add(pi.Type);
                        foreach (var genericArg in pi.GenericArgs.Safe())
                        {
                            Add(genericArg);
                        }
                    }
                }
            }
            
            if (type.Inherits != null)
            {
                Add(type.Inherits.Name);

                foreach (var genericArg in type.Inherits.GenericArgs.Safe())
                {
                    Add(genericArg);
                }
            }
            foreach (var iface in type.Implements.Safe())
            {
                if (!iface.IsSystemOrServiceStackType())
                {
                    Add(iface.Name);
                }
            }
            foreach (var pi in type.Properties.Safe())
            {
                Add(pi.Type);

                foreach (var genericArg in pi.GenericArgs.Safe())
                {
                    Add(genericArg);
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
            var namespaces = config.DefaultNamespaces.ToSet();
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

        public static void RemoveIgnoredTypesForNet(this MetadataTypes metadata, MetadataTypesConfig config)
        {
            metadata.RemoveIgnoredTypes(config);
            //Don't include Exported Types in System 
            metadata.Types.RemoveAll(x => x.IgnoreSystemType()); 
        }

        public static List<string> RemoveIgnoredTypes(this MetadataTypes metadata, MetadataTypesConfig config)
        {
            var excludeServices = config.ExcludeTypes?.Remove("services") ?? false;
            var excludeTypes = config.ExcludeTypes?.Remove("types") ?? false;
                
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
                    if (!responseType.IgnoreType(config, includeList))
                    {
                        metadata.Types.Add(responseType);
                    }
                }
            }
            
            if (excludeServices)
                metadata.Operations = new List<MetadataOperationType>();
            if (excludeTypes)
                metadata.Types = new List<MetadataType>();
            
            //Ugly but need to revert state so it's ExcludeTypes option is emitted in generated dtos
            if (excludeServices)
                config.ExcludeTypes.Add("services");
            if (excludeTypes)
                config.ExcludeTypes.Add("types");

            return includeList;
        }

        const string NameWithReferencesWildCard = ".*";
        const string NamespaceWildCard = "/*";
        const string ReferencesNameWildCard = "*.";

        public static List<string> GetIncludeList(MetadataTypes metadata, MetadataTypesConfig config)
        {
            if (config.IncludeTypes.IsEmpty())
                return null;

            var includeTypes = config.IncludeTypes.Where(x => !x.StartsWith("{")).ToList();
            
            var namespacedTypes = includeTypes
                .Where(s => s.Length > 2 && s.EndsWith(NamespaceWildCard))
                .Map(s => s.Substring(0, s.Length - 2));

            var explicitTypes = includeTypes
                .Where(x => !x.EndsWith(NamespaceWildCard))
                .ToList();

            var typesToExpand = explicitTypes
                .Where(s => s.Length > 2 && s.EndsWith(NameWithReferencesWildCard))
                .Map(s => s.Substring(0, s.Length - 2));
            
            var reverseTypesToExpand = explicitTypes
                .Where(s => s.Length > 2 && s.StartsWith(ReferencesNameWildCard))
                .Map(s => s.Substring(2)).ToArray();
            explicitTypes.AddRange(reverseTypesToExpand);

            var tags = config.IncludeTypes.Where(x => x.StartsWith("{") && x.EndsWith("}"))
                .SelectMany(x => x.Substring(1, x.Length -2).Split(',')).Distinct().ToArray();

            if (tags.Length > 0)
            {
                var tagTypes = metadata.GetOperationsByTags(tags).Map(x => x.Request.Name);
                if (tagTypes.Count > 0)
                {
                    explicitTypes.AddRange(tagTypes);
                    typesToExpand.AddRange(tagTypes);
                }
            }

            // Operation Return Types should always be included 
            var ops = metadata.Operations.Where(x => explicitTypes.Contains(x.Request.Name));
            foreach (var op in ops)
            {
                if (op.ReturnType != null)
                {
                    typesToExpand.Add(op.ReturnType.Name);
                    foreach (var arg in op.ReturnType.GenericArgs.Safe())
                    {
                        typesToExpand.Add(arg);
                    }
                }
            }

            if (typesToExpand.Count != 0 || namespacedTypes.Count != 0)
            {
                var includeTypesInNamespace = namespacedTypes.Count > 0
                    ? metadata.GetAllMetadataTypes()
                        .Where(x => namespacedTypes.Any(ns => x.Namespace?.StartsWith(ns) == true))
                        .Select(x => x.Name)
                        .ToSet()
                    : TypeConstants<string>.EmptyHashSet;

                var includedMetadataOperations = metadata.Operations
                    .Where(t => typesToExpand.Contains(t.Request.Name))
                    .ToList();

                var includedMetadataTypes = includedMetadataOperations
                    .Select(o => o.Request)
                    .ToList();

                var includeSet = includedMetadataTypes
                    .Where(x => x.RequestType?.ReturnType != null)
                    .Select(x => x.RequestType.ReturnType.Name)
                    .ToSet();

                var includedResponses = metadata.Operations
                    .Where(t => typesToExpand.Contains(t.Request.Name) && t.Response != null)
                    .Select(o => o.Response)
                    .ToList();
                includedResponses.ForEach(x => includeSet.Add(x.Name));

                var includedTypeNames = includedMetadataTypes
                    .Where(x => x.Implements?.Length > 0)
                    .SelectMany(x => x.Implements.Select(i => i.Name))
                    .ToSet();
                foreach (var op in includedMetadataOperations)
                {
                    if (op.ReturnType != null)
                        includedTypeNames.Add(typeof(IReturn<>).Name);
                    if (op.ReturnsVoid == true)
                        includedTypeNames.Add(nameof(IReturnVoid));
                }
                foreach (var metaType in metadata.Types)
                {
                    if (includedTypeNames.Contains(metaType.Name))
                        includedMetadataTypes.Add(metaType);
                }

                var returnTypesForInclude = metadata.Operations
                    .Where(x => x.Response != null && includeSet.Contains(x.Response.Name))
                    .Map(x => x.Response);

                var crudInterfaces = AutoCrudOperation.CrudInterfaceMetadataNames().ToSet();
                var crudTypeNamesForInclude = metadata.Operations
                    .Where(t => typesToExpand.Contains(t.Request.Name))
                    .SelectMany(x => x.Request.Implements)
                    .Where(x => x != null && crudInterfaces.Contains(x.Name))
                    .Map(x => x.GenericArgs[0])
                    .ToSet()
                    .ToList();
                var reverseTypeReferencesToInclude = metadata.Operations
                    .Where(x => x.ReferencesAny(reverseTypesToExpand))
                    .Map(x => x.Request.Name);

                // GetReferencedTypes for both request + response objects
                var referenceTypes = includedMetadataTypes
                    .Union(returnTypesForInclude)
                    .Where(x => x != null)
                    .SelectMany(x => x.GetReferencedTypeNames(metadata))
                    .ToList();

                var returnTypesForIncludeNames = returnTypesForInclude.Map(x => x.Name);

                var ret = referenceTypes
                    .Union(explicitTypes)
                    .Union(includeTypesInNamespace)
                    .Union(typesToExpand)
                    .Union(includedTypeNames)
                    .Union(crudTypeNamesForInclude)
                    .Union(reverseTypeReferencesToInclude)
                    .Union(returnTypesForIncludeNames)
                    .Distinct()
                    .ToList();
                return ret;
            }

            // From IncludeTypes get the corresponding MetadataTypes
            return includeTypes;
        }

        public static bool IgnoreType(this MetadataType type, MetadataTypesConfig config, List<string> overrideIncludeType = null)
        {
            if (config.ForceInclude(type))
                return false;
            
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
                Attribute = route.RouteAttribute,
                Name = "Route",
                ConstructorArgs = new List<MetadataPropertyType>
                {
                    new MetadataPropertyType { Type = "string", Value = route.Path },
                },
            };

            if (route.Verbs != null)
            {
                attr.ConstructorArgs.Add(
                    new MetadataPropertyType {
                        PropertyType = typeof(string),
                        Type = "string", 
                        Value = route.Verbs,
                    });
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

        public static List<MetadataType> GetAllTypesOrdered(this MetadataTypes metadata)
        {
            // Base Types need to be written first
            var types = metadata.Types.CreateSortedTypeList();

            var allTypes = new List<MetadataType>();
            allTypes.AddRange(types);
            allTypes.AddRange(metadata.Operations
                .Where(x => x.Response != null)
                .Select(x => x.Response)
                .Distinct());
            allTypes.AddRange(metadata.Operations.Select(x => x.Request));
            return allTypes.Distinct().ToList();
        }

        public static List<MetadataType> CreateSortedTypeList(this List<MetadataType> allTypes)
        {
            var result = new List<MetadataType>();
            foreach (var metadataType in allTypes)
            {
                AddTypeToSortedList(allTypes, result, metadataType);
            }
            return result;
        }

        private static void AddTypeToSortedList(List<MetadataType> allTypes, List<MetadataType> sortedTypes, MetadataType metadataType)
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
            AddTypeToSortedList(allTypes, sortedTypes, inheritedMetadataType);

            if (!sortedTypes.Contains(metadataType))
                sortedTypes.Add(metadataType);
        }

        private static MetadataType FindMetadataTypeByMetadataTypeName(List<MetadataType> allTypes,
            MetadataTypeName metadataTypeName)
        {
            if (metadataTypeName == null)
                return null;
            var metaDataType = allTypes.Where(x => x.Name == metadataTypeName.Name &&
                                              x.Namespace == metadataTypeName.Namespace)
                .FirstNonDefault();
            return metaDataType;
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

                var returnMarker = type.RequestType?.ReturnType;
                if (returnMarker != null)
                {
                    if (!returnMarker.GenericArgs.IsEmpty())
                        type.RequestType.ReturnType.GenericArgs.Each(x => deps.Push(typeName, x));
                    else
                        deps.Push(typeName, returnMarker.Name);
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
                if (!typesMap.TryGetValue(typeDep, out var depType)
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

            if (prop.IsSystemType() == true)
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

                return typeInfo != null && typeInfo.IsSystemType() != true && typeInfo.IsEnum != true
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

        public static string GetEnumMemberValue(this MetadataType type, int i)
        {
            return type.EnumMemberValues != null && i < type.EnumMemberValues.Count
                ? type.EnumMemberValues[i]
                : null;
        }

        public static string GetAttributeName(this Attribute attr) => 
            StringUtils.RemoveSuffix(attr.GetType().Name, "Attribute");

        public static Type[] GetDirectInterfaces(this Type type)
        {
            var childInterfaces = new List<Type>();
            if (type.BaseType != typeof(object) && type.BaseType != null)
            {
                foreach (var childIface in type.BaseType.GetInterfaces())
                {
                    childInterfaces.Add(childIface);
                }
            }
            foreach(var iface in type.GetInterfaces())
            {
                foreach (var childIface in iface.GetInterfaces())
                {
                    childInterfaces.Add(childIface);
                }
            }
            var directInterfaces = type.GetInterfaces().Except(childInterfaces);
            return directInterfaces.ToArray();
        }

        public static void Emit(this StringBuilderWrapper sb, MetadataType type, Lang lang)
        {
            var attrs = type.Type?.AllAttributes<EmitCodeAttribute>()
                ?.Where(x => x.Lang.HasFlag(lang));
            if (attrs != null)
            {
                foreach (var attr in attrs)
                {
                    foreach (var statement in attr.Statements)
                    {
                        sb.AppendLine(statement);
                    }
                }
            }
        }
        
        public static void Emit(this StringBuilderWrapper sb, MetadataPropertyType propType, Lang lang)
        {
            var attrs = propType.PropertyInfo?.AllAttributes<EmitCodeAttribute>()
                ?.Where(x => x.Lang.HasFlag(lang));
            if (attrs != null)
            {
                foreach (var attr in attrs)
                {
                    foreach (var statement in attr.Statements)
                    {
                        sb.AppendLine(statement);
                    }
                }
            }
        }

        internal static string EnsureSuffix(this string s, char suffix) =>
            s == null ? null : s[s.Length - 1] == suffix ? s : s + suffix;
    }
}