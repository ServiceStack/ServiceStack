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
        //static string TestUrl = "http://test.servicestack.net";
        static string TestUrl = "https://localhost:5001";

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
    }
}