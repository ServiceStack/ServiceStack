using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
    [Explicit]
    public class OAuthIntegrationTests
    {
        [Test]
        public void Can_transfer_access_tokens_to_server()
        {
            var client = new JsonServiceClient("http://localhost:11001/");

            var request = new Authenticate
            {
                provider = "twitter",
                AccessToken = "2931572242-zmVKk5leFHJXJWRUpQqyEkdlRlNbDMjNlUcXViJ",
                AccessTokenSecret = "wt7BL444VIG8WPp1g071WO7Z9diIi34iQRnXQ28umcR50"
            };

            var response = client.Post(request);

            response.PrintDump();

            Assert.That(response.UserId, Is.Not.Null);
            Assert.That(response.SessionId, Is.Not.Null);
            Assert.That(response.DisplayName, Is.EqualTo("TechStacks"));
        }
    }
}