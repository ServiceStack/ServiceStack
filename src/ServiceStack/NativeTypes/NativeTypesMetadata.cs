using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Host;
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
                AddReturnMarker = req.AddReturnMarker ?? defaults.AddReturnMarker,
                AddDescriptionAsComments = req.AddDescriptionAsComments ?? defaults.AddDescriptionAsComments,
                AddDataContractAttributes = req.AddDataContractAttributes ?? defaults.AddDataContractAttributes,
                AddDataAnnotationAttributes =
                    req.AddDataAnnotationAttributes ?? defaults.AddDataAnnotationAttributes,
                MakeDataContractsExtensible =
                    req.MakeDataContractsExtensible ?? defaults.MakeDataContractsExtensible,
                AddIndexesToDataMembers = req.AddIndexesToDataMembers ?? defaults.AddIndexesToDataMembers,
                InitializeCollections = req.InitializeCollections ?? defaults.InitializeCollections,
                AddImplicitVersion = req.AddImplicitVersion ?? defaults.AddImplicitVersion,
                AddResponseStatus = req.AddResponseStatus ?? defaults.AddResponseStatus,
                AddDefaultXmlNamespace = req.AddDefaultXmlNamespace ?? defaults.AddDefaultXmlNamespace,
                DefaultNamespaces = req.DefaultNamespaces ?? defaults.DefaultNamespaces,
                IgnoreTypes = defaults.IgnoreTypes,
                IgnoreTypesInNamespaces = defaults.IgnoreTypesInNamespaces,
                TypeAlias = defaults.TypeAlias,
            };
        }

        public MetadataTypes GetMetadataTypes(IRequest req, MetadataTypesConfig config = null)
        {
            return new MetadataTypesGenerator(meta, config ?? defaults).GetMetadataTypes(req);
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

        public MetadataTypes GetMetadataTypes(IRequest req)
        {
            var metadata = new MetadataTypes
            {
                Config = config,
            };

            var skipTypes = config.IgnoreTypes ?? new HashSet<Type>();
            var opTypes = new HashSet<Type>();
            var ignoreNamespaces = config.IgnoreTypesInNamespaces ?? new List<string>();

            foreach (var operation in meta.Operations)
            {
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

            foreach (var type in meta.GetAllTypes())
            {
                if (opTypes.Contains(type))
                    continue;
                if (skipTypes.Contains(type))
                    continue;
                if (ignoreNamespaces.Contains(type.Namespace))
                    continue;

                metadata.Operations.Add(new MetadataOperationType
                {
                    Request = ToType(type),
                });

                opTypes.Add(type);
            }

            var considered = new HashSet<Type>(opTypes);
            var queue = new Queue<Type>(opTypes);

            Func<Type, bool> ignoreTypeFn = t => 
                t == null 
                || considered.Contains(t)
                || skipTypes.Contains(t)
                || ignoreNamespaces.Contains(t.Namespace);

            Action<Type> registerTypeFn = t => {
                considered.Add(t);
                queue.Enqueue(t);
                if (t.IsUserType())
                    metadata.Types.Add(ToType(t));
            };

            while (queue.Count > 0)
            {
                var type = queue.Dequeue();
                foreach (var pi in type.GetSerializableProperties()
                    .Where(pi => !ignoreTypeFn(pi.PropertyType)))
                {
                    registerTypeFn(pi.PropertyType);
                }

                if (!ignoreTypeFn(type.BaseType))
                {
                    registerTypeFn(type.BaseType);
                }

                if (!type.IsGenericType()) continue;
                
                var args = type.GetGenericArguments();
                foreach (var arg in args.Where(arg => !ignoreTypeFn(arg)))
                {
                    registerTypeFn(arg);
                }
            }

            return metadata;
        }

        public MetadataTypeName ToTypeName(Type type)
        {
            if (type == null) return null;

            return new MetadataTypeName
            {
                Name = type.GetOperationName(),
                GenericArgs = type.IsGenericType
                    ? type.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                    : null,
            };
        }

        public MetadataType ToType(Type type)
        {
            if (type == null) return null;

            var metaType = new MetadataType
            {
                Name = type.GetOperationName(),
                Namespace = type.Namespace,
                GenericArgs = type.IsGenericType
                    ? type.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                    : null,
                Attributes = ToAttributes(type),
                Properties = ToProperties(type),
            };

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                metaType.Inherits = new MetadataTypeName 
                {
                    Name = type.BaseType.GetOperationName(),
                    GenericArgs = type.BaseType.IsGenericType
                        ? type.BaseType.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                        : null
                };
            }

            if (type.GetTypeWithInterfaceOf(typeof(IReturnVoid)) != null)
            {
                metaType.ReturnVoidMarker = true;
            }
            else
            {
                var genericMarker = type.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                if (genericMarker != null)
                {
                    metaType.ReturnMarkerTypeName = ToTypeName(genericMarker.GetGenericArguments().First());
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

            return metaType;
        }

        public List<MetadataAttribute> ToAttributes(Type type)
        {
            return !type.IsUserType() || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                ? null
                : ToAttributes(type.GetCustomAttributes(false));
        }

        public List<MetadataPropertyType> ToProperties(Type type)
        {
            var props = !type.IsUserType() || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                ? null
                : GetInstancePublicProperties(type).Select(x => ToProperty(x)).ToList();

            return props == null || props.Count == 0 ? null : props;
        }

        public bool ExcludeAttrsFilter(Attribute x)
        {
            var type = x.GetType();
            var name = type.Name;
            return type != typeof(RouteAttribute)
                && name != "DescriptionAttribute"
                && name != "DataContractAttribute"  //Type equality issues with Mono .NET 3.5/4
                && name != "DataMemberAttribute"
                && (config.AddDataAnnotationAttributes || type.Namespace != "ServiceStack.DataAnnotations");
        }

        public List<MetadataAttribute> ToAttributes(object[] attrs)
        {
            var to = attrs.OfType<Attribute>()
                .Where(ExcludeAttrsFilter)
                .Select(ToAttribute)
                .ToList();

            return to.Count == 0 ? null : to;
        }

        public List<MetadataAttribute> ToAttributes(IEnumerable<Attribute> attrs)
        {
            var to = attrs
                .Where(ExcludeAttrsFilter)
                .Select(ToAttribute)
                .ToList();

            return to.Count == 0 ? null : to;
        }

        public MetadataAttribute ToAttribute(Attribute attr)
        {
            var firstCtor = attr.GetType().GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault();
            var metaAttr = new MetadataAttribute
            {
                Name = attr.GetType().Name.Replace("Attribute", ""),
                ConstructorArgs = firstCtor != null
                    ? firstCtor.GetParameters().ToList().ConvertAll(ToProperty)
                    : null,
                Args = NonDefaultProperties(attr),
            };
            return metaAttr;
        }

        public List<MetadataPropertyType> NonDefaultProperties(Attribute attr)
        {
            return attr.GetType().GetPublicProperties()
                .Select(pi => ToProperty(pi, attr))
                .Where(property => property.Name != "TypeId"
                    && property.Value != null)
                .ToList();
        }

        public MetadataPropertyType ToProperty(PropertyInfo pi, object instance = null)
        {
            var property = new MetadataPropertyType
            {
                Name = pi.Name,
                Attributes = ToAttributes(pi.GetCustomAttributes(false)),
                Type = pi.PropertyType.GetOperationName(),
                DataMember = ToDataMember(pi.GetDataMember()),
                GenericArgs = pi.PropertyType.IsGenericType
                    ? pi.PropertyType.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                    : null,
            };
            if (instance != null)
            {
                var value = pi.GetValue(instance, null);
                if (value != pi.PropertyType.GetDefaultValue())
                {
                    var strValue = value as string;
                    property.Value = strValue ?? value.ToJson();
                }
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
                .Where(t => t.GetIndexParameters().Length == 0) // ignore indexed properties
                .ToArray();
        }
    }
}