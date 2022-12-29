using ServiceStack.Redis;

namespace ConsoleTests
{
    public class GoogleRedisSentinelFailoverTests : RedisSentinelFailoverTests
    {
        //gcloud compute instances list
        //url: https://cloud.google.com/sdk/gcloud/reference/compute/instances/list
        public static string[] SentinelHosts = new[]
        {
            "130.211.149.172",
            "130.211.191.163",
            "146.148.61.165",
        };

        protected override RedisSentinel CreateSentinel()
        {
            var sentinel = new RedisSentinel(SentinelHosts, "master")
            {
                IpAddressMap =
                {
                    {"10.240.109.243", "130.211.149.172"},
                    {"10.240.201.29", "130.211.191.163"},
                    {"10.240.200.252", "146.148.61.165"},
                }
            };
            return sentinel;
        }
    }
}