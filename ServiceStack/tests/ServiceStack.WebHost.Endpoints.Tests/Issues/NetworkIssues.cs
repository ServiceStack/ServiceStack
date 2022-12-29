using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [Route("/wait/{ForMs}")]
    public class Wait : IReturn<Wait>
    {
        public int ForMs { get; set; }
    }

    [TestFixture, Ignore("Requires external Services")]
    public class NetworkIssues
    {
        [Test]
        public async Task Simulate_broken_Network()
        {
            var client = new JsonServiceClient("http://test.servicestack.net");

            var response = await client.GetAsync(new Wait { ForMs = 50000 });

        }

        [Route("/hello")]
        public partial class Hello : IReturn<HelloResponse>
        {
            public virtual string Name { get; set; }
        }
        
        public partial class HelloResponse
        {
            public virtual string Result { get; set; }
        }

        [Test]
        public void Call_TestService_through_Fiddler_Proxy()
        {
            var client = new JsonServiceClient("http://test.servicestack.net") {
                Proxy = new WebProxy("http://localhost:8888")
            };

//            var response = await client.GetAsync(new Hello { Name = "Hello, World! 1 + 1 = 2" });
            var response = client.Get(new Hello { Name = "Hello, World! 1 + 1 = 2" });

            response.PrintDump();
        }

    }
}