using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceHost;
using System.Threading;

namespace ServiceStack.WebHost.EndPoints.Utils
{
    public static class FilterAttributeCache
    {
        private static Dictionary<Type, IEnumerable<IHasRequestFilter>> requestFilterAttributes
            = new Dictionary<Type, IEnumerable<IHasRequestFilter>>();

        private static Dictionary<Type, IEnumerable<IHasResponseFilter>> responseFilterAttributes
            = new Dictionary<Type, IEnumerable<IHasResponseFilter>>();

        public static IEnumerable<IHasRequestFilter> GetRequestFilterAttributes(Type requestDtoType)
        {
            IEnumerable<IHasRequestFilter> attributes;
            if (requestFilterAttributes.TryGetValue(requestDtoType, out attributes)) return attributes;

            attributes = (IHasRequestFilter[])requestDtoType.GetCustomAttributes(typeof(IHasRequestFilter), true);

            Dictionary<Type, IEnumerable<IHasRequestFilter>> snapshot, newCache;
            do
            {
                snapshot = requestFilterAttributes;
                newCache = new Dictionary<Type, IEnumerable<IHasRequestFilter>>(requestFilterAttributes);
                newCache[requestDtoType] = attributes;

            } while (!ReferenceEquals(
            Interlocked.CompareExchange(ref requestFilterAttributes, newCache, snapshot), snapshot));

            return attributes;
        }

        public static IEnumerable<IHasResponseFilter> GetResponseFilterAttributes(Type responseDtoType)
        {
            IEnumerable<IHasResponseFilter> attributes;
            if (responseFilterAttributes.TryGetValue(responseDtoType, out attributes)) return attributes;

            attributes = (IHasResponseFilter[])responseDtoType.GetCustomAttributes(typeof(IHasResponseFilter), true);

            Dictionary<Type, IEnumerable<IHasResponseFilter>> snapshot, newCache;
            do
            {
                snapshot = responseFilterAttributes;
                newCache = new Dictionary<Type, IEnumerable<IHasResponseFilter>>(responseFilterAttributes);
                newCache[responseDtoType] = attributes;

            } while (!ReferenceEquals(
            Interlocked.CompareExchange(ref responseFilterAttributes, newCache, snapshot), snapshot));

            return attributes;
        }
    }
}
