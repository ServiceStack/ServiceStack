using System.Threading.Tasks;
using System.Web;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace ServiceStack.Metadata
{
    public class MetadataTypesHandler : HttpHandlerBase
    {
        public MetadataTypesConfig Config { get; set; }

        public override void Execute(HttpContextBase context)
        {
            var request = context.ToRequest(GetType().GetOperationName());
            ProcessRequestAsync(request, request.Response, request.OperationName);
        }

        public override bool RunAsAsync()
        {
            return true;
        }

        public override Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var metadata = DtoGenFeature.GetMetadataTypes(Config, httpReq);

            return httpRes.WriteToResponse(httpReq, metadata);
        }

    }

    public static class MetadataTypeExtensions
    {
        public static MetadataType ToType(this Type type)
        {
            if (type == null) return null;

            var metaType = new MetadataType
            {
                Name = type.GetOperationName(),
                Namespace = type.Namespace,
                GenericArgs = type.IsGenericType
                    ? type.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                    : null,
                Attributes = type.ToAttributes(),
                Properties = type.ToProperties(),
            };

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                metaType.Inherits = type.BaseType.GetOperationName();
                metaType.InheritsGenericArgs = type.BaseType.IsGenericType
                    ? type.BaseType.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                    : null;
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
                    metaType.ReturnMarkerGenericArgs = genericMarker.GetGenericArguments().Select(x => x.GetOperationName()).ToArray();
                }
            }

            var routeAttrs = type.AllAttributes<RouteAttribute>().ToList();
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

        public static List<MetadataAttribute> ToAttributes(this Type type)
        {
            return !type.IsUserType() || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                ? null
                : type.GetCustomAttributes(false).ToAttributes();
        }

        public static List<MetadataPropertyType> ToProperties(this Type type)
        {
            var props = !type.IsUserType() || type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                ? null
                : type.GetInstancePublicProperties().ToList().ConvertAll(x => x.ToProperty());

            return props == null || props.Count == 0 ? null : props;
        }

        public static bool ExcludeKnownAttrsFilter(Attribute x)
        {
            return x.GetType() != typeof(RouteAttribute)
                && x.GetType().Name != "DescriptionAttribute"
                && x.GetType().Name != "DataContractAttribute"  //Type equality issues with Mono .NET 3.5/4
                && x.GetType().Name != "DataMemberAttribute";
        }

        public static List<MetadataAttribute> ToAttributes(this object[] attrs)
        {
            var to = attrs.OfType<Attribute>()
                .Where(ExcludeKnownAttrsFilter)
                .ToList().ConvertAll(x => x.ToAttribute());

            return to.Count == 0 ? null : to;
        }

        public static List<MetadataAttribute> ToAttributes(this IEnumerable<Attribute> attrs)
        {
            var to = attrs
                .Where(ExcludeKnownAttrsFilter)
                .Select(attr => attr.ToAttribute())
                .ToList();
            return to.Count == 0 ? null : to;
        }

        public static MetadataAttribute ToAttribute(this Attribute attr)
        {
            var firstCtor = attr.GetType().GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault();
            var metaAttr = new MetadataAttribute
            {
                Name = attr.GetType().Name,
                ConstructorArgs = firstCtor != null
                    ? firstCtor.GetParameters().ToList().ConvertAll(x => x.ToProperty())
                    : null,
                Args = attr.NonDefaultProperties(),
            };
            return metaAttr;
        }

        public static List<MetadataPropertyType> NonDefaultProperties(this Attribute attr)
        {
            return attr.GetType().GetPublicProperties()
                .Select(pi => pi.ToProperty(attr))
                .Where(property => property.Name != "TypeId"
                    && property.Value != null)
                .ToList();
        }

        public static MetadataPropertyType ToProperty(this PropertyInfo pi, object instance = null)
        {
            var property = new MetadataPropertyType
            {
                Name = pi.Name,
                Attributes = pi.GetCustomAttributes(false).ToAttributes(),
                Type = pi.PropertyType.GetOperationName(),
                DataMember = pi.GetDataMember().ToDataMember(),
                GenericArgs = pi.PropertyType.IsGenericType
                    ? pi.PropertyType.GetGenericArguments().Select(x => x.GetOperationName()).ToArray()
                    : null,
            };
            if (instance != null)
            {
                var value = pi.GetValue(instance, null);
                if (value != pi.PropertyType.GetDefaultValue())
                {
                    property.Value = value.ToJson();
                }
            }
            return property;
        }

        public static MetadataPropertyType ToProperty(this ParameterInfo pi)
        {
            var propertyAttrs = pi.AllAttributes();
            var property = new MetadataPropertyType
            {
                Name = pi.Name,
                Attributes = propertyAttrs.ToAttributes(),
                Type = pi.ParameterType.GetOperationName(),
                Description = pi.GetDescription(),
            };

            return property;
        }

        public static MetadataDataMember ToDataMember(this DataMemberAttribute attr)
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

        public static PropertyInfo[] GetInstancePublicProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(t => t.GetIndexParameters().Length == 0) // ignore indexed properties
                .ToArray();
        }
    }
}