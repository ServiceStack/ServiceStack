using System;

namespace ServiceStack.OpenApi.Tests.Host
{
    public class Config
    {
        public static readonly string ServiceStackBaseUri = Environment.GetEnvironmentVariable("CI_BASEURI") ?? "http://localhost:20000";
        public static readonly string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public static readonly string ListeningOn = ServiceStackBaseUri + "/";
    }
}
