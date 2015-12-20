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
    }
}