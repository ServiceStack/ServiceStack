using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using System.Threading;

namespace ServiceStack.WebHost.Endpoints.Utils
{
    public static class FilterAttributeCache
    {
		private static Dictionary<Type, IHasRequestFilter[]> requestFilterAttributes
            = new Dictionary<Type, IHasRequestFilter[]>();

		private static Dictionary<Type, IHasResponseFilter[]> responseFilterAttributes
            = new Dictionary<Type, IHasResponseFilter[]>();

        private static IHasRequestFilter[] ShallowCopy(this IHasRequestFilter[] filters)
        {
            var to = new IHasRequestFilter[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                to[i] = filters[i].Copy();
            }
            return to;
        }

        private static IHasResponseFilter[] ShallowCopy(this IHasResponseFilter[] filters)
        {
            var to = new IHasResponseFilter[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                to[i] = filters[i].Copy();
            }
            return to;
        }

        public static IHasRequestFilter[] GetRequestFilterAttributes(Type requestDtoType)
        {
        	IHasRequestFilter[] attrs;
            if (requestFilterAttributes.TryGetValue(requestDtoType, out attrs)) return attrs.ShallowCopy();

			var attributes = new List<IHasRequestFilter>(
				(IHasRequestFilter[])requestDtoType.GetCustomAttributes(typeof(IHasRequestFilter), true));

            var serviceType = EndpointHost.Metadata.GetServiceTypeByRequest(requestDtoType);
            if ( serviceType != null )
			    attributes.AddRange((IHasRequestFilter[])serviceType.GetCustomAttributes(typeof(IHasRequestFilter), true));

			attributes.Sort((x,y) => x.Priority - y.Priority);
			attrs = attributes.ToArray();

            Dictionary<Type, IHasRequestFilter[]> snapshot, newCache;
            do
            {
                snapshot = requestFilterAttributes;
                newCache = new Dictionary<Type, IHasRequestFilter[]>(requestFilterAttributes);
				newCache[requestDtoType] = attrs;

            } while (!ReferenceEquals(
            Interlocked.CompareExchange(ref requestFilterAttributes, newCache, snapshot), snapshot));

            return attrs.ShallowCopy();
        }

        public static IHasResponseFilter[] GetResponseFilterAttributes(Type responseDtoType)
        {
			IHasResponseFilter[] attrs;
            if (responseFilterAttributes.TryGetValue(responseDtoType, out attrs)) return attrs.ShallowCopy();

			var attributes = new List<IHasResponseFilter>(
	            (IHasResponseFilter[])responseDtoType.GetCustomAttributes(typeof(IHasResponseFilter), true));

        	var serviceType = EndpointHost.Metadata.GetServiceTypeByResponse(responseDtoType);
			if (serviceType != null)
			{
				attributes.AddRange(
					(IHasResponseFilter[])serviceType.GetCustomAttributes(typeof(IHasResponseFilter), true));
			}

			attributes.Sort((x, y) => x.Priority - y.Priority);
			attrs = attributes.ToArray();

            Dictionary<Type, IHasResponseFilter[]> snapshot, newCache;
            do
            {
                snapshot = responseFilterAttributes;
                newCache = new Dictionary<Type, IHasResponseFilter[]>(responseFilterAttributes);
				newCache[responseDtoType] = attrs;

            } while (!ReferenceEquals(
            Interlocked.CompareExchange(ref responseFilterAttributes, newCache, snapshot), snapshot));

            return attrs.ShallowCopy();
        }
    }
}
