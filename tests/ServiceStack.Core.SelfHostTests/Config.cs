namespace ServiceStack.Core.SelfHostTests
{
    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public const string AspNetBaseUri = "http://localhost:50000/";
        public const string AspNetServiceStackBaseUri = AspNetBaseUri + "api";
    }
}