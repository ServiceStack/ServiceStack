using System.Net;

namespace ServiceStack
{
    public static class ClientConfig
    {
        public static bool SkipEmptyArrays { get; set; } = false;

        public static void ConfigureTls12()
        {
            //https://githubengineering.com/crypto-removal-notice/
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}