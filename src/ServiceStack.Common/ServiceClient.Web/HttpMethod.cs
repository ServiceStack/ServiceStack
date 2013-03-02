using System;

namespace ServiceStack.ServiceClient.Web
{
    [Obsolete("Moved to ServiceStack.Common.Web.HttpMethods")]
    public static class HttpMethod
    {
        public const string Get = "GET";
        public const string Put = "PUT";
        public const string Post = "POST";
        public const string Delete = "DELETE";
        public const string Options = "OPTIONS";
        public const string Head = "HEAD";
        public const string Patch = "PATCH";
    }
}