using NUnit.Framework;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class CookieTests
    {
        [Test]
        public void Handles_malicious_php_cookies()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri)
            {
                StoreCookies = false,
                RequestFilter = r => r.Headers["Cookie"] = "$Version=1; $Path=/; $Path=/; RealCookie=choc-chip"
            };
            //client.Headers.Add("Cookie", "$Version=1; $Path=/; $Path=/");

            var response = client.Get(new Cookies());
            Assert.That(response.RequestCookieNames, Contains.Item("RealCookie"));
        }
    }
}