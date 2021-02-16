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
    }
}