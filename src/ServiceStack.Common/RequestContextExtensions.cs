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
            var httpReq = requestContext.Get<IHttpRequest>();
            if (httpReq != null)
                httpReq.Items[key] = value;
        }

        /// <summary>
        /// Get an entry from the IHttpRequest.Items Dictionary
        /// </summary>
        public static object GetItem(this IRequestContext requestContext, string key)
        {
            if (requestContext == null) return null;
            object value = null;
            var httpReq = requestContext.Get<IHttpRequest>();
            if (httpReq != null)
                httpReq.Items.TryGetValue(key, out value);
            return value;
        }
    }
}