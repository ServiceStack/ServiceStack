using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Web;

namespace ServiceStack.Support.WebHost
{
    public static class FilterAttributeCache
    {
        private static Dictionary<Type, IRequestFilterBase[]> requestFilterAttributes
            = new Dictionary<Type, IRequestFilterBase[]>();

        private static Dictionary<Type, IResponseFilterBase[]> responseFilterAttributes
            = new Dictionary<Type, IResponseFilterBase[]>();

        private static IRequestFilterBase[] ShallowCopy(this IRequestFilterBase[] filters)
        {
            var to = new IRequestFilterBase[filters.Length];
            for (var i = 0; i < filters.Length; i++)
            {
                to[i] = filters[i].Copy();
            }
            return to;
        }

        private static IResponseFilterBase[] ShallowCopy(this IResponseFilterBase[] filters)
        {
            var to = new IResponseFilterBase[filters.Length];
            for (var i = 0; i < filters.Length; i++)
            {
                to[i] = filters[i].Copy();
            }
            return to;
        }

        public static IRequestFilterBase[] GetRequestFilterAttributes(Type requestDtoType)
        {
            if (requestFilterAttributes.TryGetValue(requestDtoType, out var attrs)) 
                return attrs.ShallowCopy();

            var attributes = requestDtoType.AllAttributes().OfType<IRequestFilterBase>().ToList();

            var serviceType = HostContext.Metadata.GetServiceTypeByRequest(requestDtoType);
            if (serviceType != null)
            {
                attributes.AddRange(serviceType.AllAttributes().OfType<IRequestFilterBase>());
            }

            attributes.Sort((x, y) => x.Priority - y.Priority);
            attrs = attributes.ToArray();

            Dictionary<Type, IRequestFilterBase[]> snapshot, newCache;
            do
            {
                snapshot = requestFilterAttributes;
                newCache = new Dictionary<Type, IRequestFilterBase[]>(requestFilterAttributes) { [requestDtoType] = attrs };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref requestFilterAttributes, newCache, snapshot), snapshot));

            return attrs.ShallowCopy();
        }

        public static IResponseFilterBase[] GetResponseFilterAttributes(Type requestDtoType)
        {
            if (responseFilterAttributes.TryGetValue(requestDtoType, out var attrs)) 
                return attrs.ShallowCopy();

            var attributes = requestDtoType.AllAttributes().OfType<IResponseFilterBase>().ToList();

            var serviceType = HostContext.Metadata.GetServiceTypeByRequest(requestDtoType);
            if (serviceType != null)
            {
                attributes.AddRange(serviceType.AllAttributes().OfType<IResponseFilterBase>());
            }

            attributes.Sort((x, y) => x.Priority - y.Priority);
            attrs = attributes.ToArray();

            Dictionary<Type, IResponseFilterBase[]> snapshot, newCache;
            do
            {
                snapshot = responseFilterAttributes;
                newCache = new Dictionary<Type, IResponseFilterBase[]>(responseFilterAttributes) {
                    [requestDtoType] = attrs
                };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref responseFilterAttributes, newCache, snapshot), snapshot));

            return attrs.ShallowCopy();
        }
    }
}
