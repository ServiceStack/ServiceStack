#if !SILVERLIGHT && !XBOX
using ServiceStack.ServiceHost;

namespace ServiceStack.Common
{
    public static class RequestContextExtensions
    {
        /// <summary>
        /// Store an entry in the IHttpRequest.Items Dictionary
        /// </summary>
        public static void SetItem(this IRequestContext requestContext, string key, object value)
        {
            if (requestContext == null) return;
            requestContext.Get<IHttpRequest>().SetItem(key, value);
        }

        /// <summary>
        /// Store an entry in the IHttpRequest.Items Dictionary
        /// </summary>
        public static void SetItem(this IHttpRequest httpReq, string key, object value)
        {
            if (httpReq == null) return;

            httpReq.Items[key] = value;
        }

        /// <summary>
        /// Get an entry from the IHttpRequest.Items Dictionary
        /// </summary>
        public static object GetItem(this IRequestContext requestContext, string key)
        {
            return requestContext == null ? null : requestContext.Get<IHttpRequest>().GetItem(key);
        }

        /// <summary>
        /// Get an entry from the IHttpRequest.Items Dictionary
        /// </summary>
        public static object GetItem(this IHttpRequest httpReq, string key)
        {
            if (httpReq == null) return null;

            object value;
            httpReq.Items.TryGetValue(key, out value);
            return value;
        }
    }
}
#endif