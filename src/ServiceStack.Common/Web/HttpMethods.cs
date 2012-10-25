using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common.Web
{
    public static class HttpMethods
    {
        public const string Get = "GET";
        public const string Put = "PUT";
        public const string Post = "POST";
        public const string Delete = "DELETE";
        public const string Head = "HEAD";
        public const string Options = "OPTIONS";
        public const string Patch = "PATCH";

        public static EndpointAttributes GetEndpointAttribute(string httpMethod)
        {
            switch (httpMethod.ToUpper())
            {
                case Get:
                    return EndpointAttributes.HttpGet;
                case Put:
                    return EndpointAttributes.HttpPut;
                case Post:
                    return EndpointAttributes.HttpPost;
                case Delete:
                    return EndpointAttributes.HttpDelete;
                case Patch:
                    return EndpointAttributes.HttpPatch;
                case Head:
                    return EndpointAttributes.HttpHead;
                case Options:
                    return EndpointAttributes.HttpOptions;
            }

            return EndpointAttributes.None;
        }
    }
}
