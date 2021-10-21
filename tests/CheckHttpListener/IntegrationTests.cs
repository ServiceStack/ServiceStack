using System.Threading.Tasks;
using Check.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace CheckHttpListener
{
    public class IntegrationTests
    {
        [Test]
        public void Can_send_QueryPosts()
        {
            var client = new JsonServiceClient("https://techstacks.io");
            // var client = new JsonServiceClient("https://localhost:5001");

            var response = client.Send(new QueryPosts {
                Ids = new[] { 1001, 6860, 6848 },
                OrderByDesc = "Points",
                Take = 3,
            });
            response.PrintDump();
        }
        
        [Route("/testauth")]
        public partial class TestAuth
            : IReturn<TestAuthResponse>
        {
        }

        public partial class TestAuthResponse
        {
            public virtual string UserId { get; set; }
            public virtual string SessionId { get; set; }
            public virtual string UserName { get; set; }
            public virtual string DisplayName { get; set; }
            public virtual ResponseStatus ResponseStatus { get; set; }
        }
        static string TestUrl = "http://test.servicestack.net";
        // static string TestUrl = "https://localhost:5001";

        [Test]
        public void Can_authenticate_via_HTTP_BasicAuth()
        {
            var client = new JsonServiceClient(TestUrl) {
                UserName = "test",
                Password = "test",
                AlwaysSendBasicAuthHeader = true
            };

            var response = client.Get(new TestAuth());
            response.PrintDump();
        }
        
        [ValidateIsAuthenticated]
        [Route("/secured")]
        public class Secured : IReturn<SecuredResponse>
        {
            public string Name { get; set; }
        }

        public class SecuredResponse
        {
            public string Result { get; set; }

            public ResponseStatus ResponseStatus { get; set; }
        }
        
        [Route("/jwt-invalidate")]
        public class InvalidateLastAccessToken : IReturn<EmptyResponse> {}

        [Test]
        public async Task Does_fetch_AccessToken_using_RefreshTokenCookies_ServiceClient()
        {
            await AssertDoesGetAccessTokenUsingRefreshTokenCookie(new JsonServiceClient(TestUrl));
        }

        [Test]
        public async Task Does_fetch_AccessToken_using_RefreshTokenCookies_HttpClient()
        {
            await AssertDoesGetAccessTokenUsingRefreshTokenCookie(new JsonHttpClient(TestUrl));
        }
        
        private static async Task AssertDoesGetAccessTokenUsingRefreshTokenCookie(IJsonServiceClient client)
        {
            var authResponse = await client.PostAsync(new Authenticate {
                provider = "credentials",
                UserName = "test",
                Password = "test",
            });

            var initialAccessToken = client.GetTokenCookie();
            var initialRefreshToken = client.GetRefreshTokenCookie();
            Assert.That(initialAccessToken, Is.Not.Null);
            Assert.That(initialRefreshToken, Is.Not.Null);

            var request = new Secured {Name = "test"};
            var response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));

            client.Post(new InvalidateLastAccessToken());
            // JwtAuthProvider.PrintDump(initialAccessToken);
            // JwtAuthProvider.PrintDump(initialRefreshToken);

            response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo(request.Name));
            var latestAccessToken = client.GetTokenCookie();
            Assert.That(latestAccessToken, Is.Not.EqualTo(initialAccessToken));
        }
    }
}